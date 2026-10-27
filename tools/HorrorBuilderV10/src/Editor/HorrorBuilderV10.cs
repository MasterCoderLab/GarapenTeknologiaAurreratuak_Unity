using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v10: HUD, bunkerreko iluntasuna eta etsaien osasun-barrak.
/// Ezin du irakaslearen SampleScene edo NavMesh aldatu.
/// </summary>
public static class HorrorBuilderV10
{
    private const string Eszena = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Kartutxoa = "Assets/Resources/HorrorHUDV10/shell_ready.png";
    private const string KartutxoHutsa = "Assets/Resources/HorrorHUDV10/shell_empty.png";
    private const string Bineta = "Assets/Resources/HorrorHUDV10/bunker_vignette.png";

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("HBB10: Unity batch modua beharrezkoa da.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string path in new[] { Kartutxoa, KartutxoHutsa, Bineta })
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                throw new InvalidOperationException("HBB10: HUD irudia falta da: " + path);

        Scene eszena = EditorSceneManager.OpenScene(Eszena, OpenSceneMode.Single);
        GameObject kameraObj = Bilatu(eszena, "Main Camera");
        GameObject jokalaria = Bilatu(eszena, "Personaje");
        GameObject nav = Bilatu(eszena, "Horror_NavMesh");
        if (kameraObj == null || jokalaria == null || nav == null ||
            jokalaria.GetComponent<HorrorPlayerMovement>() == null ||
            jokalaria.GetComponent<HorrorPlayerJump>() == null ||
            kameraObj.GetComponent<HorrorMouseLook>() == null ||
            kameraObj.GetComponent<HorrorViewmodelSafety>() == null ||
            nav.GetComponent<HorrorNavMeshRuntime>() == null)
            throw new InvalidOperationException("HBB10: V9ren mugimendua, kamera edo NavMesh falta da.");

        HorrorShotgun eskopeta = kameraObj.GetComponent<HorrorShotgun>();
        HorrorAtmosphereV9 atmosfera = kameraObj.GetComponent<HorrorAtmosphereV9>();
        HorrorFlashlight flashlightController = kameraObj.GetComponent<HorrorFlashlight>();
        Transform spot = kameraObj.transform.Find("HB_Linterna");
        Light linterna = spot == null ? null : spot.GetComponent<Light>();
        if (eskopeta == null || atmosfera == null || flashlightController == null ||
            linterna == null || kameraObj.GetComponent<HorrorHUDV10>() != null ||
            kameraObj.GetComponent<HorrorBunkerDarknessV10>() != null)
            throw new InvalidOperationException("HBB10: erreferentzia falta edo V10 lehendik instalatuta.");

        HorrorGameManager manager = null;
        int hpCount = 0;
        foreach (GameObject root in eszena.GetRootGameObjects())
        {
            foreach (HorrorGameManager gm in root.GetComponentsInChildren<HorrorGameManager>(true))
            {
                if (manager != null) throw new InvalidOperationException("HBB10: bi GameManager aurkitu dira.");
                manager = gm;
            }
            hpCount += root.GetComponentsInChildren<HorrorEnemyHealth>(true).Length;
        }
        if (manager == null || hpCount != 3)
            throw new InvalidOperationException("HBB10: GameManager edo hiru etsaiak falta dira: " + hpCount);
        Debug.Log("HBB10_PRECHECK_OK: V9 aktibo; 3 etsai, kamera, linterna eta UI erreferentziak aurkituta.");

        // Eszenak V3ko hitPoints=2 gorde dezake. Hiru gorputz-tiro behar dira beti.
        int configured = 0;
        foreach (GameObject root in eszena.GetRootGameObjects())
        foreach (HorrorEnemyHealth hp in root.GetComponentsInChildren<HorrorEnemyHealth>(true))
        {
            SerializedObject so = new SerializedObject(hp);
            SerializedProperty hits = so.FindProperty("hitPoints");
            if (hits == null) throw new InvalidOperationException("HBB10: hitPoints propietatea falta da.");
            hits.intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hp);
            configured++;
        }

        HorrorHUDV10 hud = kameraObj.AddComponent<HorrorHUDV10>();
        hud.Konfiguratu(manager, eskopeta);
        HorrorBunkerDarknessV10 dark = kameraObj.AddComponent<HorrorBunkerDarknessV10>();
        dark.Konfiguratu(atmosfera, linterna);
        EditorSceneManager.MarkSceneDirty(eszena);
        if (!EditorSceneManager.SaveScene(eszena))
            throw new InvalidOperationException("HBB10: eszena ezin da gorde.");
        AssetDatabase.SaveAssets();
        eszena = EditorSceneManager.OpenScene(Eszena, OpenSceneMode.Single);
        kameraObj = Bilatu(eszena, "Main Camera");
        HorrorHUDV10 hud2 = kameraObj == null ? null : kameraObj.GetComponent<HorrorHUDV10>();
        HorrorBunkerDarknessV10 dark2 = kameraObj == null ? null : kameraObj.GetComponent<HorrorBunkerDarknessV10>();
        if (hud2 == null || dark2 == null)
            throw new InvalidOperationException("HBB10: HUD edo iluntasuna ez da eszenan gorde.");
        int barCount = 0;
        foreach (GameObject root in eszena.GetRootGameObjects())
        foreach (HorrorEnemyHealth hp in root.GetComponentsInChildren<HorrorEnemyHealth>(true))
        {
            SerializedObject so = new SerializedObject(hp);
            SerializedProperty hits = so.FindProperty("hitPoints");
            if (hits == null || hits.intValue != 3)
                throw new InvalidOperationException("HBB10: etsaiak ez ditu hiru bizitza: " + hp.name);
            barCount++;
        }
        if (barCount != 3) throw new InvalidOperationException("HBB10: ez dira hiru etsai: " + barCount);
        Debug.Log("HBB10_OK: HUD kartutxoekin + bizitza gorria, 3 etsaien barrak, headshot eta bunker ilunagoa; NavMesh/V9 mugimendua bere horretan.");
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
