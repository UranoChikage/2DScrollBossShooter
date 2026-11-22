using UnityEngine;
namespace GamePlay
{
    public abstract class Bullet : MonoBehaviour
    {
        //LifeTimeは仕様書に記載がなかったので、プールのために支障がなさそうな5秒に。なのでフィールドには表示しない
        protected float lifeTime = 5f;
        private BulletPool owner;
        public float Speed { get; protected set; }
        /// <summary>
        /// 仕様書にはDamageの概念が記載されていないため基本値は1か0。
        /// </summary>
        public int Damage { get; protected set; }

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
