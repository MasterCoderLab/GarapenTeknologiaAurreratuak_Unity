using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Horror Builder v8: V6ko hormak leheneratu eta bunkerreko zein basoko argiak egokitu.
/// Jokoaren logika, kamera, fisika, NavMesh eta material-fitxategiak ez dira aldatzen.
/// </summary>
public static class HorrorBuilderV8
{
    private const string ScenePath = "Assets/Laberintoa/Scenes/Laberintoa_Horror.unity";
    private const string V6 = "Assets/Laberintoa/Horror/BuilderV6/Materials/";
    private const string V7 = "Assets/Laberintoa/Horror/BuilderV7/Materials/";
    private static readonly string[] Sizes = { "Laburra", "Ertaina", "Luzea" };
    private static readonly string[] Extra = {
        "HB_Nav_IrteeraEzker", "HB_Nav_IrteeraEskuin",
        "HB4_AtzekoIrteeraItxita_1", "HB4_AtzekoIrteeraItxita_2",
        "HB_Teilatu_1", "HB_Teilatu_2", "HB_Teilatu_3", "HB_Teilatu_4"
    };

    public static void Apply()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("V8 batch moduan bakarrik exekutatu.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check(scene, false);
        Debug.Log("HBB8_PRECHECK_OK: V7ko kamera eta 53 hormigoi-gainazalak, 4 argi gorri, irteera, ilargia eta 2 basoko argiak egiaztatuta.");

        // V7ko materialak EZ ezabatu eta V6ko .mat fitxategiak EZ aldatu.
        var map = new Dictionary<Material, Material>();
        foreach (string size in Sizes)
        {
            Material source = Load(V7, "HB7_Horma" + size);
            Material target = Load(V6, "HB6_Horma" + size);
            map.Add(source, target);
        }

        int walls = 0;
        var seen = new HashSet<Renderer>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider wall in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!wall.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer == null || !seen.Add(renderer)) continue;
            if (!map.TryGetValue(renderer.sharedMaterial, out Material restored))
                throw new InvalidOperationException("Horma ustekabeko materialarekin: " + wall.name);
            renderer.sharedMaterial = restored;
            walls++;
        }
        if (walls != 45)
            throw new InvalidOperationException("45 horma espero dira: " + walls);

        foreach (string name in Extra)
        {
            GameObject item = Find(scene, name);
            Renderer renderer = item == null ? null : item.GetComponent<Renderer>();
            if (renderer == null || !map.TryGetValue(renderer.sharedMaterial, out Material restored))
                throw new InvalidOperationException("V7 hormigoi-materiala falta da: " + name);
            renderer.sharedMaterial = restored;
        }

        // Bunkerra: lau larrialdi-argi gorriak leundu, eta irteera seinalea mantendu.
        int emergency = 0, exit = 0, moonCount = 0, forest = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.name.StartsWith("HB_ArgiLarrialdia_", StringComparison.Ordinal))
            {
                light.intensity = 0.27f; // V7/V6: 0.60
                emergency++;
            }
            else if (light.name == "HB_IrteeraArgiGorria")
            {
                light.intensity = 1.25f; // V7/V6: 1.80
                exit++;
            }
            else if (light.name == "Directional Light")
            {
                light.intensity = 0.29f; // V7/V6: 0.13
                // Kolore urdinxka eta itzal leunak bere horretan.
                moonCount++;
            }
            else if (light.name == "HB2_IlargiIspilua")
            {
                light.intensity = 0.58f; // V7/V6: 0.35
                light.range = 16f;        // V7/V6: 12
                forest++;
            }
        }
        if (emergency != 4 || exit != 1 || moonCount != 1 || forest != 2)
            throw new InvalidOperationException("Argi kopuru ustekabea: " + emergency + "/" + exit + "/" + moonCount + "/" + forest);

        // Giro-argi orokorra eta lainoa EZ aldatu: kanpoko argia ez dadila
        // bunkerrean giro-argi artifizial bihurtu.
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Horror eszena ezin izan da gorde.");
        AssetDatabase.SaveAssets();
        scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Check(scene, true);
        Debug.Log("HBB8_OK: V6ko 45 horma + 8 estalki/tramo; bunker ilunagoa (4+1 argi); kanpoan ilargi argi leuna (1+2 argi); V7ko kamera, jokagarritasuna eta NavMesh ukitu gabe.");
    }

    private static Material Load(string folder, string name)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(folder + name + ".mat");
        if (mat == null)
            throw new InvalidOperationException("Materiala falta da: " + folder + name);
        return mat;
    }

    private static void Check(Scene scene, bool installed)
    {
        if (Find(scene, "HB7_LehenPlanokoKamera") == null ||
            Find(scene, "Main Camera")?.GetComponent<HorrorViewmodelSafety>() == null ||
            Find(scene, "Horror_NavMesh") == null ||
            Find(scene, "HB3_EscopetaEnMano") == null ||
            Find(scene, "HB4_LinternaIkusgai") == null)
            throw new InvalidOperationException("V7 eszenaren egitura edo jokoaren elementuak falta dira.");

        int walls = 0;
        var checkedRenderers = new HashSet<Renderer>();
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.name.StartsWith("Pared", StringComparison.Ordinal)) continue;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer == null || !checkedRenderers.Add(renderer)) continue;
            string asset = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            bool valid = false;
            foreach (string size in Sizes)
                if (asset == (installed ? V6 + "HB6_Horma" : V7 + "HB7_Horma") + size + ".mat")
                    valid = true;
            if (!valid) throw new InvalidOperationException("Horma desberdina: " + box.name + " => " + asset);
            walls++;
        }
        if (walls != 45)
            throw new InvalidOperationException("Horma kopuru okerra: " + walls);
        foreach (string name in Extra)
        {
            GameObject item = Find(scene, name);
            Renderer renderer = item == null ? null : item.GetComponent<Renderer>();
            string asset = renderer == null ? "" : AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            bool valid = false;
            foreach (string size in Sizes)
                if (asset == (installed ? V6 + "HB6_Horma" : V7 + "HB7_Horma") + size + ".mat")
                    valid = true;
            if (!valid) throw new InvalidOperationException("Tramoa/teilatua desberdina: " + name);
        }
        foreach (string size in Sizes)
        {
            Load(V6, "HB6_Horma" + size);
            Load(V7, "HB7_Horma" + size);
        }

        int red = 0, exit = 0, moon = 0, pools = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.name.StartsWith("HB_ArgiLarrialdia_", StringComparison.Ordinal))
            {
                if (light.type != LightType.Point || !Approx(light.intensity, installed ? .27f : .60f))
                    throw new InvalidOperationException("Larrialdi-argiaren balio ustekabea: " + light.name);
                red++;
            }
            else if (light.name == "HB_IrteeraArgiGorria")
            {
                if (light.type != LightType.Point || !Approx(light.intensity, installed ? 1.25f : 1.8f))
                    throw new InvalidOperationException("Irteerako argi desberdina.");
                exit++;
            }
            else if (light.name == "Directional Light")
            {
                if (light.type != LightType.Directional || !Approx(light.intensity, installed ? .29f : .13f))
                    throw new InvalidOperationException("Ilargiaren argi desberdina.");
                moon++;
            }
            else if (light.name == "HB2_IlargiIspilua")
            {
                if (light.type != LightType.Point ||
                    !Approx(light.intensity, installed ? .58f : .35f) ||
                    !Approx(light.range, installed ? 16f : 12f))
                    throw new InvalidOperationException("Basoko argi desberdina.");
                pools++;
            }
        }
        if (red != 4 || exit != 1 || moon != 1 || pools != 2)
            throw new InvalidOperationException("Eszenako argi kopuru okerra: " + red + "/" + exit + "/" + moon + "/" + pools);
    }

    private static bool Approx(float a, float b) { return Mathf.Abs(a - b) < .012f; }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item.gameObject;
        }
        return null;
    }
}
