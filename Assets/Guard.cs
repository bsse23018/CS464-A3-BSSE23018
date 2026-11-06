using UnityEngine;

// Patrols between points. Spots the player inside a vision cone (with line of sight).
public class Guard : MonoBehaviour
{
    public Vector3[] points;
    public float speed = 3f;
    public float range = 9f;
    public float halfAngle = 30f;
    public float spotTime = 0.4f;   // seconds in view before you are caught
    public Light vision;

    int idx = 1;
    float seen;

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.Playing || points == null || points.Length < 2) return;
        Patrol();
        Look(gm);
    }

    void Patrol()
    {
        Vector3 target = points[idx];
        target.y = transform.position.y;
        Vector3 to = target - transform.position;
        if (to.sqrMagnitude < 0.01f) { idx = (idx + 1) % points.Length; return; }
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 6f * Time.deltaTime);
    }

    void Look(GameManager gm)
    {
        var p = PlayerController.I;
        if (p == null) return;

        Vector3 eye = transform.position + Vector3.up * 0.5f;
        Vector3 d = p.transform.position - eye;
        float dist = d.magnitude;
        Vector3 flat = new Vector3(d.x, 0f, d.z);
        bool visible = false;

        if (flat.magnitude < 1.4f) visible = true; // too close
        else if (dist <= range && Vector3.Angle(transform.forward, flat) <= halfAngle)
        {
            if (Physics.Raycast(eye, d / dist, out RaycastHit hit, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore))
                visible = hit.collider.GetComponentInParent<PlayerController>() != null;
        }

        seen = visible ? seen + Time.deltaTime : Mathf.Max(0f, seen - 2f * Time.deltaTime);
        if (vision != null) vision.color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(seen / spotTime));
        if (seen >= spotTime) gm.Die("A guard spotted you!");
    }
}
