using Core;
using System;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// ボス<br/>
    /// 小規模のゲームなので、Player同様、移動・弾の発射・段階管理をこのクラスにまとめている。
    /// </summary>
    public class Boss : MonoBehaviour, IDamegeable
    {
        [SerializeField] private Collider2D bossCollider;
        [Tooltip("Bossの最大体力")]
        [SerializeField] private int maxHealth = 100;
        [Tooltip("Playerに接触したときに与えるダメージ")]
        [SerializeField] private int contactDamage = 1;

        [Header("上下移動")]
        [Tooltip("上下移動の速さ")]
        [SerializeField] private float moveSpeed = 1.5f;
        [Tooltip("初期位置から上下に動く幅")]
        [SerializeField] private float moveRange = 4f;

        [Header("弾")]
        [SerializeField] private BulletPool bulletPool;
        [Tooltip("狙う対象(Player)")]
        [SerializeField] private Transform target;

        [Header("発射インターバル(秒)")]
        [SerializeField] private float linearInterval = 5f;
        [SerializeField] private float fanInterval = 7f;
        [SerializeField] private float homingInterval = 10f;

        [Header("扇形")]
        [Tooltip("奇数個のときの弾数")]
        [SerializeField] private int fanOddCount = 5;
        [Tooltip("偶数個のときの弾数")]
        [SerializeField] private int fanEvenCount = 4;
        [Tooltip("扇の全体の角度(度)")]
        [SerializeField] private float fanAngle = 60f;

        public event Action GameClearEvent;
        public BossState State { get; private set; } = BossState.LinearPhase;

        private int currentHealth;
        private float startY;
        private float linearTimer;
        private float fanTimer;
        private float homingTimer;
        private bool isFanOdd = true;//扇形は奇数個と偶数個を交互に撃つ
        private bool isPlaying = true;

        public void TakeDamage(int damage)
        {
            if (!isPlaying) return;
            currentHealth -= damage;
            UpdateState();
            if (currentHealth <= 0) GameClearEvent?.Invoke();
        }

        private void Awake()
        {
            if (bulletPool == null) Debug.LogError("BulletPoolがアタッチされていません。");
            if (target == null) Debug.LogError("狙う対象(Player)がアタッチされていません。");
            if (bossCollider == null)
            {
                Debug.LogError("Collider2Dがアタッチされていません。");
            }
            else
            {
                bossCollider.isTrigger = true;
            }

            currentHealth = maxHealth;
            startY = transform.position.y;
            linearTimer = linearInterval;//最初の1発はすぐ撃つ
            GameClearEvent += StopGame;
        }

        private void Update()
        {
            if (!isPlaying) return;
            MoveVertical();
            FireUpdate();
        }

        private void MoveVertical()
        {
            Vector3 pos = transform.position;
            pos.y = startY + Mathf.Sin(Time.time * moveSpeed) * moveRange;
            transform.position = pos;
        }

        /// <summary>
        /// 段階に応じて、有効なパターンのインターバルを数えて撃つ
        /// </summary>
        private void FireUpdate()
        {
            linearTimer += Time.deltaTime;
            if (linearTimer >= linearInterval)
            {
                linearTimer = 0f;
                FireLinear();
            }

            if (State >= BossState.FanShapePhase)
            {
                fanTimer += Time.deltaTime;
                if (fanTimer >= fanInterval)
                {
                    fanTimer = 0f;
                    FireFan();
                }
            }

            if (State >= BossState.HomingPhase)
            {
                homingTimer += Time.deltaTime;
                if (homingTimer >= homingInterval)
                {
                    homingTimer = 0f;
                    FireHoming();
                }
            }
        }

        //初期状態 : Playerに向かって一直線
        private void FireLinear()
        {
            BossBullet bullet = SpawnBullet();
            bullet?.Launch(DirectionToTarget());
        }

        //1段階目 : 前方(左)に奇数個・偶数個を交互に扇形で撃つ
        private void FireFan()
        {
            int count = isFanOdd ? fanOddCount : fanEvenCount;
            isFanOdd = !isFanOdd;

            for (int i = 0; i < count; i++)
            {
                // 1発のときは正面のみ。2発以上は扇の端から端まで等間隔に並べる
                float angle = count == 1 ? 0f : -fanAngle / 2f + fanAngle / (count - 1) * i;
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.left;
                BossBullet bullet = SpawnBullet();
                bullet?.Launch(dir);
            }
        }

        //2段階目 : Playerに向かうホーミング弾
        private void FireHoming()
        {
            BossBullet bullet = SpawnBullet();
            bullet?.LaunchHoming(DirectionToTarget(), target);
        }

        private BossBullet SpawnBullet()
        {
            return bulletPool?.Get(transform.position) as BossBullet;
        }

        private Vector2 DirectionToTarget()
        {
            if (target == null) return Vector2.left;
            return (target.position - transform.position).normalized;
        }

        /// <summary>
        /// 残りライフの割合から段階を決める(1/3ずつ減るごとに段階が上がる)
        /// </summary>
        private void UpdateState()
        {
            float ratio = (float)currentHealth / maxHealth;
            BossState next = State;
            if (ratio <= 1f / 3f) next = BossState.HomingPhase;
            else if (ratio <= 2f / 3f) next = BossState.FanShapePhase;

            if (next == State) return;
            State = next;
            Debug.Log($"Bossが{State}に移行しました。");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isPlaying) return;
            if (other.TryGetComponent<Player>(out var player))
            {
                player.TakeDamage(contactDamage);
            }
        }

        private void StopGame()
        {
            Debug.Log("Game Clear");
            isPlaying = false;
            //TODO : 撃破時のAnimation、画面上の弾を消す処理、クリア画面への遷移を追加する
        }

        private void OnDestroy()
        {
            GameClearEvent -= StopGame;
        }
    }
}
