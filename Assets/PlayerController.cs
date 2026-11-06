using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController I;

    [Header("Movement")]
    public float speed = 6f;
    public float jumpSpeed = 8f;
    public float speedMultiplier = 1f;      // changed by BoostSpeed (power-up)

    public float BoostRemaining { get; private set; }

    Rigidbody rb;
    Health health;
    Renderer[] rends;
    Vector2 input;
    bool jumpQueued;
    float noControlUntil;                   // knockback: steering waits until this time
    Coroutine boostRoutine;

    void Awake()
    {
        I = this;
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        rends = GetComponentsInChildren<Renderer>();
        rb.freezeRotation = true;
        Physics.gravity = new Vector3(0f, -20f, 0f);   // snappier jumps than -9.81
    }

    void Update()
    {
        input = GameInput.Move();
        if (GameInput.Jump()) jumpQueued = true;

        // blink while the i-frames last, so you can see them
        bool blinkOff = health != null && health.Invulnerable && ((int)(Time.time * 12f) % 2 == 0);
        foreach (var r in rends) r.enabled = !blinkOff;
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        bool canMove = gm == null || gm.PlayerCanMove;
        Vector3 v = rb.linearVelocity;

        if (!canMove)
        {
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            jumpQueued = false;
            return;
        }

        // camera-relative movement
        Vector3 f = Vector3.forward, r = Vector3.right;
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            f = cam.forward; f.y = 0f; f.Normalize();
            r = cam.right;   r.y = 0f; r.Normalize();
        }
        Vector3 move = f * input.y + r * input.x;
        move = Vector3.ClampMagnitude(move, 1f) * speed * speedMultiplier;

        if (Time.time >= noControlUntil)
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        if (move.sqrMagnitude > 0.01f && Time.time >= noControlUntil)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(move), 0.25f));

        // Jump and ground check: only execute a queued jump if verified grounded.
        // A sphere cast below the player prevents infinite air jumping while reliably tolerating uneven terrain.
        if (jumpQueued && Grounded())
        {
            Vector3 cur = rb.linearVelocity;
            rb.linearVelocity = new Vector3(cur.x, jumpSpeed, cur.z);
        }
        jumpQueued = false;
    }

    // Ground check: casts a sphere downwards to detect colliders on non-trigger layers.
    // Ignores triggers (like coins or hazards) so jumping is only permitted on physical surfaces.
    bool Grounded()
    {
        return Physics.SphereCast(rb.position, 0.4f, Vector3.down, out _, 0.75f, ~0, QueryTriggerInteraction.Ignore);
    }

    // B4: Knockback logic:
    // Applies an immediate physics impulse while disabling player steering input until 'noControlUntil'.
    // Steering waits so player input in FixedUpdate does not immediately wipe out the velocity change.
    public void Knockback(Vector3 push, float seconds)
    {
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, v.y, 0f);
        rb.AddForce(push, ForceMode.Impulse);
        noControlUntil = Time.time + seconds;
    }

    // C3: Speed boost duration and timer management:
    // The timer lives on the PlayerController because the pickup GameObject is destroyed upon collection.
    // Refresh rule: picking up a second boost while one is active resets/refreshes the full duration
    // rather than stacking speeds or discarding the pickup.
    public void BoostSpeed(float multiplier, float seconds)
    {
        if (boostRoutine != null) StopCoroutine(boostRoutine);
        boostRoutine = StartCoroutine(BoostFor(multiplier, seconds));
    }

    IEnumerator BoostFor(float multiplier, float seconds)
    {
        speedMultiplier = multiplier;
        BoostRemaining = seconds;
        while (BoostRemaining > 0f)
        {
            BoostRemaining -= Time.deltaTime;
            yield return null;
        }
        BoostRemaining = 0f;
        speedMultiplier = 1f;
        boostRoutine = null;
    }

    public void Teleport(Vector3 p)
    {
        rb.linearVelocity = Vector3.zero;
        rb.position = p;
        transform.position = p;
        noControlUntil = 0f;
    }
}
