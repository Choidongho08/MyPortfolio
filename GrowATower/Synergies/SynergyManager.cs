using _01.Member.SB._01.Code;
using _01.Member.SD._01.Code.Unit.Interface;
using _01.Member.SD._01.Code.Unit.Interface.UnitSupport;
using Ami.BroAudio;
using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using SynergyTypes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies
{
    public class SynergyManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundID synergyAlarmSoundID;
        [SerializeField] private EventChannelSO synergyChannel;
        [SerializeField] private EventChannelSO uiChannel;

        private readonly List<BaseUnit> _tempSynergyUnits = new List<BaseUnit>();
        private readonly Dictionary<Synergy, List<BaseUnit>> currentSynergiesAndUnits = new Dictionary<Synergy, List<BaseUnit>>();

        // 캐시/버퍼 (필드 재사용 → GC 0)
        private readonly List<CachedUnit> _cachedUnits = new List<CachedUnit>(); // 캐싱된 유닛들 
        private readonly HashSet<BaseUnit> _matchedSet = new HashSet<BaseUnit>(); // 조건 충족된 유닛들
        private readonly List<BaseUnit> _matchedList = new List<BaseUnit>(); // 최종 리스트

        private const int kMaxTypeBits = 32;
        private readonly List<int>[] _typeBuckets = CreateBuckets(); // 시너지 타입 비트별 유닛 인덱스

        private List<Synergy> discoveredSynergies { get { return SynergyCollectionManager.Instance.GetDiscoveredSynergies(); } }
        private List<Synergy> allSynergies { get { return SynergyCollectionManager.Instance.GetAllSynergies(); } }

        // xz 좌표 + 시너지 타입 캐싱 (물리 사용 안 함)
        private struct CachedUnit
        {
            public BaseUnit unit;
            public Vector2 posXZ;
            public SynergyType type;
        }

        private void Awake()
        {
            synergyChannel.AddListener<CheckSynergyEvent>(HandleFindSynergy);

            Bus<OpenSynergyCollectionUI>.OnEvent += OpenSynergyCollection;
            Bus<CloseSynergyCollectionUI>.OnEvent += CloseSynergyCollection;

            // 조건을 cnt 내림차순으로 사전 정렬해서 빠른 실패 유도
            PreprocessSynergies();
        }

        private void OnDestroy()
        {
            synergyChannel.RemoveListener<CheckSynergyEvent>(HandleFindSynergy);
            Bus<OpenSynergyCollectionUI>.OnEvent -= OpenSynergyCollection;
            Bus<CloseSynergyCollectionUI>.OnEvent -= CloseSynergyCollection;
        }

        public void HandleFindSynergy(CheckSynergyEvent evt)
        {
            List<BaseUnit> currentAllUnits = evt.units;
            if (currentAllUnits == null || currentAllUnits.Count == 0) // 현재 유닛이 없다면 체크할 시너지도 없으니까 리턴.
                return;

            FindCancelableSynergy(); // 취소할 시너지 체크
            FindNewSynergies(currentAllUnits); // 새로운 시너지 찾기
        }

        // ────────────────────────────────────────────────
        // 기존 발동 중인 시너지 취소(사망 유닛 포함 시)
        // ────────────────────────────────────────────────
        List<Synergy> toCancel = new(); // 취소할 시너지를 임시 저장하는 변수
        private void FindCancelableSynergy()
        {
            foreach (KeyValuePair<Synergy, List<BaseUnit>> kvp in currentSynergiesAndUnits)
            {
                Synergy synergy = kvp.Key;
                List<BaseUnit> synergyUnits = kvp.Value;

                bool stillValid = true;
                foreach (BaseUnit unit in synergyUnits)
                {
                    if (unit == null || unit._plantWaterComponent.CurremtWaterState == WaterState.Dead)
                    {
                        stillValid = false;
                        break;
                    }
                }
                if (!stillValid) toCancel.Add(synergy);
            }

            foreach (Synergy synergy in toCancel)
            {
                CancelSynergy(synergy);
                currentSynergiesAndUnits.Remove(synergy);
                Debug.Log($"<color=red>시너지 취소! : {synergy.name}</color>");
            }
            toCancel.Clear(); // 마지막에 Clear하는게 시너지들 취소하고 데이터 안남길려고 했음.
        }

        // ────────────────────────────────────────────────
        // 새 시너지 탐색 (Vector2 캐싱 + 타입 버킷 + 2-패스)
        // ────────────────────────────────────────────────
        private void FindNewSynergies(List<BaseUnit> units)
        {
            _cachedUnits.Clear(); // 캐시된 유닛 초기화
            for (int b = 0; b < kMaxTypeBits; b++) _typeBuckets[b].Clear(); // 체크할 버킷 초기화

            foreach(BaseUnit unit in units)
            {
                if (unit == null || unit._plantWaterComponent.CurremtWaterState == WaterState.Dead) // 유닛이 없거나 죽은 상태이면 다음 유닛으로
                    continue;

                Vector3 pos = unit.transform.position;
                CachedUnit cu = new CachedUnit
                {
                    unit = unit,
                    posXZ = new Vector2(pos.x, pos.z),
                    type = unit.unitSynergyType
                };

                _cachedUnits.Add(cu); // 캐시한 유닛 추가
                int idx = _cachedUnits.Count - 1; // 버킷에서 사용할 인덱스라 -1

                // 멀티 타입이면 여러 버킷에 인덱스 추가
                int mask = (int)cu.type;
                for (int bit = 0; bit < kMaxTypeBits; bit++) // 모든 32개의 bit돌면서
                {
                    if ((mask & (1 << bit)) != 0) // mask = 1 << n(bit)일 때 add해줌.
                        _typeBuckets[bit].Add(idx);
                }
            }

            // 2) 모든 시너지 검사
            foreach(Synergy synergy in allSynergies)
            {
                if (currentSynergiesAndUnits.ContainsKey(synergy)) // 이미 적용된 시너지라면 continue;
                    continue;

                bool fail = false; // 실패했나 확인 불 변수
                _matchedSet.Clear(); // HashSet 초기화

                float checkRadius = synergy.checkRadius * synergy.checkRadius;
                List<SynergyCondition> conditions = synergy.synergyConditions; // Preprocess에서 이미 내림차순 정렬됨

                foreach(SynergyCondition condition in conditions)
                {
                    bool conditionMeet = false; // 컨디션의 조건이 충족되었나 확인 불 변수
                    int targetMask = (int)condition.type; // 
                    int bitIndex;

                    if (TryGetSingleBitIndex(targetMask, out bitIndex))
                    {
                        // 단일 타입 → 해당 버킷만 순회 (후보 축소)
                        List<int> centers = _typeBuckets[bitIndex];

                        for (int ci = 0; ci < centers.Count && !conditionMeet; ci++)
                        {
                            int i = centers[ci];
                            CachedUnit center = _cachedUnits[i];

                            // 1차 패스: 카운트만 (빠른 실패)
                            int count = 1; // center는 이미 cond 타입(버킷)
                            for (int cj = 0; cj < centers.Count && count < condition.cnt; cj++)
                            {
                                if (ci == cj) continue;
                                CachedUnit other = _cachedUnits[centers[cj]];
                                float dist2 = (center.posXZ - other.posXZ).sqrMagnitude;
                                if (dist2 <= checkRadius) count++;
                            }

                            if (count >= condition.cnt)
                            {
                                // 2차 패스: 실제 수집 (필요한 수만)
                                _matchedSet.Add(center.unit);
                                int need = condition.cnt - 1;
                                for (int cj = 0; cj < centers.Count && need > 0; cj++)
                                {
                                    if (ci == cj) continue;
                                    CachedUnit other = _cachedUnits[centers[cj]];
                                    float dist2 = (center.posXZ - other.posXZ).sqrMagnitude;
                                    if (dist2 <= checkRadius)
                                    {
                                        _matchedSet.Add(other.unit);
                                        need--;
                                    }
                                }
                                conditionMeet = true;
                            }
                        }
                    }
                    else
                    {
                        // 복합 플래그 → 안전하게 전체 순회
                        for (int i = 0; i < _cachedUnits.Count && !conditionMeet; i++)
                        {
                            CachedUnit center = _cachedUnits[i];

                            bool centerMatch = (((int)center.type & targetMask) != 0);
                            int count = centerMatch ? 1 : 0;

                            // 1차 패스: 카운트만
                            for (int j = 0; j < _cachedUnits.Count && count < condition.cnt; j++)
                            {
                                if (i == j) continue;
                                CachedUnit other = _cachedUnits[j];
                                if ((((int)other.type & targetMask) == 0)) continue;

                                float dist2 = (center.posXZ - other.posXZ).sqrMagnitude;
                                if (dist2 <= checkRadius) count++;
                            }

                            if (count >= condition.cnt)
                            {
                                // 2차 패스: 실제 수집
                                if (centerMatch) _matchedSet.Add(center.unit);
                                int need = condition.cnt - (centerMatch ? 1 : 0);

                                for (int j = 0; j < _cachedUnits.Count && need > 0; j++)
                                {
                                    if (i == j) continue;
                                    CachedUnit other = _cachedUnits[j];
                                    if ((((int)other.type & targetMask) == 0)) continue;

                                    float dist2 = (center.posXZ - other.posXZ).sqrMagnitude;
                                    if (dist2 <= checkRadius)
                                    {
                                        _matchedSet.Add(other.unit);
                                        need--;
                                    }
                                }
                                conditionMeet = true;
                            }
                        }
                    }

                    if (!conditionMeet) { fail = true; break; }
                }

                if (fail) continue;

                // 최종 매칭 확정
                _matchedList.Clear();
                foreach (BaseUnit u in _matchedSet) _matchedList.Add(u);

                currentSynergiesAndUnits.Add(synergy, new List<BaseUnit>(_matchedList)); // 스냅샷 저장
                _tempSynergyUnits.Clear();
                _tempSynergyUnits.AddRange(_matchedList);

                if (!SynergyCollectionManager.Instance.IsSynergyDiscovered(synergy))
                {
                    SynergyCollectionManager.Instance.DiscoverSynergy(synergy);
                    Debug.Log($"<color=yellow>새로운 시너지 발견! : {synergy.name}</color>");
                }
                Debug.Log($"<color=green>시너지 발동! : {synergy.name}</color>");
                soundChannel.Invoke(SoundEvents.PlayEvent.Initialize(synergyAlarmSoundID, transform));
                synergyChannel.Invoke(SynergyEvents.SynergyDiscoveredEvent.Initalizer(synergy));

                ApplySynergy(synergy);
                Bus<SynergyActiveEvent>.Invoke(new SynergyActiveEvent(synergy, _matchedList));
            }
        }

        // ────────────────────────────────────────────────
        // 적용/취소 공통 루틴
        // ────────────────────────────────────────────────
        private void ExecuteSynergyEffect(Synergy synergy, Action<BaseUnit> effect)
        {
            if (synergy == null)
            {
                Debug.LogError("ExecuteSynergyEffect: synergy가 null입니다!");
                return;
            }
            if (synergy.synergyStats == null)
            {
                Debug.LogError($"시너지 {synergy.name}의 synergyStats가 null입니다!");
                return;
            }

            List<BaseUnit> targets = synergy.isStatApplyOfAllUnits
                ? UnitManager.Instance.GetAllUnits()
                : _tempSynergyUnits;

            for (int i = 0; i < targets.Count; i++)
            {
                BaseUnit unit = targets[i];
                if (unit == null || unit.unitStatCompo == null) continue;
                effect(unit);
            }
        }

        private void ApplySynergy(Synergy synergy)
        {
            ExecuteSynergyEffect(synergy, delegate (BaseUnit unit) { ApplyStat(synergy, unit); });
        }

        private void CancelSynergy(Synergy synergy)
        {
            ExecuteSynergyEffect(synergy, delegate (BaseUnit unit) { CancelStat(synergy, unit); });
        }

        private void ApplyStat(Synergy synergy, BaseUnit unit)
        {
            unit.unitStatCompo.IsSynergy = true;

            if (!string.IsNullOrEmpty(synergy.vfxName))
                unit.EntityVFX.PlayVfx(synergy.vfxName, unit.transform.position, Quaternion.identity);

            foreach (SynergyStat stat in synergy.synergyStats)
                unit.unitStatCompo.SetStat(stat.type, stat.value, (StatModifyType)(int)stat.modifyType);
        }

        private void CancelStat(Synergy synergy, BaseUnit unit)
        {
            unit.unitStatCompo.IsSynergy = false;

            foreach (SynergyStat stat in synergy.synergyStats)
            {
                if (stat.modifyType == ModifyType.Set) continue;

                // Add/Multiply 등 역적용 규칙 (짝수→+1, 이전 로직 유지)
                StatModifyType reverse =
                    (StatModifyType)((int)stat.modifyType % 2 == 0 ? stat.modifyType + 1 : stat.modifyType);

                unit.unitStatCompo.SetStat(stat.type, stat.value, reverse);
            }
        }

        // ────────────────────────────────────────────────
        // UI
        // ────────────────────────────────────────────────
        [ContextMenu("OpenSynergyCollection")]
        public void TestOpen()
        {
            OpenSynergyCollection(new OpenSynergyCollectionUI());
        }

        public void OpenSynergyCollection(OpenSynergyCollectionUI evt)
        {
            synergyChannel.Invoke(SynergyEvents.OpenSynergyCollectionEvent.Initalizer(discoveredSynergies));
        }

        [ContextMenu("CloseSynergyCollection")]
        public void TestClose()
        {
            CloseSynergyCollection(new CloseSynergyCollectionUI());
        }

        public void CloseSynergyCollection(CloseSynergyCollectionUI evt)
        {
            synergyChannel.Invoke(SynergyEvents.CloseSynergyCollectionEvent.Initalizer());
        }

        [ContextMenu("RefreshSynergyCollection")]
        public void RefreshSynergyCollection()
        {
            synergyChannel.Invoke(SynergyEvents.RefreshSynergyCollectionEvent.Initalizer(discoveredSynergies));
        }

        // ────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────
        private static List<int>[] CreateBuckets()
        {
            List<int>[] arr = new List<int>[kMaxTypeBits];
            for (int i = 0; i < kMaxTypeBits; i++)
                arr[i] = new List<int>(64);
            return arr;
        }

        // 조건이 단일 타입일 땐 해당 비트 버킷만 순회해서 후보를 확 줄이려고 “단일 비트인지” 검사
        private static bool TryGetSingleBitIndex(int mask, out int bitIndex)
        {
            // power-of-two 체크
            if (mask != 0 && (mask & (mask - 1)) == 0) // 1(체크된 Flag가)이 1개만 있으면 모두 0으로 나오고 2개 + 인덱스 체크후 true반환이지만 1(체크된 Flag가)이 2개 이상이면 다른 1들이 남아서 바로 false반환
            {
                for (int i = 0; i < 32; i++)
                {
                    if ((mask & (1 << i)) != 0)
                    {
                        bitIndex = i;
                        return true;
                    }
                }
            }
            bitIndex = -1;
            return false;
        }

        private void PreprocessSynergies()
        {
            for (int i = 0; i < allSynergies.Count; i++)
            {
                Synergy syn = allSynergies[i];
                if (syn == null || syn.synergyConditions == null)
                    continue;
                syn.synergyConditions.Sort(delegate (SynergyCondition a, SynergyCondition b)
                {
                    return b.cnt.CompareTo(a.cnt); // cnt 큰 조건 먼저
                    // Condition에서 cnt가 크다는 것은 검사할 조건이 많다는 뜻. -> 실패할 확률이 높아서 빠른 리턴이 가능함.
                });
            }
        }
    }
}
