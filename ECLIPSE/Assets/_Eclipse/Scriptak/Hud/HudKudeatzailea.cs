using Eclipse.Inbentarioak;
using Eclipse.Jokoa;
using Eclipse.Misioak;
using Eclipse.Nabigazioa;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Eclipse.Proba;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Eclipse.Hud
{
    public enum HudModua
    {
        Jokoa,
        Mapa,
        Inbentarioa,
    }

    /// <summary>
    /// HUD osoa: Play-n sortzen du LevelManager-ek, eszena aldatu gabe, eta eszenarekin batera desagertzen da.
    /// Esploratzailearen ondoren exekutatzen da: Esc tekla interfazea ixteko bakarrik erabiltzen da fotograma horretan.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class HudKudeatzailea : MonoBehaviour
    {
        private const float TestuTartea = 0.1f;

        private LevelManager _kudeatzailea;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private EsploratzaileKontrola _kontrola;
#endif
        private HudFabrika _fabrika;
        private HudMarkatzaileak _markatzaileak;
        private MapaPanela _mapa;
        private InbentarioPanela _inbentarioa;
        private RectTransform _jokoGeruza;
        private Text _misioa;
        private Text _helburua;
        private Text _fasea;
        private Image _aurrerapena;
        private Text _ibilbidea;
        private Text _nukleoak;
        private Text _irteera;
        private RectTransform _eKaxa;
        private Text _eTestua;
        private RectTransform _oharKaxa;
        private Text _oharTestua;
        private bool _ibilbideaZikina = true;
        private float _hurrengoTestua;

        public HudModua Modua { get; private set; }

        public HelburuKudeatzailea Helburuak { get; private set; }

        public Canvas Oihala { get; private set; }

        public int MarkatzaileKopurua => _markatzaileak.Ikusgai;

        public int MapaSolairuak => _mapa.SolairuKopurua;

        public System.Collections.Generic.IReadOnlyList<Vector3> MapaIbilbidea => _mapa.IbilbidePuntuak;

        public string EAbisua => _eKaxa.gameObject.activeSelf ? _eTestua.text : null;

        public string HelburuTestua => _helburua.text;

        public string FaseTestua => _fasea.text;

        public string NukleoTestua => _nukleoak.text;

        public string IrteeraTestua => _irteera.text;

        public string OharTestua => _oharKaxa.gameObject.activeSelf ? _oharTestua.text : null;

        /// <summary>Jokalaria sortu ondoren deitu behar da. Salbuespena LevelManager-ek harrapatzen du: OnGUI zaharra geratzen da.</summary>
        public static HudKudeatzailea Sortu(LevelManager kudeatzailea)
        {
            var objektua = new GameObject("ECLIPSE_HUD");
            var hud = objektua.AddComponent<HudKudeatzailea>();
            try
            {
                hud.Eraiki(kudeatzailea);
            }
            catch
            {
                Destroy(objektua);
                throw;
            }

            return hud;
        }

        /// <summary>Modua aldatu: mapa eta inbentarioa ez dira batera irekitzen, eta irekita daudela jokalaria geldi dago.</summary>
        public void ModuaEzarri(HudModua modua)
        {
            Modua = modua;
            bool irekita = modua != HudModua.Jokoa;
            _jokoGeruza.gameObject.SetActive(!irekita);
            _mapa.Erakutsi(modua == HudModua.Mapa);
            _inbentarioa.Erakutsi(modua == HudModua.Inbentarioa);
            _kudeatzailea.InterfazeaIrekita = irekita;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_kontrola != null)
            {
                _kontrola.SarreraBlokeatuta = irekita;
            }
#endif

            _ibilbideaZikina = true;
            _hurrengoTestua = 0f;
        }

        private void OnEnable()
        {
            GameEvents.HelburuaBeteta += HelburuaBeteta;
        }

        private void OnDisable()
        {
            GameEvents.HelburuaBeteta -= HelburuaBeteta;
        }

        private void OnDestroy()
        {
            _fabrika?.Dispose();
        }

        private void HelburuaBeteta(string id)
        {
            // Hesiak ere gertaera honekin ixten dira: ibilbidea Update-n kalkulatzen da, denak eguneratuta daudenean.
            _ibilbideaZikina = true;
            _hurrengoTestua = 0f;
        }

        private void Update()
        {
            Teklak();
            Helburuak.Eguneratu(_ibilbideaZikina);
            _ibilbideaZikina = false;

            if (Modua == HudModua.Jokoa)
            {
                _markatzaileak.Eguneratu(Camera.main, Helburuak);
                TerminalAbisua();
            }

            if (Time.unscaledTime >= _hurrengoTestua)
            {
                _hurrengoTestua = Time.unscaledTime + TestuTartea;
                TestuakEguneratu();
            }

            _inbentarioa.Eguneratu();
            if (Modua == HudModua.Mapa)
            {
                _mapa.Eguneratu();
            }
        }

        private void Teklak()
        {
            Keyboard teklatua = Keyboard.current;
            if (teklatua == null)
            {
                return;
            }

            if (teklatua.mKey.wasPressedThisFrame)
            {
                ModuaEzarri(Modua == HudModua.Mapa ? HudModua.Jokoa : HudModua.Mapa);
            }
            else if (teklatua.tabKey.wasPressedThisFrame)
            {
                ModuaEzarri(Modua == HudModua.Inbentarioa ? HudModua.Jokoa : HudModua.Inbentarioa);
            }
            else if (teklatua.escapeKey.wasPressedThisFrame && Modua != HudModua.Jokoa)
            {
                ModuaEzarri(HudModua.Jokoa);
            }

            Inbentarioa inbentarioa = GameManager.Saioa.Inbentarioa;
            if (teklatua.digit1Key.wasPressedThisFrame)
            {
                inbentarioa.BizkorraHautatu(0);
            }
            else if (teklatua.digit2Key.wasPressedThisFrame)
            {
                inbentarioa.BizkorraHautatu(1);
            }
            else if (teklatua.digit3Key.wasPressedThisFrame)
            {
                inbentarioa.BizkorraHautatu(2);
            }
            else if (teklatua.digit4Key.wasPressedThisFrame)
            {
                inbentarioa.BizkorraHautatu(3);
            }
        }

        private void TerminalAbisua()
        {
            MisioTerminala terminala = Helburuak.TerminalBarruan();
            bool makurtu = terminala == null && Helburuak.MakurtzekoGuneaHurbil;
            _eKaxa.gameObject.SetActive(terminala != null || makurtu);
            if (makurtu)
            {
                _eTestua.text = "[Ctrl]  Makurtu azpitik pasatzeko";
                _eTestua.color = HudKoloreak.Anbarra;
            }

            if (terminala == null)
            {
                return;
            }

            bool aktibo = _kudeatzailea.Logika.Aktibo(terminala.Helburua);
            _eTestua.text = aktibo ? $"[E]  {terminala.Izena}" : $"{terminala.Izena}: oraindik ez dago prest";
            _eTestua.color = aktibo ? HudKoloreak.Anbarra : HudKoloreak.Grisa;
        }

        private void TestuakEguneratu()
        {
            MisioLogika logika = _kudeatzailea.Logika;
            _misioa.text = _kudeatzailea.MisioIzena;
            _helburua.text = logika.Amaituta ? "Misioa beteta: joan irteerara" : logika.UnekoDeskribapena;
            if (logika.Amaituta)
            {
                _fasea.text = "Fase guztiak beteta";
                _aurrerapena.rectTransform.anchorMax = Vector2.one;
            }
            else
            {
                _fasea.text = $"{logika.UnekoFasea + 1}. fasea / {logika.FaseKopurua}   |   Helburuak: {logika.FasekoBeteak} / {logika.FasekoGuztiak}";
                _aurrerapena.rectTransform.anchorMax = new Vector2(logika.FasekoGuztiak > 0 ? (float)logika.FasekoBeteak / logika.FasekoGuztiak : 0f, 1f);
            }

            switch (Helburuak.Egoera)
            {
                case BideEgoera.Ondo:
                    _ibilbidea.text = $"{Helburuak.Nagusia.Izena}: {Helburuak.BideLuzera:0} m";
                    _ibilbidea.color = HudKoloreak.Anbarra;
                    break;
                case BideEgoera.BiderikEz:
                    _ibilbidea.text = "Ibilbiderik ez: begiratu mapa (M)";
                    _ibilbidea.color = HudKoloreak.Gorria;
                    break;
                default:
                    _ibilbidea.text = string.Empty;
                    break;
            }

            _nukleoak.text = _kudeatzailea.BildumakGuztira > 0 ? $"Datu-nukleoak: {_kudeatzailea.Bildumak} / {_kudeatzailea.BildumakGuztira}" : string.Empty;
            _irteera.text = logika.Amaituta ? "IRTEERA IREKITA" : "IRTEERA ITXITA";
            _irteera.color = logika.Amaituta ? HudKoloreak.Ziana : HudKoloreak.Gorria;

            string oharra = _kudeatzailea.UnekoOharra;
            _oharKaxa.gameObject.SetActive(oharra != null && Modua == HudModua.Jokoa);
            if (oharra != null)
            {
                _oharTestua.text = oharra;
            }
        }

        private void Eraiki(LevelManager kudeatzailea)
        {
            _kudeatzailea = kudeatzailea;
            CharacterController jokalaria = FindAnyObjectByType<CharacterController>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _kontrola = jokalaria != null ? jokalaria.GetComponent<EsploratzaileKontrola>() : null;
#endif
            Vector3 hasiera = jokalaria != null ? jokalaria.transform.position : kudeatzailea.SpawnPuntua;
            NabigazioDatuak datuak = NabigazioEraikitzailea.Eraiki(hasiera);
            Helburuak = new HelburuKudeatzailea(kudeatzailea, datuak, jokalaria != null ? jokalaria.transform : null);

            _fabrika = new HudFabrika();
            Oihala = gameObject.AddComponent<Canvas>();
            Oihala.renderMode = RenderMode.ScreenSpaceOverlay;
            Oihala.sortingOrder = 100;
            var eskalatzailea = gameObject.AddComponent<CanvasScaler>();
            eskalatzailea.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            eskalatzailea.referenceResolution = new Vector2(1920f, 1080f);
            eskalatzailea.matchWidthOrHeight = 0.5f;

            _jokoGeruza = _fabrika.Elementua("Jokoa", transform);
            HudFabrika.Bete(_jokoGeruza);
            _markatzaileak = new HudMarkatzaileak(_fabrika, _jokoGeruza);
            HelburuPanelaSortu();
            KontrolakSortu();
            AbisuakSortu();

            _inbentarioa = new InbentarioPanela(_fabrika, transform, _jokoGeruza, GameManager.Saioa, kudeatzailea);
            _mapa = new MapaPanela(_fabrika, transform, Helburuak, kudeatzailea);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_kontrola != null)
            {
                _kontrola.OnGuiEzkutatu = true;
            }
#endif

            Helburuak.Eguneratu(true);
            TestuakEguneratu();
        }

        private void HelburuPanelaSortu()
        {
            RectTransform panela = HudFabrika.Kokatu(_fabrika.Panela("Helburua", _jokoGeruza, HudKoloreak.Ziana), Vector2.up, Vector2.up, new Vector2(24f, -24f), new Vector2(640f, 250f));

            Text Lerroa(string izena, int tamaina, Color kolorea, float y, float altuera, FontStyle estiloa = FontStyle.Normal)
            {
                Text testua = _fabrika.Testua(izena, panela, tamaina, kolorea, TextAnchor.UpperLeft, estiloa);
                HudFabrika.Kokatu(testua.rectTransform, Vector2.up, Vector2.up, new Vector2(22f, y), new Vector2(600f, altuera));
                return testua;
            }

            _misioa = Lerroa("Misioa", 26, HudKoloreak.Ziana, -12f, 32f, FontStyle.Bold);
            _helburua = Lerroa("Deskribapena", 28, HudKoloreak.Zuria, -46f, 72f);
            _fasea = Lerroa("Fasea", 22, HudKoloreak.Grisa, -118f, 28f);

            Image barra = _fabrika.Irudia("Barra", panela, _fabrika.Karratua, HudKoloreak.ZianIluna);
            HudFabrika.Kokatu(barra.rectTransform, Vector2.up, Vector2.up, new Vector2(22f, -150f), new Vector2(596f, 8f));
            _aurrerapena = _fabrika.Irudia("Aurrerapena", barra.transform, _fabrika.Karratua, HudKoloreak.Ziana);
            RectTransform aurrerapena = _aurrerapena.rectTransform;
            aurrerapena.anchorMin = Vector2.zero;
            aurrerapena.anchorMax = new Vector2(0f, 1f);
            aurrerapena.offsetMin = Vector2.zero;
            aurrerapena.offsetMax = Vector2.zero;

            _ibilbidea = Lerroa("Ibilbidea", 22, HudKoloreak.Anbarra, -168f, 28f, FontStyle.Bold);
            _nukleoak = Lerroa("DatuNukleoak", 22, HudKoloreak.Zuria, -204f, 28f);
            _irteera = _fabrika.Testua("Irteera", panela, 22, HudKoloreak.Gorria, TextAnchor.UpperRight, FontStyle.Bold);
            HudFabrika.Kokatu(_irteera.rectTransform, Vector2.one, Vector2.one, new Vector2(-22f, -204f), new Vector2(300f, 28f));
        }

        private void KontrolakSortu()
        {
            RectTransform panela = HudFabrika.Kokatu(_fabrika.Panela("Kontrolak", _jokoGeruza, HudKoloreak.ZianIluna), Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(720f, 92f));
            Text testua = _fabrika.Testua("Testua", panela, 20, HudKoloreak.Grisa, TextAnchor.MiddleLeft);
            testua.text = "[M] Mapa     [Tab] Inbentarioa     [Esc] Itxi / kurtsorea\n[E] Erabili     [V] Lehen / hirugarren pertsona\n[Shift] Korrika     [Ctrl] Makurtu     [Zuriunea] Jauzi";
            RectTransform rect = testua.rectTransform;
            HudFabrika.Bete(rect);
            rect.offsetMin = new Vector2(18f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
        }

        private void AbisuakSortu()
        {
            _eKaxa = HudFabrika.Kokatu(_fabrika.Panela("EAbisua", _jokoGeruza, HudKoloreak.Anbarra), new Vector2(0.5f, 0.27f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 60f));
            _eTestua = _fabrika.Testua("Testua", _eKaxa, 28, HudKoloreak.Anbarra, TextAnchor.MiddleCenter, FontStyle.Bold);
            HudFabrika.Bete(_eTestua.rectTransform);
            _eKaxa.gameObject.SetActive(false);

            _oharKaxa = HudFabrika.Kokatu(_fabrika.Panela("Oharra", _jokoGeruza, HudKoloreak.Zuria), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 60f));
            _oharTestua = _fabrika.Testua("Testua", _oharKaxa, 26, HudKoloreak.Zuria, TextAnchor.MiddleCenter);
            HudFabrika.Bete(_oharTestua.rectTransform);
            _oharKaxa.gameObject.SetActive(false);
        }
    }
}
