using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    [CreateAssetMenu(fileName = "ForceBossRoomFunctionSO", menuName = "SO/CDH/ForceBossRoomFunctionSO")]
    public class ForceBossRoomFunctionSO : RoomFunctionSO
    {
        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef, IRoom room) => BroadcastAllRoomClose<T>();

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef) => BroadcastAllRoomClose<T>();

        public override (bool isSuccess, T result) TryUse<T>(IRoom room) => BroadcastAllRoomClose<T>();

        private (bool isSuccess, T result) BroadcastAllRoomClose<T>()
        {
            BusManager.Instance.SendEvent<AllRoomCloseEvent>();
            return (true, default);
        }
    }
}
