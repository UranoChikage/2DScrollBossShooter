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

        private static IDisposable anyButtonSubscription;//onAnyButtonPressの購読解除用

        /// <summary>
        /// 入力の監視を始める<br/>
        /// Domain Reloadを切っている場合に前回の購読やイベントが残らないよう、最初に全部やり直す。
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
        /// Actionが実行されたときの、そのActionを動かしているデバイスを記録する<br/>
        /// スティックの移動のようにボタンではない入力も、これで拾える。
        /// </summary>
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is InputAction action) UpdateLastDevice(action.activeControl?.device);
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
