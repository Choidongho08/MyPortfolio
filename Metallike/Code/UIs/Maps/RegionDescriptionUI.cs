using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VHierarchy.Libs;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public class RegionDescriptionUI : StatefulView<RegionDesctionUIState>, IRegionDescriptionUI
    {
        public event Action OnClickStart;

        [Header("UI Settings")]
        [SerializeField] private GameObject moduleUIPrefab;
        [SerializeField] private Transform moduleUIsParent;
        [SerializeField] private float fadeDuration;

        [Header("Boss")]
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private Image bossImage;
        [SerializeField] private TooltipTrigger bossSpriteTooltip;

        [Header("CharacterType")]
        [SerializeField] private Image characterTypeImage;
        [SerializeField] private TooltipTrigger characterTypeSpriteTooltip;

        private List<GameObject> curModuleUIs;

        [SerializeField] private CanvasGroup descriptionGroup;
        CanvasGroup IFadeInOutable.FadeGroup => descriptionGroup;

        public void Initialize()
        {
            gameObject.SetActive(false);
            curModuleUIs = new();
        }

        public void HandleStartBtnClick()
        {
            OnClickStart?.Invoke();
        }

        protected override void OnUpdateState(RegionDesctionUIState state)
        {
            ClearAll();

            var prev = prevState.GetValueOrDefault(); // null 대비 GetValueOrDefault

            Bind(prev.BossIconInfo, state.BossIconInfo, UpdateBossUIInfo);
            Bind(prev.RegionCharacterInfo, state.RegionCharacterInfo, UpdateCharacterUIInfo);
            Bind(prev.RegionModuleInfos, state.RegionModuleInfos, UpdateModuleInfos);

            prev = state;
        }

        private void UpdateBossUIInfo(BossIconInfo bossIconInfo)
        {
            bossNameText.SetText(bossIconInfo.RegionBossName);
            bossImage.sprite = bossIconInfo.RegionBossInfo.Sprite;
            bossSpriteTooltip.SetText(bossIconInfo.RegionBossInfo.TooltipText);
        }

        private void UpdateCharacterUIInfo(IconInfo regionCharacterInfo)
        {
            characterTypeImage.sprite = regionCharacterInfo.Sprite;
            characterTypeSpriteTooltip.SetText(regionCharacterInfo.TooltipText);
        }

        private void UpdateModuleInfos(IconInfo[] regionModuleSprites)
        {
            foreach (var kvp in regionModuleSprites)
            {
                Sprite sprite = kvp.Sprite;
                string description = kvp.TooltipText;

                GameObject obj = Instantiate(moduleUIPrefab, moduleUIsParent);

                var image = obj.GetComponentInChildren<Image>();
                image.sprite = sprite;
                var tooltip = obj.GetComponentInChildren<TooltipTrigger>();
                tooltip.SetText(description);

                curModuleUIs.Add(obj);
            }
        }

        private void ClearAll()
        {
            if(curModuleUIs != null)
            {
                foreach (var ui in curModuleUIs)
                {
                    ui.Destroy();
                }
                curModuleUIs.Clear();
            }
        }

        public void DisableView(float duration)
        {
            this.FadeOut(duration, () => gameObject.SetActive(false));
        }

        public void EnableView(float duration)
        {
            this.FadeIn(duration, false, () => gameObject.SetActive(true));
        }

        public void DisableView()
        {
            this.FadeOut(fadeDuration, () => gameObject.SetActive(false));
        }

        public void EnableView()
        {
            this.FadeIn(fadeDuration, false, () => gameObject.SetActive(true));
        }
    }
}
