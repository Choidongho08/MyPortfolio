using Code.Interface;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons.Interfaces
{
    public interface IPickUpable : global::Code.Interface.IInteractable
    {
        public Transform Transform { get; }
        WeaponDataSO WeaponData { get; }
    }
}
