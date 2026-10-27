using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v9: FPS albo-mugimendua, bunker ilunagoa eta basoko linterna.
/// Bakar-bakarrik Horror eszenari gehitzen dio argiztapen-kontrolatzailea.
/// </summary>
public static class HorrorBuilderV9
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string V6 = "Assets/Laberintoa/Horror/BuilderV6/Materials/";
    private static readonly string[] Neurriak = { "Laburra", "Ertaina", "Luzea" };

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("HBB9: batchmode beharrezkoa da.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Egiaztatu(scene, false);
        Debug.Log("HBB9_PRECHECK_OK: V8ko 45 horma, 4 teilatu, linterna, kamera eta NavMesh egiaztatuta.");

        GameObject kamera = Bilatu(scene, "Main Camera");
        GameObject jokalaria = Bilatu(scene, "Personaje");
        Light linterna = kamera.transform.Find("HB_Linterna").GetComponent<Light>();
        Light ilargia = Bilatu(scene, "Directional Light").GetComponent<Light>();
        BoxCollider[] teilatuak = new BoxCollider[4];
        for (int i = 0; i < 4; i++)
            teilatuak[i] = Bilatu(scene, "HB_Teilatu_" + (i + 1)).GetComponent<BoxCollider>();

        HorrorAtmosphereV9 giroa = kamera.AddComponent<HorrorAtmosphereV9>();
        giroa.Konfiguratu(jokalaria.transform, linterna, ilargia, teilatuak);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("HBB9: Horror eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Egiaztatu(scene, true);
        Debug.Log("HBB9_OK: A/D albo-mugimendua; bunkerrean laino eta giro-argi ilunagoak; basoan linterna 28m/14; V8ko materialak eta joko-sistemak bere horretan.");
    }

    private static void Egiaztatu(Scene scene, bool instalatuta)
    {
        GameObject player = Bilatu(scene, "Personaje");
        GameObject cam = Bilatu(scene, "Main Camera");
        GameObject moonGo = Bilatu(scene, "Directional Light");
        GameObject nav = Bilatu(scene, "Horror_NavMesh");
        if (player == null || cam == null || moonGo == null || nav == null ||
            player.GetComponent<Rigidbody>() == null ||
            player.GetComponent<HorrorPlayerMovement>() == null ||
            player.GetComponent<HorrorPlayerJump>() == null ||
            cam.GetComponent<HorrorMouseLook>() == null ||
            cam.GetComponent<HorrorViewmodelSafety>() == null ||
            cam.GetComponent<HorrorFlashlight>() == null ||
            cam.GetComponent<HorrorShotgun>() == null ||
            Bilatu(scene, "HB7_LehenPlanokoKamera") == null ||
            nav.GetComponent<HorrorNavMeshRuntime>() == null)
            throw new InvalidOperationException("HBB9: jokalariaren/V7ren/NavMesharen egitura ez da zuzena.");

        HorrorAtmosphereV9 component = cam.GetComponent<HorrorAtmosphereV9>();
        if (instalatuta != (component != null))
            throw new InvalidOperationException("HBB9: kontrolatzailearen egoera ustekabekoa.");

        Transform spot = cam.transform.Find("HB_Linterna");
        Light flashlight = spot == null ? null : spot.GetComponent<Light>();
        if (flashlight == null || flashlight.type != LightType.Spot ||
            Mathf.Abs(flashlight.intensity - 10f) > .02f ||
            Mathf.Abs(flashlight.range - 20f) > .02f)
            throw new InvalidOperationException("HBB9: linternaren V8 balioak aldatu dira.");
        Light moon = moonGo.GetComponent<Light>();
        if (moon == null || moon.type != LightType.Directional ||
            Mathf.Abs(moon.intensity - .29f) > .02f)
            throw new InvalidOperationException("HBB9: V8ko ilargiaren balioa aldatu da.");

        for (int i = 1; i <= 4; i++)
            if (Bilatu(scene, "HB_Teilatu_" + i)?.GetComponent<BoxCollider>() == null)
                throw new InvalidOperationException("HBB9: bunkerreko teilatua falta da: " + i);

        int hormak = 0;
        var ikusitakoak = new HashSet<Renderer>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer == null || !ikusitakoak.Add(renderer)) continue;
            string path = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            bool onartuta = false;
            foreach (string izena in Neurriak)
                if (path == V6 + "HB6_Horma" + izena + ".mat") onartuta = true;
            if (!onartuta)
                throw new InvalidOperationException("HBB9: V6koa ez den horma: " + box.name);
            hormak++;
        }
        if (hormak != 45)
            throw new InvalidOperationException("HBB9: ez dira 45 horma: " + hormak);

        if (RenderSettings.fogMode != FogMode.Exponential)
            throw new InvalidOperationException("HBB9: gaueko lainoa ez dago Exponential moduan.");
    }

    private static GameObject Bilatu(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        }
        return null;
    }
}
