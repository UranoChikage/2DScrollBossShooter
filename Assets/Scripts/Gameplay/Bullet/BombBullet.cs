using UnityEngine;

namespace GamePlay
{
    public class BombBullet : Bullet
    {
        [SerializeField] private Collider2D bombCollider;
        [SerializeField] private float maxScale = 50f;
        [SerializeField] private float scaleSpeed = 10f;
        [Tooltip("最大サイズに達してから消えるまでの時間")]
        [SerializeField] private float holdDuration = 0.1f;
        [SerializeField, Range(0f, 1f)] private float alphaAtMaxScale = 0.7f; // 拡大し終えた時点の不透明度

        [SerializeField] private SpriteRenderer sr;
        private float holdTimer;

        private void Awake()
        {
            if (bombCollider == null) Debug.LogError("Collider2Dがアタッチされていません。");
            if (sr == null) Debug.LogError("SpriteRendererがアタッチされていません。");
            bombCollider.isTrigger = true;
        }

        // プールから取り出されるたびに状態をリセット
        protected override void OnEnable()
        {
            transform.localScale = Vector3.zero;
            holdTimer = 0f;
            SetAlpha(1f);
        }

        private void Update() => Move();

        private void OnTriggerEnter2D(Collider2D other)
        {
            // BossBulletのColliderは子オブジェクト(Visual)にあるため、親まで辿って探す
            var bossBullet = other.GetComponentInParent<IBossBullet>();
            if (bossBullet != null)
            {
                bossBullet.TakeBombDamage();
            }
        }

        public override void Move()
        {
            if (transform.localScale.x < maxScale)
            {
                transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
                if (transform.localScale.x >= maxScale)
                    transform.localScale = Vector3.one * maxScale;

                // 拡大した時点では 1f→alphaAtMaxScaleに変化するようにする
                float growT = Mathf.InverseLerp(0f, maxScale, transform.localScale.x);
                SetAlpha(Mathf.Lerp(1f, alphaAtMaxScale, growT));
                return;
            }

            // alphaAtMaxScale に達した後は holdDuration の間、徐々に透明化していく
            holdTimer += Time.deltaTime;
            float fadeT = Mathf.Clamp01(holdTimer / holdDuration);
            SetAlpha(Mathf.Lerp(alphaAtMaxScale, 0f, fadeT));

            if (holdTimer >= holdDuration) Return();
        }

        void SetAlpha(float a)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}