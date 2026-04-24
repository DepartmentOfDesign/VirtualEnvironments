using UnityEngine;

public class ScaleWithDistance : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;   // Drag your Camera here (or leave empty to auto-use Camera.main)

    [Header("Distance Settings")]
    [Tooltip("Distance at which scaling begins (farther away).")]
    public float startDistance = 5f;

    [Tooltip("Distance at which the object reaches maxScale (closer).")]
    public float endDistance = 1.5f;

    [Header("Scaling")]
    [Tooltip("Maximum scale multiplier at endDistance.")]
    public float maxScale = 2f;

    private Vector3 originalScale;

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogWarning("ScaleWithDistance: No cameraTransform assigned and no Camera.main found.");
        }

        originalScale = transform.localScale;
    }

    void Update()
    {
        if (cameraTransform == null)
            return;

        float dist = Vector3.Distance(cameraTransform.position, transform.position);

        // Ensure distances make sense; if user accidentally sets start < end, we handle it gracefully
        float far = Mathf.Max(startDistance, endDistance);
        float close = Mathf.Min(startDistance, endDistance);

        // Outside far distance → no scaling
        if (dist >= far)
        {
            transform.localScale = originalScale;
            return;
        }

        // Inside close distance → max scale
        if (dist <= close)
        {
            transform.localScale = originalScale * maxScale;
            return;
        }

        // Interpolate between far (1x) and close (maxScale)
        float t = Mathf.InverseLerp(far, close, dist); // far → 0, close → 1 as player gets closer

        float scaleMultiplier = Mathf.Lerp(1f, maxScale, t);
        transform.localScale = originalScale * scaleMultiplier;
    }
}
