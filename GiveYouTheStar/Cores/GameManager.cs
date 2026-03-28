using _01.Develop.LSW._01._Scripts.Manager;
using _01.Develop.LSW._01._Scripts.So;
using Ami.BroAudio;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameEventChannelSO textChannel;

    [Header("Sound")]
    [SerializeField] private SoundID gameEndBellSoundID;
    [Space]

    public float maxTimer;
    float currentTimer;
    [SerializeField] private SpriteRenderer clickUI;

    [SerializeField] private Transform timerVisual;
    public CameraManager cameraManager;

    [Header("Countdown Text (3,2,1)")]
    [SerializeField] private TMP_Text countText;     // ✅ 카운트다운 전용

    [Header("Charge Ready Text (Blink)")]
    [SerializeField] private TMP_Text chargeReadyText; // ✅ 차지 안내 텍스트 (따로!)

    public UnityEvent<bool> OnStartCharge;

    public UnityEvent StartRopeCharge;
    public UnityEvent EndRopeCharge;
    public UnityEvent EndGame;

    private bool isRopeCharging;
    private bool isRopeChargeEnd;
    private bool isRopeChargeTurn;
    private bool isCatchStar;
    private bool isGameStart;

    private Coroutine coroutine;

    public List<StarSo> getStarList = new();

    private float initialRotationZ;

    // =========================
    // Blink Settings
    // =========================
    [Header("Charge Ready Blink Settings")]
    [SerializeField] private bool useChargeBlink = true;
    [SerializeField] private float blinkMinAlpha = 0.15f;
    [SerializeField] private float blinkMaxAlpha = 1.0f;
    [SerializeField] private float blinkHalfDuration = 0.7f;
    [SerializeField] private Ease blinkEase = Ease.InOutSine;

    private Tween chargeBlinkTween;

    private void Awake()
    {
        isRopeCharging = false;
        isRopeChargeEnd = false;
        isRopeChargeTurn = true;
        isCatchStar = false;

        if (timerVisual != null)
            initialRotationZ = timerVisual.localEulerAngles.z;

        // ✅ 시작은 안 보이게
        SetChargeReadyVisible(false);
    }

    private void OnDisable()
    {
        StopChargeBlink();
    }

    public void FirstStart()
    {
        StartCoroutine(GameStartReady());
    }

    private IEnumerator GameStartReady()
    {
        // 카운트다운
        if (countText != null)
        {
            countText.text = "3";
            yield return new WaitForSeconds(1f);
            countText.text = "2";
            yield return new WaitForSeconds(1f);
            countText.text = "1";
            yield return new WaitForSeconds(1f);
            countText.text = "";
        }

        clickUI.enabled = true;

        GameStart();

        // ✅ 카메라 1프레임 정착
        yield return null;

        // ✅ 로프 차지 "대기" 안내 텍스트 깜빡 시작
        StartChargeBlink();

        yield return new WaitForSeconds(4f);

        // 4초 끝나면(원래 네 로직처럼) UI 끄기
        StopChargeBlink();
        clickUI.enabled = false;
    }

    public void AddStar(StarSo star)
    {
        getStarList.Add(star);
        Debug.Log(star.starName);
    }

    public void SetMaxTimer(float maxTimer)
    {
        this.maxTimer = maxTimer;
    }

    public void GameStart()
    {
        maxTimer = PlayerStatManager.Instance.GetCurrentMaxTimer();
        isGameStart = true;
        currentTimer = maxTimer;
    }

    private void Update()
    {
        if (!isGameStart) return;

        currentTimer -= Time.deltaTime;
        currentTimer = Mathf.Clamp(currentTimer, 0f, maxTimer);

        // 타이머 비주얼 회전
        if (timerVisual != null)
        {
            float t = 1f - (currentTimer / maxTimer);
            float rotX = initialRotationZ + 180f * t;
            Vector3 currentEuler = timerVisual.localEulerAngles;
            timerVisual.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, rotX);
        }

        if (currentTimer <= 0f)
            GameEnd();

        // 차지 턴
        if (isRopeChargeTurn && !isCatchStar && !cameraManager.isFullCamActive)
        {
            // ✅ 차지 대기 중이면 깜빡 유지
            if (!isRopeCharging)
                StartChargeBlink();

            if (!isRopeCharging && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // ✅ 차지 시작하면 안내 텍스트는 사라지기
                StopChargeBlink();

                isRopeCharging = true;
                isRopeChargeEnd = false;
                StartRopeCharge?.Invoke();
            }

            if (isRopeCharging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isRopeCharging = false;
                isRopeChargeEnd = true;
                isRopeChargeTurn = false;

                StopChargeBlink();
                EndRopeCharge?.Invoke();
            }
        }
        else
        {
            StopChargeBlink();
        }
    }

    private void GameEnd()
    {
        if (TutorialManager.Instance.IsPlayEndTutorial)
        {
            string rewards = getStarList.Count > 0 ? "허허허! 자네가 오늘 수확한\n눈부신 별들이 여기 다 모였군!" : "이런... 이번 기회는 아쉽지만 돌아가지\n원래 뭐든지 처음은 쉽지 않은 법이라네.";
            TextPanelEvent textPanelEvent = new();
            textPanelEvent
                .AddDialogue(rewards)
                .AddDialogue("최대한 많은 아이들의 동심을\n지켜야 하니 빨리 서두르지!")
                .AddEvent(() =>
                {
                    RealGameEnd();
                });

            textChannel.RaiseEvent(textPanelEvent);
        }
        else
        {
            RealGameEnd();
        }
    }

    private void RealGameEnd()
    {
        isGameStart = false;
        StopChargeBlink();
        StarManager.Instance.EndGame();
        BroAudio.Play(gameEndBellSoundID);
        EndGame?.Invoke();
    }

    public void SetRopeChargeTurn()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
        coroutine = StartCoroutine(SetRopeChargeTurnRoutine());
    }

    public IEnumerator SetRopeChargeTurnRoutine()
    {
        clickUI.enabled = false;

        if (countText != null)
        {
            countText.text = "3";
            yield return new WaitForSeconds(1f);
            countText.text = "2";
            yield return new WaitForSeconds(1f);
            countText.text = "1";
            yield return new WaitForSeconds(1f);
            countText.text = "";
        }

        clickUI.enabled = true;
        isRopeChargeTurn = true;
        OnStartCharge?.Invoke(true);

        // ✅ 다시 차지 대기 깜빡
        StartChargeBlink();

        yield return new WaitForSeconds(4f);

        StopChargeBlink();
        clickUI.enabled = false;
    }

    public void SetCatchStar()
    {
        isCatchStar = true;
        StopChargeBlink();
        OnStartCharge?.Invoke(false);
    }

    public void EndCatchStar()
    {
        StartCoroutine(EndCatchStarRoutine());
    }

    private IEnumerator EndCatchStarRoutine()
    {
        yield return new WaitForSeconds(2.5f);
        isCatchStar = false;
    }

    // =========================
    // Charge Ready Blink
    // =========================
    private void StartChargeBlink()
    {
        if (!useChargeBlink) return;
        if (chargeReadyText == null) return;

        if (chargeBlinkTween != null && chargeBlinkTween.IsActive())
            return;

        SetChargeReadyVisible(true);

        chargeReadyText.DOKill();
        chargeReadyText.alpha = blinkMaxAlpha;

        chargeBlinkTween = chargeReadyText
            .DOFade(blinkMinAlpha, blinkHalfDuration)
            .SetEase(blinkEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopChargeBlink()
    {
        if (chargeReadyText == null) return;

        if (chargeBlinkTween != null)
        {
            chargeBlinkTween.Kill();
            chargeBlinkTween = null;
        }

        SetChargeReadyVisible(false);
    }

    private void SetChargeReadyVisible(bool visible)
    {
        if (chargeReadyText == null) return;

        // GameObject 꺼버리면 레이캐스트/배치도 깔끔
        chargeReadyText.gameObject.SetActive(visible);

        if (visible)
        {
            var c = chargeReadyText.color;
            c.a = 1f;
            chargeReadyText.color = c;
        }
    }
}