using UnityEngine;

public class AutoDoor : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;
    [SerializeField] private float triggerDistance = 1f;

    [SerializeField] private GameObject doorLeft;
    [SerializeField] private GameObject doorRight;

    [SerializeField] private Vector3 leftDirection = Vector3.left;
    [SerializeField] private Vector3 righrDirection = Vector3.right; // keeping your original field name

    [SerializeField] private float totalMoveLength = 1.2f;
    [SerializeField] private float openAnimationTime = 1f; // seconds

    [Header("Easing")]
    [Tooltip("Higher = sharper S-curve. 0 = SmoothStep. Typical: 6~12")]
    [Range(0f, 20f)]
    [SerializeField] private float sigmoidK = 8f;

    private Vector3 _leftClosedPos;
    private Vector3 _rightClosedPos;
    private Vector3 _leftOpenPos;
    private Vector3 _rightOpenPos;

    private enum DoorState { Closed, Opening, Open, Closing }
    private DoorState _state = DoorState.Closed;

    // progress 0..1 where 0 = closed, 1 = open
    private float _progress01 = 0f;

    private void Start()
    {
        if (!doorLeft || !doorRight)
        {
            Debug.LogError($"{nameof(AutoDoor)}: doorLeft/doorRight not assigned.", this);
            enabled = false;
            return;
        }

        _leftClosedPos = doorLeft.transform.localPosition;
        _rightClosedPos = doorRight.transform.localPosition;

        Vector3 leftDirN = (leftDirection.sqrMagnitude > 1e-8f) ? leftDirection.normalized : Vector3.left;
        Vector3 rightDirN = (righrDirection.sqrMagnitude > 1e-8f) ? righrDirection.normalized : Vector3.right;

        _leftOpenPos = _leftClosedPos + leftDirN * totalMoveLength;
        _rightOpenPos = _rightClosedPos + rightDirN * totalMoveLength;

        // Ensure initial pose matches state/progress
        ApplyPose(_progress01);
    }

    private void Update()
    {
        if (playerObject == null) return;

        float dist = Vector3.Distance(playerObject.transform.position, transform.position);
        bool shouldBeOpen = dist <= triggerDistance;

        // State transitions
        if (shouldBeOpen)
        {
            if (_state == DoorState.Closed || _state == DoorState.Closing)
                _state = DoorState.Opening;
        }
        else
        {
            if (_state == DoorState.Open || _state == DoorState.Opening)
                _state = DoorState.Closing;
        }

        // Animate
        float duration = Mathf.Max(0.0001f, openAnimationTime);
        float delta = Time.deltaTime / duration;

        if (_state == DoorState.Opening)
        {
            _progress01 = Mathf.Clamp01(_progress01 + delta);
            ApplyPose(_progress01);

            if (_progress01 >= 1f)
                _state = DoorState.Open;
        }
        else if (_state == DoorState.Closing)
        {
            _progress01 = Mathf.Clamp01(_progress01 - delta);
            ApplyPose(_progress01);

            if (_progress01 <= 0f)
                _state = DoorState.Closed;
        }
    }

    private void ApplyPose(float progress01)
    {
        float eased = EaseSlowInOut(progress01);

        doorLeft.transform.localPosition = Vector3.LerpUnclamped(_leftClosedPos, _leftOpenPos, eased);
        doorRight.transform.localPosition = Vector3.LerpUnclamped(_rightClosedPos, _rightOpenPos, eased);
    }

    // Slow-in / slow-out easing:
    // - If sigmoidK == 0: SmoothStep
    // - Else: normalized logistic sigmoid (true S-curve)
    private float EaseSlowInOut(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (sigmoidK <= 0.0001f)
            return t01 * t01 * (3f - 2f * t01); // SmoothStep

        float k = sigmoidK;

        float s = Sigmoid(k * (t01 - 0.5f));
        float s0 = Sigmoid(k * (0f - 0.5f));
        float s1 = Sigmoid(k * (1f - 0.5f));

        return (s - s0) / (s1 - s0);
    }

    private float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }
}
