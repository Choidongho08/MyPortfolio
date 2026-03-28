using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using TMPro;

public class CameraManager : MonoBehaviour
{
    public UnityEvent<bool> OnTabClicked;

    [Header("VCams")]
    [SerializeField] private CinemachineCamera vcamDefault;
    [SerializeField] private CinemachineCamera vcamFollow;
    [SerializeField] private CinemachineCamera vFullCam;

    [SerializeField] private RectTransform upPanel;
    [SerializeField] private RectTransform upPanelPos;
    [SerializeField] private RectTransform downPanel;
    [SerializeField] private RectTransform downPanelPos;

    [Header("Minimap UI")]
    [SerializeField] private CanvasGroup minimap;
    [SerializeField] private CanvasGroup moveText;
    [SerializeField] private RectTransform minimapRect; // ✅ 추가 (미니맵 루트 RectTransform)

    private Tween minimapTween; // ✅ 트윈 관리

    private CinemachineCamera currentCam;

    private int activePriority = 10;
    private int backPriority = 1;

    [Header("Zoom")]
    [Range(0.5f, 5f)]
    [SerializeField] private float followZoomFactor = 0.85f;
    [SerializeField] private float catchZoomFactor = 0.85f;

    [Header("Blend Speeds")]
    [Tooltip("따라가기 전환 속도(작을수록 빠름)")]
    [SerializeField] private float followBlendTime = 0.12f;

    [Tooltip("복귀 전환 속도(작을수록 빠름)")]
    [SerializeField] private float returnBlendTime = 0.05f;

    [SerializeField] private CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

    private CinemachineBrain brain;
    private CinemachineBlendDefinition savedBlend;

    private float savedDefaultOrtho;

    private Transform currentTarget;
    private bool isFollowing;
    public bool isFullCamActive = false;

    [Header("Distance UI (Camera -> UI LocalY)")]
    [SerializeField] private RectTransform distanceUI;
    [SerializeField] private Vector2 targetWorldPos = new Vector2(250f, 250f);
    [SerializeField] private float uiMaxDistance = 500f;
    [SerializeField] private float uiMinLocalY = -250f;
    [SerializeField] private float uiMaxLocalY = 250f;

    private void Awake()
    {
        if (vcamDefault == null || vcamFollow == null || vFullCam == null)
        {
            Debug.LogError("[CameraManager] 모든 VCam을 연결해 주세요.");
            enabled = false;
            return;
        }

        var mainCam = Camera.main;
        if (mainCam != null)
            brain = mainCam.GetComponent<CinemachineBrain>();

        if (brain != null)
            savedBlend = brain.DefaultBlend;

        savedDefaultOrtho = vcamDefault.Lens.OrthographicSize;

        vcamDefault.Priority = activePriority;
        vcamFollow.Priority = backPriority;
        vFullCam.Priority = backPriority;

        isFollowing = false;

        // ✅ 시작 상태: 꺼진 상태(세로 0 + 투명)
        if (minimap != null)
        {
            minimap.alpha = 0f;
            minimap.blocksRaycasts = false;
            minimap.interactable = false;
        }
        if (minimapRect != null)
        {
            minimapRect.localScale = new Vector3(1f, 0f, 1f);
            // Pivot은 (0.5,0.5)면 TV처럼 중앙에서 켜짐. (0.5,0)면 아래에서 위로 켜짐.
        }
    }

