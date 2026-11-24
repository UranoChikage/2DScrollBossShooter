using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// クリアタイムの記録(早い順に上位5件)を保存・取得する<br/>
    /// リザルト・タイトルなどGameManagerが居ないシーンからも呼べるように、staticにしている。
    /// </summary>
    public static class ClearRecords
    {
        /// <summary>保存する記録の数</summary>
        public const int MaxCount = 5;

        private const string Key = "ClearTimes";
        private const char Separator = ',';

        /// <summary>
        /// クリアタイムを記録する<br/>
        /// 上位5件に入らなかったタイムは、保存されない。
        /// </summary>
        public static void Add(float time)
        {
            List<float> times = GetTopTimes();
            times.Add(time);
            times.Sort();//早い順
            if (times.Count > MaxCount) times.RemoveRange(MaxCount, times.Count - MaxCount);
            Save(times);
        }

        /// <summary>
        /// 保存されているタイムを、早い順に返す<br/>
        /// 記録がなければ空のListを返す。
        /// </summary>
        public static List<float> GetTopTimes()
        {
            List<float> times = new List<float>();
            string raw = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(raw)) return times;

            foreach (string text in raw.Split(Separator))
            {
                //壊れた値は読み飛ばす
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float time)) times.Add(time);
            }
            times.Sort();
            return times;
        }

        /// <summary>ベストタイム(1位)を返す。記録がなければnull</summary>
        public static float? GetBestTime()
        {
            List<float> times = GetTopTimes();
            if (times.Count == 0) return null;
            return times[0];
        }

        private static void Save(List<float> times)
        {
            List<string> texts = new List<string>();
            foreach (float time in times) texts.Add(time.ToString("R", CultureInfo.InvariantCulture));//小数点が環境で変わらないようにする
            PlayerPrefs.SetString(Key, string.Join(Separator.ToString(), texts));
            PlayerPrefs.Save();
        }
    }
}
