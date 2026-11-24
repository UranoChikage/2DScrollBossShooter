using Core;
using GamePlay;
using GamePlay.Input;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// リザルト画面を担当する<br/>
    /// 今回のタイムを表示して、リトライ/タイトルへの操作を受け付ける。<br/>
    /// ベストタイムと過去のベスト5は、RecordBoardが表示するので注意。<br/>
    /// ボタンのクリックとショートカット(Menuマップ)のどちらでも、同じ処理が呼ばれる。<br/>
    /// 操作のヒントは、ボタンのTextに付けたDeviceHintTextが切り替える。
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        [Header("シーン遷移を行うSceneChanger")]
        [SerializeField] private SceneChanger sceneChanger;

        [Header("タイム")]
        [Tooltip("今回のタイム。ベストタイムとベスト5はRecordBoardが表示する")]
        [SerializeField] private Text timeText;

        [Header("シーン名(Build Settingsに登録しておく)")]
        [SerializeField] private string inGameScene = "InGame";
        [SerializeField] private string titleScene = "Title";

        private InputSystem_Actions inputActions;

        private void Awake()
        {
            if (sceneChanger == null)
            {
                Debug.LogError("SceneChangerがアタッチされていません。");
                enabled = false;
                return;
            }
            if (timeText == null)
            {
                Debug.LogError("今回のタイムのTextがアタッチされていません。");
                enabled = false;
                return;
            }
            inputActions = new InputSystem_Actions();
            inputActions.Menu.Title.performed += ctx => OnToTitleButtonClicked();
            inputActions.Menu.Retry.performed += ctx => OnRetryButtonClicked();
        }

        private void Start() => timeText.text = TimeFormatter.Format(GameManager.LastElapsedTime);

        public void OnRetryButtonClicked()
        {
            AudioManager.Play(Sfx.UiConfirm);
            sceneChanger.LoadScene(inGameScene);
        }

        public void OnToTitleButtonClicked()
        {
            AudioManager.Play(Sfx.UiConfirm);
            sceneChanger.LoadScene(titleScene);
        }

        private void OnEnable() => inputActions?.Menu.Enable();
        private void OnDisable() => inputActions?.Menu.Disable();
        private void OnDestroy() => inputActions?.Dispose();
    }
}
