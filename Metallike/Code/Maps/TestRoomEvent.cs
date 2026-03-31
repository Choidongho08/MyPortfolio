using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public class TestRoomEvent : MonoBehaviour
    {
        [SerializeField] private Vector2Int pos;

        [ContextMenu("SetPlayerPosEvent")]
        private void SetPlayerPos()
        {
            Bus<EnterRoomEvent>.OnEvent?.Invoke(new(new DefaultRoomDef(pos, RoomType.NormalRoom, RegionType.None)));
        }


        [ContextMenu("AddRoomSelectDoorEvent")]
        private void AddRoomSelectDoor()
        {
            Bus<StartAddRoomEvent>.OnEvent?.Invoke(new());
        }
    }
}
