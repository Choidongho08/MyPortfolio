using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public interface IDoorManager : IPoolable
    {
        event Action<Vector2Int> OnNewRoom;
        Transform Transform { get; }

        void Init();
        void DoorsInitlaizer();
        void DoorSetting(Dictionary<EnterDirection, Vector3> posByDir, IRoomDef roomDef);
        void PossibleDoorsSetting(List<EnterDirection> possibleDirs);
        void ImpossibleDoorsSetting(List<EnterDirection> impossibleDirs);
        void SetRoomGridPos(Vector2Int gridPos);
        void SetMiddleBossSetting(List<DoorDef> doorDefs, IMapDataProvider model, Vector2Int room);
        void PushItem();
    }
}
