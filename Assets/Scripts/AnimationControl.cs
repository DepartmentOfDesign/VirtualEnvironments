using UnityEngine;
using System.Collections; // Required for Coroutines

public class LoopingAnimation : MonoBehaviour
{
    [Header("Animator / Clip")]
    public Animator animator;

    [Tooltip("Animator state name that contains the disassembly motion.")]
    public string stateName = "Disassemble";

    [Tooltip("Animator layer index.")]
    public int layer = 0;

    [Tooltip("Assign the AnimationClip used by the state (needed for clip length).")]
    public AnimationClip clip;

    [Header("Playback")]
    [Tooltip("Multiplier on the clip's normal speed. 1 = realtime. 2 = twice as fast.")]
    public float animationSpeedMultiplier = 1f;

    [Tooltip("Wait at fully open (t=1).")]
    public float holdAtOpenSeconds = 0.75f;

    [Tooltip("Wait at fully closed (t=0).")]
    public float holdAtClosedSeconds = 0.35f;

    [Header("Optional desync")]
    [Range(0f, 1f)]
    public float startNormalizedTime = 0f;

    private Coroutine _co;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (!animator || !clip || string.IsNullOrEmpty(stateName))
        {
            Debug.LogError($"{nameof(LoopingAnimation)}: Missing animator / clip / stateName.", this);
            enabled = false;
            return;
        }

        // Important: we will drive time ourselves.
        animator.speed = 0f;

        _co = StartCoroutine(RunPingPong());
    }

    private void OnDisable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;

        if (animator) animator.speed = 1f; // restore default behavior if desired
    }

    private IEnumerator RunPingPong()
    {
        float t = Mathf.Clamp01(startNormalizedTime);
        int dir = +1; // +1 forward, -1 backward

        // Apply initial pose immediately
        SampleAt(t);

        while (true)
        {
            // ---- Move in current direction until reaching an endpoint ----
            while (true)
            {
                float dt = Time.deltaTime;

                // Convert "speed multiplier" into normalized-time delta.
                // normalizedDelta = (seconds advanced / clipLengthSeconds)
                float clipLen = Mathf.Max(0.0001f, clip.length);
                float step = (dt * Mathf.Max(0f, animationSpeedMultiplier)) / clipLen;

                t += dir * step;
                t = Mathf.Clamp01(t);

                SampleAt(t);

                // reached end?
                if (dir > 0 && t >= 1f) break;
                if (dir < 0 && t <= 0f) break;

                yield return null;
            }

            // ---- Hold at the endpoint ----
            if (dir > 0)
            {
                if (holdAtOpenSeconds > 0f)
                    yield return new WaitForSeconds(holdAtOpenSeconds);
            }
            else
            {
                if (holdAtClosedSeconds > 0f)
                    yield return new WaitForSeconds(holdAtClosedSeconds);
            }

            // ---- Flip direction ----
            dir = -dir;
        }
    }

    private void SampleAt(float normalizedTime01)
    {
        // Force the Animator to sample the state at a specific normalized time.
        animator.Play(stateName, layer, normalizedTime01);

        // Apply immediately without advancing time.
        animator.Update(0f);
    }
}