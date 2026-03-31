using Assets.Work.CDH.Code.Maps;
using Assets.Work.CDH.Code.Maps.Rooms;
using Assets.Work.CDH.Code.UIs.Maps;
using Core.EventBus;
using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Work.CDH.Code.Eventss
{
    public readonly struct MapModelSettingEndEvent : IEvent
    {
    }

    public struct InfoRoomEvent : IEvent
    {
        public readonly RoomType RoomInfo;
        public InfoRoomEvent(RoomType roomInfo) => this.RoomInfo = roomInfo;
    }

    public readonly struct FirstEnterRoomEvent : IEvent
    {
        public readonly IRoom Room;
        public readonly IRoomDef RoomDef;

        public FirstEnterRoomEvent(IRoom room, IRoomDef roomDef)
        {
            this.Room = room;
            this.RoomDef = roomDef;
        }
    }

    public struct EnterRoomEvent : IEvent
    {
        public readonly IRoomDef RoomDef;
        public EnterRoomEvent(IRoomDef roomDef) => this.RoomDef = roomDef;
    }

    public struct RoomClearEvent : IEvent
    {
        public IRoom ClearedRoom;
        public RoomClearEvent(IRoom room) => ClearedRoom = room;
    }

    public struct EnemySpawnEvent : IEvent
    {
        public readonly IMonsterRoom RoomData;
        public EnemySpawnEvent(IMonsterRoom roomData) => RoomData = roomData;
    }

    public readonly struct ChangeRoomDefEvent : IEvent
    {
        public readonly IRoomDef RoomDef;
        public ChangeRoomDefEvent(IRoomDef roomDef)
        {
            RoomDef = roomDef;
        }
    }

    public readonly struct BuddyRoomEvent : IEvent
    {
        public readonly BuddyRoom BuddyRoom;
        public BuddyRoomEvent(BuddyRoom buddyRoom)
            => BuddyRoom = buddyRoom;
    }

    public readonly struct BossRoomEvent : IEvent
    {
        public readonly BossRoom BossRoom;
        public BossRoomEvent(BossRoom bossRoom)
            => BossRoom = bossRoom;
    }

    public struct StartSelectStartRoomEvent : IEvent
    {
    }

    public struct EndSelectStartRoomEvent : IEvent
    {
        public readonly Vector2Int Pos;

        public EndSelectStartRoomEvent(Vector2Int pos)
        {
            Pos = pos;
        }
    }

    public readonly struct PlayerSetPosEvent : IEvent
    {
        public readonly Vector3 Position;
        public PlayerSetPosEvent(Vector3 pos)
        {
            Position = pos;
        }
    }

    public readonly struct StartAddRoomEvent : IEvent
    {
    }

    public readonly struct MapInitializeEvent : IEvent
    {

    }

    public readonly struct BossPenetrationCheckEvent : IEvent
    {

    }

    public readonly struct EndAddRoomEvent : IEvent
    {
        public readonly IRoomDef NewRoomDef;

        public EndAddRoomEvent(IRoomDef newRoomDef)
        {
            NewRoomDef = newRoomDef;
        }
    }

    /// <summary>
    /// From RoomEnterTrigger, To MapManager
    /// </summary>
    public readonly struct DoorEnterTriggerEvent : IEvent
    {
        public readonly EnterDirection EnterDir;
        public readonly Vector2Int GridPos;

        public DoorEnterTriggerEvent(EnterDirection enterDir, Vector2Int gridPos)
        {
            EnterDir = enterDir;
            GridPos = gridPos;
        }
    }

    /// <summary>
    /// FirstEnterRoom시 발행?
    /// </summary>
    public readonly struct RoomEnterEvent : IEvent
    {
        public readonly IRoom RoomObj;
        public readonly IRoomDef RoomDef;

        public RoomEnterEvent(IRoom roomObj, IRoomDef roomDef)
        {
            RoomObj = roomObj;
            RoomDef = roomDef;
        }
    }

    public readonly struct RoomExitEvent : IEvent
    {
        public readonly IRoom RoomObj;
        public readonly IRoomDef RoomDef;

        public RoomExitEvent(IRoom roomObj, IRoomDef roomDef)
        {
            RoomObj = roomObj;
            RoomDef = roomDef;
        }
    }

    public readonly struct GetRoomDefsEvent : IEvent
    {
        public readonly Action<List<IRoomDef>> RoomDefsHandler;
        public GetRoomDefsEvent(Action<List<IRoomDef>> roomDefsHandler)
        {
            RoomDefsHandler = roomDefsHandler;
        }
    }
    public readonly struct AliveEnemiesEvent : IEvent
    {
        public readonly List<Entity> Enemies;

        public AliveEnemiesEvent(List<Entity> enemies)
        {
            Enemies = enemies;
        }
    }

    public readonly struct BreakObstacleEvent : IEvent
    {
        public readonly BreakingObstacleEventData DebrisData;
        public BreakObstacleEvent(BreakingObstacleEventData debrisData)
        {
            DebrisData = debrisData;
        }
    }
    public readonly struct BreakObstacleVFXEvent : IEvent
    {
        public readonly PoolItemSO VfxItem;
        public readonly Vector3 Pos;
        public readonly Quaternion Rot;
        public readonly float Duration;
        public BreakObstacleVFXEvent(PoolItemSO vfxItem, Vector3 pos, Quaternion rot, float duration)
        {
            VfxItem = vfxItem;
            Pos = pos;
            Rot = rot;
            Duration = duration;
        }
    }
    public readonly struct RoomObjGenerateEvent : IEvent
    {
        public readonly GameObject Obj;

        public RoomObjGenerateEvent(GameObject obj)
        {
            Obj = obj;
        }
    }

    public readonly struct RoomObjRemoveEvent : IEvent
    {
        public readonly GameObject Obj;

        public RoomObjRemoveEvent(GameObject obj)
        {
            Obj = obj;
        }
    }
    /// <summary>
    /// 삭제해야 하는 이벤트
    /// </summary>
    public readonly struct MapBossCountEvent : IEvent
    {
        public readonly int MiniBossCnt;
        public readonly int BigBossCnt;

        public MapBossCountEvent(int big, int mini)
        {
            MiniBossCnt = mini;
            BigBossCnt = big;
        }
    }

    public readonly struct MapViewDragEvent : IEvent
    {
        public readonly Vector2 Force;

        public MapViewDragEvent(Vector2 force)
        {
            Force = force;
        }
    }

    public readonly struct MapViewSetShowEvent : IEvent
    {
        public readonly bool IsShow;

        public MapViewSetShowEvent(bool isShow)
        {
            IsShow = isShow;
        }
    }

    public readonly struct SecurityLevelUpdateEvent : IEvent
    {
        public readonly int Level;
        public readonly float Value;
        public SecurityLevelUpdateEvent(int level, float value)
        {
            Level = level;
            Value = value;
        }
    }

    public readonly struct SecurityLevelUpgradeEvent : IEvent
    {
        public readonly IRoomDef RoomDef;
        public readonly IRoom Room;
        public readonly int SecurityLevel;
        public SecurityLevelUpgradeEvent(IRoomDef roomDef, IRoom room, int level)
        {
            RoomDef = roomDef;
            Room = room;
            SecurityLevel = level;
        }
    }

    public readonly struct AllRoomCloseEvent : IEvent
    {
    }

    public readonly struct BossRoomUseCardkeyEvent : IEvent
    {
    }

    public readonly struct BossRoomCanEnterEvent : IEvent
    {
    }

    public readonly struct MapViewScrollEvent : IEvent
    {
        public readonly float ScrollY;
        public MapViewScrollEvent(float scrollY)
        {
            ScrollY = scrollY;
        }
    }

    public readonly struct MapViewFocusEvent : IEvent
    {
    }

    public readonly struct MapViewResetEvent : IEvent
    {
    }

}