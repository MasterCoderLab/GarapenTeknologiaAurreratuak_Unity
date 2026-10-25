using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Irakaslearen jatorrizko labirintoa kopiatzen du, eta lau sektore lotzen ditu.
/// Jatorrizko eszena, pertsonaia eta etsaia ez dira aldatzen.
/// </summary>
public static class LaberintoaHorrorBuilder
{
    private const string Source = "Assets/Laberintoa/Scenes/SampleScene.unity";
    private const string Target = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const float OpeningWidth = 2f;

    [MenuItem("Tools/Laberintoa Horror/Ampliar original x4")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Gelditu Play Mode labirintoa zabaltzeko.");
            return;
        }

        Scene sourceScene = SceneManager.GetActiveScene();
        if (sourceScene.path != Source || sourceScene.isDirty)
        {
            Debug.LogError("Ireki eta gorde SampleScene jatorrizkoa; ez aldatu ezer bertan.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Target) != null ||
            System.IO.File.Exists(Target))
        {
            Debug.LogError("Laberintoa_Horror dagoeneko existitzen da. Ez da gainidatziko.");
            return;
        }

        GameObject suelo = FindRoot(sourceScene, "Suelo");
        GameObject paredes = FindRoot(sourceScene, "Paredes");
        GameObject player = FindRoot(sourceScene, "Personaje");
        GameObject enemy = FindRoot(sourceScene, "Enemigo");

        if (suelo == null || paredes == null || player == null || enemy == null)
        {
            Debug.LogError("Jatorrizko lau objektuetako bat falta da.");
            return;
        }

        Renderer sueloRenderer = suelo.GetComponent<Renderer>();
        if (sueloRenderer == null)
        {
            Debug.LogError("Suelo objektuak ez du Renderer osagairik.");
            return;
        }

        Vector3 tile = sueloRenderer.bounds.size;
        if (Mathf.Abs(tile.x - 20f) > 0.1f || Mathf.Abs(tile.z - 15f) > 0.1f)
        {
            Debug.LogError("Ustekabeko lur-neurriak: " + tile);
            return;
        }

        // Irakaslearen izenek ez dute benetako orientazioa adierazten.
        string[] outerWalls = { "ParedSur", "ParedNorte", "ParedEste", "ParedOeste1", "ParedOeste2" };
        foreach (string wallName in outerWalls)
        {
            if (paredes.transform.Find(wallName) == null)
            {
                Debug.LogError("Kanpoko horma falta da: " + wallName);
                return;
            }
        }

        if (!AssetDatabase.CopyAsset(Source, Target))
        {
            Debug.LogError("Ezin izan da jatorrizko eszena kopiatu.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
        suelo = FindRoot(scene, "Suelo");
        paredes = FindRoot(scene, "Paredes");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Laberintoa Horror x4");

        try
        {
            GameObject group = new GameObject("Horror_Sektoreak_X4");
            Undo.RegisterCreatedObjectUndo(group, "Sektore berriak sortu");

            // [x,z] indizeetako (0,0) sektorea jatorrizkoa da.
            Transform[,] sectors = new Transform[2, 2];
            sectors[0, 0] = paredes.transform;

            for (int z = 0; z < 2; z++)
            {
                for (int x = 0; x < 2; x++)
                {
                    if (x == 0 && z == 0)
                        continue;

                    Vector3 offset = new Vector3(x * tile.x, 0f, z * tile.z);
                    GameObject section = new GameObject("Sektorea_" + x + "_" + z);
                    Undo.RegisterCreatedObjectUndo(section, "Sektorea sortu");
                    section.transform.SetParent(group.transform, true);

                    CloneAt(suelo, "Suelo_" + x + "_" + z, offset, section.transform);
                    GameObject walls = CloneAt(
                        paredes, "Paredes_" + x + "_" + z, offset, section.transform);
                    sectors[x, z] = walls.transform;
                }
            }

            // X noranzkoan: partekatutako bi hormetan pasabide bera ireki.
            for (int z = 0; z < 2; z++)
            {
                SplitWall(sectors[0, z].Find("ParedSur"), false);
                SplitWall(sectors[1, z].Find("ParedNorte"), false);
            }

            // Z noranzkoan: beheko horma osoa ireki;
            // gaineko sektorearen behealdeak 2 m-ko hutsunea dauka jada.
            for (int x = 0; x < 2; x++)
                SplitWall(sectors[x, 0].Find("ParedEste"), true);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = group;
            Debug.Log("LABERINTO X4 SORTUTA (" + tile.x * 2f + " x " +
                      tile.z * 2f + "). Aztertu pasabideak eta probatu gorde aurretik.");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(ex);
            Debug.LogError("Zabalkuntza bertan behera geratu da. Jatorrizkoa bere horretan dago.");
        }
    }

    /// <summary>Objektua munduko posizioa mantenduz bikoizten du.</summary>
    private static GameObject CloneAt(GameObject original, string newName,
        Vector3 offset, Transform parent)
    {
        GameObject clone = UnityEngine.Object.Instantiate(original);
        Undo.RegisterCreatedObjectUndo(clone, "Geometria kopiatu");
        clone.name = newName;
        clone.transform.SetParent(parent, true);
        clone.transform.position = original.transform.position + offset;
        return clone;
    }

    /// <summary>
    /// Kanpoko horma bi zatitan banatzen du, erdian bi metroko hutsunea utziz.
    /// Aztertutako kanpoko hormak biratu gabe daude.
    /// </summary>
    private static void SplitWall(Transform wall, bool alongX)
    {
        if (wall == null)
            throw new InvalidOperationException("Loturako horma ez da aurkitu.");

        Vector3 originalPosition = wall.localPosition;
        Vector3 scale = wall.localScale;
        float length = alongX ? scale.x : scale.z;
        if (length <= OpeningWidth)
            throw new InvalidOperationException("Horma laburregia: " + wall.name);

        float halfSegment = (length - OpeningWidth) / 2f;
        float displacement = (length + OpeningWidth) / 4f;

        GameObject secondObject = UnityEngine.Object.Instantiate(
            wall.gameObject, wall.parent);
        Undo.RegisterCreatedObjectUndo(secondObject, "Bigarren horma zatia");
        secondObject.name = wall.name + "_B";

        Undo.RecordObject(wall, "Pasabidea ireki");
        Vector3 firstPosition = originalPosition;
        Vector3 secondPosition = originalPosition;
        if (alongX)
        {
            scale.x = halfSegment;
            firstPosition.x -= displacement;
            secondPosition.x += displacement;
        }
        else
        {
            scale.z = halfSegment;
            firstPosition.z -= displacement;
            secondPosition.z += displacement;
        }

        wall.localScale = scale;
        wall.localPosition = firstPosition;
        secondObject.transform.localScale = scale;
        secondObject.transform.localPosition = secondPosition;
    }

    /// <summary>Eszenako erro-objektua izenaren arabera bilatzen du.</summary>
    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }
}
