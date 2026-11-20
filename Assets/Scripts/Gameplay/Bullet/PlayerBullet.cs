using UnityEngine;
using Core;

namespace GamePlay
{
    public class PlayerBullet : Bullet
    {

        [SerializeField] Collider2D collider2D;
        [SerializeField] float speed = 10f;
        [SerializeField] float damage = 1f;
        void Start()
        {
            if (collider2D == null) Debug.LogError("Collider2Dがアタッチされていません。");
            Speed = speed;
            Damage = damage;
        }
        void Update() => Move();
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject != gameObject)
            {
                if (collision.gameObject.GetComponent<Player>() != null) return;
                collision.gameObject.GetComponent<IDamageable>()?.TakeDamage(Damage);
                Return();
            }
        }
        public override void Move()
        {
            transform.Translate(Vector3.right * Speed * Time.deltaTime);
        }
    }
}
