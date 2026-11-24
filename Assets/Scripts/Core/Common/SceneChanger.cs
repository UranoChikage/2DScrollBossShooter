using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Core
{
    /// <summary>
    /// シーン遷移を、画面のフェード付きで行う<br/>
    /// シーンが始まった直後にCanvasGroupのalphaを1→0にし、遷移のときは0→1にしてからLoadする。<br/>
    /// 各シーンのCanvasに1つずつ置く。DontDestroyOnLoadにはしない。<br/>
    /// ポーズ中(Time.timeScaleが0)からの遷移でも動くように、フェードはunscaledの時間で進める。
    /// </summary>
    public class SceneChanger : MonoBehaviour
    {
        [Header("フェードに使う、全画面の黒いImageに付けたCanvasGroup")]
        [SerializeField] private CanvasGroup fadeGroup;
        [Tooltip("シーン開始後、画面が見えるまでの時間(秒)")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [Tooltip("遷移を始めてから、画面が隠れきるまでの時間(秒)")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        private bool isLoading;//遷移の二重呼び出しを防ぐ

        /// <summary>遷移中か。ポーズの解除など、遷移中に受け付けたくない操作の判断に使う。</summary>
        public bool IsLoading => isLoading;

        private void Awake()
        {
            if (fadeGroup == null)
            {
                Debug.LogError("フェード用のCanvasGroupがアタッチされていません。");
                enabled = false;
                return;
            }
            fadeGroup.alpha = 1f;//最初は隠しておき、Startでフェードインする
            fadeGroup.blocksRaycasts = true;
        }

        private void Start()
        {
            if (fadeGroup != null) StartCoroutine(FadeInRoutine());
        }

        /// <summary>
        /// 画面をフェードアウトしてから、指定したシーンへ移る<br/>
        /// 遷移中に呼ばれた場合は無視する。CanvasGroupがない場合は、フェードなしで移る。
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isLoading) return;
            isLoading = true;
            if (fadeGroup == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator FadeInRoutine()
        {
            yield return Fade(1f, 0f, fadeInDuration);
            if (!isLoading) fadeGroup.blocksRaycasts = false;//操作できるように戻す
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            yield return Fade(fadeGroup.alpha, 1f, fadeOutDuration);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// alphaをfromからtoまで、duration秒かけて変える<br/>
        /// フェード中はクリックを通さない。
        /// </summary>
        private IEnumerator Fade(float from, float to, float duration)
        {
            fadeGroup.blocksRaycasts = true;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(from, to, timer / duration);
                yield return null;
            }
            fadeGroup.alpha = to;
        }
    }
}
