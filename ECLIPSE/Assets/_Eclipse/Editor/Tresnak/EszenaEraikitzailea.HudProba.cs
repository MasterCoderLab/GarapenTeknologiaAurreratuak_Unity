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

        /// <summary>Jokalariaren scriptak konpilatu, build-ik egin gabe: UNITY_EDITOR eta DEVELOPMENT_BUILD gabe ere konpilatzen dela egiaztatzeko.</summary>
        public static void BatchJokalariKonpilazioa()
        {
            string karpeta = System.IO.Path.Combine("Temp", "HudJokalariKonpilazioa");
            var ezarpenak = new UnityEditor.Build.Player.ScriptCompilationSettings
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                options = UnityEditor.Build.Player.ScriptCompilationOptions.None,
            };
            UnityEditor.Build.Player.ScriptCompilationResult emaitza = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(ezarpenak, karpeta);
            bool ondo = emaitza.assemblies.Contains("Eclipse.Runtime.dll");
            Debug.Log($"{HudMarka}_{(ondo ? "OK" : "ERROR")} Jokalariaren scriptak ({ezarpenak.target}, garapenik gabe): {emaitza.assemblies.Count} ensanblatu; Eclipse.Runtime {(ondo ? "konpilatuta" : "EZ")}");
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
                    if (Physics.Linecast(aurrekoa + Vector3.up, p + Vector3.up, out RaycastHit jo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
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
                    Oharra($"Ibilbidea: {nagusia.Izena}", puntuak.Count >= 1, $"{bideLuzera:0} m, {puntuak.Count} puntu ({helburuak.Bidea.Count} nodotik)");

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
