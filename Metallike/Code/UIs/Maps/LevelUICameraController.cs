using DG.Tweening;
using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public interface ILevelUICameraController
    {
        void ResetToDefaultView();
        void FocusToPosition(Vector2 targetLocalPos);
        void TranslateView(Vector2 force);
        void SetZoomScale(float targetScale); 
        void ZoomAndFocus(float targetScale, Vector2 targetLocalPos);
    }

    public class LevelUICameraController : MonoBehaviour, ILevelUICameraController
    {
        [Header("Target Settings")]
        [SerializeField] private RectTransform viewport = null!;
        [SerializeField] private RectTransform mapTarget = null!;

        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 2.0f;

        [Header("Translate Settings")]
        [SerializeField] private float translateViewSpeed = 1f;
        [Range(0, 1)][SerializeField] private float translateViewWeight = 1f;

        #region Controller Commands (DOTween)

        public void ResetToDefaultView()
        {
            if (mapTarget == null) return;
            mapTarget.DOKill();
            mapTarget.DOLocalMove(Vector3.zero, 0.5f).SetUpdate(true);
            mapTarget.DOScale(Vector3.one, 0.5f).SetUpdate(true);
        }

        public void FocusToPosition(Vector2 targetLocalPos)
        {
            if (mapTarget == null) return;
            Vector2 clampedPos = GetClampedPosition(-targetLocalPos, mapTarget.localScale.x);
            mapTarget.DOKill();
            mapTarget.DOAnchorPos(clampedPos, 0.5f).SetUpdate(true);
        }

        public void ZoomAndFocus(float targetScale, Vector2 targetLocalPos)
        {
            if (mapTarget == null) return;

            // 목표 스케일을 기준으로 이동해야 할 위치를 미리 계산
            Vector2 targetAnchor = -targetLocalPos * targetScale;
            Vector2 clampedPos = GetClampedPosition(targetAnchor, targetScale);

            mapTarget.DOKill();
            mapTarget.DOScale(targetScale, 0.5f).SetUpdate(true);
            mapTarget.DOAnchorPos(clampedPos, 0.5f).SetUpdate(true);
        }

        public void TranslateView(Vector2 force)
        {
            if (mapTarget == null) 
                return;
            float currentScale = mapTarget.localScale.x;
            Vector2 targetPos = mapTarget.anchoredPosition + (force * translateViewSpeed * Mathf.Max(currentScale * translateViewWeight, 1f));
            targetPos = GetClampedPosition(targetPos, currentScale);
            mapTarget.DOKill();
            mapTarget.DOAnchorPos(targetPos, 0.08f).SetUpdate(true);
        }

        public void SetZoomScale(float targetScale)
        {
            if (mapTarget == null) return;
            float currentScale = mapTarget.localScale.x;
            if (Mathf.Approximately(currentScale, targetScale)) return;

            float scaleRatio = targetScale / currentScale;
            Vector2 targetPos = mapTarget.anchoredPosition * scaleRatio;
            targetPos = GetClampedPosition(targetPos, targetScale);

            mapTarget.DOKill();
            mapTarget.DOScale(targetScale, 0.1f).SetUpdate(true);
            mapTarget.DOAnchorPos(targetPos, 0.1f).SetUpdate(true);
        }


        #endregion

        #region Bounds & Synchronization Logic

        private void GetMovementBounds(float scale, out Vector2 minBounds, out Vector2 maxBounds)
        {
            if (mapTarget == null || viewport == null)
            {
                minBounds = Vector2.zero;
                maxBounds = Vector2.zero;
                return;
            }

            Vector2 scaledMapSize = mapTarget.rect.size * scale;
            Vector2 viewportSize = viewport.rect.size;

            Vector2 diff = scaledMapSize - viewportSize;

            float boundX = Mathf.Max(0, diff.x * 0.5f);
            float boundY = Mathf.Max(0, diff.y * 0.5f);

            minBounds = new Vector2(-boundX, -boundY);
            maxBounds = new Vector2(boundX, boundY);
        }

        // private void GetMovementBounds(out Vector2 minBounds, out Vector2 maxBounds)
        // {
        //     GetMovementBounds(mapTarget != null ? mapTarget.localScale.x : 1f, out minBounds, out maxBounds);
        // }

        private Vector2 GetClampedPosition(Vector2 targetPos, float scale)
        {
            GetMovementBounds(scale, out Vector2 minBounds, out Vector2 maxBounds);

            return new Vector2(
                Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x),
                Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y)
            );
        }

        public void ClampPosition()
        {
            if (mapTarget == null) return;
            mapTarget.anchoredPosition = GetClampedPosition(mapTarget.anchoredPosition, mapTarget.localScale.x);
        }

        // public Vector2 GetNormalizedPosition()
        // {
        //     GetMovementBounds(out Vector2 minBounds, out Vector2 maxBounds);
        // 
        //     if (Mathf.Approximately(minBounds.x, maxBounds.x) || Mathf.Approximately(minBounds.y, maxBounds.y))
        //         return new Vector2(0.5f, 0.5f);
        // 
        //     float normX = Mathf.InverseLerp(maxBounds.x, minBounds.x, mapTarget.anchoredPosition.x);
        //     float normY = Mathf.InverseLerp(maxBounds.y, minBounds.y, mapTarget.anchoredPosition.y);
        // 
        //     return new Vector2(normX, normY);
        // }
        // 
        // public void SetNormalizedPosition(Vector2 normalizedPos)
        // {
        //     GetMovementBounds(out Vector2 minBounds, out Vector2 maxBounds);
        // 
        //     if (Mathf.Approximately(minBounds.x, maxBounds.x) || Mathf.Approximately(minBounds.y, maxBounds.y))
        //     {
        //         mapTarget.anchoredPosition = Vector2.zero;
        //         return;
        //     }
        // 
        //     float targetX = Mathf.Lerp(maxBounds.x, minBounds.x, normalizedPos.x);
        //     float targetY = Mathf.Lerp(maxBounds.y, minBounds.y, normalizedPos.y);
        // 
        //     mapTarget.anchoredPosition = new Vector2(targetX, targetY);
        // }

        #endregion
    }
}