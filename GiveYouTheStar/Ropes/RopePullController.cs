using _01.Develop.LSW._01._Scripts.Manager;
using _01.Develop.LSW._01._Scripts.So;
using Ami.BroAudio;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Volume = UnityEngine.Rendering.Volume;

public class RopePullController : MonoBehaviour
{
    public UnityEvent OnChainBreak;
    public UnityEvent<StarSo> OnGetStar;
    public UnityEvent<StarSo> OnGetNewStar;

    [SerializeField] private GameObject pullGameObject;

    [SerializeField] private CanvasGroup text;

    [Header("sound")]
    [SerializeField] private SoundID ropePullSoundId;
    [SerializeField] private SoundID cutRopeSoundId;
    [SerializeField] private SoundID ropeWindSoundId;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private bool autoFindMainCamera = true;

    [Header("Target (현재 잡힌 스타)")]
    [SerializeField] private StarMover starTarget;

    [Header("Rope Line")]
    [SerializeField] private LineRenderer ropeLine;
    [SerializeField] private bool autoCreateRopeLine = true;
    [SerializeField] private float lineWidth = 0.05f;

    [Header("Camera Follow / Collect")]
    [SerializeField] private bool followWhenRoped = true;
    [SerializeField] private float collectDistance = 1.2f;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Star Drift")]
    [SerializeField] private Vector2 driftDirection = new Vector2(1, 1);
    [SerializeField] private float driftSpeed = 8f;
    [Tooltip("옆으로 새는 속도를 드리프트 방향으로 되돌리는 힘")]
    [SerializeField] private float driftReturnStrength = 3f;

    [Header("Pull Input")]
    [SerializeField] private float minDragWorld = 0.8f;
    [SerializeField] private float maxDragWorld = 5f;
    [Range(1f, 4f)] [SerializeField] private float lengthPower = 2.4f;
    [SerializeField] private float minEffectiveDragDuration = 0.12f;
    [SerializeField] private float pullCooldown = 0.12f;

    [Header("Pull Strength")]
    [SerializeField] private float baseImpulse = 12f;

    [Header("Pull Velocity Damping")]
    [SerializeField] private float pullVelocityDamping = 8f;

    [Header("Rope Break Settings")]
    [SerializeField] private float maxRopeStretchDistance = 10f;

    // =========================
    // UI - Break Meter
    // =========================
    [Header("UI - Rope Break Meter (Image Filled 360)")]
    [SerializeField] private Image breakMeterImage;
    [SerializeField] private bool hideMeterWhenNoTarget = true;
    [SerializeField] private float meterSmooth = 12f;
    private float meterFill = 1f;

