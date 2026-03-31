using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class SpecialRoom : AbstractRoom
    {
        public GameObject MyEventObj { get; set; }

        [field: SerializeField] public Transform EventTrm { get; private set; }

        public override void FirstEnterRoom()
        {
            base.FirstEnterRoom();
            FastClear();
        }

        
    }
}
