using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps;
using Core.EventBus;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public interface IUpdatable<T> where T : struct
    {
        void UpdateState(in T state);
    }

    public interface IFadeInOutable
    {
        CanvasGroup FadeGroup { get; }
    }

    public interface IHaveTargetMapView
    {
    }

    #region RegionMap
    public interface IRegionMapView : IFadeInOutable, IHaveTargetMapView
    {
        event Action<RegionType> OnSelectRegion;
        event Action OnChangeView;

        void Initialize(int mapId, List<RegionImageInitData> roomInitDataList);
        void EnableView();
        void DisableView();
        void UpdateRegionDesctionUIState(RegionDesctionUIState state);
    }

    #region IRegionIntgrationMap

    public interface IRegionIntgrationMap
    {
        event Action<RegionType> OnSelectRegion;

        void Initialize(List<RegionImageInitData> initDataList);
    }

    #endregion

    #region IRegionDescriptionUI
    public interface IRegionDescriptionUI : IFadeInOutable, IUpdatable<RegionDesctionUIState>
    {
        event Action OnClickStart;

        void Initialize();
    }

    [Serializable]
    public record struct IconInfo
    {
        [field: SerializeField] public Sprite Sprite { get; private set; }
        [field: SerializeField] public string TooltipText { get; private set; }
    }

    [Serializable]
    public record struct BossIconInfo
    {
        [field: SerializeField] public IconInfo RegionBossInfo { get; private set; }
        [field: SerializeField] public string RegionBossName { get; private set; }
    }

    public readonly record struct RegionDesctionUIState(
        // 구역 정보
        RegionType RegionType,
        string RegionName,
        string RegionDescription,
        // 구역 보스 정보
        BossIconInfo BossIconInfo,
        // 구역 얻을 수 있는 캐릭터 타입 정보
        IconInfo RegionCharacterInfo,
        // 높은 확률로 얻을 수 있는 모듈 정보
        IconInfo[] RegionModuleInfos
        );
    #endregion

    #endregion

    #region RoomMap
    public readonly record struct RoomIconInitData(Vector2Int GridPos, Vector2 IconSize, RoomType RoomType);

    public readonly record struct RoomMapRegionData(
        Vector2Int MinRoomPos,
        Vector2Int MaxRoomPos
    );

    public readonly record struct RoomMapState(
        bool IsPlayerVisible,
        Vector2Int PlayerPos,
        int SecurityLevel,
        RoomType RoomInfo,
        RoomMapRegionData RoomMapRegionData
    );

    public readonly record struct RegionImageInitData(
        RegionType RegionType,
        string RegionName
        );


    #endregion

    public readonly record struct MapPresenterCreateData(
        IMapViewport Viewport,
        IRoomMapView RoomsView,
        IRegionMapView RegionView,
        IMapDataProvider Model,
        ILevelUICameraController LevelUICameraController
        );

    public readonly record struct MapPresenterInitData(
        MapInfoSO MapInfoSO,
        float CurrentGlobalZoom,
        float ZoomSpeed,
        float ZoomMinRegion,
        float ZoomThreshold,
        float ZoomMaxRoom
        );

    public class MapPresenter
    {
        private IMapViewport viewport;
        private IRoomMapView roomsView;
        private IRegionMapView regionView;
        private IMapDataProvider model;
        private ILevelUICameraController cameraController; // 🎯 이동/줌/바운더리 계산은 이 녀석의 몫!

        private RoomMapState roomMapState;
        private RegionDesctionUIState regionDesctionUIState;
        private bool isBoss = false; 
        private bool isLockedInRoomView = false;

        private Dictionary<Vector2Int, RoomIconState> roomStatesDict = new();

        private RegionType curSelectedRegionType;

        private MapInfoSO mapInfoSO;

        private float currentGlobalZoom;
        private float zoomSpeed;
        private float zoomMinRegion;
        private float zoomThreshold;
        private float zoomMaxRoom;

        private bool isRegionView;

        public MapPresenter(MapPresenterCreateData createData)
        {
            Debug.Assert(createData.Viewport != null, createData);
            Debug.Assert(createData.RoomsView != null, createData);
            Debug.Assert(createData.RegionView != null, createData);
            Debug.Assert(createData.Model != null, createData);
            Debug.Assert(createData.LevelUICameraController != null, createData);

            viewport = createData.Viewport;
            roomsView = createData.RoomsView;
            regionView = createData.RegionView;
            model = createData.Model;
            cameraController = createData.LevelUICameraController;

            isRegionView = true;
        }

        #region Initialize / Setting
        public void Initializer(MapPresenterInitData initData)
        {
            Debug.Assert(initData != null || initData != default, $"MapCore의 SetInitData 함수 실행 중 initData의 값이 없습니다.");

            SetInitData(initData);
            Release();

            RegionViewSettings();
            RoomsViewSettings();

            regionDesctionUIState = default;

            SubscribeEvents();
        }

        private void SetInitData(MapPresenterInitData initData)
        {
            mapInfoSO = initData.MapInfoSO;
            currentGlobalZoom = initData.CurrentGlobalZoom;
            zoomSpeed = initData.ZoomSpeed;
            zoomMinRegion = initData.ZoomMinRegion;
            zoomThreshold = initData.ZoomThreshold;
            zoomMaxRoom = initData.ZoomMaxRoom;
        }

        private void RegionViewSettings()
        {
            Debug.Assert(mapInfoSO != null, $"mapInfoSO가 없습니다. MapUIManager에서 mapInfoSO를 넣으십시오.");

            var dict = model.GetAllRegionDict();
            Debug.Assert(dict != null, $"model에서 가져온 구역Dict가 Null입니다.");

            List<RegionImageInitData> initDataList = new();
            foreach (var kvp in dict)
            {
                RegionImageInitData data = new(kvp.Key, kvp.Value);
                initDataList.Add(data);
            }

            int mapId = model.GetMapId();
            regionView.Initialize(mapId, initDataList);

            PushState();
        }

        private void RoomsViewSettings()
        {
            var roomDatas = model.GetAllRoomDatas();
            var initDataList = roomDatas.Select(r => new RoomIconInitData(r.GridPos, Vector2.zero, r.RoomType)).ToList();

            roomsView.Initialize(initDataList);
            roomStatesDict.Clear();
            var spriteDict = model.GetRoomSpriteDictForMapView();

            foreach (var room in roomDatas)
            {
                spriteDict.TryGetValue(room.GridPos, out var uiData);

                var state = new RoomIconState(
                    Type: room.RoomType,
                    ColorState: RoomIconColorTypeEnum.Deactive,
                    CanInteractable: false,
                    MainSprite: uiData?.Sprite,
                    SubSprite: uiData?.SubSprite
                );

                if (room.RoomType != RoomType.NormalRoom)
                {
                    state = state with { ColorState = RoomIconColorTypeEnum.Special };
                }
                if (room.RoomType == RoomType.NormalRoom)
                {
                    state = state with { CanInteractable = true };
                }

                roomStatesDict[room.GridPos] = state;
            }

            roomMapState = new RoomMapState
            {
                IsPlayerVisible = false,
                PlayerPos = Vector2Int.zero,
                SecurityLevel = 1
            };

            PushState();
            roomsView.UpdateAllRooms(roomStatesDict);
            roomsView.FadeOut(); // IFadeInOutable에 해당 메서드가 구현되어 있다고 가정
        }

        private void SubscribeEvents()
        {
            Bus<StartSelectStartRoomEvent>.OnEvent += HandleStartSelectStartRoomEvent;
            Bus<EndSelectStartRoomEvent>.OnEvent += HandleEndSelectStartRoomEvent;
            Bus<StartAddRoomEvent>.OnEvent += HandleStartAddRoomEvent;
            Bus<EndAddRoomEvent>.OnEvent += HandleEndAddRoomEvent;
            Bus<EnterRoomEvent>.OnEvent += HandleEnterRoomEvent;
            Bus<BossRoomEvent>.OnEvent += HandleBossRoomEvent;
            Bus<InfoRoomEvent>.OnEvent += ShowTextHandle;
            Bus<SecurityLevelUpgradeEvent>.OnEvent += HandleSetSecurityLevelTextEvent;

            regionView.OnSelectRegion += HandleSelectRegion;

            regionView.OnChangeView += HandleChangeView;
            roomsView.OnChangeView += HandleChangeView;
            roomsView.OnRegionZoomRequested += HandleOnRegionZoomRequested;

            Bus<MapViewDragEvent>.OnEvent += HandleMapUIPointerDragEvent;
            Bus<MapViewScrollEvent>.OnEvent += HandleMapViewScrollEvent;
            Bus<MapViewResetEvent>.OnEvent += HandleMapViewResetEvent;
            Bus<MapViewFocusEvent>.OnEvent += HandleMapViewFocusEvent;
        }

        private void UnsubscribeEvents()
        {
            Bus<StartSelectStartRoomEvent>.OnEvent -= HandleStartSelectStartRoomEvent;
            Bus<EndSelectStartRoomEvent>.OnEvent -= HandleEndSelectStartRoomEvent;
            Bus<StartAddRoomEvent>.OnEvent -= HandleStartAddRoomEvent;
            Bus<EndAddRoomEvent>.OnEvent -= HandleEndAddRoomEvent;
            Bus<EnterRoomEvent>.OnEvent -= HandleEnterRoomEvent;
            Bus<BossRoomEvent>.OnEvent -= HandleBossRoomEvent;
            Bus<InfoRoomEvent>.OnEvent -= ShowTextHandle;
            Bus<SecurityLevelUpgradeEvent>.OnEvent -= HandleSetSecurityLevelTextEvent;

            regionView.OnSelectRegion -= HandleSelectRegion;
            regionView.OnChangeView -= HandleChangeView;
            roomsView.OnChangeView -= HandleChangeView;
            roomsView.OnRegionZoomRequested -= HandleOnRegionZoomRequested;

            Bus<MapViewDragEvent>.OnEvent -= HandleMapUIPointerDragEvent;
            Bus<MapViewScrollEvent>.OnEvent -= HandleMapViewScrollEvent;
            Bus<MapViewResetEvent>.OnEvent -= HandleMapViewResetEvent;
            Bus<MapViewFocusEvent>.OnEvent -= HandleMapViewFocusEvent;
        }

        private void HandleOnRegionZoomRequested(float targetScale, Vector2 targetPos)
        {
            cameraController.ZoomAndFocus(targetScale, targetPos);
        }
        #endregion

        #region View Switching & Region Logic
        private void HandleChangeView()
        {
            isRegionView = !isRegionView;

            if (!isRegionView)
            {
                ChangeToRoomMapView();
            }
            else
            {
                ChangeToRegionMapView();
            }
        }

        private void ChangeToRegionMapView()
        {
            currentGlobalZoom = zoomThreshold;

            roomsView.DisableView();
            regionView.EnableView();

            PushState();
        }

        private void ChangeToRoomMapView()
        {
            currentGlobalZoom = zoomThreshold + 0.1f;

            regionView.DisableView();
            roomsView.EnableView();

            var minRoomPos = new Vector2Int(int.MaxValue, int.MaxValue);
            var maxRoomPos = new Vector2Int(int.MinValue, int.MinValue);
            bool hasRoom = false;

            var list = model.GetAllRoomDatas();
            foreach (var room in list)
            {
                if (room.RoomType == RoomType.BossRoom) continue;

                if (room.RegionType == curSelectedRegionType)
                {
                    if (!hasRoom)
                    {
                        minRoomPos = room.GridPos;
                        maxRoomPos = room.GridPos;
                    }
                    else
                    {
                        if (room.GridPos.x < minRoomPos.x || (room.GridPos.x == minRoomPos.x && room.GridPos.y < minRoomPos.y))
                            minRoomPos = room.GridPos;

                        if (room.GridPos.x > maxRoomPos.x || (room.GridPos.x == maxRoomPos.x && room.GridPos.y > maxRoomPos.y))
                            maxRoomPos = room.GridPos;
                    }
                    hasRoom = true;
                }
            }

            if (!hasRoom)
            {
                minRoomPos = Vector2Int.zero;
                maxRoomPos = Vector2Int.zero;
            }

            roomMapState = roomMapState with
            {
                RoomMapRegionData = roomMapState.RoomMapRegionData with
                {
                    MinRoomPos = minRoomPos,
                    MaxRoomPos = maxRoomPos
                }
            };
            PushState();
        }

        private void HandleSelectRegion(RegionType region)
        {
            curSelectedRegionType = region;

            if (region != RegionType.None && region != RegionType.Last)
            {
                var regionInfo = mapInfoSO.GetRegionInfo(region).RegionInfoSO;
                Debug.Assert(regionInfo != null, $"{region}에 대한 RegionInfoSO의 값이 존재하지 않습니다.");

                // 유효한 지역일 경우: 모든 정보를 포함하여 한 번에 생성
                regionDesctionUIState = new()
                {
                    RegionType = region,
                    // 현재 구역따라 이름이나 정보가 없어서 넣지 않음. 나중에 추가 예정
                    RegionCharacterInfo = regionInfo.CharacterTypeInfo,
                    BossIconInfo = regionInfo.BossIconInfo,
                    RegionModuleInfos = regionInfo.ModuleIconInfos
                };
            }
            else
            {
                regionDesctionUIState = new()
                {
                    RegionType = region
                };
            }

            regionView.UpdateRegionDesctionUIState(regionDesctionUIState);
        }
        #endregion

        #region Event Handlers
        private void HandleMapUIPointerDragEvent(MapViewDragEvent evt)
        {
            cameraController.TranslateView(evt.Force);
        }
        private void HandleMapViewScrollEvent(MapViewScrollEvent evt)
        {
            MouseWheelCheck(evt.ScrollY);
        }
        private void HandleMapViewResetEvent(MapViewResetEvent evt)
        {
            if (!isRegionView && !isLockedInRoomView)
            {
                return;
            }

            cameraController.ResetToDefaultView();
        }
        private void HandleMapViewFocusEvent(MapViewFocusEvent evt)
        {
            if (roomsView.IsShow && roomMapState.IsPlayerVisible)
            {
                Vector2 targetUIPos = roomsView.GetRoomUIPosition(roomMapState.PlayerPos);
                cameraController.FocusToPosition(targetUIPos);
            }
        }

        private void ShowTextHandle(InfoRoomEvent evt)
        {
            roomMapState = roomMapState with { RoomInfo = evt.RoomInfo };
            PushState();
        }

        private void HandleSetSecurityLevelTextEvent(SecurityLevelUpgradeEvent evt)
        {
            roomMapState = roomMapState with { SecurityLevel = evt.SecurityLevel };
            PushState();
        }

        private void HandleBossRoomEvent(BossRoomEvent evt)
        {
            isBoss = true;
            roomsView.DisableView();
        }

        private void HandleStartSelectStartRoomEvent(StartSelectStartRoomEvent evt)
        {
            isLockedInRoomView = false;
            var roomDataList = model.GetAllRoomDatas();
            var mimimapDict = model.GetRoomIconDictForMimimapView();

            foreach (var room in roomDataList)
            {
                RoomIconColorTypeEnum colorState = room switch
                {
                    { IsClosed: true } => RoomIconColorTypeEnum.Deactive,
                    { RoomType: not RoomType.NormalRoom } => RoomIconColorTypeEnum.Special,
                    _ => RoomIconColorTypeEnum.Active
                };

                bool canInteract = (!room.IsClosed && room.RoomType == RoomType.NormalRoom);
                mimimapDict.TryGetValue(room.GridPos, out var uiData);

                roomStatesDict[room.GridPos] = new RoomIconState(
                    Type: room.RoomType,
                    ColorState: colorState,
                    CanInteractable: canInteract,
                    MainSprite: uiData?.Sprite,
                    SubSprite: uiData?.SubSprite
                );
            }

            viewport.Show();
            roomsView.DisableView();
            regionView.EnableView();

            roomsView.UpdateAllRooms(roomStatesDict);
            roomsView.OnSelectRoom -= HandleSelectStartRoom;
            roomsView.OnSelectRoom += HandleSelectStartRoom;
        }

        private void HandleSelectStartRoom(Vector2Int selectPos)
        {
            roomsView.OnSelectRoom -= HandleSelectStartRoom;
            roomsView.DisableView();
            Bus<EndSelectStartRoomEvent>.OnEvent?.Invoke(new(selectPos));
        }

        private void HandleEndSelectStartRoomEvent(EndSelectStartRoomEvent evt)
        {
            isLockedInRoomView = true;

            var roomDatas = model.GetAllRoomDatas();

            foreach (var room in roomDatas)
            {
                var oldState = roomStatesDict[room.GridPos];
                roomStatesDict[room.GridPos] = oldState with { ColorState = RoomIconColorTypeEnum.Active };
            }
            roomsView.UpdateAllRooms(roomStatesDict);

            roomMapState = roomMapState with
            {
                IsPlayerVisible = true,
                PlayerPos = evt.Pos,
            };

            viewport.Hide();

            SetRoomFindedState(evt.Pos);
            PushState();
        }

        private void HandleEnterRoomEvent(EnterRoomEvent evt)
        {
            roomMapState = roomMapState with { PlayerPos = evt.RoomDef.GridPos };
            PushState();
        }

        private void HandleStartAddRoomEvent(StartAddRoomEvent evt)
        {
            roomsView.SetMappingTextVisible(true);
            PushState();
        }

        private void HandleEndAddRoomEvent(EndAddRoomEvent evt)
        {
            roomsView.SetMappingTextVisible(false);
            roomsView.DisableView();
            SetRoomFindedState(evt.NewRoomDef.GridPos);
            PushState();
        }
        #endregion

        #region Utilities
        private void SetRoomFindedState(Vector2Int pos)
        {
            if (roomStatesDict.TryGetValue(pos, out var oldRoomState))
            {
                var newState = oldRoomState with
                {
                    ColorState = RoomIconColorTypeEnum.Active,
                    CanInteractable = true
                };

                roomStatesDict[pos] = newState;
                roomsView.UpdateSingleRoom(pos, newState);
            }
        }

        private void PushState()
        {
            roomsView.UpdateState(roomMapState);
        }

        public void ShowMapUI(bool mapShow)
        {
            if (isBoss) return;

            if (mapShow)
            {
                viewport.Show();
                roomsView.EnableView();
                cameraController.ResetToDefaultView();
            }
            else
            {
                viewport.Hide();
                roomsView.DisableView();
                regionView.DisableView();
            }
        }

        public void MouseWheelCheck(float normalizedScrollValue)
        {
            if (Mathf.Approximately(normalizedScrollValue, 0f)) return;

            float prevZoom = currentGlobalZoom;
            currentGlobalZoom += (normalizedScrollValue * zoomSpeed);

            // 줌 스케일을 현재 허용된 최소~최대치 안에서만 놀도록 제한
            currentGlobalZoom = Mathf.Clamp(currentGlobalZoom, zoomMinRegion, zoomMaxRoom);

            if (!isLockedInRoomView)
            {
                bool wasRegion = prevZoom <= zoomThreshold;
                bool isRegion = currentGlobalZoom <= zoomThreshold;

                if (wasRegion && !isRegion)
                {
                    HandleChangeView();
                    return;
                }
                else if (!wasRegion && isRegion)
                {
                    HandleChangeView();
                    return;
                }
            }

            // 부드럽게 줌 적용 (기존 코드)
            cameraController.SetZoomScale(currentGlobalZoom);
        }

        public void Release()
        {
            UnsubscribeEvents();
            roomsView.OnSelectRoom -= HandleSelectStartRoom;
        }
        #endregion
    }
}
