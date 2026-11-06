using UnityEngine;

// C1: one Health component on the player. A hit costs hearts, then i-frames protect you.
public class Health : MonoBehaviour
{
    public int max = 3;
    public int current = 3;
    public float iFrames = 1f;      // seconds nothing can hurt you after a hit

    float invulnerableUntil;
    bool dead;

    public bool Invulnerable => Time.time < invulnerableUntil;

    void Awake() { current = max; }

    // Returns false when the hit is ignored, so a hazard may ask every physics step.
    public bool TakeDamage(int amount)
    {
        if (dead || Invulnerable) return false;

        current -= amount;
        invulnerableUntil = Time.time + iFrames;

        var gm = GameManager.Instance;
        if (current <= 0)            // <= 0, never == 0 (a crusher does 2 damage)
        {
            current = 0;
            dead = true;
            if (gm != null) { gm.RefreshHud(); gm.LoseLife("Out of hearts!"); }
        }
        else if (gm != null) gm.RefreshHud();
        return true;
    }

    public void ResetFull(float protectSeconds)
    {
        current = max;
        dead = false;
        invulnerableUntil = Time.time + protectSeconds;
        if (GameManager.Instance != null) GameManager.Instance.RefreshHud();
    }
}
