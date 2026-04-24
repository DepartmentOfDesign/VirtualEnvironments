using UnityEngine;

public class SelfRotation : MonoBehaviour
{

    [Header("Rotation (Local Space)")]
    [Tooltip("Axis to rotate around, expressed in THIS object's local coordinates.")]
    public Vector3 rotationAxisLocal = Vector3.up;

    [Tooltip("Base angular speed in degrees/second (when not in a slow zone).")]
    public float baseSpeedDegPerSec = 90f;

    [Tooltip("Initial angle offset in degrees (0–360) to desynchronize objects.")]
    [Range(0f, 360f)]
    public float initialAngleOffsetDeg = 0f;

    [Header("Slow Zones (Front/Back Holds)")]
    [Tooltip("Half-width of the slow zone (in degrees) around 0/180/360.")]
    [Range(0f, 90f)]
    public float slowZoneDegrees = 18f;

    [Tooltip("Speed multiplier at the exact hold angles (0/180/360).")]
    [Range(0.01f, 1f)]
    public float minSpeedMultiplier = 0.15f;

    [Tooltip("0 = very smooth easing, 1 = sharper transition.")]
    [Range(0f, 1f)]
    public float sharpness = 0.35f;

    [Header("Optional")]
    public bool useUnscaledTime = false;

    private Quaternion _initialLocalRotation;
    private Vector3 _axisN;
    private float _angleDeg; // 0..360

    private void Awake()
    {
        _initialLocalRotation = transform.localRotation;

        _axisN = rotationAxisLocal.sqrMagnitude > 1e-8f
            ? rotationAxisLocal.normalized
            : Vector3.up;

        // Apply initial phase offset
        _angleDeg = Mathf.Repeat(initialAngleOffsetDeg, 360f);
    }

    private void OnValidate()
    {
        if (rotationAxisLocal.sqrMagnitude < 1e-8f)
            rotationAxisLocal = Vector3.up;

        slowZoneDegrees = Mathf.Max(0f, slowZoneDegrees);
        baseSpeedDegPerSec = Mathf.Max(0f, baseSpeedDegPerSec);
        minSpeedMultiplier = Mathf.Clamp(minSpeedMultiplier, 0.01f, 1f);
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        float speedMult = ComputeHoldMultiplier(_angleDeg);
        _angleDeg = Mathf.Repeat(
            _angleDeg + baseSpeedDegPerSec * speedMult * dt,
            360f
        );

        // Apply rotation around LOCAL axis relative to the starting local rotation
        transform.localRotation =
            _initialLocalRotation * Quaternion.AngleAxis(_angleDeg, _axisN);
    }

    private float ComputeHoldMultiplier(float angleDeg)
    {
        if (slowZoneDegrees <= 0f) return 1f;

        float d0 = Mathf.Abs(Mathf.DeltaAngle(angleDeg, 0f));    // also covers 360
        float d180 = Mathf.Abs(Mathf.DeltaAngle(angleDeg, 180f));
        float d = Mathf.Min(d0, d180);

        if (d >= slowZoneDegrees) return 1f;

        float t = d / slowZoneDegrees;

        float smooth = t * t * (3f - 2f * t); // SmoothStep
        float biased = Mathf.Lerp(
            smooth,
            Mathf.Pow(t, 1f + 6f * sharpness),
            sharpness
        );

        return Mathf.Lerp(minSpeedMultiplier, 1f, biased);
    }
}
