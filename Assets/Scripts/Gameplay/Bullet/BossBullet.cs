using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// Bossが撃つ弾<br/>
    /// Launch()で直進弾、LaunchHoming()でホーミング弾として発射する。
    /// </summary>
    public class BossBullet : Bullet, IBossBullet
    {
        [SerializeField] private Collider2D bulletCollider;
        [SerializeField] private float speed = 5f;
        [SerializeField] private int damage = 1;
        [Tooltip("画面外に出てしまった場合でも消えるまでの時間(maxHomingDurationより長くすること！)")]
        [SerializeField] private float lifeSeconds = 20f;

        [Header("ホーミング")]
        [Tooltip("1秒あたりの旋回角度")]
        [SerializeField] private float turnSpeed = 90f;
        [SerializeField] private float maxHomingAngle = 30f;
        [SerializeField] private float maxHomingDuration = 15f;

        private Vector2 direction;
        private Transform target;
        private bool isHoming;
        private float startAngle;
        private float homingTimer;
        private float currentLifeTime;

        private void Awake()
        {
            if (bulletCollider == null)
            {
                Debug.LogError("Collider2Dがアタッチされていません。");
                return;
            }
            bulletCollider.isTrigger = true;
        }

        // プールから取り出されるたびに状態をリセット
        protected override void OnEnable()
        {
            currentLifeTime = lifeSeconds;
            homingTimer = 0f;
            isHoming = false;
            target = null;
            Speed = speed;
            Damage = damage;
        }

        private void Update() => Move();

        /// <summary>
        /// 指定方向に直進する弾として発射する
        /// </summary>
        public void Launch(Vector2 dir)
        {
            direction = dir.normalized;
            isHoming = false;
        }

        /// <summary>
        /// targetに向かって誘導する弾として発射する<br/>
        /// 発射時の向きから±maxHomingAngle以上曲がる、またはmaxHomingDurationが経つと直進に切り替わる。
        /// </summary>
        public void LaunchHoming(Vector2 dir, Transform homingTarget)
        {
            direction = dir.normalized;
            target = homingTarget;
            startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            homingTimer = 0f;
            isHoming = target != null;
        }

        public override void Move()
        {
            if (isHoming) Homing();

            transform.position += (Vector3)(direction * Speed * Time.deltaTime);

            currentLifeTime -= Time.deltaTime;
            if (currentLifeTime <= 0f) Return();
        }

        private void Homing()
        {
            homingTimer += Time.deltaTime;
            if (target == null || homingTimer >= maxHomingDuration)
            {
                isHoming = false;
                return;
            }

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);

            // 発射時の角度から±maxHomingAngle以上曲がったら誘導を切って直進する
            if (Mathf.Abs(Mathf.DeltaAngle(startAngle, nextAngle)) >= maxHomingAngle)
            {
                isHoming = false;
                return;
            }

            float rad = nextAngle * Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Bossもこの弾と重なるが、IDamegeableで判定するとBossにダメージが入ってしまうのでPlayerのみ対象にする
            if (other.TryGetComponent<Player>(out var player))
            {
                player.TakeDamage(Damage);
                Return();
            }
        }

        /// <summary>
        /// Bombで消される
        /// </summary>
        public void TakeBombDamage() => Return();
    }
}
