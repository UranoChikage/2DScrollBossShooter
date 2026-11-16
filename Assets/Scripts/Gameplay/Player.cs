using Core;
using GamePlay.Input;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace GamePlay
{
    public class Player : MonoBehaviour, IDamageable, IInputProvider
    {
        [SerializeField] float speed;
        [SerializeField] float crouchSpeed;
        [SerializeField] float health;
        InputSystem_Actions inputActions;
        //IInputProvider=========================
        public Vector2 Move { get; private set; }
        public bool Fireheld { get; private set; }
        public Device LastDevice { get; private set; }
        public event Action OnBombPressed;
        public bool OnCrouched { get; private set; }
        //=========================================
        public void TakeDamage(float damage) => health -= damage;


        void Awake()
        {

            inputActions = new InputSystem_Actions();
            inputActions.Player.Fire.performed += ctx => Fireheld = true;
            inputActions.Player.Fire.canceled += ctx => Fireheld = false;
            inputActions.Player.Crouch.performed += ctx => OnCrouched = true;
            inputActions.Player.Crouch.canceled += ctx => OnCrouched = false;
            inputActions.Player.Bomb.performed += ctx => OnBombPressed?.Invoke();


        }

        private void Update()
        {
            InputUpdate();
            PlayerMove();
            // Input Action Assetで設定したControl Scheme名（例: "Keyboard&Mouse", "Gamepad"）が返る
        }
        void InputUpdate()
        {
            Move = inputActions.Player.Move.ReadValue<Vector2>();
        }

        void PlayerMove()
        {
            float t = Time.deltaTime;
            float moveSpeed = OnCrouched ? crouchSpeed : speed;
            Vector3 moveDir = new Vector3(Move.x, Move.y, 0);
            transform.position += moveDir * t * moveSpeed;
        }

        void CheckDevice(InputEventPtr _, InputDevice device)
        {
            if (device is Gamepad)
            {
                Debug.Log("Gamepadが接続されました。");
                LastDevice = Device.Gamepad;
            }
            else if (device is Keyboard || device is Mouse)
            {
                Debug.Log("Keyboardが接続されました。");
                LastDevice = Device.Keyboard;
            }

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
