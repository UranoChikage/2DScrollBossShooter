using Core;
using System;
using System.Collections;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// ボス<br/>
    /// 小規模のゲームなので、Player同様、移動・弾の発射・段階管理をこのクラスにまとめている。
    /// </summary>
    public class Boss : MonoBehaviour, IDamegeable
    {
        [SerializeField] private Collider2D bossCollider;
        [Tooltip("Bossの最大体力")]
        [SerializeField] private int maxHealth = 100;
        [Tooltip("Bossの第二段階目に移る体力の割合(%)")]
        [SerializeField] private int secondPhaseHealth = 70;
        [Tooltip("Bossの第三段階目に移る体力の割合(%)")]
        [SerializeField] private int thirdPhaseHealth = 30;
        [Tooltip("Playerに接触したときに与えるダメージ")]
        [SerializeField] private int contactDamage = 1;

        [Header("ゲーム全体の状態を持つGameManager")]
        [SerializeField] private GameManager gameManager;

        [Header("上下移動")]
        [Tooltip("上下移動の速さ")]
        [SerializeField] private float moveSpeed = 1.5f;
        [Tooltip("初期位置から上下に動く幅")]
        [SerializeField] private float moveRange = 4f;

        [Header("段階ごとの動きの変化(第一段階は上の値のまま)")]
        [Tooltip("第二段階の上下移動の速さの倍率(第一段階の速さに対して)")]
        [SerializeField] private float fanSpeedScale = 1.5f;
        [Tooltip("第三段階の上下移動の速さの倍率(第一段階の速さに対して)")]
        [SerializeField] private float homingSpeedScale = 2f;
        [Tooltip("第三段階で、初期位置から前後(左右)に揺れる幅。0なら揺れない")]
        [SerializeField] private float homingSwayRange = 1f;
        [Tooltip("前後(左右)に揺れる速さ")]
        [SerializeField] private float swaySpeed = 1f;
        [Tooltip("段階が変わったとき、速さと揺れ幅が新しい値に近づく速さ(急に位置が飛ばないようにする)")]
        [SerializeField] private float moveChangeRate = 2f;

        [Header("弾")]
        [SerializeField] private BulletPool bulletPool;
        [Tooltip("狙う対象(Player)")]
        [SerializeField] private Transform target;

        [Header("弾幕の種類ごとの弾のSprite")]
        [SerializeField] private Sprite linearSprite;
        [SerializeField] private Sprite fanSprite;
        [SerializeField] private Sprite homingSprite;

        [Header("見た目(色)")]
        [Tooltip("色を変えるBossのSpriteRenderer")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("第一段階(直進)のときのBossの色")]
        [SerializeField] private Color linearPhaseColor = Color.white;
        [Tooltip("第二段階(扇形)のときのBossの色")]
        [SerializeField] private Color fanPhaseColor = Color.yellow;
        [Tooltip("第三段階(ホーミング)のときのBossの色")]
        [SerializeField] private Color homingPhaseColor = Color.magenta;
        [Tooltip("被弾したときに一回点滅する色")]
        [SerializeField] private Color hitFlashColor = Color.red;
        [Tooltip("被弾の点滅の長さ(秒)")]
        [SerializeField] private float hitFlashDuration = 0.1f;

        [Header("登場演出のAnimation")]
        [Tooltip("登場演出のclip(長さの取得に使う)")]
        [SerializeField] private AnimationClip introClip;
        [Tooltip("登場演出に遷移するTriggerの名前")]
        [SerializeField] private string introTriggerName = "Intro";

        [Header("撃破Animation")]
        [Tooltip("撃破Animationを流すAnimator")]
        [SerializeField] private Animator animator;
        [Tooltip("撃破Animationのclip(長さの取得に使う)")]
        [SerializeField] private AnimationClip defeatClip;
        [Tooltip("撃破Animationに遷移するTriggerの名前")]
        [SerializeField] private string defeatTriggerName = "Defeat";

        [Header("発射インターバル(秒)")]
        [SerializeField] private float linearInterval = 5f;
        [SerializeField] private float fanInterval = 7f;
        [SerializeField] private float homingInterval = 10f;

        [Header("扇形")]
        [Tooltip("奇数個のときの弾数")]
        [SerializeField] private int fanOddCount = 5;
        [Tooltip("偶数個のときの弾数")]
        [SerializeField] private int fanEvenCount = 4;
        [Tooltip("扇の全体の角度(度)")]
        [SerializeField] private float fanAngle = 60f;

        public event Action GameClearEvent;
        /// <summary>登場演出が終わったことをGameManagerに伝える</summary>
        public event Action IntroEndEvent;
        /// <summary>体力が変わったことをUIに伝える。引数は残り体力の割合(0〜1)</summary>
        public event Action<float> OnHealthChanged;
        public BossState State { get; private set; } = BossState.LinearPhase;

        /// <summary>残り体力の割合(0〜1)</summary>
        public float HealthRatio => Mathf.Clamp01((float)currentHealth / maxHealth);//これは機械が見るだけなのでわざわざ百分率にはしない。
        /// <summary>第二段階に移る体力の割合(0〜1)。UIのゲージの区切りに使う。</summary>
        public float SecondPhaseRatio => secondPhaseHealth / 100f;
        /// <summary>第三段階に移る体力の割合(0〜1)。UIのゲージの区切りに使う。</summary>
        public float ThirdPhaseRatio => thirdPhaseHealth / 100f;

        private int currentHealth;
        private float startX;
        private float startY;
        private float moveAngle;//上下移動の角度。速さが変わっても位置が飛ばないよう、時間ではなく角度を積み上げる
        private float swayAngle;//前後の揺れの角度
        private float currentMoveSpeed;//今の上下移動の速さ(段階に合わせて目標の値に近づく)
        private float currentSway;//今の前後の揺れ幅(段階に合わせて目標の値に近づく)
        private float linearTimer;
        private float fanTimer;
        private float homingTimer;
        private bool isFanOdd = true;//扇形は奇数個と偶数個を交互に撃つ
        private Player targetPlayer;//targetのPlayer(撃墜・復活中かを見るため)
        private Color currentPhaseColor = Color.white;//今の段階の色(被弾の点滅から戻す色)
        private Coroutine hitFlashCoroutine;//点滅中かの判断と、連続被弾時の点滅のやり直しに使う

        /// <summary>
        /// ゲームが進行中か(GameManagerがPlayingのときだけtrue)<br/>
        /// ポーズ中・リザルト中は動かず、ダメージも受けない。
        /// </summary>
        private bool IsPlaying => gameManager != null && gameManager.State == GameState.Playing;

        public void TakeDamage(int damage)
        {
            if (!IsPlaying) return;
            currentHealth -= damage;
            UpdateState();
            OnHealthChanged?.Invoke(HealthRatio);
            HitFlash();
            if (currentHealth <= 0) GameClearEvent?.Invoke();
        }

        private void Awake()
        {
            if (gameManager == null) Debug.LogError("GameManagerがアタッチされていません。");
            if (bulletPool == null) Debug.LogError("BulletPoolがアタッチされていません。");
            if (target == null) Debug.LogError("狙う対象(Player)がアタッチされていません。");
            else target.TryGetComponent(out targetPlayer);
            if (spriteRenderer == null) Debug.LogError("SpriteRendererがアタッチされていません。");
            if (animator == null || defeatClip == null) Debug.LogError("撃破用のAnimatorまたはAnimationClipがアタッチされていません。");
            if (bossCollider == null)
            {
                Debug.LogError("Collider2Dがアタッチされていません。");
            }
            else
            {
                bossCollider.isTrigger = true;
            }

            currentHealth = maxHealth;
            startX = transform.position.x;
            startY = transform.position.y;
            currentMoveSpeed = moveSpeed;
            linearTimer = linearInterval;//最初の1発はすぐ撃つ
            if (animator != null) animator.enabled = false;//演出のときだけ有効にする
            ApplyPhaseColor();
            GameClearEvent += StopGame;
        }

        private void Update()
        {
            if (!IsPlaying) return;
            Move();
            //Playerの撃墜・復活Animation中は弾を撃たない(タイマーも進めないので、復活後に溜まった分が一斉に出ることもない)
            if (targetPlayer != null && targetPlayer.State != PlayerState.Alive) return;
            FireUpdate();
        }

        private void Start() => StartCoroutine(IntroRoutine());

        /// <summary>
        /// 登場演出を流して、終わったらIntroEndEventで伝える<br/>
        /// 演出中はGameManagerがIntro状態なので、Bossは無敵(TakeDamageが無効)で弾も撃たない。
        /// </summary>
        private IEnumerator IntroRoutine()
        {
            if (animator != null)
            {
                //なぜか色を上書きされてしまうので、演出中以外はAnimatorを無効にしておく必要がある
                animator.enabled = true;//Animatorは演出中だけ有効にする
                animator.SetTrigger(introTriggerName);
            }
            if (introClip != null) yield return new WaitForSeconds(introClip.length);

            if (animator != null) animator.enabled = false;
            ApplyPhaseColor();//演出で変わった色を、今の段階の色に戻す
            IntroEndEvent?.Invoke();
        }

        /// <summary>
        /// 段階に応じた速さ・揺れ幅で動く<br/>
        /// 第一段階 : 上下のみ、第二段階 : 上下が速くなる、第三段階 : さらに速くなり前後にも揺れる。<br/>
        /// 段階が変わったときは、速さと揺れ幅を目標の値へ少しずつ近づけて、動きが急に変わらないようにする。
        /// </summary>
        private void Move()
        {
            float targetSpeed;
            float targetSway;
            switch (State)
            {
                case BossState.FanShapePhase:
                    targetSpeed = moveSpeed * fanSpeedScale;
                    targetSway = 0f;
                    break;
                case BossState.HomingPhase:
                    targetSpeed = moveSpeed * homingSpeedScale;
                    targetSway = homingSwayRange;
                    break;
                default:
                    targetSpeed = moveSpeed;
                    targetSway = 0f;
                    break;
            }

            float step = moveChangeRate * Time.deltaTime;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, step);
            currentSway = Mathf.MoveTowards(currentSway, targetSway, step);

            moveAngle += currentMoveSpeed * Time.deltaTime;
            swayAngle += swaySpeed * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y = startY + Mathf.Sin(moveAngle) * moveRange;
            pos.x = startX + Mathf.Sin(swayAngle) * currentSway;
            transform.position = pos;
        }

        /// <summary>
        /// 段階に応じて、有効なパターンのインターバルを数えて撃つ<br/>
        ///「弾発射パターンが"追加"される」と書かれていたので、前のパターンも撃つようにしている。
        /// </summary>
        private void FireUpdate()
        {
            linearTimer += Time.deltaTime;
            if (linearTimer >= linearInterval)
            {
                linearTimer = 0f;
                FireLinear();
            }

            if (State >= BossState.FanShapePhase)
            {
                fanTimer += Time.deltaTime;
                if (fanTimer >= fanInterval)
                {
                    fanTimer = 0f;
                    FireFan();
                }
            }

            if (State >= BossState.HomingPhase)
            {
                homingTimer += Time.deltaTime;
                if (homingTimer >= homingInterval)
                {
                    homingTimer = 0f;
                    FireHoming();
                }
            }
        }

        //初期状態 : Playerに向かって一直線
        private void FireLinear()
        {
            BossBullet bullet = SpawnBullet(linearSprite);
            bullet?.Launch(DirectionToTarget());
        }

        //1段階目 : 前方(左)に奇数個・偶数個を交互に扇形で撃つ
        private void FireFan()
        {
            int count = isFanOdd ? fanOddCount : fanEvenCount;
            isFanOdd = !isFanOdd;

            for (int i = 0; i < count; i++)
            {
                // 1発のときは正面のみ。2発以上は扇の端から端まで等間隔に並べる
                float angle = count == 1 ? 0f : -fanAngle / 2f + fanAngle / (count - 1) * i;
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.left;
                BossBullet bullet = SpawnBullet(fanSprite);
                bullet?.Launch(dir);
            }
        }

        //2段階目 : Playerに向かうホーミング弾
        private void FireHoming()
        {
            BossBullet bullet = SpawnBullet(homingSprite);
            bullet?.LaunchHoming(DirectionToTarget(), target);
        }

        /// <summary>
        /// 弾をプールから取り出して、弾幕の種類に合わせたSpriteにする<br/>
        /// spriteがnullのときは、弾のSpriteを変更しない。
        /// </summary>
        private BossBullet SpawnBullet(Sprite sprite)
        {
            BossBullet bullet = bulletPool?.Get(transform.position) as BossBullet;
            bullet?.SetSprite(sprite);
            return bullet;
        }

        private Vector2 DirectionToTarget()
        {
            if (target == null) return Vector2.left;
            return (target.position - transform.position).normalized;
        }

        /// <summary>
        /// 残りライフの割合から段階を決める(1/3ずつ減るごとに段階が上がる)
        /// </summary>
        private void UpdateState()
        {
            float ratio = ((float)currentHealth / maxHealth) * 100;//百分率にする
            BossState next = State;
            if (ratio <= thirdPhaseHealth) next = BossState.HomingPhase;//三段階目 : Playerに向かうホーミング弾
            else if (ratio <= secondPhaseHealth) next = BossState.FanShapePhase;//二段階目 : 前方(左)に奇数個・偶数個を交互に扇形で撃つ
            else next = BossState.LinearPhase;//初期状態 : Playerに向かって一直線

            if (next == State) return;
            State = next;
            ApplyPhaseColor();
            Debug.Log($"Bossが{State}に移行しました。");
        }

        /// <summary>
        /// 今の段階に合わせた色にする<br/>
        /// 被弾の点滅中は、点滅が終わったときに戻る色だけを更新する。
        /// </summary>
        private void ApplyPhaseColor()
        {
            switch (State)
            {
                case BossState.FanShapePhase:
                    currentPhaseColor = fanPhaseColor;
                    break;
                case BossState.HomingPhase:
                    currentPhaseColor = homingPhaseColor;
                    break;
                default:
                    currentPhaseColor = linearPhaseColor;
                    break;
            }
            if (spriteRenderer != null && hitFlashCoroutine == null) spriteRenderer.color = currentPhaseColor;
        }

        /// <summary>
        /// 被弾したときに一回点滅する<br/>
        /// 点滅中にもう一度被弾した場合は、点滅を最初からやり直す。
        /// </summary>
        private void HitFlash()
        {
            if (spriteRenderer == null) return;
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = currentPhaseColor;
            hitFlashCoroutine = null;
        }

        /// <summary>
        /// 重なっている間は毎フレーム呼ばれる<br/>
        /// 無敵が切れた時点で重なっていたらダメージを受ける仕様のため、Enterではなくこちらを使う。
        /// </summary>
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPlaying) return;
            if (other.TryGetComponent<Player>(out var player))
            {
                player.TakeDamage(contactDamage);
            }
        }

        private void StopGame()
        {
            Debug.Log("Game Clear");
            StartCoroutine(DefeatRoutine());
            //TODO : 画面上の弾を消す処理を追加する
        }

        /// <summary>
        /// 撃破Animationを流して、終わったらリザルトへ移る<br/>
        /// 撃破の時点でGameManagerはResult状態になっているので、Bossの移動や発射は止まっている。
        /// </summary>
        private IEnumerator DefeatRoutine()
        {
            if (animator != null)
            {
                animator.enabled = true;//演出のために有効にする(終わったらシーン遷移するので切り直さない)
                animator.SetTrigger(defeatTriggerName);
            }
            if (defeatClip != null) yield return new WaitForSeconds(defeatClip.length);

            if (gameManager != null) gameManager.GoToResult();
        }

        private void OnDestroy()
        {
            GameClearEvent -= StopGame;
        }
    }
}
