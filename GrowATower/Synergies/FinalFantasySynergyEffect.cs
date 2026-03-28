using Assets._01.Member.CDH.Code.Events;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies.FinalFantasy
{
    public class FinalFantasySynergyEffect : SynergyEffectComponent
    {
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private CardStat dragonFlowerCard;

        protected override void SynergyActiveMethod(Synergy synergy)
        {
            uiChannel.Invoke(UIEvents.AddCard.Initializer(dragonFlowerCard));
        }
    }
}
