using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror labirintoko lau zoruei URP/Lit hormigoia ezartzen die.
/// Hormak, talkagailuak eta jatorrizko eszena ez ditu aldatzen.
/// </summary>
public static class HorrorFloorMaterialsBatch
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string WallMaterialPath = "Assets/Laberintoa/Horror/M_Hormigoi_prueba.mat";
    private const string FloorTexturePath = "Assets/Laberintoa/Horror/Textures/Floor/ConcreteFloor_Color_2k.jpg";
    private const string FloorMaterialPath = "Assets/Laberintoa/Horror/M_Hormigoi_zorua.mat";

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Batch modua beharrezkoa da.");

        Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
        Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorTexturePath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (wallMaterial == null || color == null || shader == null)
            throw new InvalidOperationException("URP shader-a, horma materiala edo zoru testura falta da.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var walls = new List<MeshRenderer>();
        var floors = new List<MeshRenderer>();
        bool hasSectors = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Horror_Sektoreak_X4") hasSectors = true;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = r.gameObject.name;
                if (name.StartsWith("Pared", StringComparison.Ordinal)) walls.Add(r);
                else if (name == "Suelo" || name.StartsWith("Suelo_", StringComparison.Ordinal)) floors.Add(r);
            }
        }

        if (!hasSectors || walls.Count != 46 || floors.Count != 4)
            throw new InvalidOperationException("Egitura okerra: " + walls.Count + " horma, " + floors.Count + " zoru.");

        foreach (MeshRenderer wall in walls)
            if (wall.sharedMaterial != wallMaterial)
                throw new InvalidOperationException("Horma batek ez du espero zen materiala: " + wall.gameObject.name);

        // Egiaztatu lau zoruek material zahar bera dutela.
        Material oldFloor = floors[0].sharedMaterial;
        if (oldFloor == null || oldFloor == wallMaterial)
            throw new InvalidOperationException("Zoruen jatorrizko materiala okerra da.");
        foreach (MeshRenderer floor in floors)
            if (floor.sharedMaterial != oldFloor)
                throw new InvalidOperationException("Zoruen materialak ez datoz bat.");

        if (AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath) != null)
            throw new InvalidOperationException("Zoru material berria jadanik existitzen da.");

        Material floorMat = new Material(shader);
        floorMat.name = "M_Hormigoi_zorua";
        floorMat.SetColor("_BaseColor", Color.white);
        floorMat.SetTexture("_BaseMap", color);
        floorMat.SetTextureScale("_BaseMap", new Vector2(10f, 7.5f));
        floorMat.SetFloat("_Metallic", 0f);
        floorMat.SetFloat("_Smoothness", 0.12f);
        AssetDatabase.CreateAsset(floorMat, FloorMaterialPath);
        AssetDatabase.SaveAssets();

        foreach (MeshRenderer floor in floors)
        {
            floor.sharedMaterial = floorMat;
            EditorUtility.SetDirty(floor);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");

        // Gorde ondoren, diskotik berriro ireki eta bi kopuruak egiaztatu.
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int floorsOk = 0, wallsOk = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = r.gameObject.name;
                if (name.StartsWith("Pared", StringComparison.Ordinal) && r.sharedMaterial == wallMaterial)
                    wallsOk++;
                if ((name == "Suelo" || name.StartsWith("Suelo_", StringComparison.Ordinal)) && r.sharedMaterial == floorMat)
                    floorsOk++;
            }
        }
        if (floorsOk != 4 || wallsOk != 46)
            throw new InvalidOperationException("Egiaztapen huts egin du: zoru=" + floorsOk + ", horma=" + wallsOk);
        Debug.Log("HORROR_FLOORS_OK: 4/4 zoru hormigoiz; 46/46 horma mantenduta.");
    }
}
