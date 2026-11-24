using Core;
using System;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// ゲーム全体の進行を管理する<br/>
    /// 状態(Playing / Paused / Result)、クリアタイムの計測、ベストタイムの保存を担当する。<br/>
    /// タイマーや保存もこのクラスにまとめている。<br/>
    /// PlayerとBossの生成も担当する。UIなどがAwakeでPlayer/Bossを参照できるよう、他より先にAwakeを実行する。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        [Header("生成するPrefab")]
        [SerializeField] private Player playerPrefab;
        [SerializeField] private Boss bossPrefab;
        [Tooltip("Playerの弾")]
        [SerializeField] private Bullet playerBulletPrefab;
        [SerializeField] private Bullet bombPrefab;
        [SerializeField] private Bullet bossBulletPrefab;
        [Header("生成する位置")]
        [SerializeField] private Vector3 playerSpawnPosition = new Vector3(-7f, 0f, 0f);
        [SerializeField] private Vector3 bossSpawnPosition = new Vector3(7f, 0f, 0f);
        [Header("シーン遷移を行うSceneChanger")]
        [SerializeField] private SceneChanger sceneChanger;
        /// <summary>生成されたPlayer。UIなどが参照する(Awake以降に有効)</summary>
        public Player Player { get; private set; }
        /// <summary>生成されたBoss。UIなどが参照する(Awake以降に有効)</summary>
        public Boss Boss { get; private set; }
        public event Action<GameState> StateChanged;
        public event Action<bool> OnPauseMenuShowRequested;//PauseUIに通知するためのイベント。boolはtrueで表示、falseで非表示
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

        private bool isPlayerIntroEnd;//Playerの登場演出が終わったか
        private bool isBossIntroEnd;//Bossの登場演出が終わったか

        private void Awake()
        {
            if (playerPrefab == null || bossPrefab == null || sceneChanger == null
                || playerBulletPrefab == null || bombPrefab == null || bossBulletPrefab == null)
            {
                Debug.LogError("Prefab・SceneChangerのどれかがアタッチされていません。");
                enabled = false;
                return;
            }
            SpawnCharacters();
            Player.GameOverEvent += OnGameOver;
            Boss.GameClearEvent += OnGameClear;
            Player.IntroEndEvent += OnPlayerIntroEnd;
            Boss.IntroEndEvent += OnBossIntroEnd;
            Player.OnPausePressed += TogglePause;
            Time.timeScale = 1f;//タイトルへ戻る/リトライ後にtimeScaleが0のまま残らないように
        }

        /// <summary>
        /// PlayerとBossを生成して、弾のプールなどの依存を渡す<br/>
        /// Boss自身のAwakeで初期位置を使うため、位置を指定して生成する。
        /// </summary>
        private void SpawnCharacters()
        {
            BulletPool playerBulletPool = CreatePool("PlayerBulletPool", playerBulletPrefab);
            BulletPool bombPool = CreatePool("BombPool", bombPrefab);
            BulletPool bossBulletPool = CreatePool("BossBulletPool", bossBulletPrefab);

            Player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            Player.Init(this, playerBulletPool, bombPool);
            Boss = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
            Boss.Init(this, bossBulletPool, Player);
        }

        /// <summary>このオブジェクトの子に、弾のプールを作る</summary>
        private BulletPool CreatePool(string poolName, Bullet bulletPrefab)
        {
            BulletPool pool = new GameObject(poolName).AddComponent<BulletPool>();
            pool.transform.SetParent(transform);
            pool.Init(bulletPrefab);
            return pool;
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
            if (State != GameState.Playing || sceneChanger.IsLoading) return;
            Time.timeScale = 0f;
            AudioManager.Play(Sfx.Pause);
            ChangeState(GameState.Paused);
            OnPauseMenuShowRequested?.Invoke(true);
        }

        /// <summary>
        /// Paused中のみ再開する<br/>
        /// タイトルやリトライへのフェード中は、ポーズを解除できない。
        /// </summary>
        public void Resume()
        {
            if (State != GameState.Paused || sceneChanger.IsLoading) return;
            Time.timeScale = 1f;
            AudioManager.Play(Sfx.Pause);
            ChangeState(GameState.Playing);
            OnPauseMenuShowRequested?.Invoke(false);
        }

        /// <summary>
        /// 保存されているベストタイムを返す<br/>
        /// 記録がない場合はnullを返す。
        /// </summary>
        /// 「Null許容値型(Nullable)」の構文
        ///  通常のfloatは数値(初期値は 0f)しか持てないが"?"をつけることで「無効である」といった状態を表せるらしい。(Google調べ)
        public static float? GetBestTime() => ClearRecords.GetBestTime();//staticにして、GameManagerが居ないタイトルシーンからも呼べるようにする

        /// <summary>
        /// リザルトシーンへ移る<br/>
        /// 撃墜Animation・Text表示などの演出が終わったタイミングで呼ぶ。
        /// </summary>
        public void GoToResult()
        {
            Time.timeScale = 1f;
            sceneChanger.LoadScene(resultSceneName);
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
            ClearRecords.Add(ElapsedTime);//ベスト5に入るなら保存される
            AudioManager.Play(Sfx.GameClear);
            EndGame(true);
        }

        private void OnGameOver()
        {
            AudioManager.Play(Sfx.GameOver);
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

        private void ChangeState(GameState next)
        {
            State = next;
            Debug.Log($"GameStateが{State}に変わりました。");
            StateChanged?.Invoke(State);
        }

        private void OnDestroy()
        {
            if (Player != null)
            {
                Player.OnPausePressed -= TogglePause;
                Player.GameOverEvent -= OnGameOver;
                Player.IntroEndEvent -= OnPlayerIntroEnd;
            }
            if (Boss != null)
            {
                Boss.GameClearEvent -= OnGameClear;
                Boss.IntroEndEvent -= OnBossIntroEnd;
            }
        }
    }
}
