using DG.Tweening; // DOTween 네임스페이스 추가
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Work.CDH.Code.UIs.Maps.SecurityLevels
{
    public class SecurityLevelView : StatefulView<SecurityLevelViewState>, ISecurityLevelView, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image securityValueBar;

        [Header("Animation Settings")]
        [Tooltip("게이지가 차오르는 시간")]
        [SerializeField] private float barFillDuration = 0.3f;
        [Tooltip("레벨 상승 시 텍스트 팝업 강도")]
        [SerializeField] private float levelUpPunchScale = 0.5f;
        [Tooltip("레벨 상승 연출 시간")]
        [SerializeField] private float levelUpDuration = 0.3f;

        private Tween fillTween;
        private Tween textTween;

        public void Initialize()
        {
        }

        protected override void OnUpdateState(SecurityLevelViewState state)
        {
            var prev = prevState.GetValueOrDefault();

            Bind(prev.SecurityLevel, state.SecurityLevel, UpdateSecurityLevel);
            Bind(prev.SecurityValue, state.SecurityValue, UpdateSecurityValue);
        }

        private void UpdateSecurityValue(float newValue)
        {
            if (securityValueBar != null)
            {
                float targetValue = Mathf.Clamp01(newValue);

                fillTween?.Kill();

                fillTween = securityValueBar.DOFillAmount(targetValue, barFillDuration)
                                            .SetEase(Ease.OutCubic);
            }
        }

        private void UpdateSecurityLevel(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = newLevel.ToString();

                textTween?.Kill();

                levelText.transform.localScale = Vector3.one;

                textTween = levelText.transform.DOPunchScale(Vector3.one * levelUpPunchScale, levelUpDuration, vibrato: 5, elasticity: 1f);
            }
        }

        private void OnDestroy()
        {
            fillTween?.Kill();
            textTween?.Kill();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}