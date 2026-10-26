using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror eszenan hiru izaki jartzen ditu: jatorrizko etsaia eta bi kopia.
/// Etsaien patruila, talkagailuak eta bost waypointak mantentzen dira.
/// </summary>
public static class HorrorThreeCreepsBatch
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string PrefabDir = "Assets/Creep Horror Creature/Prefabs/";
    private const string DemoController =
        "Assets/Creep Horror Creature/DemoSceneAssets/DemoAnimCont.controller";
    private static readonly string[] WaypointNames = {
        "WayPoint1a", "WayPoint1b", "WayPoint1c", "WayPoint1d", "WayPoint1e"
    };

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Batch modua beharrezkoa da.");

        // Baliozkotu prefabak eta eszena, aldaketarik egin baino lehen.
        var prefabs = new GameObject[3];
        for (int i = 0; i < prefabs.Length; i++)
        {
            string path = PrefabDir + "Creep" + (i + 1) + ".prefab";
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabs[i] == null)
                throw new InvalidOperationException("Prefab falta da: " + path);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("Eszena baliogabea.");
        GameObject original = FindRoot(scene, "Enemigo");
        if (original == null || FindRoot(scene, "Horror_Enemigos_X3") != null ||
            FindRoot(scene, "Horror_Sektoreak_X4") == null)
            throw new InvalidOperationException("Eszenaren egitura ez da espero zena.");

        Enemigo oldPatrol = original.GetComponent<Enemigo>();
        CapsuleCollider originalCapsule = original.GetComponent<CapsuleCollider>();
        Transform oldSphere = original.transform.Find("Sphere");
        if (oldPatrol == null || originalCapsule == null || oldSphere == null)
            throw new InvalidOperationException("Jatorrizko etsaia ez dago osorik.");

        Transform[] basePoints = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject obj = FindRoot(scene, WaypointNames[i]);
            if (obj == null) throw new InvalidOperationException("Waypoint falta: " + WaypointNames[i]);
            basePoints[i] = obj.transform;
        }
        var serialized = new SerializedObject(oldPatrol);
        var patrolPoints = serialized.FindProperty("waypoints");
        if (patrolPoints == null || !patrolPoints.isArray || patrolPoints.arraySize != 5)
            throw new InvalidOperationException("Patruila ez da 5 puntukoa.");
        for (int i = 0; i < 5; i++)
            if (patrolPoints.GetArrayElementAtIndex(i).objectReferenceValue != basePoints[i])
                throw new InvalidOperationException("Patruila aldatu da, eten egin da.");

        int walls = 0, floors = 0;
        CountGeometry(scene, out walls, out floors);
        if (walls != 46 || floors != 4)
            throw new InvalidOperationException("Geometria: " + walls + " horma, " + floors + " zoru.");

        // Bigarren eta hirugarren sektoreek jatorrizkoaren geometria bera dute.
        Vector3[] offsets = {
            Vector3.zero, new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 15f)
        };
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DemoController);

        // Jatorrizko GameObject bere horretan uzten dugu patruila eta talkagailuekin.
        GameObject group = new GameObject("Horror_Enemigos_X3");
        GameObject[] enemies = { original, null, null };
        for (int i = 1; i < 3; i++)
        {
            GameObject clone = UnityEngine.Object.Instantiate(original);
            clone.name = "Enemigo_Creep" + (i + 1);
            clone.transform.position = original.transform.position + offsets[i];
            clone.transform.SetParent(group.transform, true);
            enemies[i] = clone;

            // Patruila independentea: waypoint berdinak, sektorearen desplazamenduarekin.
            Transform[] newPoints = new Transform[5];
            for (int j = 0; j < 5; j++)
            {
                var waypoint = new GameObject("Creep" + (i + 1) + "_" + WaypointNames[j]);
                waypoint.transform.SetParent(group.transform, false);
                waypoint.transform.position = basePoints[j].position + offsets[i];
                newPoints[j] = waypoint.transform;
            }
            SerializedObject newPatrol = new SerializedObject(clone.GetComponent<Enemigo>());
            SerializedProperty p = newPatrol.FindProperty("waypoints");
            p.arraySize = 5;
            for (int j = 0; j < 5; j++)
                p.GetArrayElementAtIndex(j).objectReferenceValue = newPoints[j];
            newPatrol.ApplyModifiedPropertiesWithoutUndo();
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject enemy = enemies[i];
            var primitiveRenderer = enemy.GetComponent<MeshRenderer>();
            if (primitiveRenderer == null || enemy.GetComponent<CapsuleCollider>() == null ||
                enemy.GetComponent<Enemigo>() == null)
                throw new InvalidOperationException("Etsaiaren oinarrizko osagaiak falta dira.");
            primitiveRenderer.enabled = false;
            Transform sphere = enemy.transform.Find("Sphere");
            if (sphere == null) throw new InvalidOperationException("Sphere falta da.");
            sphere.gameObject.SetActive(false);

            var visual = PrefabUtility.InstantiatePrefab(prefabs[i], scene) as GameObject;
            if (visual == null)
                throw new InvalidOperationException("Prefab instantzia ezin izan da sortu.");
            visual.name = "Creep" + (i + 1) + "_Visual";
            visual.transform.SetParent(enemy.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Prefabaren talkagailuek ez dute jatorrizko kapsula bikoiztuko.
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            if (animators.Length == 0 && controller != null)
                animators = new[] { visual.AddComponent<Animator>() };
            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController == null && controller != null)
                    animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
            }

            // Altuera erreala (2m) eta oinak zoruaren gainean (Y=0.5).
            Bounds bounds = VisualBounds(visual);
            if (bounds.size.y < 0.01f)
                throw new InvalidOperationException("Modeloaren altuera ezin izan da neurtu.");
            float scale = 2f / bounds.size.y;
            if (scale < 0.01f || scale > 100f)
                throw new InvalidOperationException("Modeloa eskala arraroarekin: " + scale);
            visual.transform.localScale = Vector3.one * scale;
            bounds = VisualBounds(visual);
            visual.transform.position += new Vector3(
                enemy.transform.position.x - bounds.center.x,
                0.5f - bounds.min.y,
                enemy.transform.position.z - bounds.center.z);
            Debug.Log("HORROR_CREEP: " + visual.name + " pos=" + enemy.transform.position +
                " tamaina=" + VisualBounds(visual).size +
                " Animator=" + animators.Length + " Controller=" + (controller != null));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");

        // Diskotik berrirekita egiaztatu, gorde dela ziurtatzeko.
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int count = 0;
        for (int i = 0; i < 3; i++)
        {
            string enemyName = i == 0 ? "Enemigo" : "Enemigo_Creep" + (i + 1);
            Transform enemyTransform = FindDeep(scene, enemyName);
            if (enemyTransform == null || enemyTransform.GetComponent<Enemigo>() == null ||
                enemyTransform.GetComponent<CapsuleCollider>() == null ||
                enemyTransform.Find("Creep" + (i + 1) + "_Visual") == null)
                throw new InvalidOperationException("Etsaia ez da ondo gorde: " + enemyName);
            SerializedObject sr = new SerializedObject(enemyTransform.GetComponent<Enemigo>());
            var p = sr.FindProperty("waypoints");
            if (p == null || p.arraySize != 5)
                throw new InvalidOperationException("Waypoint errorea: " + enemyName);
            for (int j = 0; j < 5; j++)
                if (p.GetArrayElementAtIndex(j).objectReferenceValue == null)
                    throw new InvalidOperationException("Waypoint hutsa: " + enemyName);
            count++;
        }
        CountGeometry(scene, out walls, out floors);
        if (count != 3 || walls != 46 || floors != 4)
            throw new InvalidOperationException("Amaierako auditoria huts egin du.");
        Debug.Log("HORROR_CREEPS_OK: 3/3 (Creep1, Creep2, Creep3), 5 waypoint bakoitzeko; 46 horma, 4 zoru mantenduta.");
    }

    private static Bounds VisualBounds(GameObject visual)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        bool initialized = false;
        Bounds result = new Bounds();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.bounds.size.sqrMagnitude < 0.0001f) continue;
            if (!initialized) { result = renderer.bounds; initialized = true; }
            else result.Encapsulate(renderer.bounds);
        }
        if (!initialized) throw new InvalidOperationException("Prefabak ez du Renderer egokirik.");
        return result;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    private static Transform FindDeep(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        }
        return null;
    }

    private static void CountGeometry(Scene scene, out int walls, out int floors)
    {
        walls = 0; floors = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = renderer.gameObject.name;
                if (name.StartsWith("Pared", StringComparison.Ordinal)) walls++;
                else if (name.StartsWith("Suelo", StringComparison.Ordinal)) floors++;
            }
    }
}
