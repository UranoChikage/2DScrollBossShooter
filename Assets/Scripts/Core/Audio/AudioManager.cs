using UnityEngine;

namespace Core
{
    /// <summary>
    /// 効果音の再生係<br/>
    /// 最初に鳴らした時に自動で生成され(シーンに置かなくてよい)、シーンを跨いで残る。<br/>
    /// 音の登録は Resources/AudioLibrary.asset(AudioLibrary)で行う。未登録の音は黙って無視する。<br/>
    /// 使い方 : AudioManager.Play(Sfx.PlayerShot)<br/>
    /// BGMは各シーンのAudioSourceで流すので、ここでは扱わない。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int VoiceCount = 16;//同時に鳴らせる数
        private const string LibraryResource = "AudioLibrary";

        private class Voice
        {
            public AudioSource source;
            public float startTime;
        }

        private static AudioManager instance;

        private AudioLibrary library;
        private Voice[] voices;
        private float[] lastPlayTime;//Sfxごとの、最後に鳴らした時間

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => instance = null;//Domain Reloadを切っていても前回のinstanceが残らないように

        private static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            library = Resources.Load<AudioLibrary>(LibraryResource);
            if (library == null)
                Debug.LogWarning("Resources/AudioLibrary.asset がありません。メニュー Tools > Audio > Create Audio Library で作成してください。音は鳴りません。", this);

            voices = new Voice[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                voices[i] = new Voice { source = src };
            }
            lastPlayTime = new float[System.Enum.GetValues(typeof(Sfx)).Length];
            for (int i = 0; i < lastPlayTime.Length; i++) lastPlayTime[i] = float.NegativeInfinity;
        }

        /// <summary>効果音を鳴らす。未登録なら何もしない</summary>
        public static void Play(Sfx id) => Instance.PlayInternal(id);

        private void PlayInternal(Sfx id)
        {
            if (library == null) return;
            Sound sound = library.Get(id);
            if (sound == null) return;
            AudioClip clip = sound.PickClip();
            if (clip == null) return;

            //ポーズ中(Time.timeScaleが0)でも間隔を測れるように、unscaledTimeを使う
            if (Time.unscaledTime - lastPlayTime[(int)id] < sound.minInterval) return;
            lastPlayTime[(int)id] = Time.unscaledTime;

            Voice v = AcquireVoice();
            AudioSource src = v.source;
            src.Stop();
            src.clip = clip;
            src.volume = sound.volume;
            src.pitch = Random.Range(sound.pitchRange.x, sound.pitchRange.y);
            v.startTime = Time.unscaledTime;
            src.Play();
        }

        /// <summary>空きを探す。無ければ一番古い音を奪う</summary>
        private Voice AcquireVoice()
        {
            Voice oldest = voices[0];
            foreach (Voice v in voices)
            {
                if (!v.source.isPlaying) return v;
                if (v.startTime < oldest.startTime) oldest = v;
            }
            return oldest;
        }
    }
}
