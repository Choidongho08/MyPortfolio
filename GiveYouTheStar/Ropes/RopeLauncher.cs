using _01.Develop.LSW._01._Scripts.Manager;
using Ami.BroAudio;
using Assets._01.Develop.CDH.Code.Fasdfags;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RopeLauncher : MonoBehaviour
{
    public UnityEvent OnRopeFinish;
    public UnityEvent OnRopeCatchStar;

    [Header("Sound")]
    [SerializeField] private SoundID throwRopeSoundID;

    [Header("References")]
    [SerializeField] private ArrowUI arrowUI;
    [SerializeField] private Transform playerTrm;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private RopePullController ropePullController;
    [SerializeField] private Rope ropePrefab;


    [Header("Spawn")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

    [Header("Angle Limit")]
    [SerializeField] private float minShootAngle = -60f; // degrees
    [SerializeField] private float maxShootAngle = 60f;  // degrees

    private Camera cam;
    private Rope curRope;

    private void Awake()
    {
        cam = Camera.main;
    }

    public void LaunchByCharge(float charge01)
    {
        if (ropePrefab == null) return;

        Vector3 spawnPos = transform.position + (Vector3)spawnOffset;
        curRope = Instantiate(ropePrefab, spawnPos, Quaternion.identity);

        Vector2 worldMouse = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 rawDir = worldMouse - (Vector2)spawnPos;

        float angleDeg = Mathf.Atan2(rawDir.y, rawDir.x) * Mathf.Rad2Deg;
        float clampedAngle = Mathf.Clamp(angleDeg, minShootAngle, maxShootAngle);

        Vector2 dir = new Vector2(
            Mathf.Cos(clampedAngle * Mathf.Deg2Rad),
            Mathf.Sin(clampedAngle * Mathf.Deg2Rad)
        ).normalized;

        gameManager.SetMaxTimer(PlayerStatManager.Instance.GetCurrentMaxTimer());
        curRope.SetSize(PlayerStatManager.Instance.GetCurrentRopeSize());
        ropePullController.SetMaxRopeStretchDistance(PlayerStatManager.Instance.GetCurrentFailDist());

        BroAudio.Play(throwRopeSoundID);

        curRope.Launch(playerTrm, dir, charge01);
        curRope.OnFinishRope += HandleCurRopeEnd;
        curRope.OnCatchStar += HandleCurRopeCatchStar;

        arrowUI.SetPivotWorld(curRope.transform);
        arrowUI.Show(true);

        if (cameraManager != null)
            cameraManager.BeginFollowObj(curRope.transform);
    }

    private void HandleCurRopeCatchStar(StarMover starTrm)
    {
        starTrm.SetStop(true);
        ropePullController.SetTarget(starTrm);
        OnRopeCatchStar?.Invoke();
        arrowUI.Show(false);
    }

    private void HandleCurRopeEnd()
    {
        if (curRope == null) return;
        curRope.OnFinishRope -= HandleCurRopeEnd;
        curRope.OnCatchStar -= HandleCurRopeCatchStar;
        OnRopeFinish?.Invoke();
        arrowUI.Show(false);
    }
}