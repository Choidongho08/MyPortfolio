using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class ModuleRoom : AbstractModuleRoom
    {
        public override void FirstEnterRoom()
        {
            base.FirstEnterRoom();
            FastClear();
        }
    }
}
