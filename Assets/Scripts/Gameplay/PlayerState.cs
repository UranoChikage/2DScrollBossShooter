namespace GamePlay
{
    /// <summary>
    /// Playerの行動状態<br/>
    /// 無敵(点滅)はこの状態とは別に管理する。
    /// </summary>
    public enum PlayerState
    {
        Alive,
        Downed,
        Respawning,
        GameOver
    }
}
