using Core;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace GamePlay
{
    /// <summary>
    /// 最後に使われたデバイス(キーボード/ゲームパッド)を覚えておく<br/>
    /// Playerが居ないリザルト・タイトルでも操作のヒントを切り替えられるように、Playerから切り離してstaticにしている。<br/>
    /// 起動時に一度だけ入力の監視を始めるので、シーンに置く必要はない。シーンをまたいで値も残る。
    /// </summary>
    public static class DeviceTracker
    {
        public static Device LastDevice { get; private set; } = Device.Keyboard;

        /// <summary>LastDeviceが切り替わったときに呼ばれる</summary>
        public static event Action<Device> OnDeviceChanged;

        /// <summary>スティックをこれ以上倒したときだけ、ゲームパッドに切り替える(0〜1)</summary>
        private const float StickThreshold = 0.4f;

        private static IDisposable anyButtonSubscription;//onAnyButtonPressの購読解除用

        /// <summary>
        /// 入力の監視を始める
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            anyButtonSubscription?.Dispose();
            InputSystem.onActionChange -= OnActionChange;
            OnDeviceChanged = null;
            LastDevice = Device.Keyboard;

            //最後にボタンを押したデバイスを記録する拡張メソッド、Call()を使うと、
            //InputSystem.onAnyButtonPressに登録したActionが呼ばれるようになる。(Google調べ)
            anyButtonSubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
            InputSystem.onActionChange += OnActionChange;
        }

        /// <summary>
        /// ボタンが押されたデバイスを記録する<br/>
        /// マウスのクリックもキーボードのキーもボタンなので、どちらもKeyboard扱いにしている。
        /// </summary>
        private static void OnAnyButtonPressed(InputControl control) => UpdateLastDevice(control.device);

        /// <summary>
        /// Actionが実行されたときに、ゲームパッドのスティックの入力だけを記録する<br/>
        /// スティックはボタンではないので、onAnyButtonPressでは拾えない。<br/>
        /// マウスはここで見ない。EventSystemのUIのActionも通知されるので、
        /// マウスを動かすだけでKeyboardに切り替わってしまうため。マウスはクリックだけで判断する。
        /// </summary>
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (!(obj is InputAction action)) return;

            InputControl control = action.activeControl;
            if (control == null || !(control.device is Gamepad)) return;
            //Input Systemの既定のデッドゾーンを超えていても、ドリフトで微妙にずれているだけのスティックは無視する
            if (control.EvaluateMagnitude() < StickThreshold) return;
            UpdateLastDevice(control.device);
        }

        private static void UpdateLastDevice(InputDevice device)
        {
            Device next;
            if (device is Gamepad) next = Device.Gamepad;
            else if (device is Keyboard || device is Mouse) next = Device.Keyboard;
            else return;//nullもここで弾かれる

            if (next == LastDevice) return;
            LastDevice = next;
            Debug.Log($"{next}が最後に使われたデバイスになりました。");
            OnDeviceChanged?.Invoke(next);
        }
    }
}
