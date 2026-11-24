using Core;
using GamePlay.Input;
using UnityEngine;
namespace GameUI
{
    /// <summary>
    /// タイトル画面を担当する<br/>
    /// ゲーム開始の操作を受け付ける。過去のベストタイムは、同じシーンに置いたRecordBoardが表示する。<br/>
    /// ボタンのクリックとショートカット(Menuマップ)のどちらでも、同じ処理が呼ばれる。<br/>
    /// 操作のヒントは、ボタンのTextに付けたDeviceHintTextが切り替える。
    /// </summary>
    public class TitleUI : MonoBehaviour
    {
        [Header("シーン遷移を行うSceneChanger")]
        [SerializeField] private SceneChanger sceneChanger;

        [Header("シーン名(Build Settingsに登録しておく)")]
        [SerializeField] private string inGameScene = "InGame";

        private InputSystem_Actions inputActions;

        private void Awake()
        {
            if (sceneChanger == null)
            {
                Debug.LogError("SceneChangerがアタッチされていません。");
                enabled = false;
                return;
            }
            inputActions = new InputSystem_Actions();
            //仕様書に記載がなかったので、開始専用のActionは作らず、MenuマップのRetryを開始のショートカットに流用している。
            inputActions.Menu.Retry.performed += ctx => OnStartButtonClicked();
        }

        public void OnStartButtonClicked()
        {
            AudioManager.Play(Sfx.UiConfirm);
            sceneChanger.LoadScene(inGameScene);
        }

        private void OnEnable() => inputActions?.Menu.Enable();
        private void OnDisable() => inputActions?.Menu.Disable();
        private void OnDestroy() => inputActions?.Dispose();
    }
}
