using Core;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay
{
    /// <summary>
    /// ゲーム全体の進行を管理する<br/>
    /// 状態(Playing / Paused / Result)、クリアタイムの計測、ベストタイムの保存を担当する。<br/>
    /// タイマーや保存もこのクラスにまとめている。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("状態の変化を受け取る対象")]
        [SerializeField] private Player player;
        [SerializeField] private Boss boss;
        public event Action<GameState> StateChanged;
        public event Action<Device, bool> OnPauseMenuShowRequested;//PauseUIに通知するためのイベント。boolはtrueで表示、falseで非表示
        public GameState State { get; private set; } = GameState.Intro;//最初は登場演出から始まる
        /// <summary>経過秒数。Playing中だけ進む。</summary>
        public float ElapsedTime { get; private set; }
        /// <summary>
        /// 直前のゲームの結果<br/>
        /// リザルトシーンではGameManagerが存在しないので、staticで残して渡す。
        /// </summary>
        public static float LastElapsedTime { get; private set; }
        public static bool LastIsCleared { get; private set; }

        [Header("シーン名(Build Settingsに登録しておく)")]
        [SerializeField] private string resultSceneName = "Result";

        private const string BestTimeKey = "BestClearTime";
        private bool isPlayerIntroEnd;//Playerの登場演出が終わったか
        private bool isBossIntroEnd;//Bossの登場演出が終わったか

        private void Awake()
        {
            if (player == null || boss == null)
            {
                Debug.LogError("PlayerまたはBossがアタッチされていません。");
                enabled = false;
                return;
            }
            player.GameOverEvent += OnGameOver;
            boss.GameClearEvent += OnGameClear;
            player.IntroEndEvent += OnPlayerIntroEnd;
            boss.IntroEndEvent += OnBossIntroEnd;
            player.OnPausePressed += TogglePause;
            Time.timeScale = 1f;//タイトルへ戻る/リトライ後にtimeScaleが0のまま残らないように
        }

        private void Update()
        {
            if (State != GameState.Playing) return;
            ElapsedTime += Time.deltaTime;
        }

        /// <summary>
        /// Pauseアクションが押されたときに、ポーズと再開を切り替える<br/>
        /// InputSystemのコールバックはTime.timeScaleが0でも呼ばれるので、ポーズ中でも再開の入力を受け取れる。
        /// </summary>
        private void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        /// <summary>Playing中のみ一時停止する</summary>
        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            ChangeState(GameState.Paused);
            OnPauseMenuShowRequested?.Invoke(player.LastDevice, true);
        }

        /// <summary>Paused中のみ再開する</summary>
        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
            OnPauseMenuShowRequested?.Invoke(player.LastDevice, false);
        }

        /// <summary>
        /// 保存されているベストタイムを返す<br/>
        /// 記録がない場合はnullを返す。
        /// </summary>
        /// 「Null許容値型(Nullable)」の構文
        ///  通常のfloatは数値(初期値は 0f)しか持てないが"?"をつけることで「無効である」といった状態を表せるらしい。(Google調べ)
        public static float? GetBestTime()//staticにして、GameManagerが居ないタイトルシーンからも呼べるようにする
        {
            if (!PlayerPrefs.HasKey(BestTimeKey)) return null;
            return PlayerPrefs.GetFloat(BestTimeKey);
        }

        /// <summary>
        /// リザルトシーンへ移る<br/>
        /// 撃墜Animation・Text表示などの演出が終わったタイミングで呼ぶ。
        /// </summary>
        public void GoToResult()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(resultSceneName);
        }

        private void OnPlayerIntroEnd()
        {
            isPlayerIntroEnd = true;
            TryStartPlaying();
        }

        private void OnBossIntroEnd()
        {
            isBossIntroEnd = true;
            TryStartPlaying();
        }

        /// <summary>
        /// PlayerとBossの登場演出が両方終わったら、Playingに移る
        /// </summary>
        private void TryStartPlaying()
        {
            if (State != GameState.Intro) return;
            if (!isPlayerIntroEnd || !isBossIntroEnd) return;
            ChangeState(GameState.Playing);
        }

        private void OnGameClear()
        {
            SaveBestTime();
            EndGame(true);
        }

        private void OnGameOver()
        {
            EndGame(false);
        }

        /// <summary>
        /// 結果をstaticに残して、Result状態に移る。<br/>
        /// シーン遷移は演出後にGoToResult()で行う。
        /// </summary>
        private void EndGame(bool isCleared)
        {
            LastElapsedTime = ElapsedTime;
            LastIsCleared = isCleared;
            ChangeState(GameState.Result);
        }

        /// <summary>
        /// 今回のタイムがベストより早ければ保存する。
        /// </summary>
        private void SaveBestTime()
        {
            float? best = GetBestTime();//初めての場合もあるのでnull許容型で受け取る
            if (best != null && best <= ElapsedTime) return;
            PlayerPrefs.SetFloat(BestTimeKey, ElapsedTime);
            PlayerPrefs.Save();
        }

        private void ChangeState(GameState next)
        {
            State = next;
            Debug.Log($"GameStateが{State}に変わりました。");
            StateChanged?.Invoke(State);
        }

        private void OnDestroy()
        {
            if (player != null) player.OnPausePressed -= TogglePause;
            if (player != null) player.GameOverEvent -= OnGameOver;
            if (boss != null) boss.GameClearEvent -= OnGameClear;
            if (player != null) player.IntroEndEvent -= OnPlayerIntroEnd;
            if (boss != null) boss.IntroEndEvent -= OnBossIntroEnd;
        }
    }
}
