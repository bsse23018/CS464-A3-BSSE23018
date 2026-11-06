using UnityEngine;

// Bounce pad mechanic: launches the player upwards using an instantaneous ForceMode.VelocityChange.
// A4: the force. VelocityChange = one kick that ignores mass, called once from OnTriggerEnter.
// It pushes UP only, because the controller rewrites the sideways speed every step.
public class BouncePad : MonoBehaviour
{
    public float launch = 11f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x, 0f, v.z);        // same launch whether falling or not
        rb.AddForce(Vector3.up * launch, ForceMode.VelocityChange);
    }
}
