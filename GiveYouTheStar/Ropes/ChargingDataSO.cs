using UnityEngine;

namespace Assets._01.Develop.CDH.Code.Ropes
{
    [CreateAssetMenu(fileName = "ChargingDataSO", menuName = "SO/CDH/ChargingData")]
    public class ChargingDataSO : ScriptableObject
    {
        public AnimationCurve chargeCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
}
