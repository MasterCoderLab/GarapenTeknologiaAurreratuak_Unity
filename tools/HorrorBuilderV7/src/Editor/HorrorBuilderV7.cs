using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// V7: hormen materialeko distirak leundu, eta arma/linterna URP Overlay kameran marraztu.
/// Fisikaren, NavMesh-aren eta jokoaren logika ez da aldatzen.
/// </summary>
public static class HorrorBuilderV7
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Folder = "Assets/Laberintoa/Horror/BuilderV7/Materials";
    private const string V6 = "Assets/Laberintoa/Horror/BuilderV6/Materials/";
    private const string NavPath = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";
    private const int ModelLayer = 30;
    private static readonly string[] Names = {"HB6_HormaLaburra", "HB6_HormaErtaina", "HB6_HormaLuzea"};

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Batch moduan bakarrik exekutatu.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check(scene, false);
        Debug.Log("HBB7_PRECHECK_OK: V6 kamera, hormak eta bi ikusgai-modeloak egiaztatuta.");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.ImportAsset(Folder + "/LEEME_MATERIALAK.txt", ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        if (!AssetDatabase.IsValidFolder(Folder))
            throw new InvalidOperationException("V7 materialen karpeta falta da: " + Folder);

        var variants = new Dictionary<Material, Material>();
        foreach (string name in Names)
        {
            Material old = AssetDatabase.LoadAssetAtPath<Material>(V6 + name + ".mat");
            if (old == null) throw new InvalidOperationException("V6 materiala falta da: " + name);
            string newName = name.Replace("HB6_", "HB7_");
            string path = Folder + "/" + newName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                throw new InvalidOperationException("V7 materiala dagoeneko existitzen da: " + path);
            Material mat = new Material(old) { name = newName };
            mat.SetFloat("_BumpScale", .12f);
            mat.SetFloat("_Smoothness", .27f);
            AssetDatabase.CreateAsset(mat, path);
            variants.Add(old, mat);
        }

        int total = 0;
        var seen = new HashSet<Renderer>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider wall in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!wall.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer render = wall.GetComponent<Renderer>();
            if (render == null || !seen.Add(render)) continue;
            if (!variants.TryGetValue(render.sharedMaterial, out Material replacement))
                throw new InvalidOperationException("Material desberdina horman: " + wall.name);
            render.sharedMaterial = replacement;
            total++;
        }
        if (total != 45) throw new InvalidOperationException("45 horma espero ziren: " + total);
        foreach (string name in new[] {
            "HB_Nav_IrteeraEzker", "HB_Nav_IrteeraEskuin",
            "HB4_AtzekoIrteeraItxita_1", "HB4_AtzekoIrteeraItxita_2",
            "HB_Teilatu_1", "HB_Teilatu_2", "HB_Teilatu_3", "HB_Teilatu_4"
        }) Replace(scene, name, variants);

        GameObject cameraGo = Find(scene, "Main Camera");
        Camera world = cameraGo.GetComponent<Camera>();
        GameObject weapon = Find(scene, "HB3_EscopetaEnMano");
        GameObject torch = Find(scene, "HB4_LinternaIkusgai");
        GameObject player = Find(scene, "Personaje");
        if (world == null || weapon == null || torch == null || player == null)
            throw new InvalidOperationException("Ikusgai-modeloak falta dira.");

        // Geruza isolatua: ez dugu TagManager edo ProjectSettings aldatzen.
        SetLayer(weapon.transform, ModelLayer);
        SetLayer(torch.transform, ModelLayer);
        world.cullingMask &= ~(1 << ModelLayer);
        UniversalAdditionalCameraData mainUrp = world.GetUniversalAdditionalCameraData();
        mainUrp.renderType = CameraRenderType.Base;
        if (mainUrp.cameraStack.Count != 0)
            throw new InvalidOperationException("Lehendik kameraren stack bat dago; ez aldatu automatikoki.");

        var viewGo = new GameObject("HB7_LehenPlanokoKamera");
        viewGo.transform.SetParent(cameraGo.transform, false);
        viewGo.transform.localPosition = Vector3.zero;
        viewGo.transform.localRotation = Quaternion.identity;
        Camera overlay = viewGo.AddComponent<Camera>();
        overlay.tag = "Untagged";
        overlay.cullingMask = 1 << ModelLayer;
        overlay.fieldOfView = world.fieldOfView;
        overlay.nearClipPlane = .015f;
        overlay.farClipPlane = 4f;
        overlay.depth = world.depth + 1f;
        overlay.allowHDR = world.allowHDR;
        overlay.allowMSAA = world.allowMSAA;
        UniversalAdditionalCameraData extra = overlay.GetUniversalAdditionalCameraData();
        extra.renderType = CameraRenderType.Overlay;
        // URP 17: clearDepth irakurtzeko bakarrik da.
        // Editor bidez konfiguratu eta egiaztatu.
        var overlayData = new SerializedObject(extra);
        var depthFlag = overlayData.FindProperty("m_ClearDepth");

        if (depthFlag == null)
            throw new InvalidOperationException("URP: m_ClearDepth ez da aurkitu.");

        depthFlag.boolValue = true;
        overlayData.ApplyModifiedPropertiesWithoutUndo();

        if (!extra.clearDepth)
            throw new InvalidOperationException("URP: Clear Depth ez da aktibatu.");
        extra.renderShadows = false;
        mainUrp.cameraStack.Add(overlay);

        if (cameraGo.GetComponent<HorrorViewmodelSafety>() != null)
            throw new InvalidOperationException("V7 segurtasun-script-a dagoeneko badago.");
        cameraGo.AddComponent<HorrorViewmodelSafety>().Konfiguratu(
            weapon.transform, torch.transform, player.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check(scene, true);
        Debug.Log("HBB7_OK: 45 horma PBR leundu; URP Overlay + arma/linterna atzeratze dinamikoa; fisika bere horretan.");
    }

    private static void SetLayer(Transform root, int layer)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            item.gameObject.layer = layer;
    }

    private static void Replace(Scene scene, string name, Dictionary<Material, Material> changes)
    {
        GameObject item = Find(scene, name);
        Renderer rend = item == null ? null : item.GetComponent<Renderer>();
        if (rend == null || !changes.TryGetValue(rend.sharedMaterial, out Material replacement))
            throw new InvalidOperationException("V6 materiala falta da: " + name);
        rend.sharedMaterial = replacement;
    }

    private static void Check(Scene scene, bool installed)
    {
        GameObject player = Find(scene, "Personaje");
        GameObject main = Find(scene, "Main Camera");
        GameObject weapon = Find(scene, "HB3_EscopetaEnMano");
        GameObject torch = Find(scene, "HB4_LinternaIkusgai");
        GameObject nav = Find(scene, "Horror_NavMesh");
        if (player == null || main == null || weapon == null || torch == null || nav == null ||
            weapon.transform.parent != main.transform || torch.transform.parent != main.transform ||
            main.GetComponent<HorrorShotgun>() == null ||
            main.GetComponent<HorrorFlashlight>() == null ||
            main.GetComponent<HorrorMouseLook>() == null ||
            player.GetComponent<HorrorPlayerJump>() == null ||
            nav.GetComponent<HorrorNavMeshRuntime>() == null ||
            AssetDatabase.GetAssetPath(nav.GetComponent<HorrorNavMeshRuntime>().navMeshData) != NavPath)
            throw new InvalidOperationException("Horror V6 eszenaren egitura desberdina da.");
        Camera baseCamera = main.GetComponent<Camera>();
        UniversalAdditionalCameraData data = main.GetComponent<UniversalAdditionalCameraData>();
        if (baseCamera == null) throw new InvalidOperationException("Kamera falta da.");
        if (!installed)
        {
            if (main.GetComponent<HorrorViewmodelSafety>() != null ||
                Find(scene, "HB7_LehenPlanokoKamera") != null ||
                AssetDatabase.LoadAssetAtPath<Material>(Folder + "/HB7_HormaLuzea.mat") != null)
                throw new InvalidOperationException("Builder V7 lehendik aplikatu da.");
            if (data != null && data.cameraStack.Count != 0)
                throw new InvalidOperationException("Beste kamera-stack bat dago.");
            // Geruza hutsik egon behar du kamera eta arma berrietatik kanpo.
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                if (tr.gameObject.layer == ModelLayer)
                    throw new InvalidOperationException("30. geruza jadanik erabilia: " + tr.name);
        }
        else
        {
            GameObject ov = Find(scene, "HB7_LehenPlanokoKamera");
            Camera overlay = ov == null ? null : ov.GetComponent<Camera>();
            if (overlay == null || ov.transform.parent != main.transform ||
                main.GetComponent<HorrorViewmodelSafety>() == null ||
                data == null || data.cameraStack.Count != 1 ||
                data.cameraStack[0] != overlay ||
                overlay.GetComponent<UniversalAdditionalCameraData>()?.renderType != CameraRenderType.Overlay ||
                !overlay.GetComponent<UniversalAdditionalCameraData>().clearDepth ||
                (baseCamera.cullingMask & (1 << ModelLayer)) != 0 ||
                (overlay.cullingMask & (1 << ModelLayer)) == 0 ||
                weapon.layer != ModelLayer || torch.layer != ModelLayer)
                throw new InvalidOperationException("URP lehen-planoko kamera ez da behar bezala gorde.");
            int walls = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
                if (box.name.StartsWith("Pared", StringComparison.Ordinal) &&
                    box.GetComponent<Renderer>()?.sharedMaterial?.name.StartsWith("HB7_Horma", StringComparison.Ordinal) == true)
                    walls++;
            if (walls != 45)
                throw new InvalidOperationException("V7-ko 45 hormak ez dira gorde: " + walls);
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
}
