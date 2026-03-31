using GondrLib.Dependencies;
using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Assets.Work.CDH.Code.Table.Table_Map;
using Random = UnityEngine.Random;

namespace Assets.Work.CDH.Code.Maps
{
    [DefaultExecutionOrder(-3)]
    public class MapSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private RoomPrefabsSO prefabSO;
        [SerializeField] private PoolItemSO doorItem;
        [SerializeField] private bool isXAxis = false;
        [SerializeField] private float roomSize;

        // 변수명을 그룹당 개수임을 명확히 하기 위해 PerGroup으로 변경했습니다.
        [Header("Room Counts (Per Group)")]
        [SerializeField] private int passiveRoomPerGroup; // ModuleRoom으로 사용
        [SerializeField] private int specialRoomPerGroup; // SpecialRoom으로 사용
        [SerializeField] private int shopRoomPerGroup;    // ShopRoom으로 사용
        // BuddyRoom은 요구사항에 따라 그룹당 고정 1개로 처리됩니다.

        [Inject] private PoolManagerMono poolManager;

        private List<MapData> mapList;
        private Dictionary<RoomType, List<PoolItemSO>> prefabsByType;
        private float cellX;
        private float cellZ;
        private Dictionary<RoomType, List<Vector3>> roomWorldPosDict = new();
        private Dictionary<Vector2Int, IRoomDef> defaultRoomsByPos;
        private List<Vector2Int> candidates;
        private List<List<string>> currentLoadedMap;
        private string[] curMapGroupNames;

        private void Awake()
        {
            cellX = roomSize;
            cellZ = roomSize;
            CreateRoomPrefab();
        }

