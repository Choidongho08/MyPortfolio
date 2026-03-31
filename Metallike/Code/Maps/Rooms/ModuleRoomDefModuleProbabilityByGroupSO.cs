using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    [Serializable]
    public struct ModuleProbability
    {
        public ModuleType TargetModuleType;
        public float Value;
    }

    [CreateAssetMenu(fileName = "ModuleRoomModuleProbabilityByGroupSO", menuName = "SO/CDH/ModuleRoomModuleProbabilityByGroupSO")]
    public class ModuleRoomDefModuleProbabilityByGroupSO : RoomFunctionSO
    {
        public RegionType TargetGroupType;
        public ModuleProbability[] Probabilities;

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef)
        {
            if (roomDef.RegionType != TargetGroupType) // 방의 그룹이 이 SO의 타겟 그룹이랑 같은지
                goto lb_return_false;

            if (roomDef is not ModuleRoomDef moduleRoomDef) // 방이 모듈이 나오는 모듈룸인지
                goto lb_return_false;

            moduleRoomDef.ModuleProbabilities = Probabilities; // 모듈 가중치 Def에 적용

            if(moduleRoomDef is T result)
                return (true, result);

        lb_return_false:
            return (false, default);
        }

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef, IRoom room) => TryUse<T>(roomDef);

        public override (bool isSuccess, T result) TryUse<T>(IRoom room) => FastReturnFalse<T>();
    }
}
