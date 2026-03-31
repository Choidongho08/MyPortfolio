using Assets.Work.CDH.Code.Maps;
using Assets.Work.CDH.Code.UIs.Maps;
using Assets.Work.CDH.Code.UIs.Maps.SecurityLevels;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.AppUI.Core;
using UnityEngine;

public struct ModuleIconSpriteByRoomType
{
    public RoomType type;
    public Sprite Sprite;
}

public struct MapDataInitData
{
    public Sprite BackgroundImage;
    public Sprite ShopIcon;
    public Sprite SpeicalIcon;
    public Sprite BossIcon;
    public List<ModuleIconSpriteByRoomType> PassiveIcons;
}

public interface IRoomUIData
{
    public Vector2Int Pos { get; set; }
    public Sprite Sprite { get; set; }
    public Sprite SubSprite { get; set; }
    public bool IsDiscovered { get; set; }
}

public struct RoomSpriteDataForMapView : IRoomUIData
{
    public Vector2Int Pos { get; set; }
    public Sprite Sprite { get; set; }
    public Sprite SubSprite { get; set; }
    public bool IsDiscovered { get; set; }
    public RoomType Type { get; set; }

    public RoomSpriteDataForMapView(Vector2Int pos, Sprite sprite, Sprite subSprite, bool isDiscovered, RoomType type)
    {
        Pos = pos;
        Sprite = sprite;
        SubSprite = subSprite;
        IsDiscovered = isDiscovered;
        Type = type;
    }
}

public struct RoomIconDataForMimap : IRoomUIData
{
    public Vector2Int Pos { get; set; }
    public Sprite Sprite { get; set; }
    public Sprite SubSprite { get; set; }
    public bool IsDiscovered { get; set; }
    public List<Vector2> Vertices { get; set; }

    public RoomIconDataForMimap(Vector2Int pos, Sprite sub, bool isDiscovered, List<Vector2> vertices)
    {
        Pos = pos;
        Sprite = null;
        SubSprite = sub;
        IsDiscovered = isDiscovered;
        Vertices = vertices;
    }
}

/// <summary>
/// 통합 맵 데이터
/// </summary>
public class MapDataProvider : IMapDataProvider
{
    public MapViewRoomSpritesSO SpritesSO { get; set; }

    // [복구됨] UI에서 사용 중인 리스트 유지
    public List<DoorDef> EnterableDoors { get; private set; } = new();
    public List<IRoomDef> EnterableRooms { get; private set; } = new();

    public Vector2Int PlayerRoomPos { get; private set; }

    public SecurityLevelDataDictSO SecurityLevelDictSO { get; private set; }
    public int SecurityLevel { get; set; } = 0;

    // Room
    private Dictionary<Vector2Int, IRoomDef> allRoomDataByPos = new();
    private Dictionary<Vector2Int, IRoomDef> curRoomDataByPos = new();

    // RoomUI
    private List<IRoomUIData> roomListForMapView = new();
    private Dictionary<Vector2Int, IRoomUIData> roomDictForMapView = new();

    private List<IRoomUIData> roomListForMimimap = new();
    private Dictionary<Vector2Int, IRoomUIData> roomDictForMimimap = new();

    // Door
    private Dictionary<Vector2Int, List<DoorDef>> allDoorDatasByRoomPos = new();

    private Dictionary<RoomType, List<Vector2Int>> roomPosesByType = new();

    // Group
    private Dictionary<RegionType, string> roomRegionNameDict = new();

    private int mapId;

    public MapDataProvider()
    {
        PlayerRoomPos = Vector2Int.zero;
    }

    public void Initializer()
    {
        Clear();
    }

    public void SetMapId(int id)
    {
        mapId = id;
    }

    public void SetRoomData(IRoomDef roomData)
    {
        Sprite roomSprite, roomIcon, sub;
        SpritesSO.GetSprite(roomData.RoomType, out roomSprite, out roomIcon);
        if (roomData is ModuleRoomDef psRoomDef)
            sub = SpritesSO.GetSprite(psRoomDef.Category);
        else
            sub = null;

        roomDictForMapView[roomData.GridPos] = new RoomSpriteDataForMapView(roomData.GridPos, roomSprite, sub, false, roomData.RoomType);
        roomDictForMimimap[roomData.GridPos] = new RoomIconDataForMimap(roomData.GridPos, roomIcon, false, roomData.Vertices);

        if (!roomPosesByType.TryGetValue(roomData.RoomType, out var posList))
            roomPosesByType[roomData.RoomType] = posList = new();
        posList.Add(roomData.GridPos);

        roomListForMapView = roomDictForMapView.Values.ToList();
        roomListForMimimap = roomDictForMimimap.Values.ToList();

        allRoomDataByPos[roomData.GridPos] = roomData;
    }

