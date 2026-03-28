using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies.Technologia
{
    [Serializable]
    public struct TechnologiaData
    {
        public int Wave;
        public float Damage;
        public float KnockDownDuration;
        public float Range;
    }

    public struct ThunderData
    {
        public float Damage;
        public float KnockDownDuration;
        public float Radius;

        public ThunderData(float damage, float knockDownDuration, float range)
        {
            Damage = damage;
            KnockDownDuration = knockDownDuration;
            Radius = range;
        }
    }

    [CreateAssetMenu(fileName = "TechnologiaTable", menuName = "SO/Combat/TechnologiaTalbe")]
    public class TechnologiaTableSO : ScriptableObject
    {
        public List<TechnologiaData> technologiaDataList;

        public ThunderData GetTechnologiaDataForWave(int wave)
        {
            int index = technologiaDataList.FindIndex(data => data.Wave == wave);

            if (index < 0) return new(-1f,-1f, -1f);

            return new(technologiaDataList[index].Damage, technologiaDataList[index].KnockDownDuration, technologiaDataList[index].Range);
        }
    }
}
