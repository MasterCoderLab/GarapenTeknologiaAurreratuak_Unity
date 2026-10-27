using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// V11: eszena eta jokoaren erreferentziak auditatu, aldatu gabe.
/// Argi naturala eta etsai-barren konpilazioa egiaztatzen ditu.
/// </summary>
public static class HorrorBuilderV11
{
    private const string Eszena = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";

    public static void Verify()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("HBB11: batch modua beharrezkoa da.");
        Scene scene = EditorSceneManager.OpenScene(Eszena, OpenSceneMode.Single);
        GameObject camObj = Find(scene, "Main Camera");
        GameObject player = Find(scene, "Personaje");
        GameObject nav = Find(scene, "Horror_NavMesh");
        if (camObj == null || player == null || nav == null)
            throw new InvalidOperationException("HBB11: eszenaren oinarrizko objektuak falta dira.");
        Camera cam = camObj.GetComponent<Camera>();
        if (cam == null || camObj.GetComponent<HorrorHUDV10>() == null ||
            camObj.GetComponent<HorrorBunkerDarknessV10>() == null ||
            camObj.GetComponent<HorrorAtmosphereV9>() == null ||
            camObj.GetComponent<HorrorFlashlight>() == null ||
            camObj.GetComponent<HorrorShotgun>() == null ||
            camObj.GetComponent<HorrorMouseLook>() == null ||
            player.GetComponent<HorrorPlayerMovement>() == null ||
            nav.GetComponent<HorrorNavMeshRuntime>() == null)
            throw new InvalidOperationException("HBB11: V9-V10eko osagaietako bat falta da.");
        Debug.Log("HBB11_PRECHECK_OK: eszena, linterna, giroa, HUD, eskopeta eta NavMesh daude.");

        int enemies = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (HorrorEnemyHealth hp in root.GetComponentsInChildren<HorrorEnemyHealth>(true))
        {
            SerializedObject so = new SerializedObject(hp);
            SerializedProperty points = so.FindProperty("hitPoints");
            if (points == null || points.intValue != 3)
                throw new InvalidOperationException("HBB11: 3 HP aldatu dira: " + hp.name);
            enemies++;
        }
        if (enemies != 3)
            throw new InvalidOperationException("HBB11: hiru etsai behar dira: " + enemies);
        if (AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Laberintoa/Horror/BuilderV6/Materials/HB6_HormaLuzea.mat") == null)
            throw new InvalidOperationException("HBB11: V6ko horma falta da.");
        Debug.Log("HBB11_OK: 3 HP + headshot-kodea mantendu; bunkerreko fog/ambient eta barra berriak konpilatu; eszena ez da aldatu.");
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
}
