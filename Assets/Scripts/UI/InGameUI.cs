using Core;
using GamePlay;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// インゲームのUI全体を担当する<br/>
    /// Bossの体力ゲージ、Playerの残機とボム、タイム、ベストタイムを表示する。操作ヒントはDeviceHintTextが切り替える。<br/>
    /// 体力・残機・ボムは変化したときのイベントで更新し、毎フレーム変わるタイムだけポーリングする。
    /// </summary>
    public class InGameUI : MonoBehaviour
    {
        [Header("状態を読み取る対象(PlayerとBossはGameManagerが生成したものを使う)")]
        [SerializeField] private GameManager gameManager;

        [Header("Bossの体力ゲージ(段階ごとに1本。ImageTypeはFilled)")]
        [Tooltip("第一段階(100%〜第二段階の割合)のゲージ")]
        [SerializeField] private Image linearGauge;
        [Tooltip("第二段階(第二段階〜第三段階の割合)のゲージ")]
        [SerializeField] private Image fanGauge;
        [Tooltip("第三段階(第三段階の割合〜0%)のゲージ")]
        [SerializeField] private Image homingGauge;

        [Header("Playerの残機とボム")]
        [SerializeField] private IconCounter lifeCounter;
        [SerializeField] private IconCounter bombCounter;

        [Header("タイム")]
        [SerializeField] private Text timeText;
        [SerializeField] private Text bestTimeText;

        private Player player;
        private Boss boss;

        private void Awake()
        {
            if (gameManager == null)
            {
                Debug.LogError("GameManagerがアタッチされていません。");
                enabled = false;
                return;
            }
            if (linearGauge == null || fanGauge == null || homingGauge == null)
            {
                Debug.LogError("Bossの体力ゲージがアタッチされていません。");
                enabled = false;
                return;
            }
            if (lifeCounter == null || bombCounter == null)
            {
                Debug.LogError("残機またはボムのIconCounterがアタッチされていません。");
                enabled = false;
                return;
            }
            if (timeText == null || bestTimeText == null)
            {
                Debug.LogError("タイムのTextがアタッチされていません。");
                enabled = false;
                return;
            }
            //GameManagerは先にAwakeを実行して、PlayerとBossを生成済み
            player = gameManager.Player;
            boss = gameManager.Boss;
            if (player == null || boss == null)
            {
                Debug.LogError("PlayerまたはBossが生成されていません。");
                enabled = false;
                return;
            }
            boss.OnHealthChanged += UpdateBossGauge;
            player.OnHealthChanged += lifeCounter.SetCount;
            player.OnBombCountChanged += bombCounter.SetCount;
        }

        /// <summary>
        /// 初期表示を作る<br/>
        /// PlayerとBossの値はAwakeで決まるので、全てのAwakeが終わったStartで読む。
        /// </summary>
        private void Start()
        {
            UpdateBossGauge(boss.HealthRatio);
            lifeCounter.Init(player.MaxHealth);
            lifeCounter.SetCount(player.CurrentHealth);
            bombCounter.Init(player.MaxBombCount);
            bombCounter.SetCount(player.BombCount);
            timeText.text = TimeFormatter.Format(gameManager.ElapsedTime);
            SetBestTimeText();
        }

        private void Update()
        {
            //ポーズ中・演出中・リザルト中はタイムが進まないので、書き換えない
            if (gameManager.State != GameState.Playing) return;
            timeText.text = TimeFormatter.Format(gameManager.ElapsedTime);
        }

        /// <summary>
        /// 残り体力の割合から、3本のゲージの中身を決める<br/>
        /// Bossの段階の区切りと同じ割合で分けているので、1本ずつ順に減っていく。
        /// </summary>
        private void UpdateBossGauge(float ratio)
        {
            float second = boss.SecondPhaseRatio;
            float third = boss.ThirdPhaseRatio;
            linearGauge.fillAmount = Mathf.InverseLerp(second, 1f, ratio);
            fanGauge.fillAmount = Mathf.InverseLerp(third, second, ratio);
            homingGauge.fillAmount = Mathf.InverseLerp(0f, third, ratio);
        }

        private void SetBestTimeText()
        {
            float? best = GameManager.GetBestTime();//記録がない場合はnull
            bestTimeText.text = best == null ? $"BEST {TimeFormatter.Empty}" : $"BEST {TimeFormatter.Format(best.Value)}";
        }

        private void OnDestroy()
        {
            //購読解除
            if (boss != null) boss.OnHealthChanged -= UpdateBossGauge;
            if (player == null) return;
            if (lifeCounter != null) player.OnHealthChanged -= lifeCounter.SetCount;
            if (bombCounter != null) player.OnBombCountChanged -= bombCounter.SetCount;
        }
    }
}
