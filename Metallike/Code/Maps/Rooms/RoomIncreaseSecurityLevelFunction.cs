using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    [CreateAssetMenu(fileName = "RoomIncreaseSecurityLevelFunction", menuName = "SO/CDH/RoomIncreaseSecurityLevelFunction")]
    public class RoomIncreaseSecurityLevelFunction : RoomFunctionSO
    {
        [SerializeField] private float securityLevelMultiflyValue = 1.0f;

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef, IRoom room) => TryUse<T>(roomDef);

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef)
        {
            // 특정한 조건
            roomDef.SecurityLevelIncreaseValue *= securityLevelMultiflyValue;

            if(roomDef is T result)
                return (true, result);

            return (false, default);
        }

        public override (bool isSuccess, T result) TryUse<T>(IRoom room) => FastReturnFalse<T>();
    }
}
