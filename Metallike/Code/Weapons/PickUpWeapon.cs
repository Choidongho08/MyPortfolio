using Assets.Work.CDH.Code.Weapons.Interfaces;
using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    public class PickUpWeapon : MonoBehaviour, IPickUpable
    {
        [SerializeField] private WeaponDataSO weaponData;
        [Header("Size Settings")]
        [Tooltip("월드 좌표계에서 이 오브젝트가 차지할 크기 (단위: Unit)")]
        [SerializeField] private float targetSize = 0.6f;

        [SerializeField]private SpriteRenderer _spriteRenderer;
        [SerializeField] private float rotationSpeed = 60;
        public Transform Transform => transform;
        public WeaponDataSO WeaponData => weaponData;

        private void Awake()
        {
            if (_spriteRenderer == null || weaponData.weaponIcon == null) return;
            _spriteRenderer.sprite = weaponData.weaponIcon;
            AdjustScale();
        }

        public void AdjustScale()
        {
            if (_spriteRenderer.sprite == null) return;

            Sprite sprite = _spriteRenderer.sprite;

            float pixelWidth = sprite.rect.width;
            float pixelHeight = sprite.rect.height;

            float ppu = sprite.pixelsPerUnit;

            float currentWorldMaxWidth = Mathf.Max(pixelWidth, pixelHeight) / ppu;

            if (currentWorldMaxWidth > 0)
            {
                float finalScale = targetSize / currentWorldMaxWidth;
                _spriteRenderer.transform.localScale = new Vector3(finalScale, finalScale, 1f);
            }
        }
        private void Update()
        {
            RotateWeapon();
        }

        private void RotateWeapon()
        {
            // Y축을 기준으로 회전 (2D 스프라이트가 월드에서 팔랑거리는 느낌을 줄 때 유용)
            // 만약 2D 평면상(Z축) 회전을 원하시면 Vector3.forward를 사용하세요.
            transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime));
        }


        public void EnterInteractionRange()
        {
            UIInfoManager.Instance.UpdatePickUI(weaponData, transform);
        }

        public void ExitInteractionRange()
        {
            UIInfoManager.Instance.DisablePickUI();
        }

        public void OnInteract(Entity interacter)
        {
            
        }
    }
}
