using Core;
using GamePlay.Input;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
namespace GamePlay
{
    /// <summary>
    /// ユーザーが操作する。<br/>
    /// 今回は小規模のゲームとなるので責任を細かく分散しない方が扱いやすいと判断
    /// </summary>
    public class Player : MonoBehaviour, IDamegeable, IInputProvider
    {
        [Tooltip("Playerの移動速度")]
        [SerializeField] private float speed;
        [SerializeField] private float crouchSpeed;
        [Tooltip("残機(この回数だけ被弾するとゲームオーバー)")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private Vector2 screenMargin = new Vector2(0.5f, 0.5f);
        [Tooltip("クールタイムの長さ")]
        [SerializeField] private float coolTimeDuration = 0.5f;
        [Tooltip("爆弾の数")]
        [SerializeField] private int bombCount = 3;
        [Header("ゲーム全体の状態を持つGameManager")]
        [SerializeField] private GameManager gameManager;
        [Header("Playerの弾/爆弾を生成するBulletPool")]
        [SerializeField] private BulletPool bulletPool;
        [SerializeField] private BulletPool bombPool;

        [Header("撃墜・復活のAnimation")]
        [Tooltip("PlayerShotDown / PlayerRespawn / Fly のStateを持つAnimator")]
        [SerializeField] private Animator animator;
        [Tooltip("撃墜Animationのclip(長さの取得に使う)")]
        [SerializeField] private AnimationClip downClip;
        [Tooltip("復活Animationのclip(長さの取得に使う)")]
        [SerializeField] private AnimationClip respawnClip;

        [Header("登場演出のAnimation")]
        [Tooltip("登場演出のclip(長さの取得に使う)")]
        [SerializeField] private AnimationClip introClip;
        [Tooltip("Animatorにある登場演出のStateの名前")]
        [SerializeField] private string introStateName = "PlayerIntro";

        [Header("無敵(復活Animationが終わった直後)")]
        [Tooltip("点滅させるSpriteRenderer(Visual)")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("無敵の長さ(秒)")]
        [SerializeField] private float invincibleDuration = 2f;
        [Tooltip("点滅の切り替え間隔(秒)")]
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("点滅の色(通常の色との間で切り替える)")]
        [Tooltip("被弾して撃墜Animationが流れている間の点滅の色")]
        [SerializeField] private Color hitBlinkColor = Color.red;
        [Tooltip("復活後の無敵中の点滅の色(半透明にすると消えかけて見える)")]
        [SerializeField] private Color invincibleBlinkColor = new Color(1f, 1f, 1f, 0.3f);
        public event Action GameOverEvent;
        /// <summary>登場演出が終わったことをGameManagerに伝える</summary>
        public event Action IntroEndEvent;

        /// <summary>撃墜・復活の状態。Bossが発射を止めるときにも参照する。</summary>
        public PlayerState State { get; private set; } = PlayerState.Alive;

        //Animatorに作ってあるStateの名前
        private const string IdleStateName = "Fly";
        private const string DownStateName = "PlayerShotDown";
        private const string RespawnStateName = "PlayerRespawn";

        private int currentHealth;
        private Color baseColor = Color.white;//点滅から戻す元の色
        private bool isInvincible;
        private float currentCoolTime = 0f;
        private bool isCoolTime;
        private InputSystem_Actions inputActions;
        private IDisposable anyButtonSubscription;//onAnyButtonPressの購読解除用

        /// <summary>
        /// 操作できる状態か(GameManagerがPlayingのときだけtrue)<br/>
        /// 登場演出中(Intro)・ポーズ中・リザルト中は、操作や被ダメージを受け付けない。
        /// </summary>
        private bool IsPlaying => gameManager != null && gameManager.State == GameState.Playing;

        //IInputProvider=========================
        public Vector2 Move { get; private set; }
        public bool FireHeld { get; private set; }
        public Device LastDevice { get; private set; }
        public event Action OnBombPressed;
        public event Action OnPausePressed;//GameManagerがポーズの切り替えに使う
        public bool OnCrouched { get; private set; }
        //=========================================
        public void TakeDamage(int damage)
        {
            //撃墜・復活Anim中と無敵中はダメージを受けない。
            if (!IsPlaying || State != PlayerState.Alive || isInvincible) return;
            currentHealth -= damage;
            if (IsGameOver()) StartCoroutine(GameOverRoutine());
            else StartCoroutine(DownAndRespawnRoutine());
        }

