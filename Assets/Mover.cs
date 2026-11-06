using UnityEngine;

// Moves an object back and forth between two world points.
// Used for moving platforms, sweeping hazards and moving coins.
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Rigidbody))]
public class Mover : MonoBehaviour
{
    public Vector3 a, b;
    public float speed = 3f;     // units per second
    public float phase = 0f;     // 0..1 offset so objects do not move in sync
    public Vector3 Delta { get; private set; }

    float t;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start() { transform.position = Pos(); }

    Vector3 Pos()
    {
        float d = Mathf.Max(0.01f, Vector3.Distance(a, b));
        float u = Mathf.PingPong(t * speed / d + phase, 1f);
        return Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, u));
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.Playing) { Delta = Vector3.zero; return; }
        t += Time.deltaTime;
        Vector3 old = transform.position;
        transform.position = Pos();
        Delta = transform.position - old;
    }
}
