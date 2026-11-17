using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Eclipse.Jokalaria;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A2: HypJo v2 protagonistaren geruza bisuala. Aurrebista (ezer aldatu gabe) eta Aplikatu; idenpotentea.
    /// - Jatorrizkoak (ThirdParty/HypJo_Cyborg, ThirdParty/Quaternius_UAL): SHA-256 egiaztatu, ez dira aldatzen.
    /// - Inportazioa API bidez: HypJo Humanoid atzamarrik gabe (P3), UAL Humanoid root motion gabe.
    /// - Sortzen ditu: URP materialak, AC_Jokalaria, Resources/Jokalaria/PF_Jokalaria_HypJo eta 03_JokalariProba eszena.
    /// Aktiboen sorrera eta inportazio-ezarpenak ez dira Undo-z desegiten (Unity-ren muga): atzera egiteko, sortutako karpetak ezabatu.
    /// </summary>
    public static class JokalariBisualTresna
    {
        private const string Tresna = "JokalariBisuala";
        private const string Marka = "ECL_JOKALARIBISUALA";

        internal const string HypJoKarpeta = "Assets/ThirdParty/HypJo_Cyborg";
        private const string Fbx = HypJoKarpeta + "/Cyborg_ECLIPSE_v2_RigMixamo65_Tpose.fbx";
        internal const string Ual = "Assets/ThirdParty/Quaternius_UAL/UAL1_Standard.fbx";
        internal const string DatuKarpeta = "Assets/_Eclipse/Datuak/Jokalaria";
        internal const string ProbaKarpeta = DatuKarpeta + "/Proba";
        private const string GorputzMat = DatuKarpeta + "/M_HypJo_Gorputza.mat";
        private const string ZapiMat = DatuKarpeta + "/M_HypJo_Lepozapia.mat";
        private const string Kontrolagailua = DatuKarpeta + "/AC_Jokalaria.controller";
        internal const string AurrefabBidea = "Assets/Resources/Jokalaria/PF_Jokalaria_HypJo.prefab";
        internal const string ProbaEszena = "Assets/_Eclipse/Eszenak/Proba/03_JokalariProba.unity";

        /// <summary>FBX-eko jatorrizko materialen izenak (P3-ko inportazioan irakurrita).</summary>
        private const string GorputzIturria = "M_Cyborg_ECLIPSE_v2.001";
        private const string ZapiIturria = "M_Cyborg_LepoZapia";

        /// <summary>Ereduaren eskala: P3-n 1,82 m (T-posea); neurketen arabera doitzen da.</summary>
        internal const float Eskala = 1f;

        private static readonly (string Bidea, string Sha)[] Jatorrizkoak =
        {
            (Fbx, "db522e5710b17f8c876dda173eb98208e69acf90d2858ed741e4a7ccbb8d45ea"),
            (HypJoKarpeta + "/T_Cyborg_BaseColor_2k.png", "6b5ef075f119222ef6916e675bb1921dc943f7a02fee8cc84dbdc93f2734cb03"),
            (HypJoKarpeta + "/T_Cyborg_Emission_2k.png", "8d30fc493edadcf7e8b31cbf2a5820e005a5e494977a5be92c5510c2a88bb9f3"),
            (HypJoKarpeta + "/T_Cyborg_MetallicSmoothness_URP_2k.png", "9a41519181c5d15a5d25033e76da60420ad594d6973414188ca878f6971f4d82"),
            (Ual, "21b32d912da3cb93426d974fb945e86f5b2e86970acd2ce89905e0fbf9f1dcc2"),
        };

        // ---------- Animator-aren definizioa ----------
        // UAL klipen abiadura naturala, root motion bertsiotik neurtua (Blender, ~/ECLIPSE_babeskopia/a2_blender/informes/a2/a2_ual_abiadurak.json):
        // Walk 0,975; Jog 5,357; Sprint 8,25; Crouch_Fwd 0,75 m/s. Atalasea = abiadura naturala: oinak ez dira irristatzen.
        // A3: "Arma" geruza (gorputzaren goiko aldea, maskararekin), lehenetsiz 0 pisua: JokalariArma-k 1 jartzen du pistola duenean.
        private const string Maskara = DatuKarpeta + "/AM_GorputzGoikoa.mask";

        private static (string, AnimatorConditionMode, float)[] B(string param, bool balioa) =>
            new[] { (param, balioa ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f) };

        private static (string, AnimatorConditionMode, float)[] B(string p1, bool b1, string p2, bool b2) =>
            new[] { (p1, b1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f), (p2, b2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f) };

        private static AnimKontrolDef KontrolDefinizioa(AvatarMask goikoa) => new AnimKontrolDef
        {
            Parametroak = new[]
            {
                ("Abiadura", AnimatorControllerParameterType.Float),
                ("AbiaduraX", AnimatorControllerParameterType.Float),
                ("AbiaduraZ", AnimatorControllerParameterType.Float),
                ("Makurtuta", AnimatorControllerParameterType.Bool),
                ("Airean", AnimatorControllerParameterType.Bool),
                ("Apuntatzen", AnimatorControllerParameterType.Bool),
                ("ApuntuPitch", AnimatorControllerParameterType.Float),
                ("Tiro", AnimatorControllerParameterType.Trigger),
                ("Birkarga", AnimatorControllerParameterType.Trigger),
                ("Kaltea", AnimatorControllerParameterType.Trigger),
            },
            Geruzak = new[]
            {
                new AnimGeruzaDef
                {
                    Izena = "Oinarria", Lehenetsia = "Zutik",
                    Egoerak = new[]
                    {
                        new AnimEgoeraDef
                        {
                            Izena = "Zutik", Parametroa = "Abiadura", OinIK = true, Kokapena = new Vector3(300f, 0f),
                            Umeak = new[]
                            {
                                new AnimUmeDef { Klipa = "Idle_Loop", Atalasea = 0f },
                                new AnimUmeDef { Klipa = "Walk_Loop", Atalasea = 0.975f },
                                new AnimUmeDef { Klipa = "Jog_Fwd_Loop", Atalasea = 5.357f },
                                new AnimUmeDef { Klipa = "Sprint_Loop", Atalasea = 8.25f },
                            },
                        },
                        new AnimEgoeraDef
                        {
                            // Makurtuta 1,5 m/s (EsploratzaileKontrola); klipa 0,75 m/s: bi aldiz azkarrago.
                            Izena = "Makurtuta", Parametroa = "Abiadura", OinIK = true, Kokapena = new Vector3(300f, 120f),
                            Umeak = new[]
                            {
                                new AnimUmeDef { Klipa = "Crouch_Idle_Loop", Atalasea = 0f },
                                new AnimUmeDef { Klipa = "Crouch_Fwd_Loop", Atalasea = 1.5f, Denbora = 2f },
                            },
                        },
                        new AnimEgoeraDef { Izena = "Airean", Klipa = "Jump_Loop", Kokapena = new Vector3(560f, 60f) },
                    },
                    Trantsizioak = new[]
                    {
                        new AnimTrantsizioDef { Nondik = "Zutik", Nora = "Makurtuta", Baldintzak = B("Makurtuta", true) },
                        new AnimTrantsizioDef { Nondik = "Makurtuta", Nora = "Zutik", Baldintzak = B("Makurtuta", false) },
                        new AnimTrantsizioDef { Nondik = "Zutik", Nora = "Airean", Baldintzak = B("Airean", true) },
                        new AnimTrantsizioDef { Nondik = "Makurtuta", Nora = "Airean", Baldintzak = B("Airean", true) },
                        new AnimTrantsizioDef { Nondik = "Airean", Nora = "Zutik", Baldintzak = B("Airean", false, "Makurtuta", false) },
                        new AnimTrantsizioDef { Nondik = "Airean", Nora = "Makurtuta", Baldintzak = B("Airean", false, "Makurtuta", true) },
                    },
                },
                new AnimGeruzaDef
                {
                    Izena = "Arma", Maskara = goikoa, Pisua = 0f, Lehenetsia = "Eutsi",
                    Egoerak = new[]
                    {
                        new AnimEgoeraDef { Izena = "Eutsi", Klipa = "Pistol_Idle_Loop", Kokapena = new Vector3(300f, 0f) },
                        new AnimEgoeraDef
                        {
                            Izena = "Apuntatu", Parametroa = "ApuntuPitch", Kokapena = new Vector3(300f, 120f),
                            Umeak = new[]
                            {
                                new AnimUmeDef { Klipa = "Pistol_Aim_Down", Atalasea = -45f },
                                new AnimUmeDef { Klipa = "Pistol_Aim_Neutral", Atalasea = 0f },
                                new AnimUmeDef { Klipa = "Pistol_Aim_Up", Atalasea = 45f },
                            },
                        },
                        new AnimEgoeraDef { Izena = "Tiro", Klipa = "Pistol_Shoot", Kokapena = new Vector3(560f, 120f) },
                        new AnimEgoeraDef { Izena = "Birkarga", Klipa = "Pistol_Reload", Kokapena = new Vector3(560f, 0f) },
                        new AnimEgoeraDef { Izena = "Kaltea", Klipa = "Hit_Chest", Kokapena = new Vector3(560f, 240f) },
                    },
                    Trantsizioak = new[]
                    {
                        new AnimTrantsizioDef { Nondik = "Eutsi", Nora = "Apuntatu", Baldintzak = B("Apuntatzen", true), Iraupena = 0.12f },
                        new AnimTrantsizioDef { Nondik = "Apuntatu", Nora = "Eutsi", Baldintzak = B("Apuntatzen", false), Iraupena = 0.2f },
                        new AnimTrantsizioDef { Nondik = AnimatorEraikitzailea.Any, Nora = "Tiro", Baldintzak = B("Tiro", true), Iraupena = 0.05f },
                        new AnimTrantsizioDef { Nondik = "Tiro", Nora = "Apuntatu", Baldintzak = B("Apuntatzen", true), Irteera = 0.7f, Iraupena = 0.1f },
                        new AnimTrantsizioDef { Nondik = "Tiro", Nora = "Eutsi", Baldintzak = B("Apuntatzen", false), Irteera = 0.7f, Iraupena = 0.2f },
                        new AnimTrantsizioDef { Nondik = AnimatorEraikitzailea.Any, Nora = "Birkarga", Baldintzak = B("Birkarga", true), Iraupena = 0.1f },
                        new AnimTrantsizioDef { Nondik = "Birkarga", Nora = "Eutsi", Irteera = 0.95f, Iraupena = 0.15f },
                        new AnimTrantsizioDef { Nondik = AnimatorEraikitzailea.Any, Nora = "Kaltea", Baldintzak = B("Kaltea", true), Iraupena = 0.05f },
                        new AnimTrantsizioDef { Nondik = "Kaltea", Nora = "Eutsi", Irteera = 0.8f, Iraupena = 0.15f },
                    },
                },
            },
        };

        [MenuItem("ECLIPSE/Jokalaria/Bisuala: aurrebista")]
        private static void MenuAurrebista() => Exekutatu(false);

        [MenuItem("ECLIPSE/Jokalaria/Bisuala: aplikatu")]
        private static void MenuAplikatu()
        {
            if (EditorUtility.DisplayDialog(Tresna, "HypJo-ren materialak, Animator-a, aurrefabrikatua eta proba-eszena sortu edo eguneratu?", "Aplikatu", "Utzi"))
            {
                Exekutatu(true);
            }
        }

        /// <summary>Batchmode: aurrebista; -eclipseAplikatu-rekin, aplikatu.</summary>
        public static void BatchJokalariBisuala()
        {
            bool aplikatu = Environment.GetCommandLineArgs().Contains("-eclipseAplikatu");
            EditorApplication.Exit(Exekutatu(aplikatu) ? 0 : 1);
        }

        private static bool Exekutatu(bool aplikatu)
        {
            var aldaketak = new List<string>();
            var egiaztapenak = new List<string>();
            var informea = new Informea(Tresna, Marka, aplikatu ? "Aplikatu" : "Aurrebista", ProbaEszena, "Jokalaria (HypJo v2)");
            try
            {
                foreach ((string bidea, string sha) in Jatorrizkoak)
                {
                    string benetakoa = Sha256(bidea);
                    if (benetakoa != sha)
                    {
                        throw new InvalidOperationException($"{bidea}: SHA-256 ez dator bat ({benetakoa ?? "ez dago"}).");
                    }
                }

                egiaztapenak.Add($"{Jatorrizkoak.Length} jatorrizko, SHA-256 zuzena");

                Karpetak(aplikatu, aldaketak);
                Testurak(aplikatu, aldaketak);
                Material gorputza = Materiala(GorputzMat, false, aplikatu, aldaketak);
                Material zapia = Materiala(ZapiMat, true, aplikatu, aldaketak);
                HypJoInportazioa(gorputza, zapia, aplikatu, aldaketak, egiaztapenak);
                UalInportazioa(aplikatu, aldaketak, egiaztapenak);
                AvatarMask goikoa = AnimatorEraikitzailea.GoikoMaskara(Maskara, aplikatu, aldaketak);
                Dictionary<string, AnimationClip> klipak = UalKlipak();
                AnimKontrolDef definizioa = KontrolDefinizioa(goikoa);
                if (AnimatorEraikitzailea.Egiaztatu(Kontrolagailua, definizioa, k => klipak.TryGetValue(k, out AnimationClip c) ? c : null, aplikatu, out AnimatorController kontrolagailua))
                {
                    aldaketak.Add($"Animator Controller {(aplikatu ? "eraikia" : "eraikiko litzateke")}: {Kontrolagailua} — Oinarria (3 egoera) + Arma (5 egoera, maskara, pisua 0)");
                }
                AurrefabrikatuaEgiaztatu(kontrolagailua, gorputza, zapia, aplikatu, aldaketak, egiaztapenak);
                ProbaEszenaEgiaztatu(aplikatu, aldaketak);

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

        internal static string Sha256(string assetBidea)
        {
            string fitxategia = Path.GetFullPath(assetBidea);
            if (!File.Exists(fitxategia))
            {
                return null;
            }

            using SHA256 sha = SHA256.Create();
            using FileStream fluxua = File.OpenRead(fitxategia);
            return BitConverter.ToString(sha.ComputeHash(fluxua)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void Karpetak(bool aplikatu, List<string> aldaketak)
        {
            foreach (string karpeta in new[] { DatuKarpeta, ProbaKarpeta, "Assets/Resources", "Assets/Resources/Jokalaria", "Assets/_Eclipse/Eszenak/Proba" })
            {
                if (AssetDatabase.IsValidFolder(karpeta))
                {
                    continue;
                }

                aldaketak.Add($"Karpeta berria: {karpeta}");
                if (aplikatu)
                {
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(karpeta).Replace('\\', '/'), Path.GetFileName(karpeta));
                }
            }
        }

        private static void Testurak(bool aplikatu, List<string> aldaketak)
        {
            foreach (string izena in new[] { "T_Cyborg_BaseColor_2k", "T_Cyborg_Emission_2k", "T_Cyborg_MetallicSmoothness_URP_2k" })
            {
                string bidea = $"{HypJoKarpeta}/{izena}.png";
                var inportatzailea = (TextureImporter)AssetImporter.GetAtPath(bidea);
                bool srgb = !izena.Contains("Metallic");
                if (inportatzailea.sRGBTexture == srgb && inportatzailea.maxTextureSize == 2048 && inportatzailea.mipmapEnabled)
                {
                    continue;
                }

                aldaketak.Add($"Testura: {izena} (sRGB {srgb}, 2048, mipmap)");
                if (aplikatu)
                {
                    inportatzailea.sRGBTexture = srgb;
                    inportatzailea.maxTextureSize = 2048;
                    inportatzailea.mipmapEnabled = true;
                    inportatzailea.SaveAndReimport();
                }
            }
        }

        /// <summary>P3-ko emisioa (HDR, ×2,2); alfa 1, Unity-k ez baitu 1etik gorako alfarik gordetzen.</summary>
        private static readonly Color EmisioKolorea = new Color(2.2f, 2.2f, 2.2f, 1f);

        /// <summary>P3-ko materialak: gorputza testurekin (metala + emisioa GI gabe), zapia kolore lauarekin.</summary>
        private static Material Materiala(string bidea, bool zapia, bool aplikatu, List<string> aldaketak)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(bidea);
            Texture2D oinarria = AssetDatabase.LoadAssetAtPath<Texture2D>($"{HypJoKarpeta}/T_Cyborg_BaseColor_2k.png");
            Texture2D metala = AssetDatabase.LoadAssetAtPath<Texture2D>($"{HypJoKarpeta}/T_Cyborg_MetallicSmoothness_URP_2k.png");
            Texture2D emisioa = AssetDatabase.LoadAssetAtPath<Texture2D>($"{HypJoKarpeta}/T_Cyborg_Emission_2k.png");
            Color zapiKolorea = new Color(0.007f, 0.0075f, 0.0085f).gamma;

            var ezberdinak = new List<string>();
            if (m == null || m.shader != lit)
            {
                ezberdinak.Add(m == null ? "ez dago" : "shader");
            }
            else if (zapia)
            {
                if (m.GetColor("_BaseColor") != zapiKolorea) ezberdinak.Add("_BaseColor");
                if (!Mathf.Approximately(m.GetFloat("_Metallic"), 0f)) ezberdinak.Add("_Metallic");
                if (!Mathf.Approximately(m.GetFloat("_Smoothness"), 0.06f)) ezberdinak.Add("_Smoothness");
            }
            else
            {
                if (m.GetTexture("_BaseMap") != oinarria) ezberdinak.Add("_BaseMap");
                if (m.GetTexture("_MetallicGlossMap") != metala) ezberdinak.Add("_MetallicGlossMap");
                if (m.GetTexture("_EmissionMap") != emisioa) ezberdinak.Add("_EmissionMap");
                if (!m.IsKeywordEnabled("_METALLICSPECGLOSSMAP")) ezberdinak.Add("_METALLICSPECGLOSSMAP");
                if (!m.IsKeywordEnabled("_EMISSION")) ezberdinak.Add("_EMISSION");
                if (!Mathf.Approximately(m.GetFloat("_Smoothness"), 1f)) ezberdinak.Add("_Smoothness");
                if (m.GetColor("_EmissionColor") != EmisioKolorea) ezberdinak.Add($"_EmissionColor {m.GetColor("_EmissionColor")}");
                if (m.globalIlluminationFlags != MaterialGlobalIlluminationFlags.RealtimeEmissive) ezberdinak.Add($"GI {m.globalIlluminationFlags}");
            }

            if (ezberdinak.Count == 0)
            {
                return m;
            }

            aldaketak.Add($"Materiala {(m == null ? "berria" : "eguneratua")}: {bidea} ({string.Join(", ", ezberdinak)})");
            if (!aplikatu)
            {
                return m;
            }

            bool berria = m == null;
            if (berria)
            {
                m = new Material(lit) { name = Path.GetFileNameWithoutExtension(bidea) };
            }
            else
            {
                Undo.RecordObject(m, Tresna);
                m.shader = lit;
            }

            if (zapia)
            {
                m.SetColor("_BaseColor", zapiKolorea);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0.06f);
            }
            else
            {
                m.SetTexture("_BaseMap", oinarria);
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_MetallicGlossMap", metala);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", 1f);
                m.SetTexture("_EmissionMap", emisioa);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", EmisioKolorea);
                // URP-k _EMISSION kentzen du AnyEmissive ez badago (ikus EszenaPropioa): RealtimeEmissive = distira ikusgai, bake-ari gehitu gabe.
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (berria)
            {
                AssetDatabase.CreateAsset(m, bidea);
            }
            else
            {
                EditorUtility.SetDirty(m);
            }

            return m;
        }

        private static bool Atzamarra(string izena) =>
            izena.Contains("Index") || izena.Contains("Thumb") || izena.Contains("Middle") || izena.Contains("Ring") || izena.Contains("Little");

        /// <summary>P3-ko konfigurazioa: Humanoid, Avatar atzamarrik gabe (ukabil fusionatuak), animaziorik gabe, materialak gureetara.</summary>
        private static void HypJoInportazioa(Material gorputza, Material zapia, bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> mapa = imp.GetExternalObjectMap();
            bool MapaZuzena(string iturria, Material helburua) =>
                helburua != null && mapa.TryGetValue(new AssetImporter.SourceAssetIdentifier(typeof(Material), iturria), out UnityEngine.Object m) && m == helburua;

            HumanDescription hd = imp.humanDescription;
            bool zuzena = imp.animationType == ModelImporterAnimationType.Human && imp.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel
                          && !imp.importAnimation && !imp.importBlendShapes && !imp.importCameras && !imp.importLights
                          && hd.human != null && hd.human.Length > 0 && !hd.human.Any(h => Atzamarra(h.humanName))
                          && MapaZuzena(GorputzIturria, gorputza) && MapaZuzena(ZapiIturria, zapia);
            if (!zuzena)
            {
                aldaketak.Add($"Inportazioa: {Fbx} → Humanoid, atzamarrik gabe, animaziorik gabe, materialak birbideratuta");
                if (!aplikatu)
                {
                    return;
                }

                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = false;
                imp.importBlendShapes = false;
                imp.importCameras = false;
                imp.importLights = false;
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), GorputzIturria), gorputza);
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), ZapiIturria), zapia);
                imp.SaveAndReimport();

                // Mapa automatikoa sortu ondoren, atzamarrak kendu.
                imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
                hd = imp.humanDescription;
                string[] kenduak = hd.human.Where(h => Atzamarra(h.humanName)).Select(h => h.humanName).ToArray();
                hd.human = hd.human.Where(h => !Atzamarra(h.humanName)).ToArray();
                imp.humanDescription = hd;
                imp.SaveAndReimport();
                aldaketak.Add($"Avatarretik kendutako atzamarrak ({kenduak.Length}): {string.Join(", ", kenduak)}");
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            string[] faltan = HumanTrait.BoneName.Where((n, i) => HumanTrait.RequiredBone(i) && !imp.humanDescription.human.Any(h => h.humanName == n)).ToArray();
            if (aplikatu || zuzena)
            {
                if (avatar == null || !avatar.isValid || !avatar.isHuman || faltan.Length > 0)
                {
                    throw new InvalidOperationException($"HypJo Avatar baliogabea: valid={avatar != null && avatar.isValid} human={avatar != null && avatar.isHuman}; derrigorrezkoak falta: {string.Join(",", faltan)}");
                }

                egiaztapenak.Add($"HypJo Avatar: isValid, isHuman, {imp.humanDescription.human.Length} hezur mapeatuta, derrigorrezko guztiak");
            }
        }

        /// <summary>E0/P3-ko konfigurazioa: Humanoid, root motion gabe (erroa posean), _Loop klipak begizta.</summary>
        private static void UalInportazioa(bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(Ual);
            bool KlipaZuzena(ModelImporterClipAnimation k) =>
                k.name == KlipIzena(k.takeName)
                && k.loopTime == k.name.EndsWith("_Loop", StringComparison.OrdinalIgnoreCase) && !k.loopPose
                && k.lockRootRotation && k.lockRootPositionXZ && k.lockRootHeightY
                && !k.keepOriginalPositionXZ && !k.keepOriginalOrientation && k.keepOriginalPositionY;

            bool zuzena = imp.bakeAxisConversion && imp.animationType == ModelImporterAnimationType.Human
                          && imp.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel && imp.importAnimation
                          && string.IsNullOrEmpty(imp.motionNodeName)
                          && imp.clipAnimations.Length > 0 && imp.clipAnimations.All(KlipaZuzena);
            if (!zuzena)
            {
                aldaketak.Add($"Inportazioa: {Ual} → Humanoid, root motion gabe, _Loop klipak begizta");
                if (!aplikatu)
                {
                    return;
                }

                imp.bakeAxisConversion = true;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = true;
                imp.SaveAndReimport();

                imp = (ModelImporter)AssetImporter.GetAtPath(Ual);
                imp.motionNodeName = string.Empty;
                ModelImporterClipAnimation[] klipak = imp.defaultClipAnimations;
                foreach (ModelImporterClipAnimation k in klipak)
                {
                    k.name = KlipIzena(k.takeName);
                    k.loopTime = k.name.EndsWith("_Loop", StringComparison.OrdinalIgnoreCase);
                    k.loopPose = false;
                    k.lockRootRotation = true;
                    k.lockRootPositionXZ = true;
                    k.lockRootHeightY = true;
                    k.keepOriginalPositionXZ = false;
                    k.keepOriginalOrientation = false;
                    k.keepOriginalPositionY = true;
                }

                imp.clipAnimations = klipak;
                imp.SaveAndReimport();
            }

            if (aplikatu || zuzena)
            {
                Dictionary<string, AnimationClip> klipakIzenez = UalKlipak();
                string[] behar = KontrolDefinizioa(null).Geruzak.SelectMany(g => g.Egoerak)
                    .SelectMany(e => e.Umeak != null ? e.Umeak.Select(u => u.Klipa) : e.Klipa != null ? new[] { e.Klipa } : Array.Empty<string>()).ToArray();
                string[] faltan = behar.Where(b => !klipakIzenez.ContainsKey(b)).ToArray();
                if (faltan.Length > 0)
                {
                    throw new InvalidOperationException($"UAL klipak falta dira: {string.Join(", ", faltan)}");
                }

                Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Ual).OfType<Avatar>().FirstOrDefault();
                egiaztapenak.Add($"UAL: {klipakIzenez.Count} klip, Avatar human={avatar != null && avatar.isHuman}; erabiliak: {string.Join(", ", behar)}");
            }
        }

        /// <summary>FBX-ko hartzearen izena aurrizkirik gabe: "Armature|Idle_Loop" → "Idle_Loop".</summary>
        private static string KlipIzena(string hartzea) => hartzea.Substring(hartzea.LastIndexOf('|') + 1);

        internal static Dictionary<string, AnimationClip> UalKlipak() =>
            AssetDatabase.LoadAllAssetRepresentationsAtPath(Ual).OfType<AnimationClip>()
                .Where(k => !k.name.StartsWith("__preview__", StringComparison.Ordinal))
                .GroupBy(k => k.name).ToDictionary(g => g.Key, g => g.First());

        private static Transform EskuinEskua(Transform erroa) =>
            erroa.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "mixamorig:RightHand");

        /// <summary>Aurrefabrikatua zuzena den: JokalariBisuala, Animator, kontrolagailua, avatarra, geruza, eskala, eskuineko eskuko ainguraketa, kolisiorik ez.</summary>
        private static List<string> AurrefabEzberdintasunak(GameObject aurrefab, AnimatorController ac, Avatar avatar, Material gorputza, Material zapia)
        {
            var ezberdinak = new List<string>();
            if (aurrefab == null)
            {
                ezberdinak.Add("ez dago");
                return ezberdinak;
            }

            var bisuala = aurrefab.GetComponent<JokalariBisuala>();
            Animator an = bisuala != null ? new SerializedObject(bisuala).FindProperty("_animator").objectReferenceValue as Animator : null;
            if (bisuala == null || an == null) ezberdinak.Add("JokalariBisuala edo Animator falta");
            if (an != null && an.runtimeAnimatorController != ac) ezberdinak.Add("kontrolagailua");
            if (an != null && an.avatar != avatar) ezberdinak.Add("avatarra");
            if (an != null && (an.applyRootMotion || an.cullingMode != AnimatorCullingMode.AlwaysAnimate)) ezberdinak.Add("root motion / culling");
            if (an != null && an.transform.localScale != Vector3.one * Eskala) ezberdinak.Add("eskala");
            if (aurrefab.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject.layer != 2)) ezberdinak.Add("geruza (Ignore Raycast)");
            if (aurrefab.GetComponentsInChildren<Collider>(true).Length > 0 || aurrefab.GetComponentsInChildren<CharacterController>(true).Length > 0) ezberdinak.Add("kolisioak");
            Transform eskua = EskuinEskua(aurrefab.transform);
            if (eskua == null || eskua.Find("Arma_Eskuina") == null) ezberdinak.Add("Arma_Eskuina");
            foreach (Renderer r in aurrefab.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterials.Any(m => m != gorputza && m != zapia)) ezberdinak.Add($"materiala {r.name}");
            }

            return ezberdinak;
        }

        private static void AurrefabrikatuaEgiaztatu(AnimatorController ac, Material gorputza, Material zapia, bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            List<string> ezberdinak = AurrefabEzberdintasunak(AssetDatabase.LoadAssetAtPath<GameObject>(AurrefabBidea), ac, avatar, gorputza, zapia);
            if (ezberdinak.Count == 0)
            {
                egiaztapenak.Add($"Aurrefabrikatua zuzena: {AurrefabBidea}");
                return;
            }

            aldaketak.Add($"Aurrefabrikatua {AurrefabBidea}: {string.Join(", ", ezberdinak)}");
            if (!aplikatu)
            {
                return;
            }

            // Aurrebistako eszena batean eraiki: irekita dagoen eszena ez da zikintzen.
            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                var erroa = new GameObject("PF_Jokalaria_HypJo");
                SceneManager.MoveGameObjectToScene(erroa, aurrebista);
                var eredua = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx), aurrebista);
                eredua.name = "HypJo";
                eredua.transform.SetParent(erroa.transform, false);
                eredua.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                eredua.transform.localScale = Vector3.one * Eskala;
                Animator an = eredua.GetComponent<Animator>();
                if (an == null)
                {
                    an = eredua.AddComponent<Animator>();
                }

                an.avatar = avatar;
                an.runtimeAnimatorController = ac;
                an.applyRootMotion = false;
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Transform eskua = EskuinEskua(eredua.transform) ?? throw new InvalidOperationException("mixamorig:RightHand ez da aurkitu");
                var arma = new GameObject("Arma_Eskuina");
                arma.transform.SetParent(eskua, false);

                var bisuala = erroa.AddComponent<JokalariBisuala>();
                var serializatua = new SerializedObject(bisuala);
                serializatua.FindProperty("_animator").objectReferenceValue = an;
                serializatua.ApplyModifiedPropertiesWithoutUndo();

                foreach (Transform t in erroa.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = 2;
                }

                foreach (Renderer r in erroa.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.receiveShadows = true;
                }

                PrefabUtility.SaveAsPrefabAsset(erroa, AurrefabBidea, out bool gordeta);
                if (!gordeta)
                {
                    throw new InvalidOperationException($"Ezin izan da gorde {AurrefabBidea}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            List<string> ondoren = AurrefabEzberdintasunak(AssetDatabase.LoadAssetAtPath<GameObject>(AurrefabBidea), ac, avatar, gorputza, zapia);
            if (ondoren.Count > 0)
            {
                throw new InvalidOperationException($"Aurrefabrikatua gorde ondoren ez da zuzena: {string.Join(", ", ondoren)}");
            }

            egiaztapenak.Add($"Aurrefabrikatua gordeta eta egiaztatuta: {AurrefabBidea}");
        }

        // ---------- 03_JokalariProba: ingurune neutroa, primitiboekin bakarrik (Asset Store-ko aktiborik gabe) ----------

        private static void ProbaEszenaEgiaztatu(bool aplikatu, List<string> aldaketak)
        {
            if (File.Exists(ProbaEszena))
            {
                return;
            }

            aldaketak.Add($"Proba-eszena berria: {ProbaEszena} (zorua, hormak, pasilloa, hodia 1,425 m-tan, koska 0,3 m, plataforma 0,8 m)");
            if (!aplikatu)
            {
                return;
            }

            // Eszena berria bakarrik: lehenago irekitakoa ez da aldatzen; editorean, gorde gabeko aldaketak badaude, Ion-i galdetzen zaio.
            string aurrekoa = SceneManager.GetActiveScene().path;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException("Proba-eszena ez da sortu: irekitako eszena gorde gabe dago.");
            }

            Scene eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProbaEszenaEraiki();
            if (!EditorSceneManager.SaveScene(eszena, ProbaEszena))
            {
                throw new InvalidOperationException($"Ezin izan da gorde {ProbaEszena}");
            }

            if (!string.IsNullOrEmpty(aurrekoa))
            {
                EditorSceneManager.OpenScene(aurrekoa, OpenSceneMode.Single);
            }
        }

        internal static Material ProbaMateriala(string izena, Color kolorea, Texture2D testura = null, float teilak = 1f)
        {
            string bidea = $"{ProbaKarpeta}/{izena}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(bidea);
            if (m != null)
            {
                return m;
            }

            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = izena };
            m.SetColor("_BaseColor", kolorea);
            m.SetFloat("_Smoothness", 0.25f);
            if (testura != null)
            {
                m.SetTexture("_BaseMap", testura);
                m.SetTextureScale("_BaseMap", Vector2.one * teilak);
            }

            AssetDatabase.CreateAsset(m, bidea);
            return m;
        }

        internal static Texture2D Laukiak()
        {
            string bidea = $"{ProbaKarpeta}/T_Proba_Laukiak.asset";
            var testura = AssetDatabase.LoadAssetAtPath<Texture2D>(bidea);
            if (testura != null)
            {
                return testura;
            }

            const int Tamaina = 256;
            testura = new Texture2D(Tamaina, Tamaina, TextureFormat.RGBA32, true) { name = "T_Proba_Laukiak", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            var pixelak = new Color32[Tamaina * Tamaina];
            for (int y = 0; y < Tamaina; y++)
            {
                for (int x = 0; x < Tamaina; x++)
                {
                    bool argia = (x / (Tamaina / 2) + y / (Tamaina / 2)) % 2 == 0;
                    bool lerroa = x % (Tamaina / 2) < 2 || y % (Tamaina / 2) < 2;
                    byte b = lerroa ? (byte)40 : argia ? (byte)150 : (byte)110;
                    pixelak[y * Tamaina + x] = new Color32(b, b, (byte)Mathf.Min(255, b + 8), 255);
                }
            }

            testura.SetPixels32(pixelak);
            testura.Apply(true);
            AssetDatabase.CreateAsset(testura, bidea);
            return testura;
        }

        internal static GameObject Kubo(string izena, Transform gurasoa, Vector3 erdia, Vector3 tamaina, Material materiala, float biraketaX = 0f)
        {
            GameObject kuboa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kuboa.name = izena;
            kuboa.transform.SetParent(gurasoa, false);
            kuboa.transform.SetLocalPositionAndRotation(erdia, Quaternion.Euler(biraketaX, 0f, 0f));
            kuboa.transform.localScale = tamaina;
            kuboa.GetComponent<Renderer>().sharedMaterial = materiala;
            return kuboa;
        }

        /// <summary>Proba-eszenen argiztapen neutroa (03 eta 04): giro-argia, bi argi zuzen eta islapen-zunda denbora errealean.</summary>
        internal static void ArgiNeutroak(Transform erroa)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.45f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.32f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.16f);

            var argiak = new GameObject("Argiak").transform;
            argiak.SetParent(erroa, false);
            var eguzkia = new GameObject("Argi_Nagusia").AddComponent<Light>();
            eguzkia.transform.SetParent(argiak, false);
            eguzkia.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            eguzkia.type = LightType.Directional;
            eguzkia.intensity = 1.6f;
            eguzkia.shadows = LightShadows.Soft;
            var betegarria = new GameObject("Argi_Betegarria").AddComponent<Light>();
            betegarria.transform.SetParent(argiak, false);
            betegarria.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
            betegarria.type = LightType.Directional;
            betegarria.intensity = 0.5f;
            betegarria.shadows = LightShadows.None;
            var zunda = new GameObject("Islapen_Zunda").AddComponent<ReflectionProbe>();
            zunda.transform.SetParent(argiak, false);
            zunda.transform.localPosition = new Vector3(0f, 2f, 0f);
            zunda.mode = ReflectionProbeMode.Realtime;
            zunda.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            zunda.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            zunda.size = new Vector3(40f, 8f, 40f);
        }

        private static void ProbaEszenaEraiki()
        {
            // Laukiak: 2x2 lauki testurako, 20 teila 40 m-tan → lauki bakoitza 1 m.
            Material zorua = ProbaMateriala("M_Proba_Zorua", Color.white, Laukiak(), 20f);
            Material horma = ProbaMateriala("M_Proba_Horma", new Color(0.55f, 0.57f, 0.6f));
            Material oztopoa = ProbaMateriala("M_Proba_Oztopoa", new Color(0.95f, 0.45f, 0.1f));

            var erroa = new GameObject("JokalariProba").transform;
            ArgiNeutroak(erroa);

            var geometria = new GameObject("Geometria").transform;
            geometria.SetParent(erroa, false);
            Kubo("Zorua", geometria, new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), zorua);
            Kubo("Horma_Iparra", geometria, new Vector3(0f, 1.5f, 20.25f), new Vector3(41f, 3f, 0.5f), horma);
            Kubo("Horma_Hegoa", geometria, new Vector3(0f, 1.5f, -20.25f), new Vector3(41f, 3f, 0.5f), horma);
            Kubo("Horma_Ekialdea", geometria, new Vector3(20.25f, 1.5f, 0f), new Vector3(0.5f, 3f, 41f), horma);
            Kubo("Horma_Mendebaldea", geometria, new Vector3(-20.25f, 1.5f, 0f), new Vector3(0.5f, 3f, 41f), horma);

            // Pasilloa X=10 inguruan, 2,4 m zabal, Z ardatzean: sorbaldako kamera hormen ondoan probatzeko.
            Kubo("Pasilloa_Horma_A", geometria, new Vector3(8.95f, 1.5f, 0f), new Vector3(0.5f, 3f, 12f), horma);
            Kubo("Pasilloa_Horma_B", geometria, new Vector3(11.65f, 1.5f, 0f), new Vector3(0.5f, 3f, 12f), horma);
            // Hodia: azpian 1,425 m libre (M2-ko PF_Hodia_Grisa__005 bezala): zutik ez, makurtuta bai.
            Kubo("Hodia_1425", geometria, new Vector3(10.3f, 1.425f + 0.175f, 0f), new Vector3(2.4f, 0.35f, 0.35f), oztopoa);
            Kubo("Koska_030", geometria, new Vector3(-8f, 0.15f, -4f), new Vector3(3f, 0.3f, 2f), oztopoa);
            Kubo("Plataforma_080", geometria, new Vector3(-8f, 0.4f, 4f), new Vector3(3f, 0.8f, 3f), horma);

            var spawn = new GameObject("Spawn_Jokalaria").transform;
            spawn.SetParent(erroa, false);
            spawn.SetLocalPositionAndRotation(new Vector3(0f, 0f, -12f), Quaternion.identity);

            var kamera = new GameObject("Main Camera") { tag = "MainCamera" };
            kamera.transform.SetParent(erroa, false);
            kamera.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.65f, -12f), Quaternion.identity);
            Camera c = kamera.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
            kamera.AddComponent<AudioListener>();

            var abiarazlea = new GameObject("Abiarazlea").AddComponent<JokalariProbaAbiarazlea>();
            abiarazlea.transform.SetParent(erroa, false);
            var serializatua = new SerializedObject(abiarazlea);
            serializatua.FindProperty("_spawn").objectReferenceValue = spawn;
            serializatua.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
