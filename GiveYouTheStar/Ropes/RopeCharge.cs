using Assets._01.Develop.CDH.Code.Ropes;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class RopeCharge : MonoBehaviour
{
    [Header("Charge Settings")]
    [Tooltip("차지 속도(초당 몇 번 왕복하느냐 느낌). 값이 클수록 더 빨리 왔다갔다.")]
    [SerializeField] private float pingPongSpeed = 1.2f;

    [Tooltip("차지 최소/최대(0~1). 예: 0.1~1이면 완전 0은 안 나오게 가능")]
    [Range(0f, 1f)] [SerializeField] private float minCharge01 = 0f;
    [Range(0f, 1f)] [SerializeField] private float maxCharge01 = 1f;

    [Tooltip("차지 곡선(선형이 싫으면 커브로 맛 조절)")]
    [SerializeField] private ChargingDataSO[] chargingDatas;

    [Header("UI (Visual Scale)")]
    [SerializeField] private Transform chargeVisual; // X축 스케일로 표현할 오브젝트

    [Header("Rotation Limit")]
    [Tooltip("마우스를 바라보는 회전 제한 각도 (Z축 기준)")]
    [SerializeField] private float minRotationAngle = -60f;
    [SerializeField] private float maxRotationAngle = 60f;

    [Header("Events")]
    [Tooltip("마우스 뗐을 때 호출(0~1 차지 값)")]
    public UnityEvent<float> onReleaseCharge01;

    // =========================
    // Color (DOTween)
    // =========================
    [Header("Color (Near Max -> Red)")]
    [SerializeField] private bool colorizeByCharge = true;
    [SerializeField] private Color nearMinColor = Color.white;          // 시작 색(원하면 Inspector에서 바꾸기)
    [SerializeField] private Color nearMaxColor = new Color(1f, 0.2f, 0.2f, 1f); // 빨강
    [SerializeField] private float colorTweenDuration = 0.06f;
    [Tooltip("1에 가까울수록 더 빠르게 빨개지게(감도). 1=선형, 2~4 추천")]
    [Range(1f, 6f)] [SerializeField] private float redPower = 2.2f;
    [Tooltip("이 값 이상부터 빨개지기 시작(0~1). 예: 0.6이면 60%부터 빨개짐")]
    [Range(0f, 1f)] [SerializeField] private float startRedFrom = 0.0f;

    public float Charge01 => charge01;
    public bool IsCharging => isCharging;

    private bool isCharging;
    private float t;
    private float charge01;

    // 캐싱
    private SpriteRenderer chargeSR;
    private Color initialColor;
    private Tween colorTween;

    // 커브 랜덤을 매 프레임 뽑으면 깜빡임 생길 수 있어서 "차지 시작 시 1번"만 뽑도록
    private AnimationCurve selectedCurve;

    private void Awake()
    {
        if (chargeVisual != null)
        {
            chargeSR = chargeVisual.GetComponent<SpriteRenderer>();
            if (chargeSR != null)
            {
                initialColor = chargeSR.color;
                // Inspector에서 nearMinColor를 따로 안 만지면 "원래 색"으로 시작하게
                if (nearMinColor == Color.white) // 기본값이면 원래색으로 덮어쓰기
                    nearMinColor = initialColor;
            }
        }
    }

    private void OnDisable()
    {
        colorTween?.Kill();
        colorTween = null;
    }

    public void ChargeStart() => StartCharge();

    public void ShowVisual(bool isShow)
    {
        if (chargeSR == null && chargeVisual != null)
            chargeSR = chargeVisual.GetComponent<SpriteRenderer>();

        if (chargeSR != null)
            chargeSR.enabled = isShow;
    }

    public void ChargeRelease() => ReleaseCharge();

    private void Update()
    {
        if (isCharging)
            UpdateCharge();
        else
            ApplyVisual(0f);

        RotateVisualToMouse();
    }

    private void StartCharge()
    {
        isCharging = true;
        t = 0f;
        charge01 = minCharge01;

        // ✅ 차지 시작할 때 커브 1개 고정
        selectedCurve = null;
        if (chargingDatas != null && chargingDatas.Length > 0)
        {
            int idx = Random.Range(0, chargingDatas.Length);
            selectedCurve = chargingDatas[idx] != null ? chargingDatas[idx].chargeCurve : null;
        }
        ShowVisual(true);
        ApplyVisual(charge01);
    }

    private void UpdateCharge()
    {
        t += Time.deltaTime * pingPongSpeed;

        float raw = Mathf.PingPong(t, 1f);

        float curved = selectedCurve != null ? selectedCurve.Evaluate(raw) : raw;
        charge01 = Mathf.Lerp(minCharge01, maxCharge01, curved);

        ApplyVisual(charge01);
    }

    private void ReleaseCharge()
    {
        isCharging = false;
        ApplyVisual(charge01);
        onReleaseCharge01?.Invoke(charge01);
        ShowVisual(false);
    }

    private void ApplyVisual(float value01)
    {
        // 스케일
        if (chargeVisual != null)
        {
            Vector3 scale = chargeVisual.localScale;
            scale.x = Mathf.Lerp(0f, 25f, value01);
            chargeVisual.localScale = scale;
        }

        // 색 (차지 높을수록 빨강)
        if (colorizeByCharge)
            ApplyColorByCharge(value01);
    }

    private void ApplyColorByCharge(float value01)
    {
        if (chargeSR == null)
        {
            if (chargeVisual == null) return;
            chargeSR = chargeVisual.GetComponent<SpriteRenderer>();
            if (chargeSR == null) return;
            initialColor = chargeSR.color;
            if (nearMinColor == Color.white)
                nearMinColor = initialColor;
        }

        // 0~1 정규화 + "몇 %부터 빨개질지"
        float x = Mathf.InverseLerp(startRedFrom, 1f, Mathf.Clamp01(value01));
        x = Mathf.Pow(x, redPower);

        Color target = Color.Lerp(nearMinColor, nearMaxColor, x);

        // DOTween으로 부드럽게
        colorTween?.Kill();
        colorTween = chargeSR
            .DOColor(target, colorTweenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(false);
    }

    private void RotateVisualToMouse()
    {
        if (chargeVisual == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = chargeVisual.position.z;

        Vector3 dir = (mouseWorldPos - chargeVisual.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float clampedAngle = Mathf.Clamp(angle, minRotationAngle, maxRotationAngle);
        chargeVisual.rotation = Quaternion.Euler(0f, 0f, clampedAngle);
    }
}
