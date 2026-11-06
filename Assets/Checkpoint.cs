using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Renderer flag;
    bool activated;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance.SetCheckpoint(transform);
        if (activated) return;
        activated = true;
        if (flag != null) flag.material.color = new Color(0.2f, 1f, 0.3f);
        GameManager.Instance.ShowMessage("Checkpoint!", 1.2f);
    }
}
