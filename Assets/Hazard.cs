using UnityEngine;

// B3 + B4: ONE script for every danger (spikes, guard, crusher slab). Trigger collider.
// It asks Health every physics step; Health says no during i-frames.
public class Hazard : MonoBehaviour
{
    public int damage = 1;
    public float knockback = 9f;
    public float knockTime = 0.25f;     // steering is off this long, so the push is visible

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out Health health)) return;
        // Knockback is only applied if TakeDamage returned true:
        // During i-frames or when dead, TakeDamage returns false. If knockback were applied
        // unconditionally during i-frames, the player would be continuously knocked around
        // on every physics frame while trying to escape the hazard zone.
        if (!health.TakeDamage(damage)) return;          // no hit, no knockback

        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;
        Vector3 away = body.position - transform.position;
        away.y = 0f;                                      // flat, away from the hazard
        if (away.sqrMagnitude < 0.01f) away = Vector3.back;
        Vector3 push = away.normalized * knockback;

        if (other.TryGetComponent(out PlayerController p))
            p.Knockback(push, knockTime);
    }
}
