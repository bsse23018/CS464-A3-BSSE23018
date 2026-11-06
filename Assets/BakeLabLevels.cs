using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Tools > Lab06 > Bake 5 levels into scenes
// Runs LevelBuilder in EDIT mode and SAVES each level as a real scene:
// Assets/Scenes/Lab06_Level01.unity ... Lab06_Level05.unity
public static class BakeLabLevels
{
    [MenuItem("Tools/Lab06/Bake 5 levels into scenes")]
    static void Bake()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");

        var cache = new Dictionary<string, Material>();
        var buildList = new List<EditorBuildSettingsScene>();

        for (int i = 0; i < LevelBuilder.Count; i++)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("Level " + (i + 1));
            LevelInfo info = LevelBuilder.Build(i, root.transform);

            var ls = root.AddComponent<LevelSettings>();
            ls.levelName = info.name;
            ls.objective = info.objective;
            ls.coins = info.coins;
            ls.time = info.time;
            ls.start = info.start;

            // turn the in-memory materials into real asset files (one per colour)
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || AssetDatabase.Contains(m)) continue;
                string key = ColorUtility.ToHtmlStringRGBA(m.color);
                if (!cache.TryGetValue(key, out var asset))
                {
                    asset = new Material(m);
                    string p = AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/Lab06_" + key + ".mat");
                    AssetDatabase.CreateAsset(asset, p);
                    cache[key] = asset;
                }
                r.sharedMaterial = asset;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = info.start + new Vector3(0f, 10f, -8f);
                cam.transform.LookAt(info.start + Vector3.up);
            }

            string path = "Assets/Scenes/Lab06_Level0" + (i + 1) + ".unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            buildList.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = buildList.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(buildList[0].path);
        EditorUtility.DisplayDialog("Done", "Saved 5 level scenes and put them in the Build list (in order).", "OK");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path));
    }
}
