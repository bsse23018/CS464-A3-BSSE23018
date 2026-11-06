using UnityEngine;

// B1 + B2: Patrol -> Chase -> Return. Kinematic Rigidbody, arrival by DISTANCE (not ==).
[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrol : MonoBehaviour
{
    public enum State { Patrol, Chase, Return }

    public Transform[] waypoints;
    public bool pingPong = false;               // false = loop
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;             // slower than the player's 6
    public float chaseRange = 6f;               // starts chasing inside this
    public float giveUpRange = 9f;              // longer, so it can't flicker at one edge
    public float arriveDistance = 0.15f;        // bigger than one step
    public float waitAtPoint = 0.3f;
    public Renderer bodyRenderer;
    public Color patrolColor = new Color(0.2f, 0.25f, 0.8f);
    public Color chaseColor = new Color(1f, 0.25f, 0.1f);

    public State state { get; private set; } = State.Patrol;

    Rigidbody rb;
    Transform player;
    int index = 1;
    int dir = 1;
    float waitUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();   // one prefab fits every level
        if (p != null) player = p.transform;
        if (waypoints == null || waypoints.Length < 2) index = 0;
        SetState(State.Patrol);
    }

    void SetState(State s)
    {
        state = s;
        if (bodyRenderer != null)
            bodyRenderer.material.color = s == State.Chase ? chaseColor : patrolColor;
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        switch (state)
        {
            case State.Patrol:
                if (InRange(chaseRange)) { SetState(State.Chase); break; }
                PatrolStep();
                break;

            case State.Chase:
                if (!InRange(giveUpRange)) { SetState(State.Return); break; }
                MoveToward(player.position, chaseSpeed);
                break;

            case State.Return:
                if (InRange(chaseRange)) { SetState(State.Chase); break; }
                Vector3 back = waypoints[index].position;
                if (Arrived(back)) SetState(State.Patrol);
                else MoveToward(back, patrolSpeed);
                break;
        }
    }

    void PatrolStep()
    {
        if (Time.time < waitUntil) return;
        Vector3 target = waypoints[index].position;
        if (Arrived(target))
        {
            Advance();
            waitUntil = Time.time + waitAtPoint;
            return;
        }
        MoveToward(target, patrolSpeed);
    }

    void Advance()
    {
        int n = waypoints.Length;
        if (n < 2) return;
        if (pingPong)
        {
            index += dir;
            if (index >= n) { dir = -1; index = n - 2; }
            else if (index < 0) { dir = 1; index = 1; }
        }
        else index = (index + 1) % n;
    }

    void MoveToward(Vector3 target, float speed)
    {
        target.y = rb.position.y;                         // stay at our height
        Vector3 d = target - rb.position;
        if (d.sqrMagnitude > 0.0001f)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(d), 0.2f));
        rb.MovePosition(Vector3.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
    }

    // arrival by distance, not ==
    bool Arrived(Vector3 target)
    {
        Vector3 a = rb.position, b = target;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) < arriveDistance;
    }

    // seen inside one range, lost outside a longer one
    bool InRange(float range)
    {
        if (player == null) return false;
        Vector3 a = rb.position, b = player.position;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) < range;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, giveUpRange);
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
            if (waypoints[i] != null && i + 1 < waypoints.Length && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
    }
}
