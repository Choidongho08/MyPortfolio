using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public struct RoomModuleFunctionData
    {
        public bool DaisRoomIsFixedDais;
        public Transform[] DaisTrms;
        public List<ModuleSO> TargetModules;
    }

    [CreateAssetMenu(fileName = "AnyRoomCreateModuleFunction", menuName = "SO/CDH/AnyRoomCreateModuleFunction")]
    public class AnyRoomCreateModuleFunction : RoomFunctionSO
    {
        [SerializeField] private float maxValue;
        [SerializeField] private ModuleSO[] moduleSos;

        private Dictionary<Category, Dictionary<ModuleType, ModuleSO>> moduleMap = new();

        private float standardValue;

        private void OnEnable()
        {
            moduleMap?.Clear();

            foreach (var module in moduleSos)
            {
                if (!moduleMap.TryGetValue(module.moduleCategory, out var Dict))
                    moduleMap[module.moduleCategory] = Dict = new();
                Dict.TryAdd(module.moduleType, module);
            }
        }

        private ModuleSO GetAllRandomModule()
        {
            if (moduleMap.Count == 0)
            {
                Debug.LogError("[Dictionary Error] moduleMap이 비어있습니다. OnEnable이 정상 호출되었는지 확인하세요.");
                return null;
            }

            // 실제 존재하는 카테고리 중에서 랜덤 선택
            var categories = moduleMap.Keys.ToList();
            Category randomCategory = categories[Random.Range(0, categories.Count)];

            // 선택된 카테고리 내에 존재하는 모듈 타입 중에서 랜덤 선택
            var innerDict = moduleMap[randomCategory];
            var moduleTypes = innerDict.Keys.ToList();
            ModuleType randomType = moduleTypes[Random.Range(0, moduleTypes.Count)];

            return innerDict[randomType];
        }

        private bool TryGetModule(Category category, ModuleType type, out ModuleSO module)
        {
            module = null;

            // 1. 카테고리 키가 있는지 확인
            if (!moduleMap.TryGetValue(category, out var innerDict))
            {
                Debug.LogError($"[Dictionary Error] '{category}' 카테고리가 moduleMap에 존재하지 않습니다! (ModuleSOs 세팅 확인 필요)");
                return false;
            }

            // 2. 모듈 타입 키가 있는지 확인
            if (!innerDict.TryGetValue(type, out module))
            {
                Debug.LogError($"[Dictionary Error] '{category}' 카테고리는 있지만, 그 안에 '{type}' 모듈 타입이 존재하지 않습니다!");
                return false;
            }

            return true;
        }

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef, IRoom room)
        {
            if (!(room is AbstractModuleRoom daisRoom))
                return (false, default);

            float standardValue = maxValue * daisRoom.Percent / 100f; // 변수 선언 위치 수정
            float value = Random.Range(0f, maxValue);
            if (value > standardValue)
                return (false, default);

            // 2. 데이터 세팅
            RoomModuleFunctionData data = new();
            data.DaisRoomIsFixedDais = daisRoom.isFixedTrm;
            data.DaisTrms = daisRoom.DaisTrms;
            data.TargetModules = new();

            int moduleCount = data.DaisTrms.Length;

            if (roomDef is ModuleRoomDef moduleDef)
            {
                Category targetCategory = moduleDef.Category;
                if (!moduleMap.ContainsKey(targetCategory))
                {
                    Debug.LogError($"{targetCategory}의 대한 List가 존재하지 않습니다.");
                    return (false, default);
                }

                var probabilities = moduleDef.ModuleProbabilities;
                if (probabilities.Length == 0)
                    return (false, default);

                // 총합 가중치 계산
                float totalWeight = 0f;
                foreach (var item in probabilities)
                {
                    totalWeight += item.Value;
                }

                // 모듈 뽑기
                for (int i = 0; i < moduleCount; i++)
                {
                    float randomValue = Random.Range(0f, totalWeight);
                    float currentWeight = 0f;
                    foreach (var probability in probabilities)
                    {
                        currentWeight += probability.Value;

                        if (randomValue <= currentWeight)
                        {
                            if (TryGetModule(targetCategory, probability.TargetModuleType, out ModuleSO targetModule))
                            {
                                data.TargetModules.Add(targetModule);
                                break;
                            }
                            else
                            {
                                data.TargetModules.Add(GetAllRandomModule());
                                break;
                            }
                        }
                    }
                }
            }
            else if (roomDef is DefaultRoomDef defaultRoomDef)
            {
                data.TargetModules.Add(GetAllRandomModule());
            }
            else
            {
                return (false, default);
            }

            if (data is T result)
            {
                return (true, result);
            }

            Debug.LogError($"타입 매칭 실패: 요청한 타입({typeof(T)})과 반환할 데이터({data.GetType()})가 다릅니다.");
            return (false, default);
        }

        public override (bool isSuccess, T result) TryUse<T>(IRoomDef roomDef) => FastReturnFalse<T>();

        public override (bool isSuccess, T result) TryUse<T>(IRoom room) => FastReturnFalse<T>();
    }
}
