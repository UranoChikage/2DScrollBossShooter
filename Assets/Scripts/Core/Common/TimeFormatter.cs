namespace Core
{
    /// <summary>
    /// タイムの表示の形をそろえる<br/>
    /// インゲーム・リザルト・タイトルで同じ表記にするためにまとめている。
    /// </summary>
    public static class TimeFormatter
    {
        /// <summary>記録がないときの表記</summary>
        public const string Empty = "--:--.--";

        /// <summary>秒数を「分:秒.小数2桁」の形にする(例 : 01:05.23)</summary>
        public static string Format(float seconds)
        {
            int minutes = (int)(seconds / 60f);
            return $"{minutes:00}:{seconds % 60f:00.00}";
        }
    }
}
