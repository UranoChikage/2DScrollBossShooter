using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GameUI
{
    /// <summary>
    /// 残機やボムのように「あと何個か」をアイコンの数で表示する<br/>
    /// 最大数ぶんのアイコンを最初に生成して、残り数を超えた分を右から非表示にする。<br/>
    /// 並べるのはContainerに付けたHorizontalLayoutGroupに任せる。
    /// </summary>
    public class IconCounter : MonoBehaviour
    {
        [Tooltip("1個ぶんのアイコンのPrefab")]
        [SerializeField] private Image iconPrefab;
        [Tooltip("アイコンを並べる親(HorizontalLayoutGroupを付ける)")]
        [SerializeField] private Transform container;

        private readonly List<Image> icons = new List<Image>();

        /// <summary>最大数ぶんのアイコンを生成する。最初に一度だけ呼ぶ。</summary>
        public void Init(int max)
        {
            if (iconPrefab == null || container == null)
            {
                Debug.LogError("アイコンのPrefabまたは親がアタッチされていません。");
                return;
            }
            for (int i = 0; i < max; i++)
            {
                icons.Add(Instantiate(iconPrefab, container));
            }
        }

        /// <summary>残り数ぶんだけアイコンを表示する</summary>
        public void SetCount(int count)
        {
            for (int i = 0; i < icons.Count; i++)
            {
                icons[i].gameObject.SetActive(i < count);
            }
        }
    }
}
