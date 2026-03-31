using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    [Serializable]
    public record struct RegionInfo
    {
        [field: SerializeField] public RegionType RegionType { get; private set; }
        [field: SerializeField] public RegionInfoSO RegionInfoSO { get; private set; }
    }

    [CreateAssetMenu(fileName = "MapInfoSO", menuName = "SO/CDH/MapInfoSO")]
    public class MapInfoSO : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<RegionInfo> regionInfoList;

        private Dictionary<RegionType, RegionInfo> regionInfoDict = new();

        // 에셋이 메모리에 로드될 때 호출
        public void OnAfterDeserialize()
        {
            regionInfoDict.Clear();

            // 방어 코드: 리스트가 아직 생성되지 않았을 때를 대비
            if (regionInfoList == null) return;

            foreach (var data in regionInfoList)
            {
                if (!regionInfoDict.ContainsKey(data.RegionType))
                {
                    regionInfoDict.Add(data.RegionType, data);
                }
            }
        }

        // 저장되기 직전에 호출
        public void OnBeforeSerialize()
        {
        }

        public RegionInfo GetRegionInfo(RegionType regionType)
        {
            return regionInfoDict.GetValueOrDefault(regionType);
        }
    }
}