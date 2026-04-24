using System.Collections.Generic;
using UnityEngine;

public class DescriptionVisControl : MonoBehaviour
{
    [Header("Panels")]
    public List<GameObject> panels = new List<GameObject>();

    [SerializeField] private GameObject playerObject;

    [Header("Distance Logic")]
    [SerializeField] private float visTriggerDistance = 3f;  // start showing at/below this distance (alpha ~ 0)
    [SerializeField] private float fullVisDistance = 2f;     // fully visible at/below this distance (alpha = 1)

    [Header("Fade Timing")]
    [Tooltip("How quickly alpha moves toward target (per second). Bigger = snappier.")]
    [SerializeField] private float fadeSpeed = 6f;

    [Tooltip("Disable the panel GameObject once it fades to ~0.")]
    [SerializeField] private bool disableWhenInvisible = true;

    // internal
    private readonly Dictionary<GameObject, CanvasGroup> _groups = new Dictionary<GameObject, CanvasGroup>();
    private GameObject _activePanel = null;

    void Start()
    {
        // Cache/add CanvasGroups and disable all panels.
        foreach (var p in panels)
        {
            if (!p) continue;

            var cg = p.GetComponent<CanvasGroup>();
            if (!cg) cg = p.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            _groups[p] = cg;

            p.SetActive(false);
        }
    }

    void Update()
    {
        if (!playerObject) return;

        // 1) Pick the closest panel within trigger range
        GameObject closest = null;
        float closestDist = float.PositiveInfinity;

        foreach (var p in panels)
        {
            if (!p) continue;

            float d = Vector3.Distance(playerObject.transform.position, p.transform.position);
            if (d <= visTriggerDistance && d < closestDist)
            {
                closestDist = d;
                closest = p;
            }
        }

        _activePanel = closest;

        // 2) Drive all panels toward their target alpha
        foreach (var p in panels)
        {
            if (!p) continue;

            var cg = GetOrAddCanvasGroup(p);

            float targetAlpha = 0f;

            if (p == _activePanel)
            {
                // Convert distance -> 0..1 (0 at triggerDistance, 1 at fullVisDistance)
                float t = InverseLerpSafe(visTriggerDistance, fullVisDistance, closestDist);
                // Slow-in/slow-out
                targetAlpha = SmoothStep01(t);

                // Make sure it is active so it can be visible
                if (!p.activeSelf) p.SetActive(true);
            }

            // Smoothly move alpha toward target
            cg.alpha = MoveTowardsExp(cg.alpha, targetAlpha, fadeSpeed, Time.deltaTime);

            // Optional: enable raycasts only when meaningfully visible
            bool visibleEnough = cg.alpha > 0.02f;
            cg.blocksRaycasts = visibleEnough;
            cg.interactable = false;

            // If not active and fully faded, disable
            if (disableWhenInvisible && p != _activePanel)
            {
                if (cg.alpha <= 0.001f && p.activeSelf)
                    p.SetActive(false);
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject p)
    {
        if (_groups.TryGetValue(p, out var cg) && cg) return cg;

        cg = p.GetComponent<CanvasGroup>();
        if (!cg) cg = p.AddComponent<CanvasGroup>();
        _groups[p] = cg;
        return cg;
    }

    // Like Mathf.InverseLerp, but safe if a==b and works for a>b too.
    private float InverseLerpSafe(float a, float b, float v)
    {
        if (Mathf.Abs(a - b) < 1e-6f) return (v <= b) ? 1f : 0f;
        return Mathf.Clamp01((v - a) / (b - a));
    }

    // SmoothStep from 0..1 to 0..1 (slow-in, slow-out)
    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    // Exponential-ish smoothing toward target (frame-rate independent feel)
    private float MoveTowardsExp(float current, float target, float speed, float dt)
    {
        // Equivalent to: current += (target-current) * (1 - exp(-speed*dt))
        float k = 1f - Mathf.Exp(-Mathf.Max(0f, speed) * dt);
        return Mathf.Lerp(current, target, k);
    }
}
