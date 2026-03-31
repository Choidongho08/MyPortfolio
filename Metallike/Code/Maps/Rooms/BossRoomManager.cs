using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class BossRoomManager : BaseManager<BossRoomManager>
    {
        [SerializeField] private int needCardkeyCount;

        private int curNeedCardkeyCount;

        public override void StartManager()
        {
            curNeedCardkeyCount = needCardkeyCount;
            Bus<BossRoomUseCardkeyEvent>.OnEvent += HandleBossRoomUseCardkeyEvent;
        }

        private void OnDestroy()
        {
            Bus<BossRoomUseCardkeyEvent>.OnEvent -= HandleBossRoomUseCardkeyEvent;
        }

        private void HandleBossRoomUseCardkeyEvent(BossRoomUseCardkeyEvent evt)
        {
            curNeedCardkeyCount--;

            if(curNeedCardkeyCount <= 0)
            {
                BusManager.Instance.SendEvent<BossRoomCanEnterEvent>();
            }
        }
    }
}
