using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController I;

    public float speed = 6f;
    public float jumpSpeed = 7.5f;
    public float gravity = -22f;
    const float KillY = -12f;

    CharacterController cc;
    float vy;

    void Awake()
    {
        I = this;
        cc = GetComponent<CharacterController>();
    }

    public void Teleport(Vector3 p)
    {
        cc.enabled = false;
        transform.position = p;
        transform.rotation = Quaternion.identity;
        cc.enabled = true;
        vy = 0f;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.Playing) return;

        Transform cam = Camera.main.transform;
        Vector3 f = cam.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.right; r.y = 0f; r.Normalize();
        Vector2 input = GameInput.Move();
        Vector3 dir = f * input.y + r * input.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        bool grounded = cc.isGrounded;
        if (grounded && vy < 0f) vy = -2f;
        if (grounded && GameInput.Jump() && Time.frameCount != gm.stateFrame) vy = jumpSpeed;
        vy += gravity * Time.deltaTime;

        Vector3 platform = Vector3.zero;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            var m = hit.collider.GetComponent<Mover>();
            if (m != null) platform = m.Delta;
        }

        cc.Move((dir * speed + Vector3.up * vy) * Time.deltaTime + platform);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);

        if (!cc.isGrounded && transform.position.y < KillY && Time.time > gm.graceUntil)
            gm.Die("You fell into the void!");
    }
}