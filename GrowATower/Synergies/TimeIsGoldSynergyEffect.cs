using _01.Member.SB._01.Code;
using _01.Member.SD._01.Code.Unit.Interface;
using System.Collections.Generic;

namespace Assets._01.Member.CDH.Code.Synergies.TimeIsGold
{
    public class TimeIsGoldSynergyEffect : SynergyEffectComponent
    {
        private List<BaseUnit> units;

        protected override void SynergyActiveMethod(Synergy synergy)
        {
            units = UnitManager.Instance.GetAllUnits();
            // logic
        }
    }
}
