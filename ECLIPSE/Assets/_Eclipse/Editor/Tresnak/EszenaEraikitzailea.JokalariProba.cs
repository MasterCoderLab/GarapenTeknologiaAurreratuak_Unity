using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Eclipse.Hud;
using Eclipse.Jokalaria;
using Eclipse.Misioak;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A2: HypJo protagonistaren proba Play moduan, batchmode-n eta teklatu birtualarekin. Eszenarik EZ du gordetzen.
    /// -eclipseMaila 0: 03_JokalariProba (ingurune neutroa): animazio-egoerak, abiadurak, orientazioa, oinak, burua, hodia, koska, kamera.
    /// -eclipseMaila 1|2|3: misioak, LevelManager-en fluxuarekin: HypJo kargatuta, HUD-a, kamera eta animazio nagusiak.
    /// Batch-ean kurtsorea ezin da blokeatu: V tekla eta sagua Ion-en proba dira. Mugitu/LateUpdate/KameraModuaEzarri islapenez deitzen dira.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string JpMarka = "ECL_JOKALARIPROBA";
        private const double JpDenbora = 420.0;

        private static string _jpEszena;
        private static string _jpAurrizkia;
        private static bool _jpNeutroa;
        private static bool _jpKapsula;
        private static string _jpGuid;

        /// <summary>Ordezkoaren proba: aurrefabrikatua Resources-etik kanpora eraman (GUID bera), jolastu eta itzuli.</summary>
        private const string KapsulaProbaBidea = "Assets/_Eclipse/Datuak/Jokalaria/PF_Jokalaria_HypJo_KAPSULAPROBA.prefab";
        private static double _jpHasiera;
        private static readonly List<string> JpErroreak = new List<string>();

        /// <summary>Batchmode: -eclipseMaila 0|1|2|3 (0 = 03_JokalariProba).</summary>
        public static void BatchJokalariProba()
        {
            try
            {
                string[] argumentuak = Environment.GetCommandLineArgs();
                int indizea = Array.IndexOf(argumentuak, "-eclipseMaila");
                _jpNeutroa = indizea < 0 || indizea + 1 >= argumentuak.Length || argumentuak[indizea + 1] == "0";
                _jpKapsula = argumentuak.Contains("-eclipseKapsula");
                if (_jpNeutroa)
                {
                    _jpEszena = JokalariBisualTresna.ProbaEszena;
                    _jpAurrizkia = "neutro";
                }
                else
                {
                    MailaDef d = BatchMailaLortu();
                    _jpEszena = d.Bidea;
                    _jpAurrizkia = $"ion_{d.Aurrizkia}";
                }

                if (_jpKapsula)
                {
                    _jpAurrizkia += "_kapsula";
                    _jpGuid = AssetDatabase.AssetPathToGUID(JokalariBisualTresna.AurrefabBidea);
                    string errorea = AssetDatabase.MoveAsset(JokalariBisualTresna.AurrefabBidea, KapsulaProbaBidea);
                    if (!string.IsNullOrEmpty(errorea))
                    {
                        throw new InvalidOperationException($"Ezin izan da aurrefabrikatua aldi baterako mugitu: {errorea}");
                    }
                }

                EditorSceneManager.OpenScene(_jpEszena, OpenSceneMode.Single);
                JpErroreak.Clear();
                Application.logMessageReceived += JpLog;
                _jpHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += JpEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{JpMarka}_ERROR {salbuespena}");
                KapsulaLeheneratu(new List<string>());
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Kapsula-proban mugitutako aurrefabrikatua bere lekura itzuli eta GUID-a egiaztatu.</summary>
        private static void KapsulaLeheneratu(List<string> lerroak)
        {
            if (!_jpKapsula || !File.Exists(KapsulaProbaBidea))
            {
                return;
            }

            string errorea = AssetDatabase.MoveAsset(KapsulaProbaBidea, JokalariBisualTresna.AurrefabBidea);
            bool ondo = string.IsNullOrEmpty(errorea) && AssetDatabase.AssetPathToGUID(JokalariBisualTresna.AurrefabBidea) == _jpGuid;
            lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: aurrefabrikatua Resources-era itzulita, GUID bera {errorea}");
            if (!ondo)
            {
                Debug.LogError($"{JpMarka}_ERROR Aurrefabrikatua ez da itzuli: {KapsulaProbaBidea} → {JokalariBisualTresna.AurrefabBidea}: {errorea}");
            }
        }

        /// <summary>
        /// A2: BatchReleaseProba-ren proba bera (QA + Release, Development gabe, M1→M2→M3), eta amaieran activeBuildTarget
        /// hasierakora itzultzen da BETI (finally), proba huts eginda ere. EditorBuildSettings eta Build Profile-ak ez dira ukitzen.
        /// </summary>
        public static void BatchA2ReleaseProba()
        {
            BuildTarget hasierakoa = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup taldea = BuildPipeline.GetBuildTargetGroup(hasierakoa);
            var lerroak = new List<string>();
            bool ondo = false;
            try
            {
                ondo = ReleaseProbaExekutatu(lerroak);
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ReleaseMarka}_ERROR {salbuespena}");
                lerroak.Add($"HUTS: salbuespena: {salbuespena.Message}");
            }
            finally
            {
                if (EditorUserBuildSettings.activeBuildTarget != hasierakoa)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(taldea, hasierakoa);
                }

                bool berdina = EditorUserBuildSettings.activeBuildTarget == hasierakoa;
                lerroak.Add($"{(berdina ? "ONDO" : "HUTS")}: activeBuildTarget leheneratuta: {EditorUserBuildSettings.activeBuildTarget} (hasieran {hasierakoa})");
            }

            var informea = new Informea(Tresna, ReleaseMarka, "A2: Release eta QA build-ak (Development gabe) + target-a leheneratuta", "M1, M2, M3", "StandaloneLinux64");
            informea.Atala("Emaitzak", lerroak);
            int hutsak = lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal));
            if (ondo && hutsak == 0)
            {
                informea.AmaituOndo($"{lerroak.Count} lerro, 0 huts");
            }
            else
            {
                informea.AmaituErrorearekin($"{hutsak} huts");
            }

            EditorApplication.Exit(ondo && hutsak == 0 ? 0 : 1);
        }

        private static void JpLog(string mezua, string pila, LogType mota)
        {
            if ((mota == LogType.Error || mota == LogType.Exception || mota == LogType.Assert) && !mezua.StartsWith(JpMarka, StringComparison.Ordinal) && JpErroreak.Count < 20)
            {
                JpErroreak.Add($"{mota}: {mezua.Split('\n')[0]}");
            }
        }

        private static void JpEguneratu()
        {
            JokalariProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<JokalariProbaKontrola>();
            if (EditorApplication.timeSinceStartup - _jpHasiera > JpDenbora)
            {
                var lerroak = kontrola != null ? kontrola.Lerroak : new List<string>();
                lerroak.Add("HUTS: denbora agortuta");
                JpAmaitu(lerroak);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (kontrola == null)
            {
                CharacterController cc = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
                if (cc != null && cc.GetComponent<EsploratzaileKontrola>() != null)
                {
                    kontrola = new GameObject("JokalariProbaKontrola").AddComponent<JokalariProbaKontrola>();
                    kontrola.Hasi(_jpNeutroa, _jpAurrizkia, _jpKapsula);
                }

                return;
            }

            if (kontrola.Amaituta)
            {
                JpAmaitu(kontrola.Lerroak);
            }
        }

        private static void JpAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= JpEguneratu;
            Application.logMessageReceived -= JpLog;
            EditorApplication.isPlaying = false;
            KapsulaLeheneratu(lerroak);
            lerroak.Add(JpErroreak.Count == 0
                ? "ONDO: Play-n 0 errore/salbuespen log-ean"
                : $"HUTS: Play-n {JpErroreak.Count} errore: {string.Join(" | ", JpErroreak)}");

            var informea = new Informea("JokalariProba", JpMarka, $"HypJo protagonista Play-n ({_jpAurrizkia})", _jpEszena, "Jokalaria");
            informea.Atala("Emaitzak", lerroak);
            int hutsak = lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal));
            if (hutsak == 0)
            {
                informea.AmaituOndo($"{lerroak.Count(l => l.StartsWith("ONDO", StringComparison.Ordinal))} egiaztapen, 0 huts");
            }
            else
            {
                informea.AmaituErrorearekin($"{hutsak} huts");
            }

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(hutsak == 0 ? 0 : 1);
        }

        private sealed class JokalariProbaKontrola : MonoBehaviour
        {
            private const BindingFlags Pribatua = BindingFlags.Instance | BindingFlags.NonPublic;
            private static readonly MethodInfo MugituM = typeof(EsploratzaileKontrola).GetMethod("Mugitu", Pribatua);
            private static readonly MethodInfo KameraM = typeof(EsploratzaileKontrola).GetMethod("LateUpdate", Pribatua);
            private static readonly MethodInfo ModuaM = typeof(EsploratzaileKontrola).GetMethod("KameraModuaEzarri", Pribatua);

            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;

            private bool _neutroa;
            private bool _kapsula;
            private string _aurrizkia;
            private Keyboard _teklatua;
            private CharacterController _cc;
            private EsploratzaileKontrola _kontrola;
            private JokalariBisuala _bisuala;
            private Animator _an;
            private Transform _burua;
            private Camera _kamera;
            private Camera _alboKamera;
            private string _kapturak;
            private string _sekuentziak;

            /// <summary>Fase bateko laginak (fasearen azken %40-an).</summary>
            private sealed class Laginak
            {
                public readonly HashSet<string> Egoerak = new HashSet<string>();
                public readonly List<float> Abiadurak = new List<float>();
                public readonly List<float> YawDesbideratzeak = new List<float>();
                public float OinBehe = float.MaxValue;
                public float OinGoi = float.MinValue;
                public float BuruGoi = float.MinValue;
                public float YGoi = float.MinValue;
                public int AireFotogramak;
                public float AbiaduraBb => Abiadurak.Count > 0 ? Abiadurak.Average() : 0f;
                /// <summary>Batez besteko zirkularra: ±180 inguruko balioak ez dira 0 bihurtzen.</summary>
                public float YawBb => YawDesbideratzeak.Count > 0
                    ? Mathf.Atan2(YawDesbideratzeak.Sum(a => Mathf.Sin(a * Mathf.Deg2Rad)), YawDesbideratzeak.Sum(a => Mathf.Cos(a * Mathf.Deg2Rad))) * Mathf.Rad2Deg
                    : 0f;
            }

            public void Hasi(bool neutroa, string aurrizkia, bool kapsula)
            {
                _neutroa = neutroa;
                _kapsula = kapsula;
                _aurrizkia = aurrizkia;
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Exekutatu());
            }

            private void Oharra(string izena, bool ondo, string xehetasuna = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena} {xehetasuna}");

            private void Datua(string izena, string balioa) => Lerroak.Add($"DATUA: {izena} {balioa}");

            private void Teklak(params Key[] teklak) => InputSystem.QueueStateEvent(_teklatua, new KeyboardState(teklak));

            private IEnumerator Exekutatu()
            {
                InputSettings ezarpenak = ScriptableObject.CreateInstance<InputSettings>();
                ezarpenak.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                ezarpenak.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings = ezarpenak;
                _teklatua = InputSystem.AddDevice<Keyboard>("JokalariProbaTeklatua");
                _teklatua.MakeCurrent();
                yield return null;
                yield return null;

                _cc = FindAnyObjectByType<CharacterController>();
                _kontrola = _cc.GetComponent<EsploratzaileKontrola>();
                _bisuala = _kontrola.Bisuala;
                _burua = _cc.transform.Find("Burua");
                _kamera = Camera.main;
                string proiektua = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _kapturak = QaKapturak.Azpikarpeta("A2_Jokalaria");
                _sekuentziak = Path.Combine(QaKapturak.Karpeta, "sekuentziak", _aurrizkia);
                Directory.CreateDirectory(_kapturak);

                if (_kapsula)
                {
                    yield return KapsulaProba();
                    Amaituta = true;
                    yield break;
                }

                EgiaztapenFinkoak();
                if (_bisuala == null)
                {
                    Amaituta = true;
                    yield break;
                }

                _an = _bisuala.Animatzailea;
                var alboa = new GameObject("JokalariProba_AlboKamera");
                _alboKamera = alboa.AddComponent<Camera>();
                _alboKamera.enabled = false;
                _alboKamera.fieldOfView = 40f;
                _alboKamera.nearClipPlane = 0.05f;

                // Lehen pertsona (hasierakoa).
                _kontrola.enabled = false;
                yield return Ibili(0.5f, null);
                Oharra("Lehen pertsona: eredua itzalak bakarrik", _bisuala.GetComponentsInChildren<Renderer>().All(r => r.shadowCastingMode == ShadowCastingMode.ShadowsOnly));
                Oharra("Lehen pertsona: kamera buruan", Vector3.Distance(_kamera.transform.position, _burua.position) < 0.01f);
                yield return Argazkia("01_lehen_pertsona", _kamera);

                // Hirugarren pertsona (V-ren baliokidea: batch-ean kurtsorea ezin da blokeatu).
                ModuaM.Invoke(_kontrola, new object[] { true });
                yield return Ibili(0.5f, null);
                Oharra("Hirugarren pertsona: eredua ikusgai", _bisuala.Ikusgai && _bisuala.GetComponentsInChildren<Renderer>().All(r => r.shadowCastingMode == ShadowCastingMode.On));
                Vector3 lokala = _burua.InverseTransformPoint(_kamera.transform.position);
                Datua("Kamera buruaren espazioan", $"{lokala.x:0.00}, {lokala.y:0.00}, {lokala.z:0.00} m");
                if (_neutroa)
                {
                    Oharra("Sorbaldako kamera leku librean (0,45; 0; -2,2)", Vector3.Distance(lokala, new Vector3(0.45f, 0f, -2.2f)) < 0.03f);
                    yield return Neutroa();
                }
                else
                {
                    yield return Misioa();
                }

                ModuaM.Invoke(_kontrola, new object[] { false });
                yield return Ibili(0.3f, null);
                Oharra("V-ren baliokidea: lehen pertsonara itzuli", !_bisuala.Ikusgai && Vector3.Distance(_kamera.transform.position, _burua.position) < 0.01f);
                Lerroak.Add("OHARRA: V tekla, Esc eta saguaren begirada Cursor.lockState-ren menpe daude; batch-ean ezin dira probatu: Ion-en proba.");
                Amaituta = true;
            }

            /// <summary>HypJo kargatu ezin denean: kapsula zaharra, mugimendua eta kamera-moduak lehen bezala.</summary>
            private IEnumerator KapsulaProba()
            {
                Transform kapsula = _cc.transform.Find("Gorputza");
                Renderer r = kapsula != null ? kapsula.GetComponent<Renderer>() : null;
                Oharra("Ordezkoa: HypJo ez dago, kapsula dago", _bisuala == null && r != null);
                Oharra("Jokalari bakarra", FindObjectsByType<CharacterController>(FindObjectsInactive.Include).Length == 1);
                if (r == null)
                {
                    yield break;
                }

                _kontrola.enabled = false;
                Oharra("Kapsula: lehen pertsonan ezkutuan", !r.enabled);
                ModuaM.Invoke(_kontrola, new object[] { true });
                yield return Ibili(0.3f, null);
                Oharra("Kapsula: hirugarren pertsonan ikusgai", r.enabled);
                yield return Kokatu(new Vector3(0f, 0f, -12f), 0f);
                Vector3 a = _cc.transform.position;
                yield return Ibili(1f, null, Key.W);
                Oharra("Kapsula: W-k mugitzen du", Vector3.Distance(a, _cc.transform.position) > 2f, $"{Vector3.Distance(a, _cc.transform.position):0.00} m");
                yield return Ibili(0.6f, null, Key.LeftCtrl);
                Oharra("Kapsula: makurtzean txikitzen da", Mathf.Abs(kapsula.localScale.y - 0.55f) < 0.01f, $"eskala y {kapsula.localScale.y:0.00}");
                yield return Ibili(0.8f, null);
                ModuaM.Invoke(_kontrola, new object[] { false });
                yield return Ibili(0.3f, null);
                Oharra("Kapsula: lehen pertsonara itzuli", !r.enabled);
                yield return Argazkia("kapsula_ordezkoa", _kamera);
            }

            private void EgiaztapenFinkoak()
            {
                Oharra("Jokalari bakarra (CharacterController 1, EsploratzaileKontrola 1)",
                    FindObjectsByType<CharacterController>(FindObjectsInactive.Include).Length == 1 && FindObjectsByType<EsploratzaileKontrola>(FindObjectsInactive.Include).Length == 1);
                Oharra("HypJo kargatuta (ez kapsula)", _bisuala != null && _cc.transform.Find("Gorputza") == null, _bisuala != null ? _bisuala.name : "null");
                if (_bisuala == null)
                {
                    return;
                }

                Oharra("Bisualak ez du kolisiorik", _bisuala.GetComponentsInChildren<Collider>(true).Length == 0);
                Oharra("Geruza: Ignore Raycast (2) dena", _bisuala.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == 2));
                Renderer[] rr = _bisuala.GetComponentsInChildren<Renderer>(true);
                string[] okerrak = rr.SelectMany(r => r.sharedMaterials.Select(m => m == null ? $"{r.name}: null"
                    : !m.shader.isSupported || m.shader.name != "Universal Render Pipeline/Lit" ? $"{r.name}: {m.shader.name}" : null)).Where(s => s != null).ToArray();
                Oharra("Materialak: URP/Lit, null-ik eta magentarik ez", okerrak.Length == 0, $"{rr.Length} errendatzaile; {string.Join(", ", okerrak)}");
                Material gorputza = rr.SelectMany(r => r.sharedMaterials).FirstOrDefault(m => m != null && m.name == "M_HypJo_Gorputza");
                Oharra("Emisioa aktibo (gorputza)", gorputza != null && gorputza.IsKeywordEnabled("_EMISSION"));
                Animator an = _bisuala.Animatzailea;
                Oharra("Animator: AC_Jokalaria, Avatar humanoidea, root motion gabe",
                    an != null && an.runtimeAnimatorController != null && an.runtimeAnimatorController.name == "AC_Jokalaria" && an.avatar != null && an.avatar.isValid && an.avatar.isHuman && !an.applyRootMotion);
                Transform eskua = an != null ? an.GetBoneTransform(HumanBodyBones.RightHand) : null;
                Oharra("Arma_Eskuina eskuineko eskuan (A3-rako)", eskua != null && eskua.Find("Arma_Eskuina") != null);
                Oharra("Kontrolatzailea: Ignore Raycast geruza", _cc.gameObject.layer == 2);
            }

            // ---------- Laguntzaileak ----------

            /// <summary>Osagaia geldituta: Mugitu teklatu birtualarekin eta kameraren LateUpdate, fotograma bakoitzean behin.</summary>
            private IEnumerator Ibili(float segundoak, Laginak laginak, params Key[] teklak)
            {
                Teklak(teklak);
                yield return null;
                float hasiera = Time.realtimeSinceStartup;
                float amaiera = hasiera + segundoak;
                float laginHasiera = hasiera + segundoak * 0.6f;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    MuguFotograma();
                    if (laginak != null)
                    {
                        Lagindu(laginak, Time.realtimeSinceStartup >= laginHasiera);
                    }

                    yield return null;
                }

                Teklak();
                yield return null;
            }

            private void MuguFotograma()
            {
                MugituM.Invoke(_kontrola, new object[] { _teklatua });
                KameraM.Invoke(_kontrola, null);
            }

            private float Lurra()
            {
                Vector3 p = _cc.transform.position;
                return Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out RaycastHit jo, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? jo.point.y : p.y;
            }

            private (float Behe, float Goi) Malla()
            {
                float behe = float.MaxValue;
                float goi = float.MinValue;
                var malla = new Mesh();
                foreach (SkinnedMeshRenderer s in _bisuala.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    s.BakeMesh(malla, true);
                    // P3-n balioztatutako bidea: BakeMesh(useScale) + localToWorldMatrix (Mixamo-ren fileScale 0,01 kontuan hartzen du).
                    Matrix4x4 m = s.transform.localToWorldMatrix;
                    foreach (Vector3 v in malla.vertices)
                    {
                        float y = m.MultiplyPoint3x4(v).y;
                        behe = Mathf.Min(behe, y);
                        goi = Mathf.Max(goi, y);
                    }
                }

                Destroy(malla);
                return (behe, goi);
            }

            private string Egoera()
            {
                AnimatorStateInfo e = _an.GetCurrentAnimatorStateInfo(0);
                foreach (string izena in new[] { "Zutik", "Makurtuta", "Airean" })
                {
                    if (e.IsName(izena))
                    {
                        return izena;
                    }
                }

                return "?";
            }

            private int _laginKontagailua;

            private void Lagindu(Laginak l, bool egonkorra)
            {
                string egoera = Egoera();
                if (egoera == "Airean")
                {
                    l.AireFotogramak++;
                }

                l.YGoi = Mathf.Max(l.YGoi, _cc.transform.position.y);
                if (!egonkorra)
                {
                    return;
                }

                l.Egoerak.Add(egoera);
                Vector3 v = _cc.velocity;
                l.Abiadurak.Add(new Vector2(v.x, v.z).magnitude);
                l.YawDesbideratzeak.Add(Mathf.DeltaAngle(_cc.transform.eulerAngles.y, _bisuala.transform.eulerAngles.y));
                if (++_laginKontagailua % 4 == 0)
                {
                    (float behe, float goi) = Malla();
                    float lurra = Lurra();
                    l.OinBehe = Mathf.Min(l.OinBehe, behe - lurra);
                    l.OinGoi = Mathf.Max(l.OinGoi, behe - lurra);
                    l.BuruGoi = Mathf.Max(l.BuruGoi, goi - lurra);
                }
            }

            private IEnumerator Kokatu(Vector3 oinak, float yaw)
            {
                _cc.enabled = false;
                _cc.transform.SetPositionAndRotation(oinak, Quaternion.Euler(0f, yaw, 0f));
                _cc.enabled = true;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                // Eredua kontrolatzailearen norabidean hasi.
                yield return Ibili(0.6f, null);
            }

            private IEnumerator Argazkia(string izena, Camera kamera, string karpeta = null, int zabalera = 1280, int altuera = 720)
            {
                if (kamera == _alboKamera)
                {
                    Vector3 erdia = _cc.transform.position + Vector3.up * 0.9f;
                    _alboKamera.transform.position = erdia + _cc.transform.right * 4.2f + Vector3.up * 0.2f;
                    _alboKamera.transform.LookAt(erdia);
                }

                yield return null;
                var rt = new RenderTexture(zabalera, altuera, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderTexture aurrekoa = kamera.targetTexture;
                kamera.targetTexture = rt;
                kamera.Render();
                RenderTexture.active = rt;
                var irudia = new Texture2D(zabalera, altuera, TextureFormat.RGB24, false);
                irudia.ReadPixels(new Rect(0, 0, zabalera, altuera), 0, 0);
                irudia.Apply();
                RenderTexture.active = null;
                kamera.targetTexture = aurrekoa;
                string helburua = karpeta ?? _kapturak;
                Directory.CreateDirectory(helburua);
                QaKapturak.Gorde(irudia.EncodeToPNG(), helburua, karpeta == null ? $"{_aurrizkia}_{izena}.png" : $"{izena}.png");
                Destroy(irudia);
                rt.Release();
                Destroy(rt);
            }

            /// <summary>Alboko kamerarekin fotograma-sekuentzia (bideorako), mugimendua etengabe mantenduz.</summary>
            private IEnumerator Sekuentzia(string izena, float segundoak, params Key[] teklak)
            {
                string karpeta = Path.Combine(_sekuentziak, izena);
                Teklak(teklak);
                int n = 0;
                float hurrengoa = 0f;
                float amaiera = Time.realtimeSinceStartup + segundoak;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    MuguFotograma();
                    if (Time.realtimeSinceStartup >= hurrengoa)
                    {
                        hurrengoa = Time.realtimeSinceStartup + 0.06f;
                        yield return Argazkia($"{n++:000}", _alboKamera, karpeta, 640, 360);
                    }
                    else
                    {
                        yield return null;
                    }
                }

                Teklak();
                yield return null;
            }

            private string Laburpena(Laginak l) =>
                $"egoerak [{string.Join(",", l.Egoerak)}], abiadura {l.AbiaduraBb:0.00} m/s, ereduaren yaw-a {l.YawBb:0} °, oin-zola {l.OinBehe:+0.000;-0.000} / {l.OinGoi:+0.000;-0.000} m, burua {l.BuruGoi:0.00} m, aire-fotogramak {l.AireFotogramak}";

            // ---------- 03_JokalariProba ----------

            private IEnumerator Neutroa()
            {
                var hasiera = new Vector3(0f, 0f, -12f);

                yield return Kokatu(hasiera, 0f);
                var geldirik = new Laginak();
                yield return Ibili(1.5f, geldirik);
                Datua("Geldirik:", Laburpena(geldirik));
                Oharra("Geldirik: Zutik egoera, abiadura 0", geldirik.Egoerak.SetEquals(new[] { "Zutik" }) && geldirik.AbiaduraBb < 0.05f);
                Oharra("Geldirik: oinak lurrean (Foot IK), ±3 cm", geldirik.OinBehe > -0.03f && geldirik.OinBehe < 0.03f, $"{geldirik.OinBehe:+0.000;-0.000} m");
                Oharra("Geldirik: altuera 1,70–1,86 m (kapsula 1,80)", geldirik.BuruGoi > 1.70f && geldirik.BuruGoi < 1.86f, $"{geldirik.BuruGoi:0.000} m");
                yield return Argazkia("02_geldirik_3p", _kamera);
                yield return Argazkia("02b_geldirik_alboa", _alboKamera);

                yield return Fasea("Ibili (W)", hasiera, 0f, 2f, "Zutik", 3f, 0f, "03_ibili", Key.W);
                yield return Fasea("Korrika (Shift+W)", hasiera, 0f, 2f, "Zutik", 6f, 0f, "04_korrika", Key.W, Key.LeftShift);
                float yaw0 = _cc.transform.eulerAngles.y;
                yield return Fasea("Atzera (S)", hasiera, 0f, 1.5f, "Zutik", 3f, 180f, "05_atzera", Key.S);
                Oharra("Atzera: kontrolatzailearen biraketa ez da aldatu", Mathf.Abs(Mathf.DeltaAngle(yaw0, _cc.transform.eulerAngles.y)) < 0.01f);
                yield return Fasea("Ezkerrera (A)", hasiera, 0f, 1.5f, "Zutik", 3f, -90f, null, Key.A);
                yield return Fasea("Eskuinera (D)", hasiera, 0f, 1.5f, "Zutik", 3f, 90f, "06_eskuinera", Key.D);

                yield return Kokatu(hasiera, 0f);
                var makurtu = new Laginak();
                yield return Ibili(1.5f, makurtu, Key.LeftCtrl);
                Datua("Makurtuta:", Laburpena(makurtu));
                Oharra("Ctrl: Makurtuta egoera eta kapsula 1,10 m", makurtu.Egoerak.SetEquals(new[] { "Makurtuta" }) && Mathf.Abs(_cc.height - 1.1f) < 0.01f);
                Oharra("Makurtuta: oinak lurrean, ±3 cm", makurtu.OinBehe > -0.03f && makurtu.OinBehe < 0.03f, $"{makurtu.OinBehe:+0.000;-0.000} m");
                Oharra("Makurtuta: burua 1,40 m-tik behera (hodia 1,425)", makurtu.BuruGoi < 1.40f, $"{makurtu.BuruGoi:0.000} m");
                Teklak(Key.LeftCtrl);
                yield return Argazkia("07_makurtuta_3p", _kamera);
                yield return Argazkia("07b_makurtuta_alboa", _alboKamera);

                yield return Fasea("Makurtuta ibili (Ctrl+W)", hasiera, 0f, 2f, "Makurtuta", 1.5f, 0f, "08_makur_ibili", Key.LeftCtrl, Key.W);

                var zutitu = new Laginak();
                yield return Ibili(1f, zutitu);
                Oharra("Ctrl askatuta: Zutik itzuli", zutitu.Egoerak.SetEquals(new[] { "Zutik" }), string.Join(",", zutitu.Egoerak));

                // Jauzia.
                yield return Kokatu(hasiera, 0f);
                float y0 = _cc.transform.position.y;
                var jauzia = new Laginak();
                Teklak(Key.Space);
                yield return null;
                bool aireanIkusia = false;
                float gailurra = y0;
                float amaiera = Time.realtimeSinceStartup + 0.35f;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    MuguFotograma();
                    Teklak();
                    gailurra = Mathf.Max(gailurra, _cc.transform.position.y);
                    aireanIkusia |= Egoera() == "Airean";
                    yield return null;
                }

                yield return Argazkia("09_jauzia_alboa", _alboKamera);
                yield return Ibili(1.2f, jauzia);
                gailurra = Mathf.Max(gailurra, jauzia.YGoi);
                aireanIkusia |= jauzia.AireFotogramak > 0;
                Oharra("Zuriunea: Airean egoera jauzian", aireanIkusia, $"gailurra {gailurra - y0:0.00} m");
                Oharra("Jauzia: lurreratu eta Zutik", Egoera() == "Zutik" && _cc.isGrounded);

                // Pasilloa eta hodia (1,425 m libre): zutik gelditzen da, makurtuta pasatzen.
                var pasilloa = new Vector3(10.3f, 0f, -4f);
                yield return Kokatu(pasilloa, 0f);
                yield return Ibili(2.5f, null, Key.W);
                float zZutik = _cc.transform.position.z;
                Oharra("Hodia: zutik ez da pasatzen", zZutik < 0f, $"z {zZutik:0.00}");
                yield return Kokatu(pasilloa, 0f);
                var azpia = new Laginak();
                float buruaAzpian = float.MinValue;
                int kameraOker = 0;
                Teklak(Key.LeftCtrl, Key.W);
                yield return null;
                amaiera = Time.realtimeSinceStartup + 5f;
                bool argazkiaAzpian = false;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    MuguFotograma();
                    Lagindu(azpia, true);
                    float z = _cc.transform.position.z;
                    if (Mathf.Abs(z) < 0.45f)
                    {
                        buruaAzpian = Mathf.Max(buruaAzpian, Malla().Goi);
                        if (!argazkiaAzpian && Mathf.Abs(z) < 0.1f)
                        {
                            argazkiaAzpian = true;
                            yield return Argazkia("10_hodia_azpitik_3p", _kamera);
                        }
                    }

                    Vector3 k = _kamera.transform.position;
                    if (Physics.CheckSphere(k, 0.04f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                        || Physics.Linecast(_burua.position, k, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        kameraOker++;
                    }

                    yield return null;
                }

                Teklak();
                Oharra("Hodia: makurtuta pasatzen da", _cc.transform.position.z > 0.5f, $"z {_cc.transform.position.z:0.00}");
                Oharra("Hodiaren azpian buruak ez du hodia zeharkatzen", buruaAzpian > 0f && buruaAzpian < 1.425f, $"burua {buruaAzpian:0.000} m < 1,425");
                Oharra("Pasilloan kamera ez da hormetan sartzen (2,4 m zabal)", kameraOker == 0, $"{kameraOker} fotograma oker");

                // Koska (0,3 m): igo eta jaitsi.
                yield return Kokatu(new Vector3(-8f, 0f, -8f), 0f);
                var koska = new Laginak();
                yield return Ibili(2.5f, koska, Key.W);
                Oharra("Koska 0,3 m: igotzen da", koska.YGoi > 0.25f, $"y gehienez {koska.YGoi:0.00}");
                Datua("Koska: aire-fotogramak (0 = trantsiziorik ez)", koska.AireFotogramak.ToString());

                // Bideorako sekuentziak (alboko kamera; eredua kontrolatzailearen norabidean).
                yield return Kokatu(hasiera, 0f);
                yield return Sekuentzia("ibili", 1.6f, Key.W);
                yield return Kokatu(hasiera, 0f);
                yield return Sekuentzia("korrika", 1.2f, Key.W, Key.LeftShift);
                yield return Kokatu(hasiera, 0f);
                yield return Sekuentzia("makur_ibili", 1.6f, Key.LeftCtrl, Key.W);
                yield return Kokatu(hasiera, 0f);
                yield return Sekuentzia("jauzia", 1.4f, Key.Space);
                Lerroak.Add($"OHARRA: sekuentziak {_sekuentziak}");
            }

            private IEnumerator Fasea(string izena, Vector3 hasiera, float yaw, float segundoak, string egoera, float abiadura, float yawEspero, string argazkia, params Key[] teklak)
            {
                yield return Kokatu(hasiera, yaw);
                var l = new Laginak();
                yield return Ibili(segundoak, l, teklak);
                Datua($"{izena}:", Laburpena(l));
                Oharra($"{izena}: {egoera} egoera", l.Egoerak.SetEquals(new[] { egoera }), string.Join(",", l.Egoerak));
                Oharra($"{izena}: abiadura {abiadura:0.0} m/s", Mathf.Abs(l.AbiaduraBb - abiadura) < abiadura * 0.05f, $"{l.AbiaduraBb:0.00}");
                Oharra($"{izena}: eredua mugimendura begira ({yawEspero:0} °)", Mathf.Abs(Mathf.DeltaAngle(l.YawBb, yawEspero)) < 10f, $"{l.YawBb:0} °");
                Oharra($"{izena}: airean faltsurik ez", l.AireFotogramak == 0);
                Datua($"{izena}: oin-zolaren tartea (pausoan)", $"{l.OinBehe:+0.000;-0.000} … {l.OinGoi:+0.000;-0.000} m");
                if (argazkia != null)
                {
                    Teklak(teklak);
                    MuguFotograma();
                    yield return Argazkia($"{argazkia}_3p", _kamera);
                    yield return Argazkia($"{argazkia}_alboa", _alboKamera);
                    Teklak();
                }
            }

            // ---------- Misioak (LevelManager) ----------

            private IEnumerator Misioa()
            {
                LevelManager km = FindAnyObjectByType<LevelManager>();
                HudKudeatzailea hud = FindAnyObjectByType<HudKudeatzailea>();
                Oharra("LevelManager-ek jokalaria sortu du eta HUD-a aktibo dago", km != null && km.HudAktibo && hud != null);
                Oharra("OnGUI zaharra ezkutatuta (HUD berria)", _kontrola.OnGuiEzkutatu);
                Oharra("Jokalaria spawn-ean", km != null && Vector3.Distance(new Vector3(_cc.transform.position.x, 0f, _cc.transform.position.z), new Vector3(km.SpawnPuntua.x, 0f, km.SpawnPuntua.z)) < 1.5f);

                Vector3 kamera = _kamera.transform.position;
                bool kameraLibre = !Physics.CheckSphere(kamera, 0.04f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                                   && !Physics.Linecast(_burua.position, kamera, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                Oharra("Spawn-eko sorbaldako kamera ez dago hormetan", kameraLibre);

                var geldirik = new Laginak();
                yield return Ibili(1.2f, geldirik);
                Datua("Geldirik:", Laburpena(geldirik));
                Oharra("Geldirik: Zutik, oinak ±3 cm", geldirik.Egoerak.SetEquals(new[] { "Zutik" }) && geldirik.OinBehe > -0.03f && geldirik.OinBehe < 0.03f, $"{geldirik.OinBehe:+0.000;-0.000} m");
                yield return Argazkia("3p_geldirik", _kamera);

                Vector3 hasiera = _cc.transform.position;
                float yaw = LibreYaw(hasiera, 4f);
                yield return Kokatu(hasiera, yaw);
                var ibili = new Laginak();
                yield return Ibili(1.2f, ibili, Key.W);
                Datua("Ibili:", Laburpena(ibili));
                Oharra("Ibili: Zutik, 3 m/s", ibili.Egoerak.SetEquals(new[] { "Zutik" }) && Mathf.Abs(ibili.AbiaduraBb - 3f) < 0.2f, $"{ibili.AbiaduraBb:0.00} m/s");
                Teklak(Key.W);
                MuguFotograma();
                yield return Argazkia("3p_ibili", _kamera);
                Teklak();

                var makurtu = new Laginak();
                yield return Ibili(1.2f, makurtu, Key.LeftCtrl);
                Oharra("Makurtuta egoera", makurtu.Egoerak.SetEquals(new[] { "Makurtuta" }), string.Join(",", makurtu.Egoerak));
                Teklak(Key.LeftCtrl);
                MuguFotograma();
                yield return Argazkia("3p_makurtuta", _kamera);
                Teklak();
                yield return Ibili(1f, null);
                yield return Kokatu(hasiera, yaw);
                ModuaM.Invoke(_kontrola, new object[] { false });
                yield return Ibili(0.3f, null);
                yield return Argazkia("1p", _kamera);
                ModuaM.Invoke(_kontrola, new object[] { true });
                yield return Ibili(0.3f, null);
            }

            private static float LibreYaw(Vector3 oinak, float luzera)
            {
                for (float yaw = 0f; yaw < 360f; yaw += 15f)
                {
                    Vector3 norabidea = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    if (!Physics.CapsuleCast(oinak + Vector3.up * 0.65f, oinak + Vector3.up * 1.5f, 0.32f, norabidea, luzera, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        return yaw;
                    }
                }

                return 0f;
            }
        }
    }
}
