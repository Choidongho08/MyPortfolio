using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public abstract class AbstractModuleRoom : AbstractRoom
    {
        [field: SerializeField] public bool isFixedTrm;
        [field: SerializeField] public Transform[] DaisTrms { get; private set; }
        [Range(0f, 100f)][field: SerializeField] public float Percent { get; set; }
    }
}