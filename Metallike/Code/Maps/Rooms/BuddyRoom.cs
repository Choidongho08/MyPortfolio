using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using GondrLib.ObjectPool.RunTime;
using UnityEngine;
using Work.SB._01.Scripts.Enemy.Script;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class BuddyRoom : AbstractMonsterRoom
    {
        public bool CanInit { get; set; } = true;

        [field: SerializeField] public Boss[] BossPrefabs { get; private set; }
        [field: SerializeField] public Transform BossSpawnTrm { get; private set; }

        [Header("Buddy Setting")]
        [SerializeField] private PoolItemSO[] playablePrefabs;
        [SerializeField] private Transform prefabPosition;

        private BuddyObj buddyObj;

        public void Initialize(CharacterManager characterManager, PoolManagerMono poolManager)
        {
            if (!CanInit)// 이녀석 다시 true안해줘서 2번째부터 안됨
                return;

            var curCharacters = characterManager.CurrentParty;

            for (int i = 0; i < playablePrefabs.Length; ++i)
            {
                var playablePrefab = playablePrefabs[Random.Range(0, playablePrefabs.Length)];
                buddyObj = poolManager.Pop<BuddyObj>(playablePrefab);
                if (curCharacters.Contains(buddyObj.character))
                    buddyObj.PushItem();
                else
                    break;
            }

            buddyObj.transform.position = prefabPosition.position;
            CanInit = false;
        }

        public override void ThisRoomClear()
        {
            base.ThisRoomClear();
            buddyObj.EnableInteract();
        }

        public override void PushRoom()
        {
            buddyObj?.PushItem();
            base.PushRoom();
        }
    }
}
