using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Borroka;
using Eclipse.Misioak;
using Eclipse.Proba;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3 (lehen blokea): borrokaren aktiboak. Aurrebista (ezer aldatu gabe) eta Aplikatu; idenpotentea.
    /// - EMP pistola propioa (primitiboekin kitbash-a, argi zianak), izpia, distira, txinpartak eta SoX-ekin sortutako soinuak.
    /// - Behin-behineko etsaia: Quaternius UAL-eko maniki CC0-a, 10 kalte-zonarekin, NavMeshAgent, Animator eta odola.
    /// - 04_BorrokaProba: eszena neutroa (primitiboak), markatzaileak, EtsaiSortzailea eta kolisio fisikoetatik eraikitako NavMesh-a.
    /// Misioetako eszenak ez dira ukitzen.
    /// </summary>
    public static class BorrokaTresna
    {
        private const string Tresna = "BorrokaTresna";
        private const string Marka = "ECL_BORROKATRESNA";

        private const string DatuKarpeta = "Assets/_Eclipse/Datuak/Borroka";
        private const string ResKarpeta = "Assets/Resources/Borroka";
        private const string AudioKarpeta = "Assets/_Eclipse/Audioa/Borroka";
        private const string PistolaMesh = DatuKarpeta + "/SM_EmpPistola.asset";
        private const string PistolaAurrefab = ResKarpeta + "/PF_EmpPistola.prefab";
        internal const string EtsaiAurrefab = ResKarpeta + "/PF_Etsaia_Proba.prefab";
        private const string EtsaiKontrolagailua = DatuKarpeta + "/AC_Etsaia_Proba.controller";
        internal const string ProbaEszena = "Assets/_Eclipse/Eszenak/Proba/04_BorrokaProba.unity";
        private const string NavMeshBidea = DatuKarpeta + "/NM_04_BorrokaProba.asset";

        /// <summary>Etsaiaren aurrefabrikatuaren bertsio-marka: geometria aldatzean igo, tresnak berreraiki dezan.</summary>
        private const string EtsaiBertsioa = "Bertsioa_2";

        private static readonly (string Izena, AnimatorControllerParameterType Mota)[] EtsaiParametroak =
        {
            ("Abiadura", AnimatorControllerParameterType.Float),
            ("Eraso", AnimatorControllerParameterType.Trigger),
            ("Kaltea", AnimatorControllerParameterType.Trigger),
            ("Hilda", AnimatorControllerParameterType.Bool),
        };

        internal static (string, AnimatorConditionMode, float)[] B(string param, bool balioa) =>
            new[] { (param, balioa ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f) };

        internal static AnimKontrolDef EtsaiDefinizioa() => new AnimKontrolDef
        {
            Parametroak = EtsaiParametroak,
            Geruzak = new[]
            {
                new AnimGeruzaDef
                {
                    Izena = "Oinarria", Lehenetsia = "Mugimendua",
                    Egoerak = new[]
                    {
                        new AnimEgoeraDef
                        {
                            Izena = "Mugimendua", Parametroa = "Abiadura", OinIK = true, Kokapena = new Vector3(300f, 0f),
                            Umeak = new[]
                            {
                                new AnimUmeDef { Klipa = "Idle_Loop", Atalasea = 0f },
                                new AnimUmeDef { Klipa = "Walk_Loop", Atalasea = 0.975f },
                                new AnimUmeDef { Klipa = "Jog_Fwd_Loop", Atalasea = 5.357f },
                            },
                        },
                        new AnimEgoeraDef { Izena = "Eraso", Klipa = "Punch_Cross", Kokapena = new Vector3(560f, 0f) },
                        new AnimEgoeraDef { Izena = "Kaltea", Klipa = "Hit_Chest", Kokapena = new Vector3(560f, 120f) },
                        new AnimEgoeraDef { Izena = "Hil", Klipa = "Death01", Kokapena = new Vector3(300f, 240f) },
                    },
                    Trantsizioak = new[]
                    {
                        new AnimTrantsizioDef { Nondik = AnimatorEraikitzailea.Any, Nora = "Eraso", Baldintzak = B("Eraso", true), Iraupena = 0.1f },
                        new AnimTrantsizioDef { Nondik = "Eraso", Nora = "Mugimendua", Irteera = 0.9f, Iraupena = 0.15f },
                        new AnimTrantsizioDef { Nondik = AnimatorEraikitzailea.Any, Nora = "Kaltea", Baldintzak = B("Kaltea", true), Iraupena = 0.05f },
                        new AnimTrantsizioDef { Nondik = "Kaltea", Nora = "Mugimendua", Irteera = 0.85f, Iraupena = 0.15f },
                        new AnimTrantsizioDef { Nondik = AnimatorEraikitzailea.Any, Nora = "Hil", Baldintzak = B("Hilda", true), Iraupena = 0.1f },
                    },
                },
            },
        };

        [MenuItem("ECLIPSE/Borroka/Aktiboak: aurrebista")]
        private static void MenuAurrebista() => Exekutatu(false);

        [MenuItem("ECLIPSE/Borroka/Aktiboak: aplikatu")]
        private static void MenuAplikatu()
        {
            if (EditorUtility.DisplayDialog(Tresna, "EMP pistola, behin-behineko etsaia eta 04_BorrokaProba sortu edo eguneratu?", "Aplikatu", "Utzi"))
            {
                Exekutatu(true);
            }
        }

        /// <summary>Batchmode: aurrebista; -eclipseAplikatu-rekin, aplikatu.</summary>
        public static void BatchBorrokaTresna()
        {
            bool aplikatu = Environment.GetCommandLineArgs().Contains("-eclipseAplikatu");
            EditorApplication.Exit(Exekutatu(aplikatu) ? 0 : 1);
        }

        private static bool Exekutatu(bool aplikatu)
        {
            var aldaketak = new List<string>();
            var egiaztapenak = new List<string>();
            var informea = new Informea(Tresna, Marka, aplikatu ? "Aplikatu" : "Aurrebista", ProbaEszena, "Borroka (A3)");
            try
            {
                Karpetak(aplikatu, aldaketak);
                var soinuak = new Dictionary<string, AudioClip>();
                foreach (string izena in new[] { "SFX_Emp_Tiroa", "SFX_Emp_Hutsik", "SFX_Emp_Birkarga", "SFX_Inpaktua" })
                {
                    AudioClip klipa = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioKarpeta}/{izena}.wav");
                    soinuak[izena] = klipa ?? throw new InvalidOperationException($"Soinua falta da: {AudioKarpeta}/{izena}.wav (SoX-ekin sortua)");
                }

                egiaztapenak.Add($"{soinuak.Count} soinu (SoX, propioak)");

                Material gorputza = LitMateriala("M_EmpPistola_Gorputza", new Color(0.12f, 0.13f, 0.15f), 0.85f, 0.55f, null, aplikatu, aldaketak);
                Material argia = LitMateriala("M_EmpPistola_Argia", new Color(0.02f, 0.05f, 0.06f), 0f, 0.6f, new Color(0.35f, 2.2f, 2.6f), aplikatu, aldaketak);
                Material etsaiMat = LitMateriala("M_Etsaia_Proba", new Color(0.32f, 0.12f, 0.1f), 0.1f, 0.35f, null, aplikatu, aldaketak);
                Material izpia = PartikulaMateriala("M_Emp_Izpia", new Color(0.45f, 0.95f, 1f, 1f), true, aplikatu, aldaketak, testurarekin: false);
                Material txinpartak = PartikulaMateriala("M_Emp_Txinpartak", new Color(0.6f, 0.95f, 1f, 1f), true, aplikatu, aldaketak);
                Material odola = PartikulaMateriala("M_Odola", new Color(0.32f, 0.01f, 0.01f, 1f), false, aplikatu, aldaketak);

                Mesh malla = PistolaMalla(aplikatu, aldaketak);
                PistolaAurrefabrikatua(malla, gorputza, argia, izpia, txinpartak, soinuak, aplikatu, aldaketak, egiaztapenak);

                Dictionary<string, AnimationClip> klipak = JokalariBisualTresna.UalKlipak();
                if (AnimatorEraikitzailea.Egiaztatu(EtsaiKontrolagailua, EtsaiDefinizioa(), k => klipak.TryGetValue(k, out AnimationClip c) ? c : null, aplikatu, out AnimatorController ac))
                {
                    aldaketak.Add($"Animator Controller: {EtsaiKontrolagailua} (Mugimendua, Eraso, Kaltea, Hil)");
                }

                EtsaiAurrefabrikatua(ac, etsaiMat, odola, txinpartak, aplikatu, aldaketak, egiaztapenak);
                ProbaEszenaEgiaztatu(aplikatu, aldaketak, egiaztapenak);

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

        private static void Karpetak(bool aplikatu, List<string> aldaketak)
        {
            foreach (string karpeta in new[] { DatuKarpeta, ResKarpeta, AudioKarpeta, "Assets/_Eclipse/Eszenak/Proba" })
            {
                if (!AssetDatabase.IsValidFolder(karpeta))
                {
                    aldaketak.Add($"Karpeta berria: {karpeta}");
                    if (aplikatu)
                    {
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(karpeta).Replace('\\', '/'), Path.GetFileName(karpeta));
                    }
                }
            }
        }

        // ---------- Materialak ----------

        internal static Material LitMateriala(string izena, Color kolorea, float metala, float leuntasuna, Color? emisioa, bool aplikatu, List<string> aldaketak, string karpeta = DatuKarpeta)
        {
            string bidea = $"{karpeta}/{izena}.mat";
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(bidea);
            bool zuzena = m != null && m.shader == lit && m.GetColor("_BaseColor") == kolorea && Mathf.Approximately(m.GetFloat("_Metallic"), metala)
                          && Mathf.Approximately(m.GetFloat("_Smoothness"), leuntasuna)
                          && (emisioa == null ? !m.IsKeywordEnabled("_EMISSION") : m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor") == emisioa.Value);
            if (zuzena)
            {
                return m;
            }

            aldaketak.Add($"Materiala {(m == null ? "berria" : "eguneratua")}: {bidea}");
            if (!aplikatu)
            {
                return m;
            }

            bool berria = m == null;
            m ??= new Material(lit) { name = izena };
            m.shader = lit;
            m.SetColor("_BaseColor", kolorea);
            m.SetFloat("_Metallic", metala);
            m.SetFloat("_Smoothness", leuntasuna);
            if (emisioa.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emisioa.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
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

        /// <summary>URP partikula argigabea: gehigarria (izpia, txinpartak) edo alfa-nahasketa (odola).</summary>
        internal static Material PartikulaMateriala(string izena, Color kolorea, bool gehigarria, bool aplikatu, List<string> aldaketak, bool testurarekin = true, string karpeta = DatuKarpeta)
        {
            string bidea = $"{karpeta}/{izena}.mat";
            Shader partikula = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(bidea);
            float nahasketa = gehigarria ? 2f : 0f;
            // Unity-ren partikula-testura biguna: hura gabe, partikulak karratuak dira.
            Texture2D testura = testurarekin ? AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd") : null;
            if (m != null && m.shader == partikula && m.GetColor("_BaseColor") == kolorea && Mathf.Approximately(m.GetFloat("_Blend"), nahasketa)
                && Mathf.Approximately(m.GetFloat("_Surface"), 1f) && m.GetTexture("_BaseMap") == testura)
            {
                return m;
            }

            aldaketak.Add($"Partikula-materiala {(m == null ? "berria" : "eguneratua")}: {bidea}");
            if (!aplikatu)
            {
                return m;
            }

            bool berria = m == null;
            m ??= new Material(partikula) { name = izena };
            m.shader = partikula;
            m.SetColor("_BaseColor", kolorea);
            m.SetTexture("_BaseMap", testura);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", nahasketa);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(gehigarria ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
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

        // ---------- EMP pistola ----------

        /// <summary>Pistola propioa primitiboekin (Z+ = kanoia, jatorria = heldulekua). 2 azpimalla: gorputza eta argi zianak.</summary>
        private static Mesh PistolaMalla(bool aplikatu, List<string> aldaketak)
        {
            Mesh malla = AssetDatabase.LoadAssetAtPath<Mesh>(PistolaMesh);
            if (malla != null && malla.subMeshCount == 2)
            {
                return malla;
            }

            aldaketak.Add($"Malla berria: {PistolaMesh} (EMP pistola, kitbash propioa)");
            if (!aplikatu)
            {
                return malla;
            }

            Mesh kuboa = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Mesh zilindroa = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            var gorputza = new List<CombineInstance>();
            var argia = new List<CombineInstance>();
            void Gehitu(List<CombineInstance> zerrenda, Mesh m, Vector3 p, Vector3 r, Vector3 s) =>
                zerrenda.Add(new CombineInstance { mesh = m, transform = Matrix4x4.TRS(p, Quaternion.Euler(r), s) });

            Gehitu(gorputza, kuboa, new Vector3(0f, 0.045f, 0.05f), Vector3.zero, new Vector3(0.036f, 0.05f, 0.2f));
            Gehitu(gorputza, kuboa, new Vector3(0f, -0.012f, -0.006f), new Vector3(15f, 0f, 0f), new Vector3(0.03f, 0.1f, 0.042f));
            Gehitu(gorputza, kuboa, new Vector3(0f, 0.002f, 0.04f), Vector3.zero, new Vector3(0.012f, 0.02f, 0.045f));
            Gehitu(gorputza, zilindroa, new Vector3(0f, 0.045f, 0.18f), new Vector3(90f, 0f, 0f), new Vector3(0.026f, 0.045f, 0.026f));
            Gehitu(gorputza, kuboa, new Vector3(0f, 0.075f, 0.02f), Vector3.zero, new Vector3(0.012f, 0.012f, 0.05f));
            Gehitu(argia, zilindroa, new Vector3(0f, 0.045f, 0.135f), new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.006f, 0.05f));
            Gehitu(argia, zilindroa, new Vector3(0f, 0.045f, 0.17f), new Vector3(90f, 0f, 0f), new Vector3(0.046f, 0.006f, 0.046f));
            Gehitu(argia, kuboa, new Vector3(0.0185f, 0.052f, 0.05f), Vector3.zero, new Vector3(0.002f, 0.008f, 0.14f));
            Gehitu(argia, kuboa, new Vector3(-0.0185f, 0.052f, 0.05f), Vector3.zero, new Vector3(0.002f, 0.008f, 0.14f));
            Gehitu(argia, kuboa, new Vector3(0f, 0.045f, 0.228f), Vector3.zero, new Vector3(0.018f, 0.018f, 0.006f));

            var a = new Mesh();
            a.CombineMeshes(gorputza.ToArray(), true, true);
            var b = new Mesh();
            b.CombineMeshes(argia.ToArray(), true, true);
            malla = new Mesh { name = "SM_EmpPistola" };
            malla.CombineMeshes(new[]
            {
                new CombineInstance { mesh = a, transform = Matrix4x4.identity },
                new CombineInstance { mesh = b, transform = Matrix4x4.identity },
            }, false, true);
            malla.RecalculateBounds();
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
            AssetDatabase.CreateAsset(malla, PistolaMesh);
            return malla;
        }

        internal static ParticleSystem Partikulak(string izena, Transform gurasoa, Material materiala, Color kolorea, Vector2 bizitza, Vector2 abiadura,
            Vector2 tamaina, float grabitatea, float angelua, bool luzatuta)
        {
            var o = new GameObject(izena);
            o.transform.SetParent(gurasoa, false);
            var ps = o.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(bizitza.x, bizitza.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(abiadura.x, abiadura.y);
            main.startSize = new ParticleSystem.MinMaxCurve(tamaina.x, tamaina.y);
            main.startColor = kolorea;
            main.gravityModifier = grabitatea;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            ParticleSystem.EmissionModule emisioa = ps.emission;
            emisioa.rateOverTime = 0f;
            ParticleSystem.ShapeModule forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = angelua;
            forma.radius = 0.01f;
            var r = o.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = materiala;
            r.renderMode = luzatuta ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            r.lengthScale = 3f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        private static void PistolaAurrefabrikatua(Mesh malla, Material gorputza, Material argia, Material izpia, Material txinpartak,
            Dictionary<string, AudioClip> soinuak, bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            var dagoena = AssetDatabase.LoadAssetAtPath<GameObject>(PistolaAurrefab);
            bool zuzena = dagoena != null && dagoena.GetComponent<EmpPistolaBisuala>() != null && dagoena.GetComponent<MeshFilter>()?.sharedMesh == malla
                          && dagoena.GetComponent<MeshRenderer>().sharedMaterials.SequenceEqual(new[] { gorputza, argia })
                          && dagoena.transform.Find("Mutura") != null;
            if (zuzena)
            {
                egiaztapenak.Add($"Pistola zuzena: {PistolaAurrefab}");
                return;
            }

            aldaketak.Add($"Aurrefabrikatua {(dagoena == null ? "berria" : "berreraikia")}: {PistolaAurrefab}");
            if (!aplikatu)
            {
                return;
            }

            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                var erroa = new GameObject("PF_EmpPistola");
                SceneManager.MoveGameObjectToScene(erroa, aurrebista);
                erroa.AddComponent<MeshFilter>().sharedMesh = malla;
                var errendatzailea = erroa.AddComponent<MeshRenderer>();
                errendatzailea.sharedMaterials = new[] { gorputza, argia };
                var mutura = new GameObject("Mutura").transform;
                mutura.SetParent(erroa.transform, false);
                mutura.localPosition = new Vector3(0f, 0.045f, 0.235f);

                var izpiObj = new GameObject("Izpia");
                izpiObj.transform.SetParent(erroa.transform, false);
                var lerroa = izpiObj.AddComponent<LineRenderer>();
                lerroa.sharedMaterial = izpia;
                lerroa.positionCount = 2;
                lerroa.useWorldSpace = true;
                lerroa.startWidth = 0.035f;
                lerroa.endWidth = 0.012f;
                lerroa.numCapVertices = 2;
                lerroa.shadowCastingMode = ShadowCastingMode.Off;
                lerroa.enabled = false;

                var distiraObj = new GameObject("Distira");
                distiraObj.transform.SetParent(mutura, false);
                var distira = distiraObj.AddComponent<Light>();
                distira.type = LightType.Point;
                distira.color = new Color(0.45f, 0.9f, 1f);
                distira.range = 5f;
                distira.intensity = 4f;
                distira.shadows = LightShadows.None;
                distira.enabled = false;

                ParticleSystem txinpartaPS = Partikulak("Txinpartak", erroa.transform, txinpartak, new Color(0.6f, 0.95f, 1f), new Vector2(0.15f, 0.35f),
                    new Vector2(1.5f, 5f), new Vector2(0.01f, 0.025f), 1f, 40f, true);
                var audioa = erroa.AddComponent<AudioSource>();
                audioa.playOnAwake = false;
                audioa.spatialBlend = 0.6f;
                audioa.volume = 0.8f;

                erroa.AddComponent<EmpPistolaBisuala>().Konfiguratu(mutura, lerroa, distira, txinpartaPS, audioa,
                    soinuak["SFX_Emp_Tiroa"], soinuak["SFX_Emp_Hutsik"], soinuak["SFX_Emp_Birkarga"], soinuak["SFX_Inpaktua"]);
                PrefabUtility.SaveAsPrefabAsset(erroa, PistolaAurrefab, out bool gordeta);
                if (!gordeta)
                {
                    throw new InvalidOperationException($"Ezin izan da gorde {PistolaAurrefab}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            egiaztapenak.Add($"Pistola gordeta: {PistolaAurrefab} ({malla.triangles.Length / 3} triangelu)");
        }

        // ---------- Behin-behineko etsaia ----------

        private static readonly (HumanBodyBones Hezurra, HumanBodyBones Umea, float Erradioa, KalteZona Zona)[] ZonaKapsulak =
        {
            (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 0.065f, KalteZona.BesoEzkerra),
            (HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 0.055f, KalteZona.BesoEzkerra),
            (HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 0.065f, KalteZona.BesoEskuina),
            (HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 0.055f, KalteZona.BesoEskuina),
            (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.09f, KalteZona.HankaEzkerra),
            (HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.07f, KalteZona.HankaEzkerra),
            (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.09f, KalteZona.HankaEskuina),
            (HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.07f, KalteZona.HankaEskuina),
        };

        private static void Kapsula(string izena, Transform gurasoa, Vector3 a, Vector3 b, float erradioa, KalteZona zona, Etsaia jabea)
        {
            var o = new GameObject(izena) { layer = 0 };
            o.transform.SetParent(gurasoa, true);
            o.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, (b - a).normalized));
            o.transform.localScale = Vector3.one;
            var k = o.AddComponent<CapsuleCollider>();
            float s = o.transform.lossyScale.y;
            k.direction = 1;
            k.radius = erradioa / s;
            k.height = (Vector3.Distance(a, b) + erradioa * 2f) / s;
            k.isTrigger = true;
            o.AddComponent<KalteZonaKolisioa>().Konfiguratu(zona, jabea);
        }

        private static void EtsaiAurrefabrikatua(AnimatorController ac, Material materiala, Material odola, Material txinpartak, bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            var dagoena = AssetDatabase.LoadAssetAtPath<GameObject>(EtsaiAurrefab);
            Animator dagoenAn = dagoena != null ? dagoena.GetComponentInChildren<Animator>() : null;
            var ezberdinak = new List<string>();
            if (dagoena == null)
            {
                ezberdinak.Add("ez dago");
            }
            else
            {
                if (dagoena.GetComponent<Etsaia>() == null) ezberdinak.Add("Etsaia");
                if (dagoena.GetComponent<NavMeshAgent>() == null) ezberdinak.Add("NavMeshAgent");
                if (dagoenAn == null || dagoenAn.runtimeAnimatorController != ac) ezberdinak.Add($"Animator ({(dagoenAn != null ? dagoenAn.runtimeAnimatorController?.name : "null")})");
                int zonak = dagoena.GetComponentsInChildren<KalteZonaKolisioa>(true).Length;
                if (zonak != ZonaKapsulak.Length + 2) ezberdinak.Add($"kalte-zonak {zonak}");
                if (dagoena.transform.Find("Txundidura") == null) ezberdinak.Add("Txundidura");
                if (dagoena.transform.Find(EtsaiBertsioa) == null) ezberdinak.Add(EtsaiBertsioa);
            }

            if (ezberdinak.Count == 0)
            {
                egiaztapenak.Add($"Etsaia zuzena: {EtsaiAurrefab} ({ZonaKapsulak.Length + 2} kalte-zona)");
                return;
            }

            aldaketak.Add($"Aurrefabrikatua {(dagoena == null ? "berria" : "berreraikia")}: {EtsaiAurrefab} ({string.Join(", ", ezberdinak)})");
            if (!aplikatu)
            {
                return;
            }

            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                var erroa = new GameObject("PF_Etsaia_Proba") { layer = 2 };
                SceneManager.MoveGameObjectToScene(erroa, aurrebista);
                NavMeshAgent agentea = erroa.AddComponent<NavMeshAgent>();
                agentea.radius = 0.35f;
                agentea.height = 1.8f;
                agentea.speed = 4.2f;
                agentea.angularSpeed = 540f;
                agentea.acceleration = 16f;
                agentea.stoppingDistance = 1.2f;
                var kapsula = erroa.AddComponent<CapsuleCollider>();
                kapsula.center = new Vector3(0f, 0.9f, 0f);
                kapsula.height = 1.8f;
                kapsula.radius = 0.3f;
                Etsaia etsaia = erroa.AddComponent<Etsaia>();

                var eredua = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(JokalariBisualTresna.Ual), aurrebista);
                eredua.name = "Eredua";
                eredua.transform.SetParent(erroa.transform, false);
                foreach (Transform t in eredua.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = 2;
                }

                Animator an = eredua.GetComponent<Animator>() ?? eredua.AddComponent<Animator>();
                an.avatar = AssetDatabase.LoadAllAssetsAtPath(JokalariBisualTresna.Ual).OfType<Avatar>().First();
                an.runtimeAnimatorController = ac;
                an.applyRootMotion = false;
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (SkinnedMeshRenderer r in eredua.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.sharedMaterials = Enumerable.Repeat(materiala, r.sharedMaterials.Length).ToArray();
                }

                foreach ((HumanBodyBones hezurra, HumanBodyBones umea, float erradioa, KalteZona zona) in ZonaKapsulak)
                {
                    Transform h = an.GetBoneTransform(hezurra) ?? throw new InvalidOperationException($"Hezurra falta da: {hezurra}");
                    Transform u = an.GetBoneTransform(umea) ?? throw new InvalidOperationException($"Hezurra falta da: {umea}");
                    Kapsula($"Zona_{hezurra}", h, h.position, u.position, erradioa, zona, etsaia);
                }

                Transform aldaka = an.GetBoneTransform(HumanBodyBones.Hips);
                Transform lepoa = an.GetBoneTransform(HumanBodyBones.Neck);
                Transform bizkarra = an.GetBoneTransform(HumanBodyBones.Spine);
                // Enborra lepoa baino 15 cm beherago amaitzen da: bestela kapsularen goiko muturrak burua estaltzen du atzetik (A3, 2. bertsioa).
                Vector3 enborGoia = lepoa.position - (lepoa.position - aldaka.position).normalized * 0.15f;
                Kapsula("Zona_Enborra", bizkarra, aldaka.position, enborGoia, 0.17f, KalteZona.Enborra, etsaia);
                new GameObject(EtsaiBertsioa).transform.SetParent(erroa.transform, false);
                Transform burua = an.GetBoneTransform(HumanBodyBones.Head);
                var buruObj = new GameObject("Zona_Burua") { layer = 0 };
                buruObj.transform.SetParent(burua, true);
                buruObj.transform.position = burua.position + Vector3.up * 0.09f;
                var esfera = buruObj.AddComponent<SphereCollider>();
                esfera.radius = 0.13f / buruObj.transform.lossyScale.y;
                esfera.isTrigger = true;
                buruObj.AddComponent<KalteZonaKolisioa>().Konfiguratu(KalteZona.Burua, etsaia);

                ParticleSystem odolPS = Partikulak("Odola", erroa.transform, odola, new Color(0.32f, 0.01f, 0.01f), new Vector2(0.5f, 0.9f),
                    new Vector2(1f, 3.5f), new Vector2(0.02f, 0.07f), 1.6f, 25f, false);
                // EMP txundidura: txinparta zianak gorputzean, etsaia txundituta dagoen bitartean (Etsaia-k pizten/itzaltzen du).
                ParticleSystem txundidura = Partikulak("Txundidura", erroa.transform, txinpartak, new Color(0.55f, 0.95f, 1f), new Vector2(0.08f, 0.2f),
                    new Vector2(0.5f, 2f), new Vector2(0.01f, 0.03f), 0f, 0f, true);
                txundidura.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                ParticleSystem.MainModule tm = txundidura.main;
                tm.loop = true;
                tm.playOnAwake = true;
                tm.simulationSpace = ParticleSystemSimulationSpace.Local;
                ParticleSystem.EmissionModule te = txundidura.emission;
                te.rateOverTime = 60f;
                te.enabled = false;
                ParticleSystem.ShapeModule tf = txundidura.shape;
                tf.shapeType = ParticleSystemShapeType.Sphere;
                tf.radius = 0.45f;
                etsaia.Konfiguratu(an, odolPS, txundidura);
                PrefabUtility.SaveAsPrefabAsset(erroa, EtsaiAurrefab, out bool gordeta);
                if (!gordeta)
                {
                    throw new InvalidOperationException($"Ezin izan da gorde {EtsaiAurrefab}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            egiaztapenak.Add($"Etsaia gordeta: {EtsaiAurrefab}");
        }

        // ---------- 04_BorrokaProba ----------

        private static void ProbaEszenaEgiaztatu(bool aplikatu, List<string> aldaketak, List<string> egiaztapenak)
        {
            if (File.Exists(ProbaEszena) && AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshBidea) != null)
            {
                egiaztapenak.Add($"Proba-eszena badago: {ProbaEszena}");
                return;
            }

            aldaketak.Add($"Proba-eszena berria: {ProbaEszena} (babesa, kantoia, 3 etsai-markatzaile, NavMesh kolisio fisikoetatik)");
            if (!aplikatu)
            {
                return;
            }

            string aurrekoa = SceneManager.GetActiveScene().path;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException("Proba-eszena ez da sortu: irekitako eszena gorde gabe dago.");
            }

            Scene eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string navMesh = EszenaEraiki();
            if (!EditorSceneManager.SaveScene(eszena, ProbaEszena))
            {
                throw new InvalidOperationException($"Ezin izan da gorde {ProbaEszena}");
            }

            egiaztapenak.Add(navMesh);
            if (!string.IsNullOrEmpty(aurrekoa))
            {
                EditorSceneManager.OpenScene(aurrekoa, OpenSceneMode.Single);
            }
        }

        private static string EszenaEraiki()
        {
            Material zorua = JokalariBisualTresna.ProbaMateriala("M_Proba_Zorua", Color.white, JokalariBisualTresna.Laukiak(), 20f);
            Material horma = JokalariBisualTresna.ProbaMateriala("M_Proba_Horma", new Color(0.55f, 0.57f, 0.6f));
            Material oztopoa = JokalariBisualTresna.ProbaMateriala("M_Proba_Oztopoa", new Color(0.95f, 0.45f, 0.1f));

            var erroa = new GameObject("BorrokaProba").transform;
            JokalariBisualTresna.ArgiNeutroak(erroa);
            var geometria = new GameObject("Geometria").transform;
            geometria.SetParent(erroa, false);
            JokalariBisualTresna.Kubo("Zorua", geometria, new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), zorua);
            JokalariBisualTresna.Kubo("Horma_Iparra", geometria, new Vector3(0f, 1.5f, 20.25f), new Vector3(41f, 3f, 0.5f), horma);
            JokalariBisualTresna.Kubo("Horma_Hegoa", geometria, new Vector3(0f, 1.5f, -20.25f), new Vector3(41f, 3f, 0.5f), horma);
            JokalariBisualTresna.Kubo("Horma_Ekialdea", geometria, new Vector3(20.25f, 1.5f, 0f), new Vector3(0.5f, 3f, 41f), horma);
            JokalariBisualTresna.Kubo("Horma_Mendebaldea", geometria, new Vector3(-20.25f, 1.5f, 0f), new Vector3(0.5f, 3f, 41f), horma);
            // Babesa: atzean etsai bat dago; tiroak ez du zeharkatu behar.
            JokalariBisualTresna.Kubo("Babesa", geometria, new Vector3(6f, 1.5f, 6f), new Vector3(5f, 3f, 0.4f), horma);
            // Kantoia (L): muturra hormaren ondoan dagoenean, tiroa hormak gelditu behar du.
            JokalariBisualTresna.Kubo("Kantoia_A", geometria, new Vector3(-9f, 1.5f, -4f), new Vector3(0.4f, 3f, 6f), horma);
            JokalariBisualTresna.Kubo("Kantoia_B", geometria, new Vector3(-11.8f, 1.5f, -1f), new Vector3(6f, 3f, 0.4f), horma);
            JokalariBisualTresna.Kubo("Kutxa_1", geometria, new Vector3(3f, 0.5f, -3f), Vector3.one, oztopoa);
            JokalariBisualTresna.Kubo("Kutxa_2", geometria, new Vector3(-3f, 0.5f, 1f), Vector3.one, oztopoa);

            var spawn = new GameObject("Spawn_Jokalaria").transform;
            spawn.SetParent(erroa, false);
            spawn.SetLocalPositionAndRotation(new Vector3(0f, 0f, -12f), Quaternion.identity);

            // Etsaiak runtime-n: misioetan bezala, MailaMarka (Etsaia) markatzaileak + EtsaiSortzailea.
            var etsaiak = new GameObject("Etsaiak").transform;
            etsaiak.SetParent(erroa, false);
            etsaiak.gameObject.AddComponent<EtsaiSortzailea>().Konfiguratu("Borroka/PF_Etsaia_Proba", true);
            foreach ((string izena, Vector3 lekua, float yaw) in new[]
                     {
                         ("Din_Marka_Etsaia_Irekia", new Vector3(0f, 0f, 6f), 180f),
                         ("Din_Marka_Etsaia_Babesa", new Vector3(6f, 0f, 9f), 180f),
                         ("Din_Marka_Etsaia_Urrun", new Vector3(-12f, 0f, 14f), 135f),
                     })
            {
                var marka = new GameObject(izena).transform;
                marka.SetParent(etsaiak, false);
                marka.SetLocalPositionAndRotation(lekua, Quaternion.Euler(0f, yaw, 0f));
                marka.gameObject.AddComponent<MailaMarka>().Konfiguratu(MarkaMota.Etsaia);
            }

            var kamera = new GameObject("Main Camera") { tag = "MainCamera" };
            kamera.transform.SetParent(erroa, false);
            kamera.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.65f, -12f), Quaternion.identity);
            Camera c = kamera.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
            kamera.AddComponent<AudioListener>();

            var abiarazlea = new GameObject("Abiarazlea").AddComponent<BorrokaProbaAbiarazlea>();
            abiarazlea.transform.SetParent(erroa, false);
            var serializatua = new SerializedObject(abiarazlea);
            serializatua.FindProperty("_spawn").objectReferenceValue = spawn;
            serializatua.ApplyModifiedPropertiesWithoutUndo();

            // NavMesh-a kolisio fisikoetatik (Ignore Raycast geruza kanpo), aktibo gisa gordeta.
            var nabigazioa = new GameObject("Nabigazioa");
            nabigazioa.transform.SetParent(erroa, false);
            var gainazala = nabigazioa.AddComponent<NavMeshSurface>();
            gainazala.collectObjects = CollectObjects.All;
            gainazala.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            gainazala.layerMask = ~(1 << 2);
            gainazala.BuildNavMesh();
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshBidea) != null)
            {
                AssetDatabase.DeleteAsset(NavMeshBidea);
            }

            AssetDatabase.CreateAsset(gainazala.navMeshData, NavMeshBidea);
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            return $"NavMesh: {tri.indices.Length / 3} triangelu, {NavMeshBidea}";
        }
    }
}
