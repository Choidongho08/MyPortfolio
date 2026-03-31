using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using Core.EventBus;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Work.CDH.Code.Maps
{
    public class EventRoomManager : MonoBehaviour, IMapManager
    {
        [SerializeField] private GameObject[] eventObjs;

        private void Awake()
        {
            Bus<FirstEnterRoomEvent>.OnEvent += HandleFirstEnterRoomEvent;
            Bus<RoomExitEvent>.OnEvent += HandleRoomExitEvent;
        }

        private void OnDestroy()
        {
            Bus<FirstEnterRoomEvent>.OnEvent -= HandleFirstEnterRoomEvent;
            Bus<RoomExitEvent>.OnEvent -= HandleRoomExitEvent;
        }

        private void HandleRoomExitEvent(RoomExitEvent evt)
        {
            var room = evt.RoomObj;
            var roomDef = evt.RoomDef;

            if (room is not SpecialRoom specialRoom || roomDef is not EventRoomDef eventRoomDef)
                return;

            foreach (var obj in eventRoomDef.DynamicObjDatas.Objects)
            {
                obj.SetActive(false);
            }
        }

        public void HandleFirstEnterRoomEvent(FirstEnterRoomEvent evt)
        {
            var room = evt.Room;
            var roomDef = evt.RoomDef;

            if (room is not SpecialRoom specialRoom || roomDef is not EventRoomDef eventRoomDef)
                return;

            if (eventRoomDef.MyEvent == null)
            {
                int randIndex = Random.Range(0, eventObjs.Length);
                eventRoomDef.EventObjIndex = randIndex;
                var targetPrefab = eventObjs[randIndex];

                var targetObj = Instantiate(targetPrefab, specialRoom.Transform);
                targetObj.transform.SetLocalPositionAndRotation(specialRoom.EventTrm.localPosition, Quaternion.identity);
                var targetCompo = targetObj.GetComponent<ISpecialRoomCompo>();
                targetCompo.Initialize();
                eventRoomDef.MyEvent = targetCompo;
                specialRoom.MyEventObj = targetObj;

                Bus<ChangeRoomDefEvent>.OnEvent?.Invoke(new(eventRoomDef));
            }
            else
            {
                if (eventRoomDef.MyEvent.IsUsed)
                    return;

                if (specialRoom.MyEventObj != null)
                {
                    Destroy(specialRoom.MyEventObj);
                }
                var targetPrefab = eventObjs[eventRoomDef.EventObjIndex];
                var targetObj = Instantiate(targetPrefab, Vector3.zero, Quaternion.identity, specialRoom.Transform);
                targetObj.transform.SetLocalPositionAndRotation(specialRoom.EventTrm.localPosition, Quaternion.identity);
                var targetCompo = targetObj.GetComponent<ISpecialRoomCompo>();
                targetCompo.Initialize();
                eventRoomDef.MyEvent = targetCompo;
                specialRoom.MyEventObj = targetObj;

                Bus<ChangeRoomDefEvent>.OnEvent?.Invoke(new(eventRoomDef));
            }
        }
    }
}
