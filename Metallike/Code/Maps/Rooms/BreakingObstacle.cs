using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using System;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    [RequireComponent(typeof(BoxCollider))]
    public class BreakingObstacle : Entity, IDamageable
    {
        public int Id => GetInstanceID();

        protected BreakingObstacleEventData debrisData;

        protected override void Awake()
        {
            base.Awake();

            // 렌더러 캐싱 및 초기 데이터 설정
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                debrisData.Material = renderer.material;
            }
        }

        public virtual void ApplyDamage(DamageData damageData, Vector3 hitPoint, Vector3 hitNormal, AttackDataSO attackData, Entity dealer)
        {
            Debug.Log($"Obstacle Break! ID: {Id}");

            debrisData.Position = hitPoint;
            debrisData.Direction = hitNormal;
            debrisData.Size = transform.lossyScale; // [추가] 크기 정보 전달
            debrisData.Id = Id;

            OnHitEvent?.Invoke();
            Bus<BreakObstacleEvent>.OnEvent?.Invoke(new(debrisData));

            gameObject.SetActive(false);
        }
    }
}