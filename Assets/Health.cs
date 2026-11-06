using UnityEngine;

// C1: Manages player hearts and damage invulnerability.
// One Health component on the player. A hit costs hearts, then i-frames protect you.
public class Health : MonoBehaviour
{
    public int max = 3;
    public int current = 3;
    // Invulnerability frames (i-frames): temporary immunity granted after taking damage or respawning
    // so the player cannot be damaged repeatedly in consecutive physics steps while escaping hazard zones.
    public float iFrames = 1f;

    float invulnerableUntil;
    bool dead;

    public bool Invulnerable => Time.time < invulnerableUntil;

    void Awake() { current = max; }

    // Returns false when the hit is ignored (during i-frames or death),
    // allowing hazards to check continuously in OnTriggerStay without re-applying damage or knockback.
    public bool TakeDamage(int amount)
    {
        if (dead || Invulnerable) return false;

        current -= amount;
        invulnerableUntil = Time.time + iFrames;

        var gm = GameManager.Instance;
        // Check <= 0 instead of == 0: heavier hazards (such as the crusher) deal 2 or more damage,
        // which can reduce current health below zero (e.g. 1 - 2 = -1) and miss an exact == 0 check.
        if (current <= 0)
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
