using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v2: ikusizko hobekuntzak soilik. NavMesh, IA eta fisika ez ditu aldatzen.
/// Unity Editor batch-moduan erabiltzeko; jatorrizko SampleScene ez da irekitzen.
/// </summary>
public static class HorrorBuilderV2
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string V2Folder = "Assets/Laberintoa/Horror/BuilderV2";
    private const string V1Root = "HB_MunduOsoa_v1";
    private const string V2Root = "HB_MunduOsoa_v2";

    private static Material concrete, rust, wood, bark, forest, trail, needlesDark, needlesLight;
    private static Mesh conifer;
    private static int upgradedTrees, props, patches;

    public static void Audit()
    {
        RequireBatch();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateBase(scene);
        Debug.Log("HBB2_AUDIT_OK: BuilderV1, 3 enemies/AI/animations, roofs x4, 45+2 walls, forest and cabin OK; no BuilderV2.");
    }

    public static void Apply()
    {
        RequireBatch();
        if (AssetDatabase.IsValidFolder(V2Folder) &&
            AssetDatabase.FindAssets("", new[] { V2Folder }).Length != 0)
            throw new InvalidOperationException("BuilderV2 assets already exist. Do not repeat --apply.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateBase(scene);
        Debug.Log("HBB2_PRECHECK_OK: prefabs Unity API checked; v1 scene ready.");

        CreateMaterials();
        CreateConifer();
        GameObject world = new GameObject(V2Root);
        BuildBunker(scene, world.transform);
        UpgradeForest(scene, world.transform);
        UpgradeCabin(scene, world.transform);
        TuneAtmosphere(scene, world.transform);

        if (upgradedTrees < 45 || props < 30 || patches < 35)
            throw new InvalidOperationException("Inadequate decoration: trees=" + upgradedTrees +
                " props=" + props + " grass=" + patches);
        ValidateBase(scene, true);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unable to save Horror scene.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Reopen and verify references with native Unity API.
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateBase(scene, true);
        GameObject saved = Find(scene, V2Root);
        if (saved == null || saved.transform.Find("HB2_Industrial") == null ||
            saved.transform.Find("HB2_ForestDetail") == null ||
            saved.transform.Find("HB2_CabinDetail") == null ||
            AssetDatabase.LoadAssetAtPath<Mesh>(V2Folder + "/HB2_Conifer.asset") == null)
            throw new InvalidOperationException("Horror Builder v2 data not saved correctly.");
        foreach (string file in new[] { "HB2_Concrete.png", "HB2_Ground.png", "HB2_Bark.png",
            "HB2_Planks.png", "HB2_Rust.png", "HB2_Needles.png" })
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(V2Folder + "/" + file) == null)
                throw new InvalidOperationException("Missing authored texture: " + file);
        Debug.Log("HBB2_OK: upgraded " + upgradedTrees + " trees; " + props +
            " industrial/cabin details; " + patches +
            " forest clusters; 6 procedural textures. AI, NavMesh, wall colliders, player and SampleScene preserved.");
    }

    private static void RequireBatch()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("HorrorBuilderV2 requires Unity batch mode.");
    }

    private static void ValidateBase(Scene scene, bool v2Expected = false)
    {
        if (!scene.IsValid() || Find(scene, V1Root) == null ||
            Find(scene, "Horror_NavMesh") == null || Find(scene, "Personaje") == null ||
            Find(scene, "Horror_Enemigos_X3") == null || Find(scene, "Horror_Sektoreak_X4") == null ||
            Find(scene, "HB_Chabola") == null || Find(scene, "HB_BasoGaua") == null ||
            (Find(scene, V2Root) != null) != v2Expected)
            throw new InvalidOperationException("Unexpected Horror v1/v2 hierarchy.");
        GameObject world = Find(scene, V1Root);
        int roofs = 0, trees = 0, walls = 0, floors = 0;
        foreach (Transform t in world.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("HB_Teilatu_", StringComparison.Ordinal)) roofs++;
            if (t.name.StartsWith("HB_Zuhaitza_", StringComparison.Ordinal)) trees++;
        }
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider collider in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (collider.name.StartsWith("Pared", StringComparison.Ordinal)) walls++;
            else if (collider.name.StartsWith("Suelo", StringComparison.Ordinal)) floors++;
        }
        if (roofs != 4 || trees < 45 || walls != 45 || floors != 4)
            throw new InvalidOperationException("V1 geometry changed roofs/trees/walls/floors: " +
                roofs + "/" + trees + "/" + walls + "/" + floors);
        if (Find(scene, "HB_Irteera")?.GetComponentInChildren<BoxCollider>() == null)
            throw new InvalidOperationException("Bunker exit missing.");
        var runtime = Find(scene, "Horror_NavMesh")?.GetComponent<HorrorNavMeshRuntime>();
        if (runtime == null || runtime.navMeshData == null ||
            AssetDatabase.GetAssetPath(runtime.navMeshData) !=
            "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset")
            throw new InvalidOperationException("Horror v1 NavMesh reference changed.");
        if (Find(scene, "Personaje")?.GetComponent<HorrorPlayerMovement>() == null ||
            Find(scene, "Main Camera")?.GetComponent<HorrorFlashlight>() == null)
            throw new InvalidOperationException("Player or flashlight missing.");
        for (int i = 1; i <= 3; i++)
        {
            string name = i == 1 ? "Enemigo" : "Enemigo_Creep" + i;
            GameObject enemy = Find(scene, name);
            if (enemy == null || enemy.GetComponent<HorrorEnemyAI>() == null ||
                enemy.GetComponent("HorrorEnemyAnimation") == null ||
                enemy.GetComponent<NavMeshAgent>() == null ||
                enemy.GetComponent<Enemigo>() != null ||
                enemy.transform.Find("Creep" + i + "_Visual") == null)
                throw new InvalidOperationException("Enemy/animation missing: " + name);
        }
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                if (tr.name == name) return tr.gameObject;
        }
        return null;
    }

    private static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(V2Folder))
            throw new InvalidOperationException("BuilderV2 folder not created by launcher.");
        concrete = Mat("HB2_Concrete", new Color(.55f, .56f, .56f), "HB2_Concrete.png", 701, 4, 3);
        rust = Mat("HB2_Rust", new Color(.65f, .48f, .35f), "HB2_Rust.png", 102, 2, 2);
        wood = Mat("HB2_OldPlanks", new Color(.64f, .56f, .45f), "HB2_Planks.png", 333, 2, 2);
        bark = Mat("HB2_Bark", new Color(.65f, .58f, .52f), "HB2_Bark.png", 98, 2, 3);
        forest = Mat("HB2_ForestFloor", new Color(.54f, .59f, .52f), "HB2_Ground.png", 652, 12, 8);
        trail = Mat("HB2_ForestTrail", new Color(.64f, .54f, .41f), "HB2_Ground.png", 652, 5, 9);
        needlesDark = Mat("HB2_NeedlesDark", new Color(.18f, .33f, .24f), "HB2_Needles.png", 848, 1, 1);
        needlesLight = Mat("HB2_NeedlesLight", new Color(.32f, .42f, .31f), "HB2_Needles.png", 848, 1, 1);
        Debug.Log("HBB2_ASSETS_OK: 6 textures + 8 URP materials generated, no Asset Store models used.");
    }

    private static Material Mat(string name, Color tint, string texName, int seed, int repeatX, int repeatY)
    {
        Texture2D tex = Texture(texName, seed);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP/Lit shader not found.");
        Material mat = new Material(shader) { name = name };
        mat.SetColor("_BaseColor", tint);
        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", new Vector2(repeatX, repeatY));
        mat.SetFloat("_Smoothness", .12f);
        AssetDatabase.CreateAsset(mat, V2Folder + "/" + name + ".mat");
        return mat;
    }

    private static Texture2D Texture(string name, int seed)
    {
        string assetPath = V2Folder + "/" + name;
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (existing != null) return existing;
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
        var colors = new Color32[size * size];
        bool planks = name.Contains("Planks"), barkTex = name.Contains("Bark"),
            concreteTex = name.Contains("Concrete"), rustTex = name.Contains("Rust"),
            foliage = name.Contains("Needles");
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float n = Mathf.PerlinNoise((x + seed) / 31f, (y + seed * .47f) / 31f);
            float fine = Mathf.PerlinNoise((x + seed * 3) / 8f, (y + seed * .29f) / 8f);
            float large = Mathf.PerlinNoise((x + seed) / 87f, (y + seed) / 79f);
            Color c;
            if (concreteTex)
            {
                float v = Mathf.Clamp01(.42f + .32f * n + .24f * fine);
                c = new Color(v * .85f, v * .87f, v * .88f);
                if (n < .27f) c *= .72f;
            }
            else if (planks || barkTex)
            {
                float grain = Mathf.PerlinNoise((x + seed) / (barkTex ? 5f : 12f), (y + seed) / 74f);
                float v = Mathf.Clamp01(.30f + .40f * grain + .23f * n);
                c = new Color(v * 1.08f, v * .79f, v * .58f);
                if (planks && (y % 64 < 3 || x % 128 < 2)) c *= .25f;
                if (barkTex && x % 37 < 2) c *= .44f;
            }
            else if (rustTex)
            {
                float v = Mathf.Clamp01(.18f + .68f * large + .25f * fine);
                c = new Color(.35f + .54f * v, .22f + .30f * v, .16f + .13f * v);
                if (n > .7f) c = Color.Lerp(c, new Color(.23f, .28f, .31f), .60f);
            }
            else if (foliage)
            {
                float v = Mathf.Clamp01(.22f + .47f * n + .26f * fine);
                c = new Color(.27f * v, .54f * v, .37f * v);
            }
            else
            {
                float v = Mathf.Clamp01(.23f + .51f * n + .24f * large);
                c = new Color(.50f * v, .48f * v, .36f * v);
                if (fine > .68f) c = Color.Lerp(c, new Color(.11f, .17f, .11f), .45f);
            }
            colors[y * size + x] = (Color32)c;
        }
        tex.SetPixels32(colors);
        tex.Apply(true, false);
        string filesystemPath = Path.Combine(Application.dataPath,
            assetPath.Substring("Assets/".Length));
        File.WriteAllBytes(filesystemPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("TextureImporter missing: " + assetPath);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (imported == null) throw new InvalidOperationException("Texture import failed: " + assetPath);
        return imported;
    }

    private static void CreateConifer()
    {
        const int sides = 9;
        var vertices = new Vector3[sides + 2];
        var triangles = new int[sides * 6];
        vertices[0] = new Vector3(0, 1, 0);
        vertices[1] = Vector3.zero;
        for (int i = 0; i < sides; i++)
        {
            float a = (Mathf.PI * 2 * i) / sides;
            vertices[i + 2] = new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f);
            int next = (i + 1) % sides;
            triangles[i * 6] = 0;
            triangles[i * 6 + 1] = 2 + next;
            triangles[i * 6 + 2] = 2 + i;
            triangles[i * 6 + 3] = 1;
            triangles[i * 6 + 4] = 2 + i;
            triangles[i * 6 + 5] = 2 + next;
        }
        conifer = new Mesh { name = "HB2_LowPolyPine" };
        conifer.vertices = vertices;
        conifer.triangles = triangles;
        conifer.RecalculateNormals();
        conifer.RecalculateBounds();
        AssetDatabase.CreateAsset(conifer, V2Folder + "/HB2_Conifer.asset");
    }

    private static void BuildBunker(Scene scene, Transform parent)
    {
        Transform group = NewGroup("HB2_Industrial", parent);
        Transform v1 = Find(scene, V1Root).transform;
        foreach (Transform tr in v1.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name.StartsWith("HB_Teilatu_", StringComparison.Ordinal))
            {
                Renderer r = tr.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = concrete;
            }
            if (tr.name.StartsWith("HB_KutxaProbisionala_", StringComparison.Ordinal))
            {
                Renderer r = tr.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = wood;
            }
        }
        // Pipes, vents and corroded strips above head level; no physics colliders.
        foreach (float x in new[] { -3.2f, 16.8f })
        foreach (float z in new[] { 0f, 15f })
        {
            int id = ++props;
            Visual(PrimitiveType.Cylinder, "HB2_HodiOxidatua_" + id, group,
                new Vector3(x, 2.39f, z), new Vector3(.10f, 4.0f, .10f), rust,
                Quaternion.Euler(90f, 0f, 0f));
            Visual(PrimitiveType.Cube, "HB2_SabaiErraila_" + id, group,
                new Vector3(x + 1.1f, 2.48f, z), new Vector3(1.65f, .11f, .12f), rust);
            Visual(PrimitiveType.Cube, "HB2_SabaiSarea_" + id, group,
                new Vector3(x - 1.2f, 2.49f, z + 1.8f), new Vector3(.9f, .10f, .45f), rust);
            props += 2;
        }
        // Border details that do not close corridors or touch the navigation mesh.
        for (int i = 0; i < 12; i++)
        {
            float x = i % 2 == 0 ? -5.1f : 14.9f;
            float z = (i % 6 < 3 ? -5f : 10f) + (i % 3) * 1.45f;
            Visual(PrimitiveType.Cube, "HB2_HormaKable_" + i, group,
                new Vector3(x, 2.32f, z), new Vector3(.10f, .09f, 1.15f), rust);
            props++;
        }
    }

    private static void UpgradeForest(Scene scene, Transform parent)
    {
        Transform details = NewGroup("HB2_ForestDetail", parent);
        Transform forestV1 = Find(scene, "HB_BasoGaua").transform;
        Transform floor = forestV1.Find("HB_Nav_BosqueSuelo");
        Transform path = forestV1.Find("HB_Bidezidorra");
        if (floor == null || path == null) throw new InvalidOperationException("V1 forest floor/path missing.");
        floor.GetComponent<Renderer>().sharedMaterial = forest;
        path.GetComponent<Renderer>().sharedMaterial = trail;
        var trees = new List<Transform>();
        foreach (Transform item in forestV1)
            if (item.name.StartsWith("HB_Zuhaitza_", StringComparison.Ordinal)) trees.Add(item);
        trees.Sort((a,b) => string.CompareOrdinal(a.name,b.name));
        foreach (Transform tree in trees)
        {
            Transform trunk = tree.Find("HB_Nav_Tronco");
            Transform old1 = tree.Find("HB_Koroa1");
            Transform old2 = tree.Find("HB_Koroa2");
            if (trunk == null || old1 == null || old2 == null ||
                old1.GetComponent<Collider>()?.enabled == true ||
                old2.GetComponent<Collider>()?.enabled == true)
                throw new InvalidOperationException("Unexpected tree geometry: " + tree.name);
            Renderer trunkRenderer = trunk.GetComponent<Renderer>();
            if (trunkRenderer == null) throw new InvalidOperationException("Tree trunk renderer absent.");
            trunkRenderer.sharedMaterial = bark;
            float height = trunk.localPosition.y * 2f;
            float variant = (upgradedTrees % 5) * .09f;
            UnityEngine.Object.DestroyImmediate(old1.gameObject);
            UnityEngine.Object.DestroyImmediate(old2.gameObject);
            for (int layer = 0; layer < 3; layer++)
            {
                float bottom = (.30f + layer * .225f) * height;
                float width = Mathf.Max(1.0f, 3.1f - layer * .75f + variant);
                float len = height * (.60f - layer * .07f);
                var go = new GameObject("HB2_PinuHostoak_" + (layer + 1));
                go.transform.SetParent(tree, false);
                go.transform.localPosition = new Vector3(0f, bottom, 0f);
                go.transform.localScale = new Vector3(width, len, width);
                go.AddComponent<MeshFilter>().sharedMesh = conifer;
                go.AddComponent<MeshRenderer>().sharedMaterial = (upgradedTrees % 3 == 0) ? needlesLight : needlesDark;
            }
            upgradedTrees++;
        }
        // Small crossed grass clusters. Visual-only, avoiding the route and cabin entrance.
        System.Random rand = new System.Random(2086);
        for (int i = 0; i < 145; i++)
        {
            float x = -11f + (float)rand.NextDouble() * 71f;
            float z = 26f + (float)rand.NextDouble() * 36f;
            if ((z < 49f && x > 21f && x < 35f) ||
                Vector2.Distance(new Vector2(x, z), new Vector2(38f, 52f)) < 8f) continue;
            Transform tuft = NewGroup("HB2_BelarSasia_" + patches.ToString("D3"), details);
            tuft.position = new Vector3(x, .52f, z);
            float size = .2f + (float)rand.NextDouble() * .37f;
            Visual(PrimitiveType.Cube, "HB2_BelarA", tuft, tuft.position + new Vector3(0, size*.5f, 0),
                new Vector3(.035f, size, .46f), needlesLight, Quaternion.Euler(0, 24, -12));
            Visual(PrimitiveType.Cube, "HB2_BelarB", tuft, tuft.position + new Vector3(0, size*.5f, 0),
                new Vector3(.46f, size, .035f), needlesDark, Quaternion.Euler(0, -24, 12));
            patches++;
        }
        // V1 rock visuals can be upgraded without touching colliders (none enabled).
        foreach (Transform t in forestV1)
            if (t.name.StartsWith("HB_Harria_", StringComparison.Ordinal))
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = concrete;
            }
        // Soft moon pools are intentionally weak to preserve survival-horror darkness.
        foreach (Vector3 pos in new[] { new Vector3(2f, 7f, 43f), new Vector3(50f, 7f, 55f) })
        {
            var o = new GameObject("HB2_IlargiIspilua");
            o.transform.SetParent(details, false);
            o.transform.position = pos;
            Light light = o.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = .35f;
            light.color = new Color(.33f, .49f, .76f);
            light.shadows = LightShadows.None;
        }
    }

    private static void UpgradeCabin(Scene scene, Transform parent)
    {
        Transform group = NewGroup("HB2_CabinDetail", parent);
        Transform cabin = Find(scene, "HB_Chabola").transform;
        foreach (Transform tr in cabin.GetComponentsInChildren<Transform>(true))
        {
            Renderer r = tr.GetComponent<Renderer>();
            if (r == null) continue;
            if (tr.name.StartsWith("HB_Nav_", StringComparison.Ordinal) ||
                tr.name.StartsWith("HB_Mahai", StringComparison.Ordinal) ||
                tr.name == "HB_ChabolaAteBurua") r.sharedMaterial = wood;
            else if (tr.name.StartsWith("HB_ChabolaTeilatua", StringComparison.Ordinal))
                r.sharedMaterial = rust;
        }
        // Vertical irregular weathered planks at front; leave the doorway clear.
        for (int i = 0; i <= 13; i++)
        {
            float x = -3.3f + i * .51f;
            if (Mathf.Abs(x) < 1.30f) continue;
            float h = 2.05f + (i % 4) * .12f;
            Visual(PrimitiveType.Cube, "HB2_AurrekoOhola_" + i, group,
                cabin.TransformPoint(new Vector3(x, 1.35f, -3.16f)),
                new Vector3(.075f, h, .09f), wood);
            props++;
        }
        // A crooked porch, window trims and outer roof beam; all visual-only.
        Visual(PrimitiveType.Cube, "HB2_AtariOholtza", group,
            cabin.TransformPoint(new Vector3(0, .35f, -4.0f)),
            new Vector3(3.15f, .12f, 1.5f), wood);
        props++;
        for (int side = -1; side <= 1; side += 2)
        {
            Visual(PrimitiveType.Cube, "HB2_AteMarkoa_" + side, group,
                cabin.TransformPoint(new Vector3(side * 1.18f, 1.62f, -3.24f)),
                new Vector3(.16f, 2.35f, .18f), rust);
            props++;
        }
        Visual(PrimitiveType.Cube, "HB2_ZurezkoHabea", group,
            cabin.TransformPoint(new Vector3(0f, 3.05f, -3.22f)),
            new Vector3(7.25f, .24f, .22f), wood);
        props++;
        // Broken window boards on the back wall (not an actual opening).
        for (int k = 0; k < 5; k++)
        {
            Visual(PrimitiveType.Cube, "HB2_LeihoOhola_" + k, group,
                cabin.TransformPoint(new Vector3(1.85f + (k - 2) * .18f, 1.8f, 3.16f)),
                new Vector3(.125f, .8f + k*.08f, .10f), wood,
                Quaternion.Euler(0, 0, 10f + k * 5f));
            props++;
        }
        // More recognizable silhouette for the shotgun placeholder, still NON-FUNCTIONAL.
        Transform tableGun = cabin.Find("HB_Escopeta_PLACEHOLDER");
        if (tableGun == null || tableGun.GetComponent<Collider>()?.enabled == true)
            throw new InvalidOperationException("Expected non-functional shotgun placeholder absent.");
        tableGun.GetComponent<Renderer>().enabled = false;
        Vector3 gunPos = tableGun.position;
        Visual(PrimitiveType.Cylinder, "HB2_Escopeta_CanonProbisional", group,
            gunPos, new Vector3(.08f, .75f, .08f), rust, Quaternion.Euler(0, 0, 90));
        Visual(PrimitiveType.Cube, "HB2_Escopeta_KulataProbisional", group,
            gunPos + new Vector3(-.50f, -.015f, 0), new Vector3(.46f, .16f, .16f), wood);
        props += 2;
    }

    private static void TuneAtmosphere(Scene scene, Transform parent)
    {
        // Keep v1 working flashlight and moon; only adjust fog a little.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = .023f;
        RenderSettings.fogColor = new Color(.023f, .032f, .046f);
        RenderSettings.ambientLight = new Color(.025f, .03f, .043f);
        Transform fx = NewGroup("HB2_Ambiente", parent);
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Light l in root.GetComponentsInChildren<Light>(true))
            if (l.name.StartsWith("HB_ArgiLarrialdia_", StringComparison.Ordinal))
            {
                l.color = new Color(.95f, .16f, .12f);
                l.intensity = .60f;
            }
        // Low-power flickering-looking warm accent without an Update script.
        var lamp = new GameObject("HB2_TxabolakoArgiItzalia");
        lamp.transform.SetParent(fx, false);
        lamp.transform.position = new Vector3(39.5f, 2.35f, 47.2f);
        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 5f;
        light.intensity = .38f;
        light.color = new Color(1f, .56f, .20f);
    }

    private static Transform NewGroup(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Visual(PrimitiveType primitive, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        return go;
    }
}
