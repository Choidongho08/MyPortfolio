using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using Assets.Work.CDH.Code.UIs.Maps;
using Assets.Work.CDH.Code.UIs.Maps.SecurityLevels;
using Core.EventBus;
using GondrLib.Dependencies;
using GondrLib.ObjectPool.RunTime;
using Public.Core.Events;
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Work.CDH.Code.Maps
{
    [Flags]
    public enum RoomType
    {
        None = 0,
        StartRoom = 1 << 0,
        NormalRoom = 1 << 1,
        ModuleRoom = 1 << 2,
        ShopRoom = 1 << 3,
        SpecialRoom = 1 << 4,
        BossRoom = 1 << 5,
        BuddyRoom = 1 << 6,
    }

    [Serializable]
    public enum RegionType
    {
        None = 0,
        Group_1 = 1,
        Group_2 = 2,
        Group_3 = 3,
        Group_4 = 4,
        Group_5 = 5,
        Last
    }

    public struct RoomStaticObjectDatas
    {
        public List<int> BreakingObstacles;

        public RoomStaticObjectDatas(int _ = 0)
        {
            BreakingObstacles = new();
        }
    }

    public struct RoomDynamicObjectDatas
    {
        public List<GameObject> Objects;

        public RoomDynamicObjectDatas(int _ = 0)
        {
            Objects = new();
        }
    }

    public interface IRoomDef
    {
        public bool IsClosed { get; set; }

        public bool IsFirstEnter { get; set; }
        public bool IsDiscovered { get; set; }
        public Dictionary<EnterDirection, bool> DoorsEnterableDict { get; set; }

        public RoomType RoomType { get; set; }
        public RegionType RegionType { get; set; }
        public int PrefabIndex { get; set; }

        public Vector2Int GridPos { get; set; }
        public Vector3 WorldPos { get; set; }
        public List<Vector2> Vertices { get; set; }

        public RoomDynamicObjectDatas DynamicObjDatas { get; set; }
        public RoomStaticObjectDatas StaticObjDatas { get; set; }

        public Vector2 RoomSize { get; set; }

        public float SecurityLevelIncreaseValue { get; set; }
    }
    public struct DefaultRoomDef : IRoomDef
    {
        public bool IsClosed { get; set; }

        public bool IsFirstEnter { get; set; }
        public bool IsDiscovered { get; set; }
        public Dictionary<EnterDirection, bool> DoorsEnterableDict { get; set; }

        public RoomType RoomType { get; set; }
        public RegionType RegionType { get; set; }
        public int PrefabIndex { get; set; }

        public Vector2Int GridPos { get; set; }
        public Vector3 WorldPos { get; set; }
        public List<Vector2> Vertices { get; set; }

        public Vector2 RoomSize { get; set; }
        public RoomDynamicObjectDatas DynamicObjDatas { get; set; }
        public RoomStaticObjectDatas StaticObjDatas { get; set; }


        public float SecurityLevelIncreaseValue { get; set; }

        public DefaultRoomDef(Vector2Int pos, RoomType type, RegionType groupType)
        {
            GridPos = pos;
            RoomType = type;
            RegionType = groupType;
            IsDiscovered = false;
            PrefabIndex = -1;
            IsFirstEnter = true;
            IsClosed = false;
            DoorsEnterableDict = new();
            RoomSize = new();
            WorldPos = new();
            Vertices = new();

            DynamicObjDatas = new(0);
            StaticObjDatas = new(0);

            SecurityLevelIncreaseValue = 1.0f;
        }
    }
    public struct ModuleRoomDef : IRoomDef
    {
        public Category Category { get; set; }

        public bool IsClosed { get; set; }

        public bool IsFirstEnter { get; set; }
        public bool IsDiscovered { get; set; }
        public Dictionary<EnterDirection, bool> DoorsEnterableDict { get; set; }

        public RoomType RoomType { get; set; }
        public RegionType RegionType { get; set; }
        public int PrefabIndex { get; set; }

        public Vector2Int GridPos { get; set; }
        public Vector3 WorldPos { get; set; }
        public List<Vector2> Vertices { get; set; }

        public ModuleProbability[] ModuleProbabilities { get; set; }
        public Vector2 RoomSize { get; set; }
        public RoomDynamicObjectDatas DynamicObjDatas { get; set; }
        public RoomStaticObjectDatas StaticObjDatas { get; set; }


        public float SecurityLevelIncreaseValue { get; set; }

        public ModuleRoomDef(Vector2Int pos, RoomType type, RegionType groupType, Category catrgory)
        {
            GridPos = pos;
            RoomType = type;
            RegionType = groupType;
            Category = catrgory;
            ModuleProbabilities = new ModuleProbability[0];
            PrefabIndex = -1;
            IsDiscovered = false;
            IsFirstEnter = true;
            IsClosed = false;
            DoorsEnterableDict = new();
            RoomSize = new();
            WorldPos = new();
            Vertices = new();

            DynamicObjDatas = new(0);
            StaticObjDatas = new(0);

            SecurityLevelIncreaseValue = 1.0f;
        }
    }

    public struct EventRoomDef : IRoomDef
    {
        public int EventObjIndex { get; set; }
        public ISpecialRoomCompo MyEvent { get; set; }

        public bool IsClosed { get; set; }

        public bool IsFirstEnter { get; set; }
        public bool IsDiscovered { get; set; }
        public Dictionary<EnterDirection, bool> DoorsEnterableDict { get; set; }
        public RoomType RoomType { get; set; }
        public RegionType RegionType { get; set; }
        public int PrefabIndex { get; set; }
        public Vector2Int GridPos { get; set; }
        public Vector3 WorldPos { get; set; }
        public List<Vector2> Vertices { get; set; }

        public Vector2 RoomSize { get; set; }
        public RoomDynamicObjectDatas DynamicObjDatas { get; set; }
        public RoomStaticObjectDatas StaticObjDatas { get; set; }

        public float SecurityLevelIncreaseValue { get; set; }


        public EventRoomDef(Vector2Int pos, RoomType type, RegionType groupType)
        {
            GridPos = pos;
            RoomType = type;
            RegionType = groupType;
            PrefabIndex = -1;
            IsDiscovered = false;
            IsFirstEnter = true;
            IsClosed = false;
            DoorsEnterableDict = new();
            EventObjIndex = -1;
            MyEvent = null;
            RoomSize = new();
            WorldPos = new();
            Vertices = new();

            DynamicObjDatas = new(0);
            StaticObjDatas = new(0);

            SecurityLevelIncreaseValue = 1.0f;
        }
    }

    public struct DoorDef : IEquatable<DoorDef>
    {
        public readonly Vector2Int AGridPos;
        public readonly Vector2Int BGridPos;

        public DoorDef(Vector2Int a, Vector2Int b)
        {
            if (IsLessOrEqual(a, b))
            {
                AGridPos = a; BGridPos = b;
            }
            else
            {
                AGridPos = b; BGridPos = a;
            }
        }

        private static bool IsLessOrEqual(Vector2Int p1, Vector2Int p2)
            => p1.y < p2.y || (p1.y == p2.y && p1.x <= p2.x);

        public bool Equals(DoorDef other)
            => AGridPos.Equals(other.AGridPos) && BGridPos.Equals(other.BGridPos);

        public override bool Equals(object obj)
            => obj is DoorDef other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(AGridPos, BGridPos);
        }
    }

    public readonly record struct SecurityModelState(int Level, int Exp);
    public delegate void SecurityStateChangedHandler(in SecurityModelState newState);

    public interface IMapDataProvider
    {
        // event SecurityStateChangedHandler

        MapViewRoomSpritesSO SpritesSO { get; set; }
        Vector2Int PlayerRoomPos { get; }
        List<DoorDef> EnterableDoors { get; }
        SecurityLevelDataDictSO SecurityLevelDictSO { get; }

        void Initializer();
        void SetRoomDatas(List<IRoomDef> roomDatas);
        void SetDoorDatas(List<DoorDef> doorDatas);
        void AddCurRoom(Vector2Int roomPos);
        void SetPlayerRoom(Vector2Int pos);
        void SetSecurityLevelDictSO(SecurityLevelDataDictSO so);
        void Clear();

        // Gets
        // For Rooms
        List<IRoomDef> GetAllRoomDatas();
        Dictionary<Vector2Int, IRoomDef> GetAllRoomDataByPos();

        // For Doors
        List<DoorDef> GetAllDoorDatas();
        Dictionary<Vector2Int, List<DoorDef>> GetAllDoorDataByRoomPos();

        // For UI
        Dictionary<Vector2Int, IRoomUIData> GetRoomSpriteDictForMapView();
        List<IRoomUIData> GetRoomSpriteListForMapView();
        Dictionary<Vector2Int, IRoomUIData> GetRoomIconDictForMimimapView();
        List<IRoomUIData> GetRoomIconListForMimimapView();

        IRoomDef GetRoomDefByPos(Vector2Int pos);

        List<Vector2Int> GetRoomPosesByType(RoomType roomType);

        IRoomDef GetPlayerRoomDef();
        void ChangeRoomData(IRoomDef roomData);
        string GetRegionName(RegionType type);

        Dictionary<RegionType, string> GetAllRegionDict();

        int GetMapId();

    }

    [DefaultExecutionOrder(1)]
    public class MapManager : MonoSingleton<MapManager>
    {
        [SerializeField] private MapViewRoomSpritesSO spritesSO;

        [Header("Map Settings")]
        [SerializeField] private MapSpawner spawner;

        [Header("MapUIManager")]
        [SerializeField] private MapUIManager mapUIManager;

        [Header("NavMesh Settings")]
        [SerializeField] private NavMeshSurface navMesh;

        [Header("Room Settings")]
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private LayerMask invisibleLayer;
        [SerializeField] private LayerMask defaultLayer;

        [Header("Fade Setting")]
        [SerializeField] private float duration;

        [Inject] private PoolManagerMono poolManager;
        [Inject] private CharacterManager characterManager;

        private MapDataProvider model;
        private IRoom currentRoom;
        private IDoorManager doorManager;
        private int curChapter;

        private bool isBoss; // 보스 방 스폰 처리를 위한 플래그

        private bool canMapping;

        protected override void Awake()
        {
            base.Awake();

            curChapter = -1;

            model = new MapDataProvider();
            model.SpritesSO = spritesSO;

            roomManager.Initialize(model);


            var roomList = spawner.PopAllRoomPrefab();
            navMesh.BuildNavMesh();
            foreach (var room in roomList)
            {
                room.PushRoom();
            }
            roomList.Clear();

            doorManager = spawner.PopDoorObj();


            Initialize();
            SubscribeEvents();

            mapUIManager.Initialize(model);

            // 시작 방 선택 이벤트 발생
            Bus<StageFirstStartUIEvents>.OnEvent?.Invoke(new());
        }

        private void SubscribeEvents()
        {
            doorManager.OnNewRoom += HandleNewRoom;

            Bus<DoorEnterTriggerEvent>.OnEvent += HandleDoorEnterTriggerEvent;
            Bus<RoomClearEvent>.OnEvent += HandleRoomClearEvent;
            Bus<StartAddRoomEvent>.OnEvent += HandleStartAddRoomEvent;
            Bus<EndAddRoomEvent>.OnEvent += HandleEndAddRoomEvent;
            Bus<EndSelectStartRoomEvent>.OnEvent += HandleEndSelectStartRoomEvent;
            Bus<BossRoomEvent>.OnEvent += HandleBossRoomEvent;
            Bus<BossDeadEvent>.OnEvent += HandleBossDeadEvent;
            Bus<ChangeRoomDefEvent>.OnEvent += HandleChangeRoomDefEvent;
            Bus<StageViewEndEvents>.OnEvent += HandleStageViewEndEvents;
            Bus<GetRoomDefsEvent>.OnEvent += HandleGetRoomDefsEvent;
            Bus<AllRoomCloseEvent>.OnEvent += HandleAllRoomCloseEvent;
        }

        private void UnsubscribeEvents()
        {
            doorManager.OnNewRoom -= HandleNewRoom;

            Bus<DoorEnterTriggerEvent>.OnEvent -= HandleDoorEnterTriggerEvent;
            Bus<RoomClearEvent>.OnEvent -= HandleRoomClearEvent;
            Bus<StartAddRoomEvent>.OnEvent -= HandleStartAddRoomEvent;
            Bus<EndAddRoomEvent>.OnEvent -= HandleEndAddRoomEvent;
            Bus<EndSelectStartRoomEvent>.OnEvent -= HandleEndSelectStartRoomEvent;
            Bus<BossRoomEvent>.OnEvent -= HandleBossRoomEvent;
            Bus<BossDeadEvent>.OnEvent -= HandleBossDeadEvent;
            Bus<ChangeRoomDefEvent>.OnEvent -= HandleChangeRoomDefEvent;
            Bus<GetRoomDefsEvent>.OnEvent -= HandleGetRoomDefsEvent;
            Bus<StageViewEndEvents>.OnEvent -= HandleStageViewEndEvents;
            Bus<AllRoomCloseEvent>.OnEvent -= HandleAllRoomCloseEvent;
        }

        private void HandleAllRoomCloseEvent(AllRoomCloseEvent evt)
        {
            AllRoomClose();
        }

        private void HandleGetRoomDefsEvent(GetRoomDefsEvent evt)
        {
            evt.RoomDefsHandler?.Invoke(model.GetAllRoomDatas());
        }

        private void HandleStartAddRoomEvent(StartAddRoomEvent evt)
        {
            canMapping = true;
            DoorSetting(model.GetPlayerRoomDef().GridPos);
        }

        private void HandleStageViewEndEvents(StageViewEndEvents evt)
        {
            Bus<StageViewEndEvents>.OnEvent -= HandleStageViewEndEvents;
            BusManager.Instance.SendEvent<StartSelectStartRoomEvent>();
        }

        private void HandleChangeRoomDefEvent(ChangeRoomDefEvent evt)
        {
            model.ChangeRoomData(evt.RoomDef);
        }

        private void HandleNewRoom(Vector2Int pos)
        {
            var roomDef = model.GetRoomDefByPos(pos);
            Bus<EndAddRoomEvent>.OnEvent?.Invoke(new(roomDef));
            DoorSetting(model.GetPlayerRoomDef().GridPos);
        }

        private void Initialize()
        {
            Debug.Assert(spawner != null, "CRITICAL: Spawner가 null입니다.");

            curChapter++;

            canMapping = true;
            isBoss = false;

            model.Clear();
            model.Initializer();

            int mapId = spawner.TableManagerRandomMapLoad();
            model.SetMapId(mapId);
            spawner.CreateDefaultRoomData();
            spawner.CreateRandomRoomData();

            SetRandomGroupName();

            var roomDatas = spawner.GetRoomDatas();
            model.SetRoomDatas(roomDatas);

            List<DoorDef> doorDatas = spawner.CreateDoorsData(roomDatas);
            model.SetDoorDatas(doorDatas);

            // 삭제해야하는 이벤트
            // Bus<MapBossCountEvent>.OnEvent?.Invoke(new(curBossCount, curRoomCount));
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void HandleBossDeadEvent(BossDeadEvent evt)
        {
            if (!isBoss)
            {
                BusManager.Instance.SendEvent<StageNextUIEvents>();
                BusManager.Instance.SendEvent(new RoomClearEvent(currentRoom));
                MapRegionClose(model.GetPlayerRoomDef().RegionType);
                // 여기서 맵 UI켜지고 다시 시작할 방 선택;
                BusManager.Instance.SendEvent<StartSelectStartRoomEvent>();
                BusManager.Instance.SendEvent(new MapViewSetShowEvent(true));
                return;
            }

            // 보스가 죽으면 게임이 재시작된다고 가정 -> Initialize 호출로 맵 재생성
            currentRoom.PushRoom();
            Initialize();
            Bus<StageViewEndEvents>.OnEvent += HandleStageViewEndEvents;
            BusManager.Instance.SendEvent<StageFirstStartUIEvents>();
        }

        private void HandleBossRoomEvent(BossRoomEvent evt)
        {
            isBoss = true;
        }

        private void HandleDoorEnterTriggerEvent(DoorEnterTriggerEvent evt)
        {
            var newPos = evt.GridPos;

            var roomDef = model.GetAllRoomDataByPos().GetValueOrDefault(newPos);
            Bus<FadeInEvent>.OnEvent?.Invoke(new(duration, () => HandleFadeInComplete(roomDef, evt.EnterDir)));
        }

        private void HandleFadeInComplete(IRoomDef newRoomDef, EnterDirection enterDir)
        {
            // [디버깅용] 단계 추적 변수
            int debugStep = 0;

            // [변경됨] 로그를 파란색(Blue) + 굵게(Bold) 표시하여 가독성 확보
            void LogStep(string msg) => Debug.Log($"<color=blue><b>[HandleFadeInComplete] STEP {++debugStep}: {msg}</b></color>");

            // 1. 입력 데이터 검증 (가장 강력한 Assert)
            Debug.Assert(newRoomDef != null, "CRITICAL: newRoomDef가 null입니다.");
            Debug.Assert(model != null, "CRITICAL: model이 null입니다.");
            Debug.Assert(currentRoom != null, "CRITICAL: currentRoom(이전 방)이 null입니다.");
            Debug.Assert(Bus<RoomExitEvent>.OnEvent != null, "WARNING: RoomExitEvent 리스너가 아무도 없습니다 (null).");

            LogStep("메소드 시작 및 기본 검증 완료");

            // 2. 방 퇴장 이벤트
            try
            {
                Bus<RoomExitEvent>.OnEvent?.Invoke(new(currentRoom, model.GetPlayerRoomDef()));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CRITICAL STOP] RoomExitEvent 실행 중 에러 발생: {e.Message}");
                throw; // 여기서 멈춤
            }

            currentRoom?.PushRoom();
            LogStep("이전 방 퇴장 처리 완료");

            currentRoom = spawner.PopRoomObj(newRoomDef);
            Debug.Assert(currentRoom != null, "CRITICAL: Spawner에서 가져온 Normal Room이 null입니다.");
            LogStep("방 생성 완료");

            if (currentRoom is BuddyRoom buddyRoom)
            {
                if (newRoomDef.IsFirstEnter)
                    buddyRoom.CanInit = true;

                Debug.Assert(characterManager != null, "CRITICAL: characterManager가 null입니다.");
                Debug.Assert(poolManager != null, "CRITICAL: poolManager가 null입니다.");

                buddyRoom.Initialize(characterManager, poolManager);
                Bus<BuddyRoomEvent>.OnEvent?.Invoke(new BuddyRoomEvent(buddyRoom));
            }

            // 5. 몬스터 룸 변환 체크
            IMonsterRoom monsterRoom = null;
            if (newRoomDef.IsFirstEnter)
            {
                currentRoom.FirstEnterRoom();
                model.ChangeRoomData(newRoomDef);

                if (currentRoom is IMonsterRoom room)
                    monsterRoom = room;
            }
            else
            {
                currentRoom.EnterRoom();
            }
            LogStep("방 진입(Enter/FirstEnter) 처리 완료");

            // 6. 동적 오브젝트 활성화 (foreach 루프 검증)
            Debug.Assert(newRoomDef.DynamicObjDatas.Objects != null, "CRITICAL: DynamicObjDatas.Objects 리스트가 null입니다.");

            int objCount = 0;
            foreach (var obj in newRoomDef.DynamicObjDatas.Objects)
            {
                Debug.Assert(obj != null, $"CRITICAL: Dynamic Object 리스트의 {objCount}번째 요소가 null입니다.");
                obj.SetActive(true);
                objCount++;
            }
            LogStep($"동적 오브젝트 {objCount}개 활성화 완료");

            Debug.Assert(currentRoom.EnterPosDict != null, "CRITICAL: currentRoom.EnterPosDict가 null입니다.");
            Debug.Assert(currentRoom.EnterPosDict.ContainsKey(enterDir), $"CRITICAL: EnterPosDict에 키 '{enterDir}'가 없습니다.");

            Vector3 playerPos = currentRoom.EnterPosDict[enterDir];
            model.SetPlayerRoom(newRoomDef.GridPos);

            LogStep("플레이어 위치 계산 완료");

            if (newRoomDef.IsFirstEnter)
            {
                Bus<FirstEnterRoomEvent>.OnEvent?.Invoke(new(currentRoom, newRoomDef));
            }
            newRoomDef.IsFirstEnter = false;
            Bus<EnterRoomEvent>.OnEvent?.Invoke(new(newRoomDef));
            Bus<RoomEnterEvent>.OnEvent?.Invoke(new(currentRoom, newRoomDef));

            // 8. 몬스터 스폰 (위험 구간)
            if (monsterRoom != null)
            {
                monsterRoom.SpawnMonsters(); // 내부에서 터질 가능성 높음
            }

            // 9. 문 설정
            DoorSetting(newRoomDef.GridPos);

            // 10. 마무리
            Bus<PlayerSetPosEvent>.OnEvent?.Invoke(new(playerPos));
            Bus<FadeOutEvent>.OnEvent?.Invoke(new(duration));

            LogStep("!!!! 함수 정상 종료 (성공) !!!!");
        }

        private void DoorSetting(Vector2Int gridPos)
        {
            doorManager.Transform.position = Vector3.zero;
            doorManager.SetRoomGridPos(gridPos); // 현재 방 위치

            doorManager.DoorsInitlaizer(); // 문 초기화 false및 아이콘

            doorManager.DoorSetting(currentRoom.DoorPosDict, model.GetRoomDefByPos(gridPos)); // 문 위치와 Enterable한지

            if (canMapping)
                PossibleDoorSetting(gridPos); // 문 잠금해제 가능한가

            // 보스 방은 이걸로 해야지
            // doorManager.SetMiddleBossSetting(model.GetAllDoorDataByRoomPos().GetValueOrDefault(gridPos), model, gridPos);
            ImpossibleDoorSetting(gridPos); // 불가능한 곳
        }

        private void ImpossibleDoorSetting(Vector2Int gridPos)
        {
            List<EnterDirection> doors = new()
            {
                EnterDirection.Up,
                EnterDirection.Down,
                EnterDirection.Left,
                EnterDirection.Right
            };

            foreach (var room in model.EnterableRooms)
            {
                Vector2Int dir = room.GridPos - gridPos;

                if (dir == Vector2Int.up)
                    doors.Remove(EnterDirection.Up);
                else if (dir == Vector2Int.down)
                    doors.Remove(EnterDirection.Down);
                else if (dir == Vector2Int.left)
                    doors.Remove(EnterDirection.Left);
                else if (dir == Vector2Int.right)
                    doors.Remove(EnterDirection.Right);
            }

            doorManager.ImpossibleDoorsSetting(doors);
        }

        private void PossibleDoorSetting(Vector2Int gridPos)
        {
            List<EnterDirection> doors = new();

            foreach (var room in model.EnterableRooms)
            {
                Vector2Int dir = room.GridPos - gridPos;

                if (dir == Vector2Int.up)
                    doors.Add(EnterDirection.Up);
                else if (dir == Vector2Int.down)
                    doors.Add(EnterDirection.Down);
                else if (dir == Vector2Int.left)
                    doors.Add(EnterDirection.Left);
                else if (dir == Vector2Int.right)
                    doors.Add(EnterDirection.Right);
            }
            doorManager.PossibleDoorsSetting(doors);
        }

        private void HandleRoomClearEvent(RoomClearEvent evt)
        {
            Bus<StartAddRoomEvent>.OnEvent?.Invoke(new());
        }

        private void HandleEndAddRoomEvent(EndAddRoomEvent evt)
        {
            canMapping = false;

            Vector2Int pos = evt.NewRoomDef.GridPos;
            model.AddCurRoom(pos);

            Vector2Int playerPos = model.PlayerRoomPos;
        }

        /// <summary>
        /// 시작 방을 고른 후 실행되는 함수
        /// </summary>
        /// <param name="evt"></param>
        private void HandleEndSelectStartRoomEvent(EndSelectStartRoomEvent evt)
        {
            var startPos = evt.Pos;
            model.SetStartRoom(startPos);

            // 시작 방 생성 후 값 세팅
            var startRoomDef = model.GetStartRoomDef();
            startRoomDef.IsDiscovered = true;
            startRoomDef.IsFirstEnter = false;
            model.ChangeRoomData(startRoomDef);
            currentRoom = spawner.PopRoomObj(startRoomDef);
            DoorSetting(startPos);

            Bus<MapModelSettingEndEvent>.OnEvent?.Invoke(new());

            if (currentRoom is PlayerStartRoom startRoom)
            {
                startRoom.FirstEnterRoom();
                Bus<PlayerSetPosEvent>.OnEvent?.Invoke(new(startRoom.StartPos.position));
            }

            Bus<StartAddRoomEvent>.OnEvent?.Invoke(new());
        }

        private void SetRandomGroupName()
        {
            string[] rawNames = spawner.GetCurMapGroupNames(); // 이름들 가져오기
            List<string> availableNames = new List<string>(rawNames); // 리스트에 복사

            List<RegionType> availableTypes = new List<RegionType>(); // 사용가능한 그룹
            for (int i = (int)RegionType.None + 1; i < (int)RegionType.Last; i++)
            {
                availableTypes.Add((RegionType)i);
            }

            int loopCount = UnityEngine.Mathf.Min(5, availableNames.Count, availableTypes.Count);
            for (int i = 0; i < loopCount; ++i)
            {
                int typeIndex = UnityEngine.Random.Range(0, availableTypes.Count); // 그룹 뽑기
                RegionType targetGroupType = availableTypes[typeIndex];
                availableTypes.RemoveAt(typeIndex); // 뽑은건 리스트에서 삭제

                int nameIndex = UnityEngine.Random.Range(0, availableNames.Count); // 이름 뽑기
                string targetName = availableNames[nameIndex];
                availableNames.RemoveAt(nameIndex); // 뽑은건 리스트에서 삭제

                model.AddGroupName(targetGroupType, targetName);
            }
        }

        /// <summary>
        /// 구역 폐쇄
        /// </summary>
        private void MapRegionClose(RegionType targetGroup)
        {
            var roomList = model.GetAllRoomDatas();
            foreach (var roomDef in roomList)
            {
                if (roomDef.RegionType == targetGroup)
                {
                    roomDef.IsClosed = true;
                    model.SetRoomData(roomDef);
                }
            }
        }

        /// <summary>
        /// 모든 방을 접근 금지로 만듬
        /// </summary>
        private void AllRoomClose()
        {
            var roomList = model.GetAllRoomDatas();
            roomList.ForEach(x => x.IsClosed = true);
            model.SetRoomDatas(roomList);
        }
    }
}