using Core;
using GamePlay;
using GamePlay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        [SerializeField] private GameObject menu;//Menu全体の親
        [SerializeField] private Text toTitle;//デバイスによってヒントを切り替える
        [SerializeField] private Text retry;//デバイスによってヒントを切り替える

        [SerializeField] private string inGameScene = "InGame";
        [SerializeField] private string titleScene = "Title";
        private InputSystem_Actions inputActions;
        private void Awake()
        {
            if (menu == null || toTitle == null || retry == null)
            {
                Debug.LogError("メニューまたはTextがアタッチされていません。");
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
            menu.SetActive(true);
            switch (device)
            {
                case Device.Keyboard:
                    toTitle.text = "タイトルへ(T)";
                    retry.text = "再挑戦(R)";
                    break;
                case Device.Gamepad:
                    //今回はXboxの表記にしている
                    toTitle.text = "タイトルへ(Y)";
                    retry.text = "再挑戦(X)";
                    break;
            }
        }

        private void HidePauseMenu()
        {
            inputActions.Menu.Disable();
            menu.SetActive(false);
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
