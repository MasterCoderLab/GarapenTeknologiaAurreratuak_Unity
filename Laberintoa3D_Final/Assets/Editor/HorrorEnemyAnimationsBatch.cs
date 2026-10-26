using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

/// <summary>
/// Creep paketeko klipak berrerabiliz, IA animazioen kontrolatzaile sinplea sortzen du.
/// Ez du jatorrizko animazio-kontrolatzailea edo prefabrikatuak aldatzen.
/// </summary>
public static class HorrorEnemyAnimationsBatch
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string Original = "Assets/Creep Horror Creature/DemoSceneAssets/DemoAnimCont.controller";
    private const string Folder = "Assets/Laberintoa/Horror/Animations";
    private const string ControllerPath = Folder + "/HorrorCreepAI.controller";
    private static readonly string[] Enemies = { "Enemigo", "Enemigo_Creep2", "Enemigo_Creep3" };
    private static readonly string[] Visuals = { "Creep1_Visual", "Creep2_Visual", "Creep3_Visual" };
    private static readonly string[] SourceNames = {
        "Creep|Idle1_Action", "Creep|Walk1_Action", "Creep|Walk2_Action", "Creep|Punch_Action"
    };
    private static readonly string[] TargetNames = { "Idle", "Patrol", "Chase", "Attack" };

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode beharrezkoa.");
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            throw new InvalidOperationException("Animazio-kontrolatzailea badago; ez da gainidazten.");

        AnimatorController original = AssetDatabase.LoadAssetAtPath<AnimatorController>(Original);
        if (original == null || original.layers.Length == 0)
            throw new InvalidOperationException("DemoAnimCont.controller ez da aurkitu.");
        var motions = new Dictionary<string, Motion>();
        Collect(original.layers[0].stateMachine, motions);
        for (int i = 0; i < SourceNames.Length; i++)
        {
            if (!motions.ContainsKey(SourceNames[i]) || motions[SourceNames[i]] == null)
                throw new InvalidOperationException("Animazio-klipa falta da: " + SourceNames[i]);
            Debug.Log("HORROR_ANIM_CLIP: " + SourceNames[i] + " -> " + motions[SourceNames[i]].name);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] enemies = new GameObject[3];
        Animator[][] animators = new Animator[3][];
        int walls = 0, floors = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MeshRenderer rend in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (rend.gameObject.name.StartsWith("Pared", StringComparison.Ordinal)) walls++;
                else if (rend.gameObject.name.StartsWith("Suelo", StringComparison.Ordinal)) floors++;
            }
        if (walls != 46 || floors != 4)
            throw new InvalidOperationException("Geometria ez da espero zena: " + walls + " / " + floors);
        for (int i = 0; i < 3; i++)
        {
            enemies[i] = Find(scene, Enemies[i]);
            if (enemies[i] == null) throw new InvalidOperationException("Etsaia falta: " + Enemies[i]);
            GameObject enemy = enemies[i];
            Transform visual = enemy.transform.Find(Visuals[i]);
            if (visual == null || enemy.GetComponent<HorrorEnemyAI>() == null ||
                enemy.GetComponent<NavMeshAgent>() == null || enemy.GetComponent<HorrorEnemyAnimation>() != null)
                throw new InvalidOperationException("Etsaiaren egitura ez da espero zena: " + enemy.name);
            animators[i] = visual.GetComponentsInChildren<Animator>(true);
            if (animators[i].Length == 0)
                throw new InvalidOperationException("Animatzaileak falta dira: " + enemy.name);
        }

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Laberintoa/Horror", "Animations");
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState[] states = new AnimatorState[4];
        for (int i = 0; i < 4; i++)
        {
            states[i] = sm.AddState(TargetNames[i]);
            states[i].motion = motions[SourceNames[i]];
            states[i].writeDefaultValues = true;
        }
        sm.defaultState = states[0];
        EditorUtility.SetDirty(ctrl);

        for (int i = 0; i < 3; i++)
        {
            var component = enemies[i].AddComponent<HorrorEnemyAnimation>();
            component.animators = animators[i];
            var attackClip = motions[SourceNames[3]] as AnimationClip;
            component.attackDuration = attackClip == null ? 0.9f : Mathf.Clamp(attackClip.length, 0.5f, 1.5f);
            foreach (Animator animator in animators[i])
            {
                animator.runtimeAnimatorController = ctrl;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                EditorUtility.SetDirty(animator);
            }
            Debug.Log("HORROR_ANIM_ASSIGN: " + enemies[i].name + " (" + animators[i].Length + " LOD Animator)");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int verified = 0;
        foreach (string name in Enemies)
        {
            var enemy = Find(scene, name);
            var comp = enemy == null ? null : enemy.GetComponent<HorrorEnemyAnimation>();
            if (comp == null || comp.animators == null || comp.animators.Length == 0)
                throw new InvalidOperationException("Animazioak ez dira gorde: " + name);
            foreach (Animator animator in comp.animators)
                if (animator == null || animator.runtimeAnimatorController == null ||
                    AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != ControllerPath)
                    throw new InvalidOperationException("Animazio-kontrolatzailea ez da gorde: " + name);
            verified++;
        }
        Debug.Log("HORROR_ANIM_OK: 3/3 enemigos; Idle, Patrol, Chase, Attack; geometria 46+4 intacta.");
    }

    private static void Collect(AnimatorStateMachine machine, Dictionary<string, Motion> motions)
    {
        foreach (ChildAnimatorState item in machine.states)
            if (item.state != null && item.state.motion != null)
                motions[item.state.name] = item.state.motion;
        foreach (ChildAnimatorStateMachine child in machine.stateMachines)
            if (child.stateMachine != null) Collect(child.stateMachine, motions);
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
