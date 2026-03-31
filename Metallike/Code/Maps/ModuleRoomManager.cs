using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using Core.EventBus;
using GondrLib.Dependencies;
using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Work.CDH.Code.Maps
{
    public class ModuleRoomManager : MonoBehaviour
    {
        [SerializeField] private RoomFunctionSO roomModuleFunction;

        [SerializeField] private PoolItemSO daisItem;
        [SerializeField] private float speed;


        [Inject] private PoolManagerMono poolManager;

        private Vector3 startPos;
        private Vector3 endPos;

        private IRoom curRoom;

        private IRoomDef curRoomDef;

        private Dictionary<int, Dais> daisDict;

        private void Awake()
        {
            daisDict = new();

            Bus<FirstEnterRoomEvent>.OnEvent += HandleRoomFirstEnterEvent;
        }

        private void OnDestroy()
        {
            Bus<FirstEnterRoomEvent>.OnEvent -= HandleRoomFirstEnterEvent;
        }

        public void HandleRoomFirstEnterEvent(FirstEnterRoomEvent evt)
        {
            curRoom = evt.Room;
            curRoomDef = evt.RoomDef;

            var kvp = roomModuleFunction.TryUse<RoomModuleFunctionData>(curRoomDef, curRoom);
            if (!kvp.isSuccess)
                return;

            var data = kvp.result;

            if (!data.DaisRoomIsFixedDais)
            {
                if (data.DaisTrms.Length < 2)
                {
                    Debug.LogError($"단상의 시작 위치와 끝 위치가 없습니다.");
                }

                Dais dais = poolManager.Pop<Dais>(daisItem);
                daisDict.Add(dais.gameObject.GetInstanceID(), dais);

                var targetModule = data.TargetModules[0];

                dais.SetModule(targetModule);
                dais.gameObject.SetActive(true);

                var daisStartTrm = data.DaisTrms[0];
                var daisEndTrm = data.DaisTrms[1];

                startPos = daisStartTrm.position;
                dais.transform.position = startPos;
                float endY = daisEndTrm.position.y - startPos.y;
                endPos = startPos + new Vector3(0f, endY, 0f);

                Bus<RoomObjGenerateEvent>.OnEvent?.Invoke(new(dais.gameObject));

                curRoom.OnClear -= HandleRoomClear;
                curRoom.OnClear += HandleRoomClear;
            }
            else
            {
                int a = 0;
                foreach (var dynamicObj in curRoomDef.DynamicObjDatas.Objects)
                {
                    int id = dynamicObj.GetInstanceID();
                    if (daisDict.ContainsKey(id))
                    {
                        a++;
                    }
                }

                var moduleList = data.TargetModules;

                for (int i = 0; i < data.DaisTrms.Length - a; i++)
                {
                    var dais = poolManager.Pop<Dais>(daisItem);
                    daisDict[dais.gameObject.GetInstanceID()] = dais;

                    // int targetCategoryCnt = Random.Range(0, moduleMap[targetCategory].Count);
                    // ModuleSO targetModule = moduleMap[targetCategory][targetCategoryCnt];
                    ModuleSO targetModule = moduleList[Random.Range(0, moduleList.Count)];
                    moduleList.Remove(targetModule);

                    dais.SetModule(targetModule);
                    dais.gameObject.SetActive(true);

                    dais.transform.position = data.DaisTrms[i].position;

                    Bus<RoomObjGenerateEvent>.OnEvent?.Invoke(new(dais.gameObject));

                    dais.OnPlayerCheck -= HandlePlayerCheckMulti;
                    dais.OnPlayerCheck += HandlePlayerCheckMulti;
                    dais.StartCheck();
                }
            }
        }

        private void HandleRoomClear()
        {
            curRoom.OnClear -= HandleRoomClear;
            UpDais();
        }

        [ContextMenu("Up Dais")]
        public void UpDais()
        {
            foreach (var dynamicObj in curRoomDef.DynamicObjDatas.Objects)
            {
                if (daisDict.TryGetValue(dynamicObj.GetInstanceID(), out var dais))
                {
                    dais.Move(endPos, speed, () =>
                    {
                        dais.gameObject.SetActive(true);
                        dais.OnPlayerCheck += HandlePlayerCheckSingle;
                        dais.StartCheck();
                    });
                }
            }
        }

        [ContextMenu("Down Dais")]
        public void DownDais()
        {
            foreach (var dynamicObj in curRoomDef.DynamicObjDatas.Objects)
            {
                if (daisDict.TryGetValue(dynamicObj.GetInstanceID(), out var dais))
                {
                    dais.Move(startPos, speed, () =>
                    {
                        dais.transform.position = Vector3.down * 100f;
                        dais.gameObject.SetActive(false);
                        Bus<RoomObjRemoveEvent>.OnEvent?.Invoke(new(dais.gameObject));
                        poolManager.Push(dais);
                    });
                }
            }
        }

        private void HandlePlayerCheckSingle(ModuleSO module)
        {
            foreach (var dynamicObj in curRoomDef.DynamicObjDatas.Objects)
            {
                if (daisDict.TryGetValue(dynamicObj.GetInstanceID(), out var dais))
                {
                    dais.OnPlayerCheck -= HandlePlayerCheckSingle;
                    dais.StopCheck();
                }
            }
            Bus<ChoosePassiveEvents>.OnEvent?.Invoke(new(module));
            DownDais();
        }

        private void HandlePlayerCheckMulti(ModuleSO module)
        {
            foreach (var dais in daisDict.Values)
            {
                dais.OnPlayerCheck -= HandlePlayerCheckSingle;
                dais.StopCheck();
                dais.gameObject.SetActive(false);
                Bus<RoomObjRemoveEvent>.OnEvent?.Invoke(new(dais.gameObject));
                poolManager.Push(dais);
            }

            Bus<ChoosePassiveEvents>.OnEvent?.Invoke(new(module));
        }
    }
}
