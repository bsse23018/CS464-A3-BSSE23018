using UnityEngine;

// Sits on the "Level N" object of each saved scene. You can edit these numbers in the Inspector.
public class LevelSettings : MonoBehaviour
{
    public string levelName = "Level";
    [TextArea] public string objective = "";
    public int coins;          // coins needed to unlock the goal
    public float time;         // 0 = no time limit
    public Vector3 start;      // where the player spawns

    public LevelInfo ToInfo()
    {
        return new LevelInfo { name = levelName, objective = objective, coins = coins, time = time, start = start };
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(start, 0.6f);   // shows the player spawn in the Scene view
    }
}
