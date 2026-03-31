using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    public class TestBat : AbstractChargeWeapon
    { 
        private StunHandle handle;
        [SerializeField] private float stunDuration;
        public override async UniTaskVoid InitWeapon(WeaponComponent component)
        {
            base.InitWeapon(component).Forget();
            handle = new StunHandle();
            handle.Duration = stunDuration;
            await UniTask.WaitUntil(() => damageCaster != null);
            damageCaster.AddCastingHandler(handle);
        }

        public override void EndCharge()
        {
            if (Mathf.Approximately(attackPercent, 1))
            {
                handle.Active(true);
            }
            else
            {
                handle.Active(false);
            }
            base.EndCharge();
        }

        public override void Release()
        {
            damageCaster.RemoveCastingHandler(handle);
            base.Release();
        }
    }
}
