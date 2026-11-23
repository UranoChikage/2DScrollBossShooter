using UnityEngine;

namespace GamePlay
{
    public class InfiniteScrollTiled : MonoBehaviour
    {
        [SerializeField] private float speed = 2f;              // 正で左へ流れる
        [SerializeField] private bool useCustomLoopWidth = false; // ループ距離を手動指定するか
        [SerializeField] private float customLoopWidth = 10f;     // 手動指定時のループ距離(正の値)

        private float loopWidth;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;

            if (useCustomLoopWidth)
            {
                loopWidth = Mathf.Abs(customLoopWidth);
            }
            else
            {
                var sr = GetComponent<SpriteRenderer>();
                // スプライト1枚分のワールド幅(スケール込み)
                loopWidth = sr.sprite.bounds.size.x * transform.lossyScale.x;
            }
        }

        private void Update()
        {
            // 移動量をループ距離で割った余りにすることで、ループさせる
            float offset = Mathf.Repeat(Time.time * speed, loopWidth);
            transform.position = startPos + Vector3.left * offset;
        }
    }
}
