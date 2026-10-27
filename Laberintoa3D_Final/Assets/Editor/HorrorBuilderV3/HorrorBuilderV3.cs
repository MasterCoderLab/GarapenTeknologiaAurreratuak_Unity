using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror v3 jokagarriaren instalatzailea. Ez du NavMesh-a, IA edo SampleScene aldatzen.
/// </summary>
public static class HorrorBuilderV3
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string NavPath = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";
    private const string MaterialMetal = "Assets/Laberintoa/Horror/BuilderV1/M_HB_Metala.mat";
    private const string MaterialWood = "Assets/Laberintoa/Horror/BuilderV1/M_HB_EgurZaharra.mat";
    private static readonly string[] EnemyNames = {"Enemigo", "Enemigo_Creep2", "Enemigo_Creep3"};

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch modua beharrezkoa da.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Validate(scene);
        Debug.Log("HBB3_PRECHECK_OK: Horror v2 + NavMesh Fix; 3 enemigos, camara y escopeta provisional.");

        GameObject player = Find(scene, "Personaje");
        GameObject cameraObject = Find(scene, "Main Camera");
        Transform pickup = Find(scene, "HB_Escopeta_PLACEHOLDER").transform;
        GameObject barrel = Find(scene, "HB2_Escopeta_CanonProbisional");
        GameObject stock = Find(scene, "HB2_Escopeta_KulataProbisional");

        var root = new GameObject("HB3_Gameplay");
        HorrorGameManager manager = root.AddComponent<HorrorGameManager>();
        HorrorPlayerHealth playerHealth = player.AddComponent<HorrorPlayerHealth>();
        playerHealth.Configure(manager);
        HorrorShotgun shotgun = cameraObject.AddComponent<HorrorShotgun>();
        GameObject weapon = BuildFirstPersonGun(cameraObject.transform);
        shotgun.Configure(manager, player.transform, pickup,
            new[] {barrel, stock}, weapon);

        var hp = new HorrorEnemyHealth[EnemyNames.Length];
        for (int i = 0; i < EnemyNames.Length; ++i)
        {
            GameObject enemy = Find(scene, EnemyNames[i]);
            hp[i] = enemy.AddComponent<HorrorEnemyHealth>();
            hp[i].Configure(manager);
        }
        manager.Configure(player.GetComponent<HorrorPlayerMovement>(), shotgun, hp);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Ezin izan da Horror eszena gorde.");
        AssetDatabase.SaveAssets();

        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject saved = Find(scene, "HB3_Gameplay");
        GameObject savedPlayer = Find(scene, "Personaje");
        GameObject savedCamera = Find(scene, "Main Camera");
        HorrorGameManager savedManager = saved == null ? null : saved.GetComponent<HorrorGameManager>();
        HorrorShotgun savedGun = savedCamera == null ? null : savedCamera.GetComponent<HorrorShotgun>();
        if (savedManager == null || savedGun == null ||
            savedPlayer.GetComponent<HorrorPlayerHealth>() == null ||
            savedPlayer.GetComponent<HorrorPlayerMovement>() == null ||
            Find(scene, "HB3_EscopetaEnMano") == null)
            throw new InvalidOperationException("Jokagarritasunaren erreferentziak ezin egiaztatu.");
        foreach (string name in EnemyNames)
            if (Find(scene, name).GetComponent<HorrorEnemyHealth>() == null)
                throw new InvalidOperationException("Etsaiaren osasuna falta da: " + name);
        HorrorNavMeshRuntime nav = Find(scene, "Horror_NavMesh").GetComponent<HorrorNavMeshRuntime>();
        if (nav == null || nav.delayedAgents == null || nav.delayedAgents.Length != 3 ||
            AssetDatabase.GetAssetPath(nav.navMeshData) != NavPath)
            throw new InvalidOperationException("NavMesh erreferentzia aldatu da.");
        Debug.Log("HBB3_OK: escopeta E, disparo clic, recarga R, 3 vidas, 3 enemigos con HP, HUD, victoria y derrota; SampleScene y NavMesh intactos.");
    }

    private static void Validate(Scene scene)
    {
        if (Find(scene, "HB3_Gameplay") != null || Find(scene, "HB3_EscopetaEnMano") != null)
            throw new InvalidOperationException("Horror v3 dagoeneko instalatuta dago.");
        if (Find(scene, "HB_MunduOsoa_v1") == null || Find(scene, "HB_MunduOsoa_v2") == null)
            throw new InvalidOperationException("Horror v1/v2 falta da.");
        GameObject player = Find(scene, "Personaje");
        GameObject camera = Find(scene, "Main Camera");
        if (player == null || camera == null || camera.GetComponent<Camera>() == null ||
            camera.transform.parent != player.transform ||
            player.GetComponent<HorrorPlayerMovement>() == null ||
            camera.GetComponent<HorrorFlashlight>() == null ||
            camera.GetComponent<HorrorShotgun>() != null ||
            player.GetComponent<HorrorPlayerHealth>() != null)
            throw new InvalidOperationException("Jokalaria / kamera ez dator bat.");
        if (Find(scene, "HB_Escopeta_PLACEHOLDER") == null ||
            Find(scene, "HB2_Escopeta_CanonProbisional") == null ||
            Find(scene, "HB2_Escopeta_KulataProbisional") == null)
            throw new InvalidOperationException("Eskopeta probisionala falta da.");
        HorrorNavMeshRuntime runtime = Find(scene, "Horror_NavMesh")?.GetComponent<HorrorNavMeshRuntime>();
        if (runtime == null || runtime.navMeshData == null ||
            AssetDatabase.GetAssetPath(runtime.navMeshData) != NavPath ||
            runtime.delayedAgents == null || runtime.delayedAgents.Length != 3)
            throw new InvalidOperationException("NavMesh Fix ez dago behar bezala instalatuta.");
        for (int i = 0; i < EnemyNames.Length; ++i)
        {
            GameObject enemy = Find(scene, EnemyNames[i]);
            if (enemy == null || enemy.GetComponent<HorrorEnemyAI>() == null ||
                enemy.GetComponent<HorrorEnemyAnimation>() == null ||
                enemy.GetComponent<HorrorEnemyHealth>() != null ||
                enemy.GetComponent<NavMeshAgent>() == null ||
                enemy.GetComponent<NavMeshAgent>().enabled ||
                runtime.delayedAgents[i] != enemy.GetComponent<NavMeshAgent>())
                throw new InvalidOperationException("Etsaia edo NavMesh agentea ez da egokia: " + EnemyNames[i]);
        }
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialMetal) == null ||
            AssetDatabase.LoadAssetAtPath<Material>(MaterialWood) == null)
            throw new InvalidOperationException("V1 materialak falta dira.");
    }

    private static GameObject BuildFirstPersonGun(Transform camera)
    {
        var root = new GameObject("HB3_EscopetaEnMano");
        root.transform.SetParent(camera, false);
        root.transform.localPosition = new Vector3(0.34f, -0.29f, 0.66f);
        root.transform.localRotation = Quaternion.Euler(0, -8f, 0);
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialWood);
        Material metal = AssetDatabase.LoadAssetAtPath<Material>(MaterialMetal);
        Part(root.transform, "HB3_Kulata", new Vector3(0, -0.08f, -0.15f),
            new Vector3(.13f, .19f, .43f), wood);
        Part(root.transform, "HB3_Bascula", new Vector3(0, 0f, .20f),
            new Vector3(.13f, .15f, .38f), metal);
        Part(root.transform, "HB3_Canon", new Vector3(0f, .025f, .68f),
            new Vector3(.075f, .075f, .70f), metal);
        root.SetActive(false);
        return root;
    }

    private static void Part(Transform parent, string name, Vector3 localPosition,
        Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<BoxCollider>());
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
}
