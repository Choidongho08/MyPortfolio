using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies.Technologia
{
    public class TechnologiaSynergyEffect : SynergyEffectComponent
    {
        [SerializeField] private float timeDuration;

        [Header("Thunder")]
        [SerializeField] private GameObject thunderEffect;
        [SerializeField] private TechnologiaTableSO technologiaTableSO;
        [SerializeField] private LayerMask enemyLayer;

        private float timer;
        private bool isActiveSynergy;
        private List<Enemy> currentEnemies;

        protected override void Awake()
        {
            base.Awake();
            isActiveSynergy = false;
            Bus<CurrentEnemiesResponseEvent>.OnEvent += HandleCurrentEenmiesResponseEvent;
            Bus<CurrentWaveResponse>.OnEvent += HandleCurrentWaveEvent;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Bus<CurrentEnemiesResponseEvent>.OnEvent -= HandleCurrentEenmiesResponseEvent;
            Bus<CurrentWaveResponse>.OnEvent -= HandleCurrentWaveEvent;
        }

        private void HandleCurrentWaveEvent(CurrentWaveResponse _evt)
        {
            int wave = _evt.CurrentWave;
            int enemyCount = currentEnemies.Count;
            int randValue = UnityEngine.Random.Range(0, enemyCount);
            ThunderData thunerData = technologiaTableSO.GetTechnologiaDataForWave(technologiaTableSO.technologiaDataList.Count > wave ? wave : technologiaTableSO.technologiaDataList.Count);
            Enemy targetEnemy = currentEnemies[randValue];
            thunderEffect.transform.position = targetEnemy.transform.position;
            thunderEffect.SetActive(true); // 이펙트 재생
            Collider[] result = Physics.OverlapSphere(targetEnemy.transform.position, thunerData.Radius, enemyLayer); // Alloc은 짜피 배열길이가 변동되므로 사용하지 않았음.
            foreach (Collider collider in result)
            {
                if (collider.TryGetComponent(out Enemy enemy))
                {
                    enemy.KnockDown(thunerData.KnockDownDuration);
                    enemy.GetCompo<EntityHealthComponent>().GetDamage(thunerData.Damage);
                }
            }
        }

        private void HandleCurrentEenmiesResponseEvent(CurrentEnemiesResponseEvent _evt)
        {
            // table에서 wave넣고 value가져와서 하기 음

            currentEnemies = _evt.CurrentEnemies;
            Bus<CurrentWaveRequest>.Invoke(new());
        }

        private void Update()
        {
            if (!isActiveSynergy)
                return;

            timer += Time.deltaTime;
            if (timer > timeDuration)
            {
                ThunderMethod();
            }
        }

        private void ThunderMethod()
        {
            Bus<CurrentEnemiesRequestEvent>.Invoke(new());
        }

        protected override void SynergyActiveMethod(Synergy synergy)
        {
            isActiveSynergy = true;
        }
    }
}
