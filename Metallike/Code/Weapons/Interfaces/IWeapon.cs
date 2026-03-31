using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    public interface IWeapon
    {
        public Transform Transform { get; }
        /// <summary>
        /// 사거리 변수
        /// </summary>
        public WeaponDataSO WeaponData { get; }
        /// <summary>
        /// 초기화 무기 바꿀때 초기화
        /// </summary>
        public void Initialize(Entity entity);
        /// <summary>
        /// 혹시 모르니 웨폰 바꿀때 버려지는? 그 떄 사용
        /// </summary>
        public void Release();

        /// <summary>
        /// WeaponComponent의 이벤트를 바인드
        /// </summary>
        /// <param name="component"></param>
    }
}
