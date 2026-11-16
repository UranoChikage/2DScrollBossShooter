using UnityEngine;

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
        pool = new ObjectPoolManager<Bullet>(() =>Create(), prefab, transform);
    }
    Bullet Create()
    {
        Bullet bullet = Instantiate(prefab, transform);
        bullet.Initialize(this);
        return bullet;
    }

    public Bullet Get()
    {
        return pool.Get();
    }

    public void Release(Bullet bullet)
    {
        pool.Release(bullet);
    }
}

