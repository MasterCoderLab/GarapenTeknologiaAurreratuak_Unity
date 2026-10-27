using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror eszenan NavMeshAgent-ak itzalita gordetzen ditu,
/// runtimeko kargatzaileak datuak kargatu eta gero aktiba ditzan.
/// SampleScene eta NavMesh asseta ez ditu ukitzen.
/// </summary>
public static class HorrorNavMeshStartFix
{
    private const string HorrorScene = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string NavAsset = "Assets/Laberintoa/Horror/NavMesh_Horror_BuilderV1.asset";

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode bakarrik.");
        Scene scene = EditorSceneManager.OpenScene(HorrorScene, OpenSceneMode.Single);
        GameObject holder = Find(scene, "Horror_NavMesh");
        HorrorNavMeshRuntime runtime = holder == null ? null : holder.GetComponent<HorrorNavMeshRuntime>();
        if (runtime == null || runtime.navMeshData == null ||
            AssetDatabase.GetAssetPath(runtime.navMeshData) != NavAsset)
            throw new InvalidOperationException("NavMesh originalaren erreferentzia ez da egokia.");
        GameObject root = Find(scene, "HB_MunduOsoa_v2");
        if (root == null || Find(scene, "HB_MunduOsoa_v1") == null)
            throw new InvalidOperationException("Horror v1/v2 eszenaren egitura falta da.");
        if (runtime.delayedAgents != null && runtime.delayedAgents.Length != 0)
            throw new InvalidOperationException("NavMesh fix-a badago lehendik; ez errepikatu.");
        string[] names = { "Enemigo", "Enemigo_Creep2", "Enemigo_Creep3" };
        var agents = new NavMeshAgent[3];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject monster = Find(scene, names[i]);
            if (monster == null || monster.GetComponent<HorrorEnemyAI>() == null ||
                monster.GetComponent<HorrorEnemyAnimation>() == null)
                throw new InvalidOperationException("Etsaia / IA falta da: " + names[i]);
            agents[i] = monster.GetComponent<NavMeshAgent>();
            if (agents[i] == null || !agents[i].enabled)
                throw new InvalidOperationException("NavMeshAgent egoera ustekabea: " + names[i]);
        }
        Debug.Log("HBNF_PRECHECK_OK: 3 enabled agents, v2 roots, old NavMesh reference OK.");
        foreach (NavMeshAgent agent in agents)
        {
            agent.enabled = false;
            EditorUtility.SetDirty(agent);
        }
        runtime.delayedAgents = agents;
        EditorUtility.SetDirty(runtime);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");
        scene = EditorSceneManager.OpenScene(HorrorScene, OpenSceneMode.Single);
        runtime = Find(scene, "Horror_NavMesh").GetComponent<HorrorNavMeshRuntime>();
        if (runtime.delayedAgents == null || runtime.delayedAgents.Length != 3 ||
            AssetDatabase.GetAssetPath(runtime.navMeshData) != NavAsset)
            throw new InvalidOperationException("Gordetako NavMesh loader baliogabea.");
        for (int i = 0; i < names.Length; i++)
        {
            NavMeshAgent a = Find(scene, names[i]).GetComponent<NavMeshAgent>();
            if (a.enabled || runtime.delayedAgents[i] != a)
                throw new InvalidOperationException("Agente / erreferentzia baliogabea: " + names[i]);
        }
        Debug.Log("HBNF_OK: 3 agents disabled in scene, serialized references valid, loader enables them after NavMesh load.");
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
