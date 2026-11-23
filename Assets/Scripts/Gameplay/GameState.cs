namespace GamePlay
{
    /// <summary>
    /// ゲーム全体の状態<br/>
    /// Player・Bossの状態とは別に、GameManagerが1つだけ持つ。<br/>
    /// Intro : PlayerとBossの登場演出中(Playerは操作できない。Bossは無敵で弾も撃たない。タイムも進まない)
    /// </summary>
    public enum GameState
    {
        Intro,
        Playing,
        Paused,
        Result
    }
}
