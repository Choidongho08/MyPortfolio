using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using GondrLib.ObjectPool.RunTime;
using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class BoomBreakingObstacle : BreakingObstacle
    {
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float radius;
        [SerializeField] private AttackDataSO attackData;
        [SerializeField] private float damage = 100f;
        [SerializeField] private PoolItemSO vfxItem;
        [SerializeField] private float vfxDuration = 3f;

        private Collider[] colliders;

        protected override void Awake()
        {
            base.Awake();

            colliders = new Collider[20];
        }

        public override void ApplyDamage(DamageData damageData, Vector3 hitPoint, Vector3 hitNormal, AttackDataSO attackData, Entity dealer)
        {
            Boom();
            Bus<BreakObstacleVFXEvent>.OnEvent?.Invoke(new(vfxItem, transform.position, Quaternion.identity, vfxDuration));
            base.ApplyDamage(damageData, hitPoint, hitNormal, attackData, dealer);
        }

        private void Boom()
        {
            Array.Clear(colliders, 0, 20);

            int cnt = Physics.OverlapSphereNonAlloc(transform.position, radius, colliders, playerLayer);
            if (cnt == 0)
                return;

            DamageData data = new DamageData() { damage = damage, isCritical = false, damageType = DamageType.None };
            foreach(var collider in colliders)
            {
                if (collider == null)
                    break;

                if (collider.gameObject == gameObject)
                    continue;

                int childCnt = collider.transform.childCount;
                for (int i = 0; i < childCnt; i++)
                {
                    if (collider.transform.GetChild(i).TryGetComponent<IDamageable>(out var damageable))
                    {
                        damageable.ApplyDamage(data, transform.position, Vector3.up, attackData, this);
                    }
                    else if(collider.transform.TryGetComponent<IDamageable>(out var damageable2))
                    {
                        damageable2.ApplyDamage(data, transform.position, Vector3.up, attackData, this);
                    }
                }
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

#endif
    }
}
