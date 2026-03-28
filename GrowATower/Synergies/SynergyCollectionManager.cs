using Jsons;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies
{
    public class SynergyCollectionManager : MonoSingleton<SynergyCollectionManager>
    {
        [SerializeField] private SynergyListSO synergyListSO;

        private const string SAVE_FILE_NAME = "SynergyCollection";
        private HashSet<string> _discoveredSynergies = new();

        protected override void Awake()
        {
            base.Awake();
            LoadCollection();
        }

        #region Public Methods

        /// <summary>
        /// 시너지를 도감에 추가합니다. 이미 발견된 시너지면 false를 반환합니다.
        /// </summary>
        public bool DiscoverSynergy(Synergy synergy)
        {
            string synergyId = GetSynergyId(synergy);
            if (_discoveredSynergies.Contains(synergyId))
            {
                Debug.Log($"이미 발견된 시너지: {synergyId}");
                return false;
            }

            _discoveredSynergies.Add(synergyId);
            SaveCollection();

            Debug.Log($"새로운 시너지 발견: {synergyId} (총 {_discoveredSynergies.Count}개)");
            return true;
        }

        /// <summary>
        /// 특정 시너지가 이미 발견되었는지 확인합니다.
        /// </summary>
        public bool IsSynergyDiscovered(Synergy synergy)
        {
            return _discoveredSynergies.Contains(GetSynergyId(synergy));
        }

        /// <summary>
        /// 발견한 시너지 목록을 반환합니다.
        /// </summary>
        public List<Synergy> GetDiscoveredSynergies()
        {
            if (synergyListSO == null)
            {
                Debug.LogWarning("[SynergyCollectionManager] synergyListSO가 null입니다!");
                return new List<Synergy>();
            }

            var discoveredList = new List<Synergy>();
            foreach (var item in synergyListSO.allSynergyList)
            {
                string id = GetSynergyId(item.synergy);
                if (_discoveredSynergies.Contains(id))
                    discoveredList.Add(item.synergy);
            }
            return discoveredList;
        }

        /// <summary>
        /// 전체 시너지 목록을 반환합니다 (미발견 포함).
        /// </summary>
        public List<Synergy> GetAllSynergies()
        {
            return synergyListSO == null
                ? new List<Synergy>()
                : synergyListSO.allSynergyList.Select(s => s.synergy).ToList();
        }

        /// <summary>
        /// 발견 진행률을 퍼센트로 반환합니다.
        /// </summary>
        public float GetDiscoveryProgress()
        {
            if (synergyListSO == null || synergyListSO.allSynergyList.Count == 0)
                return 0f;

            return (float)_discoveredSynergies.Count / synergyListSO.allSynergyList.Count * 100f;
        }

        [ContextMenu("ResetCollection")]
        /// <summary>
        /// 도감을 초기화합니다 (개발/테스트용).
        /// </summary>
        public void ResetCollection()
        {
            _discoveredSynergies.Clear();
            SaveCollection();
            Debug.Log("[SynergyCollectionManager] 시너지 도감 초기화 완료.");
        }

        [ContextMenu("DebugPrintAllIds")]
        public void DebugPrintAllIds()
        {
            Debug.Log("=== 저장된 시너지 ID 목록 ===");
            foreach (var id in _discoveredSynergies)
                Debug.Log($"저장된 ID: {id}");

            if (synergyListSO == null) return;

            Debug.Log("=== synergyListSO의 모든 시너지 ID ===");
            foreach (var item in synergyListSO.allSynergyList)
            {
                string id = GetSynergyId(item.synergy);
                bool isDiscovered = _discoveredSynergies.Contains(id);
                Debug.Log($"SO ID: {id} - 이름: {item.synergy.name} - 발견됨: {isDiscovered}");
            }
        }

        [ContextMenu("OpenSaveFolder")]
        public void OpenSaveFolder()
        {
            string folderPath = Application.persistentDataPath;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", folderPath);
#else
            Debug.Log($"저장 경로: {folderPath}");
#endif
        }

        #endregion

        #region Private Helpers

        private void SaveCollection()
        {
            SynergyCollectionData data = new SynergyCollectionData
            {
                discoveredSynergyIds = _discoveredSynergies.ToList()
            };
            JsonSaveManager.Instance.Save(SAVE_FILE_NAME, data);
            Debug.Log($"[SynergyCollectionManager] {_discoveredSynergies.Count}개 시너지 저장 완료.");
        }

        private void LoadCollection()
        {
            SynergyCollectionData data = JsonSaveManager.Instance.Load<SynergyCollectionData>(SAVE_FILE_NAME);
            _discoveredSynergies = data != null
                ? new HashSet<string>(data.discoveredSynergyIds)
                : new HashSet<string>();

            Debug.Log($"[SynergyCollectionManager] {_discoveredSynergies.Count}개 시너지 로드 완료.");
        }

        private string GetSynergyId(Synergy synergy)
        {
            if (synergy == null)
            {
                Debug.LogError("시너지가 null입니다!");
                return "null";
            }

            return GenerateHashBasedId(synergy);
        }

        private string GenerateHashBasedId(Synergy synergy)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 시너지 조건 정렬
            if (synergy.synergyConditions != null)
            {
                var sortedConditions = synergy.synergyConditions
                    .OrderBy(c => (int)c.type)
                    .ThenBy(c => c.cnt)
                    .ToList();

                foreach (var condition in sortedConditions)
                    sb.Append($"{(int)condition.type}_{condition.cnt}|");
            }

            sb.Append($"r{synergy.checkRadius:F3}|");

            // 시너지 스탯 정렬
            if (synergy.synergyStats != null)
            {
                var sortedStats = synergy.synergyStats.OrderBy(s => s.type.ToString()).ToList();
                foreach (var stat in sortedStats)
                    sb.Append($"{stat.type}:{stat.value}|");
            }

            if (!string.IsNullOrEmpty(synergy.description))
                sb.Append(synergy.description);

            // SHA256 해시 생성
            string combined = sb.ToString();
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
                System.Text.StringBuilder hashBuilder = new System.Text.StringBuilder();
                for (int i = 0; i < 8; i++)
                    hashBuilder.Append(hashBytes[i].ToString("x2"));
                return $"syn_{hashBuilder}";
            }
        }

        #endregion
    }
}