namespace GamePlay
{
    /// <summary>
    /// Bossのライフに応じた段階<br/>
    /// 段階が進むと弾の発射パターンが追加される(前の段階のパターンも続ける)。
    /// </summary>
    public enum BossState
    {
        LinearPhase,
        FanShapePhase,
        HomingPhase
    }
}