    [Header("UI Follow (Star -> UI)")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private Vector2 followOffsetWorld = new Vector2(0f, 1.2f);
    [SerializeField] private Vector2 followOffsetUI = Vector2.zero;
    [SerializeField] private bool hideWhenOffscreen = true;

    [Header("UI Follow Smoothing (Fix Jitter)")]
    [SerializeField] private float followSmoothTime = 0.06f;
    [SerializeField] private bool pixelSnap = true;

    [Header("UI - Meter Color (Yellow -> Red Only)")]
    [SerializeField] private Color yellowColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private bool useHSVColorLerp = true;

    // =========================
    // Post Processing + Pull UI
    // =========================
    [Header("PostProcessing - Lens Distortion (URP Volume)")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float lensDistortionTarget = -0.35f;
    [SerializeField] private float lensDownDuration = 0.08f;
    [SerializeField] private float lensReturnDuration = 0.18f;
    [SerializeField] private Ease lensDownEase = Ease.OutQuad;
    [SerializeField] private Ease lensReturnEase = Ease.OutQuad;

    [Header("PullGameObject Fade (CanvasGroup, Two Tweens)")]
    [SerializeField] private float pullFadeInDuration = 0.06f;
    [SerializeField] private float pullFadeOutDuration = 0.20f;
    [SerializeField] private Ease pullFadeInEase = Ease.OutQuad;
    [SerializeField] private Ease pullFadeOutEase = Ease.OutQuad;

    private LensDistortion lensDistortion;
    private Tween lensTween;

    private Rigidbody2D starRb;
    private Camera cam;

    private bool isDragging;
    private Vector2 dragStartWorld;
    private float dragStartTime;
    private float nextPullTime;
    private Vector2 pullVel;
    private Vector2 lastPullPosition;

    private CanvasGroup pullCanvasGroup;
    private Tween pullFadeInTween;
    private Tween pullFadeOutTween;

    private RectTransform breakMeterRect;
    private Vector2 followVelUI;
    private bool lastOffscreenState;

    private void Awake()
    {
        if (autoFindMainCamera)
            cam = Camera.main;

        if (driftDirection.sqrMagnitude < 0.0001f)
            driftDirection = Vector2.right;
        driftDirection = driftDirection.normalized;

        if (autoCreateRopeLine && ropeLine == null)
            SetupRopeLine();

        CacheLensDistortion();
        CachePullCanvasGroup();   // 초기에는 무조건 숨김
        InitBreakMeter();

        if (starTarget != null)
            SetTarget(starTarget);
        else
        {
            HidePullImmediate();
            HideBreakMeterIfNeeded();
        }
    }

    private void OnDisable()
    {
        lensTween?.Kill();
        pullFadeInTween?.Kill();
        pullFadeOutTween?.Kill();
    }

    public void SetMaxRopeStretchDistance(float maxRopeStretchDistance)
    {
        this.maxRopeStretchDistance = maxRopeStretchDistance;
    }

    private void CacheLensDistortion()
    {
        lensDistortion = null;
        if (postProcessVolume == null || postProcessVolume.profile == null) return;
        postProcessVolume.profile.TryGet(out lensDistortion);
    }

    // ---------------- PullGameObject Fade ----------------

    private void CachePullCanvasGroup()
    {
        pullCanvasGroup = null;
        if (pullGameObject == null) return;

        pullCanvasGroup = pullGameObject.GetComponent<CanvasGroup>();
        if (pullCanvasGroup == null)
            pullCanvasGroup = pullGameObject.AddComponent<CanvasGroup>();

        // 초기에는 안 보이게
        pullCanvasGroup.alpha = 0f;
        pullGameObject.SetActive(false);
    }

    private void ShowPullFadeIn()
    {
        if (pullGameObject == null || pullCanvasGroup == null) return;

        pullFadeOutTween?.Kill();
        pullFadeInTween?.Kill();

        if (!pullGameObject.activeSelf)
        {
            pullGameObject.SetActive(true);
            pullCanvasGroup.alpha = 0f;
        }

        pullFadeInTween = pullCanvasGroup
            .DOFade(1f, pullFadeInDuration)
            .SetEase(pullFadeInEase)
            .SetUpdate(true);
    }

    private void HidePullFadeOut()
    {
        if (pullGameObject == null || pullCanvasGroup == null) return;
        if (!pullGameObject.activeSelf) return;

        pullFadeInTween?.Kill();
        pullFadeOutTween?.Kill();

        pullFadeOutTween = pullCanvasGroup
            .DOFade(0f, pullFadeOutDuration)
            .SetEase(pullFadeOutEase)
            .SetUpdate(true)
            .OnComplete(() => pullGameObject.SetActive(false));
    }

    private void HidePullImmediate()
    {
        if (pullGameObject == null || pullCanvasGroup == null) return;
        pullFadeInTween?.Kill();
        pullFadeOutTween?.Kill();
        pullCanvasGroup.alpha = 0f;
        pullGameObject.SetActive(false);
    }

    // -----------------------------------------------------------------

    private void Update()
    {
        if (starTarget == null) return;

        UpdateRopeLine();

        if (player != null && Vector2.Distance(starTarget.transform.position, player.position) <= collectDistance)
        {
            if(StarManager.Instance.IsUnlock(starTarget.MyInfo))
            {
                Debug.LogError("TEstSDjanujodgn");
                OnGetNewStar?.Invoke(starTarget.MyInfo);
            }

            OnGetStar?.Invoke(starTarget.MyInfo);
            StarManager.Instance.AddGotStar(starTarget.MyInfo);
            EndPull();
            return;
        }

        if (cam == null || player == null || starRb == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartWorld = MouseWorld2D();
            dragStartTime = Time.time;

            PlayLensDistortionDown(); 
            
            BroAudio.Play(ropePullSoundId);

            // 평소에는 보이고, 클릭하면 페이드로 사라짐
            HidePullFadeOut();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging) return;
            isDragging = false;

            PlayLensDistortionReturn();
            TryPullOnRelease(MouseWorld2D(), Time.time - dragStartTime);

            // 클릭을 떼면 다시 페이드로 나타남
            ShowPullFadeIn();
        }
    }

    // 카메라/시네머신 업데이트 이후에 UI 위치 갱신
    private void LateUpdate()
    {
        if (starTarget == null) return;
        UpdateBreakMeterFollowSmooth();
    }

    private void FixedUpdate()
    {
        if (starRb == null) return;

        if (pullVelocityDamping > 0f)
        {
            float k = Mathf.Exp(-pullVelocityDamping * Time.fixedDeltaTime);
            pullVel *= k;
        }

        if (driftReturnStrength > 0f)
        {
            Vector2 perp = pullVel - driftDirection * Vector2.Dot(pullVel, driftDirection);
            pullVel -= perp * driftReturnStrength * Time.fixedDeltaTime;
        }

        starRb.gravityScale = 0f;
        starRb.linearVelocity = driftDirection * driftSpeed + pullVel;

        if (maxRopeStretchDistance > 0f && starTarget != null)
        {
            float distFromLastPull = Vector2.Distance(starTarget.transform.position, lastPullPosition);

            UpdateBreakMeter(distFromLastPull);

            if (distFromLastPull > maxRopeStretchDistance)
            {
                Vector2 toPlayer = ((Vector2)player.position - (Vector2)starTarget.transform.position).normalized;
                Vector2 currentDir = starRb.linearVelocity.sqrMagnitude > 0.0001f ? starRb.linearVelocity.normalized : Vector2.zero;

                if (Vector2.Dot(toPlayer, currentDir) < 0f)
                {
                    EndPull();
                    return;
                }
            }
        }
        else
        {
            if (breakMeterImage != null)
            {
                breakMeterImage.fillAmount = 1f;
                breakMeterImage.color = yellowColor;
            }
        }
    }

    private void EndPull()
    {
        PlayLensDistortionReturn(force: true);

        // 타겟이 없어지는 순간이므로 Pull UI는 다시 숨김(초기 상태로)
        HidePullFadeOut();
        HideBreakMeterIfNeeded();

        if (cameraManager != null) cameraManager.EndFollowObj();
        if (destroyOnCollect && starTarget != null)
            starTarget.SetStop(false);

        if (starTarget != null)
            starTarget.OnDestroy -= HandleDestroy;

        OnChainBreak?.Invoke();
        ClearTarget();

        BroAudio.Play(cutRopeSoundId);
        BroAudio.Stop(ropeWindSoundId);
    }

    public void SetTarget(StarMover newTarget)
    {
        text.alpha = 0f;
        starTarget = newTarget;
        starRb = null;
        pullVel = Vector2.zero;
        nextPullTime = 0f;

        if (starTarget == null)
        {
            HidePullFadeOut();
            HideBreakMeterIfNeeded();
            return;
        }

        starRb = starTarget.GetComponent<Rigidbody2D>();
        if (starRb == null)
        {
            Debug.LogError("[RopePullController] Star Target에 Rigidbody2D가 없습니다.");
            starTarget = null;
            HidePullFadeOut();
            HideBreakMeterIfNeeded();
            return;
        }

        starTarget.OnDestroy += HandleDestroy;
        lastPullPosition = starTarget.transform.position;

        // SetTarget 호출되면 그때부터 Pull UI가 보이기 시작
        ShowPullFadeIn();

        if (breakMeterImage != null)
        {
            meterFill = 1f;
            breakMeterImage.fillAmount = 1f;
            breakMeterImage.color = yellowColor;

            breakMeterImage.enabled = true;
            breakMeterImage.gameObject.SetActive(true);
        }

        if (breakMeterRect != null)
        {
            followVelUI = Vector2.zero;
            lastOffscreenState = false;
        }

        if (followWhenRoped && cameraManager != null)
            cameraManager.CatchFollowObj(starTarget.transform);

        UpdateRopeLine();
    }

    private void HandleDestroy()
    {
        EndPull();
    }

    public void ClearTarget()
    {
        starTarget = null;
        starRb = null;
        pullVel = Vector2.zero;
        isDragging = false;

        if (ropeLine != null)
            ropeLine.enabled = false;

        // 타겟이 없으면 Pull UI는 숨김
        HidePullFadeOut();
        HideBreakMeterIfNeeded();
    }

    private void TryPullOnRelease(Vector2 dragEndWorld, float dragDuration)
    {
        if (Time.time < nextPullTime) return;
        nextPullTime = Time.time + pullCooldown;

        if (starRb == null || starTarget == null) return;

        Vector2 drag = dragEndWorld - dragStartWorld;
        float dist = drag.magnitude;
        if (dist < minDragWorld) return;

        float durationPenalty = Mathf.Clamp01(dragDuration / Mathf.Max(0.001f, minEffectiveDragDuration));
        float len01 = Mathf.Clamp01((dist / Mathf.Max(0.001f, maxDragWorld)) * durationPenalty);
        float lenMul = Mathf.Pow(len01, lengthPower);

        Vector2 dirToPlayer = ((Vector2)player.position - starRb.position).normalized;
        float impulse = baseImpulse * lenMul;

        Vector2 dv = dirToPlayer * (impulse / Mathf.Max(0.001f, starRb.mass));
        pullVel = dv;

        lastPullPosition = starTarget.transform.position;
    }

    // ---------------- Break Meter ----------------

    private void InitBreakMeter()
    {
        if (breakMeterImage == null) return;

        breakMeterRect = breakMeterImage.rectTransform;

        if (uiCanvas == null)
            uiCanvas = breakMeterImage.GetComponentInParent<Canvas>();

        breakMeterImage.type = Image.Type.Filled;
        breakMeterImage.fillMethod = Image.FillMethod.Radial360;
        breakMeterImage.fillOrigin = (int)Image.Origin360.Top;
        breakMeterImage.fillClockwise = false;

        meterFill = 1f;
        breakMeterImage.fillAmount = 1f;
        breakMeterImage.color = yellowColor;

        if (hideMeterWhenNoTarget)
            breakMeterImage.gameObject.SetActive(false);
    }

    private void HideBreakMeterIfNeeded()
    {
        if (breakMeterImage == null) return;
        if (hideMeterWhenNoTarget)
            breakMeterImage.gameObject.SetActive(false);
    }

    private void UpdateBreakMeter(float distFromLastPull)
    {
        if (breakMeterImage == null) return;
        if (maxRopeStretchDistance <= 0.0001f) return;

        float progress01 = Mathf.Clamp01(distFromLastPull / maxRopeStretchDistance); // 0~1
        float targetFill = 1f - progress01;

        if (meterSmooth <= 0f)
            meterFill = targetFill;
        else
            meterFill = Mathf.Lerp(meterFill, targetFill, 1f - Mathf.Exp(-meterSmooth * Time.fixedDeltaTime));

        breakMeterImage.fillAmount = meterFill;

        breakMeterImage.color = EvaluateYellowToRed(progress01);
    }

    private Color EvaluateYellowToRed(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (!useHSVColorLerp)
            return Color.Lerp(yellowColor, redColor, progress01);

        Color.RGBToHSV(yellowColor, out float h1, out float s1, out float v1);
        Color.RGBToHSV(redColor, out float h2, out float s2, out float v2);

        float hue = Mathf.LerpAngle(h1 * 360f, h2 * 360f, progress01) / 360f;
        float sat = Mathf.Lerp(s1, s2, progress01);
        float val = Mathf.Lerp(v1, v2, progress01);

        Color c = Color.HSVToRGB(hue, sat, val);
        c.a = Mathf.Lerp(yellowColor.a, redColor.a, progress01);
        return c;
    }

    // ---------------- UI Follow Smooth (Fix Jitter) ----------------

    private void UpdateBreakMeterFollowSmooth()
    {
        if (breakMeterImage == null || breakMeterRect == null) return;
        if (uiCanvas == null) return;
        if (starTarget == null) return;
        if (!breakMeterImage.gameObject.activeSelf) return;

        Camera worldCam = cam != null ? cam : Camera.main;
        if (worldCam == null) return;

        Vector3 worldPos = starTarget.transform.position + (Vector3)followOffsetWorld;
        Vector3 screenPos = worldCam.WorldToScreenPoint(worldPos);

        bool off =
            screenPos.z < 0f ||
            screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height;

        if (hideWhenOffscreen)
        {
            if (off != lastOffscreenState)
            {
                breakMeterImage.enabled = !off;
                lastOffscreenState = off;
            }
            if (off) return;
        }
        else
        {
            if (!breakMeterImage.enabled) breakMeterImage.enabled = true;
        }

        RectTransform canvasRect = uiCanvas.transform as RectTransform;

        Camera camForUI = uiCamera;
        if (camForUI == null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camForUI = uiCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, camForUI, out Vector2 localPoint))
            return;

        Vector2 target = localPoint + followOffsetUI;

        if (pixelSnap)
        {
            float sf = uiCanvas != null ? uiCanvas.scaleFactor : 1f;
            if (sf <= 0.0001f) sf = 1f;
            target.x = Mathf.Round(target.x * sf) / sf;
            target.y = Mathf.Round(target.y * sf) / sf;
        }

        if (followSmoothTime <= 0.0001f)
        {
            breakMeterRect.anchoredPosition = target;
        }
        else
        {
            Vector2 cur = breakMeterRect.anchoredPosition;
            Vector2 next = Vector2.SmoothDamp(cur, target, ref followVelUI, followSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            breakMeterRect.anchoredPosition = next;
        }
    }

    // ---------------- Lens Distortion ----------------

    private void PlayLensDistortionDown(bool force = false)
    {
        if (lensDistortion == null) CacheLensDistortion();
        if (lensDistortion == null) return;

        float target = Mathf.Clamp(lensDistortionTarget, -1f, 1f);

        if (!force && lensTween != null && lensTween.IsActive() && lensTween.IsPlaying())
            return;

        lensTween?.Kill();
        lensTween = DOTween.To(
                () => lensDistortion.intensity.value,
                x => lensDistortion.intensity.value = x,
                target,
                lensDownDuration
            )
            .SetEase(lensDownEase);
    }

    private void PlayLensDistortionReturn(bool force = false)
    {
        if (lensDistortion == null) CacheLensDistortion();
        if (lensDistortion == null) return;

        lensTween?.Kill();
        lensTween = DOTween.To(
                () => lensDistortion.intensity.value,
                x => lensDistortion.intensity.value = x,
                0f,
                lensReturnDuration
            )
            .SetEase(lensReturnEase);
    }

    // -------------------------------------------------

    private Vector2 MouseWorld2D()
    {
        if (cam == null) cam = Camera.main;

        Vector3 m = Input.mousePosition;
        m.z = (cam != null) ? -cam.transform.position.z : 0f;
        return (cam != null) ? (Vector2)cam.ScreenToWorldPoint(m) : Vector2.zero;
    }

    private void SetupRopeLine()
    {
        GameObject go = new GameObject("RopeLine");
        ropeLine = go.AddComponent<LineRenderer>();
        ropeLine.positionCount = 2;
        ropeLine.useWorldSpace = true;
        ropeLine.startWidth = lineWidth;
        ropeLine.endWidth = lineWidth;
        ropeLine.material = new Material(Shader.Find("Sprites/Default"));

        Gradient brownGradient = new Gradient();
        brownGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.36f, 0.20f, 0.09f), 0f),
                new GradientColorKey(new Color(0.59f, 0.29f, 0.00f), 0.5f),
                new GradientColorKey(new Color(0.76f, 0.60f, 0.42f), 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
        ropeLine.colorGradient = brownGradient;
        ropeLine.enabled = false;
    }

    private void UpdateRopeLine()
    {
        if (ropeLine == null || player == null || starTarget == null) return;

        ropeLine.enabled = true;
        ropeLine.SetPosition(0, player.position);
        ropeLine.SetPosition(1, starTarget.transform.position);
    }
}
