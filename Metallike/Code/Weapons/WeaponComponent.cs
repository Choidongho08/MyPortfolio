using Assets.Work.CDH.Code.Weapons.Interfaces;
using Core.EventBus;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Assets.Work.CDH.Code.Weapons
{
    public abstract class WeaponComponent : MonoBehaviour, IEntityComponent, IChangableInfo
    {
        [SerializeField]
        private Transform _weaponHolder;
        [SerializeField] private Transform _meleeWeaponHolder;
        private WeaponDataSO _pendingData;
        [SerializeField] protected DualFloatingArmController _armController;

        public List<string> _modulenames { get; private set; }
        protected EntityStatCompo _statCompo;
        public EntityStatCompo StatCompo { get => _statCompo; }
        public struct ChangeWeaponEvent
        {
            public IWeapon prevWeapon;
            public IWeapon newWeapon;
        }

        public UnityEvent<ChangeWeaponEvent> OnChangeWeapon;

        public event Action OnMouseDown;
        public event Action OnHoldEnd;
        public event Action OnEndAttack;

        private IWeapon curWeapon;
        private Player owner;

        [SerializeField] private float dropDistance = 5f;
        public virtual async void Initialize(Entity entity)
        {
            owner = entity as Player;

            (owner as Player).PlayerInput.OnThrowPressed += HandleDropInput;
            _modulenames = new List<string>();
            Bus<ProjectileModuleEvent>.OnEvent += HandleProjectileEvt;
            // WeaponManager와 CurrentCharacter가 준비될 때까지 대기합니다.
            await UniTask.WaitUntil(() => WeaponManager.Instance != null && WeaponManager.Instance.CurrentCharacter != null);
            ChangeWeapon(WeaponManager.Instance.CurrentCharacter.defaultWeapon);
        }

        private void HandleProjectileEvt(ProjectileModuleEvent evt)
        {
            Debug.Log("ModuleAdded");
            _modulenames.Add(evt.Name);
        }

        protected virtual void OnDestroy()
        {
            (owner as Player).PlayerInput.OnThrowPressed -= HandleDropInput;
            Bus<ProjectileModuleEvent>.OnEvent -= HandleProjectileEvt;
        }

        public void HandleDropInput()
        {
            if(curWeapon.WeaponData != WeaponManager.Instance.DefaultWeapon)
            {
                DropWeapon(curWeapon);
                ChangeWeapon(WeaponManager.Instance.DefaultWeapon);

                WeaponManager.Instance.SetCharacterWeapon(null);
            }
        }
        public void PickUpWeapon(IPickUpable pickUp)
        {
            if (pickUp == null)
            {
                Debug.LogWarning("pickUp이 null 입니다.");
                return;
            }
            var data = pickUp.WeaponData;
            if (data == null || data.originalWeaponPrefab == null)
            {
                Debug.LogError("WeaponData 또는 originalWeaponPrefab이 null 입니다.");
                return;
            }
            if (curWeapon != null && curWeapon.WeaponData != WeaponManager.Instance.DefaultWeapon)//무기가 없거나 기본무기가 아닐때만 던지기
            {
                DropWeapon(curWeapon);
            }
            ChangeWeapon(data);

            Destroy(pickUp.Transform.gameObject);

            WeaponManager.Instance.SetCharacterWeapon(data);
        }
        public void ChangeWeapon(WeaponDataSO data)
        {
            _armController.SetArmsActive(false);
            _armController.StopAnimation();
            if (data == null || data.originalWeaponPrefab == null)
            {
                Debug.LogError("WeaponData 또는 originalWeaponPrefab이 null 입니다.");
                return;
            }

            // 홀더 준비 전이면 보류 → 이전 모델에 붙는 문제 차단
            if (_weaponHolder == null)
            {
                _pendingData = data;
                return;
            }

            var prev = curWeapon;

            // 이전 무기 정리
            curWeapon?.Release();


            curWeapon = null;

            if (prev != null)
            {
                Destroy(prev.Transform.gameObject);
            }

            // 항상 새 무기 인스턴스 생성
            var go = Instantiate(data.originalWeaponPrefab.gameObject);
            var newWeapon = go.GetComponent<IWeapon>();
            Debug.Assert(newWeapon != null, "무기 프리팹에 IWeapon 컴포넌트가 없습니다.");
            if(newWeapon is RangeWeapon)
            {
                newWeapon.Transform.SetParent( _weaponHolder, false);
                _armController.SetArmsActive(false);
            }
            else
            {
                newWeapon.Transform.SetParent(_meleeWeaponHolder, false);
                _armController.SetArmsActive(true);
            }
                // 부모 먼저 붙이기
            newWeapon.Transform.localPosition = Vector3.zero;
            newWeapon.Transform.localRotation = Quaternion.identity;

            newWeapon.Initialize(owner);
            Debug.Log(owner);

            curWeapon = newWeapon;
            Bus<WeaponUIEvents>.Raise(new WeaponUIEvents(curWeapon.WeaponData));
            OnChangeWeapon?.Invoke(new ChangeWeaponEvent
            {
                prevWeapon = prev,
                newWeapon = curWeapon
            });

            _armController.SetArmsActive(true);
        }

        private void DropWeapon(IWeapon prev)
        {
            Vector3 targetPos = GetRandomPosInCircle(dropDistance);

            GameObject dropWeapon = Instantiate(prev.WeaponData.pickUpWeaponPrefab);
            dropWeapon.transform.position = transform.position;
            dropWeapon.transform.DOJump(transform.position + targetPos, 2f, 1, 0.5f);
        }

        public Vector3 GetRandomPosInCircle(float radius)
        {
            // insideUnitCircle은 반지름 1인 원 안의 랜덤 Vector2(x, y)를 반환합니다.
            Vector2 randomPoint = Random.insideUnitCircle * radius;

            // 이를 3D 평면 좌표(X, Z)로 변환합니다.
            return new Vector3(randomPoint.x, 0, randomPoint.y);
        }
        public Vector3 GetRandomPosInSphere(float radius)
        {
            // insideUnitSphere는 반지름 1인 구체 안의 랜덤 좌표를 반환합니다.
            return Random.insideUnitSphere * radius;
        }
        [ContextMenu("TestAttack")]
        protected void AttackInvoke() 
        {
            OnMouseDown?.Invoke();
        }
        protected void EndChargeInvoke()
        {
            OnHoldEnd?.Invoke();
        }
        protected void AttackEndInvoke()
        {
            OnEndAttack?.Invoke();
        }

        public void Change(CharacterSO info)
        {
            WeaponDataSO weapon = WeaponManager.Instance.GetCharacterWeapon(info);
            ChangeWeapon(weapon);

            _modulenames.Clear();
        }
    }
}
