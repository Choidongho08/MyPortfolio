using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using DG.Tweening;
using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Fades
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFadeUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 1.0f;
        [SerializeField] private Ease fadeEase = Ease.Linear; // 페이드 곡선 (Linear 추천)

        private Tween _fadeTween;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            Bus<FadeInEvent>.OnEvent += HandleFadeInEvent;
            Bus<FadeOutEvent>.OnEvent += HandleFadeOutEvent;
        }

        private void OnDestroy()
        {
            // 오브젝트 파괴 시 실행 중인 트윈 안전하게 종료
            _fadeTween?.Kill();

            Bus<FadeInEvent>.OnEvent -= HandleFadeInEvent;
            Bus<FadeOutEvent>.OnEvent -= HandleFadeOutEvent;
        }

        private void HandleFadeOutEvent(FadeOutEvent evt)
        {
            FadeOut(evt.Duration, evt.OnComplete);
        }

        private void HandleFadeInEvent(FadeInEvent evt)
        {
            FadeIn(evt.Duration, evt.OnComplete);
        }

        /// <summary>
        /// 화면이 밝아짐 (검은막 1 -> 투명 0)
        /// </summary>
        public void FadeIn(float duration = -1f, Action onComplete = null)
        {
            float time = duration < 0 ? defaultDuration : duration;

            // 페이드 시작 전 클릭 차단 (페이드 도중 조작 방지)
            canvasGroup.blocksRaycasts = true;

            // 기존 트윈이 있다면 중지
            _fadeTween?.Kill();

            // DOTween 실행
            _fadeTween = canvasGroup.DOFade(1f, time)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    // 페이드 인이 끝나면(투명해지면) 클릭 허용
                    canvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 화면이 어두워짐 (투명 0 -> 검은막 1)
        /// </summary>
        public void FadeOut(float duration = -1f, Action onComplete = null)
        {
            float time = duration < 0 ? defaultDuration : duration;

            // 페이드 아웃 시작 즉시 클릭 차단
            canvasGroup.blocksRaycasts = true;

            _fadeTween?.Kill();

            _fadeTween = canvasGroup.DOFade(0f, time)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    // 어두워진 상태 유지 (클릭 차단 상태 유지)
                    onComplete?.Invoke();
                });
        }
    }
}