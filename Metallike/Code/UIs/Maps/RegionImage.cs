using Assets.Work.CDH.Code.Maps;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public class RegionImage : MonoBehaviour, IRegionImage, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler
    {
        public event Action<RegionType> OnClick;

        [field: SerializeField] public RegionType RegionType { get; private set; }

        [SerializeField] private Image image;

        private Vector2 pointerDownPos;

        public void Initialize(RegionImageInitData data)
        {
            if (image == null)
                image = GetComponent<Image>();

            image.alphaHitTestMinimumThreshold = 0.1f;
        }

        public void UpdateState(RegionImageState state)
        {
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownPos = eventData.position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            float dragThreshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 15f;

            float distance = Vector2.Distance(pointerDownPos, eventData.position);

            if (distance > dragThreshold)
            {
                return;
            }

            OnClick?.Invoke(RegionType);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}