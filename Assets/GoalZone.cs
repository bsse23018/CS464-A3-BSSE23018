using UnityEngine;

public class GoalZone : MonoBehaviour
{
    public Renderer[] parts;

    void Update()
    {
        bool open = GameManager.I != null && GameManager.I.Unlocked;
        Color c = open ? new Color(0.2f, 1f, 0.3f) : new Color(0.8f, 0.2f, 0.2f);
        foreach (var r in parts) if (r != null) r.material.color = c;   // .material = this object's own copy
    }

    void OnTriggerEnter(Collider o) { Check(o); }
    void OnTriggerStay(Collider o) { Check(o); }

    void Check(Collider o)
    {
        if (o.GetComponentInParent<PlayerController>() != null) GameManager.I.TryFinish();
    }
}
