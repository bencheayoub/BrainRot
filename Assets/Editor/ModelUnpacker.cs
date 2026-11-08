using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor tool: splits a "packed" .fbx (many objects in one file) into one prefab per object.
// Use it from the menu: Tools > FALL > Unpack Selected Models
// Every prefab is scaled so its biggest side is TargetSize and is centered on its pivot.
public static class ModelUnpacker
{
    private const string OutputRoot = "Assets/Prefabs/Unpacked";
    private const float TargetSize = 1f;

    // Friendly names for the objects inside fruits.fbx (the original names are hard to read)
    private static readonly Dictionary<string, string> FruitNames = new Dictionary<string, string>
    {
        { "apple", "Apple_Red" },
        { "apple.001", "Apple_Green" },
        { "apple.002", "Apple_Yellow" },
        { "apple.003", "Apple_RedLeaf" },
        { "apple.004", "Apple_GreenLeaf" },
        { "apple.005", "Apple_YellowLeaf" },
        { "NurbsPath", "Banana" },
        { "Cube", "Banana2" },
        { "Cube.001", "Pear_Yellow" },
        { "pear", "Pear_Green" },
        { "Cube.002", "Cherry_A" },
        { "Cube.003", "Cherry_B" },
        { "Cube.005", "Cherry_C" },
        { "Cube.004", "Pineapple" },
        { "Sphere", "Kiwi" },
        { "Sphere.001", "Kiwi_Half" },
        { "Sphere.002", "Coconut" },
        { "Sphere.003", "Coconut_Half" },
        { "blueberry", "Blueberry" },
        { "blueberry.001", "Blueberries" },
        { "red_currant", "RedCurrant" },
        { "red_currants", "RedCurrants" },
        { "lemon", "Lemon" },
        { "lime", "Lime" },
        { "strawberry", "Strawberry" },
        { "watermelon", "Watermelon" },
        { "watermelon_half", "Watermelon_Half" },
        { "watermelon_slice", "Watermelon_Slice" }
    };

    // Names for the objects inside bombs.fbx
    private static readonly Dictionary<string, string> BombNames = new Dictionary<string, string>
    {
        { "Sphere", "Bomb_A" },
        { "Sphere.001", "Bomb_B" }
    };

    [MenuItem("Tools/FALL/Unpack Selected Models")]
    private static void UnpackSelected()
    {
        HashSet<string> done = new HashSet<string>();
        int total = 0;

        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.ToLower().EndsWith(".fbx")) continue;
            if (!done.Add(path)) continue;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) continue;

            total += UnpackModel(model, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = "Created " + total + " prefabs in " + OutputRoot;
        Debug.Log("ModelUnpacker: " + message);
        EditorUtility.DisplayDialog("Unpack done", message, "OK");

        Object folder = AssetDatabase.LoadAssetAtPath<Object>(OutputRoot);
        if (folder != null) EditorGUIUtility.PingObject(folder);
    }

    [MenuItem("Tools/FALL/Unpack Selected Models", true)]
    private static bool ValidateUnpackSelected()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.ToLower().EndsWith(".fbx")) return true;
        }
        return false;
    }

    private static int UnpackModel(GameObject model, string path)
    {
        string modelName = Path.GetFileNameWithoutExtension(path);
        bool isBomb = modelName.ToLower().Contains("bomb");
        Dictionary<string, string> names = isBomb ? BombNames : FruitNames;

        string folder = OutputRoot + "/" + modelName;
        EnsureFolder(folder);

        // A plain copy of the whole model, so we can take pieces out of it
        GameObject instance = Object.Instantiate(model);
        MeshRenderer[] meshes = instance.GetComponentsInChildren<MeshRenderer>(true);

        int count = 0;
        foreach (MeshRenderer mr in meshes)
        {
            string sourceName = mr.gameObject.name;
            string niceName;
            if (!names.TryGetValue(sourceName, out niceName)) niceName = sourceName;

            // New root for this one object, then move the mesh object into it
            GameObject root = new GameObject(niceName);
            Transform t = mr.transform;
            t.SetParent(root.transform, true);

            // Scale so the biggest side equals TargetSize
            Bounds b = GetBounds(root);
            float biggest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (biggest > 0.0001f) t.localScale *= TargetSize / biggest;

            // Center it on the root's pivot
            b = GetBounds(root);
            t.position -= b.center;

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + niceName + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            Object.DestroyImmediate(root);
            count++;
        }

        Object.DestroyImmediate(instance);
        return count;
    }

    private static Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in renderers)
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}