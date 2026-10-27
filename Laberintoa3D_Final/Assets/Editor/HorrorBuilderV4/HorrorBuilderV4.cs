using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v4: jauzia, linterna ikusgai, hormak, atzeko bi irteerak eta lurra.
/// Jatorrizko SampleScene eta NavMesh fitxategiak ez dira ukitzen.
/// </summary>
public static class HorrorBuilderV4
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Folder = "Assets/Laberintoa/Horror/BuilderV4";
    private const string NavPath = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";
    private const string RootName = "HB4_Hobekuntzak";
    private static int walls, rearExits, plants;

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch modua beharrezkoa da.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Verify(scene, false);
        Debug.Log("HBB4_PRECHECK_OK: v3 + NavMesh Fix + 2 atzeko hutsune + irteera nagusia.");

        Material concrete = NewMaterial("HB4_HormigoiZaharra",
            "Assets/Laberintoa/Horror/BuilderV2/HB2_Concrete.mat", new Color(.64f, .66f, .67f), new Vector2(3f, 3f));
        Material floor = NewMaterial("HB4_BasoLurZabaldua",
            "Assets/Laberintoa/Horror/BuilderV2/HB2_ForestFloor.mat", new Color(.58f, .62f, .57f), new Vector2(30f, 22f));
        Material glass = NewMaterial("HB4_LinternaKristala",
            "Assets/Laberintoa/Horror/BuilderV2/HB2_Concrete.mat", new Color(.8f, .91f, 1f), Vector2.one);
        glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", new Color(.18f, .22f, .28f));
        Material metal = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Laberintoa/Horror/BuilderV1/M_HB_Metala.mat");
        Material bark = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Laberintoa/Horror/BuilderV1/M_HB_ZuhaitzAzala.mat");
        if (metal == null || bark == null) throw new InvalidOperationException("Oinarrizko materialak falta dira.");

        Transform root = new GameObject(RootName).transform;
        BetterWalls(scene, root, concrete, metal);
        CloseBackExits(scene, root, concrete);
        ExtendGround(root, floor, bark);
        BuildFlashlight(scene, root, metal, glass);
        Find(scene, "Personaje").AddComponent<HorrorPlayerJump>();
        if (walls < 40 || rearExits != 2 || plants < 35)
            throw new InvalidOperationException("V4 objektu kopurua okerra da.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Ezin izan da eszena gorde.");
        AssetDatabase.SaveAssets();

        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Verify(scene, true);
        Debug.Log("HBB4_OK: jauzia, linterna ikusgai, horma-materialak, atzeko 2 irteerak itxita, lur zabala; HUD euskaraz.");
    }

    private static void Verify(Scene scene, bool installed)
    {
        GameObject world1 = Find(scene, "HB_MunduOsoa_v1");
        GameObject world2 = Find(scene, "HB_MunduOsoa_v2");
        GameObject gameplay = Find(scene, "HB3_Gameplay");
        GameObject player = Find(scene, "Personaje");
        GameObject camera = Find(scene, "Main Camera");
        GameObject nav = Find(scene, "Horror_NavMesh");
        GameObject exit = Find(scene, "HB_Irteera");
        if (world1 == null || world2 == null || gameplay == null || exit == null ||
            player == null || camera == null || nav == null)
            throw new InvalidOperationException("V1/V2/V3 oinarriko objektuak falta dira.");
        if (player.GetComponent<HorrorPlayerMovement>() == null ||
            player.GetComponent<HorrorPlayerHealth>() == null ||
            camera.GetComponent<HorrorShotgun>() == null ||
            camera.GetComponent<HorrorFlashlight>() == null ||
            gameplay.GetComponent<HorrorGameManager>() == null)
            throw new InvalidOperationException("Jokalaria edo jokoa ez dira egokiak.");
        HorrorNavMeshRuntime runtime = nav.GetComponent<HorrorNavMeshRuntime>();
        if (runtime == null || runtime.delayedAgents == null || runtime.delayedAgents.Length != 3 ||
            AssetDatabase.GetAssetPath(runtime.navMeshData) != NavPath)
            throw new InvalidOperationException("NavMesh osagaia edo bere erreferentzia aldatu da.");
        foreach (string name in new[] {"Enemigo", "Enemigo_Creep2", "Enemigo_Creep3"})
        {
            GameObject enemy = Find(scene, name);
            if (enemy == null || enemy.GetComponent<HorrorEnemyAI>() == null ||
                enemy.GetComponent<HorrorEnemyHealth>() == null ||
                enemy.GetComponent<HorrorEnemyAnimation>() == null)
                throw new InvalidOperationException("Etsaiaren osagaiak falta dira: " + name);
        }
        bool exists = Find(scene, RootName) != null;
        if (exists != installed || (player.GetComponent<HorrorPlayerJump>() != null) != installed)
            throw new InvalidOperationException("V4 lehendik instalatuta dago edo ez da behar bezala gorde.");
        if (installed)
        {
            GameObject root = Find(scene, RootName);
            if (root.transform.Find("HB4_AtzekoIrteeraItxita_1") == null ||
                root.transform.Find("HB4_AtzekoIrteeraItxita_2") == null ||
                root.transform.Find("HB4_LurZabaldua") == null ||
                camera.transform.Find("HB4_LinternaIkusgai") == null)
                throw new InvalidOperationException("V4 elementuren bat falta da gordetako eszenan.");
            for (int i = 1; i <= 2; i++)
                if (root.transform.Find("HB4_AtzekoIrteeraItxita_" + i)
                    .GetComponent<BoxCollider>() == null)
                    throw new InvalidOperationException("Atzeko ateak ez du talkarik.");
        }
        // Ez dago irteera nagusiaren paretarik gehitzerik: 3 metroko atea bere horretan.
        if (exit.transform.Find("HB_Nav_IrteeraEzker") == null ||
            exit.transform.Find("HB_Nav_IrteeraEskuin") == null)
            throw new InvalidOperationException("Irteera nagusia aldatu da.");
        foreach (string name in new[] {"Paredes", "Paredes_1_0"})
        {
            GameObject group = Find(scene, name);
            if (group == null || group.transform.Find("ParedOeste1") == null ||
                group.transform.Find("ParedOeste2") == null)
                throw new InvalidOperationException("Atzeko hormen egitura ez dator bat: " + name);
            Gap(group.transform, false); // Geometria egiaztatu, aldatu gabe.
        }
    }

    private static Material NewMaterial(string name, string source, Color tint, Vector2 tiling)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            throw new InvalidOperationException("BuilderV4 karpeta ez da existitzen.");
        Material original = AssetDatabase.LoadAssetAtPath<Material>(source);
        if (original == null) throw new InvalidOperationException("Materiala falta da: " + source);
        string path = Folder + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            throw new InvalidOperationException("Materiala lehendik dago: " + path);
        Material result = new Material(original) { name = name };
        result.SetColor("_BaseColor", tint);
        result.SetTextureScale("_BaseMap", tiling);
        result.SetFloat("_Smoothness", .08f);
        AssetDatabase.CreateAsset(result, path);
        return result;
    }

    private static void BetterWalls(Scene scene, Transform root, Material concrete, Material metal)
    {
        foreach (GameObject rootGo in scene.GetRootGameObjects())
        foreach (BoxCollider box in rootGo.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer == null) continue;
            renderer.sharedMaterial = concrete;
            walls++;
        }
        foreach (string name in new[] {"HB_Nav_IrteeraEzker", "HB_Nav_IrteeraEskuin"})
        {
            GameObject part = Find(scene, name);
            if (part != null && part.GetComponent<Renderer>() != null)
                part.GetComponent<Renderer>().sharedMaterial = concrete;
        }
        // Goi aldeko ertz industrial xumeak; ez dute Collider osagairik.
        Transform detail = new GameObject("HB4_HormenXehetasunak").transform;
        detail.SetParent(root, false);
        int count = 0;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        foreach (BoxCollider wall in sceneRoot.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!wall.name.StartsWith("Pared", StringComparison.Ordinal) || ++count % 3 != 0) continue;
            Bounds b = wall.bounds;
            bool alongX = b.size.x > b.size.z;
            Vector3 scale = alongX ? new Vector3(Mathf.Min(3f, b.size.x * .65f), .07f, .08f)
                                  : new Vector3(.08f, .07f, Mathf.Min(3f, b.size.z * .65f));
            Visual(PrimitiveType.Cube, "HB4_ErtzOxidatua_" + count, detail,
                new Vector3(b.center.x, b.max.y - .12f, b.center.z), scale, metal);
        }
    }

    private static Bounds Gap(Transform group, bool unused)
    {
        BoxCollider w1 = group.Find("ParedOeste1")?.GetComponent<BoxCollider>();
        BoxCollider w2 = group.Find("ParedOeste2")?.GetComponent<BoxCollider>();
        if (w1 == null || w2 == null)
            throw new InvalidOperationException("Atzeko hormak falta dira: " + group.name);
        Bounds a = w1.bounds, b = w2.bounds;
        Bounds left = a.center.x < b.center.x ? a : b;
        Bounds right = a.center.x < b.center.x ? b : a;
        float gap = right.min.x - left.max.x;
        if (Mathf.Abs(gap - 2f) > .35f ||
            Mathf.Abs(left.center.z - right.center.z) > .08f ||
            Mathf.Abs(left.size.z - right.size.z) > .08f ||
            Mathf.Abs(left.center.z + 7f) > .5f)
            throw new InvalidOperationException("Atzeko hutsunearen neurria/kokapena ustekabea: " + group.name + " " + gap);
        float x = (left.max.x + right.min.x) * .5f;
        return new Bounds(new Vector3(x, left.center.y, left.center.z),
            new Vector3(gap, Mathf.Min(left.size.y, right.size.y), left.size.z));
    }

    private static void CloseBackExits(Scene scene, Transform root, Material concrete)
    {
        for (int i = 0; i < 2; i++)
        {
            string groupName = i == 0 ? "Paredes" : "Paredes_1_0";
            Bounds gap = Gap(Find(scene, groupName).transform, false);
            GameObject close = Box("HB4_AtzekoIrteeraItxita_" + (i + 1), root,
                gap.center, new Vector3(gap.size.x + .06f, gap.size.y, gap.size.z), concrete);
            if (close.GetComponent<BoxCollider>() == null) throw new InvalidOperationException("Itxitura talkarik gabe.");
            rearExits++;
        }
    }

    private static void ExtendGround(Transform root, Material floor, Material bark)
    {
        // Aurreko lurraren gaineko maila +0.47 da; honek 0.435 du, eta ez du estaltzen.
        Box("HB4_LurZabaldua", root, new Vector3(25f, -.065f, 38f),
            new Vector3(180f, 1f, 150f), floor);
        Transform growth = new GameObject("HB4_Landareak").transform;
        growth.SetParent(root, false);
        var random = new System.Random(20261027);
        for (int i = 0; i < 85; i++)
        {
            float x = -44f + (float)random.NextDouble() * 140f;
            float z = -27f + (float)random.NextDouble() * 128f;
            // Labirinto, basoko bidea eta txabola ingurua libre geratzen dira.
            if (x > -11f && x < 31f && z > -9f && z < 24f) continue;
            if (x > 20f && x < 36f && z > 20f && z < 52f) continue;
            if (x > 33f && x < 44f && z > 46f && z < 59f) continue;
            float size = .35f + (float)random.NextDouble() * .35f;
            Visual(PrimitiveType.Cube, "HB4_BelarTxorta_" + (++plants), growth,
                new Vector3(x, .48f, z), new Vector3(.12f, .10f, size), bark);
        }
    }

    private static void BuildFlashlight(Scene scene, Transform root, Material metal, Material glass)
    {
        GameObject camera = Find(scene, "Main Camera");
        Transform body = new GameObject("HB4_LinternaIkusgai").transform;
        body.SetParent(camera.transform, false);
        body.localPosition = new Vector3(-.36f, -.26f, .52f);
        body.localRotation = Quaternion.Euler(12f, -7f, 0f);
        VisualLocal(PrimitiveType.Cylinder, "HB4_LinternaGorputza", body,
            new Vector3(0f, 0f, 0f), new Vector3(.11f, .18f, .11f), metal, Quaternion.Euler(90f, 0f, 0f));
        VisualLocal(PrimitiveType.Cylinder, "HB4_LinternaBurua", body,
            new Vector3(0f, 0f, .23f), new Vector3(.15f, .055f, .15f), metal, Quaternion.Euler(90f, 0f, 0f));
        VisualLocal(PrimitiveType.Sphere, "HB4_LinternaKristala", body,
            new Vector3(0f, 0f, .295f), new Vector3(.19f, .19f, .04f), glass, Quaternion.identity);
        // Ez da Spot Light-a aldatzen: F tekla berdin mantentzen da.
        if (camera.transform.Find("HB_Linterna") == null)
            throw new InvalidOperationException("Jatorrizko linterna falta da.");
    }

    private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, true);
        obj.transform.position = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    private static GameObject Visual(PrimitiveType primitive, string name, Transform parent,
        Vector3 pos, Vector3 size, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(primitive);
        obj.name = name;
        obj.transform.SetParent(parent, true);
        obj.transform.position = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        Collider col = obj.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
        return obj;
    }

    private static GameObject VisualLocal(PrimitiveType primitive, string name, Transform parent,
        Vector3 pos, Vector3 size, Material mat, Quaternion rotation)
    {
        GameObject obj = GameObject.CreatePrimitive(primitive);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localRotation = rotation;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        Collider col = obj.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
        return obj;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name == name) return sceneRoot;
            foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
        }
        return null;
    }
}
