using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    [Serializable]
    public struct RoomPrefab
    {
        public RoomType Type;
        public List<PoolItemSO> Items;
    }

    [CreateAssetMenu(fileName = "RoomPrefabSO", menuName = "SO/CDH/RoomPrefabs")]
    public class RoomPrefabsSO : ScriptableObject
    {
        public List<RoomPrefab> roomPrefabsByType;
    }
}
