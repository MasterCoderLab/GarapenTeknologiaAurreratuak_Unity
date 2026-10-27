using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v6: saguaren ikuspegia, kamera hormetatik babestea eta
/// hormigoi PBRaren distira/erliebea leuntzea. Ez aldatu kolisiorik.
/// </summary>
public static class HorrorBuilderV6
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Folder = "Assets/Laberintoa/Horror/BuilderV6/Materials";
    private const string V5 = "Assets/Laberintoa/Horror/BuilderV5/Materials/";
    private const string Nav = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";
    private const string ScriptPath = "Assets/Laberintoa/Scripts/HorrorMouseLook.cs";
    private static readonly string[] Variants = { "HB5_HormaLaburra", "HB5_HormaErtaina", "HB5_HormaLuzea" };

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Builder v6 batch moduan bakarrik exekutatu.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check(scene, false);
        Debug.Log("HBB6_PRECHECK_OK: v5 PBR, v4 jauzia, 3 etsai, kamera eta NavMesh egiaztatuta.");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            // Unityk karpeta lehenengo aldian ez ikustea ekidin.
            AssetDatabase.ImportAsset(Folder + "/LEEME_MATERIALAK.txt",
                ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        if (!AssetDatabase.IsValidFolder(Folder))
            throw new InvalidOperationException("V6 materialen karpeta falta da: " + Folder);

        var replacements = new Dictionary<Material, Material>();
        foreach (string name in Variants)
        {
            Material original = AssetDatabase.LoadAssetAtPath<Material>(V5 + name + ".mat");
            if (original == null) throw new InvalidOperationException("V5 materiala falta da: " + name);
            string newPath = Folder + "/" + name.Replace("HB5_", "HB6_") + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(newPath) != null)
                throw new InvalidOperationException("V6 materiala lehendik dago: " + newPath);
            Material material = new Material(original) { name = name.Replace("HB5_", "HB6_") };
            // Material zaharrak gorde: normal-map eta distira handiegiak leundu.
            material.SetFloat("_BumpScale", 0.33f);
            material.SetFloat("_Smoothness", 0.43f);
            AssetDatabase.CreateAsset(material, newPath);
            replacements.Add(original, material);
        }

        int switched = 0;
        var encountered = new HashSet<Renderer>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer == null || !encountered.Add(renderer)) continue;
            if (!replacements.TryGetValue(renderer.sharedMaterial, out Material better))
                throw new InvalidOperationException("Horma ez da V5 materialekoa: " + box.name);
            renderer.sharedMaterial = better;
            switched++;
        }
        if (switched != 45)
            throw new InvalidOperationException("V5 45 horma espero ziren; aurkitu: " + switched);

        // Sarrerako bi zati eta v4-ko bi atzeko itxierak ere hormigoi berarekin.
        foreach (string name in new[] { "HB_Nav_IrteeraEzker", "HB_Nav_IrteeraEskuin",
                     "HB4_AtzekoIrteeraItxita_1", "HB4_AtzekoIrteeraItxita_2" })
            ReplaceObjectMaterial(scene, name, replacements);
        for (int i = 1; i <= 4; ++i)
            ReplaceObjectMaterial(scene, "HB_Teilatu_" + i, replacements);

        GameObject player = Find(scene, "Personaje");
        GameObject cameraObject = Find(scene, "Main Camera");
        Camera camera = cameraObject.GetComponent<Camera>();
        // 0,3 m-ko near clip zaharrak pareta agerian jar zezakeen hurbiletik.
        // Kamera kapsularen erdian mantendu, 10 cm barrurantz atzeratuta.
        camera.nearClipPlane = 0.045f;
        camera.transform.localPosition = new Vector3(0f, 0f, -0.10f);
        if (cameraObject.GetComponent<HorrorMouseLook>() != null)
            throw new InvalidOperationException("HorrorMouseLook jada instalatuta dago.");
        cameraObject.AddComponent<HorrorMouseLook>();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Horror eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();

        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check(scene, true);
        Debug.Log("HBB6_OK: Mouse X/Y + pitch mugak, kamera near=0.045, 45 horma PBR leunago; NavMesh eta jokagarritasuna bere horretan.");
    }

    private static void ReplaceObjectMaterial(Scene scene, string name, Dictionary<Material, Material> replacements)
    {
        GameObject obj = Find(scene, name);
        Renderer rend = obj == null ? null : obj.GetComponent<Renderer>();
        if (rend == null || !replacements.TryGetValue(rend.sharedMaterial, out Material m))
            throw new InvalidOperationException("Material ezezaguna: " + name);
        rend.sharedMaterial = m;
    }

    private static void Check(Scene scene, bool installed)
    {
        foreach (string name in new[] { "HB_MunduOsoa_v1", "HB_MunduOsoa_v2", "HB3_Gameplay",
                    "HB4_Hobekuntzak", "Personaje", "Main Camera", "Horror_NavMesh", "HB_Irteera" })
            if (Find(scene, name) == null)
                throw new InvalidOperationException("Eszenan falta da: " + name);

        GameObject player = Find(scene, "Personaje");
        GameObject cam = Find(scene, "Main Camera");
        GameObject nav = Find(scene, "Horror_NavMesh");
        if (cam.transform.parent != player.transform ||
            player.GetComponent<Rigidbody>() == null || player.GetComponent<CapsuleCollider>() == null ||
            player.GetComponent<HorrorPlayerMovement>() == null || player.GetComponent<HorrorPlayerJump>() == null ||
            player.GetComponent<HorrorPlayerHealth>() == null ||
            cam.GetComponent<HorrorShotgun>() == null || cam.GetComponent<HorrorFlashlight>() == null)
            throw new InvalidOperationException("Kameraren edo jokalariaren egitura aldatu da.");

        HorrorNavMeshRuntime loader = nav.GetComponent<HorrorNavMeshRuntime>();
        if (loader == null || loader.delayedAgents == null || loader.delayedAgents.Length != 3 ||
            AssetDatabase.GetAssetPath(loader.navMeshData) != Nav)
            throw new InvalidOperationException("NavMesh konfigurazioa aldatu da.");

        if (!installed && cam.GetComponent<HorrorMouseLook>() != null)
            throw new InvalidOperationException("HorrorMouseLook lehendik dago.");
        if (installed)
        {
            Camera camera = cam.GetComponent<Camera>();
            if (cam.GetComponent<HorrorMouseLook>() == null ||
                Mathf.Abs(camera.nearClipPlane - 0.045f) > .003f ||
                Vector3.Distance(cam.transform.localPosition, new Vector3(0f,0f,-.10f)) > .001f)
                throw new InvalidOperationException("Kameraren v6 konfigurazioa ez da gorde.");
            if (Find(scene, "HB4_AtzekoIrteeraItxita_1")?.GetComponent<BoxCollider>() == null ||
                Find(scene, "HB4_AtzekoIrteeraItxita_2")?.GetComponent<BoxCollider>() == null)
                throw new InvalidOperationException("Atzeko irteeraren kolisioa falta da.");
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
                if (box.name.StartsWith("Pared", StringComparison.Ordinal) &&
                    box.GetComponent<Renderer>()?.sharedMaterial?.name.StartsWith("HB6_Horma", StringComparison.Ordinal) == true)
                    count++;
            if (count != 45)
                throw new InvalidOperationException("Ez dira 45 horma gorde V6 materialekin: " + count);
        }
        else
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(Folder + "/HB6_HormaLuzea.mat") != null)
                throw new InvalidOperationException("V6 aurretik aplikatua dago.");
            foreach (string name in Variants)
                if (AssetDatabase.LoadAssetAtPath<Material>(V5 + name + ".mat") == null)
                    throw new InvalidOperationException("V5 materiala ez da existitzen: " + name);
        }
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item.gameObject;
        }
        return null;
    }
}
