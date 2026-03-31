using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using Public.Core.Events;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class RoomManager : MonoBehaviour
    {
        [Header("RoomData Setting")]
        [SerializeField] private RoomFunctionSO[] roomDefDefaultSettingFuncitions;

        private static IMapDataProvider model;
        private IRoomDef curRoomDef;
        private IRoom curRoom;

        private int curSecurityLevel;
        private float curSecurityValue;

        private Dictionary<int, float> securityLevelTargetValueDict;

        private void Awake()
        {
            curSecurityLevel = 1;
            securityLevelTargetValueDict = new();

            Bus<RoomObjGenerateEvent>.OnEvent += HandleRoomObjGenerateEvent;
            Bus<RoomObjRemoveEvent>.OnEvent += HandleRoomObjRemoveEvent;
            Bus<BreakObstacleEvent>.OnEvent += HandleBreakingObstacleEvent;
            Bus<RoomEnterEvent>.OnEvent += HandleRoomEnterEvent;
            Bus<RoomExitEvent>.OnEvent += HandleRoomExitEvent;
            Bus<FirstEnterRoomEvent>.OnEvent += HandleFirstEnterRoomEvent;
            Bus<BossDeadEvent>.OnEvent += HandleBossDeadEvent;
        }

        private void Start()
        {
            TableManager tableManager = Shared.InitTableMgr();
            var data = tableManager.SecurityLevel.Get();
            foreach(var item in data)
            {
                securityLevelTargetValueDict.Add(item.Level, item.TargetValue);
            }

            Bus<GetRoomDefsEvent>.OnEvent?.Invoke(new(HandleRoomDefHandler));
        }

        private void OnDestroy()
        {
            Bus<RoomObjGenerateEvent>.OnEvent -= HandleRoomObjGenerateEvent;
            Bus<RoomObjRemoveEvent>.OnEvent -= HandleRoomObjRemoveEvent;
            Bus<BreakObstacleEvent>.OnEvent -= HandleBreakingObstacleEvent;
            Bus<RoomEnterEvent>.OnEvent -= HandleRoomEnterEvent;
            Bus<RoomExitEvent>.OnEvent -= HandleRoomExitEvent;
            Bus<FirstEnterRoomEvent>.OnEvent -= HandleFirstEnterRoomEvent;
            Bus<BossDeadEvent>.OnEvent -= HandleBossDeadEvent;
        }

        private void HandleBossDeadEvent(BossDeadEvent evt)
        {
            ResetSercurityLevel();
        }

        private void HandleFirstEnterRoomEvent(FirstEnterRoomEvent evt)
        {
            SecurtityLevelUpgrade(evt.RoomDef);
        }

        private void HandleRoomDefHandler(List<IRoomDef> roomDefs)
        {
            int length = roomDefs.Count;
            for (int i = 0; i < length; ++i)
            {
                IRoomDef roomDef = roomDefs[i];
                foreach (var roomDefFunction in roomDefDefaultSettingFuncitions)
                {
                    var kvp = roomDefFunction.TryUse<IRoomDef>(roomDef);
                    if (!kvp.isSuccess || kvp.result == default)
                        continue;

                    IRoomDef changedRoomDef = kvp.result;
                    roomDef = changedRoomDef;
                }
                Bus<ChangeRoomDefEvent>.OnEvent?.Invoke(new(roomDef));
            }
        }

        private void HandleRoomEnterEvent(RoomEnterEvent evt)
        {
            var prevRoom = curRoomDef;
            curRoomDef = evt.RoomDef;
            curRoom = evt.RoomObj;
            foreach (var dynamicObj in evt.RoomDef.DynamicObjDatas.Objects)
            {
                dynamicObj.SetActive(true);
            }
            foreach (var kv in curRoom.BreakingObstacles)
            {
                kv.Value.SetActive(true);

                foreach (var id in curRoomDef.StaticObjDatas.BreakingObstacles)
                {
                    if (kv.Key.Equals(id))
                        kv.Value.SetActive(false);
                }
            }
        }

        private void HandleRoomExitEvent(RoomExitEvent evt)
        {
            foreach (var dynamicObj in evt.RoomDef.DynamicObjDatas.Objects)
            {
                dynamicObj.SetActive(false);
            }
        }

        public void Initialize(IMapDataProvider m)
        {
            model = m;
        }

        private void HandleRoomObjRemoveEvent(RoomObjRemoveEvent evt)
        {
            IRoomDef curRoomDef = model.GetPlayerRoomDef();
            var list = curRoomDef.DynamicObjDatas.Objects;
            if (list.Contains(evt.Obj))
            {
                list.Remove(evt.Obj);
            }
            model.ChangeRoomData(curRoomDef);
        }

        private void HandleRoomObjGenerateEvent(RoomObjGenerateEvent evt)
        {
            IRoomDef curRoomDef = model.GetPlayerRoomDef();
            curRoomDef.DynamicObjDatas.Objects.Add(evt.Obj);
            model.ChangeRoomData(curRoomDef);
        }

        private void HandleBreakingObstacleEvent(BreakObstacleEvent evt)
        {
            int id = evt.DebrisData.Id;
            curRoomDef.StaticObjDatas.BreakingObstacles.Add(id);
            BusManager.Instance.SendEvent(new ChangeRoomDefEvent(curRoomDef));
        }

        /// <summary>
        /// 보스 룸 진입 시그널 (페이드인)
        /// By Timeline Signal Receiver
        /// </summary>
        [ContextMenu("BossRoomPenetrationSignal_FadeInOut")]
        public void BossRoomPenetrationSignal_FadeInOut()
        {
            Bus<FadeInEvent>.OnEvent?.Invoke(new(1f,
                () => Bus<FadeOutEvent>.OnEvent?.Invoke(new(1f,
                () =>
                {
                    BusManager.Instance.SendEvent(new PlayerSetPosEvent((curRoom as BossRoom).PlayerPos.position));
                    BusManager.Instance.SendEvent(new BossRoomEvent((curRoom as BossRoom)));
                }
                ))));
        }

        public static string GetGroupName(RegionType type)
        {
            return model.GetRegionName(type);
        }

        private void ResetSercurityLevel()
        {
            curSecurityLevel = 1;
        }

        private void SecurtityLevelUpgrade(IRoomDef roomDef)
        {
            curSecurityValue += roomDef.SecurityLevelIncreaseValue;
            if (securityLevelTargetValueDict.TryGetValue(curSecurityLevel, out var targetSecurityLevelValue))
            {
                if (curSecurityValue >= targetSecurityLevelValue)
                {
                    curSecurityValue = 0.0f;
                    curSecurityLevel++;
                    BusManager.Instance.SendEvent(new SecurityLevelUpgradeEvent(roomDef, curRoom, curSecurityLevel));
                }
            }
        }
    }
}
