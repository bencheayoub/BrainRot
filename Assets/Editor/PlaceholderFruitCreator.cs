using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlaceholderFruitCreator
{
    [MenuItem("Tools/FALL Jam/Create Placeholder Fruits")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");

        Make("Fruit_Good",     FruitType.Good,     10,  0, PrimitiveType.Sphere, new Color(0.30f, 0.80f, 0.30f), 0.6f);
        Make("Fruit_Bad",      FruitType.Bad,      -5,  0, PrimitiveType.Sphere, new Color(0.45f, 0.30f, 0.15f), 0.6f);
        Make("Fruit_Golden",   FruitType.Golden,   50,  0, PrimitiveType.Sphere, new Color(1.00f, 0.84f, 0.10f), 0.7f);
        Make("Obstacle_Spike", FruitType.Obstacle,  0,  1, PrimitiveType.Cube,   new Color(0.85f, 0.15f, 0.15f), 0.7f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Placeholder fruits created in Assets/Prefabs");
    }

    private static void Make(string name, FruitType type, int points, int heartDamage,
                             PrimitiveType shape, Color color, float scale)
    {
        string matPath = "Assets/Materials/" + name + ".mat";
        string prefabPath = "Assets/Prefabs/" + name + ".prefab";

        // Re-running the tool replaces old versions
        AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.DeleteAsset(prefabPath);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, matPath);

        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.GetComponent<Collider>().isTrigger = true;

        Fruit fruit = go.AddComponent<Fruit>();
        fruit.type = type;
        fruit.points = points;
        fruit.heartDamage = heartDamage;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
    }

    private static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(path));
        }
    }
}