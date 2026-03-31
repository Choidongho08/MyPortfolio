using Assets.Work.CDH.Code.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public class RegionIntgrationMap : MonoBehaviour, IRegionIntgrationMap
    {
        public event Action<RegionType> OnSelectRegion;

        [SerializeField] private RegionBackground background;

        private List<IRegionImage> regionImages;

        private void HandleRegionClick(RegionType type)
        {
            OnSelectRegion?.Invoke(type);
        }

        private void GetAllRegionImages()
        {
            regionImages = GetComponentsInChildren<IRegionImage>().ToList();
        }

        public void Initialize(List<RegionImageInitData> initDataList)
        {
            Debug.Assert(initDataList != null || initDataList.Count != 0, $"{name}의 Init중 initDataList의 값이 없습니다.");

            GetAllRegionImages();

            if (initDataList == null || regionImages == null || regionImages.Count <= 0) return;

            var initDataDict = new Dictionary<RegionType, RegionImageInitData>(initDataList.Count);

            foreach (var data in initDataList)
            {
                initDataDict.TryAdd(data.RegionType, data);
            }
            foreach (var regionImage in regionImages)
            {
                if (initDataDict.TryGetValue(regionImage.RegionType, out RegionImageInitData matchedData))
                {
                    regionImage.Initialize(matchedData);
                    regionImage.OnClick += HandleRegionClick;
                }
            }
            background.OnClick += HandleRegionBackgroundClick;
        }

        private void OnDestroy()
        {
            background.OnClick -= HandleRegionBackgroundClick;
        }

        private void HandleRegionBackgroundClick(RegionType type)
        {
            OnSelectRegion?.Invoke(type);
        }
    }
}
