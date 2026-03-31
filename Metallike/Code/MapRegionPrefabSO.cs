using Cysharp.Threading.Tasks.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Work.CDH.Code
{
    [Serializable]
    public struct MapRegionPrefab
    {
        public int Id;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "MapRegionPrefabSO", menuName = "SO/CDH/MapRegionPrefabSO")]
    public class MapRegionPrefabSO : ScriptableObject
    {
        [SerializeField] private List<MapRegionPrefab> mapRegionPrefabList;

        public Dictionary<int, GameObject> MapRegionPrefabDict = new();
        
        private void OnEnable()
        {
            foreach (var item in mapRegionPrefabList)
            {
                MapRegionPrefabDict[item.Id] = item.Prefab;
            }
        }

        public GameObject GetMapRegionPrefab(int id) => MapRegionPrefabDict[id];
    }
}
