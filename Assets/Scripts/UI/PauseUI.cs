using Core;
using GamePlay;
using GamePlay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [Header("PauseメニューのUI")]
        [SerializeField] private GameObject pauseMenu_Keyboard;
        [SerializeField] private GameObject pauseMenu_Gamepad;

        [SerializeField] private string inGameScene = "InGame";
        [SerializeField] private string titleScene = "Title";
        private InputSystem_Actions inputActions;
        private void Awake()
        {
            if (pauseMenu_Keyboard == null || pauseMenu_Gamepad == null)
            {
                Debug.LogError("Pauseメニューがアタッチされていません。");
                enabled = false;
                return;
            }
            if (gameManager == null)
            {
                Debug.LogError("GameManagerがアタッチされていません。");
                enabled = false;
                return;
            }
            inputActions = new InputSystem_Actions();
            inputActions.Menu.Title.performed += ctx => OnToTitleButtonClicked();
            inputActions.Menu.Retry.performed += ctx => OnRetryButtonClicked();
            gameManager.OnPauseMenuShowRequested += TogglePause;
            HidePauseMenu();
        }
        private void TogglePause(Device device, bool show)
        {
            if (!show) HidePauseMenu();
            else ShowPauseMenu(device);
        }
        private void ShowPauseMenu(Device device)
        {
            inputActions.Menu.Enable();
            switch (device)
            {
                case Device.Keyboard:
                    pauseMenu_Keyboard.SetActive(true);
                    break;
                case Device.Gamepad:
                    pauseMenu_Gamepad.SetActive(true);
                    break;
            }
        }

        private void HidePauseMenu()
        {
            inputActions.Menu.Disable();
            pauseMenu_Keyboard.SetActive(false);
            pauseMenu_Gamepad.SetActive(false);
        }
        public void OnToTitleButtonClicked()
        {
            SceneManager.LoadScene(titleScene);
        }
        public void OnRetryButtonClicked()
        {
            SceneManager.LoadScene(inGameScene);
        }
        private void OnDestroy()
        {
            //購読解除
            if (gameManager != null) gameManager.OnPauseMenuShowRequested -= TogglePause;
            inputActions?.Dispose();
        }
    }
}
