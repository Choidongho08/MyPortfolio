using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    [Serializable]
    public struct SecurityLevelPaneltyRoomFunction
    {
        public int SecurityLevel;
        public RoomFunctionSO Function;
    }

    [CreateAssetMenu(fileName = "SecurityLevelPaneltyRoomFunctionListSO", menuName = "SO/CDH/SecurityLevelPaneltyRoomFunctionListSO")]
    public class SecurityLevelPaneltyRoomFunctionListSO : ScriptableObject
    {
        [SerializeField] private List<SecurityLevelPaneltyRoomFunction> roomFunctionSOList;

        public Dictionary<int, List<SecurityLevelPaneltyRoomFunction>> PaneltyFunctionDict = new();

        private void OnEnable()
        {
            foreach(var item in roomFunctionSOList)
            {
                if (!PaneltyFunctionDict.TryGetValue(item.SecurityLevel, out var list))
                    PaneltyFunctionDict[item.SecurityLevel] = list = new();
                list.Add(item);
            }
        }
    }
}
