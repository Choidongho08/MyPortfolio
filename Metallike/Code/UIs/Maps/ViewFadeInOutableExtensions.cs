using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine; // gameObject 접근을 위해 필요할 수 있습니다.

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public static class ViewFadeInOutableExtensions
    {
        public static void FadeIn(this IFadeInOutable view, float duration = 0.3f, bool isStartToBegin = true, Action onComplete = null)
        {
            var fadeGroup = view.FadeGroup;

            if (!fadeGroup.gameObject.activeSelf)
            {
                fadeGroup.gameObject.SetActive(true);
            }

            if (fadeGroup.alpha == 1f && isStartToBegin)
                fadeGroup.alpha = 0f;

            fadeGroup.interactable = true;
            fadeGroup.blocksRaycasts = true;
            fadeGroup.DOKill();

            fadeGroup.DOFade(1f, duration).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
        }

        public static async UniTask FadeInAsync(this IFadeInOutable view, bool isStartToBegin = true, float duration = 0.3f)
        {
            var fadeGroup = view.FadeGroup;

            if (!fadeGroup.gameObject.activeSelf)
            {
                fadeGroup.gameObject.SetActive(true);
            }

            if (fadeGroup.alpha == 1f && isStartToBegin)
                fadeGroup.alpha = 0f;

            fadeGroup.interactable = true;
            fadeGroup.blocksRaycasts = true;
            fadeGroup.DOKill();

            await fadeGroup.DOFade(1f, duration).SetUpdate(true).ToUniTask();
        }

        public static void FadeOut(this IFadeInOutable view, float duration = 0.3f, Action onComplete = null)
        {
            var fadeGroup = view.FadeGroup;

            if (fadeGroup.alpha == 0f && !fadeGroup.gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.DOKill();

            fadeGroup.DOFade(0f, duration).SetUpdate(true).OnComplete(() =>
            {
                fadeGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public static async UniTask FadeOutAsync(this IFadeInOutable view, float duration = 0.3f)
        {
            var fadeGroup = view.FadeGroup;

            if (fadeGroup.alpha == 0f && !fadeGroup.gameObject.activeSelf)
            {
                return;
            }

            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.DOKill();

            await fadeGroup.DOFade(0f, duration).SetUpdate(true).ToUniTask();

            fadeGroup.gameObject.SetActive(false);
        }
    }
}