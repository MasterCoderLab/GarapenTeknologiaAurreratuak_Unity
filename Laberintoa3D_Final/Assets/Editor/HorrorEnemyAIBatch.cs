using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Horma/zoruko talkagailuekin NavMesh eraiki eta 3 etsaiei IA ematen die.
/// Jatorrizko tutoriala ez du aldatzen; jokalaria sektore seguruan jartzen du.
/// </summary>
public static class HorrorEnemyAIBatch
{
    const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    const string MeshPath = "Assets/Laberintoa/Horror/NavMesh_Horror.asset";

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch modua beharrezkoa da.");
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(MeshPath) != null)
            throw new InvalidOperationException("NavMesh jada existitzen da. Ez da gainidazten.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = Find(scene, "Personaje");
        GameObject group = Find(scene, "Horror_Enemigos_X3");
        GameObject[] enemies = {
            Find(scene, "Enemigo"), Find(scene, "Enemigo_Creep2"), Find(scene, "Enemigo_Creep3")
        };
        if (player == null || group == null || Find(scene, "Horror_NavMesh") != null ||
            Find(scene, "Horror_Sektoreak_X4") == null)
            throw new InvalidOperationException("Ez da espero zen Horror eszena.");
        foreach (GameObject enemy in enemies)
            if (enemy == null || enemy.GetComponent<Enemigo>() == null ||
                enemy.GetComponent<CapsuleCollider>() == null ||
                enemy.transform.Find(enemy == enemies[0] ? "Creep1_Visual" :
                    enemy == enemies[1] ? "Creep2_Visual" : "Creep3_Visual") == null ||
                enemy.GetComponent<HorrorEnemyAI>() != null)
                throw new InvalidOperationException("Etsaiaren egitura ez da zuzena.");

        var sources = new List<NavMeshBuildSource>();
        int walls = 0, floors = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (BoxCollider collider in root.GetComponentsInChildren<BoxCollider>(true))
            {
                string name = collider.gameObject.name;
                bool wall = name.StartsWith("Pared", StringComparison.Ordinal);
                bool floor = name.StartsWith("Suelo", StringComparison.Ordinal);
                if (!wall && !floor) continue;
                if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Talkagailu itzalia: " + name);
                if (wall) walls++; else floors++;
                sources.Add(new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = collider.transform.localToWorldMatrix * Matrix4x4.Translate(collider.center),
                    size = collider.size,
                    area = 0
                });
            }
        if (walls != 46 || floors != 4 || sources.Count != 50)
            throw new InvalidOperationException("Geometria aldatuta: " + walls + "/46 horma; " + floors + "/4 zoru.");

        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = 0.28f;
        settings.agentHeight = 1.8f;
        settings.agentClimb = 0.35f;
        settings.agentSlope = 40f;
        settings.minRegionArea = 0.1f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.075f;
        var bounds = new Bounds(new Vector3(10f, 1f, 7.5f), new Vector3(55f, 12f, 45f));
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds,
            Vector3.zero, Quaternion.identity);
        if (data == null) throw new InvalidOperationException("NavMesh eraikuntzak huts egin du.");

        NavMeshDataInstance temporary = NavMesh.AddNavMeshData(data);
        if (!temporary.valid) throw new InvalidOperationException("NavMesh ezin izan da aktibatu.");

        Vector3[] spawnPositions = new Vector3[3];
        Vector3 playerPosition;
        try
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 oldPos = enemies[i].transform.position;
                Vector3 atFloor = new Vector3(oldPos.x, 0.6f, oldPos.z);
                if (!NavMesh.SamplePosition(atFloor, out NavMeshHit hit, 0.8f, NavMesh.AllAreas))
                    throw new InvalidOperationException("NavMesh faltan etsaian: " + enemies[i].name);
                spawnPositions[i] = hit.position;
            }
            // Sektore hutsaren barneko leku seguruenetik hasi, etsaietatik urrun.
            float bestDistance = -1f;
            playerPosition = Vector3.zero;
            for (float x = 12f; x <= 28f; x += 2f)
                for (float z = 9f; z <= 21f; z += 2f)
                {
                    if (!NavMesh.SamplePosition(new Vector3(x, 0.6f, z), out NavMeshHit hit,
                        0.7f, NavMesh.AllAreas)) continue;
                    if (Mathf.Abs(hit.position.x - x) > 0.5f ||
                        Mathf.Abs(hit.position.z - z) > 0.5f || hit.position.y > 1f) continue;
                    float nearest = float.PositiveInfinity;
                    bool allReachable = true;
                    for (int i = 0; i < 3; i++)
                    {
                        nearest = Mathf.Min(nearest, Vector3.Distance(spawnPositions[i], hit.position));
                        var path = new NavMeshPath();
                        if (!NavMesh.CalculatePath(spawnPositions[i], hit.position, NavMesh.AllAreas, path) ||
                            path.status != NavMeshPathStatus.PathComplete)
                        { allReachable = false; break; }
                    }
                    if (allReachable && nearest > bestDistance)
                    { bestDistance = nearest; playerPosition = hit.position; }
                }
            if (bestDistance < 10f)
                throw new InvalidOperationException("Ez dago jokalariaren hasiera segururik, edo sektoreak deskonektatuta daude.");
            Debug.Log("HORROR_AI_AUDIT: NavMesh OK; hasierako distantzia minimoa=" + bestDistance);
        }
        finally { temporary.Remove(); }

        // Hemendik aurrera bakarrik aldatzen da eszena; aldez aurreko ibilbideak ondo daude.
        AssetDatabase.CreateAsset(data, MeshPath);
        var navRoot = new GameObject("Horror_NavMesh");
        var navRuntime = navRoot.AddComponent<HorrorNavMeshRuntime>();
        navRuntime.navMeshData = data;
        player.transform.position = new Vector3(playerPosition.x, player.transform.position.y, playerPosition.z);

        Vector3[] patrolCenters = {
            new Vector3(0f, 0f, 0f), new Vector3(20f, 0f, 0f), new Vector3(0f, 0f, 15f)
        };
        for (int i = 0; i < 3; i++)
        {
            GameObject enemy = enemies[i];
            Vector3 oldPos = enemy.transform.position;
            enemy.GetComponent<Enemigo>().enabled = false; // Waypointak gordeta, script zaharra itzalita.
            enemy.transform.position = spawnPositions[i];
            foreach (Transform child in enemy.transform)
                if (child.name == "Creep" + (i + 1) + "_Visual")
                    child.localPosition += new Vector3(0f, oldPos.y - spawnPositions[i].y, 0f);
            var capsule = enemy.GetComponent<CapsuleCollider>();
            capsule.center += new Vector3(0f, oldPos.y - spawnPositions[i].y, 0f);

            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.agentTypeID = settings.agentTypeID;
            agent.radius = 0.28f;
            agent.height = 1.8f;
            agent.speed = 3.2f;
            agent.acceleration = 7f;
            agent.angularSpeed = 420f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = true;
            agent.baseOffset = 0f;

            var ai = enemy.AddComponent<HorrorEnemyAI>();
            ai.player = player.transform;
            ai.sectorCenter = patrolCenters[i];
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();

        // Diskotik ireki eta egiaztatu.
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int agents = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (HorrorEnemyAI ai in root.GetComponentsInChildren<HorrorEnemyAI>(true))
            {
                if (ai.player == null || ai.GetComponent<NavMeshAgent>() == null ||
                    ai.GetComponent<Enemigo>() == null || ai.GetComponent<Enemigo>().enabled)
                    throw new InvalidOperationException("Etsaiaren IA ez da ondo gorde.");
                agents++;
            }
        var loader = Find(scene, "Horror_NavMesh");
        if (agents != 3 || loader == null ||
            loader.GetComponent<HorrorNavMeshRuntime>()?.navMeshData == null)
            throw new InvalidOperationException("Amaierako auditoria huts egin du.");
        Debug.Log("HORROR_AI_OK: 3/3 NavMeshAgent + patrulla aleatoria + persecucion; jugador en cuarto sector; 46 paredes y 4 suelos intactos.");
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
        }
        return null;
    }
}
