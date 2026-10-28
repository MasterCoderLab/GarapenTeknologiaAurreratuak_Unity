using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// V12: Hasiera eszena sortu, menua gehitu eta Build Settings eguneratu.
/// Laberintoa_Horror eta irakaslearen SampleScene ez dira aldatzen.
/// </summary>
public static class HorrorBuilderV12
{
    private const string MenuScene = "Assets/Laberintoa/Scenes/Hasiera.unity";
    private const string GameScene = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Folder = "Assets/Laberintoa/Horror/BuilderV12";
    private const string MaterialFolder = Folder + "/Materials";
    private const string MeshFolder = Folder + "/Meshes";
    private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("HBB12: batch mode beharrezkoa da.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) != null ||
            AssetDatabase.IsValidFolder(Folder))
            throw new InvalidOperationException("HBB12: aurreko menuaren fitxategiak badaude.");
        EditorBuildSettingsScene[] before = EditorBuildSettings.scenes;
        if (before.Length != 1 || before[0].path != GameScene || !before[0].enabled)
            throw new InvalidOperationException("HBB12: Build Settings ez da espero zen oinarrizko egoera.");

        Scene game = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        GameObject cam = Find(game, "Main Camera");
        if (cam == null || cam.GetComponent<HorrorHUDV10>() == null ||
            cam.GetComponent<HorrorAtmosphereV9>() == null ||
            cam.GetComponent<HorrorViewmodelSafety>() == null)
            throw new InvalidOperationException("HBB12: v11 kamera/HUD egoera ez dago osorik.");
        int enemies = 0;
        foreach (GameObject enemyRoot in game.GetRootGameObjects())
            enemies += enemyRoot.GetComponentsInChildren<HorrorEnemyHealth>(true).Length;
        if (enemies != 3) throw new InvalidOperationException("HBB12: 3 etsai behar dira: " + enemies);
        if (Find(game, "Horror_NavMesh") == null || Find(game, "Personaje") == null)
            throw new InvalidOperationException("HBB12: jokoaren oinarrizko egitura falta da.");
        Debug.Log("HBB12_PRECHECK_OK: v11 HUD, 3 etsai, kamera eta NavMesh egiaztatuta.");

