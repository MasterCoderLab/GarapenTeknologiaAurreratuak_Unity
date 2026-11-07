using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Hud;
using Eclipse.Inbentarioak;
using Eclipse.Misioak;
using Eclipse.Nabigazioa;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// HUD, markatzaile, mapa eta inbentarioaren proba, Play moduan eta batchmode-n: teklatu birtuala, kapturak bi bereizmenetan,
    /// eta robot bat maparen ibilbide marraztuetatik misio osoa egiten, hurrengo eszena kargatu arte. Eszenarik ez du gordetzen.
    /// Batch-ean kurtsorea ezin da blokeatu: mugimenduaren blokeoa Ion-ek egiaztatu behar du Play-n.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string HudMarka = "ECL_HUDPROBA";
        private const double HudDenbora = 500.0;

        private static MailaDef _hudMaila;
        private static double _hudHasiera;

        /// <summary>Batchmode: -eclipseMaila 1|2|3.</summary>
        public static void BatchHudProba()
        {
            try
            {
                _hudMaila = BatchMailaLortu();
                EditorSceneManager.OpenScene(_hudMaila.Bidea, OpenSceneMode.Single);
                _hudHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += HudProbaEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{HudMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Mision02_Energia-n hodien bi kolisioak gehitu, kontrol zehatzarekin: aurrebistak 2 aldaketa bakarrik aurkitu behar ditu,
        /// bestela ez da ezer aplikatzen ez gordetzen. Kolisioek ez dute renderer-ik: ez dute argiztapena ukitzen.
        /// </summary>
        public static void BatchM2HodiKolisioak()
        {
            const int Espero = 2;
            int kodea = 1;
            try
            {
                MailaDef d = Mision02();
                EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                if (!MailaExekutatu(d, false) || _azkenAldaketak != Espero)
                {
                    Debug.LogError($"{HudMarka}_ERROR Aurrebista: {_azkenAldaketak} aldaketa, {Espero} espero ziren. Ez da ezer aplikatu.");
                }
                else if (MailaExekutatu(d, true) && EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
                {
                    EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                    bool bigarrena = MailaExekutatu(d, false);
                    Debug.Log($"{HudMarka}_{(bigarrena && _azkenAldaketak == 0 ? "OK" : "ERROR")} M2 kolisioak: aplikatuta; bigarren aurrebista {_azkenAldaketak} aldaketa.");
                    kodea = bigarrena && _azkenAldaketak == 0 ? 0 : 1;
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{HudMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>
        /// Jokalariaren scriptak konpilatu, build-ik egin gabe, DEVELOPMENT_BUILD gabe: jokalaria DLL-an dagoela eta autoprobatzailea ez dagoela
        /// islapenez egiaztatzeko. Kontrol negatiboa: A0-ko Release zaharrean mota EZ da aurkitu behar.
        /// </summary>
        public static void BatchJokalariKonpilazioa()
        {
            string karpeta = System.IO.Path.Combine("Temp", "HudJokalariKonpilazioa");
            var ezarpenak = new UnityEditor.Build.Player.ScriptCompilationSettings
            {
                target = BuildTarget.StandaloneLinux64,
                group = BuildTargetGroup.Standalone,
                options = UnityEditor.Build.Player.ScriptCompilationOptions.None,
            };
            UnityEditor.Build.Player.ScriptCompilationResult emaitza = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(ezarpenak, karpeta);
            string dll = System.IO.Path.Combine(karpeta, "Eclipse.Runtime.dll");
            bool konpilatuta = emaitza.assemblies.Contains("Eclipse.Runtime.dll") && System.IO.File.Exists(dll);
            bool jokalaria = konpilatuta && DllMotaDu(dll, JokalariMota);
            bool autoProba = konpilatuta && DllMotaDu(dll, AutoProbaMota);

            string zaharra = System.IO.Directory.Exists("Build_Windows/ECLIPSE_Eszenatokia")
                ? System.IO.Directory.GetFiles("Build_Windows/ECLIPSE_Eszenatokia", "Eclipse.Runtime.dll", System.IO.SearchOption.AllDirectories).FirstOrDefault()
                : null;
            string kontrola = zaharra == null ? "ez da exekutatu (Release zaharra ez dago)" : DllMotaDu(zaharra, JokalariMota) ? "HUTS: mota aurkitu du" : "ondo: ez du mota aurkitu";

            bool ondo = jokalaria && !autoProba && !kontrola.StartsWith("HUTS", StringComparison.Ordinal);
            Debug.Log($"{HudMarka}_{(ondo ? "OK" : "ERROR")} Jokalariaren scriptak ({ezarpenak.target}, garapenik gabe): {emaitza.assemblies.Count} ensanblatu; EsploratzaileKontrola={jokalaria}; AutoProbaQA={autoProba}; kontrol negatiboa: {kontrola}");
            EditorApplication.Exit(ondo ? 0 : 1);
        }

        private static void HudProbaEguneratu()
        {
            if (EditorApplication.timeSinceStartup - _hudHasiera > HudDenbora)
            {
                HudProbaKontrola geratua = UnityEngine.Object.FindAnyObjectByType<HudProbaKontrola>();
                var lerroak = geratua != null ? geratua.Lerroak : new List<string>();
                lerroak.Add("HUTS: denbora agortuta");
                HudProbaAmaitu(lerroak);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            HudProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<HudProbaKontrola>();
            if (kontrola == null && UnityEngine.Object.FindAnyObjectByType<HudKudeatzailea>() != null)
            {
                kontrola = new GameObject("HudProbaKontrola").AddComponent<HudProbaKontrola>();
                kontrola.Hasi(_hudMaila);
            }

            if (kontrola != null && kontrola.Amaituta)
            {
                HudProbaAmaitu(kontrola.Lerroak);
            }
        }

        private static void HudProbaAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= HudProbaEguneratu;
            var informea = new Informea(Tresna, HudMarka, $"{_hudMaila.Izena}: HUD, mapa eta inbentarioa (Play, sarrera birtuala)", _hudMaila.Bidea, _hudMaila.Erroa);
            informea.Atala("Emaitzak", lerroak);
            int hutsak = lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal));
            if (hutsak == 0)
            {
                informea.AmaituOndo($"{lerroak.Count} egiaztapen, 0 huts");
            }
            else
            {
                informea.AmaituErrorearekin($"{hutsak} huts, {lerroak.Count} egiaztapenetik");
            }

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(hutsak == 0 ? 0 : 1);
        }

        private sealed class HudProbaKontrola : MonoBehaviour
        {
            private const float RobotAbiadura = 5f;
            private const float Geldirik = 6f;

            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;

            private MailaDef _maila;
            private Keyboard _teklatua;
            private HudKudeatzailea _hud;
            private LevelManager _km;
            private CharacterController _cc;
            private EsploratzaileKontrola _kontrola;

            public void Hasi(MailaDef maila)
            {
                _maila = maila;
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Hasiera());
            }

            private void Oharra(string izena, bool ondo, string xehetasuna = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena} {xehetasuna}");

            private void Teklak(params Key[] teklak) => InputSystem.QueueStateEvent(_teklatua, new KeyboardState(teklak));

            private IEnumerator Sakatu(Key tekla)
            {
                Teklak(tekla);
                yield return null;
                yield return null;
                Teklak();
                yield return null;
                yield return null;
            }

            private IEnumerator Kokatu(Vector3 oinak, float yaw = 0f)
            {
                _cc.enabled = false;
                _cc.transform.SetPositionAndRotation(oinak, Quaternion.Euler(0f, yaw, 0f));
                _cc.enabled = true;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            /// <summary>Kameraren irudia eta HUD-a batera. Oihala kamerara lotzen da kaptura bakoitzerako: Overlay ez dago Camera.Render-en.</summary>
            private IEnumerator Argazkia(string izena)
            {
                foreach ((int zabalera, int altuera) in new[] { (1920, 1080), (1280, 720) })
                {
                    Camera kamera = Camera.main;
                    Canvas oihala = _hud.Oihala;
                    var rt = new RenderTexture(zabalera, altuera, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    RenderMode aurrekoa = oihala.renderMode;
                    kamera.targetTexture = rt;
                    oihala.renderMode = RenderMode.ScreenSpaceCamera;
                    oihala.worldCamera = kamera;
                    oihala.planeDistance = 0.5f;
                    yield return null;
                    yield return null;

                    // Eskala kaptura honetan aldatu da: testuak berriro sortu, bestela aurreko eskalako letrekin ateratzen dira.
                    foreach (UnityEngine.UI.Text testua in oihala.GetComponentsInChildren<UnityEngine.UI.Text>())
                    {
                        testua.SetAllDirty();
                    }

                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    kamera.Render();
                    RenderTexture.active = rt;
                    var irudia = new Texture2D(zabalera, altuera, TextureFormat.RGB24, false);
                    irudia.ReadPixels(new Rect(0, 0, zabalera, altuera), 0, 0);
                    irudia.Apply();
                    RenderTexture.active = null;
                    EbidentziaKapturak.ProbaGorde(irudia, $"HUD_{_maila.Aurrizkia}_{izena}_{altuera}.png");
                    DestroyImmediate(irudia);
                    kamera.targetTexture = null;
                    oihala.worldCamera = null;
                    oihala.renderMode = aurrekoa;
                    rt.Release();
                    DestroyImmediate(rt);
                    yield return null;
                }
            }

            private IEnumerator Hasiera()
            {
                InputSettings ezarpenak = ScriptableObject.CreateInstance<InputSettings>();
                ezarpenak.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                ezarpenak.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings = ezarpenak;
                _teklatua = InputSystem.AddDevice<Keyboard>("ProbaTeklatua");
                _teklatua.MakeCurrent();
                yield return null;
                yield return null;
                Lortu();

                NabigazioDatuak datuak = _hud.Helburuak.Datuak;
                Oharra("LevelManager-ek HUD-a sortu du", _km.HudAktibo);
                Oharra("Sarea erabilgarria", datuak.Erabilgarria, $"{datuak.Sarea?.NodoKopurua} nodo, {datuak.Sarea?.AlderdiKopurua} alderdi, {datuak.Gelak.Count} gela, {datuak.Blokeoak.Count} blokeo, {datuak.Milisegundoak} ms");
                Oharra("Diagnostikoa hutsik", datuak.Diagnostikoa.Count == 0, string.Join(" | ", datuak.Diagnostikoa));
                int blokeoGabe = datuak.Blokeoak.Count(b => b.Alderdiak == 0);
                Oharra("Hesi guztiek alderdiak mozten dituzte", blokeoGabe == 0, string.Join(", ", datuak.Blokeoak.Select(b => $"{b.Atea.name}: {b.Alderdiak}")));
                Oharra("Solairuak", _hud.MapaSolairuak >= 1, _hud.MapaSolairuak.ToString());

                TestuakProbatu();
                yield return Argazkia("hasiera");
                yield return TeklakProbatu();
                yield return AbisuakProbatu();
                yield return BildumaProbatu();
                yield return MarkatzaileakProbatu();
                IrteeraBlokeoaProbatu();
                yield return MugimenduaProbatu();
                if (_maila.Izena == "Mision02_Energia")
                {
                    yield return HodiakProbatu();
                }

                yield return Robota();
            }

            private void Lortu()
            {
                _hud = FindAnyObjectByType<HudKudeatzailea>();
                _km = FindAnyObjectByType<LevelManager>();
                _cc = FindAnyObjectByType<CharacterController>();
                _kontrola = _cc != null ? _cc.GetComponent<EsploratzaileKontrola>() : null;
            }

            private void TestuakProbatu()
            {
                MisioLogika logika = _km.Logika;
                Oharra("Helburu-testua logikatik dator", _hud.HelburuTestua == logika.UnekoDeskribapena, _hud.HelburuTestua);
                Oharra("Fasearen aurrerapena", _hud.FaseTestua.Contains($"/ {logika.FaseKopurua}"), _hud.FaseTestua);
                Oharra("Datu-nukleoen kontagailua", _km.BildumakGuztira == 0 || _hud.NukleoTestua.Contains($"/ {_km.BildumakGuztira}"), _hud.NukleoTestua);
                Oharra("Irteera itxita hasieran", _hud.IrteeraTestua == "IRTEERA ITXITA", _hud.IrteeraTestua);
            }

            private IEnumerator TeklakProbatu()
            {
                void Egoera(string izena, HudModua modua)
                {
                    bool irekita = modua != HudModua.Jokoa;
                    Oharra(izena, _hud.Modua == modua && _km.InterfazeaIrekita == irekita && (_kontrola == null || _kontrola.SarreraBlokeatuta == irekita), $"modua {_hud.Modua}");
                }

                yield return Sakatu(Key.M);
                Egoera("M: mapa ireki eta sarrera blokeatu", HudModua.Mapa);
                yield return Argazkia("mapa");
                if (_hud.MapaSolairuak > 1)
                {
                    yield return Sakatu(Key.UpArrow);
                    yield return new WaitForSecondsRealtime(0.3f);
                    yield return Argazkia("mapa_goia");
                    yield return Sakatu(Key.DownArrow);
                }

                Oharra("Maparen ibilbideak puntuak ditu", _hud.MapaIbilbidea.Count >= 2, $"{_hud.MapaIbilbidea.Count} puntu, {_hud.Helburuak.BideLuzera:0} m");
                yield return Sakatu(Key.Escape);
                Egoera("Esc: mapa itxi", HudModua.Jokoa);
                yield return Sakatu(Key.Tab);
                Egoera("Tab: inbentarioa ireki", HudModua.Inbentarioa);
                yield return Argazkia("inbentarioa");
                yield return Sakatu(Key.M);
                Egoera("M inbentariotik: mapara pasatu", HudModua.Mapa);
                yield return Sakatu(Key.M);
                Egoera("M: mapa itxi", HudModua.Jokoa);
                yield return Sakatu(Key.Tab);
                yield return Sakatu(Key.Tab);
                Egoera("Tab birritan: itxita", HudModua.Jokoa);

                yield return Sakatu(Key.Digit1);
                Oharra("1 hutsik dagoen ranuran: ez du ezer hautatzen", GameManager.Saioa.Inbentarioa.Hautatua == -1);
                Oharra("Arma fikziorik ez", !GameManager.Saioa.Inbentarioa.Badago(ElementuDatuak.EmpPistolaId) && GameManager.Saioa.Inbentarioa.Bizkorrak.All(r => r.Hutsik));
            }

            private IEnumerator AbisuakProbatu()
            {
                MisioLogika logika = _km.Logika;
                MisioTerminala[] terminalak = FindObjectsByType<MisioTerminala>(FindObjectsInactive.Include);
                MisioTerminala aktiboa = terminalak.First(t => logika.Aktibo(t.Helburua));
                MisioTerminala besteFasekoa = terminalak.FirstOrDefault(t => !logika.Aktibo(t.Helburua) && !logika.BetetaDago(t.Helburua));
                Vector3 hasiera = _cc.transform.position;

                // Terminal aktiboa: E abisua, eta mapa irekita E-k ez du ezer egiten.
                yield return TerminalaraJoan(aktiboa);
                Oharra("Jokalaria terminalaren trigger-ean", aktiboa.JokalariaBarruan);
                yield return new WaitForSecondsRealtime(0.2f);
                Oharra("E abisua terminal aktiboan", _hud.EAbisua != null && _hud.EAbisua.Contains("[E]") && _hud.EAbisua.Contains(aktiboa.Izena), _hud.EAbisua);
                yield return Argazkia("terminala");
                yield return Sakatu(Key.M);
                yield return Sakatu(Key.E);
                Oharra("Mapa irekita, E-k ez du helbururik betetzen", !logika.BetetaDago(aktiboa.Helburua));
                yield return Sakatu(Key.M);

                if (besteFasekoa != null)
                {
                    yield return TerminalaraJoan(besteFasekoa);
                    yield return new WaitForSecondsRealtime(0.2f);
                    Oharra("Beste faseko terminala: ez dago prest", _hud.EAbisua != null && !_hud.EAbisua.Contains("[E]"), _hud.EAbisua);
                }

                yield return Kokatu(hasiera);
                yield return new WaitForSecondsRealtime(0.2f);
                Oharra("Abisua desagertzen da irtetean", _hud.EAbisua == null);
            }

            private IEnumerator TerminalaraJoan(MisioTerminala terminala)
            {
                // Terminalaren trigger-a 4,2 m-koa da: sareko nodo hurbilena barruan dago.
                NabigazioSarea sarea = _hud.Helburuak.Datuak.Sarea;
                int nodoa = sarea.Hurbilena(terminala.transform.position, 3.5f);
                Vector3 oinak = nodoa >= 0 ? sarea.Posizioa(nodoa) : terminala.transform.position;
                yield return Kokatu(oinak + Vector3.up * 0.05f);
            }

            private IEnumerator BildumaProbatu()
            {
                MisioBilduma bilduma = FindObjectsByType<MisioBilduma>(FindObjectsInactive.Exclude).FirstOrDefault();
                if (bilduma == null)
                {
                    Oharra("Datu-nukleorik ez maila honetan", true);
                    yield break;
                }

                int aurretik = _km.Bildumak;
                int saioan = GameManager.Saioa.DatuNukleoak;
                Vector3 hasiera = _cc.transform.position;
                NabigazioSarea sarea = _hud.Helburuak.Datuak.Sarea;
                int nodoa = sarea.Hurbilena(bilduma.transform.position, 3f, 1f);
                Oharra("Datu-nukleoa sarean", nodoa >= 0, bilduma.name);
                if (nodoa >= 0)
                {
                    yield return Kokatu(sarea.Posizioa(nodoa) + Vector3.up * 0.05f);
                    yield return new WaitForSecondsRealtime(0.3f);
                    Oharra("Datu-nukleoa bildu da", _km.Bildumak == aurretik + 1 && !bilduma.gameObject.activeSelf, $"{_km.Bildumak} / {_km.BildumakGuztira}");
                    Oharra("HUD-ak kontagailua eguneratu du", _hud.NukleoTestua.Contains($"{_km.Bildumak} /"), _hud.NukleoTestua);
                    Oharra("Saioak zenbatu du", GameManager.Saioa.DatuNukleoak == saioan + 1, GameManager.Saioa.DatuNukleoak.ToString());
                    Oharra("Bildutakoa hautaketatik kanpo", _hud.Helburuak.Hautatuak.All(h => h.Id != bilduma.name));
                }

                yield return Kokatu(hasiera);
            }

            private IEnumerator MarkatzaileakProbatu()
            {
                MisioLogika logika = _km.Logika;
                yield return null;
                List<HelburuHautagaia> hautatuak = _hud.Helburuak.Hautatuak;
                int terminalak = hautatuak.Count(h => h.Mota == HelburuMota.Terminala);
                int bestelakoak = hautatuak.Count(h => h.Mota == HelburuMota.Terminala && !logika.Aktibo(h.Id));
                Oharra("Terminal aktiboak bakarrik markatuta", terminalak > 0 && bestelakoak == 0, $"{terminalak} terminal");
                Oharra("Irteera ez dago markatuta misioa amaitu arte", hautatuak.All(h => h.Mota != HelburuMota.Irteera));
                Oharra("Pantaila ez dago beteta", hautatuak.Count <= 6, $"{hautatuak.Count} markatzaile");

                // 360 graduko bira, fotogramaz fotograma, HUD erreal bidez: markatzaileak ez dira galtzen eta beti pantailan daude.
                Camera kamera = Camera.main;
                RectTransform geruza = (RectTransform)_hud.transform.Find("Jokoa/Markatzaileak");
                int kanpoan = 0;
                int barruan = 0;
                int ertzean = 0;
                for (int yaw = 0; yaw < 360; yaw += 10)
                {
                    _cc.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    yield return null;
                    yield return null;
                    foreach (RectTransform m in geruza.Cast<Transform>().Where(t => t.gameObject.activeSelf).Cast<RectTransform>())
                    {
                        Vector2 p = m.anchoredPosition;
                        if (p.x < 0f || p.y < 0f || p.x > geruza.rect.width || p.y > geruza.rect.height)
                        {
                            kanpoan++;
                        }

                        if (m.Find("Gezia").gameObject.activeSelf)
                        {
                            ertzean++;
                        }
                        else
                        {
                            barruan++;
                        }
                    }
                }

                Oharra("Bira osoan: pantailatik kanpo 0", kanpoan == 0, $"{kanpoan} kanpoan, {barruan} barruan, {ertzean} ertzean geziarekin");
                Oharra("Bira osoan: barruan eta ertzean egon da", barruan > 0 && ertzean > 0);
                Oharra("Kamera nagusia badago", kamera != null);
            }

            /// <summary>Batch-ean kurtsorea ezin da blokeatu: osagaia geldituta, Mugitu benetako teklatu-egoerarekin deitzen da, fotograma bakoitzean behin.</summary>
            private IEnumerator Ibili(float segundoak, Action fotograma, params Key[] teklak)
            {
                System.Reflection.MethodInfo mugitu = typeof(EsploratzaileKontrola).GetMethod("Mugitu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Teklak(teklak);
                yield return null;
                float amaiera = Time.realtimeSinceStartup + segundoak;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    mugitu.Invoke(_kontrola, new object[] { _teklatua });
                    fotograma?.Invoke();
                    yield return null;
                }

                Teklak();
                yield return null;
            }

            private float LibreYaw(Vector3 oinak, float luzera)
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

            private IEnumerator MugimenduaProbatu()
            {
                _kontrola.enabled = false;
                Vector3 hasiera = _cc.transform.position;
                float yaw = LibreYaw(hasiera, 6f);
                yield return Kokatu(hasiera, yaw);
                float y0 = _cc.transform.position.y;
                float gailurra = y0;
                void Gailurra() => gailurra = Mathf.Max(gailurra, _cc.transform.position.y);

                yield return Ibili(1.1f, Gailurra, Key.Space);
                Oharra("Zuriunea: jauzia", gailurra - y0 > 0.8f && gailurra - y0 < 1.15f, $"{gailurra - y0:0.00} m");
                Oharra("Jauzia: lurrera itzuli da", _cc.isGrounded && Mathf.Abs(_cc.transform.position.y - y0) < 0.1f, $"y {_cc.transform.position.y:0.00}");

                // Airean bigarren aldiz sakatuta: ez du berriro jauzi egiten.
                gailurra = y0;
                yield return Ibili(0.15f, Gailurra, Key.Space);
                yield return Ibili(0.9f, Gailurra, Key.Space);
                Oharra("Airean ez dago bigarren jauzirik", gailurra - y0 < 1.15f, $"{gailurra - y0:0.00} m");

                yield return Ibili(0.5f, null, Key.LeftCtrl);
                Oharra("Ctrl: makurtu", Mathf.Abs(_kontrola.UnekoAltuera - 1.1f) < 0.01f && Mathf.Abs(_cc.height - 1.1f) < 0.01f, $"altuera {_cc.height:0.00}");
                Oharra("Ctrl: begiak behera", Mathf.Abs(_cc.transform.Find("Burua").localPosition.y - 0.95f) < 0.01f);

                gailurra = y0;
                yield return Ibili(0.6f, Gailurra, Key.LeftCtrl, Key.Space);
                Oharra("Makurtuta ez dago jauzirik", gailurra - y0 < 0.05f, $"{gailurra - y0:0.00} m");

                yield return Kokatu(hasiera, yaw);
                Vector3 a = _cc.transform.position;
                float t0 = Time.realtimeSinceStartup;
                yield return Ibili(1f, null, Key.LeftCtrl, Key.W);
                float abiadura = Vector3.Distance(a, _cc.transform.position) / (Time.realtimeSinceStartup - t0);
                Oharra("Makurtuta motelago", abiadura > 1.1f && abiadura < 1.6f, $"{abiadura:0.00} m/s");

                yield return Kokatu(hasiera, yaw);
                a = _cc.transform.position;
                t0 = Time.realtimeSinceStartup;
                yield return Ibili(1f, null, Key.LeftCtrl, Key.LeftShift, Key.W);
                abiadura = Vector3.Distance(a, _cc.transform.position) / (Time.realtimeSinceStartup - t0);
                Oharra("Makurtuta ez dago korrikarik", abiadura < 1.6f, $"{abiadura:0.00} m/s");

                yield return Ibili(0.5f, null);
                Oharra("Ctrl askatuta: zutik", Mathf.Abs(_cc.height - 1.8f) < 0.01f, $"altuera {_cc.height:0.00}");
                yield return Kokatu(hasiera);
                _kontrola.enabled = true;
            }

            /// <summary>Hodien benetako kolisioak, M2-ko neurriekin: pasabidea 4,6 m-ra, hodia 58,125 m-ko X-an eta bertikala (52,875; 46,875).</summary>
            private IEnumerator HodiakProbatu()
            {
                const float Pasabidea = 4.65f;
                _kontrola.enabled = false;
                Vector3 hasiera = _cc.transform.position;

                // Hegoaldeko pasabidea, mailaditik ekialdera.
                yield return Kokatu(new Vector3(57.45f, Pasabidea, 31.2f), 90f);
                yield return Ibili(1.2f, null, Key.W);
                float x = _cc.transform.position.x;
                Oharra("Hegoaldea: zutik, __005 hodiak gelditzen du", x < 57.8f && x > 57.4f, $"x {x:0.00}");
                yield return Ibili(2f, null, Key.LeftCtrl, Key.W);
                x = _cc.transform.position.x;
                Oharra("Hegoaldea: makurtuta, azpitik pasatzen da", x > 58.6f, $"x {x:0.00}");
                yield return Ibili(0.5f, null);
                Oharra("Hegoaldea: pasatu ondoren zutik", Mathf.Abs(_cc.height - 1.8f) < 0.01f, $"altuera {_cc.height:0.00}");

                // Hodiaren azpian Ctrl askatuta: ez da zutitzen, eta ez du hodia zeharkatzen.
                yield return Ibili(0.5f, null, Key.LeftCtrl);
                yield return Kokatu(new Vector3(58.125f, Pasabidea, 31.2f), 90f);
                yield return Ibili(0.6f, null);
                Oharra("Azpian Ctrl askatuta: makurtuta jarraitzen du", _cc.height < 1.15f, $"altuera {_cc.height:0.00}");
                yield return Ibili(0.6f, null, Key.Space);
                Oharra("Azpian: jauzi ezin da", _cc.transform.position.y < Pasabidea + 0.1f, $"y {_cc.transform.position.y:0.00}");
                yield return Ibili(1.2f, null, Key.W);
                x = _cc.transform.position.x;
                yield return Ibili(0.5f, null);
                Oharra("Azpitik irtetean zutitzen da", x > 58.5f && Mathf.Abs(_cc.height - 1.8f) < 0.01f, $"x {x:0.00}, altuera {_cc.height:0.00}");

                // Iparraldeko pasabidea, ekialdetik mendebaldera.
                yield return Kokatu(new Vector3(59.5f, Pasabidea, 47.55f), 270f);
                yield return Ibili(1.2f, null, Key.W);
                x = _cc.transform.position.x;
                Oharra("Iparraldea: zutik, __005 hodiak gelditzen du", x > 58.45f, $"x {x:0.00}");
                yield return Ibili(2f, null, Key.LeftCtrl, Key.W);
                x = _cc.transform.position.x;
                Oharra("Iparraldea: makurtuta, azpitik pasatzen da", x < 57.7f, $"x {x:0.00}");
                yield return Ibili(0.5f, null);

                // __002 hodi bertikala: talka, eta alboan pasabidea libre.
                yield return Kokatu(new Vector3(54.5f, Pasabidea, 46.875f), 270f);
                yield return Ibili(1.2f, null, Key.W);
                x = _cc.transform.position.x;
                Oharra("__002 hodi bertikalak gelditzen du", x > 53.2f, $"x {x:0.00}");
                yield return Ibili(0.8f, null, Key.Space, Key.W);
                x = _cc.transform.position.x;
                Oharra("__002: jauzita ere ezin da gainetik pasa", x > 53.2f, $"x {x:0.00}");
                yield return Kokatu(new Vector3(54.5f, Pasabidea, 47.9f), 270f);
                yield return Ibili(1.5f, null, Key.W);
                x = _cc.transform.position.x;
                // Hodia 52,875-ean dago; aurrerago terminalaren kontsolak gelditzen du (50,44-an).
                Oharra("__002: alboan zutik pasatzen da", x < 52.3f, $"x {x:0.00}");

                // Ekialdeko pasabidea: goiko hodien azpitik zutik.
                yield return Kokatu(new Vector3(66.3f, Pasabidea, 43.5f), 0f);
                yield return Ibili(1.2f, null, Key.W);
                float z = _cc.transform.position.z;
                Oharra("__004 (2,13 m): zutik azpitik", z > 46f && _cc.height > 1.79f, $"z {z:0.00}");
                yield return Kokatu(new Vector3(66.3f, Pasabidea, 35f), 0f);
                yield return Ibili(1.2f, null, Key.W);
                z = _cc.transform.position.z;
                Oharra("Laranja __000 (1,75 m): zutik azpitik, kolisiorik gabe", z > 37.5f && _cc.height > 1.79f, $"z {z:0.00}");

                yield return Kokatu(hasiera);
                _kontrola.enabled = true;
            }

            private void IrteeraBlokeoaProbatu()
            {
                NabigazioDatuak datuak = _hud.Helburuak.Datuak;
                MisioIrteera irteera = FindAnyObjectByType<MisioIrteera>();
                int helmuga = datuak.Sarea.Hurbilena(irteera.transform.position, 3.5f, 1f);
                int hasiera = datuak.Sarea.Hurbilena(_cc.transform.position, 3f);
                var bidea = new List<int>();
                bool itxita = datuak.Sarea.BideaBilatu(hasiera, helmuga, _hud.Helburuak.BlokeoItxiak, bidea);
                Oharra("Irteera sarean dago", helmuga >= 0);
                Oharra("Hesi itxiekin ez dago ibilbiderik irteerara", !itxita);
                bool irekita = datuak.Sarea.BideaBilatu(hasiera, helmuga, null, bidea);
                Oharra("Hesiak irekita ibilbidea badago", irekita, $"{datuak.Sarea.BideLuzera(bidea):0} m");
            }

            /// <summary>
            /// Tartea zoruari jarraituz, jokalariak bezala: bularraren altueran ezer ez da zeharkatzen eta maila bakoitza igo daiteke.
            /// Bi muturren arteko zuzen hutsak mailadiaren bloke barrutik egingo luke bidea.
            /// </summary>
            private static string TarteanTalka(Vector3 a, Vector3 b)
            {
                const float Pausoa = 0.1f;
                int pausoak = Mathf.Max(1, Mathf.CeilToInt(new Vector2(b.x - a.x, b.z - a.z).magnitude / Pausoa));
                Vector3 aurrekoa = a;
                for (int k = 1; k <= pausoak; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (float)k / pausoak);
                    p.y = aurrekoa.y;
                    if (!Physics.SphereCast(p + Vector3.up * (MailaIgoera + 0.05f + OinErradioa), OinErradioa, Vector3.down, out RaycastHit zorua, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        return $"zorurik ez {p}";
                    }

                    if (zorua.point.y - aurrekoa.y > MailaIgoera)
                    {
                        return $"maila altuegia {p}: {zorua.collider.name}";
                    }

                    p.y = zorua.point.y;
                    // Makurtuta dagoen gorputzaren erdian: hodi baxuen azpitik pasatzen diren tarteak baliozkoak dira.
                    if (Physics.Linecast(aurrekoa + Vector3.up * 0.6f, p + Vector3.up * 0.6f, out RaycastHit jo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        return $"horma {p}: {jo.collider.name}";
                    }

                    aurrekoa = p;
                }

                return null;
            }

            /// <summary>Irteerako trigger-a azken puntura iritsi aurretik pizten da, eta eszena aldatzen da.</summary>
            private bool MisioaItxita => _cc == null || _km == null || _km.MisioaAmaituta;

            /// <summary>Ibilbide marraztuak jarraitu: kapsula tarte zuzenetan doa eta tarte bakoitzak ez du hormarik zeharkatzen.</summary>
            private IEnumerator Robota()
            {
                MisioLogika logika = _km.Logika;
                HelburuKudeatzailea helburuak = _hud.Helburuak;
                NabigazioSarea sarea = helburuak.Datuak.Sarea;
                string hurrengoa = _maila.Misioa.Hurrengoa;
                int nukleoakHasieran = GameManager.Saioa.DatuNukleoak;
                var puntuak = new List<Vector3>();
                float ibilia = 0f;
                int tarteak = 0;
                int talkak = 0;
                string lehenTalka = null;
                int terminalak = 0;
                string aurrekoNagusia = null;
                bool atzeraEginda = false;
                bool makurAbisua = false;

                while (true)
                {
                    helburuak.Eguneratu(true);
                    if (!helburuak.NagusiaDago || helburuak.Egoera != BideEgoera.Ondo)
                    {
                        Oharra("Ibilbidea helburura", false, $"{helburuak.Nagusia.Izena}: egoera {helburuak.Egoera}, jokalaria {_cc.transform.position}");
                        Amaitu();
                        yield break;
                    }

                    HelburuHautagaia nagusia = helburuak.Nagusia;
                    if (aurrekoNagusia == nagusia.Id && nagusia.Mota == HelburuMota.Terminala)
                    {
                        Oharra("Betetako helburua ez da errepikatzen", false, nagusia.Id);
                        Amaitu();
                        yield break;
                    }

                    aurrekoNagusia = nagusia.Id;
                    float bideLuzera = helburuak.BideLuzera;
                    sarea.Sinplifikatu(helburuak.Bidea, helburuak.BlokeoItxiak, puntuak);
                    Oharra($"Ibilbidea: {nagusia.Izena}", puntuak.Count >= 1, $"{bideLuzera:0} m, {puntuak.Count} puntu ({helburuak.Bidea.Count} nodotik){(helburuak.BideanMakurtu ? ", makurtuta" : string.Empty)}");
                    if (helburuak.BideanMakurtu)
                    {
                        var kausak = new List<string>();
                        foreach (int n in helburuak.Bidea.Where(sarea.MakurtuBehar))
                        {
                            Vector3 p = sarea.Posizioa(n);
                            Collider[] joak = Physics.OverlapCapsule(p + Vector3.up * 0.62f, p + Vector3.up * 1.55f, 0.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                            string kausa = joak.Length > 0 ? $"{joak[0].transform.parent?.parent?.name}/{joak[0].name} {joak[0].bounds.center} {joak[0].bounds.size}" : "?";
                            kausak.Add($"y={p.y:0.0} {kausa}");
                        }

                        Lerroak.Add($"OHARRA: makurtzeko nodoak ({kausak.Count}): {string.Join(" | ", kausak.Distinct().Take(8))}");
                    }

                    if (_maila.Izena == "Mision02_Energia" && nagusia.Id == "pasabidea")
                    {
                        Oharra("Maparen ibilbideak hodiaren azpitik pasatzen du", helburuak.BideanMakurtu);
                    }

                    for (int i = 1; i < puntuak.Count; i++)
                    {
                        tarteak++;
                        string talka = TarteanTalka(puntuak[i - 1], puntuak[i]);
                        if (talka != null)
                        {
                            talkak++;
                            lehenTalka ??= $"{puntuak[i - 1]} -> {puntuak[i]}: {talka}";
                        }
                    }

                    foreach (Vector3 puntua in puntuak.Skip(1).ToList())
                    {
                        float hurbilena = float.MaxValue;
                        float aurrerapena = Time.time;
                        while (!MisioaItxita)
                        {
                            Vector3 alde = puntua - _cc.transform.position;
                            alde.y = 0f;
                            float distantzia = alde.magnitude;
                            if (distantzia < 0.3f)
                            {
                                break;
                            }

                            if (distantzia < hurbilena - 0.02f)
                            {
                                hurbilena = distantzia;
                                aurrerapena = Time.time;
                            }
                            else if (Time.time - aurrerapena > Geldirik)
                            {
                                Oharra("Robota trabatuta ibilbide marraztuan", false, $"{_cc.transform.position} -> {puntua}");
                                Amaitu();
                                yield break;
                            }

                            _kontrola.MakurtuEzarri(RobotakMakurtuBehar(_cc.transform.position, alde.normalized));
                            if (!makurAbisua && _hud.EAbisua != null && _hud.EAbisua.Contains("[Ctrl]"))
                            {
                                makurAbisua = true;
                                yield return Argazkia("makurtu_abisua");
                            }

                            Vector3 aurretik = _cc.transform.position;
                            _cc.Move(alde.normalized * Mathf.Min(RobotAbiadura * Time.deltaTime, distantzia) + Vector3.down * (9.8f * Time.deltaTime));
                            ibilia += Vector3.Distance(aurretik, _cc.transform.position);
                            yield return null;
                        }
                    }

                    if (nagusia.Mota == HelburuMota.Irteera)
                    {
                        break;
                    }

                    // Helburua: E teklarekin, benetako trigger-a itxaronda.
                    MisioTerminala terminala = FindObjectsByType<MisioTerminala>(FindObjectsInactive.Exclude).First(t => t.Helburua == nagusia.Id);
                    float itxaron = Time.time;
                    while (!terminala.JokalariaBarruan && Time.time - itxaron < Geldirik)
                    {
                        yield return null;
                    }

                    Oharra($"Ibilbidea terminalaren barruan amaitzen da: {nagusia.Id}", terminala.JokalariaBarruan, $"{_cc.transform.position}");
                    yield return Sakatu(Key.E);
                    Oharra($"E-k helburua bete du: {nagusia.Id}", logika.BetetaDago(nagusia.Id));
                    terminalak++;
                    if (!logika.BetetaDago(nagusia.Id))
                    {
                        Amaitu();
                        yield break;
                    }

                    yield return null;
                    helburuak.Eguneratu(true);
                    Oharra($"Betetako markatzailea desagertu da: {nagusia.Id}", helburuak.Hautatuak.All(h => h.Id != nagusia.Id));
                    if (logika.Amaituta && !atzeraEginda)
                    {
                        atzeraEginda = true;
                        yield return Sakatu(Key.M);
                        yield return new WaitForSecondsRealtime(0.3f);
                        yield return Argazkia("mapa_amaiera");
                        yield return Sakatu(Key.M);
                        Oharra("Misioa amaituta: irteera markatuta", helburuak.Hautatuak.Any(h => h.Mota == HelburuMota.Irteera));
                        Oharra("Misioa amaituta: hesiak irekita", helburuak.BlokeoItxiak.All(i => !i));
                        Oharra("HUD: irteera irekita", _hud.IrteeraTestua == "IRTEERA IREKITA", _hud.IrteeraTestua);
                        yield return Argazkia("amaiera");
                    }
                }

                bool irteeraIritsi = MisioaItxita;
                if (_maila.Izena == "Mision02_Energia")
                {
                    Oharra("HUD-ak [Ctrl] abisua erakutsi du hodira iristean", makurAbisua);
                }

                Oharra("Tarte marraztuek ez dute ezer zeharkatzen", talkak == 0, $"{tarteak} tarte, {talkak} talka {lehenTalka}");
                Oharra("Terminal guztiak ibilbidez", terminalak == _maila.Misioa.Terminalak.Length, $"{terminalak}, {ibilia:0} m");

                // Irteerak misioa ixten du eta hurrengoa kargatzen du. Saioak iraun behar du eta HUD berria sortu.
                float amaiera = Time.realtimeSinceStartup + 20f;
                string hasierakoEszena = _maila.Izena;
                while (!MisioaItxita && Time.realtimeSinceStartup < amaiera)
                {
                    yield return null;
                }

                Oharra("Misioa amaitu da irteeran", MisioaItxita, irteeraIritsi ? "ibilbidearen barruan" : "ibilbidearen amaieran");
                if (!string.IsNullOrEmpty(hurrengoa))
                {
                    while (SceneManager.GetActiveScene().name == hasierakoEszena && Time.realtimeSinceStartup < amaiera)
                    {
                        yield return null;
                    }

                    yield return null;
                    yield return null;
                    Oharra("Hurrengo eszena kargatu da", SceneManager.GetActiveScene().name == hurrengoa, SceneManager.GetActiveScene().name);
                    Lortu();
                    Oharra("HUD berria sortu da hurrengo misioan", _hud != null && _km != null && _km.HudAktibo);
                    Oharra("Datu-nukleoek iraun dute misio-aldaketan", GameManager.Saioa.DatuNukleoak >= nukleoakHasieran, $"{GameManager.Saioa.DatuNukleoak}");
                    Oharra("GameManager bakarra", FindObjectsByType<GameManager>(FindObjectsInactive.Include).Length == 1);
                    Oharra("Mapa irekita ez dago hasieran", _hud != null && _hud.Modua == HudModua.Jokoa);
                }

                Amaitu();
            }

            private void Amaitu()
            {
                InputSystem.RemoveDevice(_teklatua);
                Amaituta = true;
            }
        }
    }
}
