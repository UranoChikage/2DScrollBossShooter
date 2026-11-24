using Core;
using GamePlay;
using GamePlay.Input;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// リザルト画面を担当する<br/>
    /// 今回のタイム、ベストタイム、過去のベスト5を表示して、リトライ/タイトルへの操作を受け付ける。<br/>
    /// ボタンのクリックとショートカット(Menuマップ)のどちらでも、同じ処理が呼ばれる。<br/>
    /// 操作のヒントは、ボタンのTextに付けたDeviceHintTextが切り替える。
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        [Header("シーン遷移を行うSceneChanger")]
        [SerializeField] private SceneChanger sceneChanger;

        [Header("タイム")]
        [Tooltip("今回のタイム")]
        [SerializeField] private Text timeText;
        [SerializeField] private Text bestTimeText;

        [Header("過去のベスト5(1位から順に)")]
        [SerializeField] private Text[] rankingTexts;

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
            if (timeText == null || bestTimeText == null || rankingTexts == null)
            {
                Debug.LogError("タイムのTextがアタッチされていません。");
                enabled = false;
                return;
            }
            inputActions = new InputSystem_Actions();
            inputActions.Menu.Title.performed += ctx => OnToTitleButtonClicked();
            inputActions.Menu.Retry.performed += ctx => OnRetryButtonClicked();
        }

        private void Start()
        {
            timeText.text = TimeFormatter.Format(GameManager.LastElapsedTime);
            ShowRecords();
        }

        /// <summary>
        /// ベストタイムと過去のベスト5を表示する<br/>
        /// 記録が5件に満たない順位は、空の表記にする。
        /// </summary>
        private void ShowRecords()
        {
            List<float> times = ClearRecords.GetTopTimes();
            bestTimeText.text = times.Count > 0 ? TimeFormatter.Format(times[0]) : TimeFormatter.Empty;

            for (int i = 0; i < rankingTexts.Length; i++)
            {
                if (rankingTexts[i] == null) continue;
                string time = i < times.Count ? TimeFormatter.Format(times[i]) : TimeFormatter.Empty;
                rankingTexts[i].text = $"{i + 1}. {time}";
            }
        }

        public void OnRetryButtonClicked()
        {
            sceneChanger.LoadScene(inGameScene);
        }

        public void OnToTitleButtonClicked()
        {
            sceneChanger.LoadScene(titleScene);
        }

        private void OnEnable() => inputActions?.Menu.Enable();
        private void OnDisable() => inputActions?.Menu.Disable();
        private void OnDestroy() => inputActions?.Dispose();
    }
}
