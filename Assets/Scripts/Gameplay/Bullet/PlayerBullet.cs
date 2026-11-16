using UnityEngine;
using Core;

namespace GamePlay
{
    public class PlayerBullet : Bullet
    {
        Collider2D collider2D;

        void Start()
        {
            collider2D = GetComponent<Collider2D>();
            if (collider2D == null)
            {
                Debug.LogError("Collider2Dがアタッチされていません。");
            }
        }

        // Update is called once per frame
        void Update()
        {
            Move();
        }
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
