using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps.SecurityLevels
{
    [CreateAssetMenu(fileName = "SecurityLevelData", menuName = "SO/CDH/SecurityLevelData")]
    public class SecurityLevelDataSO : ScriptableObject
    {
        public int Level;
        public float EnemyHpMultiplier;
        public float EnemyAtkMultiplier;
        public float GoldDropBonus;
        [TextArea] public string[] SpecialPenalties; // "포격이 항시 당신을 겨누고 있습니다!" 등
    }
}
