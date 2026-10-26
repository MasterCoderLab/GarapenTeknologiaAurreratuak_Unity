using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v1: auditatu, tutorial hondarrak garbitu eta munduaren lehen bertsioa eraiki.
/// Batch moduan bakarrik; SampleScene ez da irekitzen edo aldatzen.
/// </summary>
public static class HorrorBuilderV1
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string NavPath = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";
    private const string MaterialFolder = "Assets/Laberintoa/Horror/BuilderV1";
    private const string BuilderRoot = "HB_MunduOsoa_v1";
    private const string ExitWallName = "ParedEste";

    private static Material concrete;
    private static Material ground;
    private static Material wood;
    private static Material leaves;
    private static Material bark;
    private static Material stone;
    private static Material metal;
    private static Material path;
    private static readonly List<Collider> navExtras = new List<Collider>();

    public static void Audit()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch modua beharrezkoa da.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateOriginal(scene);
        Debug.Log("HBB1_AUDIT_OK: 3 prefab+AI+animazio, 46 horma, 4 zoru, 15 waypoint, 3 Sphere, 0 BuilderRoot");
    }

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch modua beharrezkoa da.");
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath) != null)
            throw new InvalidOperationException("Builder v1-eko NavMesh dagoeneko existitzen da. Ez errepikatu --apply.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateOriginal(scene);

        GameObject[] enemies = EnemyRoots(scene);
        GameObject player = Find(scene, "Personaje");
        var navLoader = Find(scene, "Horror_NavMesh").GetComponent<HorrorNavMeshRuntime>();
        GameObject sector = Find(scene, "Sektorea_1_1");
        Transform wallGroup = sector.transform.Find("Paredes_1_1");
        GameObject exitWall = wallGroup.Find(ExitWallName).gameObject;
        var wallRenderer = exitWall.GetComponent<MeshRenderer>();
        Material originalWallMat = wallRenderer != null ? wallRenderer.sharedMaterial : null;

        // Ez dira datu zaharrak aldatzen auditoretza osatu arte.
        CleanLegacy(scene, enemies, player);
        GameObject root = new GameObject(BuilderRoot);
        concrete = CreateMaterial("M_HB_Hormigoia", new Color(0.29f, 0.30f, 0.32f));
        ground = CreateMaterial("M_HB_Lurra", new Color(0.16f, 0.18f, 0.17f));
        wood = CreateMaterial("M_HB_EgurZaharra", new Color(0.29f, 0.21f, 0.15f));
        leaves = CreateMaterial("M_HB_HostoIlunak", new Color(0.055f, 0.10f, 0.074f));
        bark = CreateMaterial("M_HB_ZuhaitzAzala", new Color(0.18f, 0.13f, 0.11f));
        stone = CreateMaterial("M_HB_Harria", new Color(0.21f, 0.24f, 0.25f));
        metal = CreateMaterial("M_HB_Metala", new Color(0.19f, 0.22f, 0.25f));
        path = CreateMaterial("M_HB_Bidezidorra", new Color(0.23f, 0.20f, 0.16f));

        BuildRoof(root.transform);
        BuildBunkerDecoration(root.transform);
        BuildExit(root.transform, exitWall, originalWallMat ?? concrete);
        BuildForest(root.transform);
        BuildCabin(root.transform);
        SetupNightAndFlashlight(scene, root.transform, player);
        BuildNavMesh(scene, navLoader, player.transform.position, enemies);

        int roof = CountPrefix(root, "HB_Teilatu_");
        int trees = CountPrefix(root, "HB_Zuhaitza_");
        if (roof != 4 || trees < 45 || root.transform.Find("HB_Chabola") == null ||
            root.transform.Find("HB_BasoGaua") == null || root.transform.Find("HB_Irteera") == null)
            throw new InvalidOperationException("Munduaren egituraren balidazioa huts egin du.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();

        // Diskotik berriz irakurri: Editor API-k prefab erreferentzia guztiak ebazten ditu.
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject saved = Find(scene, BuilderRoot);
        if (saved == null || CountPrefix(saved, "HB_Teilatu_") != 4 ||
            CountPrefix(saved, "HB_Zuhaitza_") < 45 ||
            Find(scene, "Horror_NavMesh")?.GetComponent<HorrorNavMeshRuntime>()?.navMeshData == null ||
            Find(scene, "Personaje")?.GetComponent<HorrorPlayerMovement>() == null ||
            Find(scene, "Main Camera")?.GetComponent<HorrorFlashlight>() == null)
            throw new InvalidOperationException("Gordetako eszenaren egiaztapena huts egin du.");
        foreach (GameObject enemy in EnemyRoots(scene))
        {
            if (enemy.GetComponent<Enemigo>() != null || enemy.GetComponent<HorrorEnemyAI>() == null ||
                enemy.GetComponent("HorrorEnemyAnimation") == null || enemy.GetComponent<NavMeshAgent>() == null)
                throw new InvalidOperationException("Etsaia ez da zuzen garbitu: " + enemy.name);
        }
        if (Find(scene, "WayPoint1a") != null || Find(scene, "Sphere") != null)
            throw new InvalidOperationException("Hondar zaharrak aurkitu dira.");
        Debug.Log("HBB1_OK: búnker 4 techos, salida, linterna F, bosque nocturno, cabana, NavMesh v1 nuevo, legacy limpio; SampleScene intacta.");
    }

    private static void ValidateOriginal(Scene scene)
    {
        if (Find(scene, BuilderRoot) != null)
            throw new InvalidOperationException("Builder v1 jada aplikatuta: ez da bikoiztuko.");
        if (Find(scene, "Horror_Sektoreak_X4") == null || Find(scene, "Horror_NavMesh") == null ||
            Find(scene, "Horror_Enemigos_X3") == null || Find(scene, "Personaje") == null)
            throw new InvalidOperationException("Horror eszenaren egitura falta da.");
        int walls = 0, floors = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (box.name.StartsWith("Pared", StringComparison.Ordinal)) walls++;
            if (box.name.StartsWith("Suelo", StringComparison.Ordinal)) floors++;
        }
        if (walls != 46 || floors != 4)
            throw new InvalidOperationException("Geometria ustekabea: " + walls + " horma, " + floors + " zoru.");
        GameObject[] enemies = EnemyRoots(scene);
        foreach (int i in new[] {0, 1, 2})
        {
            GameObject e = enemies[i];
            if (e == null || e.GetComponent<Enemigo>() == null || e.GetComponent<Enemigo>().enabled ||
                e.GetComponent<HorrorEnemyAI>() == null ||
                e.GetComponent("HorrorEnemyAnimation") == null ||
                e.GetComponent<NavMeshAgent>() == null ||
                e.GetComponent<CapsuleCollider>() == null ||
                e.transform.Find("Creep" + (i + 1) + "_Visual") == null ||
                e.transform.Find("Sphere") == null)
                throw new InvalidOperationException("Etsaien egitura desberdina: " + (e == null ? i.ToString() : e.name));
        }
        if (Find(scene, "Sektorea_1_1")?.transform.Find("Paredes_1_1/ParedEste")?.GetComponent<BoxCollider>() == null)
            throw new InvalidOperationException("Irteteko kanpoko horma falta da.");
        if (Find(scene, "Personaje").GetComponent<Personaje>() == null ||
            Find(scene, "Personaje").GetComponent<SphereCollider>() == null ||
            Find(scene, "Personaje").GetComponent<Rigidbody>() == null ||
            Find(scene, "Main Camera")?.GetComponent<Camera>() == null)
            throw new InvalidOperationException("Jokalaria edo kamera falta da.");
        int oldPoints = 0;
        foreach (GameObject go in AllSceneObjects(scene))
            if (go.name.StartsWith("Creep2_WayPoint1", StringComparison.Ordinal) ||
                go.name.StartsWith("Creep3_WayPoint1", StringComparison.Ordinal) ||
                go.name.StartsWith("WayPoint1", StringComparison.Ordinal))
            {
                if (go.GetComponents<Component>().Length != 1 || go.transform.childCount != 0)
                    throw new InvalidOperationException("Waypoint erabilia/aldatua: " + go.name);
                oldPoints++;
            }
        if (oldPoints != 15) throw new InvalidOperationException("Waypoint kopurua ez da 15: " + oldPoints);
        Debug.Log("HBB1_PRECHECK: prefabak Unity API bidez egiaztatuta; 3 enemy+IA+animazio; 46 horma, 4 zoru, 15 waypoint.");
    }

    private static void CleanLegacy(Scene scene, GameObject[] enemies, GameObject player)
    {
        foreach (GameObject enemy in enemies)
        {
            UnityEngine.Object.DestroyImmediate(enemy.GetComponent<Enemigo>());
            Transform sphere = enemy.transform.Find("Sphere");
            if (sphere.gameObject.activeSelf) throw new InvalidOperationException("Sphere bizirik dago: " + enemy.name);
            UnityEngine.Object.DestroyImmediate(sphere.gameObject);
        }
        List<GameObject> points = new List<GameObject>();
        foreach (GameObject obj in AllSceneObjects(scene))
            if (obj.name.StartsWith("Creep2_WayPoint1", StringComparison.Ordinal) ||
                obj.name.StartsWith("Creep3_WayPoint1", StringComparison.Ordinal) ||
                obj.name.StartsWith("WayPoint1", StringComparison.Ordinal))
                points.Add(obj);
        if (points.Count != 15) throw new InvalidOperationException("Waypoint kopurua aldatu da.");
        foreach (GameObject point in points) UnityEngine.Object.DestroyImmediate(point);
        UnityEngine.Object.DestroyImmediate(player.GetComponent<Personaje>());
        UnityEngine.Object.DestroyImmediate(player.GetComponent<SphereCollider>());
        CapsuleCollider hitbox = player.AddComponent<CapsuleCollider>();
        hitbox.radius = .33f;
        hitbox.height = 1.8f;
        hitbox.center = Vector3.zero;
        player.AddComponent<HorrorPlayerMovement>();
        if (player.GetComponent<MeshRenderer>() != null) player.GetComponent<MeshRenderer>().enabled = false;
    }

    private static void BuildRoof(Transform root)
    {
        float[] xs = {0f, 20f};
        float[] zs = {0f, 15f};
        int num = 0;
        foreach (float x in xs)
        foreach (float z in zs)
        {
            GameObject panel = Box("HB_Teilatu_" + (++num), root,
                new Vector3(x, 2.77f, z), new Vector3(20.1f, 0.40f, 15.1f), concrete);
            // Techo colisionable, pero NO fuente de NavMesh.
            if (panel.GetComponent<BoxCollider>() == null) throw new InvalidOperationException("Teilatua ez du collider.");
        }
    }

    private static void BuildBunkerDecoration(Transform root)
    {
        Transform decoration = new GameObject("HB_BunkerDekorazioa").transform;
        decoration.SetParent(root, false);
        // Detalle industrial ligero: no alteramos pasillos ni navegación con colisiones nuevas.
        Vector3[] sectorLights = {
            new Vector3(-3f, 2.3f, 0f), new Vector3(17f, 2.3f, 0f),
            new Vector3(-3f, 2.3f, 15f), new Vector3(17f, 2.3f, 15f)
        };
        for (int i = 0; i < sectorLights.Length; i++)
        {
            var lamp = new GameObject("HB_ArgiLarrialdia_" + (i + 1));
            lamp.transform.SetParent(decoration, false);
            lamp.transform.position = sectorLights[i];
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.9f, .16f, .09f);
            light.range = 6f;
            light.intensity = .45f;
        }
        Vector3[] boxLocations = {
            new Vector3(-6f, .9f, -5.5f), new Vector3(14f, .9f, -5.5f),
            new Vector3(-6f, .9f, 9.5f), new Vector3(14f, .9f, 9.5f)
        };
        for (int i = 0; i < boxLocations.Length; i++)
        {
            GameObject crate = Box("HB_KutxaProbisionala_" + (i + 1), decoration,
                boxLocations[i], new Vector3(.9f, .8f, .8f), wood);
            crate.GetComponent<Collider>().enabled = false;
        }
    }

    private static void BuildExit(Transform root, GameObject wall, Material oldMaterial)
    {
        Transform group = new GameObject("HB_Irteera").transform;
        group.SetParent(root, false);
        // Kanpoko horma: Paredes_1_1 / ParedEste (z=22); irekidura x=24..27.
        Bounds b = wall.GetComponent<BoxCollider>().bounds;
        if (Mathf.Abs(b.center.z - 22f) > 1f || Mathf.Abs(b.size.x - 20f) > 1f)
            throw new InvalidOperationException("Irteera hormaren kokapena ustekabea: " + b);
        UnityEngine.Object.DestroyImmediate(wall);
        Box("HB_Nav_IrteeraEzker", group, new Vector3(17f, 1.5f, 22f), new Vector3(14f, 2f, 1f), oldMaterial);
        Box("HB_Nav_IrteeraEskuin", group, new Vector3(28.5f, 1.5f, 22f), new Vector3(3f, 2f, 1f), oldMaterial);
        // Atearen goiko habeak 2 m-ko pasabidea uzten du (lurreko goia y=0.5).
        Box("HB_IrteeraAteBurua", group, new Vector3(25.5f, 2.52f, 22f),
            new Vector3(3.05f, 0.09f, 1.0f), metal);
        Box("HB_IrteeraSeinalea", group, new Vector3(25.5f, 2.55f, 21.35f),
            new Vector3(2f, 0.16f, 0.13f), metal);
        GameObject warning = new GameObject("HB_IrteeraArgiGorria");
        warning.transform.SetParent(group, false);
        warning.transform.position = new Vector3(25.5f, 2.35f, 20.8f);
        Light light = warning.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.10f, 0.06f);
        light.range = 5f;
        light.intensity = 1.8f;
    }

    private static void BuildForest(Transform root)
    {
        Transform forest = new GameObject("HB_BasoGaua").transform;
        forest.SetParent(root, false);
        GameObject floor = Box("HB_Nav_BosqueSuelo", forest,
            new Vector3(25f, -0.03f, 44f), new Vector3(80f, 1f, 46f), ground);
        navExtras.Add(floor.GetComponent<Collider>());
        // Atea -> basoa: lehen bidea hutsik mantendu, animaliak eta jokalaria igarotzeko.
        Box("HB_Bidezidorra", forest, new Vector3(27f, 0.49f, 32f),
            new Vector3(4.4f, 0.03f, 17f), path).GetComponent<Collider>().enabled = false;
        var rnd = new System.Random(20261026);
        int built = 0;
        for (int i = 0; i < 100 && built < 65; i++)
        {
            float x = (float)(-11.0 + rnd.NextDouble() * 71.0);
            float z = (float)(26.0 + rnd.NextDouble() * 37.0);
            // Sarrera, bidezidorra eta etxolaren inguruak hutsik.
            if (Vector2.Distance(new Vector2(x, z), new Vector2(25.5f, 26f)) < 6f ||
                (z < 48f && x > 22f && x < 35f) ||
                Vector2.Distance(new Vector2(x, z), new Vector2(38f, 52f)) < 9f) continue;
            Transform tree = new GameObject("HB_Zuhaitza_" + (++built).ToString("D2")).transform;
            tree.SetParent(forest, false);
            tree.position = new Vector3(x, 0.47f, z);
            float height = 4.0f + (float)rnd.NextDouble() * 3.1f;
            float radius = 0.32f + (float)rnd.NextDouble() * 0.12f;
            GameObject trunk = Cylinder("HB_Nav_Tronco", tree,
                new Vector3(0, height / 2f, 0), new Vector3(radius * 2f, height / 2f, radius * 2f), bark);
            navExtras.Add(trunk.GetComponent<Collider>());
            Sphere("HB_Koroa1", tree, new Vector3(0, height * 0.78f, 0),
                new Vector3(2.6f, 3.2f, 2.6f), leaves).GetComponent<Collider>().enabled = false;
            Sphere("HB_Koroa2", tree, new Vector3(0, height * 1.02f, 0),
                new Vector3(1.7f, 2.6f, 1.7f), leaves).GetComponent<Collider>().enabled = false;
        }
        if (built < 45) throw new InvalidOperationException("Basoko zuhaitz nahikorik ez.");
        for (int i = 0; i < 25; i++)
        {
            float x = (float)(-7.0 + rnd.NextDouble() * 64f);
            float z = (float)(28 + rnd.NextDouble() * 34);
            if ((z < 47f && x > 22f && x < 35f) ||
                Vector2.Distance(new Vector2(x, z), new Vector2(38f, 52f)) < 7f) continue;
            Sphere("HB_Harria_" + i, forest, new Vector3(x, 0.55f, z),
                new Vector3(0.6f, 0.3f, 0.8f), stone).GetComponent<Collider>().enabled = false;
        }
    }

    private static void BuildCabin(Transform root)
    {
        Transform cabin = new GameObject("HB_Chabola").transform;
        cabin.SetParent(root, false);
        cabin.position = new Vector3(38f, 0.47f, 52f);
        GameObject cabinFloor = BoxLocal("HB_Nav_ChabolaSuelo", cabin,
            new Vector3(0, 0.13f, 0), new Vector3(7f, 0.25f, 6f), wood);
        navExtras.Add(cabinFloor.GetComponent<Collider>());
        // Ate irekia hegoaldean; etxola barrura sar daiteke.
        CabinWall("HB_Nav_AtzekoHorma", cabin, new Vector3(0, 1.54f, 3f), new Vector3(7f, 2.6f, .22f));
        CabinWall("HB_Nav_EzkerHorma", cabin, new Vector3(-3.5f, 1.54f, 0), new Vector3(.22f, 2.6f, 6f));
        CabinWall("HB_Nav_EskuinHorma", cabin, new Vector3(3.5f, 1.54f, 0), new Vector3(.22f, 2.6f, 6f));
        CabinWall("HB_Nav_AurrealdeaEzker", cabin, new Vector3(-2.35f, 1.54f, -3f), new Vector3(2.30f, 2.6f, .22f));
        CabinWall("HB_Nav_AurrealdeaEskuin", cabin, new Vector3(2.35f, 1.54f, -3f), new Vector3(2.30f, 2.6f, .22f));
        BoxLocal("HB_ChabolaAteBurua", cabin, new Vector3(0, 2.7f, -3f),
            new Vector3(2.4f, .30f, .24f), wood);
        GameObject roofL = BoxLocal("HB_ChabolaTeilatuaEzker", cabin,
            new Vector3(-1.76f, 3.38f, 0), new Vector3(4f, .22f, 6.7f), stone);
        roofL.transform.localRotation = Quaternion.Euler(0, 0, -17f);
        GameObject roofR = BoxLocal("HB_ChabolaTeilatuaEskuin", cabin,
            new Vector3(1.76f, 3.38f, 0), new Vector3(4f, .22f, 6.7f), stone);
        roofR.transform.localRotation = Quaternion.Euler(0, 0, 17f);
        BoxLocal("HB_MahaiZaharra", cabin, new Vector3(-1.8f, .95f, 1.7f),
            new Vector3(2.1f, .20f, 1.2f), wood);
        GameObject placeholder = BoxLocal("HB_Escopeta_PLACEHOLDER", cabin,
            new Vector3(-1.9f, 1.11f, 1.7f), new Vector3(1.5f, .13f, .17f), metal);
        placeholder.GetComponent<Collider>().enabled = false;
        GameObject lamp = new GameObject("HB_ChabolaArgi");
        lamp.transform.SetParent(cabin, false);
        lamp.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .55f, .23f);
        light.range = 8f;
        light.intensity = 1.6f;
    }

    private static void CabinWall(string name, Transform parent, Vector3 localPosition, Vector3 size)
    {
        GameObject wall = BoxLocal(name, parent, localPosition, size, wood);
        navExtras.Add(wall.GetComponent<Collider>());
    }

    private static void SetupNightAndFlashlight(Scene scene, Transform root, GameObject player)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.025f, .030f, .043f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = .019f;
        RenderSettings.fogColor = new Color(.028f, .038f, .049f);
        RenderSettings.skybox = null;
        foreach (GameObject item in AllSceneObjects(scene))
        if (item.name == "Directional Light")
        {
            Light moon = item.GetComponent<Light>();
            if (moon != null)
            {
                moon.intensity = .13f;
                moon.color = new Color(.37f, .49f, 1f);
                moon.shadows = LightShadows.Soft;
            }
        }
        Camera camera = Find(scene, "Main Camera")?.GetComponent<Camera>();
        if (camera == null || camera.GetComponent<HorrorFlashlight>() != null)
            throw new InvalidOperationException("Linternaren kamera okerra.");
        GameObject torch = new GameObject("HB_Linterna");
        torch.transform.SetParent(camera.transform, false);
        torch.transform.localPosition = new Vector3(0, -0.06f, .2f);
        torch.transform.localRotation = Quaternion.identity;
        Light spot = torch.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 67f;
        spot.range = 20f;
        spot.intensity = 10f;
        spot.color = new Color(.95f, .96f, 1f);
        spot.shadows = LightShadows.Soft;
        spot.shadowBias = .04f;
        camera.gameObject.AddComponent<HorrorFlashlight>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.008f, .012f, .025f);
    }

    private static void BuildNavMesh(Scene scene, HorrorNavMeshRuntime loader,
        Vector3 playerPosition, GameObject[] enemies)
    {
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        int oldWalls = 0, oldFloors = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.enabled || !box.gameObject.activeInHierarchy) continue;
            if (box.name.StartsWith("Pared", StringComparison.Ordinal)) { AddBoxSource(sources, box); oldWalls++; }
            else if (box.name.StartsWith("Suelo", StringComparison.Ordinal)) { AddBoxSource(sources, box); oldFloors++; }
        }
        // Kanpoko horma bat ireki dugu: 45 zahar + ateko 2 tarte berri.
        if (oldWalls != 45 || oldFloors != 4)
            throw new InvalidOperationException("NavMesh aurreko geometria desberdina: " + oldWalls + "/45; " + oldFloors + "/4");
        GameObject exit = Find(scene, "HB_Irteera");
        foreach (BoxCollider b in exit.GetComponentsInChildren<BoxCollider>())
            if (b.name.StartsWith("HB_Nav_", StringComparison.Ordinal)) AddBoxSource(sources, b);
        foreach (Collider collider in navExtras)
        {
            if (collider == null || !collider.enabled) continue;
            BoxCollider box = collider as BoxCollider;
            if (box != null) AddBoxSource(sources, box);
            else
            {
                // Basoko enborren CapsuleCollider-ak kaxa baliokide gisa gehitu.
                Bounds bound = collider.bounds;
                sources.Add(new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(bound.center, Quaternion.identity, Vector3.one),
                    size = bound.size,
                    area = 0
                });
            }
        }
        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = .28f;
        settings.agentHeight = 1.8f;
        settings.agentClimb = .35f;
        settings.agentSlope = 40f;
        settings.minRegionArea = .1f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = .09f;
        Bounds bounds = new Bounds(new Vector3(22f, 1f, 27f), new Vector3(110f, 11f, 100f));
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds,
            Vector3.zero, Quaternion.identity);
        if (data == null) throw new InvalidOperationException("NavMesh berria ez da sortu.");
        NavMeshDataInstance temp = NavMesh.AddNavMeshData(data);
        if (!temp.valid) throw new InvalidOperationException("NavMesh ezin aktibatu.");
        try
        {
            Vector3 doorOutside = new Vector3(25.5f, .6f, 27f);
            Vector3 cabinEntrance = new Vector3(38f, .6f, 47f);
            if (!CanReach(playerPosition, doorOutside))
                throw new InvalidOperationException("Jokalariak ezin du atea gurutzatu: NavMesh.");
            if (!CanReach(doorOutside, cabinEntrance))
                throw new InvalidOperationException("Basoaren eta etxolaren arteko biderik ez dago.");
            foreach (GameObject enemy in enemies)
                if (!CanReach(enemy.transform.position, doorOutside))
                    throw new InvalidOperationException("Etsaiak ezin du basora irten: " + enemy.name);
        }
        finally { temp.Remove(); }
        // NavMesh zaharra bere horretan geratzen da; loader berrira lotuko da.
        AssetDatabase.CreateAsset(data, NavPath);
        loader.navMeshData = data;
        EditorUtility.SetDirty(loader);
        Debug.Log("HBB1_NAV_OK: bunker -> atea -> basoa -> etxola; 3 enemies reach the exit.");
    }

    private static bool CanReach(Vector3 a, Vector3 b)
    {
        if (!NavMesh.SamplePosition(a, out NavMeshHit start, 1.8f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(b, out NavMeshHit end, 1.8f, NavMesh.AllAreas)) return false;
        var pathCheck = new NavMeshPath();
        return NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, pathCheck) &&
            pathCheck.status == NavMeshPathStatus.PathComplete;
    }

    private static void AddBoxSource(List<NavMeshBuildSource> sources, BoxCollider box)
    {
        sources.Add(new NavMeshBuildSource {
            shape = NavMeshBuildSourceShape.Box,
            transform = box.transform.localToWorldMatrix * Matrix4x4.Translate(box.center),
            size = box.size,
            area = 0
        });
    }

    private static GameObject[] EnemyRoots(Scene scene)
    {
        return new[] {Find(scene, "Enemigo"), Find(scene, "Enemigo_Creep2"), Find(scene, "Enemigo_Creep3")};
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

    private static List<GameObject> AllSceneObjects(Scene scene)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                result.Add(tr.gameObject);
        return result;
    }

    private static int CountPrefix(GameObject root, string prefix)
    {
        int count = 0;
        foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
            if (tr.name.StartsWith(prefix, StringComparison.Ordinal)) count++;
        return count;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string p = MaterialFolder + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(p) != null)
            throw new InvalidOperationException("Materiala existitzen da: " + p);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP/Lit shader ez da aurkitu.");
        var result = new Material(shader) { name = name };
        result.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(result, p);
        return result;
    }

    private static GameObject Box(string name, Transform parent, Vector3 worldPos, Vector3 worldSize, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.localScale = worldSize;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }
    private static GameObject BoxLocal(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
    private static GameObject Cylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
    private static GameObject Sphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