        CreateFolder("Assets/Laberintoa/Horror", "BuilderV12");
        CreateFolder(Folder, "Materials");
        CreateFolder(Folder, "Meshes");
        Material lurra = NewMaterial("Lurra", new Color(.038f, .055f, .069f), .12f);
        Material enborra = NewMaterial("Enborra", new Color(.047f, .036f, .035f), .10f);
        Material hostoa = NewMaterial("Hostoa", new Color(.032f, .073f, .077f), .08f);
        Material hostoaIluna = NewMaterial("HostoaIluna", new Color(.022f, .052f, .058f), .08f);
        Material etxola = NewMaterial("Etxola", new Color(.103f, .082f, .076f), .12f);
        Material teilatua = NewMaterial("Teilatua", new Color(.045f, .051f, .062f), .12f);
        Material leihoa = NewMaterial("Leihoa", new Color(.90f, .50f, .17f), .2f);
        Material bidea = NewMaterial("Bidea", new Color(.078f, .077f, .073f), .05f);
        Mesh pinua = MakeConeMesh();
        AssetDatabase.CreateAsset(pinua, MeshFolder + "/HB12_PinuKonoa.asset");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .018f;
        RenderSettings.fogColor = new Color(.035f, .055f, .078f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.13f, .19f, .26f);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.018f, .027f, .043f);
        camera.fieldOfView = 60f;
        camera.farClipPlane = 150f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 2.45f, -13.5f);
        cameraObject.transform.LookAt(new Vector3(3.4f, 1.7f, 23f));
        GameObject menuController = new GameObject("HB12_Menu_Nagusia");
        menuController.AddComponent<HorrorMainMenuV12>();
        GameObject root = new GameObject("HB12_Gaueko_Basoa");
        Cube("LurIluna", root.transform, new Vector3(0f, -.40f, 22f), new Vector3(150f, .60f, 160f), lurra);
        Cube("Bidea", root.transform, new Vector3(2.0f, -.067f, 19f), new Vector3(3.15f, .02f, 100f), bidea);

        System.Random random = new System.Random(1212);
        for (int i = 0; i < 34; i++)
        {
            float z = -5f + (float)random.NextDouble() * 75f;
            float x = (i % 2 == 0 ? -1f : 1f) * (5.0f + (float)random.NextDouble() * 15f);
            float size = .9f + (float)random.NextDouble() * 1.6f;
            Tree(root.transform, new Vector3(x, -.10f, z), size, enborra,
                i % 3 == 0 ? hostoaIluna : hostoa, pinua);
        }
        Cabin(root.transform, new Vector3(5.5f, -.08f, 26f), etxola, teilatua, leihoa);

        GameObject moon = new GameObject("HB12_Ilargia");
        Light sun = moon.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(.42f, .62f, .93f);
        sun.intensity = .28f;
        sun.shadows = LightShadows.Soft;
        moon.transform.rotation = Quaternion.Euler(43f, -38f, 0f);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, MenuScene))
            throw new InvalidOperationException("HBB12: Hasiera eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorBuildSettings.scenes = new [] {
            new EditorBuildSettingsScene(MenuScene, true),
            new EditorBuildSettingsScene(GameScene, true),
        };
        EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        EditorBuildSettingsScene[] after = EditorBuildSettings.scenes;
        if (after.Length != 2 || after[0].path != MenuScene || after[1].path != GameScene ||
            !after[0].enabled || !after[1].enabled ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) == null)
            throw new InvalidOperationException("HBB12: Hasiera ez dago Build Settings-eko lehen postuan.");
        Debug.Log("HBB12_OK: Hasiera eszena sortuta; JOLASTU/AUKERAK/IRTEN, bolumena; Build Settings menu->jokoa. Jokoaren eszena ukitu gabe.");
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        }
        return null;
    }

    private static void CreateFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
            AssetDatabase.CreateFolder(parent, folder);
    }

    private static Material NewMaterial(string name, Color color, float smoothness)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) throw new InvalidOperationException("HBB12: URP/Lit Shader falta da.");
        var material = new Material(s) { name = "HB12_" + name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, MaterialFolder + "/" + material.name + ".mat");
        materials[name] = material;
        return material;
    }

    private static GameObject Cube(string name, Transform parent, Vector3 localPosition,
        Vector3 scale, Material material, Vector3 euler = default(Vector3))
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = euler;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static void Tree(Transform parent, Vector3 position, float size,
        Material bark, Material leaf, Mesh cone)
    {
        var root = new GameObject("HB12_Pinua");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;
        Cube("Enborra", root.transform, new Vector3(0f, size * .92f, 0f),
            new Vector3(size * .24f, size * 1.84f, size * .24f), bark);
        for (int j = 0; j < 3; j++)
        {
            GameObject foliage = new GameObject("Hostoa_" + (j + 1));
            foliage.transform.SetParent(root.transform, false);
            foliage.transform.localPosition = new Vector3(0f, size * (.90f + j * .54f), 0f);
            foliage.transform.localScale = new Vector3(size * (1.18f - j * .22f), size * 1.0f, size * (1.18f - j * .22f));
            foliage.AddComponent<MeshFilter>().sharedMesh = cone;
            foliage.AddComponent<MeshRenderer>().sharedMaterial = leaf;
        }
    }

    private static Mesh MakeConeMesh()
    {
        const int segments = 12;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            verts.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
        }
        verts.Add(new Vector3(0f, 2.1f, 0f));
        int top = segments;
        for (int i = 0; i < segments; i++)
        {
            tris.Add(i);
            tris.Add(top);
            tris.Add((i + 1) % segments);
        }
        Mesh m = new Mesh { name = "HB12_PinuKonoa" };
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static void Cabin(Transform parent, Vector3 position,
        Material wall, Material roof, Material window)
    {
        GameObject cabin = new GameObject("HB12_Txabola_Itzala");
        cabin.transform.SetParent(parent, false);
        cabin.transform.localPosition = position;
        Cube("Etxola", cabin.transform, new Vector3(0f, 2.0f, 0f), new Vector3(6.2f, 4f, 5.2f), wall);
        Cube("Atea", cabin.transform, new Vector3(-.9f, 1.2f, -2.62f), new Vector3(1.6f, 2.4f, .08f), roof);
        Cube("LeihoArgia", cabin.transform, new Vector3(1.35f, 2.2f, -2.65f), new Vector3(1.16f, 1.14f, .07f), window);
        Cube("Teilatua_Ezker", cabin.transform, new Vector3(-1.52f, 4.65f, 0f),
            new Vector3(3.8f, .25f, 6f), roof, new Vector3(0f, 0f, 21f));
        Cube("Teilatua_Eskuin", cabin.transform, new Vector3(1.52f, 4.65f, 0f),
            new Vector3(3.8f, .25f, 6f), roof, new Vector3(0f, 0f, -21f));
        GameObject lightObj = new GameObject("HB12_LeihoArgia");
        lightObj.transform.SetParent(cabin.transform, false);
        lightObj.transform.localPosition = new Vector3(1.35f, 2.15f, -3.1f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 9f;
        light.intensity = 2.3f;
        light.color = new Color(1f, .55f, .23f);
        light.shadows = LightShadows.None;
    }
}
