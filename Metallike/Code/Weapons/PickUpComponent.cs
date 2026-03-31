using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Weapons.Interfaces;
using Code.Interface;
using Core.EventBus;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Work.CDH.Code.Weapons
{
    public class PickUpComponent : MonoBehaviour, IEntityComponent
    {
        [Header("Settings")]
        [SerializeField] private Transform checkPosition;
        [field: SerializeField] public float CheckRadius { get; set; } = 2f;
        [SerializeField] private LayerMask pickUpLayer;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent<IPickUpable> OnPickUpWeapon;

        private Entity owner;
        private IInteractable currentTarget; // 현재 가장 가까운 인터랙션 대상
        private Collider[] results = new Collider[16];

        private const float checkInterval = 0.15f; // 약간 더 기민하게 반응하도록 조정
        private float lastCheckTime = 0;

        public void Initialize(Entity _entity)
        {
            owner = _entity;
            if (checkPosition == null) checkPosition = transform;
        }

        private void Update()
        {
            // 1. 입력 감지
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                PerformInteraction();
            }

            // 2. 일정 시간마다 주변 탐색 (성능 최적화)
            if (Time.time >= lastCheckTime + checkInterval)
            {
                UpdateNearestTarget();
                lastCheckTime = Time.time;
            }
        }

        private void UpdateNearestTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(checkPosition.position, CheckRadius, results, pickUpLayer);

            IInteractable closestInteractable = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                // Collider로부터 상호작용 가능한 컴포넌트 추출
                if (TryGetInteractable(results[i], out var interactable))
                {
                    float distance = Vector3.Distance(checkPosition.position, results[i].transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            // 대상이 바뀌었을 때만 Enter/Exit 호출
            if (closestInteractable != currentTarget)
            {
                currentTarget?.ExitInteractionRange();

                currentTarget = closestInteractable;

                currentTarget?.EnterInteractionRange();
                
                if (currentTarget is IPickUpable pickUp)
                {
                    if (pickUp.WeaponData == null)
                    {
                        UIInfoManager.Instance.CanPick(false);
                        return;
                    }
                    if (pickUp.WeaponData.characterInfo.Contains((owner as Player).CurrentCharacter.myClass))
                    {
                        UIInfoManager.Instance.CanPick(false);
                    }
                    else
                    {
                        UIInfoManager.Instance.CanPick(true);
                    }
                }
            }

            // 사용 후 배열 정리 (NonAlloc 효율성 유지)
            System.Array.Clear(results, 0, count);
        }

        private void PerformInteraction()
        {
            if (currentTarget == null) return;

            // 1. 공통 상호작용 실행 (애니메이션, 사운드 등)
            currentTarget.OnInteract(owner);

            // 2. 만약 대상이 픽업 가능한 아이템(IPickUpable)이라면 픽업 로직 수행
            if (currentTarget is IPickUpable pickUp)
            {
                if(pickUp.WeaponData.characterInfo.Contains((owner as Player).CurrentCharacter.myClass))
                {
                    HandlePickUp(pickUp);
                }
            }
        }

        private void HandlePickUp(IPickUpable pickUp)
        {
            // 이벤트 발행 및 데이터 처리
            OnPickUpWeapon?.Invoke(pickUp);

            // Interaction 범위 탈출 처리 (삭제 전 상태 정리)
            pickUp.ExitInteractionRange();

            // 이벤트 버스 알림 및 오브젝트 파괴
            GameObject targetObj = pickUp.Transform.gameObject;
            Bus<RoomObjRemoveEvent>.Raise(new RoomObjRemoveEvent(targetObj));

            // 현재 타겟 초기화 (파괴될 것이므로)
            currentTarget = null;

            Destroy(targetObj);
        }

        private bool TryGetInteractable(Collider collider, out IInteractable interactable)
        {
            // GetComponent의 성능 최적화를 위해 TryGetComponent 활용
            if (collider.TryGetComponent(out interactable)) return true;

            interactable = collider.GetComponentInParent<IInteractable>();
            if (interactable != null) return true;

            interactable = collider.GetComponentInChildren<IInteractable>();
            return interactable != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (checkPosition == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(checkPosition.position, CheckRadius);

            if (currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(checkPosition.position, (currentTarget as MonoBehaviour).transform.position);
            }
        }
#endif
    }
}