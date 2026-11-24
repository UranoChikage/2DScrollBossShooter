using Core;
using GamePlay;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// ベストタイムと、過去のベスト5を表示する<br/>
    /// タイトルとリザルトの両方で同じ表示をするために、切り出した！<br/>
    /// シーンが始まったときに、保存されている記録(ClearRecords)を読んで表示する。
    /// </summary>
    public class RecordBoard : MonoBehaviour
    {
        [SerializeField] private Text bestTimeText;
        [Header("過去のベスト5(1位から順に)")]
        [SerializeField] private Text[] rankingTexts;

        private void Awake()
        {
            if (bestTimeText == null || rankingTexts == null)
            {
                Debug.LogError("タイムのTextがアタッチされていません。");
                enabled = false;
            }
        }

        private void Start() => Show();

        /// <summary>
        /// 記録を読み直して表示する<br/>
        /// 記録が5件に満たない順位は、空の表記にする。
        /// </summary>
        public void Show()
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
    }
}
