using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets._01.Member.CDH.Code.Cores
{
    public class TransitionManager : MonoSingleton<TransitionManager>
    {
        [SerializeField] private Animator transitionAnimator;
        [SerializeField] private AnimationClip coverClip;
        [SerializeField] private AnimationClip discoverClip;
        [SerializeField] private string loadingSceneName;
        [SerializeField] private float minLoadingTime;
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform parent;

        [field: SerializeField] public string NextSceneName { get; set; }

        string coverAnimationName;
        string discoverAnimationName;

        protected override void Awake()
        {
            base.Awake();

            coverAnimationName = coverClip.name;
            discoverAnimationName = discoverClip.name;
            panel.SetActive(false);
        }

        [ContextMenu("TestLoadScene")]
        public async void LoadScene()
        {
            panel.SetActive(true);
            parent.gameObject.SetActive(true);
            await Play();
            await Loading();
            await PlayReverse();
            panel.SetActive(false);
            parent.gameObject.SetActive(false);
        }

        private async Task Play()
        {
            transitionAnimator.speed = 1f;
            transitionAnimator.Play(coverAnimationName, 0, 0.0f);

            AnimatorStateInfo info = transitionAnimator.GetCurrentAnimatorStateInfo(0);

            while (transitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f ||
                   !transitionAnimator.GetCurrentAnimatorStateInfo(0).IsName(coverAnimationName))
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }

        private async Task Loading()
        {
            float timer = 0f;

            // 1. 로딩 씬 먼저 로드 (동기 또는 비동기)
            AsyncOperation loadingOp = SceneManager.LoadSceneAsync(loadingSceneName);
            loadingOp.allowSceneActivation = true; // 로딩씬은 바로 활성화

            while (!loadingOp.isDone)
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            parent.DOShakeAnchorPos(minLoadingTime, 30f, 20, 180f, false, true);

            // 2. 다음 씬 비동기 로드
            AsyncOperation nextOp = SceneManager.LoadSceneAsync(NextSceneName);
            nextOp.allowSceneActivation = false;

            // 최소 시간 동안 로딩 화면 유지
            while (timer < minLoadingTime)
            {
                timer += Time.deltaTime;
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            while (!nextOp.allowSceneActivation)
            {
                float progress = Mathf.Clamp01(nextOp.progress / 0.9f);

                if (progress >= 1f)
                {
                    nextOp.allowSceneActivation = true; // 최소 시간 지나고 씬 전환
                }

                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

        }

        private async Task PlayReverse()
        {
            transitionAnimator.speed = 1f;
            transitionAnimator.Play(discoverAnimationName, 0, 0.0f);

            AnimatorStateInfo info = transitionAnimator.GetCurrentAnimatorStateInfo(0);

            while (transitionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f ||
                   !transitionAnimator.GetCurrentAnimatorStateInfo(0).IsName(discoverAnimationName))
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }
    }
}
