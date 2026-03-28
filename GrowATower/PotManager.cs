using _01.Member.SD._01.Code.Unit.Interface;
using _01.Member.SD._01.Code.Unit.Interface.UnitSupport;
using Ami.BroAudio;
using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using Assets._01.Member.CDH.Code.MainMenus;
using Assets._01.Member.CDH.Code.UIs;
using GondrLib.Dependencies;
using GondrLib.ObjectPool.RunTime;
using Jsons;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Cores
{
    [Serializable]
    public struct UnitSaveData
    {
        public int unitId;

        public UnitSaveData(int unitId)
        {
            this.unitId = unitId;
        }
    }

    public class PotManager : MonoBehaviour
    {
        [SerializeField] private SceneLoader loader;
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundID clickSoundID;
        [SerializeField] private SoundID potSceneBgmID;
        [SerializeField] private CharacterSelectComponent selectComponent;
        [SerializeField] private Transform posesParent;
        [SerializeField] private UsingStatListSO unitListSO;
        [SerializeField] private LayerMask unitLayer;
        [SerializeField] private PoolItemSO popUpItem;
        [SerializeField] private Transform popUpParent;
        [SerializeField] private GameObject roundDecalPrefab;
        [SerializeField] private UnitTooltipUI unitTooltipUI;
        [SerializeField] private GameObject emptyTooltipText;

        [Inject] private PoolManagerMono poolManager;

        private List<UnitSaveData> currentUnits = new();

        private const string POT_MANAGER_SAVE_NAME = "PotManager_Units";
        private UsingStat selectedUnit;
        private bool isPotScene;
        private RoundDecal roundDecal;
        private List<ValueTuple<int, Vector3>> unitsPosList;

        protected void Awake()
        {
            Debug.Log("PotManager Awake : " + name);

            // Decal
            GameObject tempDecal = Instantiate(roundDecalPrefab);
            roundDecal = tempDecal.GetComponent<RoundDecal>();
            roundDecal.SetDecalSize(3.0f);
            roundDecal.SetProjectionActive(false);
            roundDecal.SetMatState(DecalColorEnum.WHITE);

            LoadUnitList();
            unitsPosList = new();

            if (SceneData.Instance.UnitDataForPotManager != null)
                SaveSelectedUnit(SceneData.Instance.UnitDataForPotManager);

            SettingUnits();

            isPotScene = true;
            Bus<SceneLoadEvent>.OnEvent += HandleSceneLoadEvent;

            soundChannel.Invoke(SoundEvents.BGMEvent.Initialize(potSceneBgmID));
        }

        private void Start()
        {
            // tooltipPopUp
            unitTooltipUI.SetActive(false);

            Debug.Assert(poolManager != null, "poolManager is null");
            PopupUI sceneDescriptionPopup = poolManager.Pop<PopupUI>(popUpItem);
            const string sceneDescriptionStr = "PotScene";
            const string sceneDescriptionDescriptionStr = "이곳은 게임 끝나고 가지고 온 유닛을\n저장하는 공간입니다. 유닛을 선택하면\n게임에 가지고 갈 수 있습니다.";
            sceneDescriptionPopup.Setting(700f, sceneDescriptionStr, sceneDescriptionDescriptionStr, PU_BTN_SET.OK, popUpParent, async (b) =>
            {
                await InitializeAsync();
            });
            sceneDescriptionPopup.transform.localPosition = Vector3.zero;
        }

        private async Task InitializeAsync()
        {
            const string cursorImageName = "Sab";
            Bus<SetCursorEvent>.Invoke(new SetCursorEvent(CursorSpot.LowerLeft, cursorImageName));

            while (isPotScene)
            {
                // 유닛 클릭 대기 (비동기)
                BaseUnit unit = await MouseSelectManager.Instance.SetMouseClick<BaseUnit>(unitLayer);
                soundChannel.Invoke(SoundEvents.PlayEvent.Initialize(clickSoundID, transform));

                if (!isPotScene || unit == null)
                    continue;

                // UI 초기화
                unitTooltipUI.SetActive(false);
                roundDecal.SetProjectionActive(false);

                // 유닛 선택 처리
                selectedUnit = unit.MyInfo;
                SceneData.Instance.UnitDataForPotManager = selectedUnit;
                int unitId = unitListSO.GetUnitId(selectedUnit);
                selectComponent.SelectUnit(unit.MyInfo, unit.gameObject);

                unitTooltipUI.SetTexts(unit.MyInfo.unitName, $"{(UnitType)unit.MyInfo.type}\n\n{unit.MyInfo.unitDescription}");
                unitTooltipUI.SetActive(true);

                // 유닛 위치 찾기 및 표시
                foreach (var (id, pos) in unitsPosList)
                {
                    if (unitId == id)
                    {
                        roundDecal.transform.position = pos + new Vector3(0f, 0.5f, 0f);
                        roundDecal.SetProjectionActive(true);

                        break;
                    }
                }

                await Task.Yield(); // 한 프레임 양보
            }
        }

        private void HandleSceneLoadEvent(SceneLoadEvent _evt)
        {
            isPotScene = false;
            if (selectedUnit is not null)
                JsonSaveManager.Instance.DeleteValue<UnitSaveData>(POT_MANAGER_SAVE_NAME, new(unitListSO.GetUnitId(selectedUnit)));
            Bus<SceneLoadEvent>.OnEvent -= HandleSceneLoadEvent;
            MouseSelectManager.Instance.StopMouseClick();
            Bus<SetCursorEvent>.Invoke(new SetCursorEvent(CursorSpot.Default));
        }

        #region === 외부에서 호출할 단일 저장 함수 ===

        /// <summary>
        /// 외부에서 PotManager.Instance.SaveSelectedUnit(selectedUnit) 호출하면
        /// </summary>
        private void SaveSelectedUnit(UsingStat selectedUnit)
        {
            if (selectedUnit == null || selectedUnit.unitName == null)
            {
                Debug.LogWarning("[PotManager] 저장하려는 유닛이 null입니다.");
                return;
            }

            int id = unitListSO.GetUnitId(selectedUnit);
            foreach (var (key, value) in unitListSO.GetUnitIdDict())
            {
                if (key == id)
                {
                    UnitSaveData unitSaveData = new(key);
                    currentUnits.Add(unitSaveData);
                    JsonSaveManager.Instance.Add(POT_MANAGER_SAVE_NAME, unitSaveData);
                    break;
                }
            }

            Debug.Log($"[PotManager] 선택된 유닛 '{selectedUnit.unitName}' 저장 완료.");
        }

        #endregion


        private void LoadUnitList()
        {
            if (!JsonSaveManager.Instance.HasKey(POT_MANAGER_SAVE_NAME))
            {
                emptyTooltipText.SetActive(true);
                return;
            }

            List<UnitSaveData> dataList = JsonSaveManager.Instance.LoadList<UnitSaveData>(POT_MANAGER_SAVE_NAME);
            currentUnits.Clear();

            foreach (var data in dataList)
            {
                if (unitListSO.GetUnitIdDict().ContainsKey(data.unitId))
                    currentUnits.Add(data);
            }

            if (currentUnits.Count < 1) emptyTooltipText.SetActive(true);
            else
            {
                Debug.LogWarning("Tq");
                emptyTooltipText.SetActive(false);
            }

            Debug.Log($"[PotManager] {currentUnits.Count}개의 유닛 로드 완료.");
        }

        private void SettingUnits()
        {
            int cnt = posesParent.childCount;
            for (int i = 0; i < cnt && i < currentUnits.Count; i++)
            {
                Transform pos = posesParent.GetChild(i);
                if (unitListSO.GetUnitIdDict().TryGetValue(currentUnits[i].unitId, out var unit))
                {
                    GameObject newUnit = Instantiate(unit.unitPrefab, pos.position, Quaternion.identity);
                    int id = unitListSO.GetUnitId(unit);
                    unitsPosList.Add((id, newUnit.transform.position));
                }
            }
        }

        public void GameStart()
        {
            loader.OnClickStartBtn();
        }
    }
}
