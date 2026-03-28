using Ami.BroAudio;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Rope : MonoBehaviour
{
    public event Action OnFinishRope;
    public event Action<StarMover> OnCatchStar;

    [Header("Audio")]
    [SerializeField] private SoundID bgmAudio;
    [SerializeField] private SoundID cutRopeSoundId;
    [SerializeField] private SoundID ropeWindSoundId;

    [Header("Movement")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float speedByCharge = 10f;
    private float maxLifeTime = 2.5f;
    [SerializeField] private float maxDistance = 18f;

    [Header("Bias Turn (Screen): y>x면 왼쪽 / y<x면 오른쪽")]
    [SerializeField] private float biasTurnDegPerSec = 180f;
    [SerializeField] private float maxBiasAngleDeg = 35f;
    [Range(0f, 0.49f)]
    [SerializeField] private float deadZone = 0.07f;
    [Range(0.2f, 4f)]
    [SerializeField] private float exponent = 1.4f;
    [SerializeField] private bool scaleByDistanceFromDiagonal = true;
    [SerializeField] private bool invertLeftRight = false;

    [Header("Rope Visual")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private float lineWidth = 0.05f;

    private Rigidbody2D rb;
    private Transform origin;

    private Vector2 launchDir;
    private float speed;

    private Vector2 startPos;
    private float alive;
    private float biasAngleDeg;

    public void Launch(Transform originTransform, Vector2 initialDir, float charge01)
    {
        origin = originTransform;
        launchDir = initialDir.normalized;
        speed = baseSpeed + speedByCharge * Mathf.Clamp01(charge01);

        maxLifeTime = 10.5f + 8.0f * Mathf.Clamp01(charge01);

        startPos = rb.position;
        alive = 0f;
        biasAngleDeg = 0f;

        rb.linearVelocity = launchDir * speed;

        SetupLineIfNeeded();
        UpdateLine();

        BroAudio.Play(ropeWindSoundId);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        SetupLineIfNeeded();
    }

    private void FixedUpdate()
    {
        if (origin == null)
        {
            OnFinishRope?.Invoke();
            BroAudio.Play(cutRopeSoundId);
            BroAudio.Stop(ropeWindSoundId);
            Destroy(gameObject);
            return;
        }

        alive += Time.fixedDeltaTime;
        if (alive >= maxLifeTime || Vector2.Distance(startPos, rb.position) >= maxDistance)
        {
            OnFinishRope?.Invoke();
            BroAudio.Play(cutRopeSoundId);
            BroAudio.Stop(ropeWindSoundId);
            Destroy(gameObject);
            return;
        }

        float nx = (Screen.width > 0) ? Mathf.Clamp01(Input.mousePosition.x / Screen.width) : 0.5f;
        float ny = (Screen.height > 0) ? Mathf.Clamp01(Input.mousePosition.y / Screen.height) : 0.5f;

        float delta = ny - nx;
        float abs = Mathf.Abs(delta);

        float t = 0f;
        if (abs > deadZone)
            t = (abs - deadZone) / (1f - deadZone);

        t = Mathf.Clamp01(Mathf.Pow(t, exponent));
        float strength01 = scaleByDistanceFromDiagonal ? t : (abs > deadZone ? 1f : 0f);

        float sign = (delta >= 0f) ? 1f : -1f;
        if (invertLeftRight) sign *= -1f;

        float turnThisFrame = sign * biasTurnDegPerSec * strength01 * Time.fixedDeltaTime;

        biasAngleDeg += turnThisFrame;
        if (maxBiasAngleDeg > 0f)
            biasAngleDeg = Mathf.Clamp(biasAngleDeg, -maxBiasAngleDeg, maxBiasAngleDeg);

        Vector2 newDir = Quaternion.Euler(0f, 0f, biasAngleDeg) * launchDir;
        rb.linearVelocity = newDir.normalized * speed;
    }

    private void LateUpdate()
    {
        UpdateLine();
    }

    private void SetupLineIfNeeded()
    {
        if (line != null) return;

        var go = new GameObject("RopeLine");
        go.transform.SetParent(transform);
        line = go.AddComponent<LineRenderer>();
        Gradient brownGradient = new Gradient();

brownGradient.SetKeys(
    new GradientColorKey[]
    {
        new GradientColorKey(new Color(0.36f, 0.20f, 0.09f), 0f), // Dark Brown
        new GradientColorKey(new Color(0.59f, 0.29f, 0.00f), 0.5f), // Brown
        new GradientColorKey(new Color(0.76f, 0.60f, 0.42f), 1f), // Light Brown
    },
    new GradientAlphaKey[]
    {
        new GradientAlphaKey(1f, 0f),
        new GradientAlphaKey(1f, 1f),
    }
);
        line.colorGradient = brownGradient;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void UpdateLine()
    {
        if (line == null || origin == null) return;
        line.SetPosition(0, origin.position);
        line.SetPosition(1, transform.position);
    }

    public void SetSize(float size)
    {
        transform.localScale = new Vector3(size, size, size);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Area"))
        {
            OnFinishRope?.Invoke();
            BroAudio.Play(cutRopeSoundId);
            BroAudio.Stop(ropeWindSoundId);
            Destroy(gameObject);
        }
        if (collision.CompareTag("Star"))
        {
            OnCatchStar?.Invoke(collision.GetComponent<StarMover>());
            BroAudio.Play(bgmAudio);
            BroAudio.Stop(ropeWindSoundId);
            Destroy(gameObject);
        }
    }
}