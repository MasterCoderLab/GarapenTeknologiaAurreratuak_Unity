using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.Atrezzo
{
    /// <summary>
    /// Sektoreen identitate-demoa (A3, C blokea): EDITOREAN BAKARRIK eta Ion-ek aktibatzen duenean (ECLIPSE/Atrezzo/Demoa) edo
    /// -eclipseAtrezzoDemoa argumentuarekin (probak: robota, kapturak). Resources/Atrezzo/AD_&lt;eszena&gt;-ko piezak runtime-n jartzen ditu
    /// "Atrezzo_Demoa" erroan (dinamikoak, GI-rik gabe). Eszenak, argiztapena eta lightmap-ak ez dira aldatzen. Build-etan ez du ezer egiten.
    /// </summary>
    public static class AtrezzoKargatzailea
    {
        public const string EditorGakoa = "ECLIPSE_AtrezzoDemoa";
        public const string ErroIzena = "Atrezzo_Demoa";
        private const string Marka = "ECL_ATREZZODEMOA";

        public static bool Aktibo
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(EditorGakoa, false)
                       || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-eclipseAtrezzoDemoa") >= 0;
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Erregistratu()
        {
            SceneManager.sceneLoaded -= Kargatuta;
            SceneManager.sceneLoaded += Kargatuta;
            Kargatuta(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Kargatuta(Scene eszena, LoadSceneMode modua)
        {
            if (!Aktibo)
            {
                return;
            }

            Jarri(eszena);
        }

        /// <summary>Eszena batean piezak jarri (behin bakarrik). Editoreko tresnek ere erabiltzen dute (aurretik/ondoren kapturak).</summary>
        public static GameObject Jarri(Scene eszena)
        {
            foreach (GameObject erroa in eszena.GetRootGameObjects())
            {
                if (erroa.name == ErroIzena)
                {
                    return erroa;
                }
            }

            var datuak = Resources.Load<AtrezzoDatuak>($"Atrezzo/AD_{eszena.name}");
            if (datuak == null)
            {
                return null;
            }

            var gurasoa = new GameObject(ErroIzena);
            SceneManager.MoveGameObjectToScene(gurasoa, eszena);
            var aurrefabak = new Dictionary<string, GameObject>();
            int jarriak = 0;
            foreach (AtrezzoDatuak.Elementua e in datuak.Elementuak)
            {
                if (!aurrefabak.TryGetValue(e.Pieza, out GameObject aurrefab))
                {
                    aurrefab = Resources.Load<GameObject>($"Atrezzo/Piezak/{e.Pieza}");
                    aurrefabak[e.Pieza] = aurrefab;
                }

                if (aurrefab == null)
                {
                    Debug.LogWarning($"{Marka}_ERROR Pieza falta da: {e.Pieza}");
                    continue;
                }

                GameObject o = Object.Instantiate(aurrefab, e.Lekua, Quaternion.Euler(0f, e.Yaw, 0f), gurasoa.transform);
                o.name = $"{e.Pieza}_{e.Gela}";
                jarriak++;
            }

            Debug.Log($"{Marka}_OK {eszena.name}: {jarriak} pieza");
            return gurasoa;
        }
    }
}
