using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.UIs.Maps;
using Assets.Work.CDH.Code.UIs.Maps.SecurityLevels;
using Core.EventBus;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Work.CDH.Code.Maps
{
    public class MapUIManager : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private MapUIInstaller mapInstaller;
        [SerializeField] private MinimapUIInstaller miniMapInstaller;
        [SerializeField] private SecurityLevelUIInstaller securityLevelUIInstaller;
        [SerializeField] private SecurityLevelDataDictSO securityLevelDictSO;

        [Header("MapCore Settings")]
        [SerializeField] private MapInfoSO mapInfoSO;
        [SerializeField] private float currentGlobalZoom;
        [SerializeField] private float zoomSpeed;
        [SerializeField] private float zoomMinRegion;
        [SerializeField] private float zoomTheshold;
        [SerializeField] private float zoomMaxRoom;

        private MinimapPresenter minimapPresenter;
        private MapPresenter mapPresenter;
        private SecurityLevelPresenter securityLevelPresenter;
        private IMapDataProvider model;

        private bool mapShow = false;
        private bool isInit;

        public void Initialize(IMapDataProvider model)
        {
            this.model = model;

            Debug.Assert(securityLevelDictSO != null, $"{name}의 SecurityLevelPresenter를 Initializer 함수를 실행하던 중 securityLevelDictSO가 null입니다");
            model.SetSecurityLevelDictSO(securityLevelDictSO);

            mapPresenter = mapInstaller.Initializer(model);
            minimapPresenter = miniMapInstaller.Initializer(model);
            securityLevelPresenter = securityLevelUIInstaller.Iniitalizer(model);

            isInit = true;
            mapShow = false;

            MapPresenterInitData mapPresenterInitData = new(
                mapInfoSO,
                currentGlobalZoom,
                zoomSpeed,
                zoomMinRegion,
                zoomTheshold,
                zoomMaxRoom
                );
            mapPresenter.Initializer(mapPresenterInitData);
            securityLevelPresenter.Initialize();

            SubscribeEvents();
        }

        private void OnDestroy()
        {
            if (mapPresenter != null)
                mapPresenter.Release();
            if (minimapPresenter != null)
                minimapPresenter.Release();

            UnsubscribeEvents();
        }
        
        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.tabKey.wasPressedThisFrame && !isInit)
            {
                mapShow = !mapShow;
                mapPresenter.ShowMapUI(mapShow);
            }

            if (Mouse.current == null)
                return;

            float scrollY = Mouse.current.scroll.ReadValue().y;
            if (scrollY != 0f)
            {
                mapPresenter.MouseWheelCheck(Mathf.Sign(scrollY));
            }
        }

        private void SubscribeEvents()
        {
            Bus<EndSelectStartRoomEvent>.OnEvent += HandleEndSelectStartRoomEvent;
            Bus<EndAddRoomEvent>.OnEvent += HandleEndAddRoomEvent;
        }

        private void UnsubscribeEvents()
        {
            Bus<EndSelectStartRoomEvent>.OnEvent -= HandleEndSelectStartRoomEvent;
            Bus<EndAddRoomEvent>.OnEvent -= HandleEndAddRoomEvent;
        }

        private void HandleEndSelectStartRoomEvent(EndSelectStartRoomEvent evt)
        {
            isInit = false;
            MiniMapUISetting();
        }

        private void HandleEndAddRoomEvent(EndAddRoomEvent evt)
        {
            mapShow = false;
        }

        private void MiniMapUISetting()
        {
            minimapPresenter.Initialize(model.PlayerRoomPos);
        }

    }
}
