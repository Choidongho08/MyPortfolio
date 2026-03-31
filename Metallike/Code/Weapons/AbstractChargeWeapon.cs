using Core.EventBus;
using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    public class AbstractChargeWeapon : AbstractWeapon
    {
        [SerializeField] private float maxChargingTime = 1.0f;

        private bool isCharging;
        private float chargeTimer;
        protected float attackPercent;

        private const string CASTDAMAGE = "CASTDAMAGE";

        private ChargeCompo chargeCompo;

        public override void Initialize(Entity entity)
        {
            if (attackDataList != null)
                maxComboCount = attackDataList.Length - 1;

            _owner = entity;
            weaponCompo = entity.GetCompo<WeaponComponent>();
            //damageCaster.SetWeapon(WeaponData);
            damageCompo = entity.GetCompo<DamageCompo>();
            armCompo = entity.GetCompo<DualFloatingArmController>();
            batteryCompo = entity.GetCompo<BatteryCompo>();
            IsAttacking = false;
            isCoolTime = false;

            damageCaster.InitCaster(entity);
            chargeCompo = entity.GetCompo<ChargeCompo>();

            if (entityVFX != null)
                entityVFX.Initialize(_owner);

            isCharging = false;
            chargeTimer = 0f;
            attackPercent = 0f;
        }

        protected override void Update()
        {
            // 부모의 Update(쿨타임 타이머 로직) 실행
            base.Update();

            if (isCharging)
            {
                chargeTimer += Time.deltaTime;
                attackPercent = Mathf.Clamp01(chargeTimer / maxChargingTime);

                Bus<ChargeEvent>.Raise(new ChargeEvent(attackPercent));
            }
            else
            {
                Bus<ChargeEvent>.Raise(new ChargeEvent(-1));
            }
        }

        public override async UniTaskVoid InitWeapon(WeaponComponent component)
        {
            // 1. 부모의 InitWeapon이 UniTaskVoid이므로 await를 사용할 수 없습니다.
            // 대신 실행만 시켜두고(Forget), 필요한 조건이 충족될 때까지 여기서 직접 대기합니다.
            weaponCompo = component;

            // 2. 부모의 InitWeapon 내부 로직과 동기화하기 위해 동일한 대기 로직을 수행합니다.
            await UniTask.WaitUntil(() => weaponCompo != null && weaponCompo.StatCompo != null)
                         .AttachExternalCancellation(destroyCancellationToken);

            isInitialized = true;

            // 4. 차징 전용 이벤트 바인딩
            weaponCompo.OnMouseDown += StartCharge;
            weaponCompo.OnHoldEnd += EndCharge;

            // 5. 기타 컴포넌트 설정
            if (armCompo != null)
            {
                armCompo.SetSubArmSocket(subArmSocket);
                armCompo.SetComboChain(armAnimDatas);
                armCompo.SubscribeEvent(CASTDAMAGE, CastProcess);

                weaponCompo.StatCompo.AddModifier(damageStat, this, modifyValue);
            }

            if (entityVFX != null)
                entityVFX.Initialize(_owner);
            chargeCompo.Active();
        }

        protected override void ReleaseAction()
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            chargeCompo.Disable();
            armCompo.UnsubscribeEvent(CASTDAMAGE, CastProcess);

            if (weaponCompo != null)
            {
                weaponCompo.StatCompo.RemoveModifier(damageStat, this);
                weaponCompo.OnMouseDown -= StartCharge;
                weaponCompo.OnEndAttack -= EndAttack;
                weaponCompo.OnHoldEnd -= EndCharge;
            }
        }

        public override bool CanAttack()
        {
            // 배터리 체크
            bool battery = batteryCompo != null && batteryCompo.CanUseBattery(batteryUse);

            // [핵심] 차징 중일 때는 이미 IsAttacking이 true인 상태임.
            // 이때 EndCharge에서 호출한 Attack()이 통과되려면 IsAttacking 체크를 우회해야 함.
            if (isCharging)
            {
                return !isCoolTime && isInitialized && battery;
            }

            // 평상시(차징 시작 전)에는 부모의 엄격한 조건(!IsAttacking 포함)을 따름
            return base.CanAttack();
        }

        public void StartCharge()
        {
            // 순수하게 공격이 가능한 상태인지 체크 (부모의 CanAttack 활용)
            if (!base.CanAttack()) return;

            isCharging = true;
            IsAttacking = true; // 차징 시작을 공격의 시작으로 간주
            chargeTimer = 0f;
        }

        public override void Attack()
        {
            // 부모의 Attack을 호출하지 않고 직접 구현 (데미지에 attackPercent를 적용하기 위함)
            // 만약 부모의 Attack을 호출하면 내부에서 CanAttack을 다시 체크함 (오버라이드 했으므로 통과됨)

            if (attackDataList == null || attackDataList.Length == 0) return;

            // 차징 무기는 콤보를 쓰지 않더라도 데이터를 참조
            curAttackData = attackDataList[curComboCount];

            // 1. 애니메이션 실행
        }

        private void CastProcess()
        {
            batteryCompo.UseBattery(batteryUse);

            // 3. 데미지 계산 및 투사 (차징 퍼센트 적용)
            // CalculateDamage가 3개의 인자를 받는 버전이 있다고 가정하거나, 결과값에 곱해줍니다.
            var damageData = damageCompo.CalculateDamage(weaponCompo.StatCompo.GetStat(damageStat), curAttackData);

            // 예시: 차징에 따른 데미지 증폭 (기본 100% + 차징 시 추가 100% = 총 200%)
            damageData.damage *= attackPercent;

            bool res = damageCaster.CastDamage(damageData, weaponCompo.transform.position, weaponCompo.transform.forward, curAttackData);
            if (res && _owner is Player player)
            {
                Bus<AddChangeSkillGauge>.Raise(new AddChangeSkillGauge(damageData.damage, PlayerClass.Assault));

                if (damageData.isCritical)
                    StopManager.Instance.GenerateStop(curAttackData.criticalStop);
            }

            // 4. 효과 실행
            MakeImpulse();
            PlayVFX();
        }

        public virtual void EndCharge()
        {
            if (!isCharging) return;

            // Attack() 내부에서 CanAttack()을 호출할 때 isCharging 조건으로 통과시키기 위해 
            // Attack 호출 직후 혹은 직전에 상태를 신중히 변경해야 합니다.

            // 실제 공격 수행
            Attack();

            // 공격 로직이 끝난 후 차징 상태 해제
            isCharging = false;

            // 후딜레이 후 공격 종료 처리
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(AutoEndAttackAfterDelay(attackCoolTime));
        }

        public override void EndAttack()
        {
            // 중복 실행 방지
            if (!IsAttacking) return;

            IsAttacking = false;
            isCoolTime = true;
            timer = 0f;

            // 차징 무기도 다음 공격 데이터를 위해 인덱스 순환
            curComboCount = (curComboCount + 1) % attackDataList.Length;
            lastAttackTime = Time.time;

            attackRoutine = null;
        }
    }
}