        private void Awake()
        {
            if (gameManager == null) Debug.LogError("GameManagerがアタッチされていません。");
            if (animator == null) Debug.LogError("Animatorがアタッチされていません。");
            if (downClip == null || respawnClip == null) Debug.LogError("撃墜/復活のAnimationClipがアタッチされていません。");
            if (spriteRenderer == null) Debug.LogError("SpriteRendererがアタッチされていません。");
            else baseColor = spriteRenderer.color;
            InitInputActions();
            currentHealth = maxHealth;
            OnBombPressed += Bomb;
            GameOverEvent += StopGame;
        }

        private void Start() => StartCoroutine(IntroRoutine());

        /// <summary>
        /// 登場演出を流して、終わったらIntroEndEventで伝える<br/>
        /// 演出中(Intro状態)はPlayerは操作できない。
        /// </summary>
        private IEnumerator IntroRoutine()
        {
            PlayAnimation(introStateName);
            if (introClip != null) yield return new WaitForSeconds(introClip.length);

            PlayAnimation(IdleStateName);
            IntroEndEvent?.Invoke();
        }

        private void Update()
        {
            InputUpdate();//入力の更新
            //ポーズ中・リザルト中・撃墜/復活Animation中はここで止まる
            //FireHeldは保持されるので、復活後も押していれば撃ち始めることができる！
            if (!IsPlaying || State != PlayerState.Alive) return;
            PlayerMove();
            CoolTimeCount();
            if (FireHeld && !isCoolTime) Fire();
        }
        private void InputUpdate()
        {
            Move = inputActions.Player.Move.ReadValue<Vector2>();
            if (Move != Vector2.zero)
            {
                //Moveを今動かしているコントロールのデバイス
                UpdateLastDevice(inputActions.Player.Move.activeControl?.device);
            }
        }

        private void PlayerMove()
        {
            float moveSpeed = OnCrouched ? crouchSpeed : speed;
            Vector2 dir = Vector2.ClampMagnitude(Move, 1f);
            transform.position += (Vector3)dir * moveSpeed * Time.deltaTime;
            ClampToScreen();
        }

