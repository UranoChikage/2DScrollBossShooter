namespace GamePlay
{
    /// <summary>
    /// ゲーム全体の状態<br/>
    /// Player・Bossの状態とは別に、GameManagerが1つだけ持つ。
    /// </summary>
    public enum GameState
    {
        Playing,
        Paused,
        Result
    }
}
