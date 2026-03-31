using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.UIs.Maps;
using Core.EventBus;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public class EventTester : MonoBehaviour
    {
        [SerializeField] private int level;
        [SerializeField] private float value;

        [ContextMenu("sagsdhd")]
        private void Method()
        {
            BusManager.Instance.SendEvent(new SecurityLevelUpdateEvent(level, value));
        }
    }
}
