using UnityEngine;

// Goal zone trigger: unlocks and triggers level completion once target score is reached.
// Red while locked, green once the score reaches targetScore.
public class GoalZone : MonoBehaviour
{
    public Renderer[] parts;
    int lastState = -1;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        int s = gm.GoalOpen ? 1 : 0;
        if (s == lastState) return;
        lastState = s;
        Color c = s == 1 ? new Color(0.2f, 1f, 0.3f) : new Color(0.85f, 0.2f, 0.2f);
        foreach (var r in parts) if (r != null) r.material.color = c;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) GameManager.Instance.TryFinish();
    }
}
