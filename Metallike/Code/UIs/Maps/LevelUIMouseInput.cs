using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    // 스크롤과 클릭 인터페이스 추가
    public class LevelUIMouseInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IScrollHandler, IPointerClickHandler
    {
        [Header("Draging Settings")]
        [SerializeField] private float dragingDuration;
        private float time;

        [Header("Size Setting")]
        [SerializeField] private Vector2 maxTargetSize;
        [SerializeField] private Vector2 defaultSize;

        private bool isDown;
        private bool isDragged; // ✨ 추가: 드래그 상태를 추적하는 변수
        private Vector2 prevPos;

        private RectTransform rect;
        private Vector2 defaultPos;

        private void Awake()
        {
            isDown = false;
            isDragged = false;

            rect = GetComponent<RectTransform>();
            defaultPos = rect.anchoredPosition;

            if (defaultSize == Vector2.zero)
                defaultSize = rect.sizeDelta;

            Bus<MapViewSetShowEvent>.OnEvent += HandleMapViewSetShowEvent;
        }

        private void OnDestroy()
        {
            Bus<MapViewSetShowEvent>.OnEvent -= HandleMapViewSetShowEvent;
        }

        private void HandleMapViewSetShowEvent(MapViewSetShowEvent evt)
        {
            isDown = false;
            isDragged = false; // 지도 표시 상태가 변경될 때 드래그 상태도 초기화
        }

        #region Drag Input
        public void OnPointerDown(PointerEventData eventData)
        {
            isDown = true;
            isDragged = false; // ✨ 추가: 마우스를 누를 때 드래그 여부 초기화
            time = Time.unscaledTime;
            prevPos = eventData.position;
            rect.sizeDelta = maxTargetSize;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDown = false;
            rect.anchoredPosition = defaultPos;
            rect.sizeDelta = defaultSize;

            // 주의: Unity 이벤트 순서상 OnPointerUp 이후에 OnPointerClick이 호출되므로 
            // 여기에서 isDragged를 false로 초기화하면 안 됩니다.
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isDown) return;
            if (Time.unscaledTime - time <= dragingDuration) return;

            isDragged = true; // ✨ 추가: 실제 드래그 조건을 만족하여 이동했을 때 true로 설정

            Vector2 force = eventData.position - prevPos;

            BusManager.Instance.SendEvent(new MapViewDragEvent(force));
            prevPos = eventData.position;
        }
        #endregion

        #region Scroll & Click Input
        public void OnScroll(PointerEventData eventData)
        {
            BusManager.Instance.SendEvent(new MapViewScrollEvent(eventData.scrollDelta.y));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragged) return;

            if (eventData.button == PointerEventData.InputButton.Middle)
            {
                BusManager.Instance.SendEvent<MapViewFocusEvent>();
            }
            else if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
            {
                BusManager.Instance.SendEvent<MapViewResetEvent>();
            }
        }
        #endregion
    }
}