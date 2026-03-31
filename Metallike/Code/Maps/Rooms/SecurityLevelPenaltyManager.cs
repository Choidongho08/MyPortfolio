using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class SecurityLevelPenaltyManager : MonoBehaviour
    {
        [SerializeField] private SecurityLevelPaneltyRoomFunctionListSO functionListSO;

        private void Awake()
        {
            Bus<SecurityLevelUpgradeEvent>.OnEvent += HandleSecurityLevelUpgradeEvent;
        }

        private void OnDestroy()
        {
            Bus<SecurityLevelUpgradeEvent>.OnEvent -= HandleSecurityLevelUpgradeEvent;
        }

        private void HandleSecurityLevelUpgradeEvent(SecurityLevelUpgradeEvent evt)
        {
            if (functionListSO.PaneltyFunctionDict.TryGetValue(evt.SecurityLevel, out var list))
            {
                var item = list[Random.Range(0, list.Count)];
                // if (!item.Function.TryUse<>(evt.RoomDef, evt.Room))
                // {
                //     Debug.LogError($"{item.Function.name}기능이 실패하였습니다. 현재 방 : {evt.Room.GameObject.name} 타입 : {evt.RoomDef.RoomType}");
                //     return;
                // }
            }
        }
    }
}
