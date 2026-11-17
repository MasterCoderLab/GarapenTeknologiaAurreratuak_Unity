using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Jokalaria;
using Eclipse.Zinematika;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Pieza = Eclipse.EditorTresnak.AtrezzoPiezak;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3 (F blokea): amaierako zinematikaren prototipoa, eszena bereizi batean (07_AmaieraZinematika). Erreaktorearen eszena ez da ukitzen.
    /// Basamortu arrotza (Terrain prozedurala, zeru prozedurala, lainoa, ilargiak, harkaitz-orratzak eta arkua), estazioaren fatxada eta
    /// atea, ontzi propioa (kitbash), eta Timeline bat: atea ireki → HypJo irten → ontzia lurreratu → HypJo igo → aireratu → iluntzea →
    /// «JARRAITUKO DU…». Aurrebista / Aplikatu; idenpotentea (-eclipseEszenaBerria: eszena eta Timeline-a berreraiki).
    /// </summary>
    public static class ZinematikaTresna
    {
        private const string Tresna = "ZinematikaTresna";
        private const string Marka = "ECL_ZINEMATIKATRESNA";
        private const string DatuKarpeta = "Assets/_Eclipse/Datuak/Zinematika";
        private const string PiezaKarpeta = DatuKarpeta + "/Piezak";
        private const string AudioKarpeta = "Assets/_Eclipse/Audioa/Zinematika";
        internal const string Eszena = "Assets/_Eclipse/Eszenak/Proba/07_AmaieraZinematika.unity";
        private const string TimelineBidea = DatuKarpeta + "/TL_Amaiera.playable";
        private const string HypJo = "Assets/Resources/Jokalaria/PF_Jokalaria_HypJo.prefab";
        internal const string AmaieraTestua = "JARRAITUKO DU…";
        internal const float Iraupena = 28f;

        /// <summary>Ontzia lurreratzen den lekua (mundua) eta HypJo-ren ibilbidea.</summary>
        internal static readonly Vector3 Lurreratzea = new Vector3(2f, 0f, 20f);

        [MenuItem("ECLIPSE/Zinematika/Amaiera: aurrebista")]
        private static void MenuAurrebista() => Exekutatu(false, false);

        [MenuItem("ECLIPSE/Zinematika/Amaiera: aplikatu")]
        private static void MenuAplikatu()
        {
            if (EditorUtility.DisplayDialog(Tresna, "07_AmaieraZinematika (basamortua, ontzia eta Timeline-a) sortu edo eguneratu? Misioak ez dira ukitzen.", "Aplikatu", "Utzi"))
            {
                Exekutatu(true, false);
            }
        }

        public static void BatchZinematikaTresna()
        {
            string[] args = Environment.GetCommandLineArgs();
            EditorApplication.Exit(Exekutatu(args.Contains("-eclipseAplikatu"), args.Contains("-eclipseEszenaBerria")) ? 0 : 1);
        }

        private static bool Exekutatu(bool aplikatu, bool berria)
        {
            var aldaketak = new List<string>();
            var egiaztapenak = new List<string>();
            var informea = new Informea(Tresna, Marka, aplikatu ? "Aplikatu" : "Aurrebista", Eszena, "Amaierako zinematika (A3, F)");
            try
            {
                if (aplikatu)
                {
                    Pieza.KarpetaZiurtatu(PiezaKarpeta);
                }

                var soinuak = new Dictionary<string, AudioClip>();
                foreach (string izena in new[] { "SFX_Ontzia_Motorra", "SFX_Atea_Irekitzea", "SFX_Amaiera_Danbada" })
                {
                    soinuak[izena] = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioKarpeta}/{izena}.wav")
                                     ?? throw new InvalidOperationException($"Soinua falta da: {AudioKarpeta}/{izena}.wav (SoX-ekin sortua)");
                }

                Dictionary<string, Material> m = Materialak(aplikatu, aldaketak);
                var piezak = new Dictionary<string, GameObject>();
                foreach (Pieza.PiezaDef def in PiezaDefinizioak())
                {
                    piezak[def.Izena] = Pieza.AurrefabrikatuaSortu(def, m, aplikatu, aldaketak, PiezaKarpeta);
                }

                egiaztapenak.Add($"{piezak.Count} pieza (ontzia, fatxada, ate-panela, harkaitzak): {PiezaKarpeta}");
                bool badago = File.Exists(Eszena) && AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelineBidea) != null;
                if (badago && !berria)
                {
                    egiaztapenak.Add($"Eszena eta Timeline-a badaude: {Eszena}");
                }
                else
                {
                    aldaketak.Add($"Eszena {(badago ? "berreraikia" : "berria")}: {Eszena} + {TimelineBidea} (Terrain, ontzia, HypJo, {Iraupena:0} s)");
                    if (aplikatu)
                    {
                        egiaztapenak.AddRange(EszenaEraiki(m, piezak, soinuak));
                    }
                }

                if (aplikatu)
                {
                    AssetDatabase.SaveAssets();
                }

                informea.Atala(aplikatu ? "Aplikatutako aldaketak" : "Aplikatuko liratekeen aldaketak", aldaketak);
                informea.Atala("Egiaztapenak", egiaztapenak);
                informea.AmaituOndo($"{aldaketak.Count} aldaketa");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.Atala("Aldaketak (errorearen aurretik)", aldaketak);
                informea.Atala("Egiaztapenak", egiaztapenak);
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
        }

        // ---------- Materialak eta piezak ----------

        private static Dictionary<string, Material> Materialak(bool aplikatu, List<string> aldaketak)
        {
            Material L(string izena, Color c, float metala, float leuna, Color? emisioa = null) =>
                BorrokaTresna.LitMateriala(izena, c, metala, leuna, emisioa, aplikatu, aldaketak, DatuKarpeta);
            return new Dictionary<string, Material>
            {
                ["arroka"] = L("M_Zn_Arroka", new Color(0.46f, 0.25f, 0.15f), 0f, 0.15f),
                ["arroka_iluna"] = L("M_Zn_ArrokaIluna", new Color(0.24f, 0.12f, 0.09f), 0f, 0.2f),
                ["fatxada"] = L("M_Zn_Fatxada", new Color(0.3f, 0.32f, 0.33f), 0.5f, 0.35f),
                ["ontzia"] = L("M_Zn_Ontzia", new Color(0.55f, 0.57f, 0.6f), 0.85f, 0.55f),
                ["ontzia_iluna"] = L("M_Zn_OntziaIluna", new Color(0.1f, 0.11f, 0.12f), 0.7f, 0.45f),
                ["horia"] = L("M_Zn_Horia", new Color(0.85f, 0.62f, 0.08f), 0.3f, 0.45f),
                ["motorra"] = L("M_Zn_Motorra", Color.black, 0f, 0.5f, new Color(0.4f, 1.9f, 2.4f)),
                ["anbarra"] = L("M_Zn_Anbarra", Color.black, 0f, 0.5f, new Color(2.4f, 1.1f, 0.15f)),
                ["kabina"] = L("M_Zn_Kabina", new Color(0.05f, 0.08f, 0.1f), 0.2f, 0.95f, new Color(0.05f, 0.15f, 0.2f)),
                ["ilargia"] = L("M_Zn_Ilargia", new Color(0.85f, 0.8f, 0.72f), 0f, 0.1f, new Color(0.55f, 0.5f, 0.42f)),
                ["ilargia2"] = L("M_Zn_Ilargia2", new Color(0.6f, 0.75f, 0.8f), 0f, 0.1f, new Color(0.3f, 0.42f, 0.48f)),
                ["hautsa"] = BorrokaTresna.PartikulaMateriala("M_Zn_Hautsa", new Color(0.62f, 0.42f, 0.28f, 0.55f), false, aplikatu, aldaketak, karpeta: DatuKarpeta),
            };
        }

        private static Pieza.PiezaDef Def(string izena, Vector3 oina, IEnumerable<Pieza.Zatia> zatiak) =>
            new Pieza.PiezaDef { Izena = izena, Sektorea = "Zinematika", Horma = true, Oina = oina, Zatiak = zatiak.ToArray() };

        /// <summary>Pieza propioak (primitiboak, materialeko azpimalla bat). Horma=true: kolisiorik gabe (eszenografia hutsa).</summary>
        internal static List<Pieza.PiezaDef> PiezaDefinizioak()
        {
            var l = new List<Pieza.PiezaDef>
            {
                // Ontzia: sudurra +Z-n. Eskuineko alboan (+X) atea eta arrapala.
                Def("PF_Zn_Ontzia", new Vector3(12f, 4.5f, 14f), new[]
                {
                    Pieza.E(0f, 1.9f, 0f, 3.4f, 2.4f, 12.5f, "ontzia"),
                    Pieza.E(0f, 2.55f, 3.6f, 2.0f, 1.3f, 3.4f, "kabina"),
                    Pieza.E(0f, 1.75f, 6.3f, 1.4f, 1.0f, 2.2f, "ontzia_iluna"),
                    Pieza.K(0f, 1.2f, 0f, 2.2f, 0.5f, 8f, "ontzia_iluna"),
                    Pieza.K(3.4f, 1.75f, -1.2f, 4.6f, 0.22f, 4f, "ontzia_iluna", 0f, -18f, -6f),
                    Pieza.K(-3.4f, 1.75f, -1.2f, 4.6f, 0.22f, 4f, "ontzia_iluna", 0f, 18f, 6f),
                    Pieza.K(5.6f, 2.1f, -2.2f, 0.35f, 1.4f, 2.6f, "horia"),
                    Pieza.K(-5.6f, 2.1f, -2.2f, 0.35f, 1.4f, 2.6f, "horia"),
                    Pieza.K(0f, 3.5f, -4.6f, 0.3f, 2.2f, 2.8f, "ontzia_iluna", -22f),
                    Pieza.K(0f, 3.08f, 0.5f, 0.35f, 0.05f, 5f, "horia"),
                    Pieza.Z(1.7f, 1.9f, -6f, 1.3f, 2.6f, "ontzia", 90f),
                    Pieza.Z(-1.7f, 1.9f, -6f, 1.3f, 2.6f, "ontzia", 90f),
                    Pieza.Z(1.7f, 1.9f, -7.33f, 1.05f, 0.08f, "motorra", 90f),
                    Pieza.Z(-1.7f, 1.9f, -7.33f, 1.05f, 0.08f, "motorra", 90f),
                    Pieza.Z(1.5f, 0.55f, 3f, 0.16f, 1.1f, "ontzia_iluna"), Pieza.Z(-1.5f, 0.55f, 3f, 0.16f, 1.1f, "ontzia_iluna"),
                    Pieza.Z(1.5f, 0.55f, -3.5f, 0.16f, 1.1f, "ontzia_iluna"), Pieza.Z(-1.5f, 0.55f, -3.5f, 0.16f, 1.1f, "ontzia_iluna"),
                    Pieza.K(1.5f, 0.04f, 3f, 0.7f, 0.08f, 0.7f, "ontzia_iluna"), Pieza.K(-1.5f, 0.04f, 3f, 0.7f, 0.08f, 0.7f, "ontzia_iluna"),
                    Pieza.K(1.5f, 0.04f, -3.5f, 0.7f, 0.08f, 0.7f, "ontzia_iluna"), Pieza.K(-1.5f, 0.04f, -3.5f, 0.7f, 0.08f, 0.7f, "ontzia_iluna"),
                    Pieza.K(1.72f, 1.55f, -0.5f, 0.06f, 1.7f, 1.2f, "anbarra"),
                    Pieza.K(2.45f, 0.5f, -0.5f, 1.75f, 0.08f, 1.2f, "ontzia_iluna", 0f, 0f, -33f),
                    Pieza.E(1.75f, 2.1f, 2.2f, 0.22f, 0.22f, 0.22f, "anbarra"), Pieza.E(-1.75f, 2.1f, 2.2f, 0.22f, 0.22f, 0.22f, "anbarra"),
                }),

                // Estazioaren fatxada: atea x∈[-1,5, 1,5], y∈[0, 4]; aurrealdea z = +1,5.
                Def("PF_Zn_Fatxada", new Vector3(23f, 12f, 3f), new[]
                {
                    Pieza.K(-6.5f, 4.5f, 0f, 10f, 9f, 3f, "fatxada"), Pieza.K(6.5f, 4.5f, 0f, 10f, 9f, 3f, "fatxada"), Pieza.K(0f, 6.5f, 0f, 3f, 5f, 3f, "fatxada"),
                    Pieza.K(0f, 2f, -1.6f, 3f, 4f, 0.2f, "ontzia_iluna"),
                    Pieza.K(-1.62f, 2f, 1.52f, 0.24f, 4f, 0.06f, "horia"), Pieza.K(1.62f, 2f, 1.52f, 0.24f, 4f, 0.06f, "horia"),
                    Pieza.K(0f, 4.1f, 1.52f, 3.48f, 0.22f, 0.06f, "horia"),
                    Pieza.E(-2.2f, 4.4f, 1.6f, 0.3f, 0.3f, 0.3f, "anbarra"), Pieza.E(2.2f, 4.4f, 1.6f, 0.3f, 0.3f, 0.3f, "anbarra"),
                    Pieza.K(-6f, 9.3f, 0f, 4f, 0.6f, 2.5f, "ontzia_iluna"), Pieza.Z(5f, 9.8f, 0f, 1.2f, 1.6f, "ontzia"), Pieza.Z(7f, 11f, 0f, 0.12f, 4f, "ontzia_iluna"),
                    Pieza.Z(-4f, 4.5f, 1.6f, 0.35f, 9f, "ontzia"), Pieza.Z(-3.4f, 4.5f, 1.6f, 0.25f, 9f, "ontzia_iluna"), Pieza.Z(4.2f, 4.5f, 1.6f, 0.35f, 9f, "ontzia"),
                    Pieza.K(-8f, 6f, 1.52f, 3f, 0.3f, 0.06f, "horia"), Pieza.K(8f, 6f, 1.52f, 3f, 0.3f, 0.06f, "horia"),
                }),

                Def("PF_Zn_AtePanela", new Vector3(1.5f, 4f, 0.2f), new[]
                {
                    Pieza.K(0f, 2f, 0f, 1.5f, 4f, 0.2f, "ontzia"), Pieza.K(0f, 2f, 0.11f, 0.18f, 4f, 0.02f, "horia"), Pieza.K(0f, 3.5f, 0.11f, 1.3f, 0.08f, 0.02f, "anbarra"),
                }),

                Def("PF_Zn_Orratza", new Vector3(4f, 14f, 4f), new[]
                {
                    Pieza.E(0f, 4f, 0f, 4f, 9f, 4f, "arroka"), Pieza.E(0.6f, 9f, 0.3f, 2.6f, 5f, 2.4f, "arroka"),
                    Pieza.E(-0.3f, 12.5f, 0.1f, 1.4f, 4f, 1.3f, "arroka_iluna"), Pieza.E(1.2f, 1.2f, 1f, 3f, 2.5f, 2.6f, "arroka_iluna"),
                }),

                Def("PF_Zn_Harria", new Vector3(3.5f, 2f, 3f), new[]
                {
                    Pieza.E(0f, 0.8f, 0f, 3f, 2f, 2.6f, "arroka"), Pieza.E(0.9f, 0.5f, 0.6f, 1.8f, 1.3f, 1.6f, "arroka_iluna"),
                }),
            };

            // Arku arrotza: erdizirkulua, 9 m-ko erradioa.
            var arkua = new List<Pieza.Zatia>();
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI * i / 10f;
                arkua.Add(Pieza.K(9f * Mathf.Cos(a), 9f * Mathf.Sin(a) + 0.5f, 0f, 2.6f, 3.2f, 3f, i % 2 == 0 ? "arroka" : "arroka_iluna", 0f, 0f, a * Mathf.Rad2Deg));
            }

            l.Add(Def("PF_Zn_Arkua", new Vector3(21f, 11f, 3f), arkua));
            return l;
        }

        // ---------- Lurra, zerua eta argia ----------

        private static Texture2D HareaTestura()
        {
            string bidea = $"{DatuKarpeta}/T_Zn_Harea.png";
            const int N = 512;
            var t = new Texture2D(N, N, TextureFormat.RGB24, false);
            var oinarria = new Color(0.74f, 0.47f, 0.3f);
            var r = new System.Random(77);
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    // Errepikagarria: perlin-a sinu-koordenatuetan; olatu-marrak eta ale fina.
                    float p = Mathf.PerlinNoise(Mathf.Sin(u * Mathf.PI * 2f) * 3f + 10f, Mathf.Sin(v * Mathf.PI * 2f) * 3f + 20f);
                    float marrak = 0.5f + 0.5f * Mathf.Sin((v * 9f + p * 1.5f) * Mathf.PI * 2f);
                    float alea = (float)r.NextDouble();
                    float f = 0.82f + 0.18f * p + 0.06f * marrak + 0.08f * (alea - 0.5f);
                    t.SetPixel(x, y, oinarria * f);
                }
            }

            File.WriteAllBytes(bidea, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(bidea);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(bidea);
        }

        private static float Leun(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Dunak: 400×400 m, erdialdea (estazioa, lurreratzea) laua y = 0-n.</summary>
        private static Terrain Lurra(Transform erroa)
        {
            var geruza = new TerrainLayer { diffuseTexture = HareaTestura(), tileSize = new Vector2(6f, 6f), smoothness = 0.1f };
            AssetDatabase.CreateAsset(geruza, $"{DatuKarpeta}/TL_Zn_Harea.terrainlayer");
            const int Res = 257;
            var datuak = new TerrainData { heightmapResolution = Res };
            datuak.size = new Vector3(400f, 40f, 400f);
            var h = new float[Res, Res];
            for (int iz = 0; iz < Res; iz++)
            {
                for (int ix = 0; ix < Res; ix++)
                {
                    float x = -200f + ix * 400f / (Res - 1), z = -200f + iz * 400f / (Res - 1);
                    float d1 = Mathf.PerlinNoise(x * 0.008f + 10f, z * 0.008f + 20f);
                    float d2 = Mathf.PerlinNoise(x * 0.03f + 5f, z * 0.03f + 9f);
                    float gailurra = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x * 0.015f, z * 0.015f) - 1f);
                    float dunak = 0.28f * d1 * d1 + 0.08f * d2 + 0.12f * gailurra * d1;
                    float erdia = Vector2.Distance(new Vector2(x, z), new Vector2(Lurreratzea.x, 12f));
                    h[iz, ix] = 0.05f + dunak * Leun(35f, 75f, erdia);
                }
            }

            datuak.SetHeights(0, 0, h);
            datuak.terrainLayers = new[] { geruza };
            AssetDatabase.CreateAsset(datuak, $"{DatuKarpeta}/TD_Zn_Basamortua.asset");
            GameObject o = Terrain.CreateTerrainGameObject(datuak);
            o.name = "Basamortua";
            o.transform.SetParent(erroa, false);
            o.transform.position = new Vector3(-200f, -2f, -200f);
            var lurra = o.GetComponent<Terrain>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "M_Zn_Lurra" };
            AssetDatabase.CreateAsset(mat, $"{DatuKarpeta}/M_Zn_Lurra.mat");
            lurra.materialTemplate = mat;
            return lurra;
        }

        private static void ZeruaEtaArgia(Transform erroa)
        {
            var zerua = new Material(Shader.Find("Skybox/Procedural")) { name = "M_Zn_Zerua" };
            zerua.SetFloat("_SunSize", 0.05f);
            zerua.SetFloat("_AtmosphereThickness", 1.9f);
            zerua.SetColor("_SkyTint", new Color(0.85f, 0.5f, 0.35f));
            zerua.SetColor("_GroundColor", new Color(0.45f, 0.3f, 0.22f));
            zerua.SetFloat("_Exposure", 1.15f);
            AssetDatabase.CreateAsset(zerua, $"{DatuKarpeta}/M_Zn_Zerua.mat");
            RenderSettings.skybox = zerua;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.78f, 0.55f, 0.4f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.48f, 0.36f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.29f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.12f, 0.09f);

            var eguzkia = new GameObject("Eguzkia").AddComponent<Light>();
            eguzkia.transform.SetParent(erroa, false);
            eguzkia.type = LightType.Directional;
            eguzkia.transform.rotation = Quaternion.Euler(14f, 200f, 0f);
            eguzkia.color = new Color(1f, 0.78f, 0.55f);
            eguzkia.intensity = 1.6f;
            eguzkia.shadows = LightShadows.Soft;
            RenderSettings.sun = eguzkia;
        }

        private static GameObject Jarri(GameObject aurrefab, Transform gurasoa, string izena, Vector3 lekua, float yaw, float eskala = 1f)
        {
            var o = (GameObject)PrefabUtility.InstantiatePrefab(aurrefab, gurasoa.gameObject.scene);
            o.name = izena;
            o.transform.SetParent(gurasoa, false);
            o.transform.SetLocalPositionAndRotation(lekua, Quaternion.Euler(0f, yaw, 0f));
            o.transform.localScale = Vector3.one * eskala;
            return o;
        }

        private static Transform Hutsa(string izena, Transform gurasoa, Vector3 lekua)
        {
            var t = new GameObject(izena).transform;
            t.SetParent(gurasoa, false);
            t.localPosition = lekua;
            return t;
        }

        // ---------- Animazio-klipak ----------

        private static AnimationCurve Kurba(IEnumerable<(float T, float V)> giltzak)
        {
            var k = new AnimationCurve(giltzak.Select(g => new Keyframe(g.T, g.V)).ToArray());
            for (int i = 0; i < k.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(k, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(k, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return k;
        }

        private static void Posizioa(AnimationClip c, string bidea, (float T, Vector3 P)[] g)
        {
            c.SetCurve(bidea, typeof(Transform), "localPosition.x", Kurba(g.Select(x => (x.T, x.P.x))));
            c.SetCurve(bidea, typeof(Transform), "localPosition.y", Kurba(g.Select(x => (x.T, x.P.y))));
            c.SetCurve(bidea, typeof(Transform), "localPosition.z", Kurba(g.Select(x => (x.T, x.P.z))));
        }

        private static void Biraketa(AnimationClip c, string bidea, (float T, Vector3 E)[] g)
        {
            c.SetCurve(bidea, typeof(Transform), "localEulerAnglesRaw.x", Kurba(g.Select(x => (x.T, x.E.x))));
            c.SetCurve(bidea, typeof(Transform), "localEulerAnglesRaw.y", Kurba(g.Select(x => (x.T, x.E.y))));
            c.SetCurve(bidea, typeof(Transform), "localEulerAnglesRaw.z", Kurba(g.Select(x => (x.T, x.E.z))));
        }

        /// <summary>Kamera: begiratzeko puntutik Euler angeluak, aurrekoarekin jarraituak (360-ko jauzirik gabe).</summary>
        private static Vector3 Begira(Vector3 lekua, Vector3 helburua, Vector3? aurrekoa)
        {
            Vector3 e = Quaternion.LookRotation(helburua - lekua).eulerAngles;
            Vector3 erref = aurrekoa ?? Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                while (e[i] - erref[i] > 180f) e[i] -= 360f;
                while (e[i] - erref[i] < -180f) e[i] += 360f;
            }

            return e;
        }

        private static AnimationClip Klipa(string izena)
        {
            string bidea = $"{DatuKarpeta}/{izena}.anim";
            AssetDatabase.DeleteAsset(bidea);
            var c = new AnimationClip { name = izena, frameRate = 30f };
            AssetDatabase.CreateAsset(c, bidea);
            return c;
        }

        // ---------- Eszena eta Timeline-a ----------

        private static List<string> EszenaEraiki(Dictionary<string, Material> m, Dictionary<string, GameObject> piezak, Dictionary<string, AudioClip> soinuak)
        {
            var emaitza = new List<string>();
            string aurrekoa = SceneManager.GetActiveScene().path;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException("Eszena ez da sortu: irekitako eszena gorde gabe dago.");
            }

            Scene eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var erroa = new GameObject("Zinematika").transform;
            ZeruaEtaArgia(erroa);
            Terrain lurra = Lurra(erroa);
            emaitza.Add($"Terrain: {lurra.terrainData.heightmapResolution}² altuera-mapa, 400×400 m, dunak erdigunetik 35 m-tik aurrera");

            // Paisaia: harkaitz-orratzak, arkua, harriak (ibilbidetik eta lurreratze-eremutik kanpo) eta bi ilargi.
            Transform paisaia = Hutsa("Paisaia", erroa, Vector3.zero);
            foreach ((Vector3 p, float yaw, float s) in new[]
                     {
                         (new Vector3(-30f, 0f, 40f), 10f, 1.4f), (new Vector3(-45f, 0f, 22f), 80f, 1.1f), (new Vector3(38f, 0f, 55f), 200f, 1.7f),
                         (new Vector3(60f, 0f, 18f), 140f, 1.2f), (new Vector3(-14f, 0f, 85f), 300f, 2.0f), (new Vector3(-70f, 0f, 70f), 30f, 2.4f),
                     })
            {
                Jarri(piezak["PF_Zn_Orratza"], paisaia, "Orratza", p, yaw, s);
            }

            Jarri(piezak["PF_Zn_Arkua"], paisaia, "Arkua", new Vector3(26f, 0f, 95f), 20f, 1.3f);
            var r = new System.Random(1234);
            int harriak = 0;
            for (int i = 0; i < 60 && harriak < 24; i++)
            {
                var p = new Vector3((float)(r.NextDouble() * 120.0 - 60.0), 0f, (float)(r.NextDouble() * 100.0 - 10.0));
                bool ibilbidea = p.x > -9f && p.x < 12f && p.z > -2f && p.z < 32f;
                bool fatxada = Mathf.Abs(p.x) < 14f && p.z < 4f;
                if (ibilbidea || fatxada)
                {
                    continue;
                }

                Jarri(piezak["PF_Zn_Harria"], paisaia, "Harria", p, (float)(r.NextDouble() * 360.0), 0.5f + (float)r.NextDouble() * 1.3f);
                harriak++;
            }

            foreach ((string izena, Vector3 p, float s, string mat) in new[]
                     {
                         ("Ilargia_1", new Vector3(-110f, 85f, 190f), 34f, "ilargia"), ("Ilargia_2", new Vector3(95f, 48f, 175f), 13f, "ilargia2"),
                     })
            {
                GameObject il = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.DestroyImmediate(il.GetComponent<Collider>());
                il.name = izena;
                il.transform.SetParent(paisaia, false);
                il.transform.localPosition = p;
                il.transform.localScale = Vector3.one * s;
                var mr = il.GetComponent<MeshRenderer>();
                mr.sharedMaterial = m[mat];
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            emaitza.Add($"Paisaia: 6 orratz, arku 1, {harriak} harri, 2 ilargi");

            // Estazioa: fatxada eta atea (bi panel irristakor, barneko argia eta soinua).
            Jarri(piezak["PF_Zn_Fatxada"], erroa, "Estazioa_Fatxada", Vector3.zero, 0f);
            Transform atea = Hutsa("Atea", erroa, Vector3.zero);
            Animator ateaAn = atea.gameObject.AddComponent<Animator>();
            Jarri(piezak["PF_Zn_AtePanela"], atea, "Panela_Ezkerra", new Vector3(-0.75f, 0f, 1.25f), 0f);
            Jarri(piezak["PF_Zn_AtePanela"], atea, "Panela_Eskuina", new Vector3(0.75f, 0f, 1.25f), 0f);
            var barnekoa = new GameObject("Barneko_Argia").AddComponent<Light>();
            barnekoa.transform.SetParent(atea, false);
            barnekoa.transform.localPosition = new Vector3(0f, 3f, -0.8f);
            barnekoa.type = LightType.Point;
            barnekoa.color = new Color(1f, 0.62f, 0.3f);
            barnekoa.range = 9f;
            barnekoa.intensity = 0f;
            AudioSource ateSoinua = atea.gameObject.AddComponent<AudioSource>();
            ateSoinua.playOnAwake = false;
            ateSoinua.spatialBlend = 0.6f;

            // Ontzia: Animator-a errigian (jatorrian, geldi) eta kurbak umean ("Ontzia"). Erroko kurbak Timeline-k root motion gisa
            // tratatzen ditu (lehen giltzarekiko desplazamenduak): umeari aplikatuta, posizioak absolutuak dira.
            Transform ontziRig = Hutsa("Ontzia_Rig", erroa, Vector3.zero);
            Animator ontziAn = ontziRig.gameObject.AddComponent<Animator>();
            Transform ontzia = Hutsa("Ontzia", ontziRig, new Vector3(-120f, 70f, 140f));
            Jarri(piezak["PF_Zn_Ontzia"], ontzia, "Ontzia_Eredua", Vector3.zero, 0f);
            var motorArgia = new GameObject("Motor_Argia").AddComponent<Light>();
            motorArgia.transform.SetParent(ontzia, false);
            motorArgia.transform.localPosition = new Vector3(0f, 1.9f, -8f);
            motorArgia.type = LightType.Point;
            motorArgia.color = new Color(0.4f, 0.85f, 1f);
            motorArgia.range = 14f;
            motorArgia.intensity = 4f;
            AudioSource motorra = ontzia.gameObject.AddComponent<AudioSource>();
            motorra.playOnAwake = false;
            motorra.spatialBlend = 1f;
            motorra.minDistance = 8f;
            motorra.maxDistance = 250f;

            // Hautsa lurreratzean eta aireratzean (Timeline-k aktibatzen ditu).
            GameObject Hautsa(string izena)
            {
                ParticleSystem ps = BorrokaTresna.Partikulak(izena, erroa, m["hautsa"], new Color(0.62f, 0.42f, 0.28f, 0.5f), new Vector2(1.5f, 2.6f),
                    new Vector2(3f, 7f), new Vector2(1.5f, 3.5f), -0.03f, 85f, false);
                ps.transform.SetPositionAndRotation(Lurreratzea + Vector3.up * 0.2f, Quaternion.Euler(-90f, 0f, 0f));
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = true;
                main.maxParticles = 400;
                ParticleSystem.EmissionModule em = ps.emission;
                em.rateOverTime = 110f;
                ParticleSystem.ShapeModule forma = ps.shape;
                forma.radius = 3.5f;
                ps.gameObject.SetActive(false);
                return ps.gameObject;
            }

            GameObject hautsaBehera = Hautsa("Hautsa_Lurreratzea");
            GameObject hautsaGora = Hautsa("Hautsa_Aireratzea");

            // HypJo: gurasoa Timeline-k mugitzen du; umearen Animator humanoideak AC_Npc (oinez/korrika abiaduraren arabera).
            Transform hypjoRig = Hutsa("HypJo_Rig", erroa, Vector3.zero);
            Animator bideAn = hypjoRig.gameObject.AddComponent<Animator>();
            Transform hypjoBidea = Hutsa("HypJo_Bidea", hypjoRig, new Vector3(0f, 0f, -0.8f));
            GameObject hypjo = Jarri(AssetDatabase.LoadAssetAtPath<GameObject>(HypJo) ?? throw new InvalidOperationException($"HypJo falta da: {HypJo}"),
                hypjoBidea, "HypJo", Vector3.zero, 0f);
            foreach (JokalariBisuala b in hypjo.GetComponentsInChildren<JokalariBisuala>(true))
            {
                b.enabled = false;
            }

            Animator hypjoAn = hypjo.GetComponentInChildren<Animator>() ?? throw new InvalidOperationException("HypJo-k ez du Animator-ik");
            hypjoAn.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(NpcTresna.NpcKontrolagailua)
                                                ?? throw new InvalidOperationException($"{NpcTresna.NpcKontrolagailua} falta da (NpcTresna aplikatu)");
            hypjoAn.applyRootMotion = false;
            hypjoAn.gameObject.AddComponent<ZinematikaAbiadura>();

            // Kamera-errigia (Timeline) eta amaierako danbada.
            Transform rig = Hutsa("Kamera_Rig", erroa, Vector3.zero);
            Animator rigAn = rig.gameObject.AddComponent<Animator>();
            var kameraObj = new GameObject("Kamera") { tag = "MainCamera" };
            kameraObj.transform.SetParent(rig, false);
            kameraObj.transform.localPosition = new Vector3(-9f, 1.7f, 13f);
            Camera kamera = kameraObj.AddComponent<Camera>();
            kamera.fieldOfView = 50f;
            kamera.nearClipPlane = 0.1f;
            kamera.farClipPlane = 1500f;
            kameraObj.AddComponent<AudioListener>();
            AudioSource danbada = rig.gameObject.AddComponent<AudioSource>();
            danbada.playOnAwake = false;
            danbada.spatialBlend = 0f;

            // Amaierako pantaila: beltza (iluntzea) eta testua, CanvasGroup-en alfa Timeline-k animatuta.
            var pantaila = new GameObject("Amaiera_Pantaila");
            pantaila.transform.SetParent(erroa, false);
            Canvas oihala = pantaila.AddComponent<Canvas>();
            oihala.renderMode = RenderMode.ScreenSpaceOverlay;
            oihala.sortingOrder = 100;
            CanvasScaler eskalatzailea = pantaila.AddComponent<CanvasScaler>();
            eskalatzailea.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            eskalatzailea.referenceResolution = new Vector2(1920f, 1080f);
            CanvasGroup taldea = pantaila.AddComponent<CanvasGroup>();
            taldea.alpha = 1f;
            taldea.blocksRaycasts = false;
            Animator pantailaAn = pantaila.AddComponent<Animator>();
            RectTransform Betea(string izena, Transform gurasoa)
            {
                var rt = new GameObject(izena, typeof(RectTransform)).GetComponent<RectTransform>();
                rt.SetParent(gurasoa, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return rt;
            }

            Betea("Beltza", pantaila.transform).gameObject.AddComponent<Image>().color = Color.black;
            RectTransform testuRt = Betea("Testua", pantaila.transform);
            testuRt.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            Text testua = testuRt.gameObject.AddComponent<Text>();
            testua.text = AmaieraTestua;
            testua.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            testua.fontSize = 76;
            testua.fontStyle = FontStyle.Bold;
            testua.alignment = TextAnchor.MiddleCenter;
            testua.color = new Color(0.85f, 0.88f, 0.9f);

            // ---------- Animazio-klipak ----------
            AnimationClip ateKlipa = Klipa("AN_Zn_Atea");
            Posizioa(ateKlipa, "Panela_Ezkerra", new[] { (0f, new Vector3(-0.75f, 0f, 1.25f)), (0.8f, new Vector3(-0.75f, 0f, 1.25f)), (2.4f, new Vector3(-2.25f, 0f, 1.25f)) });
            Posizioa(ateKlipa, "Panela_Eskuina", new[] { (0f, new Vector3(0.75f, 0f, 1.25f)), (0.8f, new Vector3(0.75f, 0f, 1.25f)), (2.4f, new Vector3(2.25f, 0f, 1.25f)) });
            ateKlipa.SetCurve("Barneko_Argia", typeof(Light), "m_Intensity", Kurba(new[] { (0f, 0f), (0.8f, 0f), (2f, 5f) }));

            Vector3 Lur(Vector3 v) => Lurreratzea + v;
            AnimationClip hypjoKlipa = Klipa("AN_Zn_HypJo");
            Posizioa(hypjoKlipa, "HypJo_Bidea", new[]
            {
                (0f, new Vector3(0f, 0f, -0.8f)), (2.4f, new Vector3(0f, 0f, -0.8f)), (6.4f, new Vector3(0f, 0f, 4f)), (12.5f, new Vector3(0f, 0f, 4f)),
                (15.1f, Lur(new Vector3(-0.5f, 0f, -2.9f))), (15.6f, Lur(new Vector3(-0.5f, 0.75f, -2.1f))),
            });
            float joggingYaw = Mathf.Atan2(Lurreratzea.x - 0.5f, Lurreratzea.z - 2.9f - 4f) * Mathf.Rad2Deg;
            Biraketa(hypjoKlipa, "HypJo_Bidea", new[] { (0f, Vector3.zero), (12.5f, Vector3.zero), (12.8f, new Vector3(0f, joggingYaw, 0f)), (15.6f, new Vector3(0f, joggingYaw, 0f)) });

            (float T, Vector3 P, Vector3 E)[] ontziGiltzak =
            {
                (0f, new Vector3(-120f, 70f, 140f), new Vector3(5f, 90f, -10f)),
                (6.5f, new Vector3(-120f, 70f, 140f), new Vector3(5f, 90f, -10f)),
                (9.5f, new Vector3(-20f, 14f, 26f), new Vector3(-5f, 90f, 5f)),
                (10.8f, Lur(new Vector3(0f, 3f, 0f)), new Vector3(-8f, 90f, 0f)),
                (12f, Lurreratzea, new Vector3(0f, 90f, 0f)),
                (16f, Lurreratzea, new Vector3(0f, 90f, 0f)),
                (17.5f, Lur(new Vector3(0f, 6f, 0f)), new Vector3(-5f, 90f, 0f)),
                (19f, new Vector3(25f, 22f, 30f), new Vector3(-20f, 80f, -10f)),
                (21.5f, new Vector3(160f, 120f, 80f), new Vector3(-25f, 75f, -15f)),
            };
            AnimationClip ontziKlipa = Klipa("AN_Zn_Ontzia");
            Posizioa(ontziKlipa, "Ontzia", ontziGiltzak.Select(g => (g.T, g.P)).ToArray());
            Biraketa(ontziKlipa, "Ontzia", ontziGiltzak.Select(g => (g.T, g.E)).ToArray());
            AnimationCurve[] ontziKurbak = Enumerable.Range(0, 3).Select(i => Kurba(ontziGiltzak.Select(g => (g.T, g.P[i])))).ToArray();
            Vector3 OntziaUnean(float t) => new Vector3(ontziKurbak[0].Evaluate(t), ontziKurbak[1].Evaluate(t), ontziKurbak[2].Evaluate(t));

            // Planoak: (denbora, kamera, helburua). Mozketak: giltza bikoitza 1 ms-ko tartearekin.
            Vector3 k2 = new Vector3(6f, 1.3f, 7f);
            Vector3 k4 = new Vector3(-16f, 2.5f, 8f);
            (float T, Vector3 P, Vector3 H)[] planoak =
            {
                (0f, new Vector3(-9f, 1.7f, 13f), new Vector3(0f, 2.2f, 1f)),
                (6.499f, new Vector3(-7.5f, 1.6f, 11f), new Vector3(0f, 1.8f, 2.5f)),
                (6.5f, k2, OntziaUnean(6.5f)), (8f, k2, OntziaUnean(8f)), (9.5f, k2, OntziaUnean(9.5f)), (10.8f, k2, OntziaUnean(10.8f) + Vector3.up),
                (12.499f, k2, Lur(new Vector3(0f, 1.5f, 0f))),
                (12.5f, new Vector3(-2.5f, 2f, 1.5f), Lur(new Vector3(-0.5f, 1.5f, -1f))), (15.999f, new Vector3(-1.5f, 2.2f, 7f), Lur(new Vector3(-0.5f, 1.8f, -1f))),
                (16f, k4, Lur(new Vector3(0f, 2f, 0f))), (17.5f, k4, OntziaUnean(17.5f)), (19f, k4, OntziaUnean(19f)), (21.5f, k4, OntziaUnean(21.5f)),
            };
            var kameraGiltzak = new List<(float T, Vector3 P, Vector3 E)>();
            Vector3? aurrekoE = null;
            foreach ((float t, Vector3 p, Vector3 h) in planoak)
            {
                Vector3 e = Begira(p, h, aurrekoE);
                kameraGiltzak.Add((t, p, e));
                aurrekoE = e;
            }

            AnimationClip kameraKlipa = Klipa("AN_Zn_Kamera");
            Posizioa(kameraKlipa, "Kamera", kameraGiltzak.Select(g => (g.T, g.P)).ToArray());
            Biraketa(kameraKlipa, "Kamera", kameraGiltzak.Select(g => (g.T, g.E)).ToArray());

            AnimationClip pantailaKlipa = Klipa("AN_Zn_Pantaila");
            pantailaKlipa.SetCurve(string.Empty, typeof(CanvasGroup), "m_Alpha", Kurba(new[] { (0f, 1f), (1.2f, 0f), (20.3f, 0f), (21.8f, 1f), (Iraupena, 1f) }));
            pantailaKlipa.SetCurve("Testua", typeof(CanvasGroup), "m_Alpha", Kurba(new[] { (0f, 0f), (22.3f, 0f), (23.8f, 1f), (Iraupena, 1f) }));

            // ---------- Timeline-a ----------
            AssetDatabase.DeleteAsset(TimelineBidea);
            var tl = ScriptableObject.CreateInstance<TimelineAsset>();
            tl.name = "TL_Amaiera";
            AssetDatabase.CreateAsset(tl, TimelineBidea);
            var zuzendariObj = new GameObject("Zuzendaria");
            zuzendariObj.transform.SetParent(erroa, false);
            PlayableDirector zuzendaria = zuzendariObj.AddComponent<PlayableDirector>();
            zuzendaria.playableAsset = tl;
            zuzendaria.playOnAwake = true;
            zuzendaria.extrapolationMode = DirectorWrapMode.Hold;
            zuzendariObj.AddComponent<ZinematikaAmaiera>();

            void Anim(string izena, Animator lotura, AnimationClip klipa)
            {
                AnimationTrack pista = tl.CreateTrack<AnimationTrack>(null, izena);
                TimelineClip c = pista.CreateClip(klipa);
                c.start = 0;
                // Zinematika osoa estali: klipa begiztarik gabe dagoenez, azken giltza mantentzen du (objektuak ez dira hasierara itzultzen).
                c.duration = Iraupena;
                zuzendaria.SetGenericBinding(pista, lotura);
            }

            void Aktibo(string izena, GameObject lotura, double hasiera, double amaiera)
            {
                ActivationTrack pista = tl.CreateTrack<ActivationTrack>(null, izena);
                TimelineClip c = pista.CreateDefaultClip();
                c.start = hasiera;
                c.duration = amaiera - hasiera;
                pista.postPlaybackState = ActivationTrack.PostPlaybackState.LeaveAsIs;
                zuzendaria.SetGenericBinding(pista, lotura);
            }

            void Soinua(string izena, AudioSource lotura, AudioClip klipa, double hasiera)
            {
                AudioTrack pista = tl.CreateTrack<AudioTrack>(null, izena);
                TimelineClip c = pista.CreateClip(klipa);
                c.start = hasiera;
                zuzendaria.SetGenericBinding(pista, lotura);
            }

            Anim("Atea", ateaAn, ateKlipa);
            Anim("HypJo", bideAn, hypjoKlipa);
            Aktibo("HypJo_Aktibo", hypjoBidea.gameObject, 0, 15.6);
            Anim("Ontzia", ontziAn, ontziKlipa);
            Aktibo("Hautsa_Lurreratzea", hautsaBehera, 10.4, 13.0);
            Aktibo("Hautsa_Aireratzea", hautsaGora, 16.0, 19.0);
            Anim("Kamera", rigAn, kameraKlipa);
            Anim("Pantaila", pantailaAn, pantailaKlipa);
            Soinua("Atea_Soinua", ateSoinua, soinuak["SFX_Atea_Irekitzea"], 0.8);
            Soinua("Motorra", motorra, soinuak["SFX_Ontzia_Motorra"], 6.5);
            Soinua("Danbada", danbada, soinuak["SFX_Amaiera_Danbada"], 22.2);
            EditorUtility.SetDirty(tl);
            emaitza.Add($"Timeline: {tl.GetOutputTracks().Count()} pista, {tl.duration:0.0} s ({TimelineBidea})");

            if (!EditorSceneManager.SaveScene(eszena, Eszena))
            {
                throw new InvalidOperationException($"Ezin izan da gorde {Eszena}");
            }

            emaitza.Add($"Eszena gordeta: {Eszena}");
            if (!string.IsNullOrEmpty(aurrekoa) && aurrekoa != Eszena)
            {
                EditorSceneManager.OpenScene(aurrekoa, OpenSceneMode.Single);
            }

            return emaitza;
        }
    }
}
