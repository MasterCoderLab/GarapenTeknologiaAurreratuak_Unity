using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Argiztapen-prozesu osoa batchmode-n (Unity itxita bakarrik):
    /// aset propioak eta argiak → flag estatikoak → UV2 (baimenarekin) → probe-ak → bake → gorde.
    /// Urrats bakoitzak bere aurrebista eta informea ditu. Errorea badago, eszena ez da gordetzen.
    /// </summary>
    public static class ArgiztapenProzesua
    {
        private const string Marka = "ECL_ARGIZTAPENPROZESUA";
        private const string EszenaBidea = "Assets/_Eclipse/Eszenak/01_Instalazioa.unity";

        /// <summary>UV2-k ThirdParty-ko .meta aldatzen ditu: Ion-en baimena komando-lerroan esplizitua izan behar da.</summary>
        private const string UV2Baimena = "-eclipseUV2Baimena";
        private const string BakeGabe = "-eclipseBakeGabe";

        /// <summary>Irakurtzeko: auditoria eta UV2 behar duten modeloak. Ez du eszena gordetzen.</summary>
        public static void BatchNeurketa()
        {
            int kodea = 1;
            try
            {
                Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                bool ondo = GeometriaAuditoria.Exekutatu();
                foreach (GameObject erroa in Sektoreak(eszena))
                {
                    ondo &= ArgiztapenPreseta.AurrebistaErroan(erroa);
                }

                kodea = ondo ? 0 : 1;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        public static void BatchOsoa()
        {
            int kodea = 1;
            try
            {
                kodea = Osoa() ? 0 : 1;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        private static bool Osoa()
        {
            string[] argumentuak = Environment.GetCommandLineArgs();
            bool uv2Baimena = argumentuak.Contains(UV2Baimena);
            bool bakeGabe = argumentuak.Contains(BakeGabe);
            Debug.Log($"{Marka}_HASI UV2 baimena: {uv2Baimena}; bake: {!bakeGabe}");

            Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);

            if (!EszenaPropioa.Exekutatu(false) || !EszenaPropioa.Exekutatu(true))
            {
                return Hutsa("EszenaPropioa");
            }

            foreach (GameObject erroa in Sektoreak(eszena))
            {
                if (!ArgiztapenPreseta.AurrebistaErroan(erroa) || !ArgiztapenPreseta.FlagakErroan(erroa))
                {
                    return Hutsa($"ArgiztapenPreseta {erroa.name}");
                }

                if (uv2Baimena && !ArgiztapenPreseta.UV2Erroan(erroa, true))
                {
                    return Hutsa($"UV2 {erroa.name}");
                }

                if (!ProbeJartzailea.AurrebistaErroan(erroa) || !ProbeJartzailea.AplikatuErroan(erroa))
                {
                    return Hutsa($"ProbeJartzailea {erroa.name}");
                }
            }

            if (!EditorSceneManager.SaveScene(eszena))
            {
                return Hutsa("eszena gordetzea");
            }

            AssetDatabase.SaveAssets();

            // Bake aurreko argazkiak: lightmap gabe, konparatzeko.
            EbidentziaKapturak.ArgiztapenArgazkiak("E1_bake_gabe", eszena);

            if (bakeGabe)
            {
                Debug.Log($"{Marka}_OK Bake gabe amaituta.");
                return true;
            }

            float hasiera = Time.realtimeSinceStartup;
            Debug.Log($"{Marka}_BAKE_HASI {Lightmapping.lightingSettings.lightmapper}");
            if (!Lightmapping.Bake())
            {
                return Hutsa("Lightmapping.Bake");
            }

            float iraupena = Time.realtimeSinceStartup - hasiera;
            int kopurua = LightmapSettings.lightmaps.Length;
            if (kopurua == 0)
            {
                return Hutsa("bake-ak ez du lightmap-ik sortu");
            }

            if (!EditorSceneManager.SaveScene(eszena))
            {
                return Hutsa("eszena gordetzea bake ondoren");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"{Marka}_OK Bake: {kopurua} lightmap, {iraupena:0} s.");
            return true;
        }

        private static GameObject[] Sektoreak(Scene eszena)
        {
            return eszena.GetRootGameObjects()
                .Where(o => o.name.StartsWith("Sektorea", StringComparison.Ordinal))
                .OrderBy(o => o.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool Hutsa(string urratsa)
        {
            Debug.LogError($"{Marka}_ERROR Urrats honetan gelditu da: {urratsa}. Eszena ez da gorde urrats honetatik aurrera.");
            return false;
        }
    }
}
