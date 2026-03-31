using DG.Tweening;
using GondrLib.ObjectPool.RunTime;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Work.CDH.Code.Maps
{
    public class Dais : MonoBehaviour, IPoolable
    {
        [Header("Input Setting")]
        [SerializeField] private PlayerInputSO playerInputSO;

        [Header("Settings")]
        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }

        public GameObject GameObject => gameObject;

        public Action<ModuleSO> OnPlayerCheck;

        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float checkRadius;

        [SerializeField] private GameObject textObj;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        private Collider[] colliders;
        private bool isChecked;
        private ModuleSO module;
        private Camera mainCamera;

        private Pool myPool;


        private void Awake()
        {
            isChecked = true;
            colliders = new Collider[1];

        }

        private void OnEnable()
        {
            playerInputSO.OnInteraction += HandleOnInteraction;
        }

        private void OnDisable()
        {
            
            playerInputSO.OnInteraction -= HandleOnInteraction;
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            textObj.transform.rotation = mainCamera.transform.rotation;
        }

        private void HandleOnInteraction()
        {
            Debug.LogError("dddd");
            if (!isChecked)
            {
                int cnt = Physics.OverlapSphereNonAlloc(transform.position, checkRadius, colliders, playerLayer);
                if (cnt > 0)
                {
                    isChecked = true;
                    OnPlayerCheck?.Invoke(module);
                }
            }
        }

        public void StartCheck()
        {
            isChecked = false;
        }

        public void StopCheck()
        {
            isChecked = true;
        }

        public void Move(Vector3 targetPos, float speed, Action OnComplete = null)
        {
            transform.DOKill();

            transform
                .DOMove(targetPos, speed)
                .SetSpeedBased(true)
                 .SetEase(Ease.OutSine)
                 .OnComplete(() => OnComplete?.Invoke());
        }

        public void SetModule(ModuleSO targetModule)
        {
            module = targetModule;
            nameText.text = targetModule.name;
            descriptionText.text = targetModule.descript;
        }

        public void SetUpPool(Pool pool)
        {
            myPool = pool;
        }

        public void ResetItem()
        {
            isChecked = true;
        }


#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, checkRadius);
        }


#endif
    }
}
