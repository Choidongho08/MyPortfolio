using Assets.Work.CDH.Code.Maps;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public class RegionMapView : MonoBehaviour, IRegionMapView
    {
        public event Action<RegionType> OnSelectRegion;
        public event Action OnChangeView;

        [Header("UI References")]
        [SerializeField] private MapRegionPrefabSO mapRegionPrefabSO;
        [SerializeField] private RectTransform mapRegionRoot;
        [SerializeField] private RegionDescriptionUI regionDescriptionUI;

        [Header("Fade Settings")]
        [SerializeField, Tooltip("내가 보여줄 내용을 관리하는 캔버스 그룹")] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 1f;

        CanvasGroup IFadeInOutable.FadeGroup => fadeGroup;

        private IRegionIntgrationMap regionUI;

        private bool isShow;

        public void Initialize(int mapId, List<RegionImageInitData> regionDataList)
        {
            EnableView();
            regionDescriptionUI.Initialize();
            regionDescriptionUI.DisableView(0f);

            CreateMapUI(mapId);
            regionUI.Initialize(regionDataList);

            regionUI.OnSelectRegion += HandleSelectRegion;
            regionDescriptionUI.OnClickStart += HandleViewToRoomView;
        }

        private void OnDestroy()
        {
            if (regionUI != null)
            {
                regionUI.OnSelectRegion -= HandleSelectRegion;
                regionDescriptionUI.OnClickStart -= HandleViewToRoomView;
            }
        }

        private void HandleViewToRoomView()
        {
            OnChangeView?.Invoke();
        }

        private void HandleSelectRegion(RegionType regionType)
        {
            OnSelectRegion?.Invoke(regionType);
        }

        private void CreateMapUI(int mapId)
        {
            GameObject prefab = mapRegionPrefabSO.GetMapRegionPrefab(mapId);
            IRegionIntgrationMap regionUI = Instantiate(prefab, mapRegionRoot).GetComponent<IRegionIntgrationMap>();
            this.regionUI = regionUI;
        }

        public void UpdateRegionDesctionUIState(RegionDesctionUIState state)
        {
            if(state == default || state.RegionType == RegionType.None || state.RegionType == RegionType.Last)
            {
                regionDescriptionUI.DisableView();
                return;
            }

            regionDescriptionUI.EnableView();
            regionDescriptionUI.UpdateState(state);
        }

        public void EnableView()
        {
            if (isShow)
                return;

            isShow = true;
            this.FadeIn(fadeDuration);
        }

        public void DisableView()
        {
            if (!isShow)
                return;

            isShow = false;
            this.FadeOut(fadeDuration);
            regionDescriptionUI.DisableView(fadeDuration);
        }
    }
}