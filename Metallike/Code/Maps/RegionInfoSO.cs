using Assets.Work.CDH.Code.UIs.Maps;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    [CreateAssetMenu(fileName = "RegionInfoSO ", menuName = "SO/CDH/RegionInfoSO ")]
    public class RegionInfoSO : ScriptableObject
    {
        [field: SerializeField] public BossIconInfo BossIconInfo { get; private set; }
        [field: SerializeField] public IconInfo CharacterTypeInfo { get; private set; }
        [field: SerializeField] public IconInfo[] ModuleIconInfos { get; private set; }
    }
}
