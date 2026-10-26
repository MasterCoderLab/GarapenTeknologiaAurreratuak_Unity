using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror eszenako 46 hormei probatutako hormigoizko materiala esleitzen die.
/// Ez ditu zorua, jatorrizko eszena edo talkagailuak aldatzen.
/// </summary>
public static class HorrorWallMaterialsBatch
{
    private const string ScenePath =
        "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string MaterialPath =
        "Assets/Laberintoa/Horror/M_Hormigoi_prueba.mat";

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Batch modua beharrezkoa da.");

        Material hormigoia = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (hormigoia == null || hormigoia.shader == null ||
            hormigoia.shader.name != "Universal Render Pipeline/Lit" ||
            hormigoia.GetTexture("_BaseMap") == null)
            throw new InvalidOperationException(
                "URP/Lit materiala edo Base Map testura falta da: " + MaterialPath);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<MeshRenderer> hormak = new List<MeshRenderer>();
        int zoruak = 0;
        bool sektoreak = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Horror_Sektoreak_X4") sektoreak = true;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string izena = renderer.gameObject.name;
                if (izena.StartsWith("Pared", StringComparison.Ordinal))
                    hormak.Add(renderer);
                else if (izena.StartsWith("Suelo", StringComparison.Ordinal))
                    zoruak++;
            }
        }

        if (!sektoreak || hormak.Count != 46 || zoruak != 4)
            throw new InvalidOperationException(
                "Auditoria huts egin du: " + hormak.Count +
                " horma, " + zoruak + " zoru; sektoreak=" + sektoreak);

        foreach (MeshRenderer renderer in hormak)
        {
            renderer.sharedMaterial = hormigoia;
            EditorUtility.SetDirty(renderer);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");

        // Diskotik berriro ireki eta aldaketa guztiak egiaztatu.
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int egiaztatuak = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.gameObject.name.StartsWith("Pared", StringComparison.Ordinal) &&
                    renderer.sharedMaterial == hormigoia)
                    egiaztatuak++;

        if (egiaztatuak != 46)
            throw new InvalidOperationException(
                "Materialaren egiaztapenak huts egin du: " + egiaztatuak + "/46");

        Debug.Log("HORROR_WALLS_OK: 46/46 horma hormigoiz; 4 zoru ukitu gabe.");
    }
}
