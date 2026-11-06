using UnityEngine;
using UnityEngine.SceneManagement;

// Starts itself on Play. The LEVEL now comes from the saved scene (LevelSettings),
// it is no longer built in code at run time.
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public enum State { Intro, Playing, Dead, LevelDone, GameOver, Won }

    public State state = State.Intro;
    public int level, lives, coins;
    public float timeLeft;
    public int stateFrame;
    public float graceUntil;          // no fall-death right after a spawn

    // survive a scene load
    static int pendingLives = -1;     // -1 = give a fresh StartLives
    static bool pendingStart;

    LevelInfo info;
    PlayerController player;
    string msg = "", toast = "";
    float toastTimer;
    GUIStyle hud, center;
    const int StartLives = 3;

    public bool Playing => state == State.Playing;
    public bool Unlocked => coins >= info.coins;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        I = this;
        Physics.autoSyncTransforms = true;

        var ls = FindAnyObjectByType<LevelSettings>();
        if (ls == null)
        {
            Debug.LogError("No LevelSettings in this scene. Open a Lab06 level scene (Tools > Lab06 > Bake 5 levels into scenes).");
            enabled = false;
            return;
        }
        info = ls.ToInfo();
        level = SceneManager.GetActiveScene().buildIndex;

        SetupCamera();
        CreatePlayer(info.start);

        lives = pendingLives < 0 ? StartLives : pendingLives;
        pendingLives = -1;
        coins = 0;
        timeLeft = info.time;
        graceUntil = Time.time + 0.75f;
        SetState(pendingStart ? State.Playing : State.Intro);
        pendingStart = false;
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = false;
        cam.fieldOfView = 60f;
        if (cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
    }

    void CreatePlayer(Vector3 at)
    {
        var pg = new GameObject("Player");
        pg.transform.position = at;
        var cc = pg.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(pg.transform, false);
        Destroy(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().material.color = new Color(0.2f, 0.5f, 1f);

        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(pg.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
        nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
        Destroy(nose.GetComponent<Collider>());
        nose.GetComponent<Renderer>().material.color = Color.white;

        player = pg.AddComponent<PlayerController>();
    }

    // Loading another level = loading another saved scene (its build index = level number).
    void Go(int levelIndex, bool resetLives, bool start)
    {
        pendingLives = resetLives ? -1 : lives;
        pendingStart = start;
        SceneManager.LoadScene(levelIndex);
    }

    void SetState(State s) { state = s; stateFrame = Time.frameCount; }

    public void AddCoin() { coins++; }

    public void Die(string reason)
    {
        if (!Playing) return;

        // ignore bogus "fell" deaths while the player is actually above the void
        if (reason.StartsWith("You fell") && player.transform.position.y > -5f) return;

        Debug.Log("DIE: " + reason + " at " + player.transform.position);
        lives--;
        msg = reason;
        SetState(lives <= 0 ? State.GameOver : State.Dead);
    }

    public void TryFinish()
    {
        if (!Playing) return;
        if (!Unlocked)
        {
            toast = "Collect all coins first (" + coins + "/" + info.coins + ")";
            toastTimer = 1.5f;
            return;
        }
        SetState(State.LevelDone);
    }

    void Update()
    {
        if (toastTimer > 0f) toastTimer -= Time.deltaTime;
        if (GameInput.Skip()) { Go((level + 1) % LevelBuilder.Count, true, false); return; }

        switch (state)
        {
            case State.Intro:
                if (GameInput.Confirm()) SetState(State.Playing);
                break;
            case State.Playing:
                if (GameInput.Restart()) { Go(level, false, true); break; }
                if (info.time > 0f)
                {
                    timeLeft -= Time.deltaTime;
                    if (timeLeft <= 0f) { timeLeft = 0f; Die("Time's up!"); }
                }
                break;
            case State.Dead:
                if (GameInput.Confirm()) Go(level, false, true);
                break;
            case State.LevelDone:
                if (GameInput.Confirm())
                {
                    if (level + 1 < LevelBuilder.Count) Go(level + 1, true, false);
                    else SetState(State.Won);
                }
                break;
            case State.GameOver:
            case State.Won:
                if (GameInput.Confirm()) Go(0, true, false);
                break;
        }
    }

    string OverlayText()
    {
        switch (state)
        {
            case State.Intro: return "LEVEL " + (level + 1) + ": " + info.name + "\n" + info.objective + "\n\nPress SPACE to start";
            case State.Dead: return msg + "\nLives left: " + lives + "\n\nPress SPACE to retry";
            case State.LevelDone: return level + 1 < LevelBuilder.Count ? "LEVEL COMPLETE!\n\nPress SPACE for the next level" : "FINAL LEVEL COMPLETE!\n\nPress SPACE";
            case State.GameOver: return "GAME OVER\n" + msg + "\n\nPress SPACE to restart from Level 1";
            case State.Won: return "YOU WIN!\nAll 5 levels cleared!\n\nPress SPACE to play again";
        }
        return "";
    }

    void OnGUI()
    {
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = Screen.height / s;

        if (hud == null)
        {
            hud = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            hud.normal.textColor = Color.white;
            center = new GUIStyle(hud) { fontSize = 32, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        GUI.Label(new Rect(20, 10, 1000, 30), "Level " + (level + 1) + "/" + LevelBuilder.Count + ": " + info.name, hud);
        GUI.Label(new Rect(20, 40, 1000, 30), info.objective, hud);
        string stats = "Lives: " + lives;
        if (info.coins > 0) stats += "    Coins: " + coins + "/" + info.coins;
        if (info.time > 0f) stats += "    Time: " + Mathf.CeilToInt(timeLeft) + "s";
        GUI.Label(new Rect(20, 70, 1000, 30), stats, hud);
        GUI.Label(new Rect(20, H - 40, 1100, 30), "WASD/Arrows: move    SPACE: jump    R: restart level    N: skip level (testing)", hud);

        if (toastTimer > 0f) GUI.Label(new Rect(0, H - 110, W, 40), toast, center);

        if (state != State.Playing)
        {
            var box = new Rect(W / 2 - 380, H / 2 - 150, 760, 300);
            GUI.Box(box, GUIContent.none);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(W / 2 - 360, H / 2 - 140, 720, 280), OverlayText(), center);
        }
    }
}
