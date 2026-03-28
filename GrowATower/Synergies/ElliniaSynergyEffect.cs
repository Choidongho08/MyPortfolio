using _01.Member.SB._01.Code;
using _01.Member.SD._01.Code.Unit.Interface;
using _01.Member.SD._01.Code.Unit.Interface.UnitSupport;
using System.Collections.Generic;

namespace Assets._01.Member.CDH.Code.Synergies.Ellinia
{
    internal class ElliniaSynergyEffect : SynergyEffectComponent
    {
        public List<BaseUnit> units => UnitManager.Instance.GetAllUnits();

        protected override void SynergyActiveMethod(Synergy synergy)
        {
            foreach(BaseUnit unit in units)
            {
                // 시든 유닛 살리기
                if(unit._plantWaterComponent.CurremtWaterState == WaterState.Dead)
                    unit._plantWaterComponent.SetWaterValueToMax();
            }
        }
    }
}
