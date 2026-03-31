using Core.EventBus;
using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class PlayerStartRoom : AbstractRoom
    {
        [SerializeField] private string description;
        [field: SerializeField] public Transform StartPos { get; private set; }


        private void Awake()
        {
            Bus<GameStartEvents>.OnEvent += StartHandle;
        }

        private void StartHandle(GameStartEvents evt)
        {
            Bus<DescriptionUIEvents>.Raise(new DescriptionUIEvents(description, new Transform[] { doorPosList[0].Trm, doorPosList[1].Trm, doorPosList[2].Trm, doorPosList[3].Trm } ,5f,DescriptionBoxType.Door));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Bus<GameStartEvents>.OnEvent -= StartHandle;
        }
    }
}
