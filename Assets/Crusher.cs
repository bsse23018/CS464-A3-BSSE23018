using System.Collections;
using UnityEngine;

// B3: a danger that warns first. Rest (nothing) -> Warn (yellow marker) -> Strike (slab + collider on).
public class Crusher : MonoBehaviour
{
    public float restTime = 2f;
    public float warnTime = 0.8f;      // 0.5 s or more, so a player can react
    public float strikeTime = 0.5f;
    public float startDelay = 0f;      // staggers several crushers
    public GameObject warning;
    public GameObject slab;            // has the trigger collider + Hazard (damage 2)

    IEnumerator Start()
    {
        slab.SetActive(false);
        warning.SetActive(false);
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        while (true)
        {
            yield return new WaitForSeconds(restTime);
            warning.SetActive(true);
            yield return new WaitForSeconds(warnTime);
            warning.SetActive(false);
            slab.SetActive(true);
            yield return new WaitForSeconds(strikeTime);
            slab.SetActive(false);
        }
    }
}
