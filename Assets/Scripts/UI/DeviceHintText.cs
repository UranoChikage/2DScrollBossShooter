using Core;
using GamePlay;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// 操作のヒントのTextを、最後に使われたデバイスに合わせて切り替える<br/>
    /// 表示されたとき(OnEnable)に今のデバイスで表記を決め、表示中にデバイスが変わったら切り替える。<br/>
    /// 文言はInspectorで決めるので、使う側のUIはヒントのコードを持たなくてよい。<br/>
    /// Time.timeScaleが0のポーズ中でも、InputSystemのイベントで切り替わる。
    /// </summary>
    public class DeviceHintText : MonoBehaviour
    {
        [Tooltip("文言を書き換えるText")]
        [SerializeField] private Text text;
        [Header("デバイスごとの文言")]
        [SerializeField] private string keyboardText;
        [Tooltip("ゲームパッドはXboxの表記にしている")]
        [SerializeField] private string gamepadText;

        private void Awake()
        {
            if (text == null)
            {
                Debug.LogError("Textがアタッチされていません。");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Apply(DeviceTracker.LastDevice);
            DeviceTracker.OnDeviceChanged += Apply;
        }

        private void OnDisable() => DeviceTracker.OnDeviceChanged -= Apply;

        private void Apply(Device device)
        {
            text.text = device == Device.Gamepad ? gamepadText : keyboardText;
        }
    }
}
