using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Irakurtzeko tresna: argazkiak eta informeak docs/qa/ karpetan. Ez du eszenarik gordetzen eta
    /// argiztapen-aldaketa guztiak gorde gabeko eszena-saio honetan bakarrik geratzen dira.
    /// Batchmode-n exekutatzen da, Unity itxita dagoela.
    /// </summary>
    public static class EbidentziaKapturak
    {
        private const string Tresna = "EbidentziaKapturak";
        private const string Marka = "ECL_EBIDENTZIAKAPTURAK";
        private const string MagixBox = "Assets/ThirdParty/Sci Fi Modular Pack/Prefabs/";
        private const string EszenaBidea = "Assets/_Eclipse/Eszenak/01_Instalazioa.unity";

        private static readonly string[] Moduluak =
        {
            "Starter/Profile1", "Starter/ProfileDoor", "ProfileEnd", "ProfileWindow", "WallDoor",
            "Wall12", "Floor1", "TopWall4", "DoorFrame", "WallIntersection",
        };

        private static string Karpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "qa", "kapturak"));

        /// <summary>Modulu bakoitza: parteen zerrenda (informea) eta bi ikuspegi aurkakoak (irudi bakarra).</summary>
        public static void BatchModuluak()
        {
            int kodea = 1;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ArgiNeutroa();
                Camera kamera = KameraSortu();
                var informea = new Informea(Tresna, Marka, "Moduluak", "(eszena hutsa, gorde gabe)", "MagixBox moduluak");

                const int Zabalera = 400;
                const int Altuera = 300;
                var orria = new Texture2D(Zabalera * Moduluak.Length / 2, Altuera * 4, TextureFormat.RGB24, false);
                for (int i = 0; i < Moduluak.Length; i++)
                {
                    string bidea = $"{MagixBox}{Moduluak[i]}.prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
                    if (prefab == null)
                    {
                        informea.Lerroa($"- **FALTA:** {bidea}");
                        continue;
                    }

                    var instantzia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instantzia.transform.position = Vector3.zero;
                    Bounds kaxa = KaxaLortu(instantzia);
                    informea.Atala($"{prefab.name} {V(kaxa.size)}", PartenLerroak(instantzia));

                    int zutabea = i % (Moduluak.Length / 2);
                    int blokea = i / (Moduluak.Length / 2);
                    Kokatu(kamera, kaxa, new Vector3(1f, 0.5f, -1f));
                    Itsatsi(orria, Argazkia(kamera, Zabalera, Altuera), zutabea * Zabalera, (3 - blokea * 2) * Altuera);
                    Kokatu(kamera, kaxa, new Vector3(-1f, 0.5f, 1f));
                    Itsatsi(orria, Argazkia(kamera, Zabalera, Altuera), zutabea * Zabalera, (2 - blokea * 2) * Altuera);

                    UnityEngine.Object.DestroyImmediate(instantzia);
                }

                Directory.CreateDirectory(Karpeta);
                string fitxategia = Path.Combine(Karpeta, "moduluak.png");
                orria.Apply();
                File.WriteAllBytes(fitxategia, orria.EncodeToPNG());
                informea.Lerroa(string.Empty);
                informea.Lerroa($"Argazkia: {fitxategia}");
                informea.AmaituOndo("moduluak neurtuta eta argazkian");
                kodea = 0;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>
        /// 01_Instalazioa-ren argazkiak: gainetik (sabairik gabe) eta barrutik. Argi neutroa gehitzen du
        /// geometria ikusteko; eszena ez da gordetzen.
        /// </summary>
        public static void BatchEszena()
        {
            int kodea = 1;
            try
            {
                Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                ArgiNeutroa();
                Camera kamera = KameraSortu();
                Directory.CreateDirectory(Karpeta);

                var barrukoak = new List<string>();
                foreach (GameObject erroa in eszena.GetRootGameObjects().Where(o => o.name.StartsWith("Sektorea", StringComparison.Ordinal)).OrderBy(o => o.name))
                {
                    Bounds sektorea = KaxaLortu(erroa);
                    var sabaiak = erroa.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Sabaia").ToList();
                    sabaiak.ForEach(t => t.gameObject.SetActive(false));

                    kamera.orthographic = true;
                    kamera.orthographicSize = Mathf.Max(sektorea.size.z * 0.55f, sektorea.size.x * 0.55f * 9f / 16f);
                    kamera.transform.position = sektorea.center + Vector3.up * 60f;
                    kamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Gorde(Argazkia(kamera, 1280, 720), $"{erroa.name}_planta.png");
                    kamera.orthographic = false;

                    sabaiak.ForEach(t => t.gameObject.SetActive(true));

                    foreach (BoxCollider gela in erroa.GetComponentsInChildren<BoxCollider>(true).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)).OrderBy(k => k.name))
                    {
                        Vector3 erdia = gela.transform.TransformPoint(gela.center);
                        Vector3 begia = new Vector3(erdia.x - gela.size.x * 0.42f, 1.6f, erdia.z - gela.size.z * 0.42f);
                        kamera.transform.position = begia;
                        kamera.transform.rotation = Quaternion.LookRotation(new Vector3(erdia.x, 1.3f, erdia.z) - begia);
                        Gorde(Argazkia(kamera, 960, 540), $"{gela.name}.png");
                        barrukoak.Add(gela.name);
                    }
                }

                var informea = new Informea(Tresna, Marka, "Eszena", EszenaBidea, "Sektore guztiak");
                informea.Atala("Barruko argazkiak", barrukoak);
                informea.Lerroa($"Karpeta: {Karpeta}");
                informea.AmaituOndo($"{barrukoak.Count} gela argazkian (eszena ez da gorde)");
                kodea = 0;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>
        /// 01_Instalazioa-ren egiaztapen fisikoa, gorde gabe: 1,8 m-ko kapsula sektore bakoitzean, muturretik
        /// muturrera ateetatik; zorua ibilbide osoan; salak itxita; prefab-loturak eta materialak.
        /// </summary>
        public static void BatchEgiaztapena()
        {
            int kodea = 1;
            try
            {
                Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                Physics.SyncTransforms();
                var informea = new Informea(Tresna, Marka, "Egiaztapena", EszenaBidea, "Sektore guztiak");
                var hutsak = new List<string>();
                var emaitzak = new List<string>();

                foreach (GameObject erroa in eszena.GetRootGameObjects().Where(o => o.name.StartsWith("Sektorea", StringComparison.Ordinal)).OrderBy(o => o.name))
                {
                    Egiaztatu(erroa, emaitzak, hutsak);
                }

                informea.Atala("Emaitzak", emaitzak);
                informea.Atala("HUTSAK", hutsak);
                if (hutsak.Count > 0)
                {
                    informea.AmaituErrorearekin($"{hutsak.Count} huts");
                }
                else
                {
                    informea.AmaituOndo($"0 huts; {string.Join(" | ", emaitzak)}");
                    kodea = 0;
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        private static void Egiaztatu(GameObject erroa, List<string> emaitzak, List<string> hutsak)
        {
            const float Erradioa = 0.3f;
            const float Altuera = 1.8f;
            const float Urratsa = 0.05f;

            List<BoxCollider> gelak = erroa.GetComponentsInChildren<BoxCollider>(true)
                .Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal))
                .OrderBy(k => k.transform.position.x)
                .ToList();
            float z = erroa.transform.position.z;
            float x0 = gelak.First().transform.position.x;
            float x1 = gelak.Last().transform.position.x;

            // Lehen gela korridorea bada (S3), ibilbidea haren muturretik hasten da, tapatik tarte batera.
            int urratsak = 0;
            int talkak = 0;
            int zoruGabe = 0;
            string lehenTalka = null;
            for (float x = x0; x <= x1; x += Urratsa)
            {
                urratsak++;
                var behea = new Vector3(x, Erradioa + 0.02f, z);
                var goia = new Vector3(x, Altuera - Erradioa + 0.02f, z);
                if (Physics.CheckCapsule(behea, goia, Erradioa, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    talkak++;
                    lehenTalka ??= $"x={x:0.00}";
                }

                bool zorua = Physics.Raycast(new Vector3(x, 0.5f, z), Vector3.down, out RaycastHit jo, 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                             && Mathf.Abs(jo.point.y) < 0.05f;
                if (!zorua)
                {
                    zoruGabe++;
                }
            }

            if (talkak > 0)
            {
                hutsak.Add($"{erroa.name}: kapsulak {talkak} talka ibilbidean (lehena {lehenTalka})");
            }

            if (zoruGabe > 0)
            {
                hutsak.Add($"{erroa.name}: {zoruGabe} urrats zorurik gabe");
            }

            // Itxitura: gela bakoitzaren erditik 32 norabidetan, zerbaitek gelditu behar du izpia.
            int ihesak = 0;
            foreach (BoxCollider gela in gelak)
            {
                Vector3 jatorria = gela.transform.position + Vector3.up * 1f;
                for (int i = 0; i < 32; i++)
                {
                    Vector3 norabidea = Quaternion.Euler(0f, i * 360f / 32f, 0f) * Vector3.forward;
                    if (!Physics.Raycast(jatorria, norabidea, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        ihesak++;
                    }
                }
            }

            if (ihesak > 0)
            {
                hutsak.Add($"{erroa.name}: {ihesak} izpik ihes egiten dute (sala ez dago itxita)");
            }

            // Prefab-loturak eta materialak.
            int instantziak = 0;
            int etenak = 0;
            foreach (Transform t in erroa.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                {
                    instantziak++;
                    if (PrefabUtility.GetPrefabInstanceStatus(t.gameObject) != PrefabInstanceStatus.Connected)
                    {
                        etenak++;
                    }
                }
            }

            int materialTxarrak = erroa.GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.sharedMaterials)
                .Count(m => m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader");
            if (etenak > 0)
            {
                hutsak.Add($"{erroa.name}: {etenak} prefab-lotura eten");
            }

            if (materialTxarrak > 0)
            {
                hutsak.Add($"{erroa.name}: {materialTxarrak} material oker");
            }

            emaitzak.Add($"{erroa.name}: {gelak.Count} gela, {instantziak} prefab, {urratsak} urrats, {talkak} talka, {zoruGabe} zorurik gabe, {ihesak} ihes, {materialTxarrak} material oker");
        }

        /// <summary>Kamera finkoak, Gela_* kaxetatik kalkulatuta: konparaketek enkoadraketa bera dute.</summary>
        private static List<(string Izena, Vector3 Begia, Vector3 Helburua)> KameraFinkoak(Scene eszena)
        {
            var emaitza = new List<(string, Vector3, Vector3)>();
            foreach (BoxCollider gela in GelaGuztiak(eszena))
            {
                Vector3 erdia = gela.transform.TransformPoint(gela.center);
                Vector3 s = gela.size;
                var begia = new Vector3(erdia.x - s.x * 0.42f, Mathf.Min(1.7f, s.y * 0.6f), erdia.z - s.z * 0.42f);
                if (gela.name.Contains("Korridorea"))
                {
                    begia = new Vector3(erdia.x - s.x * 0.45f, 1.5f, erdia.z);
                }

                emaitza.Add((gela.name, begia, new Vector3(erdia.x, 1.3f, erdia.z)));
            }

            return emaitza;
        }

        private static List<BoxCollider> GelaGuztiak(Scene eszena)
        {
            return eszena.GetRootGameObjects()
                .Where(o => o.name.StartsWith("Sektorea", StringComparison.Ordinal))
                .OrderBy(o => o.name)
                .SelectMany(o => o.GetComponentsInChildren<BoxCollider>(true))
                .Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal))
                .ToList();
        }

        /// <summary>Argiztapen errealarekin, argi neutrorik gabe. Aurrizkia: E1_bake_gabe edo E1_bake_bai.</summary>
        internal static void ArgiztapenArgazkiak(string aurrizkia, Scene eszena)
        {
            Directory.CreateDirectory(Karpeta);
            Camera kamera = KameraSortu();
            foreach ((string izena, Vector3 begia, Vector3 helburua) in KameraFinkoak(eszena))
            {
                kamera.transform.position = begia;
                kamera.transform.rotation = Quaternion.LookRotation(helburua - begia);
                Gorde(Argazkia(kamera, 1280, 720), $"{aurrizkia}_{izena}.png");
            }

            UnityEngine.Object.DestroyImmediate(kamera.gameObject);
        }

        /// <summary>
        /// Bake ondoko ebidentziak, gorde gabe: lightmap-a, Light Probe-ak objektu dinamiko batean, sarearen planta, eskala, normalak eta UV-ak.
        /// </summary>
        public static void BatchArgiztapena()
        {
            int kodea = 1;
            try
            {
                Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                Directory.CreateDirectory(Karpeta);
                var informea = new Informea(Tresna, Marka, "Argiztapena", EszenaBidea, "Sektore guztiak");

                ArgiztapenArgazkiak("E1_bake_bai", eszena);
                int atlasak = AtlasakGorde();
                informea.Lerroa($"- **Lightmap kopurua:** {atlasak}");

                Camera kamera = KameraSortu();
                BoxCollider lab = GelaGuztiak(eszena).First(g => g.name == "Gela_S1_Laborategia");
                Vector3 labErdia = lab.transform.TransformPoint(lab.center);

                ReflectionProbe probe = lab.GetComponentInChildren<ReflectionProbe>(true);
                informea.Lerroa($"- **Laborategiko Reflection Probe:** {(probe != null ? $"{probe.mode}, box {probe.boxProjection}, {probe.size}, testura: {(probe.bakedTexture != null ? probe.bakedTexture.name : "EZ")}" : "EZ DAGO")}");
                if (LightmapSettings.lightProbes != null)
                {
                    int beltzak = LightmapSettings.lightProbes.bakedProbes.Count(sh => sh[0, 0] + sh[1, 0] + sh[2, 0] < 0.0005f);
                    informea.Lerroa($"- **Light Probe horneatuak:** {LightmapSettings.lightProbes.bakedProbes.Length}; beltzak: {beltzak}");
                }

                Vector3 begia;

                // Light Probe-ak: esfera dinamikoak argi-itzal trantsizio batean.
                var esferak = new List<GameObject>();
                for (int i = 0; i < 5; i++)
                {
                    GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    esfera.transform.localScale = Vector3.one * 0.9f;
                    esfera.transform.position = new Vector3(labErdia.x - 6f + i * 3f, 0.55f, 0.6f);
                    esferak.Add(esfera);
                }

                var kamEsfera = new Vector3(labErdia.x, 1.7f, -5.2f);
                kamera.transform.position = kamEsfera;
                kamera.transform.rotation = Quaternion.LookRotation(esferak[2].transform.position - kamEsfera);
                esferak.ForEach(e => e.GetComponent<Renderer>().lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes);
                Gorde(Argazkia(kamera, 1280, 720), "E3_lightprobe_bai.png");
                esferak.ForEach(e => e.GetComponent<Renderer>().lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off);
                Gorde(Argazkia(kamera, 1280, 720), "E3_lightprobe_ez.png");
                esferak.ForEach(UnityEngine.Object.DestroyImmediate);

                // Eskala: 1,8 m-ko kapsula atearen ondoan.
                GameObject kapsula = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                kapsula.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                kapsula.transform.position = new Vector3(lab.transform.position.x - lab.size.x * 0.5f + 1.6f, 0.9f, 1.8f);
                begia = new Vector3(lab.transform.position.x - lab.size.x * 0.5f + 5.5f, 1.6f, 2.2f);
                kamera.transform.position = begia;
                kamera.transform.rotation = Quaternion.LookRotation(new Vector3(lab.transform.position.x - lab.size.x * 0.5f, 1.4f, 0.4f) - begia);
                Gorde(Argazkia(kamera, 1280, 720), "E4_eskala_kapsula.png");
                UnityEngine.Object.DestroyImmediate(kapsula);

                // Light Probe sarea: planta, sabairik gabe, esfera txikiekin.
                var markak = new List<GameObject>();
                foreach (LightProbeGroup taldea in UnityEngine.Object.FindObjectsByType<LightProbeGroup>(FindObjectsInactive.Include))
                {
                    foreach (Vector3 p in taldea.probePositions)
                    {
                        GameObject marka = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        marka.transform.localScale = Vector3.one * 0.3f;
                        marka.transform.position = taldea.transform.TransformPoint(p);
                        markak.Add(marka);
                    }
                }

                ArgiNeutroa();
                GameObject s1 = eszena.GetRootGameObjects().First(o => o.name == "Sektorea1");
                List<Transform> sabaiak = s1.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Sabaia").ToList();
                sabaiak.ForEach(t => t.gameObject.SetActive(false));
                Bounds kaxa = KaxaLortu(s1);
                kamera.orthographic = true;
                kamera.orthographicSize = Mathf.Max(kaxa.size.z * 0.55f, kaxa.size.x * 0.55f * 9f / 16f);
                kamera.transform.position = kaxa.center + Vector3.up * 60f;
                kamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Gorde(Argazkia(kamera, 1600, 900), "E5_probe_sarea_planta.png");
                kamera.orthographic = false;
                sabaiak.ForEach(t => t.gameObject.SetActive(true));
                markak.ForEach(UnityEngine.Object.DestroyImmediate);
                informea.Lerroa($"- **Light Probe kopurua:** {markak.Count}");

                // Normalak eta UV-ak: materialak ordezkatuta saio honetan bakarrik.
                DiagnostikoArgazkiak(eszena, kamera, labErdia);

                informea.Lerroa($"- **Karpeta:** {Karpeta}");
                informea.AmaituOndo($"{atlasak} lightmap; argazkiak gordeta (eszena ez da gorde)");
                kodea = atlasak > 0 ? 0 : 1;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>
        /// E2: Reflection Probe bai/ez eta Box Projection bai/ez (-eclipseRPModua bai|box_ez|ez), gorde gabe.
        /// Isla-sistemak editorearen tick-ak behar ditu aldaketa hartzeko: bestela argazkiak berdinak ateratzen dira.
        /// Horregatik metodoa ez da berehala amaitzen: EditorApplication.update-k kontatzen du eta gero argazkia ateratzen du.
        /// </summary>
        public static void BatchRP()
        {
            try
            {
                string[] argumentuak = Environment.GetCommandLineArgs();
                int indizea = Array.IndexOf(argumentuak, "-eclipseRPModua");
                string modua = indizea >= 0 && indizea + 1 < argumentuak.Length ? argumentuak[indizea + 1] : "bai";

                Scene eszena = EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                Directory.CreateDirectory(Karpeta);
                ReflectionProbe[] probeak = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include);
                if (modua == "ez")
                {
                    Array.ForEach(probeak, p => p.gameObject.SetActive(false));
                }
                else if (modua == "box_ez")
                {
                    Array.ForEach(probeak, p => p.boxProjection = false);
                }

                int tickak = 0;
                EditorApplication.CallbackFunction urratsa = null;
                urratsa = () =>
                {
                    if (++tickak < 40)
                    {
                        return;
                    }

                    EditorApplication.update -= urratsa;
                    int kodea = 1;
                    try
                    {
                        RPArgazkiak(eszena, modua, probeak.Length);
                        kodea = 0;
                    }
                    catch (Exception salbuespena)
                    {
                        Debug.LogError($"{Marka}_ERROR {salbuespena}");
                    }

                    EditorApplication.Exit(kodea);
                };
                EditorApplication.update += urratsa;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void RPArgazkiak(Scene eszena, string modua, int kopurua)
        {
            BoxCollider lab = GelaGuztiak(eszena).First(g => g.name == "Gela_S1_Laborategia");
            Vector3 erdia = lab.transform.TransformPoint(lab.center);
            Camera kamera = KameraSortu();

            var begia = new Vector3(erdia.x - 3.5f, 0.75f, -1.5f);
            kamera.transform.position = begia;
            kamera.transform.rotation = Quaternion.LookRotation(new Vector3(erdia.x + 1f, 0.5f, 3.5f) - begia);
            Gorde(Argazkia(kamera, 1280, 720), $"E2_zorua_{modua}.png");

            GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.transform.localScale = Vector3.one * 2f;
            esfera.transform.position = new Vector3(erdia.x, 1.2f, -3f);
            var ispilua = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ispilua.SetFloat("_Metallic", 1f);
            ispilua.SetFloat("_Smoothness", 0.98f);
            ispilua.SetColor("_BaseColor", Color.white);
            esfera.GetComponent<Renderer>().sharedMaterial = ispilua;
            begia = new Vector3(erdia.x - 3f, 1.6f, -6.5f);
            kamera.transform.position = begia;
            kamera.transform.rotation = Quaternion.LookRotation(esfera.transform.position - begia);
            Gorde(Argazkia(kamera, 1280, 720), $"E2_ispilua_{modua}.png");

            var informea = new Informea(Tresna, Marka, $"RP {modua}", EszenaBidea, "Laborategia");
            informea.Lerroa($"- **Probe kopurua:** {kopurua}; modua: {modua}");
            informea.AmaituOndo($"E2 {modua} (eszena ez da gorde)");
        }

        private static int AtlasakGorde()
        {
            LightmapData[] mapak = LightmapSettings.lightmaps;
            for (int i = 0; i < mapak.Length; i++)
            {
                Texture2D atlasa = mapak[i].lightmapColor;
                if (atlasa == null)
                {
                    continue;
                }

                int tamaina = Mathf.Min(atlasa.width, 1536);
                var helburua = new RenderTexture(tamaina, tamaina, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(atlasa, helburua);
                RenderTexture.active = helburua;
                var irudia = new Texture2D(tamaina, tamaina, TextureFormat.RGB24, false);
                irudia.ReadPixels(new Rect(0, 0, tamaina, tamaina), 0, 0);
                irudia.Apply();
                RenderTexture.active = null;
                helburua.Release();
                Gorde(irudia, $"E6_lightmap_{i}.png");
            }

            return mapak.Length;
        }

        private static void DiagnostikoArgazkiak(Scene eszena, Camera kamera, Vector3 labErdia)
        {
            Shader shader = Shader.Find("Hidden/Eclipse/Diag");
            if (shader == null)
            {
                Debug.LogWarning($"{Marka} Hidden/Eclipse/Diag ez da aurkitu: normalen eta UV-en argazkirik gabe.");
                return;
            }

            var materiala = new Material(shader);
            foreach (GameObject erroa in eszena.GetRootGameObjects().Where(o => o.name.StartsWith("Sektorea", StringComparison.Ordinal)))
            {
                foreach (Renderer r in erroa.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterials = Enumerable.Repeat(materiala, r.sharedMaterials.Length).ToArray();
                }
            }

            var begia = new Vector3(labErdia.x - 6.5f, 1.9f, -3f);
            kamera.transform.position = begia;
            kamera.transform.rotation = Quaternion.LookRotation(new Vector3(labErdia.x + 1f, 1.5f, 3.5f) - begia);
            foreach ((float modua, string izena) in new[] { (0f, "E7_normalak"), (1f, "E7_uv0"), (2f, "E7_uv2") })
            {
                materiala.SetFloat("_Modua", modua);
                Gorde(Argazkia(kamera, 1280, 720), $"{izena}.png");
            }
        }

        private static void ArgiNeutroa()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);
            var objektua = new GameObject("KapturaArgia");
            var argia = objektua.AddComponent<Light>();
            argia.type = LightType.Directional;
            argia.intensity = 0.8f;
            objektua.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        /// <summary>
        /// Kit-eko Profile tartearen puntu-argi bakoitzak 6 itzal-mapa ditu: eszena osoan 79 mapa, eta prozesu batek
        /// Camera.Render()-en ordubete eman zuen zintzilik. Argazkietarako itzal horiek kentzen dira, saio honetan bakarrik
        /// (eszena ez da gordetzen). Argi propioek eta bake-ak ez dute aldaketarik.
        /// </summary>
        private static int KitItzalakKendu()
        {
            int kenduak = 0;
            foreach (Light argia in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (argia.type == LightType.Point && argia.lightmapBakeType == LightmapBakeType.Realtime
                    && argia.shadows != LightShadows.None && !argia.name.StartsWith("Argia_", StringComparison.Ordinal))
                {
                    argia.shadows = LightShadows.None;
                    kenduak++;
                }
            }

            Debug.Log($"{Marka}_ITZALAK {kenduak} puntu-argiri itzalak kenduta (saio honetan bakarrik)");
            return kenduak;
        }

        private static Camera KameraSortu()
        {
            KitItzalakKendu();
            var objektua = new GameObject("KapturaKamera");
            var kamera = objektua.AddComponent<Camera>();
            kamera.clearFlags = CameraClearFlags.SolidColor;
            kamera.backgroundColor = new Color(0.12f, 0.16f, 0.25f);
            kamera.fieldOfView = 70f;
            kamera.nearClipPlane = 0.05f;
            kamera.farClipPlane = 300f;
            kamera.enabled = false;

            // Saioko lehen errendatzea beltz ateratzen da: lehen hau bota egiten da.
            Argazkia(kamera, 64, 64);
            return kamera;
        }

        private static void Kokatu(Camera kamera, Bounds kaxa, Vector3 norabidea)
        {
            float distantzia = Mathf.Max(kaxa.extents.magnitude * 2.2f, 1f);
            kamera.transform.position = kaxa.center + norabidea.normalized * distantzia;
            kamera.transform.rotation = Quaternion.LookRotation(kaxa.center - kamera.transform.position);
        }

        private static Texture2D Argazkia(Camera kamera, int zabalera, int altuera)
        {
            var helburua = new RenderTexture(zabalera, altuera, 24);
            kamera.targetTexture = helburua;
            kamera.Render();
            GL.Flush();
            RenderTexture.active = helburua;
            var irudia = new Texture2D(zabalera, altuera, TextureFormat.RGB24, false);
            irudia.ReadPixels(new Rect(0, 0, zabalera, altuera), 0, 0);
            irudia.Apply();
            RenderTexture.active = null;
            kamera.targetTexture = null;
            helburua.Release();
            return irudia;
        }

        private static void Itsatsi(Texture2D orria, Texture2D irudia, int x, int y)
        {
            orria.SetPixels(x, y, irudia.width, irudia.height, irudia.GetPixels());
        }

        private static void Gorde(Texture2D irudia, string izena)
        {
            // Irudi uniformea ez da ebidentzia: errendatzeak huts egin du (adib. Render Graph errorea).
            Color32[] pixelak = irudia.GetPixels32();
            if (pixelak.All(p => p.r == pixelak[0].r && p.g == pixelak[0].g && p.b == pixelak[0].b))
            {
                throw new InvalidOperationException($"{izena}: irudi uniformea; errendatzeak huts egin du.");
            }

            File.WriteAllBytes(Path.Combine(Karpeta, izena), irudia.EncodeToPNG());
        }

        private static Bounds KaxaLortu(GameObject objektua)
        {
            Renderer[] rendererrak = objektua.GetComponentsInChildren<Renderer>(false);
            if (rendererrak.Length == 0)
            {
                return new Bounds(objektua.transform.position, Vector3.one);
            }

            Bounds kaxa = rendererrak[0].bounds;
            foreach (Renderer renderer in rendererrak)
            {
                kaxa.Encapsulate(renderer.bounds);
            }

            return kaxa;
        }

        private static List<string> PartenLerroak(GameObject instantzia)
        {
            var lerroak = new List<string>();
            foreach (Renderer renderer in instantzia.GetComponentsInChildren<Renderer>(true))
            {
                string prefabIzena = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject) is GameObject erroa
                                     && erroa != instantzia
                    ? $" [prefab: {erroa.name}]"
                    : string.Empty;
                lerroak.Add($"{TresnaLaguntzailea.Bidea(renderer.transform, instantzia.transform)}: {V(renderer.bounds.size)} zentroa {V(renderer.bounds.center)}{prefabIzena}");
            }

            foreach (Light argia in instantzia.GetComponentsInChildren<Light>(true))
            {
                lerroak.Add($"LIGHT {TresnaLaguntzailea.Bidea(argia.transform, instantzia.transform)}: {argia.type}, {argia.lightmapBakeType}, tartea {F(argia.range)}, intentsitatea {F(argia.intensity)}, zentroa {V(argia.transform.position)}");
            }

            return lerroak;
        }

        private static string F(float balioa) => balioa.ToString("0.00", CultureInfo.InvariantCulture);

        private static string V(Vector3 v) => $"({F(v.x)}, {F(v.y)}, {F(v.z)})";
    }
}