    public void ChangeRoomData(IRoomDef roomData)
    {
        allRoomDataByPos[roomData.GridPos] = roomData;
        DoorEnterable(roomData.GridPos);
    }

    public void SetRoomDatas(List<IRoomDef> roomDatas)
    {
        foreach (var roomData in roomDatas)
        {
            SetRoomData(roomData);
        }
    }

    public void SetDoorData(DoorDef data)
    {
        if (!allDoorDatasByRoomPos.TryGetValue(data.AGridPos, out var doorsA))
            allDoorDatasByRoomPos[data.AGridPos] = doorsA = new List<DoorDef>();
        if (!allDoorDatasByRoomPos.TryGetValue(data.BGridPos, out var doorsB))
            allDoorDatasByRoomPos[data.BGridPos] = doorsB = new List<DoorDef>();

        doorsA.Add(data);
        doorsB.Add(data);
    }

    public void SetDoorDatas(List<DoorDef> doorDatas)
    {
        foreach (var data in doorDatas)
        {
            SetDoorData(data);
        }

        UpdateEnterable();
    }

    public void SetPlayerRoom(Vector2Int newPos)
    {
        PlayerRoomPos = newPos;
        UpdateEnterable();
    }

    public void SetStartRoom(Vector2Int startPos)
    {
        if (allRoomDataByPos.TryGetValue(startPos, out var room))
        {
            room.RoomType = RoomType.StartRoom;
            allRoomDataByPos[startPos] = room;
        }
        SetPlayerRoom(startPos);
        AddCurRoom(startPos);
    }

    public void AddCurRoom(Vector2Int newRoomPos)
    {
        if (!curRoomDataByPos.ContainsKey(newRoomPos))
        {
            if (allRoomDataByPos.TryGetValue(newRoomPos, out var roomDef))
            {
                roomDef.IsDiscovered = true;
                curRoomDataByPos.Add(newRoomPos, roomDef);
            }
            DoorEnterable(newRoomPos);

            if (roomDictForMapView.TryGetValue(newRoomPos, out var roomSprite))
            {
                roomSprite.IsDiscovered = true;
                roomDictForMapView[newRoomPos] = roomSprite;
            }
            if(roomDictForMimimap.TryGetValue(newRoomPos, out var roomIcon))
            {
                roomIcon.IsDiscovered = true;
                roomDictForMimimap[newRoomPos] = roomIcon;
            }

            roomListForMapView = roomDictForMapView.Values.ToList();

            UpdateEnterable();
        }
    }

    Vector2Int[] dirs = new Vector2Int[4]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private void DoorEnterable(Vector2Int newRoomPos)
    {
        if (!allRoomDataByPos.TryGetValue(newRoomPos, out var newRoom))
            return;

        for (int i = 0; i < 4; ++i)
        {
            // target을 기준으로 4방향 현재 방 아님
            Vector2Int targetPos = newRoomPos + dirs[i];

            if (!allRoomDataByPos.TryGetValue(targetPos, out var targetRoom))
                continue;

            if (!targetRoom.IsDiscovered)
                continue;

            Vector2Int dir = newRoomPos - targetPos;

            if (dir == Vector2Int.up)
            {
                targetRoom.DoorsEnterableDict[EnterDirection.Up] = true;
                newRoom.DoorsEnterableDict[EnterDirection.Down] = true;
            }
            if (dir == Vector2Int.down)
            {
                targetRoom.DoorsEnterableDict[EnterDirection.Down] = true;
                newRoom.DoorsEnterableDict[EnterDirection.Up] = true;
            }
            if (dir == Vector2Int.left)
            {
                targetRoom.DoorsEnterableDict[EnterDirection.Left] = true;
                newRoom.DoorsEnterableDict[EnterDirection.Right] = true;
            }
            if (dir == Vector2Int.right)
            {
                targetRoom.DoorsEnterableDict[EnterDirection.Right] = true;
                newRoom.DoorsEnterableDict[EnterDirection.Left] = true;
            }

            allRoomDataByPos[targetPos] = targetRoom;
            allRoomDataByPos[newRoomPos] = newRoom;
        }
    }

    public void SetSecurityLevelDictSO(SecurityLevelDataDictSO so)
    {
        SecurityLevelDictSO = so;
    }

