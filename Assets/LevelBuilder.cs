using UnityEngine;

public struct LevelInfo
{
    public string name, objective;
    public int coins;       // coins needed to unlock the goal
    public float time;      // 0 = no time limit
    public Vector3 start;
}

// Builds all 5 levels from Unity primitives (cubes, cylinders, capsules).
public static class LevelBuilder
{
    public const int Count = 5;

    static Material baseMat;
    static Transform root;

    static readonly Color GroundC = new Color(0.30f, 0.55f, 0.35f);
    static readonly Color StoneC  = new Color(0.45f, 0.47f, 0.55f);
    static readonly Color WoodC   = new Color(0.55f, 0.38f, 0.22f);
    static readonly Color RedC    = new Color(0.90f, 0.15f, 0.15f);
    static readonly Color GoldC   = new Color(1.00f, 0.82f, 0.10f);

    public static LevelInfo Build(int index, Transform parent)
    {
        root = parent;
        if (baseMat == null)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(tmp);
        }
        switch (index)
        {
            case 0: return Level1();
            case 1: return Level2();
            case 2: return Level3();
            case 3: return Level4();
            default: return Level5();
        }
    }

    // ---------------------------------------------------------------- helpers

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, bool trigger = false)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(root, false);
        g.transform.position = pos;
        g.transform.localScale = scale;
        var m = new Material(baseMat);
        m.color = color;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (trigger) g.GetComponent<Collider>().isTrigger = true;
        return g;
    }

    // Platform whose TOP surface is at height y.
    static GameObject Plat(float x, float y, float z, float sx, float sz, Color c, float th = 1f)
    {
        return Prim(PrimitiveType.Cube, "Platform", new Vector3(x, y - th / 2f, z), new Vector3(sx, th, sz), c);
    }

    static void Wall(float x, float z, float sx, float sz, float h = 2.5f)
    {
        Prim(PrimitiveType.Cube, "Wall", new Vector3(x, h / 2f, z), new Vector3(sx, h, sz), StoneC);
    }

    static void Block(float x, float z, float size, float h, Color c)
    {
        Prim(PrimitiveType.Cube, "Block", new Vector3(x, h / 2f, z), new Vector3(size, h, size), c);
    }

    static GameObject Coin(Vector3 p)
    {
        var g = Prim(PrimitiveType.Cylinder, "Coin", p, new Vector3(0.8f, 0.08f, 0.8f), GoldC, true);
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        g.AddComponent<Collectible>();
        return g;
    }

    static void Spike(float x, float z)
    {
        var g = Prim(PrimitiveType.Cube, "Spike", new Vector3(x, 0.45f, z), Vector3.one * 0.9f, RedC, true);
        g.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
        g.AddComponent<Hazard>();
    }

    static Mover Move(GameObject g, Vector3 a, Vector3 b, float speed, float phase = 0f)
    {
        var m = g.AddComponent<Mover>();
        m.a = a; m.b = b; m.speed = speed; m.phase = phase;
        return m;
    }

    static void Sweeper(Vector3 a, Vector3 b, Vector3 size, float speed, float phase = 0f)
    {
        var g = Prim(PrimitiveType.Cube, "Sweeper", a, size, RedC, true);
        g.AddComponent<Hazard>();
        Move(g, a, b, speed, phase);
    }

    static void Goal(float x, float z)
    {
        var disc = Prim(PrimitiveType.Cylinder, "Goal", new Vector3(x, 0.08f, z), new Vector3(3f, 0.08f, 3f), Color.gray);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        var bc = disc.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1f, 25f, 1f);
        bc.center = new Vector3(0f, 12f, 0f);

        var beam = Prim(PrimitiveType.Cylinder, "GoalBeam", new Vector3(x, 3f, z), new Vector3(0.4f, 3f, 0.4f), Color.gray);
        Object.DestroyImmediate(beam.GetComponent<Collider>());

        var gz = disc.AddComponent<GoalZone>();
        gz.parts = new[] { disc.GetComponent<Renderer>(), beam.GetComponent<Renderer>() };
    }

    static void AddGuard(float speed, float range, float halfAngle, float spotTime, params Vector3[] pts)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        g.name = "Guard";
        g.transform.SetParent(root, false);
        g.transform.position = pts[0] + Vector3.up;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        var m = new Material(baseMat);
        m.color = new Color(0.15f, 0.15f, 0.6f);
        g.GetComponent<Renderer>().sharedMaterial = m;

        var lg = new GameObject("Vision");
        lg.transform.SetParent(g.transform, false);
        lg.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Spot;
        l.range = range;
        l.spotAngle = halfAngle * 2f;
        l.intensity = 3f;
        l.color = Color.yellow;

        var gd = g.AddComponent<Guard>();
        gd.points = pts;
        gd.speed = speed;
        gd.range = range;
        gd.halfAngle = halfAngle;
        gd.spotTime = spotTime;
        gd.vision = l;
    }

    // ----------------------------------------------------------------- LEVEL 1
    // Collect all coins. Open field, no hazards. (Teaches the basics.)
    static LevelInfo Level1()
    {
        Plat(0, 0, 0, 26, 26, GroundC);
        Wall(0, 13.5f, 28, 1); Wall(0, -13.5f, 28, 1);
        Wall(13.5f, 0, 1, 26); Wall(-13.5f, 0, 1, 26);

        Block(-4, 2, 1.5f, 1.5f, WoodC);
        Block(4, 2, 1.5f, 1.5f, WoodC);
        Block(0, -6, 1.5f, 1.5f, WoodC);

        Vector3[] coins =
        {
            new Vector3(-9, 1, -8), new Vector3(9, 1, -8),
            new Vector3(-9, 1, 0),  new Vector3(9, 1, 0),
            new Vector3(-6, 1, 7),  new Vector3(6, 1, 7),
            new Vector3(0, 1, 3),   new Vector3(0, 1, -3)
        };
        foreach (var c in coins) Coin(c);

        Goal(0, 10);
        return new LevelInfo
        {
            name = "Coin Garden",
            objective = "Collect all 8 coins, then step on the goal",
            coins = 8, time = 0f, start = new Vector3(0, 1.2f, -10)
        };
    }

    // ----------------------------------------------------------------- LEVEL 2
    // Reach the goal before the timer runs out. Jumping across gaps.
    static LevelInfo Level2()
    {
        Plat(0, 0, 0, 8, 8, GroundC);
        float[] xs = { 0f, 2f, -2f, 2.5f, -1.5f, 2f, -2f, 0f };
        float prevFront = 4f;
        for (int i = 0; i < xs.Length; i++)
        {
            float size = Mathf.Lerp(4f, 3f, i / 7f);
            float gap = 2.2f + 0.12f * i;
            float z = prevFront + gap + size / 2f;
            Plat(xs[i], 0f, z, size, size, Color.Lerp(StoneC, WoodC, i / 7f));
            prevFront = z + size / 2f;
        }
        float fz = prevFront + 2.5f + 5f;
        Plat(0, 0, fz, 10, 10, GroundC);
        Goal(0, fz);

        return new LevelInfo
        {
            name = "Beat the Clock",
            objective = "Reach the goal before time runs out. Don't fall!",
            coins = 0, time = 32f, start = new Vector3(0, 1.2f, 0)
        };
    }

    // ----------------------------------------------------------------- LEVEL 3
    // Survive hazards (spikes + sweeping bars) and collect 5 coins.
    static LevelInfo Level3()
    {
        Plat(0, 0, 21, 16, 56, GroundC);
        Wall(-8.5f, 21, 1, 57); Wall(8.5f, 21, 1, 57);
        Wall(0, -7.5f, 18, 1);  Wall(0, 49.5f, 18, 1);

        foreach (float x in new[] { -6f, -4f, -2f, 2f, 4f, 6f }) Spike(x, 6f);
        foreach (float x in new[] { -6f, -4f, -2f, 0f, 2f }) Spike(x, 14f);

        Sweeper(new Vector3(-6, 0.5f, 22), new Vector3(6, 0.5f, 22), new Vector3(4, 1, 1), 3f);
        Sweeper(new Vector3(6, 1.25f, 28), new Vector3(-6, 1.25f, 28), new Vector3(3, 2.5f, 1), 3.5f, 0.25f);
        Sweeper(new Vector3(-6, 0.5f, 34), new Vector3(6, 0.5f, 34), new Vector3(4, 1, 1), 4f);
        Sweeper(new Vector3(6, 1.25f, 36), new Vector3(-6, 1.25f, 36), new Vector3(3, 2.5f, 1), 4f, 0.5f);

        foreach (float x in new[] { -6f, -4f, -2f, 0f, 2f }) Spike(x, 41f);
        foreach (float x in new[] { 6f, 4f, 2f, 0f, -2f }) Spike(x, 44f);

        Coin(new Vector3(0, 1, 6));
        Coin(new Vector3(5, 1, 14));
        Coin(new Vector3(0, 1, 25));
        Coin(new Vector3(-6, 1, 31));
        Coin(new Vector3(6, 1, 42.5f));

        Goal(0, 46.5f);
        return new LevelInfo
        {
            name = "Spike Run",
            objective = "Collect 5 coins and reach the goal. Avoid the red hazards!",
            coins = 5, time = 0f, start = new Vector3(0, 1.2f, -4)
        };
    }

    // ----------------------------------------------------------------- LEVEL 4
    // Stealth: avoid the guards' vision cones (yellow lights). Use cover.
    static LevelInfo Level4()
    {
        Plat(0, 0, 0, 30, 30, new Color(0.25f, 0.30f, 0.30f));
        Wall(0, 15.5f, 32, 1, 3f); Wall(0, -15.5f, 32, 1, 3f);
        Wall(15.5f, 0, 1, 30, 3f); Wall(-15.5f, 0, 1, 30, 3f);

        // cover pillars
        foreach (float x in new[] { -8f, 0f, 8f }) { Block(x, -3.5f, 2.5f, 3f, StoneC); Block(x, 3.5f, 2.5f, 3f, StoneC); }
        foreach (float x in new[] { -8f, 8f }) { Block(x, -10.5f, 2.5f, 3f, StoneC); Block(x, 10.5f, 2.5f, 3f, StoneC); }

        AddGuard(3.2f, 9f, 30f, 0.4f, new Vector3(-12, 0, -7), new Vector3(12, 0, -7));
        AddGuard(3.6f, 9f, 30f, 0.4f, new Vector3(12, 0, 0),  new Vector3(-12, 0, 0));
        AddGuard(3.2f, 9f, 30f, 0.4f, new Vector3(-12, 0, 7), new Vector3(12, 0, 7));

        Coin(new Vector3(-12, 1, -10.5f));
        Coin(new Vector3(12, 1, -3.5f));
        Coin(new Vector3(-12, 1, 3.5f));
        Coin(new Vector3(12, 1, 10.5f));
        Coin(new Vector3(4, 1, -3.5f));
        Coin(new Vector3(-4, 1, 10.5f));

        Goal(0, 13);
        return new LevelInfo
        {
            name = "Guard Duty",
            objective = "Collect 6 coins and reach the goal without being spotted",
            coins = 6, time = 0f, start = new Vector3(0, 1.2f, -13)
        };
    }

    // ----------------------------------------------------------------- LEVEL 5
    // Final gauntlet: moving platforms + sweepers + spikes + guard + timer.
    static void MovingPlat(float z, float x1, float x2, float speed, float phase, bool coin)
    {
        var a = new Vector3(x1, -0.5f, z);
        var b = new Vector3(x2, -0.5f, z);
        var g = Plat(x1, 0f, z, 4f, 4f, WoodC);
        Move(g, a, b, speed, phase);
        if (coin)
        {
            var c = Coin(new Vector3(x1, 1f, z));
            Move(c, a + Vector3.up * 1.5f, b + Vector3.up * 1.5f, speed, phase);
        }
    }

    static LevelInfo Level5()
    {
        Plat(0, 0, 0, 8, 8, GroundC);

        // A: moving platforms over the void
        MovingPlat(8.5f, -5f, 5f, 2.5f, 0f, false);
        MovingPlat(14.5f, 5f, -5f, 3f, 0.3f, true);
        MovingPlat(20.5f, -5f, 5f, 3.5f, 0.6f, false);

        // B: hazard hall
        Plat(0, 0, 37, 16, 24, GroundC);
        Sweeper(new Vector3(-6, 0.5f, 30), new Vector3(6, 0.5f, 30), new Vector3(4, 1, 1), 3f);
        Sweeper(new Vector3(6, 1.25f, 36), new Vector3(-6, 1.25f, 36), new Vector3(3, 2.5f, 1), 3.5f, 0.3f);
        foreach (float x in new[] { -6f, -4f, -2f, 2f, 4f, 6f }) Spike(x, 41f);
        foreach (float x in new[] { -7f, -5f, -3f }) Spike(x, 44f);
        AddGuard(3f, 8f, 30f, 0.35f, new Vector3(-6, 0, 47), new Vector3(6, 0, 47));

        Coin(new Vector3(6, 1, 27.5f));
        Coin(new Vector3(-6, 1, 33));
        Coin(new Vector3(6, 1, 38.5f));
        Coin(new Vector3(0, 1, 42.5f));
        Coin(new Vector3(6, 1, 45.5f));

        // C: narrow stepping stones guarded by sweepers
        float[] sx = { 0f, 2f, -2f, 0f };
        float[] sz = { 52.5f, 58f, 63.5f, 69f };
        for (int i = 0; i < 4; i++) Plat(sx[i], 0f, sz[i], 3f, 3f, StoneC);
        Sweeper(new Vector3(-5, 1.25f, 55.25f), new Vector3(5, 1.25f, 55.25f), new Vector3(2, 2.5f, 1), 4f);
        Sweeper(new Vector3(5, 1.25f, 60.75f), new Vector3(-5, 1.25f, 60.75f), new Vector3(2, 2.5f, 1), 4f, 0.5f);
        Coin(new Vector3(-2, 1, 63.5f));

        Plat(0, 0, 78, 10, 10, GroundC);
        Goal(0, 78);

        return new LevelInfo
        {
            name = "The Gauntlet",
            objective = "Collect 7 coins and reach the goal in 90 seconds",
            coins = 7, time = 90f, start = new Vector3(0, 1.2f, 0)
        };
    }
}