    private void LateUpdate()
    {
        if (isFollowing && currentTarget == null)
            EndFollowObj();

        UpdateDistanceUIByCameraPos();

        if (!isFollowing)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ActivateFullCam();
                OnTabClicked?.Invoke(true);
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                DeactivateFullCam();
                OnTabClicked?.Invoke(false);
            }
        }
    }

    public void BeginFollowObj(Transform target)
    {
        if (target == null) return;
        moveText.alpha = 1f;

        MovePanels(upPanelPos, downPanelPos);

        currentTarget = target;
        isFollowing = true;

        savedDefaultOrtho = vcamDefault.Lens.OrthographicSize;

        vcamFollow.Follow = target;
        vcamFollow.LookAt = null;

        float targetSize = savedDefaultOrtho * followZoomFactor;

        DOTween.To(
            () => vcamFollow.Lens.OrthographicSize,
            x => vcamFollow.Lens.OrthographicSize = x,
            targetSize,
            2f
        ).SetEase(Ease.Linear).OnComplete(SetConfiner);

        SetBlendTime(followBlendTime);

        vcamDefault.Priority = backPriority;
        vcamFollow.Priority = activePriority;
        vFullCam.Priority = backPriority;
    }

    public void CatchFollowObj(Transform target)
    {
        if (target == null) return;

        MovePanels(upPanelPos, downPanelPos);

        currentTarget = target;
        isFollowing = true;

        savedDefaultOrtho = vcamDefault.Lens.OrthographicSize;

        vcamFollow.Follow = target;
        vcamFollow.LookAt = null;

        float targetSize = savedDefaultOrtho * catchZoomFactor;

        DOTween.To(
            () => vcamFollow.Lens.OrthographicSize,
            x => vcamFollow.Lens.OrthographicSize = x,
            targetSize,
            0.4f
        ).SetEase(Ease.OutQuad).OnComplete(SetConfiner);

        SetBlendTime(followBlendTime);

        vcamDefault.Priority = backPriority;
        vcamFollow.Priority = activePriority;
        vFullCam.Priority = backPriority;
    }

    private void SetConfiner()
    {
        var confiner = vcamFollow.GetComponent<CinemachineConfiner2D>();

        DOVirtual.DelayedCall(0.1f, () =>
        {
            confiner.InvalidateLensCache();
            confiner.InvalidateBoundingShapeCache();
        });
    }

    private void MovePanels(RectTransform upTarget, RectTransform downTarget)
    {
        // ✅ 미니맵: TV 켜지듯 등장
        ShowMinimapTV(true);

        if (upPanel != null && upTarget != null)
        {
            upPanel.DOKill();
            upPanel.DOAnchorPos(upTarget.anchoredPosition, 0.2f);
        }

        if (downPanel != null && downTarget != null)
        {
            downPanel.DOKill();
            downPanel.DOAnchorPos(downTarget.anchoredPosition, 0.2f);
        }
    }

    public void EndFollowObj()
    {
        // ✅ 미니맵: TV 꺼지듯 사라짐(세로 0)
        ShowMinimapTV(false);

        isFollowing = false;
        currentTarget = null;

        SetBlendTime(returnBlendTime);

        upPanel.DOAnchorPosY(2100f, 0.2f);
        downPanel.DOAnchorPosY(-1000f, 0.2f);

        vcamFollow.Follow = null;
        vcamFollow.LookAt = null;

        vcamDefault.Lens.OrthographicSize = savedDefaultOrtho;

        vcamDefault.Priority = activePriority;
        vcamFollow.Priority = backPriority;
        vFullCam.Priority = backPriority;

        RestoreBlendLater(returnBlendTime + 0.01f);
    }

    // =======================
    // ✅ TV on/off Minimap Anim
    // =======================
    private void ShowMinimapTV(bool show)
    {
        if (minimap == null || minimapRect == null) return;

        minimapTween?.Kill();

        if (show)
        {
            minimap.blocksRaycasts = true;
            minimap.interactable = true;

            // 시작 상태 보정
            minimap.alpha = 0f;
            minimapRect.localScale = new Vector3(4.736695f, 0f, 1f);

            minimapTween = DOTween.Sequence()
                .Join(minimap.DOFade(1f, 0.15f).SetEase(Ease.OutQuad))
                // TV 켜짐 느낌: 0 -> 1.1 -> 1 (살짝 튀는 느낌)
                .Join(minimapRect.DOScaleY(4.836695f, 0.12f).SetEase(Ease.OutBack))
                .Append(minimapRect.DOScaleY(4.736695f, 0.08f).SetEase(Ease.OutQuad));
        }
        else
        {
            minimap.blocksRaycasts = false;
            minimap.interactable = false;

            minimapTween = DOTween.Sequence()
                .Join(minimap.DOFade(0f, 0.12f).SetEase(Ease.OutQuad))
                .Join(minimapRect.DOScaleY(0f, 0.12f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    // 완전 종료 상태 고정
                    minimap.alpha = 0f;
                    minimapRect.localScale = new Vector3(1f, 0f, 1f);
                });
        }
    }

    private void SetBlendTime(float t)
    {
        if (brain == null) return;
        brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, Mathf.Max(0f, t));
    }

    private void RestoreBlendLater(float delay)
    {
        if (brain == null) return;
        CancelInvoke(nameof(RestoreBlendNow));
        Invoke(nameof(RestoreBlendNow), delay);
    }

    private void RestoreBlendNow()
    {
        if (brain == null) return;
        brain.DefaultBlend = savedBlend;
    }

    private void ActivateFullCam()
    {
        if (isFullCamActive) return;
        isFullCamActive = true;

        vFullCam.Priority = activePriority + 1;
        vcamDefault.Priority = backPriority;
        vcamFollow.Priority = backPriority;
    }

    private void DeactivateFullCam()
    {
        if (!isFullCamActive) return;
        isFullCamActive = false;

        vFullCam.Priority = backPriority;

        if (isFollowing)
        {
            vcamDefault.Priority = backPriority;
            vcamFollow.Priority = activePriority;
        }
        else
        {
            vcamDefault.Priority = activePriority;
            vcamFollow.Priority = backPriority;
        }
    }

    private void UpdateDistanceUIByCameraPos()
    {
        if (distanceUI == null) return;

        Transform camTr = null;
        if (brain != null && brain.OutputCamera != null)
            camTr = brain.OutputCamera.transform;
        else if (Camera.main != null)
            camTr = Camera.main.transform;

        if (camTr == null) return;

        Vector2 camPos2D = new Vector2(camTr.position.x, camTr.position.y);
        float dist = Vector2.Distance(camPos2D, targetWorldPos);

        float t = 1f - Mathf.Clamp01(dist / Mathf.Max(0.0001f, uiMaxDistance));
        float y = Mathf.Lerp(uiMinLocalY, uiMaxLocalY, t);

        Vector2 ap = distanceUI.anchoredPosition;
        ap.y = y;
        distanceUI.anchoredPosition = ap;
    }
}