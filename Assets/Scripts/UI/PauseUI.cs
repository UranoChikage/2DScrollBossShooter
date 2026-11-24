using Core;
using GamePlay;
using GamePlay.Input;
using UnityEngine;
namespace GameUI
{
    /// <summary>
    /// ポーズメニューの表示と、タイトル/リトライの操作を担当する<br/>
    /// ボタンのクリックとゲームパッドのどちらでも同じ処理が呼ばれる。<br/>
    /// ショートカットのMenuマップは、メニューを表示している間だけ有効にする。
    /// </summary>
    public class PauseUI : MonoBehaviour
    {

        [SerializeField] private GameManager gameManager;
        [Header("シーン遷移を行うSceneChanger")]
        [SerializeField] private SceneChanger sceneChanger;
        [Header("PauseメニューのUI")]
        [SerializeField] private GameObject menu;//Menu全体の親。ヒントはDeviceHintTextが切り替える

        [SerializeField] private string inGameScene = "InGame";
        [SerializeField] private string titleScene = "Title";
        private InputSystem_Actions inputActions;
        private void Awake()
        {
            if (menu == null)
            {
                Debug.LogError("メニューがアタッチされていません。");
                enabled = false;
                return;
            }
            if (gameManager == null || sceneChanger == null)
            {
                Debug.LogError("GameManagerまたはSceneChangerがアタッチされていません。");
                enabled = false;
                return;
            }
            inputActions = new InputSystem_Actions();
            inputActions.Menu.Title.performed += ctx => OnToTitleButtonClicked();
            inputActions.Menu.Retry.performed += ctx => OnRetryButtonClicked();
            gameManager.OnPauseMenuShowRequested += TogglePause;
            HidePauseMenu();
        }
        private void TogglePause(bool show)
        {
            if (!show) HidePauseMenu();
            else ShowPauseMenu();
        }
        private void ShowPauseMenu()
        {
            inputActions.Menu.Enable();
            menu.SetActive(true);
        }

        private void HidePauseMenu()
        {
            inputActions.Menu.Disable();
            menu.SetActive(false);
        }
        public void OnToTitleButtonClicked()
        {
            AudioManager.Play(Sfx.UiConfirm);
            sceneChanger.LoadScene(titleScene);
        }
        public void OnRetryButtonClicked()
        {
            AudioManager.Play(Sfx.UiConfirm);
            sceneChanger.LoadScene(inGameScene);
        }
        private void OnDestroy()
        {
            //購読解除
            if (gameManager != null) gameManager.OnPauseMenuShowRequested -= TogglePause;
            inputActions?.Dispose();
        }
    }
}
