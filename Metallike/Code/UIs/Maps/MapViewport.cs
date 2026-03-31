using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public interface IMapViewport
    {
        void Show();
        void Hide();
    }

    public class MapViewport : MonoBehaviour, IMapViewport, IFadeInOutable
    {
        [Header("FadeInOut Setting")]
        [SerializeField, Tooltip("Inside가 아닌 전체 레벨 UI자체")] private CanvasGroup fadeInOutCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool isShow = true;

        // 명시적 구현
        CanvasGroup IFadeInOutable.FadeGroup => fadeInOutCanvasGroup;

        public void Show()
        {
            if (isShow)
                return;

            isShow = true;
            this.FadeIn(fadeDuration);
        }

        public void Hide()
        {
            if (!isShow)
                return;

            isShow = false;
            this.FadeOut(fadeDuration);
        }
    }
}