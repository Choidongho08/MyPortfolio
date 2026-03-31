using Core.EventBus;
using System;

namespace Assets.Work.CDH.Code.Eventss
{
    public struct FadeInEvent : IEvent
    {
        public float Duration;
        public Action OnComplete;

        public FadeInEvent(float duration, Action onComplete = null)
        {
            Duration = duration;
            OnComplete = onComplete;
        }
    }
    public struct FadeOutEvent : IEvent
    {
        public float Duration;
        public Action OnComplete;

        public FadeOutEvent(float duration, Action onComplete = null)
        {
            Duration = duration;
            OnComplete = onComplete;
        }
    }
}