        private void CreateRoomPrefab()
        {
            prefabsByType = new();
            foreach (var roomPrefabs in prefabSO.roomPrefabsByType)
            {
                foreach (var roomPrefab in roomPrefabs.Items)
                {
                    if (!prefabsByType.TryGetValue(roomPrefabs.Type, out List<PoolItemSO> list))
                    {
                        prefabsByType[roomPrefabs.Type] = list = new();
                    }
                    list.Add(roomPrefab);
                }
            }

            int totalCount = 0;
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                List<PoolItemSO> itemList = prefabsByType.GetValueOrDefault(type);
                if (itemList == null) continue;

                int size = itemList.Count;
                for (int i = 0; i < size; ++i)
                {
                    Vector3 pos = Vector3.zero;

                    if (isXAxis)
                        pos = new Vector3(totalCount * cellX, 0f, 0f);
                    else
                        pos = new Vector3(0f, 0f, totalCount * cellZ);

                    totalCount++;

                    if (!roomWorldPosDict.TryGetValue(type, out var list))
                        roomWorldPosDict[type] = list = new();

                    list.Add(pos);
                }
            }
        }

        public int TableManagerRandomMapLoad()
        {
            TableManager tableManager = Shared.InitTableMgr();
            mapList = tableManager.Map.GetMaps();
            Debug.Assert(mapList != null && mapList.Count > 0, $"MapList가 로드되지 않았습니다.");

            MapData targetMap = mapList[Random.Range(0, mapList.Count)];
            currentLoadedMap = targetMap.Map;
            curMapGroupNames = targetMap.GroupNames;
            return targetMap.MapId;
        }

        private RegionType GetGroupType(string str)
        {
            if (!int.TryParse(str, out int result))
            {
                Debug.LogError("str이 int형으로 변환이 실패했습니다");
                goto lb_bug_return;
            }
            else if (result >= (int)RegionType.Last)
            {
                Debug.LogError("GroupCount를 벗어났습니다.");
                goto lb_bug_return;
            }

            return (RegionType)result;

        lb_bug_return:
            return RegionType.None;
        }

        public string[] GetCurMapGroupNames()
        {
            return curMapGroupNames;
        }

        public void CreateDefaultRoomData()
        {
            int row = currentLoadedMap.Count;
            int roomTotal = currentLoadedMap.Sum(r => r.Count(val => val != "0"));
            defaultRoomsByPos = new(roomTotal);
            int candidateTotal = currentLoadedMap.Sum(r => r.Count(val => val != "0" && val != "B"));
            candidates = new(candidateTotal);

            int x = 0;
            // 기존의 int y = 0; 삭제

            for (int i = 0; i < row; i++)
            {
                // [해결포인트] 상하반전: 데이터를 읽는 인덱스(i)가 0일 때 가장 높은 Y좌표를 갖도록 역순 할당
                int y = (row - 1) - i;

                int column = currentLoadedMap[i].Count;
                for (int j = 0; j < column; ++j)
                {
                    var str = currentLoadedMap[i][j];
                    x++;
                    if (str == "0") continue;

                    var pos = new Vector2Int(x, y);

                    if (str == "B")
                    {
                        defaultRoomsByPos[pos] = new DefaultRoomDef(pos, RoomType.BossRoom, RegionType.None);
                        continue;
                    }

                    candidates.Add(pos);
                    defaultRoomsByPos[pos] = new DefaultRoomDef(pos, RoomType.NormalRoom, GetGroupType(str));
                }
                x = 0;
                // 기존의 y++; 삭제
            }
        }

        public void CreateRandomRoomData()
        {
            // 1) 그룹별로 후보군(candidates) 분류
            var candidatesByGroup = candidates
                .GroupBy(pos => defaultRoomsByPos[pos].RegionType)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 분포용 데이터 (그룹과 무관하게 전체 맵 배치를 저장하여 간섭 체크)
            List<(Vector2Int Pos, RoomType Type)> alreadyPlaced = new();

            // 8방향에 특수방 없는지 체크
            bool IsStrictlyIsolated(Vector2Int candidatePos)
            {
                foreach (var placed in alreadyPlaced)
                {
                    int dx = Mathf.Abs(candidatePos.x - placed.Pos.x);
                    int dy = Mathf.Abs(candidatePos.y - placed.Pos.y);

                    if (dx <= 1 && dy <= 1) return false;
                }
                return true;
            }

            // 자리가 없을 때 DFS로 설치가능한 구역이 있는지 체크
            bool IsValidPlacementWithDFS(Vector2Int candidatePos, RoomType targetType)
            {
                List<(Vector2Int Pos, RoomType Type)> tempPlaced = new(alreadyPlaced) { (candidatePos, targetType) };
                Stack<(Vector2Int Pos, RoomType Type)> stack = new();
                HashSet<Vector2Int> visited = new();
                List<(Vector2Int Pos, RoomType Type)> cluster = new();

                stack.Push((candidatePos, targetType));
                visited.Add(candidatePos);

                while (stack.Count > 0)
                {
                    var curr = stack.Pop();
                    cluster.Add(curr);

                    foreach (var other in tempPlaced)
                    {
                        if (visited.Contains(other.Pos)) continue;

                        int dx = Mathf.Abs(curr.Pos.x - other.Pos.x);
                        int dy = Mathf.Abs(curr.Pos.y - other.Pos.y);

                        if (dx + dy == 1)
                        {
                            visited.Add(other.Pos);
                            stack.Push(other);
                        }
                    }
                }
                return cluster.Count <= 1;
            }

            // 2) 그룹별로 특수방 배치 시작
            foreach (var kvp in candidatesByGroup)
            {
                RegionType currentGroup = kvp.Key;
                List<Vector2Int> groupCandidates = kvp.Value;

                // 후보군 셔플
                for (int i = groupCandidates.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (groupCandidates[i], groupCandidates[j]) = (groupCandidates[j], groupCandidates[i]);
                }

                // 그룹 내 스폰 리스트 생성
                int cap = groupCandidates.Count;

                // 그룹당 1개의 BuddyRoom 고정
                int buddy = Mathf.Clamp(1, 0, cap);
                int sp = Mathf.Clamp(specialRoomPerGroup, 0, cap - buddy);
                int sh = Mathf.Clamp(shopRoomPerGroup, 0, cap - buddy - sp);
                int pa = Mathf.Clamp(passiveRoomPerGroup, 0, cap - buddy - sp - sh);

                List<RoomType> roomsToSpawn = new();
                for (int i = 0; i < buddy; i++) roomsToSpawn.Add(RoomType.BuddyRoom);
                for (int i = 0; i < sp; i++) roomsToSpawn.Add(RoomType.SpecialRoom);
                for (int i = 0; i < sh; i++) roomsToSpawn.Add(RoomType.ShopRoom);
                for (int i = 0; i < pa; i++) roomsToSpawn.Add(RoomType.ModuleRoom); // passiveRoom -> ModuleRoom 적용

                foreach (RoomType targetType in roomsToSpawn)
                {
                    int selectedIndex = -1;

                    // [Plan A] 우선 무조건 1칸 이상 떨어져 있는 완벽한 빈자리를 찾습니다.
                    for (int i = 0; i < groupCandidates.Count; i++)
                    {
                        Vector2Int candidate = groupCandidates[i];
                        if (defaultRoomsByPos[candidate].RoomType != RoomType.NormalRoom) continue;

                        if (IsStrictlyIsolated(candidate))
                        {
                            selectedIndex = i;
                            break;
                        }
                    }

                    // [Plan B] 빈자리가 없다면 DFS를 돌려서 뭉쳐도 되는 자리를 찾습니다.
                    if (selectedIndex == -1)
                    {
                        for (int i = 0; i < groupCandidates.Count; i++)
                        {
                            Vector2Int candidate = groupCandidates[i];
                            if (defaultRoomsByPos[candidate].RoomType != RoomType.NormalRoom) continue;

                            if (IsValidPlacementWithDFS(candidate, targetType))
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }

                    // [Plan C] 배치 확정
                    if (selectedIndex != -1)
                    {
                        Vector2Int selectedPos = groupCandidates[selectedIndex];
                        IRoomDef roomDef;
                        RegionType groupType = defaultRoomsByPos[selectedPos].RegionType;

                        if (targetType == RoomType.ModuleRoom)
                        {
                            var randCategory = (Category)Random.Range(0, (int)Category.Count);
                            roomDef = new ModuleRoomDef(selectedPos, RoomType.ModuleRoom, groupType, randCategory);
                        }
                        else if (targetType == RoomType.SpecialRoom)
                        {
                            roomDef = new EventRoomDef(selectedPos, RoomType.SpecialRoom, groupType);
                        }
                        else
                        {
                            roomDef = new DefaultRoomDef(selectedPos, targetType, groupType);
                        }

                        defaultRoomsByPos[selectedPos] = roomDef;
                        alreadyPlaced.Add((selectedPos, targetType));

                        // 쓴 자리는 전체 후보군(candidates)과 그룹 후보군에서 제거
                        groupCandidates.RemoveAt(selectedIndex);
                        candidates.Remove(selectedPos);
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomGen] 그룹 {currentGroup}의 맵이 너무 좁아 {targetType}을(를) 단독으로도, 뭉쳐서도 배치할 수 없습니다. 스폰을 스킵합니다.");
                    }
                }
            }
        }

        public IRoom PopRoomObj(IRoomDef roomDef)
        {
            if (roomDef.PrefabIndex == -1)
            {
                roomDef.PrefabIndex = Random.Range(0, prefabsByType[roomDef.RoomType].Count);
            }

            var prefab = prefabsByType[roomDef.RoomType][roomDef.PrefabIndex];
            IRoom room = poolManager.Pop<IRoom>(prefab);
            room.GridPos = roomDef.GridPos;
            room.Transform.position = roomWorldPosDict[roomDef.RoomType][roomDef.PrefabIndex];
            roomDef.RoomSize = room.Size;
            roomDef.WorldPos = room.Transform.position;
            roomDef.Vertices = room.Vertices.Select(x => new Vector2(x.position.x, x.position.z)).ToList();
            return room;
        }

        public List<IRoom> PopAllRoomPrefab()
        {
            List<IRoom> roomList = new();
            foreach (var kv in prefabsByType)
            {
                int cnt = kv.Value.Count;
                for (int i = 0; i < cnt; ++i)
                {
                    var prefab = kv.Value[i];
                    IRoom room = poolManager.Pop<IRoom>(prefab);
                    room.Transform.position = roomWorldPosDict[kv.Key][i];
                    room.Initialize();
                    roomList.Add(room);
                }
            }
            return roomList;
        }

        public List<DoorDef> CreateDoorsData(List<IRoomDef> rooms)
        {
            List<DoorDef> doorDefs = new();

            if (rooms == null || rooms.Count == 0)
                return doorDefs;

            var map = new Dictionary<Vector2Int, IRoomDef>(rooms.Count);
            foreach (var room in rooms)
            {
                if (room == null) continue;
                map[room.GridPos] = room;
            }

            foreach (var kv in map)
            {
                Vector2Int aPos = kv.Key;

                Vector2Int rightPos = aPos + Vector2Int.right;
                if (map.TryGetValue(rightPos, out _))
                    doorDefs.Add(new DoorDef(aPos, rightPos));

                Vector2Int upPos = aPos + Vector2Int.up;
                if (map.TryGetValue(upPos, out _))
                    doorDefs.Add(new DoorDef(aPos, upPos));
            }

            return doorDefs;
        }

        public IDoorManager PopDoorObj()
        {
            if (doorItem == null)
            {
                Debug.LogError("[MapSpawner] doorItem이 설정되지 않았습니다.");
                return null;
            }

            IDoorManager doorObj = poolManager.Pop<DoorManager>(doorItem);
            doorObj.Init();

            return doorObj;
        }

        public List<IRoomDef> GetRoomDatas()
        {
            return defaultRoomsByPos.Values.ToList();
        }
    }
}