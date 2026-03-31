using UnityEngine;
using GondrLib.ObjectPool.RunTime;
using DG.Tweening; // DOTween 필수

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class Debris : MonoBehaviour, IDebris
    {
        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;

        private Pool myPool;
        private Rigidbody rb;
        private Renderer myRenderer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            myRenderer = GetComponent<Renderer>();
        }

        public void SetUpPool(Pool pool)
        {
            myPool = pool;
        }

        public void SetMaterial(Material mat)
        {
            if (myRenderer != null && mat != null)
                myRenderer.material = mat;
        }

        public void ResetItem()
        {
            transform.DOKill();

            if (myRenderer != null)
            {
                var color = myRenderer.material.color;
                color.a = 1f;
                myRenderer.material.color = color;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public void Eject(Vector3 forceVector)
        {
            if (rb != null)
            {
                rb.AddForce(forceVector, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
        }

        // 인터페이스 요구사항
        public void Eject(Vector3 dir, float force)
        {
            Eject(dir * force);
        }

        public void PushItem()
        {
            if (!gameObject.activeSelf) return;
            myPool?.Push(this);
        }

        // [DOTween 적용]
        public void SetLifeTime(float lifeTime)
        {
            transform.DOKill();

            transform.DOScale(0f, 0.5f)        
                     .SetDelay(lifeTime - 0.5f)  
                     .OnComplete(() => {
                         transform.localScale = Vector3.one;
                         PushItem();
                     });
        }

        private void OnDisable()
        {
            transform.DOKill(); 
        }
    }
}