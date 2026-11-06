using UnityEngine;

// C3: pickup only. It calls the player's BoostSpeed; the TIMER runs on the player.
public class PowerUp : MonoBehaviour
{
    public float multiplier = 1.6f;
    public float duration = 5f;

    Vector3 basePos;

    void Start() { basePos = transform.position; }

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.2f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out PlayerController p)) return;

        bool refreshing = p.BoostRemaining > 0f;
        p.BoostSpeed(multiplier, duration);
        if (GameManager.Instance != null)
            GameManager.Instance.ShowMessage(refreshing ? "Boost refreshed to full time!" : "Speed boost!", 1.5f);
        Destroy(gameObject);
    }
}
