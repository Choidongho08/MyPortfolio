using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Work.CDH.Code.Maps
{
    public class DoorUI : MonoBehaviour, IDoorUI, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public DoorDef DoorDef { get; private set; }
        public Action OnClickDoor { get; set; }

        [SerializeField] private float sizeMultiplier;
        [SerializeField] private float hoveringDuration;

        private Vector2 defaultSize;
        private Vector2 hoverSize;
        private RectTransform rect;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickDoor?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            rect.DOKill();
            rect.DOSizeDelta(hoverSize, hoveringDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            rect.DOKill();
            rect.DOSizeDelta(defaultSize, hoveringDuration);
        }

        public void SetDoorDef(DoorDef doorDef)
        {
            DoorDef = doorDef;
            rect = transform as RectTransform;
            defaultSize = rect.sizeDelta;
            hoverSize = defaultSize * sizeMultiplier;
        }
    }
}
