using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Efektuak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Mailen argiztapena, hiru prozesu bereizitan (batchmode, Unity itxita): prestatu, bake eta oklusioa.
    /// Argazkiak beste prozesu batean ateratzen dira, Camera.Render-en blokeoa bake-a arriskuan jar ez dezan.
    /// 01_Instalazioa-ren argiztapen-ezarpenak ez dira ukitzen: mailek beren asset-a dute.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string MailaArgiztapena = "Assets/_Eclipse/Datuak/Argiztapena/ECLIPSE_Mailak.lighting";
        private const string BerrizMarka = "-eclipseBerriz";
        private const string ArgiMarka = "ECL_MAILARGIAK";
        private const string PasilloAurrizkia = "Pasilloa_";

        private const StaticEditorFlags OklusioFlagak = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic;

        /// <summary>Geometria eta kolisioak egiaztatu ondoren: flag-ak, probe-ak, argi-moduak eta ezarpenak. Ez du hornitzen.</summary>
        public static void BatchMailaPrestatu()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                GameObject erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
                var informea = new Informea(Tresna, ArgiMarka, $"{d.Izena}: Prestatu", d.Bidea, d.Erroa);

                bool ondo = ArgiztapenPreseta.AurrebistaErroan(erroa) && ArgiztapenPreseta.FlagakErroan(erroa);
                int ezGi = BeirakGarbitu(erroa);
                int oklusioa = OklusioFlagakEzarri(erroa);
                int pasilloak = PasilloakAtlasetikKanpo(erroa);
                ondo = ondo && ProbeJartzailea.AurrebistaErroan(erroa) && ProbeJartzailea.AplikatuErroan(erroa);
                (int hornituak, int denbora) = ArgiModuakEzarri(erroa);
                LightingSettings ezarpenak = MailaEzarpenak();
                Lightmapping.lightingSettings = ezarpenak;
                informea.Lerroa($"- **Hornituko diren argiak:** {hornituak}; **denbora errealekoak:** {denbora}");
                informea.Lerroa($"- **GI-tik kanpo (beira, hesiak):** {ezGi}; **pasilloetako piezak (Light Probe + denbora errealeko argiak):** {pasilloak}; **oklusio-flagak:** {oklusioa}");
                informea.Lerroa($"- **Ezarpenak:** {MailaArgiztapena}: {ezarpenak.lightmapResolution} texel/m, {ezarpenak.lightmapMaxSize}, {ezarpenak.lightmapper}");
                if (!ondo || !EditorSceneManager.SaveScene(eszena))
                {
                    informea.AmaituErrorearekin("Prestaketak huts egin du: ikusi tresnen informeak");
                }
                else
                {
                    AssetDatabase.SaveAssets();
                    informea.AmaituOndo($"{hornituak} argi hornitzeko prest");
                    kodea = 0;
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ArgiMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Bake-a bakarrik: argazkirik gabe. Dagoeneko hornituta badago, ez da errepikatzen (-eclipseBerriz behartzeko).</summary>
        public static void BatchMailaBake()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                if (LightmapSettings.lightmaps.Length > 0 && !Environment.GetCommandLineArgs().Contains(BerrizMarka))
                {
                    Debug.Log($"{ArgiMarka}_OK {d.Izena}: dagoeneko hornituta ({LightmapSettings.lightmaps.Length} lightmap); ez da errepikatu.");
                    EditorApplication.Exit(0);
                    return;
                }

                float hasiera = Time.realtimeSinceStartup;
                Debug.Log($"{ArgiMarka}_BAKE_HASI {d.Izena} {Lightmapping.lightingSettings.lightmapper}");
                bool ondo = Lightmapping.Bake();
                float iraupena = Time.realtimeSinceStartup - hasiera;
                int kopurua = LightmapSettings.lightmaps.Length;
                if (!ondo || kopurua == 0)
                {
                    Debug.LogError($"{ArgiMarka}_ERROR {d.Izena}: bake-ak huts egin du ({kopurua} lightmap, {iraupena:0} s). Eszena ez da gorde.");
                }
                else if (EditorSceneManager.SaveScene(eszena))
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"{ArgiMarka}_OK {d.Izena}: {kopurua} lightmap, {iraupena:0} s.");
                    kodea = 0;
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ArgiMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Oklusio-datuak: pasillo eta horma askoko mailetan ikusten ez dena marraztu gabe uzteko.</summary>
        public static void BatchMailaOklusioa()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                StaticOcclusionCulling.smallestOccluder = 1.5f;
                StaticOcclusionCulling.smallestHole = 0.25f;
                StaticOcclusionCulling.backfaceThreshold = 100f;
                float hasiera = Time.realtimeSinceStartup;
                bool ondo = StaticOcclusionCulling.Compute();
                float iraupena = Time.realtimeSinceStartup - hasiera;
                if (ondo && EditorSceneManager.SaveScene(eszena))
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"{ArgiMarka}_OK {d.Izena}: oklusioa {iraupena:0} s, {StaticOcclusionCulling.umbraDataSize / 1024} KB.");
                    kodea = 0;
                }
                else
                {
                    Debug.LogError($"{ArgiMarka}_ERROR {d.Izena}: oklusioak huts egin du.");
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ArgiMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>
        /// Salak bakarrik hornitzen dira. Probetan (docs/estado.md): salak bakarrik garbi ateratzen dira, eta pasilloak sartzean sabai eta hormetan
        /// adabaki angeluzuzenak agertzen dira. Pasilloek Light Probe-ak eta denbora errealeko argi itzalgabeak erabiltzen dituzte (Forward+).
        /// </summary>
        private static int PasilloakAtlasetikKanpo(GameObject erroa)
        {
            Transform geometria = SemeaBilatu(erroa.transform, Geometria);
            int aldaketak = 0;
            foreach (Transform sala in geometria)
            {
                if (!sala.name.StartsWith(PasilloAurrizkia, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Transform t in sala.GetComponentsInChildren<Transform>(true))
                {
                    StaticEditorFlags flagak = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    if ((flagak & StaticEditorFlags.ContributeGI) != 0)
                    {
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, flagak & ~StaticEditorFlags.ContributeGI);
                        if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                        }

                        aldaketak++;
                    }
                }
            }

            return aldaketak;
        }

        private static bool PasillokoaDa(Transform t)
        {
            if (t.name.StartsWith("Argia_" + PasilloAurrizkia, StringComparison.Ordinal))
            {
                return true;
            }

            for (Transform uneko = t; uneko != null; uneko = uneko.parent)
            {
                if (uneko.name.StartsWith(PasilloAurrizkia, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Oklusorea: horma eta hodi-kasko handiak bakarrik. Pieza finek ez dute ezer ezkutatu behar.</summary>
        private static bool OklusoreHandia(string izena)
        {
            return izena.StartsWith("Wall12__", StringComparison.Ordinal)
                   || izena.StartsWith("WallIntersection__", StringComparison.Ordinal)
                   || izena.StartsWith("TopWall4__", StringComparison.Ordinal)
                   || izena.StartsWith("ProfileEnd__", StringComparison.Ordinal)
                   || izena == "Profile1"
                   || izena == "ProfileDoor";
        }

        /// <summary>Oklusore-flagak mugatu eta datuak berriz kalkulatu. Argiztapen-datuak ez dira ukitzen.</summary>
        public static void BatchOklusioDoitu()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                GameObject erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
                Transform geometria = SemeaBilatu(erroa.transform, Geometria);
                int kenduak = 0;
                foreach (Renderer r in geometria.GetComponentsInChildren<Renderer>(true))
                {
                    StaticEditorFlags flagak = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                    StaticEditorFlags nahi = flagak;
                    if (!OklusoreHandia(r.gameObject.name))
                    {
                        nahi &= ~StaticEditorFlags.OccluderStatic;
                    }

                    if (BetiMarraztu(r.gameObject.name))
                    {
                        nahi &= ~StaticEditorFlags.OccludeeStatic;
                    }

                    if (nahi != flagak)
                    {
                        GameObjectUtility.SetStaticEditorFlags(r.gameObject, nahi);
                        if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                        }

                        kenduak++;
                    }
                }

                StaticOcclusionCulling.smallestOccluder = 1.5f;
                StaticOcclusionCulling.smallestHole = 0.25f;
                StaticOcclusionCulling.backfaceThreshold = 100f;
                bool ondo = StaticOcclusionCulling.Compute();
                if (ondo && EditorSceneManager.SaveScene(eszena))
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"{ArgiMarka}_OK {d.Izena}: {kenduak} pieza aldatuta; lightmap-ak {LightmapSettings.lightmaps.Length}.");
                    kodea = 0;
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ArgiMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Salen egitura merkea (2-188 triangelu): oklusioak ia ez du aurrezten, eta ezkutatuz gero zuloa ikusten da.</summary>
        private static bool BetiMarraztu(string izena)
        {
            return izena.StartsWith(ZoruPropioa + "__", StringComparison.Ordinal)
                   || izena.StartsWith("Wall12__", StringComparison.Ordinal)
                   || izena.StartsWith("TopWall4__", StringComparison.Ordinal);
        }

        /// <summary>Oklusio-datuak kendu: kamera bakoitzak frustum-a bakarrik erabiliko du. Argiztapen-datuak ez dira ukitzen.</summary>
        public static void BatchOklusioKendu()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                StaticOcclusionCulling.Clear();
                if (EditorSceneManager.SaveScene(eszena))
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"{ArgiMarka}_OK {d.Izena}: oklusio-datuak kenduta; lightmap-ak {LightmapSettings.lightmaps.Length}.");
                    kodea = 0;
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ArgiMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Beirak eta hesiak ez dira horniduran sartzen: gardenak dira eta hesia desagertu egiten da.</summary>
        private static int BeirakGarbitu(GameObject erroa)
        {
            int aldaketak = 0;
            foreach (Renderer r in erroa.GetComponentsInChildren<Renderer>(true))
            {
                string izena = r.gameObject.name;
                bool gardena = izena.StartsWith("PF_Beira", StringComparison.Ordinal) || izena == "Hesia";
                if (gardena && GameObjectUtility.GetStaticEditorFlags(r.gameObject) != 0)
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, 0);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                    aldaketak++;
                }
            }

            return aldaketak;
        }

        private static int OklusioFlagakEzarri(GameObject erroa)
        {
            Transform geometria = SemeaBilatu(erroa.transform, Geometria);
            if (geometria == null)
            {
                return 0;
            }

            int aldaketak = 0;
            foreach (Renderer r in geometria.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name.StartsWith("PF_Beira", StringComparison.Ordinal))
                {
                    continue;
                }

                // Pieza finak oklusore izanda, ikusgai dagoen geometria ezkutatzen zen: horma eta kasko handiak bakarrik.
                StaticEditorFlags flagak = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                StaticEditorFlags nahi = (flagak | OklusioFlagak) & (OklusoreHandia(r.gameObject.name) ? ~(StaticEditorFlags)0 : ~StaticEditorFlags.OccluderStatic);
                if (BetiMarraztu(r.gameObject.name))
                {
                    nahi &= ~StaticEditorFlags.OccludeeStatic;
                }

                if (flagak != nahi)
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, nahi);
                    if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                    }

                    aldaketak++;
                }
            }

            return aldaketak;
        }

        /// <summary>Argi estatikoak hornitu (itzalekin, doan baitira); pultsatzen dutenak eta adierazleak denbora errealean, itzalik gabe.</summary>
        private static (int Hornituak, int Denbora) ArgiModuakEzarri(GameObject erroa)
        {
            int hornituak = 0;
            int denbora = 0;
            foreach (Light argia in erroa.GetComponentsInChildren<Light>(true))
            {
                bool denboraErreala = argia.GetComponent<ArgiPultsua>() != null || argia.name == "Adierazlea" || PasillokoaDa(argia.transform);
                LightmapBakeType mota = denboraErreala ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
                LightShadows itzalak = denboraErreala ? LightShadows.None : LightShadows.Soft;
                if (argia.enabled && !denboraErreala)
                {
                    hornituak++;
                }
                else if (argia.enabled)
                {
                    denbora++;
                }

                if (argia.lightmapBakeType != mota || argia.shadows != itzalak)
                {
                    argia.lightmapBakeType = mota;
                    argia.shadows = itzalak;
                    if (PrefabUtility.IsPartOfPrefabInstance(argia))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(argia);
                    }

                    EditorUtility.SetDirty(argia);
                }
            }

            return (hornituak, denbora);
        }

        /// <summary>16 texel/m: maila hauek 01_Instalazioa baino askoz handiagoak dira, eta memoria eta denbora mugatu behar dira.</summary>
        private static LightingSettings MailaEzarpenak()
        {
            var ezarpenak = AssetDatabase.LoadAssetAtPath<LightingSettings>(MailaArgiztapena);
            bool berria = ezarpenak == null;
            ezarpenak ??= new LightingSettings { name = "ECLIPSE_Mailak" };
            ezarpenak.lightmapper = LightingSettings.Lightmapper.UnityComputeGPU;
            ezarpenak.lightmapResolution = 16f;
            ezarpenak.lightmapMaxSize = 2048;
            ezarpenak.lightmapPadding = 4;
            ezarpenak.maxBounces = 2;
            ezarpenak.directSampleCount = 32;
            ezarpenak.indirectSampleCount = 128;
            ezarpenak.environmentSampleCount = 64;
            ezarpenak.mixedBakeMode = MixedLightingMode.IndirectOnly;
            ezarpenak.bakedGI = true;
            ezarpenak.realtimeGI = false;
            if (berria)
            {
                AssetDatabase.CreateAsset(ezarpenak, MailaArgiztapena);
            }
            else
            {
                EditorUtility.SetDirty(ezarpenak);
            }

            return ezarpenak;
        }
    }
}
