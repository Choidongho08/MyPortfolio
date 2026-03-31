using Core.EventBus;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    [Serializable]
    public struct VFXInfo
    {
        public string vfxName;
        public BezierPoint vfxPosAndRot;
        public bool isOwnerVFX;
        public Vector3 size;
        public bool useWeaponDirection;
    }

    public abstract class AbstractWeapon : MonoBehaviour, IWeapon
    {
        [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }
        [SerializeField] protected DamageCaster damageCaster;
        [SerializeField] protected CinemachineImpulseSource impulseSource;
        [SerializeField] protected Transform subArmSocket;
        [SerializeField] protected StopInfoSO criticalStop;

        public Transform Transform => transform;

        [field: SerializeField]
        public bool IsAttacking { get; protected set; }

        [SerializeField] protected float attackCoolTime;
        [Header("attack data"), SerializeField] protected AttackDataSO[] attackDataList;
        [SerializeField] private float comboWindow;

        [SerializeField]
        protected StatSO damageStat;
        [SerializeField] protected float modifyValue;
        public float ModifyValue { get { return modifyValue; } }

        [SerializeField] protected float batteryUse;

        public float BatteryUse { get { return batteryUse; } }

        protected float lastAttackTime;
        protected int maxComboCount;
        protected int curComboCount;

        protected float timer;
        protected bool isCoolTime = false;
        protected bool isInitialized = false;
        protected AttackDataSO curAttackData;

        protected WeaponComponent weaponCompo;
        protected DamageCompo damageCompo;
        protected BatteryCompo batteryCompo;
        protected Entity _owner;

        // 애니메이터가 없을 때 공격 지속 시간을 제어하기 위한 코루틴
        protected Coroutine attackRoutine;
        protected DualFloatingArmController armCompo;

        [SerializeField] protected List<BezierAnimData> armAnimDatas;
        [SerializeField] protected EntityVFX entityVFX;
        [SerializeField] protected List<VFXInfo> attackVFXInfo;

        private const string CASTATTACK = "CASTDAMAGE";
        protected virtual void Update()
        {
            if (!isCoolTime)
                return;

            timer += Time.deltaTime;
            if (timer > attackCoolTime)
            {
                isCoolTime = false;
                timer = 0f;
            }
        }

        public virtual void Attack()
        {
            if (!CanAttack())
                return;

            if (attackDataList == null || attackDataList.Length == 0)
            {
                Debug.LogError($"{name}: attackDataList가 비어있습니다.");
                return;
            }

            // 콤보 계산
            bool comboWindowExpired = (Time.time - lastAttackTime) > comboWindow;
            if (comboWindowExpired || curComboCount > maxComboCount)
            {
                curComboCount = 0;
            }

            curAttackData = attackDataList[curComboCount];

            // 공격 수행

            if (!CanAttack()) return;
            Debug.Log(this);
            IsAttacking = true;
        }

        protected virtual void StartAttackProcess()
        {
            // 데미지 판정
            batteryCompo.UseBattery(batteryUse);
            var damageData = damageCompo.CalculateDamage(weaponCompo.StatCompo.GetStat(damageStat), curAttackData);
            bool res = damageCaster.CastDamage(damageData, weaponCompo.transform.position, weaponCompo.transform.forward, curAttackData);
            if (res && _owner is Player player)
            {
                Bus<AddChangeSkillGauge>.Raise(new AddChangeSkillGauge(damageData.damage,PlayerClass.Assault));
                if (damageData.isCritical)
                    StopManager.Instance.GenerateStop(criticalStop);
            }

            Debug.Log(res);
            MakeImpulse();

            PlayVFX();
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(AutoEndAttackAfterDelay(attackCoolTime));
        }

        protected IEnumerator AutoEndAttackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndAttack();
        }
        protected void PlayVFX()
        {
            VFXInfo curVFX = attackVFXInfo[curComboCount];

            Quaternion rot = Quaternion.identity;
            if (curVFX.useWeaponDirection)
            {
                rot = weaponCompo.transform.rotation;
            }

            if (curVFX.isOwnerVFX == false)
            {
                entityVFX.PlayVfx(curVFX.vfxName, curVFX.vfxPosAndRot.position + weaponCompo.transform.position, rot * curVFX.vfxPosAndRot.rotation, curVFX.size);
            }
            else
            {
                _owner.GetCompo<EntityVFX>().PlayVfx(curVFX.vfxName, curVFX.vfxPosAndRot.position + weaponCompo.transform.position, rot * curVFX.vfxPosAndRot.rotation, curVFX.size);
            }

        }

        protected void PlayVFX(VFXInfo vfxInfo)
        {
            Quaternion rot = Quaternion.identity;
            if (vfxInfo.useWeaponDirection)
            {
                rot = weaponCompo.transform.rotation;
            }
            if (vfxInfo.isOwnerVFX == false)
            {
                entityVFX.PlayVfx(vfxInfo.vfxName, vfxInfo.vfxPosAndRot.position + weaponCompo.transform.position, rot * vfxInfo.vfxPosAndRot.rotation, vfxInfo.size);
            }
            else
            {
                _owner.GetCompo<EntityVFX>().PlayVfx(vfxInfo.vfxName, vfxInfo.vfxPosAndRot.position + weaponCompo.transform.position, rot * vfxInfo.vfxPosAndRot.rotation, vfxInfo.size);
            }
        }

        protected void StopVFX(VFXInfo vfxInfo)
        {
            if (vfxInfo.isOwnerVFX)
                _owner.GetCompo<EntityVFX>().StopVfx(vfxInfo.vfxName);
            else
                entityVFX.StopVfx(vfxInfo.vfxName);
        }
        public virtual bool CanAttack()
        {
            bool battery = batteryCompo == null ? false : batteryCompo.CanUseBattery(batteryUse);
            return !isCoolTime && !IsAttacking && isInitialized && battery;
        }

        public virtual void EndAttack()
        {
            if (!IsAttacking) return; // 중복 실행 방지

            IsAttacking = false;
            isCoolTime = true;
            timer = 0f;


            VFXInfo curVFX = attackVFXInfo[curComboCount];
            if (curVFX.isOwnerVFX)
                _owner.GetCompo<EntityVFX>().StopVfx(curVFX.vfxName);
            lastAttackTime = Time.time;


            curComboCount++;
            attackRoutine = null;
        }

        public virtual void Initialize(Entity entity)
        {
            if (attackDataList != null)
                maxComboCount = attackDataList.Length - 1;
            isInitialized = true;

            _owner = entity;
            weaponCompo = entity.GetCompo<WeaponComponent>();
            //damageCaster.SetWeapon(WeaponData);
            damageCompo = entity.GetCompo<DamageCompo>();
            armCompo = entity.GetCompo<DualFloatingArmController>();
            batteryCompo = entity.GetCompo<BatteryCompo>();
            IsAttacking = false;
            isCoolTime = false;

            damageCaster.InitCaster(entity);


            if (entityVFX != null)
                entityVFX.Initialize(_owner);
        }

        public virtual void Release()
        {
            ReleaseAction();
            ReleaseVFX();
        }

        private void ReleaseVFX()
        {
            foreach(VFXInfo fxInfo in attackVFXInfo)
            {
                if (fxInfo.isOwnerVFX == false)
                {
                    entityVFX.StopVfx(fxInfo.vfxName);
                }
                else
                {
                    _owner.GetCompo<EntityVFX>().StopVfx(fxInfo.vfxName);
                }
            }
        }

        protected virtual void ReleaseAction()
        {

            if (weaponCompo != null)
            {
                weaponCompo.OnMouseDown -= Attack;
                weaponCompo.OnEndAttack -= EndAttack;

                weaponCompo.StatCompo.RemoveModifier(damageStat, this);
            }
            if (attackRoutine != null) StopCoroutine(attackRoutine);

            armCompo.UnsubscribeEvent(CASTATTACK, StartAttackProcess);
        }

        public virtual async UniTaskVoid InitWeapon(WeaponComponent component)
        {
            isInitialized = false;
            weaponCompo = component;

            armCompo.SubscribeEvent(CASTATTACK, StartAttackProcess);
            // 1. StatCompo가 Null인 동안 대기합니다. (무한 루프 방지를 위해 컴포넌트 자체 체크도 포함)
            await UniTask.WaitUntil(() => weaponCompo != null && weaponCompo.StatCompo != null).AttachExternalCancellation(destroyCancellationToken);
            isInitialized = true;
            weaponCompo.StatCompo.AddModifier(damageStat, this, modifyValue);

            // 나머지 초기화 로직
            weaponCompo.OnMouseDown += Attack;
            weaponCompo.OnEndAttack += EndAttack;
            armCompo.SetSubArmSocket(subArmSocket);
            foreach (BezierAnimData animData in armAnimDatas)
            {
                animData.duration = attackCoolTime;
                animData.comboWindow = comboWindow;
            }
        }

        protected void MakeImpulse()
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulse();
        }
    }
}