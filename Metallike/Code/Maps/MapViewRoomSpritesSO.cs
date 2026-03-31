using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    [Serializable]
    public struct RoomSprite
    {
        public Sprite Sprite;
        public RoomType RoomType;
    }

    [Serializable]
    public struct ModuleRoomSprite
    {
        public Sprite Sprite;
        public Category RoomType;
    }

    [CreateAssetMenu(fileName = "MapViewSpritesSO", menuName = "SO/CDH/MapViewSpriteSO")]
    public class MapViewRoomSpritesSO : ScriptableObject, ISerializationCallbackReceiver
    {
        public List<RoomSprite> mapViewRoomSprite;
        public List<RoomSprite> minimapViewRoomIcon;
        public List<ModuleRoomSprite> moduleSprites;

        private Dictionary<RoomType, Sprite> mapViewRoomSpriteByType = new();
        private Dictionary<RoomType, Sprite> mimimapViewRoomIconByType = new();
        private Dictionary<Category, Sprite> spriteByModule = new();

        private void OnValidate()
        {
            OnAfterDeserialize();
        }

        public void OnAfterDeserialize()
        {
            mapViewRoomSpriteByType.Clear();
            mimimapViewRoomIconByType.Clear();
            spriteByModule.Clear();
            foreach (var sprite in mapViewRoomSprite)
            {
                mapViewRoomSpriteByType.Add(sprite.RoomType, sprite.Sprite);
            }
            foreach(var icon in minimapViewRoomIcon)
            {
                mimimapViewRoomIconByType.Add(icon.RoomType, icon.Sprite);
            }
            foreach (var sprite in moduleSprites)
            {
                spriteByModule.Add(sprite.RoomType, sprite.Sprite);
            }
        }

        public Sprite GetSprite(Category roomType)
        {
            return spriteByModule.GetValueOrDefault(roomType);
        }

        public void GetSprite(RoomType roomType, out Sprite roomSpriteForMapView, out Sprite roomIconForMimimapView)
        {
            roomSpriteForMapView = mapViewRoomSpriteByType.GetValueOrDefault(roomType);
            roomIconForMimimapView = mimimapViewRoomIconByType.GetValueOrDefault(roomType);
        }

        public void OnBeforeSerialize()
        {
        }
    }
}
