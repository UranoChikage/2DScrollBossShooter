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
        public BossState State { get; private set; } = BossState.LinearPhase;

        private int currentHealth;
        private float startY;
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
            if (bossCollider == null)
            {
                Debug.LogError("Collider2Dがアタッチされていません。");
            }
            else
            {
                bossCollider.isTrigger = true;
            }

            currentHealth = maxHealth;
            startY = transform.position.y;
            linearTimer = linearInterval;//最初の1発はすぐ撃つ
            ApplyPhaseColor();
            GameClearEvent += StopGame;
        }

        private void Update()
        {
            if (!IsPlaying) return;
            MoveVertical();
            //Playerの撃墜・復活Animation中は弾を撃たない(タイマーも進めないので、復活後に溜まった分が一斉に出ることもない)
            if (targetPlayer != null && targetPlayer.State != PlayerState.Alive) return;
            FireUpdate();
        }

        private void MoveVertical()
        {
            Vector3 pos = transform.position;
            pos.y = startY + Mathf.Sin(Time.time * moveSpeed) * moveRange;
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
            //TODO : 撃破時のAnimation、画面上の弾を消す処理、クリア画面への遷移を追加する
        }

        private void OnDestroy()
        {
            GameClearEvent -= StopGame;
        }
    }
}
