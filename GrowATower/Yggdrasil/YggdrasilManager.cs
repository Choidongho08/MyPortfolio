using Ami.BroAudio;
using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using Assets._04.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Volume = UnityEngine.Rendering.Volume;

namespace Assets._01.Member.CDH.Code.Yggdrasils
{
    public class YggdrasilManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundID gameOverSoundID;
        [SerializeField] private SoundID hitSoundID;

        [SerializeField] private float maxHealth;

        private int clearedWave;
        private bool isDead;

        [SerializeField] private Volume volume;
        private Vignette vignette;
        private Coroutine vignetteRoutine;

        private void Awake()
        {
            isDead = false;
            clearedWave = 0;
            Yggdrasil.Instance.Initialize(maxHealth);
            Yggdrasil.Instance.OnYggdrasilHealthChanged += HandleYggdrasilHealthChaned;

            volume = FindAnyObjectByType<Volume>();
            if (volume != null)
            {
                volume.profile.TryGet(out vignette);
            }
        }

        private void OnDestroy()
        {
            Yggdrasil.Instance.OnYggdrasilHealthChanged -= HandleYggdrasilHealthChaned;
        }

        private void HandleYggdrasilHealthChaned(float health, float maxHealth)
        {
            if (isDead)
                return;

            if (vignette != null)
            {
                if (vignetteRoutine != null)
                    StopCoroutine(vignetteRoutine);

                vignetteRoutine = StartCoroutine(VignettePulseEffect());
            }

            if (health <= 0)
            {
                isDead = true;
                Bus<YggdrasilDeadEvent>.Invoke(new YggdrasilDeadEvent());
                soundChannel.Invoke(SoundEvents.PlayEvent.Initialize(gameOverSoundID, transform));
                var d = FindAnyObjectByType<GameManager>();

                if (d != null && d.IsFastGame)
                    d.HandleFastGame();

            }
            else
            {
                soundChannel.Invoke(SoundEvents.PlayEvent.Initialize(hitSoundID, transform));
                Camera.main.GetComponent<CinemachineImpulseSource>().GenerateImpulse();
            }
        }

        private System.Collections.IEnumerator VignettePulseEffect()
        {
            float durationUp = 0.2f;
            float durationDown = 0.6f;
            float targetIntensity = 0.2f;
            float startIntensity = vignette.intensity.value;

            // 점점 증가
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / durationUp;
                vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
                yield return null;
            }

            // 점점 감소
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / durationDown;
                vignette.intensity.value = Mathf.Lerp(targetIntensity, 0f, t);
                yield return null;
            }

            vignette.intensity.value = 0f;
        }

        [ContextMenu("TestDead")]
        public void TestDead()
        {
            Bus<YggdrasilDeadEvent>.Invoke(new YggdrasilDeadEvent());
        }
    }
}
