using Core;
using GamePlay;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// インゲームのUI全体を担当する<br/>
    /// Bossの体力ゲージ、Playerの残機とボム、タイム、ベストタイム、操作ヒントを表示する。<br/>
    /// 体力・残機・ボムは変化したときのイベントで更新し、毎フレーム変わるタイムだけポーリングする。
    /// </summary>
    public class InGameUI : MonoBehaviour
    {
        [Header("状態を読み取る対象")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Player player;
        [SerializeField] private Boss boss;

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

        [Header("操作のヒント(使っているデバイスで表記を切り替える)")]
        [SerializeField] private Text pauseHint;
        [SerializeField] private Text fireHint;
        [SerializeField] private Text bombHint;

        private void Awake()
        {
            if (gameManager == null || player == null || boss == null)
            {
                Debug.LogError("GameManager・Player・Bossのどれかがアタッチされていません。");
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
            if (pauseHint == null || fireHint == null || bombHint == null)
            {
                Debug.LogError("操作ヒントのTextがアタッチされていません。");
                enabled = false;
                return;
            }
            boss.OnHealthChanged += UpdateBossGauge;
            player.OnHealthChanged += lifeCounter.SetCount;
            player.OnBombCountChanged += bombCounter.SetCount;
            player.OnDeviceChanged += UpdateHints;
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
            timeText.text = FormatTime(gameManager.ElapsedTime);
            SetBestTimeText();
            UpdateHints(player.LastDevice);
        }

        private void Update()
        {
            //ポーズ中・演出中・リザルト中はタイムが進まないので、書き換えない
            if (gameManager.State != GameState.Playing) return;
            timeText.text = FormatTime(gameManager.ElapsedTime);
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
            bestTimeText.text = best == null ? "BEST --:--.--" : $"BEST {FormatTime(best.Value)}";
        }

        /// <summary>
        /// 最後に使われたデバイスに合わせて、ヒントの表記を切り替える<br/>
        /// ゲームパッドはXboxの表記にしている。
        /// </summary>
        private void UpdateHints(Device device)
        {
            switch (device)
            {
                case Device.Keyboard:
                    pauseHint.text = "ポーズ(Esc)";
                    fireHint.text = "ショット(Space)";
                    bombHint.text = "ボム(X)";
                    break;
                case Device.Gamepad:
                    pauseHint.text = "ポーズ(Start)";
                    fireHint.text = "ショット(B)";
                    bombHint.text = "ボム(A)";
                    break;
            }
        }

        /// <summary>秒数を「分:秒.小数2桁」の形にする(例 : 01:05.23)</summary>
        private string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60f);
            return $"{minutes:00}:{seconds % 60f:00.00}";
        }

        private void OnDestroy()
        {
            //購読解除
            if (boss != null) boss.OnHealthChanged -= UpdateBossGauge;
            if (player == null) return;
            if (lifeCounter != null) player.OnHealthChanged -= lifeCounter.SetCount;
            if (bombCounter != null) player.OnBombCountChanged -= bombCounter.SetCount;
            player.OnDeviceChanged -= UpdateHints;
        }
    }
}
