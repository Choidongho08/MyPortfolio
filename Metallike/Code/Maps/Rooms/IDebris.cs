using GondrLib.ObjectPool.RunTime;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public interface IDebris : IPoolable
    {
        Transform Transform { get; }
        void SetMaterial(Material mat);
        void Eject(Vector3 forceVector);
        void PushItem();
        void SetLifeTime(float lifeTime);
    }
}
