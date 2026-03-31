using Assets.Work.CDH.Code.UIs.Maps;
using GondrLib.ObjectPool.RunTime;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Work.CDH.Code.Maps
{
    public enum RoomIconColorTypeEnum
    {
        Special,
        Active,
        Deactive,
        Find,
    }

    public readonly record struct RoomIconState(
        RoomType Type,
        RoomIconColorTypeEnum ColorState,
        bool CanInteractable,
        Sprite MainSprite = null,
        Sprite SubSprite = null
    );

    public interface IRoomIcon : IUpdatable<RoomIconState>
    {
        event Action<Vector2Int> OnRoomIconClick;
        RectTransform Rect { get; }
        Image Background { get; }
        Image Icon { get; }
        Vector2Int GridPos { get; }

        void SetGridPos(Vector2Int gridPos);
    }
}