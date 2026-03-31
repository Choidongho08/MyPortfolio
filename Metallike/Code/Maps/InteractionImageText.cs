using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Work.CDH.Code.Maps
{
    public enum InteractionIconEnum
    {
        LockIcon,
        EIcon,
        MiddleBossIcon,
        Possible,
        Impossible,
        BattleMode,
    }

    [Serializable]
    public struct InteractionIcon
    {
        public InteractionIconEnum iconType;
        public Sprite icon;
    }

    [DefaultExecutionOrder(-1)]
    public class InteractionImageText : MonoBehaviour
    {
        [Header("--- UGUI Components ---")]
        [SerializeField] private RectTransform visualsRoot;
        [SerializeField] private Image outlineImage;
        [SerializeField] private Image iconImage;

        [Header("Sprite Settings")]
        [SerializeField] private List<InteractionIcon> icons;

        [Header("--- Follow Settings ---")]
        [SerializeField] private Transform target3D;
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2.0f, 0);

        [Header("--- Animation Settings ---")]
        [SerializeField] private float animDuration = 0.25f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        [Header("Color Setting")]
        [SerializeField] private Color possibleColor;
        [SerializeField] private Color impossibleColor;
        [SerializeField] private Color middleBossColor;

        private Dictionary<InteractionIconEnum, Sprite> iconSpriteByTypeDict;

        private bool isVisible = false;
        private Vector3 originalScale;
        private Camera mainCam;

        private bool isBattle = false;

        private void Awake()
        {
            if (visualsRoot == null) visualsRoot = GetComponent<RectTransform>();
            originalScale = visualsRoot.localScale;
            mainCam = Camera.main;

            visualsRoot.localScale = new Vector3(originalScale.x, 0f, originalScale.z);
            visualsRoot.gameObject.SetActive(false);
            isVisible = false;
            isBattle = false;

            iconSpriteByTypeDict = icons.ToDictionary(x => x.iconType, x => x.icon);
        }

        private void LateUpdate()
        {
            // [수정됨] !isVisible 조건 삭제
            // isVisible 변수와 상관없이, 타겟이 있고 UI가 켜져(Active)있으면 무조건 위치 갱신
            if (target3D == null || mainCam == null) return;

            // UI 오브젝트가 꺼져있으면(Hide 애니메이션 끝난 후) 계산 안 함
            if (!visualsRoot.gameObject.activeInHierarchy) return;

            // --- 좌표 변환 로직 ---
            Vector3 worldPos = target3D.position + worldOffset;
            Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                // 카메라 뒤면 잠시 안 보이게 (Active는 유지하되 렌더링만 끄는 식이나, 그냥 둠)
                // 여기서는 깜빡임 방지를 위해 그냥 둡니다. 필요시 CanvasGroup Alpha 조정 권장
            }
            else
            {
                visualsRoot.position = screenPos;
            }
        }

        public void Init()
        {

        }

        public void Show()
        {
            if (visualsRoot == null || isBattle) return;

            isVisible = true;
            visualsRoot.gameObject.SetActive(true);

            visualsRoot.DOKill();
            if (visualsRoot.localScale.y <= 0.01f)
                visualsRoot.localScale = new Vector3(originalScale.x, 0f, originalScale.z);

            visualsRoot.DOScaleY(originalScale.y, animDuration).SetEase(showEase);
        }

        public void Hide()
        {
            if (visualsRoot == null || !isVisible) return;

            isVisible = false; // 논리적으로는 꺼짐 상태

            visualsRoot.DOKill();
            visualsRoot.DOScaleY(0f, animDuration)
                .SetEase(hideEase)
                .OnComplete(() =>
                {
                    // [중요] 애니메이션이 다 끝나야 진짜로 비활성화됨
                    // 이 전까지는 LateUpdate가 계속 돌면서 위치를 잡아줌
                    visualsRoot.gameObject.SetActive(false);
                });
        }

        public void IconSetting(InteractionIconEnum iconType, InteractionIconEnum colorType, Vector2 spriteSize, float fontSize)
        {
            if (colorType == InteractionIconEnum.Possible || colorType == InteractionIconEnum.LockIcon)
            {
                outlineImage.color = possibleColor;
                iconImage.color = possibleColor;
            }
            if (colorType == InteractionIconEnum.Impossible)
            {
                outlineImage.color = impossibleColor;
                iconImage.color = impossibleColor;
            }
            if (colorType == InteractionIconEnum.MiddleBossIcon)
            {
                outlineImage.color = middleBossColor;
                iconImage.color = middleBossColor;
            }

            if (iconSpriteByTypeDict.TryGetValue(iconType, out var icon))
            {
                iconImage.sprite = icon;
                iconImage.rectTransform.sizeDelta = spriteSize;
            }
        }

        public void IconSetting(InteractionIconEnum iconType, InteractionIconEnum colorType, Vector2 spriteSize, float fontSize, object param)
        {
            IconSetting(iconType, colorType, spriteSize, fontSize);

            if (iconType == InteractionIconEnum.BattleMode)
            {
                isBattle = (bool)param;
                if (isBattle)
                    Hide();
            }
        }
    }
}