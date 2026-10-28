using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>V13ko soinu originalak bi eszenetan instalatu eta egiaztatu.</summary>
public static class HorrorBuilderV13
{
    private const string Menu = "Assets/Laberintoa/Scenes/Hasiera.unity";
    private const string Game = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string AudioFolder = "Assets/Laberintoa/Horror/AudioV13/";

    private static AudioClip Clip(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + name + ".wav");
        if (clip == null) throw new InvalidOperationException("HBB13: falta audio " + name);
        return clip;
    }

    private static void Install(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("HBB13: no se abre " + path);
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<HorrorAudioV13>(true) != null)
                throw new InvalidOperationException("HBB13: audio ya instalado en " + path);
        var go = new GameObject("HB13_Audio");
        var a = go.AddComponent<HorrorAudioV13>();
        var s = new SerializedObject(a);
        string[] fields = { "menuGiroa", "bunkerGiroa", "basoGiroa", "pausoa", "tiroa", "birkarga", "etsaia", "kolpea", "amaiera" };
        string[] clips  = { "menu_ambience", "bunker_ambience", "forest_ambience", "footstep", "shotgun", "reload", "monster", "damage", "ending" };
        for (int i = 0; i < fields.Length; i++)
        {
            var prop = s.FindProperty(fields[i]);
            if (prop == null) throw new InvalidOperationException("HBB13: campo ausente " + fields[i]);
            prop.objectReferenceValue = Clip(clips[i]);
        }
        s.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("HBB13: no se pudo guardar " + path);
        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        bool ok = false;
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<HorrorAudioV13>(true) != null) ok = true;
        if (!ok) throw new InvalidOperationException("HBB13: componente no guardado " + path);
    }

    public static void Apply()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("HBB13: solo modo batch");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("HBB13_PRECHECK_OK: scripts y sonidos importados");
        Install(Menu);
        Install(Game);
        AssetDatabase.SaveAssets();
        Debug.Log("HBB13_OK: ambiente menu, bunker, bosque y siete efectos; escenas guardadas");
    }
}
