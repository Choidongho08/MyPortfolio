using Assets.Work.CDH.Code.Maps.Rooms;
using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public interface IRoom : IPoolable
    {
        Vector2 Size { get; }
        Vector2Int GridPos { get; set; }
        RoomType RoomType { get; }
        Action OnClear { get; set; }
        Transform Transform { get; }
        Transform Center { get; }
        Dictionary<int, GameObject> BreakingObstacles { get; }
        Dictionary<EnterDirection, Vector3> EnterPosDict { get; }
        Dictionary<EnterDirection, Vector3> DoorPosDict { get; }
        List<Transform> Vertices { get; }

        void FirstEnterRoom();
        void EnterRoom();
        void Initialize();
        void PushRoom();
    }
}
