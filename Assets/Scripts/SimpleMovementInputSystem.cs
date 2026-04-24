using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Look")]
    public float lookSensitivity = 1.5f;
    public float maxLookX = 80f;
    public float minLookX = -80f;

    [Header("Height Clamp (Y Axis)")]
    public float minHeight = 1.5f;
    public float maxHeight = 1.8f;

    [Header("Proximity Slowdown")]
    [Tooltip("Objects that will slow the camera when you get close.")]
    public List<Transform> slowDownTargets = new List<Transform>();

    [Tooltip("Distance at which slowdown starts to apply.")]
    public float slowDownRadius = 2f;

    [Range(0f, 1f)]
    [Tooltip("Multiplier for movement speed when near a target (0 = stop, 1 = no change).")]
    public float moveSlowMultiplier = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Multiplier for look sensitivity when near a target.")]
    public float lookSlowMultiplier = 0.3f;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float yaw;   // left/right rotation
    private float pitch; // up/down rotation

    private CharacterController controller;

    // Store the original speeds so we can restore them
    private float baseMoveSpeed;
    private float baseLookSensitivity;

    // Current effective speeds (used in Move/Look)
    private float currentMoveSpeed;
    private float currentLookSensitivity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        controller = GetComponent<CharacterController>();

        // Initialize yaw/pitch from the existing transform
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        // Cache base speeds
        baseMoveSpeed = moveSpeed;
        baseLookSensitivity = lookSensitivity;

        currentMoveSpeed = baseMoveSpeed;
        currentLookSensitivity = baseLookSensitivity;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        UpdateProximitySlowdown();
        Move();
        Look();
        ClampHeight();
    }

    /// <summary>
    /// Adjusts currentMoveSpeed and currentLookSensitivity
    /// based on distance to any slowDownTargets.
    /// </summary>
    void UpdateProximitySlowdown()
    {
        bool isNear = false;

        if (slowDownTargets != null && slowDownTargets.Count > 0)
        {
            Vector3 camPos = transform.position;

            foreach (Transform t in slowDownTargets)
            {
                if (t == null) continue;

                float dist = Vector3.Distance(camPos, t.position);
                if (dist <= slowDownRadius)
                {
                    isNear = true;
                    break;
                }
            }
        }

        if (isNear)
        {
            currentMoveSpeed = baseMoveSpeed * moveSlowMultiplier;
            currentLookSensitivity = baseLookSensitivity * lookSlowMultiplier;
        }
        else
        {
            currentMoveSpeed = baseMoveSpeed;
            currentLookSensitivity = baseLookSensitivity;
        }
    }

    void Move()
    {
        // Convert input to world space direction
        Vector3 dir = transform.forward * moveInput.y + transform.right * moveInput.x;
        dir.Normalize();

        // Movement amount (use currentMoveSpeed instead of moveSpeed)
        Vector3 movement = dir * currentMoveSpeed * Time.deltaTime;

        // Apply movement through the Character Controller
        controller.Move(movement);
    }

    void Look()
    {
        // Use currentLookSensitivity instead of lookSensitivity
        yaw += lookInput.x * currentLookSensitivity;
        pitch -= lookInput.y * currentLookSensitivity;
        pitch = Mathf.Clamp(pitch, minLookX, maxLookX);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void ClampHeight()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }
}
