using UnityEngine;
using Core;

namespace GamePlay
{
    /// <summary>
    /// Player用
    /// ObjectPoolManagerを使用して球をPoolする。
    /// </summary>
    public class BulletPool : MonoBehaviour
    {
        [SerializeField] private Bullet prefab;
        private ObjectPoolManager<Bullet> pool;

        private void Awake()
        {
            pool = new ObjectPoolManager<Bullet>(() => Create());
        }
        private Bullet Create()
        {
            Bullet bullet = Instantiate(prefab, transform);
            bullet.Initialize(this);
            return bullet;
        }

        public Bullet Get(Vector3 position)
        {
            Bullet bullet = pool.Get();
            bullet.transform.position = position;
            return bullet;
        }

        public void Release(Bullet bullet)
        {
            pool.Release(bullet);
        }
    }
}
