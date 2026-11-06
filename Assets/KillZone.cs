using UnityEngine;

// Kill zone trigger: catches the player falling off platforms and deducts a life.
// C2: one wide trigger under the level. Falling costs a life.
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance.LoseLife("You fell!");
    }
}
