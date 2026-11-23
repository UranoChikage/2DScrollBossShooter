using Core;
using GamePlay.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
        [Tooltip("Playerの最大体力")]
        [SerializeField] private int maxHealth = 1;
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
        public event Action GameOverEvent;

        private int currentHealth;
        private float currentCoolTime = 0f;
        private bool isCoolTime;
        private InputSystem_Actions inputActions;

        /// <summary>
        /// ゲームが進行中か(GameManagerがPlayingのときだけtrue)<br/>
        /// ポーズ中・リザルト中は操作や被ダメージを受け付けない。
        /// </summary>
        private bool IsPlaying => gameManager != null && gameManager.State == GameState.Playing;

        //IInputProvider=========================
        public Vector2 Move { get; private set; }
        public bool FireHeld { get; private set; }
        public Device LastDevice { get; private set; }
        public event Action OnBombPressed;
        public bool OnCrouched { get; private set; }
        //=========================================
        public void TakeDamage(int damage)
        {
            if (!IsPlaying) return;
            currentHealth -= damage;
            if (IsGameOver()) GameOverEvent?.Invoke();
        }

        private void Awake()
        {
            if (gameManager == null) Debug.LogError("GameManagerがアタッチされていません。");
            InitInputActions();
            currentHealth = maxHealth;
            OnBombPressed += Bomb;
            GameOverEvent += StopGame;
        }

        private void Update()
        {
            InputUpdate();//入力の更新
            if (!IsPlaying)
            {
                //ポーズ中・リザルト中はここで止まる(ポーズ中に弾が出続けないようにする)
                //TODO : ゲームオーバー時の処理を追加する　入力をUI操作に切り替える。
                return;
            }
            PlayerMove();
            CoolTimeCount();
            if (FireHeld && !isCoolTime) Fire();
        }
        private void InputUpdate()
        {
            Move = inputActions.Player.Move.ReadValue<Vector2>();
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
            if (!IsPlaying || bombCount <= 0) return;//ポーズ中はボムも使えない
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
            //TODO : ゲームオーバー時のAnimation等処理を追加する
        }

        /// <summary>
        /// 接続されているデバイスを確認する
        /// </summary>
        private void CheckDevice(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;

            Device next;
            if (device is Gamepad) next = Device.Gamepad;
            else if (device is Keyboard || device is Mouse) next = Device.Keyboard;
            else return;

            if (next == LastDevice) return;

            if (next == Device.Gamepad)
            {
                Debug.Log("Gamepadが接続されました。");
                LastDevice = Device.Gamepad;
            }
            else if (next == Device.Keyboard || CheckMouseClick())
            {
                Debug.Log("Keyboardが接続されました。");
                LastDevice = Device.Keyboard;
            }
        }
        private bool CheckMouseClick()
        {

            if (Mouse.current.leftButton.isPressed)
            {
                Debug.Log("マウスが左クリックされました。");
                return true;
            }
            return false;
        }
        private void InitInputActions()
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Fire.performed += ctx => FireHeld = true;
            inputActions.Player.Fire.canceled += ctx => FireHeld = false;
            inputActions.Player.Crouch.performed += ctx => OnCrouched = true;
            inputActions.Player.Crouch.canceled += ctx => OnCrouched = false;
            inputActions.Player.Bomb.performed += ctx => OnBombPressed?.Invoke();
        }
        private void OnEnable()
        {
            inputActions.Enable();
            InputSystem.onEvent += CheckDevice;
        }
        private void OnDisable()
        {
            inputActions.Disable();
            InputSystem.onEvent -= CheckDevice;

        }
        private void OnDestroy()
        {
            inputActions.Dispose();
            OnBombPressed -= Bomb;
            GameOverEvent -= StopGame;
        }
    }
}
