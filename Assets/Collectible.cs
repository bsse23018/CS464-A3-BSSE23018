using UnityEngine;

public class Collectible : MonoBehaviour
{
    void Update() { transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World); }

    void OnTriggerEnter(Collider o)
    {
        if (o.GetComponentInParent<PlayerController>() == null) return;
        GameManager.I.AddCoin();
        Destroy(gameObject);
    }
}
