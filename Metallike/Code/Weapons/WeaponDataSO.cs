using Code.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    [Serializable]
    public struct StatData
    {
        public StatSO ModifyStat;
        public float ModifyValue;
    }
    [CreateAssetMenu(fileName = "WeaponData", menuName = "SO/CDH/WeaponData")]
    public class WeaponDataSO : ScriptableObject, IContextFeature
    {
        public string weaponName;
        public string description;
        public Mythical mythical;
        public GameObject originalWeaponPrefab;
        private Weapon weapon;
        public WeaponType type;
        public List<PlayerClass> characterInfo;
        public float ModifiedValue 
        { get
            {
                if (weapon)
                    return _statDataLookUp["ATTACKDAMAGE"].ModifyValue;
                else
                    return 0f;
            }
        }
        public float BatteryUseValue 
        { get
            {
                if (weapon)
                    return _statDataLookUp["BATTERYUSE"].ModifyValue;
                else
                    return 0f;
            }
        }
        public GameObject pickUpWeaponPrefab;
        public Sprite weaponIcon;
        public RangeMarkData rangeMarkData;

        
        [Header("Weapon Data Section")]
        public List<StatData> _statDataList;
        public List<WeaponAdditionalDataSO> _additionalDatas;

        public Dictionary<Type, WeaponAdditionalDataSO> _additionalDataLookup;
        public Dictionary<string, StatData> _statDataLookUp;

        private void OnEnable()
        {
            if(originalWeaponPrefab != null)
            {
                weapon = originalWeaponPrefab.GetComponent<Weapon>();
            }
            _additionalDataLookup = _additionalDatas.ToDictionary((d) => d.GetType());
            _statDataLookUp = _statDataList.ToDictionary((d) => d.ModifyStat.statName);
        }

        public T GetAdditionalData<T>() where T : WeaponAdditionalDataSO
        {
            if (_additionalDataLookup.TryGetValue(typeof(T), out WeaponAdditionalDataSO value))
            {
                return (T)value;
            }
            else
            {
                return default(T);
            }
        }
    }
}

public enum WeaponType
{
    Melee,
    MeleeCharge,
    Range,
    RangeCharge,
}
