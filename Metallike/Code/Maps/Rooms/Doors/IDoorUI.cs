using GondrLib.ObjectPool.RunTime;
using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public interface IDoorUI
    {
        Action OnClickDoor { get; set; }
        void SetDoorDef(DoorDef doorDef);
    }
}
