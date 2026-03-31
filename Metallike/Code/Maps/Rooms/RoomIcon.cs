using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Core.EventBus;
using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.UIs.Maps;

namespace Assets.Work.CDH.Code.Maps
{
    public class RoomIcon : StatefulView<RoomIconState>, IRoomIcon, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        public readonly record struct SpriteData(
            Sprite MainSprite,
            Sprite SubSprite
            );

        public event Action<Vector2Int> OnRoomIconClick;

        [field: SerializeField] public Image Background { get; private set; }
        [field: SerializeField] public Image Icon { get; private set; }
        [field: SerializeField] public Image subIcon { get; private set; }
        [field: SerializeField] public Image line { get; private set; }
        [field: SerializeField] public TextMeshProUGUI cctv;

        public RectTransform Rect => transform as RectTransform;

        public Vector2Int GridPos { get; private set; }

        [SerializeField] private Color enterColor;
        [SerializeField] private Color specialColor;
        [SerializeField] private Color activeColor;
        [SerializeField] private Color deactiveColor;
        [SerializeField] private Color lineColor;

        private bool isDont;
        private bool isFind;
        private bool canClick;
        private RoomType myType;
        private Color targetColor;

        [Header("Click Setting")]
        [SerializeField] private float clickDuration = 0.5f;
        private float downTime;

        #region Update Icon Logic

        protected override void OnUpdateState(RoomIconState state)
        {
            if (prevState != null && prevState.Value == state)
                return;

            var prev = prevState.GetValueOrDefault();

            Bind(prev.Type, state.Type, SettingType);
            Bind(prev.CanInteractable, state.CanInteractable, SetInteractable);
            SpriteData prevSpriteData = new SpriteData
            {
                MainSprite = prev.MainSprite,
                SubSprite = prev.SubSprite,
            };
            SpriteData curSpriteData = new SpriteData
            {
                MainSprite = state.MainSprite,
                SubSprite = state.SubSprite,
            };
            Bind(prevSpriteData, curSpriteData, SettingSprite);
            Bind(prev.ColorState, state.ColorState, ApplyColorState);

            prevState = state;
        }

        private void ApplyColorState(RoomIconColorTypeEnum type)
        {
            if (type is not (RoomIconColorTypeEnum.Special or RoomIconColorTypeEnum.Active or RoomIconColorTypeEnum.Deactive or RoomIconColorTypeEnum.Find))
                return;

            var (targetColor, targetLineColor, targetIsDont) = type switch
            {
                RoomIconColorTypeEnum.Special => (specialColor, specialColor, true),
                RoomIconColorTypeEnum.Active => (activeColor, lineColor, false),
                RoomIconColorTypeEnum.Deactive => (deactiveColor, lineColor, false),
                RoomIconColorTypeEnum.Find => (Color.green, lineColor, false),
                _ => default
            };
            this.targetColor = targetColor;

            line.color = targetLineColor;
            isDont = targetIsDont;
            isFind = type == RoomIconColorTypeEnum.Find; // if문 대신 한 줄로 깔끔하게 처리

            Icon.color = targetColor;
            subIcon.color = targetColor;
            Background.color = targetColor;
            cctv.color = targetColor;
        }

        private void SettingSprite(SpriteData data)
        {
            var main = data.MainSprite;
            var sub = data.SubSprite;

            if (sub != null)
            {
                subIcon.sprite = sub;
                subIcon.gameObject.SetActive(true);
            }
            else
                subIcon.gameObject.SetActive(false);

            if (main != null)
            {
                Icon.sprite = main;
                Icon.gameObject.SetActive(true);
            }
            else
                Icon.gameObject.SetActive(false);
        }

        private void SetInteractable(bool active)
        {
            canClick = active;
        }

        public void SetGridPos(Vector2Int gridPos)
        {
            GridPos = gridPos;
        }

        private void SettingType(RoomType roomType)
        {
            myType = roomType;
        }
        #endregion

        #region MousePointer Logic
        public void OnPointerEnter(PointerEventData eventData)
        {
            Bus<InfoRoomEvent>.Raise(new InfoRoomEvent(myType));
            if (isDont || isFind || !canClick) return;

            Color color;
            color = enterColor;
            Icon.color = color;
            subIcon.color = color;
            Background.color = color;
            line.color = lineColor;
            cctv.color = color;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDont || isFind || !canClick) return;

            Color color;
            color = targetColor;
            Icon.color = color;
            subIcon.color = color;
            Background.color = color;
            line.color = lineColor;
            cctv.color = color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // canClick = true;
            downTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!canClick)
                return;
            if (Time.unscaledTime - downTime > clickDuration)
                return;

            canClick = false;
            OnRoomIconClick?.Invoke(GridPos);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            // canClick = false;
        }
        #endregion
    }
}