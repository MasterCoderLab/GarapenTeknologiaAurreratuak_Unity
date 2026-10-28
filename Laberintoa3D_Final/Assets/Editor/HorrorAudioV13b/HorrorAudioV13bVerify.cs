using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>V13b: bi eszenen eta benetako bederatzi audioen auditoria.</summary>
public static class HorrorAudioV13bVerify
{
    public static void Verify()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("V13b: batch modua behar da.");
        string folder = "Assets/Laberintoa/Horror/AudioV13/";
        string[] names = { "menu_ambience", "bunker_ambience", "forest_ambience", "footstep",
            "shotgun", "reload", "monster", "damage", "ending" };
        foreach (string name in names)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(folder + name + ".wav");
            if (clip == null || clip.length < .08f) throw new InvalidOperationException("V13b: audio falta da: " + name);
        }
        foreach (string path in new[] { "Assets/Laberintoa/Scenes/Hasiera.unity",
                     "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity" })
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("V13b: eszena falta da " + path);
            HorrorAudioV13 sound = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                HorrorAudioV13 found = root.GetComponentInChildren<HorrorAudioV13>(true);
                if (found != null) { sound = found; break; }
            }
            if (sound == null || sound.menuGiroa == null || sound.bunkerGiroa == null || sound.basoGiroa == null)
                throw new InvalidOperationException("V13b: audio erreferentziak falta dira " + path);
            Debug.Log("HBB13B_SCENE_OK: " + path);
        }
        if (Resources.LoadAll<AudioClip>("HorrorAudioV13b").Length < 8)
            throw new InvalidOperationException("V13b: aldakuntza-audioak falta dira.");
        Debug.Log("HBB13B_OK: 9 audio nagusi eta urrats/marru aldakorrak; eszenak ukitu gabe.");
    }
}
