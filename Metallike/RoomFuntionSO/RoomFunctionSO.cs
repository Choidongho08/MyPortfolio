using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    [CreateAssetMenu(fileName = "RoomFunctionSO", menuName = "SO/CDH/RoomFunctionSO")]
    public abstract class RoomFunctionSO : ScriptableObject
    {
        public abstract (bool isSuccess, T result) TryUse<T, TDef>(TDef roomDef, IRoom room) where TDef : IRoomDef;
        public abstract (bool isSuccess, T result) TryUse<T, TDef>(TDef roomDef) where TDef : IRoomDef;
        public abstract (bool isSuccess, T result) TryUse<T>(IRoom room);

        protected (bool, T result) FastReturnFalse<T>() => (false, default);
    }
}
