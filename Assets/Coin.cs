using UnityEngine;

// A1: Coin prefab. Trigger collider + tag check. The player's Rigidbody makes the trigger fire.
public class Coin : MonoBehaviour
{
    [SerializeField] int value = 1;
    [SerializeField] float spinSpeed = 120f;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;   // tags are case-sensitive
        GameManager.Instance.AddScore(value);
        Destroy(gameObject);
    }
}
