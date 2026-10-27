using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v5: PBR testurak soilik (hormak, bunkerreko zorua, basoko lurra).
/// Ez ditu Collider, NavMesh, etsaien portaerak, armak edo HUDa aldatzen.
/// </summary>
public static class HorrorBuilderV5
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Root = "Assets/Laberintoa/Horror/BuilderV5";
    private const string Tex = Root + "/Textures";
    private const string Mats = Root + "/Materials";
    private const string NavPath = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";
    private const string WallV4 = "Assets/Laberintoa/Horror/BuilderV4/HB4_HormigoiZaharra.mat";
    private static int walls, floors, exteriors;

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch modua beharrezkoa da.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Validate(scene, false);
        Debug.Log("HBB5_PRECHECK_OK: v4 + jokagarritasuna + 3 PBR multzoak; NavMesh bere horretan.");

        ConfigureImports();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (!AssetDatabase.IsValidFolder(Mats))
        {
            // Unityk karpeta hutsa ezagutu ez badu, testua inportatu eta berriz saiatu.
            // Ez erabili CreateFolder: izen bereko karpetari zenbaki bat gehi diezaioke.
            string marker = Mats + "/LEEME_MATERIALAK.txt";
            Directory.CreateDirectory(Absolute(Mats));
            if (!File.Exists(Absolute(marker)))
                File.WriteAllText(Absolute(marker), "Horror Builder v5 - PBR materialak (CC0).\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        if (!AssetDatabase.IsValidFolder(Mats))
            throw new InvalidOperationException("Materials karpeta ezin izan da Unity AssetDatabase-n erregistratu: " + Mats);
        Debug.Log("HBB5_MATERIALS_FOLDER_OK: PBR materialen karpeta Unityk ezagutzen du.");

        Material wallShort = Build("HB5_HormaLaburra", "concrete", new Vector2(1.3f, 1.1f), new Color(.82f, .85f, .89f), .62f);
        Material wallMid = Build("HB5_HormaErtaina", "concrete", new Vector2(2.7f, 1.1f), new Color(.82f, .85f, .89f), .62f);
        Material wallLong = Build("HB5_HormaLuzea", "concrete", new Vector2(5.2f, 1.1f), new Color(.82f, .85f, .89f), .62f);
        Material floorBunker = Build("HB5_BunkerZorua", "concrete_floor_worn_02", new Vector2(6f, 5f), new Color(.81f, .84f, .87f), .72f);
        Material forest = Build("HB5_BasoLurra", "mud_forest", new Vector2(25f, 15f), new Color(.78f, .81f, .83f), .62f);
        Material forestWide = Build("HB5_BasoLurZabala", "mud_forest", new Vector2(60f, 50f), new Color(.78f, .81f, .83f), .62f);
        Material trail = Build("HB5_Bidezidorra", "mud_forest", new Vector2(2f, 6f), new Color(.88f, .83f, .78f), .46f);

        var seen = new HashSet<Renderer>();
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        foreach (BoxCollider b in sceneRoot.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!b.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer rend = b.GetComponent<Renderer>();
            if (rend == null || !seen.Add(rend)) continue;
            string prev = AssetDatabase.GetAssetPath(rend.sharedMaterial);
            if (prev != WallV4)
                throw new InvalidOperationException("Horma ezezaguna; v4 materiala espero zen: " + b.name + " / " + prev);
            float length = Mathf.Max(b.bounds.size.x, b.bounds.size.z);
            rend.sharedMaterial = length >= 11f ? wallLong : length >= 5f ? wallMid : wallShort;
            walls++;
        }
        if (walls < 40) throw new InvalidOperationException("Horma kopuru desegokia: " + walls);

        foreach (string name in new[] {"HB_Nav_IrteeraEzker", "HB_Nav_IrteeraEskuin", "HB4_AtzekoIrteeraItxita_1", "HB4_AtzekoIrteeraItxita_2"})
            Set(name, wallMid, true);

        // Bunkerreko lur guztiak; ez aldatu kabinako zurezko zorua.
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!(t.name == "Suelo" || t.name.StartsWith("Suelo_", StringComparison.Ordinal))) continue;
            Renderer r = t.GetComponent<Renderer>();
            if (r == null || !seen.Add(r)) continue;
            r.sharedMaterial = floorBunker;
            floors++;
        }
        if (floors < 4) throw new InvalidOperationException("Bunker zoru gutxiegi: " + floors);

        Set("HB_Nav_BosqueSuelo", forest, true); exteriors++;
        Set("HB4_LurZabaldua", forestWide, true); exteriors++;
        Set("HB_Bidezidorra", trail, true); exteriors++;

        // Teilatu lauak: material zaharra ordezkatu, baina geometria ez da aldatzen.
        // Teilatuan luzera handiko materiala erabiltzeak errepikapena orekatzen du.
        for (int i = 1; i <= 4; i++)
            Set("HB_Teilatu_" + i, wallLong, true);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Validate(scene, true);
        Debug.Log($"HBB5_OK: {walls} horma, {floors} bunker zoru, {exteriors} kanpoko gainazal + 4 teilatu; PBR 2K; jokagarritasuna ez da aldatu.");
    }

    private static void Validate(Scene scene, bool installed)
    {
        foreach (string item in new[] {"HB_MunduOsoa_v1", "HB_MunduOsoa_v2", "HB3_Gameplay", "HB4_Hobekuntzak", "Personaje", "Main Camera", "Horror_NavMesh", "HB_Irteera"})
            if (Find(scene, item) == null) throw new InvalidOperationException("Falta: " + item);
        GameObject player = Find(scene, "Personaje");
        GameObject cam = Find(scene, "Main Camera");
        if (player.GetComponent<HorrorPlayerJump>() == null || player.GetComponent<HorrorPlayerHealth>() == null ||
            cam.GetComponent<HorrorShotgun>() == null || cam.GetComponent<HorrorFlashlight>() == null)
            throw new InvalidOperationException("V4ko jokagarritasuna osatu gabe.");
        GameObject nav = Find(scene, "Horror_NavMesh");
        var loader = nav.GetComponent<HorrorNavMeshRuntime>();
        if (loader == null || loader.delayedAgents == null || loader.delayedAgents.Length != 3 ||
            AssetDatabase.GetAssetPath(loader.navMeshData) != NavPath)
            throw new InvalidOperationException("NavMesh erreferentzia aldatu da.");
        foreach (string name in new[] {"Enemigo", "Enemigo_Creep2", "Enemigo_Creep3"})
        {
            GameObject e = Find(scene, name);
            if (e == null || e.GetComponent<HorrorEnemyAI>() == null || e.GetComponent<HorrorEnemyHealth>() == null ||
                e.GetComponent<HorrorEnemyAnimation>() == null || e.GetComponent<NavMeshAgent>() == null)
                throw new InvalidOperationException("Etsaiaren osagaiak falta dira: " + name);
        }
        if (Find(scene, "HB4_AtzekoIrteeraItxita_1")?.GetComponent<BoxCollider>() == null ||
            Find(scene, "HB4_AtzekoIrteeraItxita_2")?.GetComponent<BoxCollider>() == null ||
            Find(scene, "HB_Nav_IrteeraEzker") == null || Find(scene, "HB_Nav_IrteeraEskuin") == null)
            throw new InvalidOperationException("Irteeren egitura ez dator bat.");
        Material floor = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/HB5_BunkerZorua.mat");
        if (installed != (floor != null)) throw new InvalidOperationException("V5 materialak lehendik daude edo ez dira sortu.");
        if (installed)
        {
            if (Find(scene, "HB4_LurZabaldua")?.GetComponent<Renderer>()?.sharedMaterial?.name != "HB5_BasoLurZabala" ||
                Find(scene, "HB4_AtzekoIrteeraItxita_1")?.GetComponent<Renderer>()?.sharedMaterial?.name != "HB5_HormaErtaina")
                throw new InvalidOperationException("V5 material-erreferentziak ez dira gorde.");
        }
    }

    private static void ConfigureImports()
    {
        foreach (string id in new[] {"concrete", "concrete_floor_worn_02", "mud_forest"})
        foreach (string mode in new[] {"diff", "nor_gl", "rough"})
        {
            string assetPath = Tex + "/" + id + "_" + mode + "_2k.jpg";
            if (!File.Exists(Absolute(assetPath))) throw new InvalidOperationException("PBR testura falta da: " + assetPath);
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) throw new InvalidOperationException("TextureImporter falta da: " + assetPath);
            imp.textureType = mode == "nor_gl" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = mode == "diff";
            imp.isReadable = mode == "rough";
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
    }

    private static Material Build(string name, string id, Vector2 tiling, Color tint, float normalStrength)
    {
        string p = Mats + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(p) != null)
            throw new InvalidOperationException("V5 materiala existitzen da: " + p);
        Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "/" + id + "_diff_2k.jpg");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "/" + id + "_nor_gl_2k.jpg");
        Texture2D rough = AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "/" + id + "_rough_2k.jpg");
        if (color == null || normal == null || rough == null)
            throw new InvalidOperationException("PBR testura ezin kargatu: " + id);
        string maskPath = Mats + "/HB5_Mask_" + id + ".png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath) == null)
        {
            var px = rough.GetPixels32();
            var outPx = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
                outPx[i] = new Color32(0, 0, 0, (byte)(255 - px[i].r));
            Texture2D map = new Texture2D(rough.width, rough.height, TextureFormat.RGBA32, true, true);
            map.SetPixels32(outPx);
            map.Apply(true, false);
            File.WriteAllBytes(Absolute(maskPath), map.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(map);
            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceSynchronousImport);
            var maskImp = AssetImporter.GetAtPath(maskPath) as TextureImporter;
            maskImp.sRGBTexture = false;
            maskImp.wrapMode = TextureWrapMode.Repeat;
            maskImp.maxTextureSize = 2048;
            maskImp.SaveAndReimport();
        }
        var gloss = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null || gloss == null) throw new InvalidOperationException("URP/Lit edo maska falta da.");
        Material material = new Material(urp) { name = name };
        material.SetTexture("_BaseMap", color);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetColor("_BaseColor", tint);
        material.SetTexture("_BumpMap", normal);
        material.SetTextureScale("_BumpMap", tiling);
        material.SetFloat("_BumpScale", normalStrength);
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Metallic", 0f);
        material.SetTexture("_MetallicGlossMap", gloss);
        material.SetTextureScale("_MetallicGlossMap", tiling);
        material.SetFloat("_Smoothness", .85f);
        material.SetFloat("_SmoothnessTextureChannel", 0f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        AssetDatabase.CreateAsset(material, p);
        return material;
    }

    private static string Absolute(string assetPath)
    {
        if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            throw new InvalidOperationException("Assets/ errorea: " + assetPath);
        return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
    }

    private static void Set(string name, Material material, bool mandatory)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject go = Find(scene, name);
        Renderer rend = go == null ? null : go.GetComponent<Renderer>();
        if (rend == null)
        {
            if (mandatory) throw new InvalidOperationException("V5 objektua falta da: " + name);
            return;
        }
        rend.sharedMaterial = material;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
        }
        return null;
    }
}
