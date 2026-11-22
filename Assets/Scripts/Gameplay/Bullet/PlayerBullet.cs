using UnityEngine;
using Core;

namespace GamePlay
{
    public class PlayerBullet : Bullet
    {

        [SerializeField] private Collider2D collider2D;

        [SerializeField] private float speed = 10f;

        [SerializeField] private int damage = 1;

        private float currentLifeTime;
        private void Start()
        {
            if (collider2D == null) Debug.LogError("Collider2Dがアタッチされていません。");
            collider2D.isTrigger = true;
        }
        private void Update() => Move();
        protected override void OnEnable()
        {
            currentLifeTime = lifeTime;
            Speed = speed;
            Damage = damage;
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject != gameObject)
            {
                if (collision.gameObject.GetComponent<Player>() != null) return;
                if (collision.gameObject.TryGetComponent<IDamegeable>(out var damegable))
                {
                    damegable.TakeDamage(Damage);
                    Return();
                }
            }
        }
        public override void Move()
        {
            transform.Translate(Vector3.right * Speed * Time.deltaTime);
            currentLifeTime -= Time.deltaTime;
            if (currentLifeTime <= 0)
            {
                currentLifeTime = lifeTime;
                Return();
            }
        }
    }
}