    /// <summary>
    /// UI, 열수있는 문 Enter가 아님
    /// </summary>
    private void UpdateEnterable()
    {
        // 1. 리스트 초기화
        EnterableDoors.Clear();
        EnterableRooms.Clear();

        // 2. 필수 데이터 검증 (Pattern Matching 활용)
        if (!allRoomDataByPos.TryGetValue(PlayerRoomPos, out var currentRoom) ||
            !allDoorDatasByRoomPos.TryGetValue(PlayerRoomPos, out var doorList))
        {
            return;
        }

        // 3. 문과 방 탐색
        foreach (var door in doorList)
        {
            // 삼항 연산자로 반대편 좌표 계산
            var otherPos = (door.AGridPos == PlayerRoomPos) ? door.BGridPos : door.AGridPos;

            // 맨해튼 거리 계산 (인접한 방인지 확인)
            int manhattanDist = Mathf.Abs(otherPos.x - PlayerRoomPos.x) + Mathf.Abs(otherPos.y - PlayerRoomPos.y);
            if (manhattanDist != 1)
                continue;

            // 방 데이터가 없거나, 예외 타입(잠긴 방 등)이면 스킵
            if (!allRoomDataByPos.TryGetValue(otherPos, out var otherRoom)) 
                continue;

            // 4. 방 리스트 추가 (중복 방지)
            // 인접한 방의 개수는 매우 적으므로(최대 4개), HashSet을 새로 할당하는 것보다
            // List.Contains를 사용하는 것이 GC 오버헤드가 없어 훨씬 효율적입니다.
            if (!EnterableRooms.Contains(otherRoom))
            {
                EnterableRooms.Add(otherRoom);
            }

            // 5. 문 리스트 추가
            // 이미 방문/처리된 위치라면 문을 추가하지 않음
            if (curRoomDataByPos.ContainsKey(otherPos)) 
                continue;

            if (!EnterableDoors.Contains(door))
            {
                EnterableDoors.Add(door);
            }
        }
    }

    public void Clear()
    {
        EnterableDoors.Clear(); // [복구됨]
        EnterableRooms.Clear();

        allRoomDataByPos.Clear();
        curRoomDataByPos.Clear();
        allDoorDatasByRoomPos.Clear();

        roomListForMapView.Clear();
        roomDictForMapView.Clear();
        roomDictForMimimap.Clear();
        roomListForMimimap.Clear();

        PlayerRoomPos = Vector2Int.zero;
    }

    public void AddGroupName(RegionType targetGroupType, string name)
    {
        roomRegionNameDict.Add(targetGroupType, name);
    }

    #region Get Methods

    public IRoomDef GetStartRoomDef()
    {
        return allRoomDataByPos[PlayerRoomPos];
    }

    public IRoomDef GetPlayerRoomDef()
    {
        return allRoomDataByPos[PlayerRoomPos];
    }

    public Dictionary<Vector2Int, IRoomUIData> GetRoomSpriteDictForMapView()
    {
        return roomDictForMapView;
    }

    public List<IRoomUIData> GetRoomSpriteListForMapView()
    {
        return roomListForMapView;
    }

    public Dictionary<Vector2Int, IRoomUIData> GetRoomIconDictForMimimapView()
    {
        return roomDictForMimimap;
    }

    public List<IRoomUIData> GetRoomIconListForMimimapView()
    {
        return roomListForMimimap;
    }


    public Dictionary<Vector2Int, IRoomDef> GetAllRoomDataByPos()
    {
        return allRoomDataByPos;
    }

    public Dictionary<Vector2Int, List<DoorDef>> GetAllDoorDataByRoomPos()
    {
        return allDoorDatasByRoomPos;
    }

    public List<IRoomDef> GetAllRoomDatas()
    {
        return allRoomDataByPos.Values.ToList();
    }

    public List<DoorDef> GetAllDoorDatas()
    {
        return allDoorDatasByRoomPos.Values.SelectMany(list => list).ToList();
    }

    public List<Vector2Int> GetRoomPosesByType(RoomType roomType)
    {
        return roomPosesByType.GetValueOrDefault(roomType);
    }

    public IRoomDef GetRoomDefByPos(Vector2Int pos)
    {
        return allRoomDataByPos.GetValueOrDefault(pos);
    }

    public string GetRegionName(RegionType type)
    {
        roomRegionNameDict.TryGetValue(type, out string name);
        return name;
    }

    public Dictionary<RegionType, string> GetAllRegionDict()
    {
        return roomRegionNameDict;
    }

    public int GetMapId() => mapId;

    #endregion
}