using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// 01_Instalazioa eszena eraikitzen du: sektore-erroak, Gela_* bolumenak eta kit-eko moduluak.
    /// Pieza bakoitza Unity barruan neurtzen da (Renderer.bounds) eta neurri horiekin kokatzen da.
    /// Sortutako piezek IZENA__000 izena dute: izen hori ez duten objektuak ez dira inoiz ukitzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string Tresna = "EszenaEraikitzailea";
        private const string Marka = "ECL_ESZENAERAIKITZAILEA";
        private const string Menua = "ECLIPSE/Eszena/";
        private const string EszenaBidea = "Assets/_Eclipse/Eszenak/01_Instalazioa.unity";

        private const string MagixBoxKarpeta = "Assets/ThirdParty/Sci Fi Modular Pack/Prefabs";
        private const string RustyKarpeta = "Assets/ThirdParty/Data/Sci-fi_assets/Sci-Fi Old rusty props PBR/Prefabs";

        private const string Zorua = "Floor1";
        private const string Horma = "Wall12";
        private const string Sabaia = "TopWall4";
        private const string Sekzioa = "Profile1";
        private const string SekzioAtea = "ProfileDoor";
        private const string SekzioMuturra = "ProfileEnd";
        private const string Markoa = "WallIntersection";

        /// <summary>
        /// Egitura-moduluen eskala uniformea. Kit-a 1:1 eskalan: atea 1,85 m eta korridorea 2,56 m (kapsula
        /// 1,8 m). 1,25-ekin: atea 2,31 m eta korridorea 3,2 m, sorbalda-kamerarako. Aldatzeko, hau bakarrik.
        /// </summary>
        private const float ModuluEskala = 1.25f;

        /// <summary>Wall12: aurpegi landua +X da (kapturan egiaztatuta); bizkarra, leuna.</summary>
        private const bool HormaBarruaPositiboa = true;

        private const int HormaIlarak = 2;
        private const float KolisioLodiera = 0.2f;
        private const float AteGuneSakonera = 2f;

        private const string Geometria = "Geometria";
        private const string Zoruak = "Zorua";
        private const string Hormak = "Hormak";
        private const string Sabaiak = "Sabaia";
        private const string Propak = "Propak";
        private const string Hodiak = "Hodia";
        private const string Kolisioak = "Kolisioak";
        private const string KolisioIzena = "Kolisioa";
        private const string AteOrria = "Door";

        private const float Tolerantzia = 0.001f;
        private const float EgiaztapenTolerantzia = 0.02f;
        private const float EskalaAbisua = 0.25f;
        private const float SektoreErdiZ = 29f;
        private const float PropMarjina = 0.1f;

        private static bool _eskalarekin;

        private static readonly Regex PiezaIzena = new Regex(@"__\d{3}$");

        // Sala: lauzak X ardatzean eta horma-panelak Z ardatzean (bakoitia: atea erdian geratzeko).
        // Null: sala horren ordez, korridorea ProfileEnd batekin ixten da.
        private static readonly SektoreDef[] Sektoreak =
        {
            new SektoreDef(1, new Vector3(0f, 0f, 0f), new SalaDef("Sarrera", 5, 3), 3, new SalaDef("Laborategia", 8, 5)),
            new SektoreDef(2, new Vector3(0f, 0f, 60f), new SalaDef("Fusibleak", 4, 3), 2, new SalaDef("Sorgailua", 8, 5)),
            new SektoreDef(3, new Vector3(0f, 0f, 120f), null, 3, new SalaDef("Erreaktorea", 10, 7)),
        };

        // u eta v: -1etik 1era sala barruan; ±1 denean, propa hormaren kontra geratzen da.
        private static readonly PropDef[] Sektorea1Propak =
        {
            new PropDef("Sarrera", RustyKarpeta, "Container_Big", -1f, 1f, 0f, false),
            new PropDef("Sarrera", RustyKarpeta, "Container_small", -1f, -1f, 0f, false),
            new PropDef("Sarrera", MagixBoxKarpeta, "Box1", -0.15f, 0.45f, 0f, true),
            new PropDef("Sarrera", MagixBoxKarpeta, "Box3", 0.1f, -0.45f, 0f, true),
            new PropDef("Laborategia", RustyKarpeta, "Supply_box", 1f, 1f, 0f, false),
            new PropDef("Laborategia", RustyKarpeta, "Rack", -1f, 1f, 0f, false),
            new PropDef("Laborategia", RustyKarpeta, "Old_computer", 0.1f, -1f, 0f, false),
            new PropDef("Laborategia", RustyKarpeta, "Container_BigOpen", 1f, -1f, 0f, false),
            new PropDef("Laborategia", MagixBoxKarpeta, "Box3", -0.2f, 0.3f, 0f, true),
            new PropDef("Laborategia", MagixBoxKarpeta, "Box1", 0.25f, -0.3f, 0f, true),
            new PropDef("Laborategia", RustyKarpeta, "Container_small", -0.5f, -0.55f, 0f, false),
        };

        // ---------- Menuak ----------

        [MenuItem(Menua + "Neurketa (ez du ezer aldatzen)")]
        private static void Neurketa() => NeurketaExekutatu();

        private static bool NeurketaExekutatu()
        {
            Debug.Log($"{Marka}_HASI Neurketa");
            string eszena = SceneManager.GetActiveScene().path;
            var informea = new Informea(Tresna, Marka, "Neurketa", eszena, "Bi kit-etako prefab guztiak");
            try
            {
                string[] guidak = AssetDatabase.FindAssets("t:Prefab", new[] { MagixBoxKarpeta, RustyKarpeta });
                List<GameObject> prefabak = guidak
                    .Select(guida => AssetDatabase.GUIDToAssetPath(guida))
                    .OrderBy(bidea => bidea, StringComparer.Ordinal)
                    .Select(bidea => AssetDatabase.LoadAssetAtPath<GameObject>(bidea))
                    .ToList();

                List<Modulua> neurriak = NeurtuGuztiak(prefabak);
                informea.Lerroa(string.Empty);
                informea.Lerroa("Neurriak metrotan, prefab-aren erroa (0,0,0) puntuan dagoela.");
                informea.Lerroa(string.Empty);
                informea.Lerroa("| Prefab | X | Y | Z | Zentroa | Biraketa | Renderer | Collider | Rigidbody | Light |");
                informea.Lerroa("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
                foreach (Modulua m in neurriak)
                {
                    Vector3 s = m.Kaxa.size;
                    informea.Lerroa($"| {m.Izena} | {F(s.x)} | {F(s.y)} | {F(s.z)} | {V(m.Kaxa.center)} | {V(m.ErroBiraketa.eulerAngles)} | {m.RendererKop} | {m.ColliderKop} | {(m.RigidbodyDu ? "bai" : "ez")} | {m.ArgiKop} |");
                }

                informea.AmaituOndo($"{neurriak.Count} prefab neurtuta (ez da ezer aldatu)");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
        }

        [MenuItem(Menua + "Dena eraiki (neurketa, aurrebista, aplikatu)")]
        private static void DenaEraikiMenua() => DenaEraiki();

        /// <summary>
        /// Batchmode-rako sarrera (Unity itxita bakarrik): eszena ireki, dena eraiki eta, ondo bukatzen
        /// bada, gorde. Irteera-kodea: 0 ondo, 1 errorea; errorearekin ez da gordetzen.
        /// </summary>
        public static void BatchDenaEraiki()
        {
            int kodea = 1;
            try
            {
                Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                if (DenaEraiki())
                {
                    if (EditorSceneManager.SaveScene(eszena))
                    {
                        kodea = 0;
                    }
                    else
                    {
                        Debug.LogError($"{Marka}_ERROR Ezin izan da eszena gorde: {EszenaBidea}");
                    }
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Neurketa eta urrats bakoitzaren aurrebista + aplikazioa. Ctrl+Z bakarrak dena desegiten du.</summary>
        private static bool DenaEraiki()
        {
            Debug.Log($"{Marka}_HASI Dena eraiki");
            if (!NeurketaExekutatu())
            {
                return false;
            }

            Undo.IncrementCurrentGroup();
            int taldea = Undo.GetCurrentGroup();
            bool ondo = Urratsa("Egitura", new[] { 1, 2, 3 }, true, false)
                        && Urratsa("Sektorea1 eraiki", new[] { 1 }, false, true)
                        && Urratsa("Blockout S2-S3", new[] { 2, 3 }, false, true);
            Undo.SetCurrentGroupName("ECLIPSE Dena eraiki");
            Undo.CollapseUndoOperations(taldea);

            if (ondo)
            {
                Debug.Log($"{Marka}_OK Dena eraiki: amaituta. Gorde eszena.");
            }
            else
            {
                Debug.LogError($"{Marka}_ERROR Dena eraiki: gelditu da. Ikusi azken informea docs/qa/tresnak/ karpetan.");
            }

            return ondo;
        }

        private static bool Urratsa(string izena, int[] sektoreak, bool erroakSortu, bool geometria)
        {
            return Exekutatu($"{izena}: Aurrebista", false, sektoreak, erroakSortu, geometria)
                   && Exekutatu($"{izena}: Aplikatu", true, sektoreak, erroakSortu, geometria);
        }

        [MenuItem(Menua + "Egitura: Aurrebista")]
        private static void EgituraAurrebista() => Exekutatu("Egitura: Aurrebista", false, new[] { 1, 2, 3 }, true, false);

        [MenuItem(Menua + "Egitura: Aplikatu")]
        private static void EgituraAplikatu() => Exekutatu("Egitura: Aplikatu", true, new[] { 1, 2, 3 }, true, false);

        [MenuItem(Menua + "Sektorea1 eraiki: Aurrebista")]
        private static void S1Aurrebista() => Exekutatu("Sektorea1 eraiki: Aurrebista", false, new[] { 1 }, false, true);

        [MenuItem(Menua + "Sektorea1 eraiki: Aplikatu")]
        private static void S1Aplikatu() => Exekutatu("Sektorea1 eraiki: Aplikatu", true, new[] { 1 }, false, true);

        [MenuItem(Menua + "Blockout S2-S3: Aurrebista")]
        private static void BlockoutAurrebista() => Exekutatu("Blockout S2-S3: Aurrebista", false, new[] { 2, 3 }, false, true);

        [MenuItem(Menua + "Blockout S2-S3: Aplikatu")]
        private static void BlockoutAplikatu() => Exekutatu("Blockout S2-S3: Aplikatu", true, new[] { 2, 3 }, false, true);

        // ---------- Exekuzioa ----------

        private static bool Exekutatu(string modua, bool aplikatu, int[] sektoreak, bool erroakSortu, bool geometria)
        {
            Debug.Log($"{Marka}_HASI {modua}");
            if (!EszenaZuzena(out string errorea))
            {
                Debug.LogError($"{Marka}_ERROR {errorea}");
                return false;
            }

            string esparrua = string.Join(", ", sektoreak.Select(n => $"Sektorea{n}"));
            var informea = new Informea(Tresna, Marka, modua, EszenaBidea, esparrua);
            Scene eszena = SceneManager.GetActiveScene();

            Plana plana;
            try
            {
                plana = PlanaEraiki(sektoreak, erroakSortu, geometria);
            }
            catch (Exception salbuespena)
            {
                informea.AmaituErrorearekin(salbuespena.Message);
                return false;
            }

            informea.Atala("Salak", plana.SalaLerroak);
            informea.Atala("Modulu erabiliak", plana.ModuluLerroak);
            informea.Atala("Oharrak", plana.Oharrak);

            // Lehenengo pasa beti lehorrean: erroreak daudenean eszena ez da ukitzen.
            var aurreikuspena = new Aldaketak();
            Sinkronizatu(plana, false, eszena, aurreikuspena);
            List<string> erroreak = plana.Erroreak.Concat(aurreikuspena.Erroreak).ToList();
            if (erroreak.Count > 0)
            {
                informea.Atala("Erroreak", erroreak);
                informea.AmaituErrorearekin($"{erroreak.Count} errore: ez da ezer aldatu");
                return false;
            }

            if (!aplikatu)
            {
                InformatuAldaketak(informea, aurreikuspena);
                informea.AmaituOndo($"{aurreikuspena.Guztira} aldaketa ({aurreikuspena.Laburpena}); ez da ezer aldatu");
                return true;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"ECLIPSE {modua}");
            int taldea = Undo.GetCurrentGroup();
            var aldaketak = new Aldaketak();
            try
            {
                Sinkronizatu(plana, true, eszena, aldaketak);
                Undo.CollapseUndoOperations(taldea);
            }
            catch (Exception salbuespena)
            {
                Undo.RevertAllDownToGroup(taldea);
                informea.AmaituErrorearekin($"{salbuespena.Message} (desegin da)");
                return false;
            }

            if (aldaketak.Guztira > 0)
            {
                EditorSceneManager.MarkSceneDirty(eszena);
            }

            InformatuAldaketak(informea, aldaketak);

            // Egiaztapena: benetako Renderer.bounds planarekin bat etorri behar da, eta bigarren pasak 0 eman.
            List<string> hutsak = Egiaztatu(plana, eszena);
            informea.Atala("Egiaztapena: pieza hauek ez daude espero zen tokian", hutsak);
            var bigarrena = new Aldaketak();
            Sinkronizatu(plana, false, eszena, bigarrena);
            if (hutsak.Count > 0 || bigarrena.Guztira > 0)
            {
                informea.AmaituErrorearekin($"{hutsak.Count} pieza okerreko tokian eta {bigarrena.Guztira} aldaketa geratzen dira. Eraikuntza eszenan dago: Ctrl+Z-k desegiten du");
                return false;
            }

            informea.AmaituOndo($"{aldaketak.Guztira} aldaketa ({aldaketak.Laburpena})");
            return true;
        }

        private static bool EszenaZuzena(out string errorea)
        {
            errorea = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                errorea = "Play moduan ezin da exekutatu.";
                return false;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                errorea = "Itxi prefab modua lehenago.";
                return false;
            }

            if (SceneManager.GetActiveScene().path != EszenaBidea)
            {
                errorea = $"Eszena aktiboak {EszenaBidea} izan behar du (orain: '{SceneManager.GetActiveScene().path}').";
                return false;
            }

            return true;
        }

        private static void InformatuAldaketak(Informea informea, Aldaketak aldaketak)
        {
            informea.Lerroa(string.Empty);
            informea.Lerroa($"**Aldaketak guztira: {aldaketak.Guztira}** ({aldaketak.Laburpena})");
            informea.Atala("Sortzeko edo sortuak", aldaketak.Sortuak);
            informea.Atala("Eguneratzeko edo eguneratuak", aldaketak.Eguneratuak);
            informea.Atala("Ezabatzeko edo ezabatuak", aldaketak.Ezabatuak);
        }

        // ---------- Neurketa ----------

        /// <summary>
        /// Aurrebista-eszena batean neurtzen da: eszena irekia ez da ukitzen. Erroa (0,0,0) puntuan
        /// jartzen da eta prefab-aren jatorrizko biraketa mantentzen du.
        /// </summary>
        private static List<Modulua> NeurtuGuztiak(IEnumerable<GameObject> prefabak, bool eskalarekin = false)
        {
            _eskalarekin = eskalarekin;
            var emaitza = new List<Modulua>();
            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (GameObject prefab in prefabak)
                {
                    emaitza.Add(Neurtu(prefab, aurrebista, EgituraEskala(prefab)));
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            return emaitza;
        }

        private static float EgituraEskala(GameObject prefab)
        {
            return _eskalarekin && AssetDatabase.GetAssetPath(prefab).StartsWith(MagixBoxKarpeta, StringComparison.Ordinal)
                ? ModuluEskala
                : 1f;
        }

        private static Modulua Neurtu(GameObject prefab, Scene aurrebista, float eskala)
        {
            var instantzia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, aurrebista);
            instantzia.transform.position = Vector3.zero;
            instantzia.transform.localScale *= eskala;

            List<Renderer> rendererrak = GeometriaRendererrak(instantzia);
            Bounds kaxa = rendererrak.Count > 0 ? rendererrak[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in rendererrak)
            {
                kaxa.Encapsulate(renderer.bounds);
            }

            var zatiak = new Dictionary<string, Bounds>();
            foreach (Renderer renderer in rendererrak)
            {
                if (!zatiak.ContainsKey(renderer.name))
                {
                    zatiak.Add(renderer.name, renderer.bounds);
                }
            }

            return new Modulua
            {
                Zatiak = zatiak,
                Izena = prefab.name,
                Prefab = prefab,
                Kaxa = kaxa,
                ErroBiraketa = instantzia.transform.rotation,
                ErroEskala = instantzia.transform.localScale,
                RendererKop = rendererrak.Count,
                ColliderKop = instantzia.GetComponentsInChildren<Collider>(true).Length,
                RigidbodyDu = instantzia.GetComponentsInChildren<Rigidbody>(true).Length > 0,
                ArgiKop = instantzia.GetComponentsInChildren<Light>(true).Length,
            };
        }

        private static List<Renderer> GeometriaRendererrak(GameObject objektua)
        {
            return objektua.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToList();
        }

        // ---------- Plana (eszena ukitu gabe) ----------

        private static Plana PlanaEraiki(int[] sektoreak, bool erroakSortu, bool geometria)
        {
            var plana = new Plana { ErroakSortu = erroakSortu };

            var oinarrizkoak = new[]
            {
                $"{MagixBoxKarpeta}/{Zorua}.prefab",
                $"{MagixBoxKarpeta}/{Horma}.prefab",
                $"{MagixBoxKarpeta}/{Sabaia}.prefab",
                $"{MagixBoxKarpeta}/{Markoa}.prefab",
                $"{MagixBoxKarpeta}/{SekzioMuturra}.prefab",
                $"{MagixBoxKarpeta}/Starter/{Sekzioa}.prefab",
                $"{MagixBoxKarpeta}/Starter/{SekzioAtea}.prefab",
            };

            var prefabak = new List<GameObject>();
            foreach (string bidea in oinarrizkoak)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
                if (prefab == null)
                {
                    plana.Erroreak.Add($"Ez da aurkitu oinarrizko prefab-a: {bidea}");
                }
                else
                {
                    prefabak.Add(prefab);
                }
            }

            if (geometria && sektoreak.Contains(1))
            {
                foreach (string bidea in Sektorea1Propak.Select(prop => $"{prop.Karpeta}/{prop.Prefab}.prefab").Distinct())
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
                    if (prefab == null)
                    {
                        plana.Oharrak.Add($"Ez da aurkitu {bidea}; omitituta.");
                    }
                    else
                    {
                        prefabak.Add(prefab);
                    }
                }
            }

            if (plana.Erroreak.Count > 0)
            {
                return plana;
            }

            Dictionary<string, Modulua> moduluak = NeurtuGuztiak(prefabak, true).ToDictionary(m => m.Izena);
            Neurriak n = NeurriakLortu(moduluak, plana);
            if (n == null)
            {
                return plana;
            }

            foreach (Modulua m in moduluak.Values.OrderBy(m => m.Izena, StringComparer.Ordinal))
            {
                plana.ModuluLerroak.Add($"{m.Izena}: {V(m.Kaxa.size)} m; {m.ColliderKop} collider");
            }

            plana.ModuluLerroak.Add($"Eskala: {F(ModuluEskala)}; atea {F(n.AteZabalera)} x {F(n.AteAltuera)} m; korridorea {F(n.HodiBarruErdia * 2f)} x {F(n.HodiAltuera)} m; salen altuera {F(n.SalaAltuera)} m");

            foreach (int zenbakia in sektoreak)
            {
                SektoreDef def = Sektoreak.First(s => s.Zenbakia == zenbakia);
                plana.Sektoreak.Add(SektoreaPlanifikatu(def, moduluak, n, geometria, zenbakia == 1, plana));
            }

            return plana;
        }

        /// <summary>Kit-aren neurri errealak balidatu eta eraikuntzarako behar direnak atera.</summary>
        private static Neurriak NeurriakLortu(Dictionary<string, Modulua> m, Plana plana)
        {
            Vector3 zorua = m[Zorua].Kaxa.size;
            if (zorua.y >= Mathf.Min(zorua.x, zorua.z))
            {
                plana.Erroreak.Add($"{Zorua} ez dirudi zoru-modulua: {V(zorua)} m.");
            }

            Vector3 sabaia = m[Sabaia].Kaxa.size;
            if (sabaia.y >= Mathf.Min(sabaia.x, sabaia.z))
            {
                plana.Erroreak.Add($"{Sabaia} ez dirudi sabai-modulua: {V(sabaia)} m.");
            }

            HormaBalidatu(m[Horma], plana);
            HormaBalidatu(m[Markoa], plana);

            bool zatiakDaude = ZatiaDago(m[Sekzioa], "Floor2", plana)
                               & ZatiaDago(m[Sekzioa], "TopWall4", plana)
                               & ZatiaDago(m[Sekzioa], "Wall12 (1)", plana)
                               & ZatiaDago(m[Sekzioa], Sekzioa, plana)
                               & ZatiaDago(m[SekzioAtea], SekzioAtea, plana)
                               & ZatiaDago(m[SekzioAtea], AteOrria, plana);
            if (!zatiakDaude || plana.Erroreak.Count > 0)
            {
                return null;
            }

            Modulua horma = m[Horma];
            float zoruGoia = m[Sekzioa].Zatiak["Floor2"].max.y;
            float sabaiBehea = m[Sekzioa].Zatiak["TopWall4"].min.y;
            float panelAltuera = horma.Kaxa.size.y;

            var n = new Neurriak
            {
                Panela = horma.Kaxa.size[LuzeraArdatza(horma)],
                PanelAltuera = panelAltuera,
                HormaLodiera = horma.Kaxa.size[LodieraArdatza(horma)],
                Lauza = m[Zorua].Kaxa.size,
                SalaAltuera = panelAltuera * HormaIlarak,
                SekzioLuzera = m[Sekzioa].Zatiak[Sekzioa].size.z,
                AteLuzera = m[SekzioAtea].Zatiak[SekzioAtea].size.z,
                MuturLuzera = m[SekzioMuturra].Kaxa.size.z,
                HodiErdiaY = -zoruGoia,
                HodiAltuera = sabaiBehea - zoruGoia,
                HodiBarruErdia = Mathf.Abs(m[Sekzioa].Zatiak["Wall12 (1)"].max.x),
                AteZabalera = m[SekzioAtea].Zatiak[AteOrria].size.x,
                AteAltuera = m[SekzioAtea].Zatiak[AteOrria].size.y,
                MarkoLodiera = m[Markoa].Kaxa.size[LodieraArdatza(m[Markoa])],
            };

            if (Mathf.Abs(n.Lauza.z * 2f - n.Panela) > 0.05f)
            {
                plana.Oharrak.Add($"Lauza Z ({F(n.Lauza.z)}) x 2 eta panela ({F(n.Panela)}) ez datoz bat: panelak eskalatuko dira.");
            }

            return n;
        }

        private static bool ZatiaDago(Modulua m, string zatia, Plana plana)
        {
            if (m.Zatiak.ContainsKey(zatia))
            {
                return true;
            }

            plana.Erroreak.Add($"{m.Izena}: ez du '{zatia}' zatia; kit-aren egitura ez da espero zena.");
            return false;
        }

        private static void HormaBalidatu(Modulua m, Plana plana)
        {
            Vector3 s = m.Kaxa.size;
            float lodiera = s[LodieraArdatza(m)];
            if (lodiera > 1.5f || lodiera >= Mathf.Max(s.x, s.z) || s.y < 1.8f || s.y > 12f)
            {
                plana.Erroreak.Add($"{m.Izena} ez dirudi horma-modulua: {V(s)} m.");
            }
        }

        private static SektorePlana SektoreaPlanifikatu(SektoreDef def, Dictionary<string, Modulua> m, Neurriak n, bool geometria, bool osoa, Plana plana)
        {
            var sektorea = new SektorePlana { Izena = $"Sektorea{def.Zenbakia}", Posizioa = def.Posizioa, Geometria = geometria };

            // Lehenengo luzerak, gero dena X=0 inguruan zentratuta.
            float loturaGehigarria = n.MarkoLodiera - n.HormaLodiera;
            float luzeraEzker = def.Ezkerra != null ? def.Ezkerra.Lauzak * n.Lauza.x : 0f;
            float luzeraEskuin = def.Eskuina != null ? def.Eskuina.Lauzak * n.Lauza.x : 0f;
            float hodia = def.Sekzioak * n.SekzioLuzera
                          + (def.Ezkerra != null ? loturaGehigarria + n.AteLuzera : n.MuturLuzera)
                          + (def.Eskuina != null ? loturaGehigarria + n.AteLuzera : n.MuturLuzera);
            float x = -(luzeraEzker + hodia + luzeraEskuin) * 0.5f;

            float hodiHasiera = x + luzeraEzker;
            float hodiAmaiera = hodiHasiera + hodia;
            string korridorea = "Korridorea";

            GelaGehitu(sektorea, def, korridorea,
                new Vector3((hodiHasiera + hodiAmaiera) * 0.5f, 0f, 0f),
                new Vector3(hodia, n.HodiAltuera, n.HodiBarruErdia * 2f), plana);

            SalaPlana ezkerra = def.Ezkerra != null ? SalaSortu(def.Ezkerra, x, n) : null;
            SalaPlana eskuina = def.Eskuina != null ? SalaSortu(def.Eskuina, hodiAmaiera, n) : null;
            foreach (SalaPlana sala in new[] { ezkerra, eskuina }.Where(s => s != null))
            {
                if (sala.ZMax > SektoreErdiZ)
                {
                    plana.Erroreak.Add($"{sektorea.Izena}/{sala.Izena}: Z ardatzean {F(sala.ZMax)} m-ra iristen da eta hurrengo sektorearekin talka egin dezake.");
                }

                GelaGehitu(sektorea, def, sala.Izena,
                    new Vector3((sala.XMin + sala.XMax) * 0.5f, 0f, 0f),
                    new Vector3(sala.XMax - sala.XMin - 2f * n.HormaLodiera, n.SalaAltuera, sala.ZMax - sala.ZMin - 2f * n.HormaLodiera), plana);
            }

            if (!geometria || plana.Erroreak.Count > 0)
            {
                return sektorea;
            }

            KorridoreaPlanifikatu(sektorea, korridorea, m, n, hodiHasiera, hodiAmaiera, def.Ezkerra != null, def.Eskuina != null, def.Sekzioak, plana);
            if (ezkerra != null)
            {
                SalaPlanifikatu(sektorea, ezkerra, m, n, false, true, osoa, plana);
            }

            if (eskuina != null)
            {
                SalaPlanifikatu(sektorea, eskuina, m, n, true, false, osoa, plana);
            }

            if (osoa)
            {
                PropakPlanifikatu(sektorea, new[] { ezkerra, eskuina }.Where(s => s != null).ToList(), m, n, plana);
            }

            return sektorea;
        }

        private static SalaPlana SalaSortu(SalaDef def, float xHasiera, Neurriak n)
        {
            float zErdia = def.Panelak * n.Lauza.z;
            return new SalaPlana
            {
                Izena = def.Izena,
                Nx = def.Lauzak,
                Nz = def.Panelak * 2,
                XMin = xHasiera,
                XMax = xHasiera + def.Lauzak * n.Lauza.x,
                ZMin = -zErdia,
                ZMax = zErdia,
            };
        }

        private static void GelaGehitu(SektorePlana sektorea, SektoreDef def, string sala, Vector3 posizioa, Vector3 tamaina, Plana plana)
        {
            string izena = $"Gela_S{def.Zenbakia}_{sala}";
            sektorea.Gelak.Add(new GelaPlana
            {
                Izena = izena,
                Posizioa = posizioa,
                Zentroa = new Vector3(0f, tamaina.y * 0.5f, 0f),
                Tamaina = tamaina,
            });
            plana.SalaLerroak.Add($"{izena}: barrua {F(tamaina.x)} x {F(tamaina.z)} m; altuera {F(tamaina.y)} m");
        }

        /// <summary>
        /// Korridorea X ardatzean: (marko-lotura) ProfileDoor + Profile1 × n + ProfileDoor (marko-lotura).
        /// Sala ez dagoen muturrean, ProfileEnd. Atearen orria instantzian ezkutatzen da: oraindik ez dago script-ik.
        /// </summary>
        private static void KorridoreaPlanifikatu(SektorePlana sektorea, string sala, Dictionary<string, Modulua> m, Neurriak n,
            float hasiera, float amaiera, bool salaEzker, bool salaEskuin, int sekzioak, Plana plana)
        {
            Quaternion yaw90 = Quaternion.Euler(0f, 90f, 0f);
            float lotura = n.MarkoLodiera - n.HormaLodiera;
            float x = hasiera;

            if (salaEzker)
            {
                LoturaZorua(sektorea, sala, m, n, x, x + lotura, plana);
                x += lotura;
                AteSekzioa(sektorea, sala, m[SekzioAtea], n, x, yaw90, plana);
                x += n.AteLuzera;
            }
            else
            {
                MuturSekzioa(sektorea, sala, m[SekzioMuturra], n, x, Vector3.right, plana);
                x += n.MuturLuzera;
            }

            for (int i = 0; i < sekzioak; i++)
            {
                HodiPieza(sektorea, sala, m[Sekzioa], x + n.SekzioLuzera * 0.5f, n, yaw90, null, plana);
                x += n.SekzioLuzera;
            }

            if (salaEskuin)
            {
                AteSekzioa(sektorea, sala, m[SekzioAtea], n, x, yaw90, plana);
                x += n.AteLuzera;
                LoturaZorua(sektorea, sala, m, n, x, x + lotura, plana);
            }
            else
            {
                MuturSekzioa(sektorea, sala, m[SekzioMuturra], n, x, Vector3.left, plana);
            }

            // Kolisioak: zorua eta bi hormak mutur batetik bestera.
            float erdia = (hasiera + amaiera) * 0.5f;
            float luzera = amaiera - hasiera;
            float z = n.HodiBarruErdia + KolisioLodiera * 0.5f;
            KolisioGehitu(sektorea, sala, new Vector3(erdia, -KolisioLodiera * 0.5f, 0f), new Vector3(luzera, KolisioLodiera, n.HodiBarruErdia * 2f + KolisioLodiera * 2f));
            KolisioGehitu(sektorea, sala, new Vector3(erdia, n.HodiAltuera * 0.5f, z), new Vector3(luzera, n.HodiAltuera, KolisioLodiera));
            KolisioGehitu(sektorea, sala, new Vector3(erdia, n.HodiAltuera * 0.5f, -z), new Vector3(luzera, n.HodiAltuera, KolisioLodiera));
            KolisioGehitu(sektorea, sala, new Vector3(erdia, n.HodiAltuera + KolisioLodiera * 0.5f, 0f), new Vector3(luzera, KolisioLodiera, n.HodiBarruErdia * 2f + KolisioLodiera * 2f));
        }

        private static void HodiPieza(SektorePlana sektorea, string sala, Modulua m, float x, Neurriak n, Quaternion biraketa, string ezkutatu, Plana plana)
        {
            Vector3 zentroa = biraketa * m.Kaxa.center;
            PiezaGehitu(sektorea, sala, Hodiak, m, biraketa, Vector3.one, new Vector3(x + zentroa.x, n.HodiErdiaY + zentroa.y, zentroa.z), plana, ezkutatu);
        }

        private static void AteSekzioa(SektorePlana sektorea, string sala, Modulua m, Neurriak n, float x, Quaternion biraketa, Plana plana)
        {
            float erdia = x + n.AteLuzera * 0.5f;
            HodiPieza(sektorea, sala, m, erdia, n, biraketa, AteOrria, plana);

            // Atearen kolisioa: bi albo eta goiko aldea; zuloa atearen orriaren neurrikoa da.
            float alboa = n.HodiBarruErdia - n.AteZabalera * 0.5f;
            float albokoZ = n.AteZabalera * 0.5f + alboa * 0.5f;
            KolisioGehitu(sektorea, sala, new Vector3(erdia, n.HodiAltuera * 0.5f, albokoZ), new Vector3(n.AteLuzera, n.HodiAltuera, alboa));
            KolisioGehitu(sektorea, sala, new Vector3(erdia, n.HodiAltuera * 0.5f, -albokoZ), new Vector3(n.AteLuzera, n.HodiAltuera, alboa));
            float goia = n.HodiAltuera - n.AteAltuera;
            if (goia > 0.01f)
            {
                KolisioGehitu(sektorea, sala, new Vector3(erdia, n.AteAltuera + goia * 0.5f, 0f), new Vector3(n.AteLuzera, goia, n.AteZabalera));
            }
        }

        /// <summary>ProfileEnd-en barruko aldea (+Z prefab-ean) korridorera begira.</summary>
        private static void MuturSekzioa(SektorePlana sektorea, string sala, Modulua m, Neurriak n, float x, Vector3 barrura, Plana plana)
        {
            Quaternion biraketa = Quaternion.LookRotation(barrura);
            float erdia = x + n.MuturLuzera * 0.5f;
            HodiPieza(sektorea, sala, m, erdia, n, biraketa, null, plana);
            KolisioGehitu(sektorea, sala, new Vector3(erdia, n.HodiAltuera * 0.5f, 0f), new Vector3(n.MuturLuzera, n.HodiAltuera, n.HodiBarruErdia * 2f));
        }

        /// <summary>Salaren ertzaren eta korridorearen arteko atalasea: zoru-lauza bat tartera egokituta.</summary>
        private static void LoturaZorua(SektorePlana sektorea, string sala, Dictionary<string, Modulua> m, Neurriak n, float x0, float x1, Plana plana)
        {
            float luzera = x1 - x0;
            if (luzera < 0.01f)
            {
                return;
            }

            var faktoreak = new Vector3(luzera / n.Lauza.x, 1f, n.Panela / n.Lauza.z);
            PiezaGehitu(sektorea, sala, Zoruak, m[Zorua], Quaternion.identity, faktoreak, new Vector3((x0 + x1) * 0.5f, -n.Lauza.y * 0.5f, 0f), plana);
        }

        private static void SalaPlanifikatu(SektorePlana sektorea, SalaPlana sala, Dictionary<string, Modulua> m, Neurriak n,
            bool ateaMendebaldean, bool ateaEkialdean, bool osoa, Plana plana)
        {
            Modulua zorua = m[Zorua];
            Modulua sabaia = m[Sabaia];
            Quaternion iraulita = Quaternion.Euler(180f, 0f, 0f);
            var sabaiFaktoreak = new Vector3(n.Lauza.x / sabaia.Kaxa.size.x, 1f, n.Lauza.z / sabaia.Kaxa.size.z);
            Bounds sabaiNeurria = KaxaKalkulatu(sabaia, iraulita, sabaiFaktoreak);

            for (int ix = 0; ix < sala.Nx; ix++)
            {
                for (int iz = 0; iz < sala.Nz; iz++)
                {
                    float x = sala.XMin + (ix + 0.5f) * n.Lauza.x;
                    float z = sala.ZMin + (iz + 0.5f) * n.Lauza.z;
                    PiezaGehitu(sektorea, sala.Izena, Zoruak, zorua, Quaternion.identity, Vector3.one, new Vector3(x, -n.Lauza.y * 0.5f, z), plana);
                    if (osoa)
                    {
                        PiezaGehitu(sektorea, sala.Izena, Sabaiak, sabaia, iraulita, sabaiFaktoreak, new Vector3(x, n.SalaAltuera + sabaiNeurria.size.y * 0.5f, z), plana);
                    }
                }
            }

            HormaLerroa(sektorea, sala, m, n, Vector3.back, sala.ZMax, sala.XMin, sala.XMax, false, plana);
            HormaLerroa(sektorea, sala, m, n, Vector3.forward, sala.ZMin, sala.XMin, sala.XMax, false, plana);
            // E/W hormak N/S hormen barruko aurpegien artean: izkinetan gainjarrita, texel ezkutuek lightmap-a zikintzen dute.
            float z0 = sala.ZMin + n.HormaLodiera;
            float z1 = sala.ZMax - n.HormaLodiera;
            HormaLerroa(sektorea, sala, m, n, Vector3.right, sala.XMin, z0, z1, ateaMendebaldean, plana);
            HormaLerroa(sektorea, sala, m, n, Vector3.left, sala.XMax, z0, z1, ateaEkialdean, plana);

            // Kolisioak: zorua.
            float lx = sala.XMax - sala.XMin;
            float lz = sala.ZMax - sala.ZMin;
            float cx = (sala.XMin + sala.XMax) * 0.5f;
            KolisioGehitu(sektorea, sala.Izena, new Vector3(cx, -KolisioLodiera * 0.5f, 0f), new Vector3(lx, KolisioLodiera, lz));
            if (osoa)
            {
                KolisioGehitu(sektorea, sala.Izena, new Vector3(cx, n.SalaAltuera + KolisioLodiera * 0.5f, 0f), new Vector3(lx, KolisioLodiera, lz));
            }
        }

        private static void HormaLerroa(SektorePlana sektorea, SalaPlana sala, Dictionary<string, Modulua> m, Neurriak n,
            Vector3 barrukoNormala, float ertza, float hasiera, float amaiera, bool atea, Plana plana)
        {
            float zentroNormalean = ertza + n.HormaLodiera * 0.5f * Zeinua(barrukoNormala);
            float erdia = (hasiera + amaiera) * 0.5f;

            if (!atea)
            {
                PanelIlarak(sektorea, sala, m[Horma], n, barrukoNormala, ertza, hasiera, amaiera, plana);
                HormaKolisioa(sektorea, sala.Izena, n, barrukoNormala, zentroNormalean, hasiera, amaiera, 0f, n.SalaAltuera);
                return;
            }

            float gunea0 = erdia - n.Panela * 0.5f;
            float gunea1 = erdia + n.Panela * 0.5f;
            PanelIlarak(sektorea, sala, m[Horma], n, barrukoNormala, ertza, hasiera, gunea0, plana);
            PanelIlarak(sektorea, sala, m[Horma], n, barrukoNormala, ertza, gunea1, amaiera, plana);
            HormaKolisioa(sektorea, sala.Izena, n, barrukoNormala, zentroNormalean, hasiera, gunea0, 0f, n.SalaAltuera);
            HormaKolisioa(sektorea, sala.Izena, n, barrukoNormala, zentroNormalean, gunea1, amaiera, 0f, n.SalaAltuera);

            // Markoa: prefab-ean aurpegi landua -X da; salara begira jarri, hormaren barruko aurpegiarekin bat.
            Modulua markoa = m[Markoa];
            Quaternion biraketa = Quaternion.Euler(0f, barrukoNormala.x < 0f ? 0f : 180f, 0f);
            Bounds neurria = KaxaKalkulatu(markoa, biraketa, Vector3.one);
            float barrukoAurpegia = ertza + n.HormaLodiera * Zeinua(barrukoNormala);
            float markoX = barrukoAurpegia - neurria.size.x * 0.5f * Zeinua(barrukoNormala);
            var zentroa = new Vector3(markoX, n.HodiErdiaY + neurria.center.y, erdia + neurria.center.z);
            PiezaGehitu(sektorea, sala.Izena, Hormak, markoa, biraketa, Vector3.one, zentroa, plana);

            // Markoaren albo-zuloak korridorearen kaskoak ixten ditu; gainean, panel bat sabaira arte.
            float markoGoia = zentroa.y + neurria.size.y * 0.5f;
            if (n.SalaAltuera - markoGoia > 0.01f)
            {
                HormaPieza(sektorea, sala.Izena, m[Horma], n, barrukoNormala, ertza, gunea0, gunea1, markoGoia, n.SalaAltuera, plana);
            }

            float hodiGoia = n.HodiAltuera;
            float z0 = erdia - n.HodiBarruErdia;
            float z1 = erdia + n.HodiBarruErdia;
            HormaKolisioa(sektorea, sala.Izena, n, barrukoNormala, zentroNormalean, gunea0, z0, 0f, n.SalaAltuera);
            HormaKolisioa(sektorea, sala.Izena, n, barrukoNormala, zentroNormalean, z1, gunea1, 0f, n.SalaAltuera);
            HormaKolisioa(sektorea, sala.Izena, n, barrukoNormala, zentroNormalean, z0, z1, hodiGoia, n.SalaAltuera);

            sala.Ateak.Add(new AteGunea { BarrukoNormala = barrukoNormala, Erdia = erdia });
        }

        private static void HormaKolisioa(SektorePlana sektorea, string sala, Neurriak n, Vector3 normala, float normalean, float a, float b, float y0, float y1)
        {
            float luzera = b - a;
            float altuera = y1 - y0;
            if (luzera < 0.01f || altuera < 0.01f)
            {
                return;
            }

            float lodiera = Mathf.Max(n.HormaLodiera, KolisioLodiera);
            bool xNormala = Mathf.Abs(normala.x) > 0.5f;
            Vector3 zentroa = HormaZentroa(normala, normalean, (a + b) * 0.5f, (y0 + y1) * 0.5f);
            Vector3 tamaina = xNormala ? new Vector3(lodiera, altuera, luzera) : new Vector3(luzera, altuera, lodiera);
            KolisioGehitu(sektorea, sala, zentroa, tamaina);
        }

        private static void PanelIlarak(SektorePlana sektorea, SalaPlana sala, Modulua horma, Neurriak n,
            Vector3 barrukoNormala, float ertza, float hasiera, float amaiera, Plana plana)
        {
            float luzera = amaiera - hasiera;
            if (luzera < 0.01f)
            {
                return;
            }

            int kopurua = Mathf.Max(1, Mathf.RoundToInt(luzera / n.Panela));
            float tartea = luzera / kopurua;
            if (Mathf.Abs(tartea / n.Panela - 1f) > EskalaAbisua)
            {
                plana.Oharrak.Add($"{sala.Izena}: {horma.Izena} panelak {F(tartea / n.Panela)} eskalan ({F(luzera)} m betetzeko).");
            }

            for (int ilara = 0; ilara < HormaIlarak; ilara++)
            {
                float y0 = ilara * n.PanelAltuera;
                for (int i = 0; i < kopurua; i++)
                {
                    HormaPieza(sektorea, sala.Izena, horma, n, barrukoNormala, ertza,
                        hasiera + i * tartea, hasiera + (i + 1) * tartea, y0, y0 + n.PanelAltuera, plana);
                }
            }
        }

        private static void HormaPieza(SektorePlana sektorea, string sala, Modulua m, Neurriak n, Vector3 barrukoNormala,
            float ertza, float hasiera, float amaiera, float y0, float y1, Plana plana)
        {
            Quaternion biraketa = HormaBiraketa(m, HormaBarruaPositiboa, barrukoNormala);
            Vector3 faktoreak = Vector3.one;
            faktoreak[LuzeraArdatza(m)] = (amaiera - hasiera) / m.Kaxa.size[LuzeraArdatza(m)];
            faktoreak.y = (y1 - y0) / m.Kaxa.size.y;

            float zentroNormalean = ertza + m.Kaxa.size[LodieraArdatza(m)] * 0.5f * Zeinua(barrukoNormala);
            Vector3 zentroa = HormaZentroa(barrukoNormala, zentroNormalean, (hasiera + amaiera) * 0.5f, (y0 + y1) * 0.5f);
            PiezaGehitu(sektorea, sala, Hormak, m, biraketa, faktoreak, zentroa, plana);
        }

        private static void PropakPlanifikatu(SektorePlana sektorea, List<SalaPlana> salak, Dictionary<string, Modulua> m, Neurriak n, Plana plana)
        {
            var hartuak = new Dictionary<string, List<Bounds>>();
            foreach (PropDef prop in Sektorea1Propak)
            {
                SalaPlana sala = salak.First(s => s.Izena == prop.Sala);
                string izena = $"{sala.Izena}/{prop.Prefab}";
                if (!m.TryGetValue(prop.Prefab, out Modulua modulua))
                {
                    continue;
                }

                if (modulua.RigidbodyDu)
                {
                    plana.Oharrak.Add($"{izena}: Rigidbody du (dinamikoa); omitituta.");
                    continue;
                }

                Quaternion biraketa = Quaternion.Euler(0f, prop.Yaw, 0f);
                Vector3 tamaina = KaxaKalkulatu(modulua, biraketa, Vector3.one).size;
                float erdiX = (sala.XMax - sala.XMin) * 0.5f - n.HormaLodiera;
                float erdiZ = (sala.ZMax - sala.ZMin) * 0.5f - n.HormaLodiera;
                if (tamaina.x > erdiX * 2f || tamaina.z > erdiZ * 2f || tamaina.y > n.SalaAltuera)
                {
                    plana.Oharrak.Add($"{izena}: ez da sartzen ({V(tamaina)} m); omitituta.");
                    continue;
                }

                var zentroa = new Vector3(
                    (sala.XMin + sala.XMax) * 0.5f + prop.U * (erdiX - tamaina.x * 0.5f),
                    tamaina.y * 0.5f,
                    (sala.ZMin + sala.ZMax) * 0.5f + prop.V * (erdiZ - tamaina.z * 0.5f));
                var kaxa = new Bounds(zentroa, tamaina);

                if (AteaBlokeatzenDu(kaxa, sala, n))
                {
                    plana.Oharrak.Add($"{izena}: atearen pasabidea blokeatzen du; omitituta.");
                    continue;
                }

                if (!hartuak.TryGetValue(sala.Izena, out List<Bounds> salakoak))
                {
                    salakoak = new List<Bounds>();
                    hartuak.Add(sala.Izena, salakoak);
                }

                var marjinarekin = new Bounds(zentroa, tamaina + Vector3.one * PropMarjina);
                if (salakoak.Any(hartua => hartua.Intersects(marjinarekin)))
                {
                    plana.Oharrak.Add($"{izena}: beste prop batekin gainjartzen da; omitituta.");
                    continue;
                }

                salakoak.Add(kaxa);
                PiezaGehitu(sektorea, sala.Izena, Propak, modulua, biraketa, Vector3.one, zentroa, plana);
                if (prop.KolisioaBehar)
                {
                    KolisioGehitu(sektorea, sala.Izena, zentroa, tamaina);
                }
            }
        }

        private static bool AteaBlokeatzenDu(Bounds kaxa, SalaPlana sala, Neurriak n)
        {
            foreach (AteGunea atea in sala.Ateak)
            {
                float barrukoAurpegia = (atea.BarrukoNormala.x > 0f ? sala.XMin : sala.XMax) + n.HormaLodiera * Zeinua(atea.BarrukoNormala);
                float zentroX = barrukoAurpegia + AteGuneSakonera * 0.5f * Zeinua(atea.BarrukoNormala);
                var gunea = new Bounds(
                    new Vector3(zentroX, kaxa.center.y, atea.Erdia),
                    new Vector3(AteGuneSakonera, kaxa.size.y, n.Panela + 1f));
                if (gunea.Intersects(kaxa))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------- Geometria ----------

        private static int LodieraArdatza(Modulua m) => m.Kaxa.size.x < m.Kaxa.size.z ? 0 : 2;

        private static int LuzeraArdatza(Modulua m) => 2 - LodieraArdatza(m);

        private static float Zeinua(Vector3 normala) => normala.x + normala.y + normala.z;

        private static Vector3 HormaZentroa(Vector3 normala, float normalean, float hormaren, float y)
        {
            return Mathf.Abs(normala.x) > 0.5f
                ? new Vector3(normalean, y, hormaren)
                : new Vector3(hormaren, y, normalean);
        }

        /// <summary>Yaw-a (90°-ko urratsetan), piezaren barruko aurpegiak gelako barrura begira dezan.</summary>
        private static Quaternion HormaBiraketa(Modulua m, bool barruaPositiboa, Vector3 barrukoNormala)
        {
            Vector3 lokala = Vector3.zero;
            lokala[LodieraArdatza(m)] = barruaPositiboa ? 1f : -1f;
            for (int yaw = 0; yaw < 360; yaw += 90)
            {
                Quaternion biraketa = Quaternion.Euler(0f, yaw, 0f);
                if (Vector3.Dot(biraketa * lokala, barrukoNormala) > 0.99f)
                {
                    return biraketa;
                }
            }

            throw new InvalidOperationException($"{m.Izena}: ez da aurkitu biraketarik {barrukoNormala} normalerako.");
        }

        /// <summary>Neurtutako kaxa, faktoreekin eskalatuta eta gero biratuta. Erroa (0,0,0) puntuan.</summary>
        private static Bounds KaxaKalkulatu(Modulua m, Quaternion biraketa, Vector3 faktoreak)
        {
            Vector3 zentroa = Vector3.Scale(m.Kaxa.center, faktoreak);
            Vector3 e = Vector3.Scale(m.Kaxa.extents, faktoreak);
            var kaxa = new Bounds(biraketa * (zentroa + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(-e.x, -e.y, e.z)));
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(-e.x, e.y, -e.z)));
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(-e.x, e.y, e.z)));
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(e.x, -e.y, -e.z)));
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(e.x, -e.y, e.z)));
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(e.x, e.y, -e.z)));
            kaxa.Encapsulate(biraketa * (zentroa + new Vector3(e.x, e.y, e.z)));
            return kaxa;
        }

        private static void PiezaGehitu(SektorePlana sektorea, string sala, string kategoria, Modulua m, Quaternion biraketa,
            Vector3 faktoreak, Vector3 zentroa, Plana plana, string ezkutatu = null)
        {
            Bounds neurria = KaxaKalkulatu(m, biraketa, faktoreak);
            string bidea = $"{Geometria}/{sala}/{kategoria}";
            string gakoa = $"{bidea}/{m.Izena}";
            sektorea.Kontagailuak.TryGetValue(gakoa, out int indizea);
            sektorea.Kontagailuak[gakoa] = indizea + 1;

            sektorea.Piezak.Add(new Pieza
            {
                Kontainerra = bidea,
                Izena = $"{m.Izena}__{indizea:000}",
                Modulua = m,
                Posizioa = zentroa - neurria.center,
                Biraketa = biraketa * m.ErroBiraketa,
                Eskala = LokalEskala(m, faktoreak, plana),
                Helburua = new Bounds(zentroa, neurria.size),
                Ezkutatu = ezkutatu,
            });
        }

        /// <summary>Renderer gabeko BoxCollider bat: kit-eko egitura-piezek ez dute collider-ik.</summary>
        private static void KolisioGehitu(SektorePlana sektorea, string sala, Vector3 zentroa, Vector3 tamaina)
        {
            string bidea = $"{Geometria}/{sala}/{Kolisioak}";
            sektorea.Kontagailuak.TryGetValue(bidea, out int indizea);
            sektorea.Kontagailuak[bidea] = indizea + 1;
            sektorea.Kolisioak.Add(new KolisioPlana
            {
                Kontainerra = bidea,
                Izena = $"{KolisioIzena}__{indizea:000}",
                Zentroa = zentroa,
                Tamaina = tamaina,
            });
        }

        /// <summary>Munduko ardatzetako faktoreak erroaren eskala lokalera (erroa 90°-ko multiploetan bakarrik).</summary>
        private static Vector3 LokalEskala(Modulua m, Vector3 faktoreak, Plana plana)
        {
            if ((faktoreak - Vector3.one).sqrMagnitude < Tolerantzia * Tolerantzia)
            {
                return m.ErroEskala;
            }

            Vector3 emaitza = m.ErroEskala;
            for (int lokala = 0; lokala < 3; lokala++)
            {
                Vector3 ardatza = Vector3.zero;
                ardatza[lokala] = 1f;
                ardatza = m.ErroBiraketa * ardatza;

                int mundukoa = 0;
                for (int i = 1; i < 3; i++)
                {
                    if (Mathf.Abs(ardatza[i]) > Mathf.Abs(ardatza[mundukoa]))
                    {
                        mundukoa = i;
                    }
                }

                if (Mathf.Abs(ardatza[mundukoa]) < 0.999f)
                {
                    plana?.Erroreak.Add($"{m.Izena}: erroaren biraketa ({V(m.ErroBiraketa.eulerAngles)}) ez da 90°-ren multiploa eta ezin da eskalatu.");
                    return m.ErroEskala;
                }

                emaitza[lokala] = m.ErroEskala[lokala] * faktoreak[mundukoa];
            }

            return emaitza;
        }

        // ---------- Sinkronizazioa: pasa berak balio du aurrebistarako eta aplikatzeko ----------

        private static void Sinkronizatu(Plana plana, bool aplikatu, Scene eszena, Aldaketak a)
        {
            foreach (SektorePlana sektorea in plana.Sektoreak)
            {
                Transform erroa = ErroaSinkronizatu(sektorea, plana.ErroakSortu, aplikatu, eszena, a);
                foreach (GelaPlana gela in sektorea.Gelak)
                {
                    GelaSinkronizatu(sektorea, gela, erroa, aplikatu, a);
                }

                if (sektorea.Geometria)
                {
                    GeometriaSinkronizatu(sektorea, erroa, aplikatu, eszena, a);
                }
            }
        }

        private static Transform ErroaSinkronizatu(SektorePlana sektorea, bool sortuDaiteke, bool aplikatu, Scene eszena, Aldaketak a)
        {
            GameObject aurkitua = eszena.GetRootGameObjects().FirstOrDefault(objektua => objektua.name == sektorea.Izena);
            if (aurkitua == null)
            {
                if (!sortuDaiteke)
                {
                    a.Erroreak.Add($"{sektorea.Izena} ez dago: exekutatu lehenago 'Egitura: Aplikatu'.");
                    return null;
                }

                a.Sortuak.Add(sektorea.Izena);
                if (!aplikatu)
                {
                    return null;
                }

                var berria = new GameObject(sektorea.Izena);
                berria.transform.position = sektorea.Posizioa;
                Undo.RegisterCreatedObjectUndo(berria, sektorea.Izena);
                return berria.transform;
            }

            Transform erroa = aurkitua.transform;
            bool zuzena = Berdin(erroa.position, sektorea.Posizioa)
                          && Berdin(erroa.localScale, Vector3.one)
                          && Quaternion.Angle(erroa.rotation, Quaternion.identity) < 0.05f;
            if (zuzena)
            {
                return erroa;
            }

            if (!sortuDaiteke)
            {
                a.Erroreak.Add($"{sektorea.Izena} ez dago {V(sektorea.Posizioa)} posizioan edo biratuta/eskalatuta dago: exekutatu 'Egitura: Aplikatu'.");
                return erroa;
            }

            a.Eguneratuak.Add($"{sektorea.Izena}: posizioa {V(sektorea.Posizioa)}");
            if (aplikatu)
            {
                Undo.RecordObject(erroa, sektorea.Izena);
                erroa.SetPositionAndRotation(sektorea.Posizioa, Quaternion.identity);
                erroa.localScale = Vector3.one;
            }

            return erroa;
        }

        private static void GelaSinkronizatu(SektorePlana sektorea, GelaPlana gela, Transform erroa, bool aplikatu, Aldaketak a)
        {
            string bidea = $"{sektorea.Izena}/{gela.Izena}";
            Transform aurkitua = erroa != null ? SemeaBilatu(erroa, gela.Izena) : null;
            if (aurkitua == null)
            {
                a.Sortuak.Add($"{bidea}: kaxa {V(gela.Tamaina)} m");
                if (aplikatu)
                {
                    var berria = new GameObject(gela.Izena);
                    berria.transform.SetParent(erroa, false);
                    berria.transform.localPosition = gela.Posizioa;
                    var kaxa = berria.AddComponent<BoxCollider>();
                    GelaKonfiguratu(kaxa, gela);
                    Undo.RegisterCreatedObjectUndo(berria, gela.Izena);
                }

                return;
            }

            var kolisionatzailea = aurkitua.GetComponent<BoxCollider>();
            bool zuzena = kolisionatzailea != null
                          && kolisionatzailea.isTrigger
                          && Berdin(kolisionatzailea.center, gela.Zentroa)
                          && Berdin(kolisionatzailea.size, gela.Tamaina)
                          && Berdin(aurkitua.localPosition, gela.Posizioa)
                          && Berdin(aurkitua.localScale, Vector3.one)
                          && Quaternion.Angle(aurkitua.localRotation, Quaternion.identity) < 0.05f;
            if (zuzena)
            {
                return;
            }

            a.Eguneratuak.Add($"{bidea}: kaxa {V(gela.Tamaina)} m, trigger");
            if (!aplikatu)
            {
                return;
            }

            Undo.RecordObject(aurkitua, gela.Izena);
            aurkitua.localPosition = gela.Posizioa;
            aurkitua.localRotation = Quaternion.identity;
            aurkitua.localScale = Vector3.one;
            if (kolisionatzailea == null)
            {
                kolisionatzailea = Undo.AddComponent<BoxCollider>(aurkitua.gameObject);
            }

            Undo.RecordObject(kolisionatzailea, gela.Izena);
            GelaKonfiguratu(kolisionatzailea, gela);
        }

        private static void GelaKonfiguratu(BoxCollider kaxa, GelaPlana gela)
        {
            kaxa.isTrigger = true;
            kaxa.center = gela.Zentroa;
            kaxa.size = gela.Tamaina;
        }

        private static void GeometriaSinkronizatu(SektorePlana sektorea, Transform erroa, bool aplikatu, Scene eszena, Aldaketak a)
        {
            var mantendu = new HashSet<Transform>();
            var kontainerBerriak = new HashSet<string>();

            foreach (Pieza pieza in sektorea.Piezak)
            {
                Transform kontainerra = KontainerraLortu(sektorea, erroa, pieza.Kontainerra, aplikatu, kontainerBerriak, a);
                string bidea = $"{sektorea.Izena}/{pieza.Kontainerra}/{pieza.Izena}";
                Transform aurkitua = kontainerra != null ? SemeaBilatu(kontainerra, pieza.Izena) : null;

                if (aurkitua != null && PrefabUtility.GetCorrespondingObjectFromSource(aurkitua.gameObject) != pieza.Modulua.Prefab)
                {
                    a.Ezabatuak.Add($"{bidea}: ez da {pieza.Modulua.Izena} prefab-a; ordezkatuko da");
                    if (aplikatu)
                    {
                        Undo.DestroyObjectImmediate(aurkitua.gameObject);
                    }

                    aurkitua = null;
                }

                if (aurkitua == null)
                {
                    a.Sortuak.Add(bidea);
                    if (aplikatu)
                    {
                        mantendu.Add(PiezaSortu(pieza, kontainerra, eszena));
                    }

                    continue;
                }

                mantendu.Add(aurkitua);
                bool zuzena = Berdin(aurkitua.localPosition, pieza.Posizioa)
                              && Berdin(aurkitua.localScale, pieza.Eskala)
                              && Quaternion.Angle(aurkitua.localRotation, pieza.Biraketa) < 0.05f
                              && OrriaEzkutatuta(aurkitua, pieza);
                if (zuzena)
                {
                    continue;
                }

                a.Eguneratuak.Add(bidea);
                if (aplikatu)
                {
                    Undo.RecordObject(aurkitua, pieza.Izena);
                    TransformEzarri(aurkitua, pieza);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(aurkitua);
                    OrriaEzkutatu(aurkitua, pieza);
                }
            }

            foreach (KolisioPlana kolisioa in sektorea.Kolisioak)
            {
                Transform kontainerra = KontainerraLortu(sektorea, erroa, kolisioa.Kontainerra, aplikatu, kontainerBerriak, a);
                string bidea = $"{sektorea.Izena}/{kolisioa.Kontainerra}/{kolisioa.Izena}";
                Transform aurkitua = kontainerra != null ? SemeaBilatu(kontainerra, kolisioa.Izena) : null;
                if (aurkitua == null)
                {
                    a.Sortuak.Add(bidea);
                    if (aplikatu)
                    {
                        var berria = new GameObject(kolisioa.Izena);
                        berria.transform.SetParent(kontainerra, false);
                        berria.transform.localPosition = kolisioa.Zentroa;
                        berria.AddComponent<BoxCollider>().size = kolisioa.Tamaina;
                        Undo.RegisterCreatedObjectUndo(berria, kolisioa.Izena);
                        mantendu.Add(berria.transform);
                    }

                    continue;
                }

                mantendu.Add(aurkitua);
                var kaxa = aurkitua.GetComponent<BoxCollider>();
                if (kaxa != null && Berdin(aurkitua.localPosition, kolisioa.Zentroa) && Berdin(kaxa.size, kolisioa.Tamaina) && Berdin(kaxa.center, Vector3.zero))
                {
                    continue;
                }

                a.Eguneratuak.Add(bidea);
                if (aplikatu)
                {
                    Undo.RecordObject(aurkitua, kolisioa.Izena);
                    aurkitua.localPosition = kolisioa.Zentroa;
                    if (kaxa == null)
                    {
                        kaxa = Undo.AddComponent<BoxCollider>(aurkitua.gameObject);
                    }

                    Undo.RecordObject(kaxa, kolisioa.Izena);
                    kaxa.center = Vector3.zero;
                    kaxa.size = kolisioa.Tamaina;
                }
            }

            ZaharrakEzabatu(sektorea, erroa, mantendu, aplikatu, a);
        }

        private static Transform KontainerraLortu(SektorePlana sektorea, Transform erroa, string bidea, bool aplikatu,
            HashSet<string> berriak, Aldaketak a)
        {
            Transform unekoa = erroa;
            string metatua = sektorea.Izena;
            foreach (string zatia in bidea.Split('/'))
            {
                metatua += $"/{zatia}";
                Transform hurrengoa = unekoa != null ? SemeaBilatu(unekoa, zatia) : null;
                if (hurrengoa == null)
                {
                    if (berriak.Add(metatua))
                    {
                        a.Sortuak.Add($"{metatua}/");
                    }

                    if (!aplikatu)
                    {
                        unekoa = null;
                        continue;
                    }

                    var berria = new GameObject(zatia);
                    berria.transform.SetParent(unekoa, false);
                    Undo.RegisterCreatedObjectUndo(berria, zatia);
                    hurrengoa = berria.transform;
                }
                else if (!Berdin(hurrengoa.localPosition, Vector3.zero)
                         || !Berdin(hurrengoa.localScale, Vector3.one)
                         || Quaternion.Angle(hurrengoa.localRotation, Quaternion.identity) >= 0.05f)
                {
                    if (berriak.Add(metatua))
                    {
                        a.Eguneratuak.Add($"{metatua}/: transformazioa hasieratu");
                    }

                    if (aplikatu)
                    {
                        Undo.RecordObject(hurrengoa, zatia);
                        hurrengoa.localPosition = Vector3.zero;
                        hurrengoa.localRotation = Quaternion.identity;
                        hurrengoa.localScale = Vector3.one;
                    }
                }

                unekoa = hurrengoa;
            }

            return unekoa;
        }

        private static Transform PiezaSortu(Pieza pieza, Transform kontainerra, Scene eszena)
        {
            var instantzia = (GameObject)PrefabUtility.InstantiatePrefab(pieza.Modulua.Prefab, eszena);
            instantzia.name = pieza.Izena;
            instantzia.transform.SetParent(kontainerra, false);
            TransformEzarri(instantzia.transform, pieza);
            OrriaEzkutatu(instantzia.transform, pieza);
            Undo.RegisterCreatedObjectUndo(instantzia, pieza.Izena);
            return instantzia.transform;
        }

        private static bool OrriaEzkutatuta(Transform instantzia, Pieza pieza)
        {
            if (pieza.Ezkutatu == null)
            {
                return true;
            }

            Transform orria = SemeaBilatu(instantzia, pieza.Ezkutatu);
            return orria == null || !orria.gameObject.activeSelf;
        }

        private static void OrriaEzkutatu(Transform instantzia, Pieza pieza)
        {
            if (OrriaEzkutatuta(instantzia, pieza))
            {
                return;
            }

            GameObject orria = SemeaBilatu(instantzia, pieza.Ezkutatu).gameObject;
            Undo.RecordObject(orria, pieza.Izena);
            orria.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(orria);
        }

        private static void TransformEzarri(Transform transformazioa, Pieza pieza)
        {
            transformazioa.localPosition = pieza.Posizioa;
            transformazioa.localRotation = pieza.Biraketa;
            transformazioa.localScale = pieza.Eskala;
        }

        /// <summary>Planean ez dauden IZENA__000 piezak bakarrik; Ion-ek eskuz gehitutakoak ez dira ukitzen.</summary>
        private static void ZaharrakEzabatu(SektorePlana sektorea, Transform erroa, HashSet<Transform> mantendu, bool aplikatu, Aldaketak a)
        {
            Transform geometria = erroa != null ? SemeaBilatu(erroa, Geometria) : null;
            if (geometria == null)
            {
                return;
            }

            HashSet<string> planekoak = sektorea.Piezak.Select(pieza => $"{pieza.Kontainerra}/{pieza.Izena}").ToHashSet();
            planekoak.UnionWith(sektorea.Kolisioak.Select(kolisioa => $"{kolisioa.Kontainerra}/{kolisioa.Izena}"));
            foreach (Transform objektua in geometria.GetComponentsInChildren<Transform>(true))
            {
                if (objektua == null || !PiezaIzena.IsMatch(objektua.name) || mantendu.Contains(objektua))
                {
                    continue;
                }

                string erlatiboa = TresnaLaguntzailea.Bidea(objektua, erroa);
                erlatiboa = erlatiboa.Substring(erlatiboa.IndexOf('/') + 1);
                if (!aplikatu && planekoak.Contains(erlatiboa))
                {
                    continue;
                }

                a.Ezabatuak.Add($"{sektorea.Izena}/{erlatiboa}");
                if (aplikatu)
                {
                    Undo.DestroyObjectImmediate(objektua.gameObject);
                }
            }
        }

        private static List<string> Egiaztatu(Plana plana, Scene eszena)
        {
            var hutsak = new List<string>();
            foreach (SektorePlana sektorea in plana.Sektoreak)
            {
                GameObject erroa = eszena.GetRootGameObjects().First(objektua => objektua.name == sektorea.Izena);
                foreach (Pieza pieza in sektorea.Piezak)
                {
                    Transform kontainerra = erroa.transform.Find(pieza.Kontainerra);
                    Transform aurkitua = kontainerra != null ? SemeaBilatu(kontainerra, pieza.Izena) : null;
                    if (aurkitua == null)
                    {
                        hutsak.Add($"{sektorea.Izena}/{pieza.Kontainerra}/{pieza.Izena}: ez dago");
                        continue;
                    }

                    List<Renderer> rendererrak = GeometriaRendererrak(aurkitua.gameObject);
                    if (rendererrak.Count == 0)
                    {
                        continue;
                    }

                    Bounds benetakoa = rendererrak[0].bounds;
                    foreach (Renderer renderer in rendererrak)
                    {
                        benetakoa.Encapsulate(renderer.bounds);
                    }

                    Vector3 esperoZentroa = sektorea.Posizioa + pieza.Helburua.center;
                    bool ondo = (benetakoa.center - esperoZentroa).magnitude < EgiaztapenTolerantzia
                                && (benetakoa.size - pieza.Helburua.size).magnitude < EgiaztapenTolerantzia;
                    if (!ondo)
                    {
                        hutsak.Add($"{sektorea.Izena}/{pieza.Kontainerra}/{pieza.Izena}: benetakoa {V(benetakoa.center)} / {V(benetakoa.size)}; esperotakoa {V(esperoZentroa)} / {V(pieza.Helburua.size)}");
                    }
                }
            }

            return hutsak;
        }

        // ---------- Laguntzaileak ----------

        private static Transform SemeaBilatu(Transform gurasoa, string izena)
        {
            for (int i = 0; i < gurasoa.childCount; i++)
            {
                Transform semea = gurasoa.GetChild(i);
                if (semea.name == izena)
                {
                    return semea;
                }
            }

            return null;
        }

        private static bool Berdin(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < Tolerantzia * Tolerantzia;

        private static string F(float balioa) => balioa.ToString("0.00", CultureInfo.InvariantCulture);

        private static string V(Vector3 v) => $"({F(v.x)}, {F(v.y)}, {F(v.z)})";

        // ---------- Datuak ----------

        private sealed class SektoreDef
        {
            public readonly int Zenbakia;
            public readonly Vector3 Posizioa;
            public readonly SalaDef Ezkerra;
            public readonly int Sekzioak;
            public readonly SalaDef Eskuina;

            public SektoreDef(int zenbakia, Vector3 posizioa, SalaDef ezkerra, int sekzioak, SalaDef eskuina)
            {
                Zenbakia = zenbakia;
                Posizioa = posizioa;
                Ezkerra = ezkerra;
                Sekzioak = sekzioak;
                Eskuina = eskuina;
            }
        }

        private sealed class SalaDef
        {
            public readonly string Izena;
            public readonly int Lauzak;
            public readonly int Panelak;

            public SalaDef(string izena, int lauzak, int panelak)
            {
                Izena = izena;
                Lauzak = lauzak;
                Panelak = panelak;
            }
        }

        private sealed class PropDef
        {
            public readonly string Sala;
            public readonly string Karpeta;
            public readonly string Prefab;
            public readonly float U;
            public readonly float V;
            public readonly float Yaw;
            public readonly bool KolisioaBehar;

            public PropDef(string sala, string karpeta, string prefab, float u, float v, float yaw, bool kolisioaBehar)
            {
                Sala = sala;
                Karpeta = karpeta;
                Prefab = prefab;
                U = u;
                V = v;
                Yaw = yaw;
                KolisioaBehar = kolisioaBehar;
            }
        }

        /// <summary>Kit-aren neurri errealak, eskala aplikatuta.</summary>
        private sealed class Neurriak
        {
            public float Panela;
            public float PanelAltuera;
            public float HormaLodiera;
            public Vector3 Lauza;
            public float SalaAltuera;
            public float SekzioLuzera;
            public float AteLuzera;
            public float MuturLuzera;
            public float HodiErdiaY;
            public float HodiAltuera;
            public float HodiBarruErdia;
            public float AteZabalera;
            public float AteAltuera;
            public float MarkoLodiera;
        }

        private sealed class KolisioPlana
        {
            public string Kontainerra;
            public string Izena;
            public Vector3 Zentroa;
            public Vector3 Tamaina;
        }

        private sealed class Modulua
        {
            public string Izena;
            public GameObject Prefab;
            public Bounds Kaxa;
            public Dictionary<string, Bounds> Zatiak;
            public Quaternion ErroBiraketa;
            public Vector3 ErroEskala;
            public int RendererKop;
            public int ColliderKop;
            public bool RigidbodyDu;
            public int ArgiKop;
        }

        private sealed class Plana
        {
            public bool ErroakSortu;
            public readonly List<SektorePlana> Sektoreak = new List<SektorePlana>();
            public readonly List<string> Erroreak = new List<string>();
            public readonly List<string> Oharrak = new List<string>();
            public readonly List<string> SalaLerroak = new List<string>();
            public readonly List<string> ModuluLerroak = new List<string>();
        }

        private sealed class SektorePlana
        {
            public string Izena;
            public Vector3 Posizioa;
            public bool Geometria;
            public readonly List<GelaPlana> Gelak = new List<GelaPlana>();
            public readonly List<Pieza> Piezak = new List<Pieza>();
            public readonly List<KolisioPlana> Kolisioak = new List<KolisioPlana>();
            public readonly Dictionary<string, int> Kontagailuak = new Dictionary<string, int>();
        }

        private sealed class SalaPlana
        {
            public string Izena;
            public int Nx;
            public int Nz;
            public float XMin;
            public float XMax;
            public float ZMin;
            public float ZMax;
            public readonly List<AteGunea> Ateak = new List<AteGunea>();
        }

        private sealed class AteGunea
        {
            public Vector3 BarrukoNormala;
            public float Erdia;
        }

        private sealed class GelaPlana
        {
            public string Izena;
            public Vector3 Posizioa;
            public Vector3 Zentroa;
            public Vector3 Tamaina;
        }

        private sealed class Pieza
        {
            public string Kontainerra;
            public string Izena;
            public Modulua Modulua;
            public Vector3 Posizioa;
            public Quaternion Biraketa;
            public Vector3 Eskala;
            public Bounds Helburua;
            public string Ezkutatu;
            public bool? HodiArgiaPiztuta;
            public Color? HodiArgiKolorea;
        }

        private sealed class Aldaketak
        {
            public readonly List<string> Sortuak = new List<string>();
            public readonly List<string> Eguneratuak = new List<string>();
            public readonly List<string> Ezabatuak = new List<string>();
            public readonly List<string> Erroreak = new List<string>();

            public int Guztira => Sortuak.Count + Eguneratuak.Count + Ezabatuak.Count;
            public string Laburpena => $"{Sortuak.Count} sortu, {Eguneratuak.Count} eguneratu, {Ezabatuak.Count} ezabatu";
        }
    }
}
