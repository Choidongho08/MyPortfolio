using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections; // Coroutine 사용을 위해 추가
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Work.CDH.Code.Maps
{
    [RequireComponent(typeof(Image))]
    public class MinimapEntityIcon : MonoBehaviour, IPoolable
    {
        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }

        [SerializeField] private Image image;
        [SerializeField] private float fadeDuration = 0.5f; // 페이드 시간 설정

        public GameObject GameObject => gameObject;

        private Pool myPool;
        public RectTransform Rect { get; private set; }
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            Rect = transform as RectTransform;
        }

        public void ResetItem()
        {
            // 초기화 시 알파값을 0으로 설정 후 페이드 인 시작
            SetAlpha(0f);

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeRoutine(1f, null)); // 1로 페이드 인, 콜백 없음
        }

        public void PushItem()
        {
            // 즉시 푸시하지 않고, 페이드 아웃 시작
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            // 0으로 페이드 아웃, 완료 후 풀에 반환(Push)
            fadeCoroutine = StartCoroutine(FadeRoutine(0f, () =>
            {
                myPool.Push(this);
            }));
        }

        public void SetUpPool(Pool pool)
        {
            myPool = pool;
        }

        public void SetColor(Color color)
        {
            // 색상을 변경하되, 현재 진행 중인 알파값은 유지해야 자연스러움
            float currentAlpha = image.color.a;
            color.a = currentAlpha;
            image.color = color;
        }

        public void SetAsLastSibling()
        {
            Rect.SetAsLastSibling();
        }

        // 알파값만 즉시 변경하는 헬퍼 함수
        private void SetAlpha(float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        // 페이드 애니메이션 코루틴
        private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
        {
            float startAlpha = image.color.a;
            float time = 0f;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
                SetAlpha(newAlpha);
                yield return null;
            }

            // 루프 종료 후 목표값 확실히 적용
            SetAlpha(targetAlpha);

            // 완료 후 실행할 작업이 있다면 실행 (PushItem의 경우 여기서 Push됨)
            onComplete?.Invoke();
            fadeCoroutine = null;
        }
    }
}