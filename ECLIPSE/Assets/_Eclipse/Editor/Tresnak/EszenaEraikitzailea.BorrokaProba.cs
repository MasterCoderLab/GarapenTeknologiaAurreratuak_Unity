using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Eclipse.Borroka;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3 (lehen blokea): borrokaren proba Play moduan, 04_BorrokaProba eszenan (batch, sarrera simulatua). Eszenarik ez da gordetzen.
    /// Arma, apuntatzea, kalte lokalizatua, besoaren haustura, heriotza, hormak eta kantoiak, munizioa, etsaiaren IA, jokalariaren
    /// kaltea, heriotza eta berpiztea. Kapturak (ingurune neutroa) docs/qa/kapturak/A3_Borroka/-n.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string BpMarka = "ECL_BORROKAPROBA";
        private const double BpDenbora = 420.0;

        private static double _bpHasiera;
        private static readonly List<string> BpErroreak = new List<string>();

        public static void BatchBorrokaProba()
        {
            try
            {
                EditorSceneManager.OpenScene(BorrokaTresna.ProbaEszena, OpenSceneMode.Single);
                BpErroreak.Clear();
                Application.logMessageReceived += BpLog;
                _bpHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += BpEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{BpMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void BpLog(string mezua, string pila, LogType mota)
        {
            if ((mota == LogType.Error || mota == LogType.Exception || mota == LogType.Assert) && !mezua.StartsWith(BpMarka, StringComparison.Ordinal) && BpErroreak.Count < 20)
            {
                BpErroreak.Add($"{mota}: {mezua.Split('\n')[0]}");
            }
        }

        private static void BpEguneratu()
        {
            BorrokaProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<BorrokaProbaKontrola>();
            if (EditorApplication.timeSinceStartup - _bpHasiera > BpDenbora)
            {
                var lerroak = kontrola != null ? kontrola.Lerroak : new List<string>();
                lerroak.Add("HUTS: denbora agortuta");
                BpAmaitu(lerroak);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (kontrola == null && EtsaiSortzailea.Desgaituta)
            {
                // Erregresio-modua: etsairik ez da sortu behar.
                if (UnityEngine.Object.FindAnyObjectByType<JokalariArma>() != null && EditorApplication.timeSinceStartup - _bpHasiera > 20.0)
                {
                    int etsaiak = UnityEngine.Object.FindObjectsByType<Etsaia>(FindObjectsInactive.Include).Length;
                    BpAmaitu(new List<string> { $"{(etsaiak == 0 ? "ONDO" : "HUTS")}: -eclipseEtsairikGabe: {etsaiak} etsai (0 espero)" });
                }

                return;
            }

            if (kontrola == null)
            {
                var arma = UnityEngine.Object.FindAnyObjectByType<JokalariArma>();
                EtsaiSortzailea sortzailea = UnityEngine.Object.FindAnyObjectByType<EtsaiSortzailea>();
                if (arma != null && sortzailea != null && sortzailea.Sortuak.Count > 0)
                {
                    kontrola = new GameObject("BorrokaProbaKontrola").AddComponent<BorrokaProbaKontrola>();
                    kontrola.Hasi();
                }

                return;
            }

            if (kontrola.Amaituta)
            {
                BpAmaitu(kontrola.Lerroak);
            }
        }

        private static void BpAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= BpEguneratu;
            Application.logMessageReceived -= BpLog;
            lerroak.Add(BpErroreak.Count == 0 ? "ONDO: Play-n 0 errore/salbuespen log-ean" : $"HUTS: Play-n {BpErroreak.Count} errore: {string.Join(" | ", BpErroreak)}");
            var informea = new Informea("BorrokaProba", BpMarka, "A3: EMP pistola, kaltea eta etsaia Play-n (04_BorrokaProba)", BorrokaTresna.ProbaEszena, "Borroka");
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

        private sealed class BorrokaProbaKontrola : MonoBehaviour
        {
            private const BindingFlags Pribatua = BindingFlags.Instance | BindingFlags.NonPublic;
            private static readonly MethodInfo MugituM = typeof(EsploratzaileKontrola).GetMethod("Mugitu", Pribatua);
            private static readonly MethodInfo KameraM = typeof(EsploratzaileKontrola).GetMethod("LateUpdate", Pribatua);
            private static readonly MethodInfo ModuaM = typeof(EsploratzaileKontrola).GetMethod("KameraModuaEzarri", Pribatua);
            private static readonly FieldInfo PitchF = typeof(EsploratzaileKontrola).GetField("_pitch", Pribatua);

            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;

            private EsploratzaileKontrola _kontrola;
            private CharacterController _cc;
            private JokalariArma _arma;
            private JokalariBizitza _bizitza;
            private BorrokaHud _hud;
            private Transform _burua;
            private Camera _kamera;
            private Camera _alboKamera;
            private Animator _an;
            private Etsaia _irekia;
            private Etsaia _babesa;
            private Etsaia _urrun;
            private string _kapturak;
            private string _sekuentziak;

            public void Hasi()
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Exekutatu());
            }

            private void Oharra(string izena, bool ondo, string xehetasuna = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena} {xehetasuna}");

            private void Datua(string izena, string balioa) => Lerroak.Add($"DATUA: {izena} {balioa}");

            private void MuguFotograma()
            {
                MugituM.Invoke(_kontrola, new object[] { null });
                KameraM.Invoke(_kontrola, null);
            }

            private IEnumerator Itxaron(float segundoak, Action fotograma = null)
            {
                float amaiera = Time.time + segundoak;
                while (Time.time < amaiera)
                {
                    MuguFotograma();
                    fotograma?.Invoke();
                    yield return null;
                }
            }

            private IEnumerator Kokatu(Vector3 oinak, float yaw)
            {
                _cc.enabled = false;
                _cc.transform.SetPositionAndRotation(oinak, Quaternion.Euler(0f, yaw, 0f));
                _cc.enabled = true;
                Physics.SyncTransforms();
                yield return Itxaron(0.4f);
            }

            /// <summary>Kameraren erdigunea helburura: yaw-a erroan, pitch-a buruan (kamera sorbaldan dagoenez, iterazio batzuk).</summary>
            private IEnumerator Begiratu(Vector3 helburua)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 d = helburua - _kamera.transform.position;
                    float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Asin(Mathf.Clamp(d.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
                    _cc.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    _burua.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                    PitchF.SetValue(_kontrola, pitch);
                    MuguFotograma();
                    yield return null;
                }
            }

            private IEnumerator Tiro(string izena, Etsaia helburua, KalteZona zona)
            {
                Collider k = helburua.GetComponentsInChildren<KalteZonaKolisioa>().First(z => z.Zona == zona).GetComponent<Collider>();
                yield return Itxaron(0.36f);
                yield return Begiratu(k.bounds.center);
                TiroEmaitza e = _arma.TiroEgin();
                TiroInfo t = _arma.AzkenTiroa;
                Oharra($"{izena}: tiroa eta zona {zona}", e == TiroEmaitza.Tiro && ReferenceEquals(t.Jo, helburua) && t.Zona == zona,
                    $"{e}, jo={(t.Jo as Etsaia)?.name ?? "ezer ez"}, zona={t.Zona}, bizitza {helburua.Bizitza.Unekoa:0}");
            }

            private IEnumerator Argazkia(string izena, Camera kamera, string karpeta = null, int zabalera = 1280, int altuera = 720, Transform begira = null)
            {
                if (kamera == _alboKamera)
                {
                    Transform t = begira != null ? begira : _cc.transform;
                    Vector3 erdia = (t.position + _cc.transform.position) * 0.5f + Vector3.up * 1f;
                    Vector3 alboa = Vector3.Cross(Vector3.up, (t.position - _cc.transform.position).normalized);
                    if (alboa.sqrMagnitude < 0.01f)
                    {
                        alboa = _cc.transform.right;
                    }

                    _alboKamera.transform.position = erdia + alboa * 6f + Vector3.up * 0.6f;
                    _alboKamera.transform.LookAt(erdia);
                }

                yield return null;
                var rt = new RenderTexture(zabalera, altuera, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Canvas oihala = _hud != null && kamera == _kamera ? _hud.Oihala : null;
                RenderMode aurrekoa = oihala != null ? oihala.renderMode : RenderMode.ScreenSpaceOverlay;
                kamera.targetTexture = rt;
                if (oihala != null)
                {
                    oihala.renderMode = RenderMode.ScreenSpaceCamera;
                    oihala.worldCamera = kamera;
                    oihala.planeDistance = 0.5f;
                    Canvas.ForceUpdateCanvases();
                }

                kamera.Render();
                RenderTexture.active = rt;
                var irudia = new Texture2D(zabalera, altuera, TextureFormat.RGB24, false);
                irudia.ReadPixels(new Rect(0, 0, zabalera, altuera), 0, 0);
                irudia.Apply();
                RenderTexture.active = null;
                kamera.targetTexture = null;
                if (oihala != null)
                {
                    oihala.worldCamera = null;
                    oihala.renderMode = aurrekoa;
                }

                string helburua = karpeta ?? _kapturak;
                Directory.CreateDirectory(helburua);
                QaKapturak.Gorde(irudia.EncodeToPNG(), helburua, $"{(karpeta == null ? "neutro_" : string.Empty)}{izena}.png");
                Destroy(irudia);
                rt.Release();
                Destroy(rt);
            }

            private IEnumerator Exekutatu()
            {
                _kontrola = FindAnyObjectByType<EsploratzaileKontrola>();
                _cc = _kontrola.GetComponent<CharacterController>();
                _arma = _kontrola.GetComponent<JokalariArma>();
                _bizitza = _kontrola.GetComponent<JokalariBizitza>();
                _hud = FindAnyObjectByType<BorrokaHud>();
                _burua = _cc.transform.Find("Burua");
                _kamera = _kontrola.Kamera;
                _an = _kontrola.Bisuala != null ? _kontrola.Bisuala.Animatzailea : null;
                Etsaia[] etsaiak = FindAnyObjectByType<EtsaiSortzailea>().Sortuak.ToArray();
                foreach (Etsaia e in etsaiak)
                {
                    e.AdimenaGelditu = true;
                }

                _irekia = etsaiak.FirstOrDefault(e => e.name.EndsWith("_01", StringComparison.Ordinal));
                _babesa = etsaiak.FirstOrDefault(e => e.name.EndsWith("_02", StringComparison.Ordinal));
                _urrun = etsaiak.FirstOrDefault(e => e.name.EndsWith("_03", StringComparison.Ordinal));
                string proiektua = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _kapturak = QaKapturak.Azpikarpeta("A3_Borroka");
                _sekuentziak = Path.Combine(QaKapturak.Karpeta, "sekuentziak");
                var alboa = new GameObject("BorrokaProba_AlboKamera");
                _alboKamera = alboa.AddComponent<Camera>();
                _alboKamera.enabled = false;
                _alboKamera.fieldOfView = 45f;
                _alboKamera.nearClipPlane = 0.05f;
                _kontrola.enabled = false;
                yield return null;

                // ---------- Konfigurazioa ----------
                Oharra("Jokalari bakarra, armarekin eta bizitzarekin", FindObjectsByType<CharacterController>(FindObjectsInactive.Include).Length == 1 && _arma != null && _bizitza != null && _hud != null);
                Oharra("3 etsai sortuta markatzaileetan (runtime)", etsaiak.Length == 3 && _irekia != null && _babesa != null && _urrun != null, string.Join(", ", etsaiak.Select(e => e.name)));
                Oharra("Etsaiak NavMesh-ean", etsaiak.All(e => e.GetComponent<NavMeshAgent>().isOnNavMesh));
                Oharra("Etsai bakoitzak 10 kalte-zona", etsaiak.All(e => e.GetComponentsInChildren<KalteZonaKolisioa>().Length == 10));
                Transform eskua = _an != null ? _an.GetBoneTransform(HumanBodyBones.RightHand) : null;
                Oharra("Pistola eskuineko eskuan", _arma.Pistola != null && eskua != null && Vector3.Distance(_arma.Pistola.transform.position, eskua.position) < 0.15f,
                    eskua != null && _arma.Pistola != null ? $"{Vector3.Distance(_arma.Pistola.transform.position, eskua.position):0.000} m" : "");
                Oharra("Arma geruza aktibo (pisua 1)", _an != null && Mathf.Approximately(_an.GetLayerWeight(1), 1f));
                Renderer[] rr = _arma.Pistola.GetComponentsInChildren<Renderer>().Concat(_irekia.GetComponentsInChildren<Renderer>()).ToArray();
                string[] okerrak = rr.SelectMany(r => r.sharedMaterials.Select(m => m == null ? $"{r.name}: null" : !m.shader.isSupported ? $"{r.name}: {m.shader.name}" : null)).Where(s => s != null).ToArray();
                Oharra("Pistola eta etsaiaren materialak (null-ik eta magentarik ez)", okerrak.Length == 0, string.Join(", ", okerrak));
                Oharra("Munizioa hasieran 8 / 24", _arma.Logika.Kargadorean == 8 && _arma.Logika.Erreserba == 24);

                ModuaM.Invoke(_kontrola, new object[] { true });
                yield return Kokatu(new Vector3(0f, 0f, -2f), 0f);
                yield return Argazkia("01_eutsi_3p", _kamera);

                // ---------- Apuntatu ----------
                _arma.Apuntatu(true);
                yield return Begiratu(_irekia.transform.position + Vector3.up * 1.3f);
                yield return Itxaron(0.6f);
                bool apuntuEgoera = _an.GetCurrentAnimatorStateInfo(1).IsName("Apuntatu");
                Oharra("Apuntatu: modelo kamerara, kamera hurbilago, Apuntatu egoera", _kontrola.Bisuala.Modua == Jokalaria.OrientazioModua.Kamera
                    && _kontrola.KameraDesplazamendua == JokalariArma.ApuntuDesplazamendua && apuntuEgoera);
                Vector3 apuntua = _kamera.transform.position + _kamera.transform.forward * 8f;
                if (Physics.Raycast(_kamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out RaycastHit erdia, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    apuntua = erdia.point;
                }

                float angelua = Vector3.Angle(_arma.Pistola.transform.forward, apuntua - _arma.Pistola.transform.position);
                Oharra("Kanoia erretikulara lerrokatuta (< 3°)", angelua < 3f, $"{angelua:0.0}°");
                yield return Argazkia("02_apuntatu_3p", _kamera);
                yield return Argazkia("02b_apuntatu_alboa", _alboKamera, begira: _irekia.transform);

                // ---------- Kalte lokalizatua, besoaren haustura eta heriotza ----------
                int zatiak = 0;
                KalteZona? hautsia = null;
                _irekia.ZatiaHautsita += (_, z) => { zatiak++; hautsia = z; };
                yield return Tiro("Enborra", _irekia, KalteZona.Enborra);
                Oharra("Enborra: 25 kalte (100 → 75)", Mathf.Approximately(_irekia.Bizitza.Unekoa, 75f), $"{_irekia.Bizitza.Unekoa:0.##}");
                Oharra("Jotze-markatzailea jotzean", _hud.MarkaAlfa > 0.5f, $"{_hud.MarkaAlfa:0.00}");
                Oharra("Erretikula ikusgai (≥ 24 px) eta bizitza zenbakiz", _hud.ErretikulaTamaina >= 24f && _hud.BizitzaTestua.Contains("BIZITZA"), $"{_hud.ErretikulaTamaina:0} px, \"{_hud.BizitzaTestua}\"");
                yield return Argazkia("03_inpaktua_3p", _kamera);
                yield return Tiro("Beso ezkerra 1", _irekia, KalteZona.BesoEzkerra);
                yield return Tiro("Beso ezkerra 2", _irekia, KalteZona.BesoEzkerra);
                Oharra("Besoa: bi tiroren ondoren oraindik osorik", zatiak == 0);
                yield return Tiro("Beso ezkerra 3", _irekia, KalteZona.BesoEzkerra);
                Oharra("Besoa hausten da hirugarren tiroan (45 ≥ 40), gertaera bakarra", zatiak == 1 && hautsia == KalteZona.BesoEzkerra && _irekia.Gorputza.Hautsita(KalteZona.BesoEzkerra));
                Oharra("Besoa: 3 × 15 kalte (75 → 30)", Mathf.Abs(_irekia.Bizitza.Unekoa - 30f) < 0.01f, $"{_irekia.Bizitza.Unekoa:0.##}");
                yield return Argazkia("04_odola_alboa", _alboKamera, begira: _irekia.transform);
                yield return Tiro("Burua", _irekia, KalteZona.Burua);
                Oharra("Burua: ×2 → hilda", !_irekia.Bizirik && _irekia.Egoera == EtsaiEgoera.Hilda);
                yield return Itxaron(0.3f);
                Oharra("Hildakoak ez du kolisio aktiborik ez agenterik", _irekia.GetComponentsInChildren<Collider>().All(k => !k.enabled) && !_irekia.GetComponent<NavMeshAgent>().enabled);
                yield return Itxaron(1.5f);
                yield return Argazkia("05_hilda_alboa", _alboKamera, begira: _irekia.transform);
                _arma.Apuntatu(false);

                // ---------- Babesa: horma zeharkatu gabe ----------
                yield return Kokatu(new Vector3(6f, 0f, -2f), 0f);
                _arma.Apuntatu(true);
                yield return Begiratu(_babesa.transform.position + Vector3.up * 1.3f);
                yield return Itxaron(0.36f);
                _arma.TiroEgin();
                TiroInfo t = _arma.AzkenTiroa;
                Oharra("Horma atzean: tiroak ez du etsaia jotzen", t.Jo == null && Mathf.Approximately(_babesa.Bizitza.Unekoa, 100f) && t.Kolisioa != null && t.Kolisioa.name == "Babesa",
                    $"kolisioa {t.Kolisioa?.name}");
                yield return Argazkia("06_babesa_3p", _kamera);
                yield return Itxaron(0.2f);
                Oharra("Tiroaren zarata: babeseko etsaia jabetzen da", _babesa.Egoera != EtsaiEgoera.Zain, $"{_babesa.Egoera}");

                // ---------- Ertza: kamerak (eskuinera desplazatuta) hormaren muturra gainditzen du, muturrak ez ----------
                // Babesa: x 3,5..8,5, z 5,8..6,2. Jokalaria x 8,1-en: kamera ~8,7 (librea), mutura ~8,3 (hormaren atzean).
                yield return Kokatu(new Vector3(8.1f, 0f, 5f), 0f);
                yield return Begiratu(new Vector3(8.75f, 1.5f, 19.5f));
                yield return Itxaron(0.36f);
                _arma.TiroEgin();
                t = _arma.AzkenTiroa;
                Datua("Ertza:", $"kamera x {_kamera.transform.position.x:0.00}, mutura x {_arma.Pistola.Mutura.position.x:0.00}, kolisioa {t.Kolisioa?.name}, oztopatuta {t.Oztopatuta}");
                Oharra("Ertzean muturra blokeatuta: inpaktua hurbileko horman (ez da zeharkatzen)", t.Oztopatuta && t.Kolisioa != null && t.Kolisioa.name == "Babesa");
                yield return Argazkia("07_kantoia_3p", _kamera);
                _arma.Apuntatu(false);

                // ---------- Munizioa eta birkarga ----------
                int tiroak = 0;
                TiroEmaitza azkena = TiroEmaitza.Tiro;
                for (int i = 0; i < 12 && azkena != TiroEmaitza.Hutsik; i++)
                {
                    yield return Itxaron(0.32f);
                    azkena = _arma.TiroEgin();
                    tiroak += azkena == TiroEmaitza.Tiro ? 1 : 0;
                }

                Oharra("Kargadorea hustu eta 'hutsik'", azkena == TiroEmaitza.Hutsik && _arma.Logika.Kargadorean == 0, $"{tiroak} tiro gehiago");
                int erreserba = _arma.Logika.Erreserba;
                Oharra("R: birkarga hasi", _arma.Birkargatu());
                yield return Itxaron(0.3f);
                Oharra("Birkargatzen: tirorik ez", _arma.TiroEgin() == TiroEmaitza.Birkargatzen && _an.GetCurrentAnimatorStateInfo(1).IsName("Birkarga"));
                yield return Argazkia("08_birkarga_3p", _kamera);
                yield return Itxaron(1.4f);
                Oharra("Birkarga amaituta: 8 kargadorean, erreserbatik kenduta", _arma.Logika.Kargadorean == 8 && _arma.Logika.Erreserba == erreserba - 8, $"{_arma.Logika.Kargadorean}/{_arma.Logika.Erreserba}");
                Oharra("HUD-ak munizioa erakusten du", _hud.MunizioTestua.Contains("8 /"), _hud.MunizioTestua);

                // ---------- Etsaiaren IA: jarraitu, eraso, EMP txundidura, heriotza ----------
                yield return Kokatu(new Vector3(-6f, 0f, 8f), 300f);
                float bizitza0 = _bizitza.Logika.Unekoa;
                _urrun.AdimenaGelditu = false;
                var egoerak = new HashSet<EtsaiEgoera>();
                float hurbilena = float.MaxValue;
                string sekuentzia = Path.Combine(_sekuentziak, "erasoa");
                int fotogramak = 0;
                float hurrengoArgazkia = 0f;
                float amaiera = Time.time + 9f;
                while (Time.time < amaiera && _urrun.Kolpeak < 2)
                {
                    MuguFotograma();
                    egoerak.Add(_urrun.Egoera);
                    hurbilena = Mathf.Min(hurbilena, Vector3.Distance(_urrun.transform.position, _cc.transform.position));
                    if (Time.time >= hurrengoArgazkia)
                    {
                        hurrengoArgazkia = Time.time + 0.08f;
                        yield return Argazkia($"{fotogramak++:000}", _alboKamera, sekuentzia, 640, 360, _urrun.transform);
                    }
                    else
                    {
                        yield return null;
                    }
                }

                Datua("Etsaiaren egoerak:", string.Join(", ", egoerak));
                Oharra("Etsaiak jokalaria ikusi eta NavMesh-ean jarraitzen du", egoerak.Contains(EtsaiEgoera.Jarraitu) && hurbilena < 2.2f, $"gutxienez {hurbilena:0.00} m");
                Oharra("Etsaiak erasotzen du eta jokalariak kaltea jasotzen du", egoerak.Contains(EtsaiEgoera.Eraso) && _urrun.Kolpeak > 0 && _bizitza.Logika.Unekoa < bizitza0,
                    $"{_urrun.Kolpeak} kolpe, bizitza {bizitza0:0} → {_bizitza.Logika.Unekoa:0}");
                Oharra("Kaltean distira gorria (HUD)", _hud.DistiraAlfa > 0f, $"{_hud.DistiraAlfa:0.00}");
                yield return Argazkia("09_erasoa_alboa", _alboKamera, begira: _urrun.transform);
                yield return Argazkia("09b_erasoa_3p", _kamera);

                yield return Tiro("Erasotzailea", _urrun, KalteZona.Enborra);
                yield return Itxaron(0.05f);
                Oharra("EMP: kolpeak txunditzen du", _urrun.Egoera == EtsaiEgoera.Txundituta, $"{_urrun.Egoera}");
                yield return Itxaron(0.05f);
                ParticleSystem txundidura = _urrun.transform.Find("Txundidura")?.GetComponent<ParticleSystem>();
                Oharra("EMP: txinparta zianak txunditzean", txundidura != null && txundidura.emission.enabled && txundidura.particleCount > 0, txundidura != null ? $"{txundidura.particleCount} partikula" : "ez dago");
                yield return Argazkia("09c_txundidura_alboa", _alboKamera, begira: _urrun.transform);
                for (int i = 0; i < 4 && _urrun.Bizirik; i++)
                {
                    yield return Tiro($"Erasotzailea {i + 2}", _urrun, KalteZona.Enborra);
                }

                Oharra("Erasotzailea hilda (4 tiro enborrean)", !_urrun.Bizirik);

                // ---------- Jokalariaren heriotza eta berpiztea ----------
                _bizitza.KalteaJaso(new KalteInfo(KalteZona.Enborra, 500f, _cc.transform.position, Vector3.forward));
                Oharra("Jokalaria hilda", !_bizitza.Bizirik && _bizitza.Heriotzak == 1);
                yield return Itxaron(2.4f);
                Oharra("Berpiztua hasieran, bizitza osoa", _bizitza.Bizirik && Mathf.Approximately(_bizitza.Logika.Unekoa, 100f)
                    && Vector3.Distance(_cc.transform.position, new Vector3(0f, 0f, -12f)) < 0.6f, $"{_cc.transform.position}");

                // Lehen pertsona: zehaztasuna (babeseko etsaiaren buruan, hormarik gabeko aldetik).
                ModuaM.Invoke(_kontrola, new object[] { false });
                _babesa.AdimenaGelditu = true;
                yield return Kokatu(new Vector3(6f, 0f, 14f), 180f);
                yield return Tiro("Lehen pertsona, burua", _babesa, KalteZona.Burua);
                Oharra("Lehen pertsona: tiroa ez dago oztopatuta", !_arma.AzkenTiroa.Oztopatuta);
                yield return Itxaron(0.3f);
                Oharra("Lehen pertsona: arma eta eredua itzalak bakarrik", _kontrola.Bisuala.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer || r is SkinnedMeshRenderer)
                    .All(r => r.shadowCastingMode == ShadowCastingMode.ShadowsOnly));
                yield return Argazkia("10_lehen_pertsona", _kamera);
                Lerroak.Add($"OHARRA: erasoaren sekuentzia {sekuentzia}");
                Lerroak.Add("OHARRA: sagua (apuntatu/tiro), R eta V benetako sarrerarekin Ion-en proba dira (batch-ean kurtsorea ezin da blokeatu).");
                Amaituta = true;
            }
        }
    }
}
