using System;
using UnityEngine;

namespace Core
{
    /// <summary>効果音の種類。コードからは AudioManager.Play(Sfx.xxx) で鳴らす</summary>
    public enum Sfx
    {
        PlayerShot,
        PlayerBomb,
        PlayerDown,
        PlayerRespawn,
        BossHit,
        BossPhaseUp,
        BossShot,
        BossDefeat,
        GameOver,
        GameClear,
        Pause,
        UiConfirm,
    }

    /// <summary>1つの効果音の設定。クリップが空なら鳴らない(エラーにもならない)</summary>
    [Serializable]
    public class Sound
    {
        [Tooltip("複数入れると鳴らすたびにランダムで選ぶ")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("ピッチのランダム幅。同じ音が重なっても単調になりにくい。1,1で固定")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [Tooltip("同じ音を鳴らす最小間隔(秒)。連射や多段ヒットで音が団子にならないようにする")]
        public float minInterval = 0.03f;

        public bool HasClip
        {
            get
            {
                if (clips == null) return false;
                foreach (AudioClip c in clips) if (c != null) return true;
                return false;
            }
        }

        public AudioClip PickClip()
        {
            if (!HasClip) return null;
            AudioClip c;
            do { c = clips[UnityEngine.Random.Range(0, clips.Length)]; } while (c == null);
            return c;
        }
    }

    /// <summary>
    /// 効果音の一覧。Resources/AudioLibrary.asset に置くと AudioManager が自動で読む。<br/>
    /// 作成はメニュー Tools &gt; Audio &gt; Create Audio Library。
    /// </summary>
    [CreateAssetMenu(menuName = "Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("Player")]
        public Sound playerShot = new Sound { minInterval = 0.05f };
        public Sound playerBomb = new Sound();
        public Sound playerDown = new Sound();
        public Sound playerRespawn = new Sound();

        [Header("Boss")]
        public Sound bossHit = new Sound { minInterval = 0.05f };
        [Tooltip("段階が上がったとき")]
        public Sound bossPhaseUp = new Sound();
        [Tooltip("弾幕を撃ったとき(扇形でも1回だけ鳴る)")]
        public Sound bossShot = new Sound { minInterval = 0.05f };
        public Sound bossDefeat = new Sound();

        [Header("ゲーム進行")]
        public Sound gameOver = new Sound();
        public Sound gameClear = new Sound();
        [Tooltip("ポーズを開いたときと閉じたとき")]
        public Sound pause = new Sound();
        [Tooltip("メニューやボタンの決定")]
        public Sound uiConfirm = new Sound();

        public Sound Get(Sfx id)
        {
            switch (id)
            {
                case Sfx.PlayerShot: return playerShot;
                case Sfx.PlayerBomb: return playerBomb;
                case Sfx.PlayerDown: return playerDown;
                case Sfx.PlayerRespawn: return playerRespawn;
                case Sfx.BossHit: return bossHit;
                case Sfx.BossPhaseUp: return bossPhaseUp;
                case Sfx.BossShot: return bossShot;
                case Sfx.BossDefeat: return bossDefeat;
                case Sfx.GameOver: return gameOver;
                case Sfx.GameClear: return gameClear;
                case Sfx.Pause: return pause;
                case Sfx.UiConfirm: return uiConfirm;
                default: return null;
            }
        }
    }
}