        /// <summary>
        /// Playerが画面内から出ないようにする
        /// </summary>
        private void ClampToScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector2 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, min.x + screenMargin.x, max.x - screenMargin.x);
            pos.y = Mathf.Clamp(pos.y, min.y + screenMargin.y, max.y - screenMargin.y);
            transform.position = pos;
        }
        private void CoolTimeCount()
        {
            if (isCoolTime)
            {
                currentCoolTime += Time.deltaTime;
                if (currentCoolTime >= coolTimeDuration)
                {
                    isCoolTime = false;
                    currentCoolTime = 0;
                }
            }
        }
        private void Fire()
        {
            isCoolTime = true;
            bulletPool?.Get(transform.position);
        }
        private void Bomb()
        {
            if (!IsPlaying || State != PlayerState.Alive || bombCount <= 0) return;//ポーズ中・撃墜/復活中はボムも使えない
            bombCount--;
            bombPool?.Get(transform.position);
        }
        /// <summary>
        /// GameOverかどうかのみを判断する<br/>
        /// ゲームを止める処理はStopGame()で行う。
        /// </summary>
        private bool IsGameOver()
        {
            if (currentHealth <= 0)
            {
                Debug.Log("Healthが0になりました。");
                return true;
            }
            return false;
        }
        private void StopGame()
        {
            Debug.Log("Game Over");
            OnBombPressed -= Bomb;
            //TODO : Text表示、リザルトシーンへの遷移(GameManager.GoToResult())を追加する
        }

        /// <summary>
        /// 残機が残っているとき : 撃墜Animation → 復活Animation → 無敵の順に進める<br/>
        /// WaitForSecondsはTime.timeScaleの影響を受けるので、ポーズ中は待ち時間も止まる。
        /// </summary>
        private IEnumerator DownAndRespawnRoutine()
        {
            State = PlayerState.Downed;
            PlayAnimation(DownStateName);
            if (downClip != null)
            {
                StartCoroutine(BlinkRoutine(hitBlinkColor, downClip.length));//撃墜Animationと同時に点滅
                yield return new WaitForSeconds(downClip.length);
            }

            //撃墜された位置から復活する(位置は動かさない)
            State = PlayerState.Respawning;
            PlayAnimation(RespawnStateName);
            if (respawnClip != null) yield return new WaitForSeconds(respawnClip.length);

            PlayAnimation(IdleStateName);
            State = PlayerState.Alive;
            StartCoroutine(InvincibleRoutine());
        }

        /// <summary>
        /// 残機が0のとき : 撃墜Animationを最後まで流してからGameOverEventを呼ぶ
        /// </summary>
        private IEnumerator GameOverRoutine()
        {
            State = PlayerState.Downed;
            PlayAnimation(DownStateName);
            if (downClip != null)
            {
                StartCoroutine(BlinkRoutine(hitBlinkColor, downClip.length));
                yield return new WaitForSeconds(downClip.length);
            }

            State = PlayerState.GameOver;
            GameOverEvent?.Invoke();
        }

        /// <summary>
        /// 復活直後の無敵<br/>
        /// 復活Animationとは別で、点滅させながらinvincibleDuration秒間ダメージを受けない。
        /// </summary>
        private IEnumerator InvincibleRoutine()
        {
            isInvincible = true;
            yield return BlinkRoutine(invincibleBlinkColor, invincibleDuration);//点滅が終わるまで待つ
            isInvincible = false;
        }

        /// <summary>
        /// 通常の色と指定した色をblinkInterval秒ごとに切り替えて、duration秒間点滅させる<br/>
        /// 終わったら必ず通常の色に戻す。
        /// </summary>
        private IEnumerator BlinkRoutine(Color blinkColor, float duration)
        {
            if (spriteRenderer == null) yield break;

            bool isBlinkColor = false;
            float timer = 0f;
            while (timer < duration)
            {
                isBlinkColor = !isBlinkColor;
                spriteRenderer.color = isBlinkColor ? blinkColor : baseColor;
                yield return new WaitForSeconds(blinkInterval);
                timer += blinkInterval;
            }
            spriteRenderer.color = baseColor;
        }

        private void PlayAnimation(string stateName)
        {
            if (animator != null) animator.Play(stateName);
        }

        /// <summary>
        /// 最後にボタンを押したデバイスを記録する<br/>
        /// マウスのクリックもキーボードのキーもボタンなので、どちらもKeyboard扱いにしている。
        /// </summary>
        private void OnAnyButtonPressed(InputControl control) => UpdateLastDevice(control.device);

        /// <summary>
        /// 最後に使われたデバイスを更新する<br/>
        /// OnAnyButtonPressedと、Moveの入力の両方から呼ばれる。
        /// </summary>
        private void UpdateLastDevice(InputDevice device)
        {
            Device next;
            if (device is Gamepad) next = Device.Gamepad;
            else if (device is Keyboard || device is Mouse) next = Device.Keyboard;
            else return;//nullもここで弾かれる

            if (next == LastDevice) return;
            LastDevice = next;
            Debug.Log($"{next}が最後に使われたデバイスになりました。");
        }
        private void InitInputActions()
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Fire.performed += ctx => FireHeld = true;
            inputActions.Player.Fire.canceled += ctx => FireHeld = false;
            inputActions.Player.Crouch.performed += ctx => OnCrouched = true;
            inputActions.Player.Crouch.canceled += ctx => OnCrouched = false;
            inputActions.Player.Bomb.performed += ctx => OnBombPressed?.Invoke();
            inputActions.Player.Pause.performed += ctx => OnPausePressed?.Invoke();
        }
        private void OnEnable()
        {
            inputActions.Enable();
            //最後にボタンを押したデバイスを記録する拡張メソッド、Call()を使うと、
            //InputSystem.onAnyButtonPressに登録したActionが呼ばれるようになる。(Google調べ)
            anyButtonSubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }
        private void OnDisable()
        {
            inputActions.Disable();
            anyButtonSubscription?.Dispose();
        }
        private void OnDestroy()
        {
            inputActions.Dispose();
            OnBombPressed -= Bomb;
            GameOverEvent -= StopGame;
        }
    }
}
