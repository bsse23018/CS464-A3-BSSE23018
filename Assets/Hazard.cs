using UnityEngine;

public class Hazard : MonoBehaviour
{
    void OnTriggerEnter(Collider o) { Hit(o); }
    void OnTriggerStay(Collider o) { Hit(o); }

    void Hit(Collider o)
    {
        if (o.GetComponentInParent<PlayerController>() != null && GameManager.I != null)
            GameManager.I.Die("You touched a hazard!");
    }
}
