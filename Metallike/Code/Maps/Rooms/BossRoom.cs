using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using System;
using System.IO.Compression;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class BossRoom : AbstractRoom
    {
        [field: SerializeField] public Transform PlayerPos;

        [SerializeField] private GameObject bossEntranceRoom;
        [SerializeField] private GameObject realBossRoom;
        [SerializeField] private Transform bossCheckTrm;
        [SerializeField] private Vector3 bossCheckSize;
        [SerializeField] private LayerMask playerLayer;

        private Collider[] colliders;
        private bool isCheck;

        private void Awake()
        {
            colliders = new Collider[1];
            isCheck = false;
        }

        public override void EnterRoom()
        {
            base.EnterRoom();
            Array.Clear(colliders, 0, colliders.Length);
        }

        public override void FirstEnterRoom()
        {
            base.FirstEnterRoom();
            realBossRoom.SetActive(false);
            bossEntranceRoom.SetActive(true);
            isCheck = false;
        }

        private void Update()
        {
            if (isCheck)
                return;

            int cnt = Physics.OverlapBoxNonAlloc(bossCheckTrm.position, bossCheckSize, colliders, Quaternion.identity, playerLayer);
            if (cnt == 0)
                return;

            isCheck = true;
            Bus<BossPenetrationCheckEvent>.OnEvent?.Invoke(new());
            realBossRoom.SetActive(true);
            bossEntranceRoom.SetActive(false);
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (bossCheckTrm == null)
                return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bossCheckTrm.position, bossCheckSize * 2);
        }

#endif
    }
}
