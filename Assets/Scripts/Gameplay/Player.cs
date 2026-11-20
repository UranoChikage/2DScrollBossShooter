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
    public class Player : MonoBehaviour, IDamageable, IInputProvider
    {
        [Tooltip("Playerの移動速度")]
        [SerializeField] float speed;
        [SerializeField] float crouchSpeed;
        [Tooltip("Playerの最大体力")]
        [SerializeField] float MaxHealth;
        [Tooltip("クールタイムの長さ")]
        [SerializeField] float coolTimeDuration;
        [Header("Playerの弾を生成するBulletPool")]
        [SerializeField] BulletPool bulletPool;

        float currentHealth;
        float currentCoolTime = 0f;
        bool isCoolTime;
        InputSystem_Actions inputActions;
        //IInputProvider=========================
        public Vector2 Move { get; private set; }
        public bool Fireheld { get; private set; }
        public Device LastDevice { get; private set; }
        public event Action OnBombPressed;
        public bool OnCrouched { get; private set; }
        //=========================================
        public void TakeDamage(float damage) => MaxHealth -= damage;


        void Awake()
        {
            InitInputActions();
            currentHealth = MaxHealth;
        }

        private void Update()
        {
            InputUpdate();
            PlayerMove();
            CoolTimeCount();
            if (Fireheld && !isCoolTime) Fire();
        }
        void InputUpdate()
        {
            Move = inputActions.Player.Move.ReadValue<Vector2>();
        }

        Vector2 PlayerMove()
        {
            float moveSpeed = OnCrouched ? crouchSpeed : speed;
            Vector2 dir = Vector2.ClampMagnitude(Move, 1f);
            transform.position += (Vector3)dir * moveSpeed * Time.deltaTime;
            return dir;
        }
        void CoolTimeCount()
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
        void Fire()
        {
            isCoolTime = true;
            bulletPool?.Get(transform.position);
        }

        /// <summary>
        /// 接続されているデバイスを確認する
        /// </summary>
        void CheckDevice(InputEventPtr eventPtr, InputDevice device)
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
        bool CheckMouseClick()
        {

            if (Mouse.current.leftButton.isPressed)
            {
                Debug.Log("マウスが左クリックされました。");
                return true;
            }
            return false;
        }
        void InitInputActions()
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Fire.performed += ctx => Fireheld = true;
            inputActions.Player.Fire.canceled += ctx => Fireheld = false;
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
        private void OnDestroy() => inputActions.Dispose();
    }
}
