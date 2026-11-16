using UnityEngine;
namespace GamePlay
{
    public abstract class Bullet : MonoBehaviour
    {
        BulletPool owner;
        public float Speed { get; set; }
        public float Damage { get; set; }

        public virtual void Initialize(BulletPool bulletPool)
        {
            owner = bulletPool;
        }
        protected virtual void OnEnable()
        {
            //Enable時に初期化する処理を追加する場合はここに記述する
        }
        public abstract void Move();
        public void Return()
        {
            owner.Release(this);
        }

    }
}
