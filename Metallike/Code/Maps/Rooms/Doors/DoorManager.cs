using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms.Doors;
using Core.EventBus;
using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Core;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public class DoorManager : MonoBehaviour, IDoorManager
    {
        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }

        public GameObject GameObject => gameObject;
        public Transform Transform => transform;

        [Header("Door Objects")]
        [SerializeField] private Door[] doors;

        private Pool myPool;
        private Vector2Int currentGridPos;

        private Dictionary<EnterDirection, Door> doorsByDir;

        public event Action<Vector2Int> OnNewRoom;

        public void SetUpPool(Pool pool)
        {
            myPool = pool;
        }

        public void ResetItem()
        {
            foreach (var door in doors)
            {
                door.OnEnter -= HandleDoorEnter;
                door.OnUnlock -= HandleDoorUnlock;
            }
        }

        public void Init()
        {
            doorsByDir = new();
            foreach (var door in doors)
            {
                doorsByDir[door.Dir] = door;
                door.OnEnter += HandleDoorEnter;
                door.OnUnlock += HandleDoorUnlock;
            }
        }


        private void Awake()
        {
            Bus<EnemySpawnEvent>.OnEvent += HandleEnemySpawnEvent;
            Bus<RoomClearEvent>.OnEvent += HandleRoomClearEvent;
            Bus<EndAddRoomEvent>.OnEvent += HandleEndAddRoomEvent;
        }

        private void OnDestroy()
        {
            Bus<EnemySpawnEvent>.OnEvent -= HandleEnemySpawnEvent;
            Bus<RoomClearEvent>.OnEvent -= HandleRoomClearEvent;
            Bus<EndAddRoomEvent>.OnEvent -= HandleEndAddRoomEvent;
        }

        private void HandleEndAddRoomEvent(EndAddRoomEvent evt)
        {
            foreach (var door in doors)
            {
                door.CantUnlockState();
            }
        }

        private void HandleEnemySpawnEvent(EnemySpawnEvent evt)
        {
            foreach (var door in doors)
            {
                door.Battle();
            }
        }

        private void HandleRoomClearEvent(RoomClearEvent evt)
        {
            foreach (var door in doors)
            {
                if(door.IsBattle)
                    door.BattleEnd();
            }
        }

        private void HandleDoorEnter(EnterDirection dir)
        {
            Vector2Int nextGridPos = currentGridPos;
            switch (dir)
            {
                case EnterDirection.Up:
                    nextGridPos.y += 1;
                    break;
                case EnterDirection.Down:
                    nextGridPos.y -= 1;
                    break;
                case EnterDirection.Left:
                    nextGridPos.x -= 1;
                    break;
                case EnterDirection.Right:
                    nextGridPos.x += 1;
                    break;
            }

            Bus<DoorEnterTriggerEvent>.OnEvent?.Invoke(new(dir, nextGridPos));
        }

        private void HandleDoorUnlock(EnterDirection dir)
        {
            Vector2Int nextRoomPos = currentGridPos;
            switch (dir)
            {
                case EnterDirection.Up:
                    nextRoomPos.y += 1;
                    break;
                case EnterDirection.Down:
                    nextRoomPos.y -= 1;
                    break;
                case EnterDirection.Left:
                    nextRoomPos.x -= 1;
                    break;
                case EnterDirection.Right:
                    nextRoomPos.x += 1;
                    break;
            }

            OnNewRoom?.Invoke(nextRoomPos);
        }

        public void SetRoomGridPos(Vector2Int gridPos)
        {
            currentGridPos = gridPos;
        }

        public void DoorsInitlaizer()
        {
            foreach (var door in doorsByDir.Values)
            {
                door.Initialize();
            }
        }

        public void DoorSetting(Dictionary<EnterDirection, Vector3> posByDir, IRoomDef roomDef)
        {
            foreach (var kv in posByDir)
            {
                if (doorsByDir.TryGetValue(kv.Key, out var door))
                {
                    door.transform.position = kv.Value;
                }
            }
            foreach(var kv in roomDef.DoorsEnterableDict)
            {
                if(kv.Value)
                {
                    if(doorsByDir.TryGetValue(kv.Key, out var door))
                    {
                        door.SetEnterable();
                    }
                }
            }
        }

        public void PushItem()
        {
            myPool.Push(this);
        }

        public void SetMiddleBossSetting(List<DoorDef> doorDefs, IMapDataProvider model, Vector2Int room)
        {
            int doorCnt = doorDefs.Count;
            for(int i = 0; i < doorCnt; ++i)
            {
                var doorDef = doorDefs[i];
                var targetPos = doorDef.AGridPos == room ? doorDef.BGridPos : doorDef.AGridPos;
                var dir = targetPos - room;
                EnterDirection targetDir = EnterDirection.Up;
                if(dir == Vector2Int.up)
                {
                    targetDir = EnterDirection.Up;
                }
                if (dir == Vector2Int.down)
                {
                    targetDir = EnterDirection.Down;
                }
                if (dir == Vector2Int.right)
                {
                    targetDir = EnterDirection.Right;
                }
                if (dir == Vector2Int.left)
                {
                    targetDir = EnterDirection.Left;
                }
                Door door = new();
                foreach(var tDoor in doors)
                {
                    if (tDoor.Dir == targetDir)
                        door = tDoor;
                }
                if (door.IsEnterable && model.GetRoomDefByPos(targetPos).IsFirstEnter)
                {
                    door.IconSetting(InteractionIconEnum.EIcon, InteractionIconEnum.MiddleBossIcon);
                    continue;
                }
                else if (door.IsEnterable)
                    continue;

                door.IconSetting(InteractionIconEnum.MiddleBossIcon, InteractionIconEnum.MiddleBossIcon);
            }
        }

        public void PossibleDoorsSetting(List<EnterDirection> possibleDirs)
        {
            foreach (var dir in possibleDirs)
            {
                if (doorsByDir.TryGetValue(dir, out var door))
                {
                    if (door.IsEnterable)
                        continue;

                    door.CanUnlockState();
                    door.IconSetting(InteractionIconEnum.Possible, InteractionIconEnum.Possible);
                }
            }
        }

        public void ImpossibleDoorsSetting(List<EnterDirection> impossibleDirs)
        {
            foreach (var dir in impossibleDirs)
            {
                if (doorsByDir.TryGetValue(dir, out var door))
                {
                    if (door.IsEnterable)
                        continue;

                    door.CantUnlockState();
                    door.IconSetting(InteractionIconEnum.Impossible, InteractionIconEnum.Impossible);
                }
            }
        }
    }
}