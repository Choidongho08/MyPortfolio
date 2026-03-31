using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps.SecurityLevels
{
    [CreateAssetMenu(fileName = "SecurityLevelDataDictSO", menuName = "SO/CDH/SecurityLevelDataDictSO")]
    public class SecurityLevelDataDictSO : ScriptableObject
    {
        [SerializeField] private List<SecurityLevelDataSO> securityLevelDataList;

        private Dictionary<int, SecurityLevelDataSO> securityLevelDataListDict = new();

        private void OnEnable()
        {
            securityLevelDataListDict.Clear();
            foreach (var item in securityLevelDataList)
            {
                securityLevelDataListDict[item.Level] = item;
            }
        }

        public SecurityLevelDataSO GetSecurityLevelData(int targetLevel)
        {
            return securityLevelDataListDict.GetValueOrDefault(targetLevel);
        }
    }
}
