using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eclipse.Misioak;
using Eclipse.Nabigazioa;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Eclipse.Hud
{
    /// <summary>
    /// Mapa (M). Oinarria sarearen benetako gelaxkak dira, testura batean margotuta: barruko hormak eta prop handiak hutsune gisa ikusten dira.
    /// Iparra gora, X ekialdera. Ibilbidea sareko alderdietatik bakarrik doa; ez badago, ez da lerrorik marrazten.
    /// </summary>
    internal sealed class MapaPanela
    {
        private const int PixelakMetroan = 6;
        private const float SolairuMuga = 2f;
        private const float MapaTartea = 2f;
        private const float EguneratzeTartea = 0.25f;
        private const int GelaGehienez = 9;

        private static readonly Vector2 EremuTamaina = new Vector2(1340f, 1020f);
        private static readonly Color32 GelaFondoa = new Color32(14, 28, 38, 255);
        private static readonly Color32 GelaIbilgarria = new Color32(32, 70, 88, 255);
        private static readonly Color32 PasilloIbilgarria = new Color32(24, 52, 66, 255);
        private static readonly Color32 Ertza = new Color32(90, 205, 235, 255);
        private static readonly Color32 MakurGunea = new Color32(150, 105, 30, 255);

        private readonly HudFabrika _f;
        private readonly HelburuKudeatzailea _helburuak;
        private readonly LevelManager _kudeatzailea;
        private readonly NabigazioDatuak _datuak;
        private readonly RectTransform _erroa;
        private readonly RectTransform _eremua;
        private readonly RawImage _azpikoa;
        private readonly RawImage _oinarria;
        private readonly RectTransform _ibilbideGeruza;
        private readonly RectTransform _ikonoGeruza;
        private readonly Texture2D[] _testurak;
        private readonly List<Image> _tarteak = new List<Image>();
        private readonly List<(Text Testua, int Solairua)> _etiketak = new List<(Text, int)>();
        private readonly List<Image> _ikonoak = new List<Image>();
        private readonly List<Vector3> _puntuak = new List<Vector3>();
        private readonly Image _jokalaria;
        private readonly Text _alboTestua;
        private readonly Text _solairuTestua;
        private readonly Vector2 _mapaMin;
        private readonly float _eskala;
        private readonly Vector2 _desplazamendua;
        private int _ikonoErabiliak;
        private float _hurrengoEguneraketa;

        public MapaPanela(HudFabrika f, Transform gurasoa, HelburuKudeatzailea helburuak, LevelManager kudeatzailea)
        {
            _f = f;
            _helburuak = helburuak;
            _kudeatzailea = kudeatzailea;
            _datuak = helburuak.Datuak;

            Image estalkia = f.Irudia("Mapa", gurasoa, f.Karratua, HudKoloreak.FondoOsoa);
            _erroa = estalkia.rectTransform;
            HudFabrika.Bete(_erroa);

            _eremua = HudFabrika.Kokatu(f.Elementua("Eremua", _erroa), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), EremuTamaina);

            // Mugak: gela guztiak eta nodoak. Sare gabe ere gelak ikusten dira.
            var mugak = new Rect();
            bool lehena = true;
            void Zabaldu(float x, float z)
            {
                if (lehena)
                {
                    mugak = new Rect(x, z, 0f, 0f);
                    lehena = false;
                    return;
                }

                mugak.xMin = Mathf.Min(mugak.xMin, x);
                mugak.xMax = Mathf.Max(mugak.xMax, x);
                mugak.yMin = Mathf.Min(mugak.yMin, z);
                mugak.yMax = Mathf.Max(mugak.yMax, z);
            }

            foreach (GelaDatua g in _datuak.Gelak)
            {
                Zabaldu(g.Kaxa.min.x, g.Kaxa.min.z);
                Zabaldu(g.Kaxa.max.x, g.Kaxa.max.z);
            }

            if (_datuak.Erabilgarria)
            {
                for (int n = 0; n < _datuak.Sarea.NodoKopurua; n++)
                {
                    Vector3 p = _datuak.Sarea.Posizioa(n);
                    Zabaldu(p.x, p.z);
                }
            }

            _mapaMin = new Vector2(mugak.xMin - MapaTartea, mugak.yMin - MapaTartea);
            Vector2 mapaTamaina = new Vector2(Mathf.Max(1f, mugak.width) + MapaTartea * 2f, Mathf.Max(1f, mugak.height) + MapaTartea * 2f);
            _eskala = Mathf.Min(EremuTamaina.x / mapaTamaina.x, EremuTamaina.y / mapaTamaina.y);
            _desplazamendua = (EremuTamaina - mapaTamaina * _eskala) * 0.5f;

            int solairuak = _datuak.Erabilgarria && Enumerable.Range(0, _datuak.Sarea.NodoKopurua).Any(n => _datuak.Sarea.Posizioa(n).y >= SolairuMuga) ? 2 : 1;
            _testurak = new Texture2D[solairuak];
            for (int s = 0; s < solairuak; s++)
            {
                _testurak[s] = TesturaMargotu(s, mapaTamaina);
            }

            RectTransform OinarriRect(RawImage irudia)
            {
                return HudFabrika.Kokatu(irudia.rectTransform, Vector2.zero, Vector2.zero, _desplazamendua, mapaTamaina * _eskala);
            }

            _azpikoa = Elementua("Azpikoa").gameObject.AddComponent<RawImage>();
            _azpikoa.raycastTarget = false;
            _azpikoa.color = new Color(1f, 1f, 1f, 0.3f);
            OinarriRect(_azpikoa);
            _oinarria = Elementua("Oinarria").gameObject.AddComponent<RawImage>();
            _oinarria.raycastTarget = false;
            OinarriRect(_oinarria);

            _ibilbideGeruza = Elementua("Ibilbidea");
            HudFabrika.Bete(_ibilbideGeruza);
            EtiketakSortu();
            _ikonoGeruza = Elementua("Ikonoak");
            HudFabrika.Bete(_ikonoGeruza);

            _jokalaria = f.Irudia("Jokalaria", _eremua, f.Gezia, HudKoloreak.Zuria);
            HudFabrika.Kokatu(_jokalaria.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));

            // Iparra: mapa ez da biratzen, eta jokalariaren geziak horren arabera seinalatzen du.
            Image iparra = f.Irudia("Iparra", _eremua, f.Gezia, HudKoloreak.Grisa);
            HudFabrika.Kokatu(iparra.rectTransform, Vector2.up, new Vector2(0.5f, 1f), new Vector2(30f, -8f), new Vector2(26f, 26f));
            Text iparTestua = f.Testua("IparTestua", _eremua, 20, HudKoloreak.Grisa, TextAnchor.UpperCenter, FontStyle.Bold);
            iparTestua.text = "I";
            HudFabrika.Kokatu(iparTestua.rectTransform, Vector2.up, new Vector2(0.5f, 1f), new Vector2(30f, -36f), new Vector2(40f, 26f));

            RectTransform albokoa = f.Panela("Alboa", _erroa, HudKoloreak.Ziana);
            HudFabrika.Kokatu(albokoa, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(500f, 1020f));
            Text izenburua = f.Testua("Izenburua", albokoa, 40, HudKoloreak.Ziana, TextAnchor.UpperLeft, FontStyle.Bold);
            izenburua.text = "MAPA";
            HudFabrika.Kokatu(izenburua.rectTransform, Vector2.up, Vector2.up, new Vector2(28f, -22f), new Vector2(440f, 54f));
            _solairuTestua = f.Testua("Solairua", albokoa, 24, HudKoloreak.Anbarra, TextAnchor.UpperLeft, FontStyle.Bold);
            HudFabrika.Kokatu(_solairuTestua.rectTransform, Vector2.up, Vector2.up, new Vector2(28f, -78f), new Vector2(450f, 34f));
            _alboTestua = f.Testua("Testua", albokoa, 24, HudKoloreak.Zuria);
            _alboTestua.supportRichText = true;
            HudFabrika.Kokatu(_alboTestua.rectTransform, Vector2.up, Vector2.up, new Vector2(28f, -120f), new Vector2(450f, 520f));
            LegendaSortu(albokoa);
            Erakutsi(false);
        }

        public bool Irekita => _erroa.gameObject.activeSelf;

        public int Solairua { get; private set; }

        public int SolairuKopurua => _testurak.Length;

        public IReadOnlyList<Vector3> IbilbidePuntuak => _puntuak;

        public void Erakutsi(bool ikusgai)
        {
            _erroa.gameObject.SetActive(ikusgai);
            if (ikusgai)
            {
                Solairua = SolairuaKalkulatu(_helburuak.JokalariPosizioa.y);
                _hurrengoEguneraketa = 0f;
            }
        }

        /// <summary>Probetarako eta geziekin: solairua aldatu.</summary>
        public void SolairuaEzarri(int solairua)
        {
            Solairua = Mathf.Clamp(solairua, 0, _testurak.Length - 1);
            _hurrengoEguneraketa = 0f;
        }

        public void Eguneratu()
        {
            Keyboard teklatua = Keyboard.current;
            if (teklatua != null && _testurak.Length > 1)
            {
                if (teklatua.upArrowKey.wasPressedThisFrame)
                {
                    SolairuaEzarri(Solairua + 1);
                }
                else if (teklatua.downArrowKey.wasPressedThisFrame)
                {
                    SolairuaEzarri(Solairua - 1);
                }
            }

            // Jokalaria fotograma guztietan; gainerakoa maiztasun txikiagoan.
            Vector3 jokalaria = _helburuak.JokalariPosizioa;
            _jokalaria.rectTransform.anchoredPosition = MapaPuntua(jokalaria);
            _jokalaria.rectTransform.localEulerAngles = new Vector3(0f, 0f, -JokalariYaw());
            _jokalaria.color = SolairuaKalkulatu(jokalaria.y) == Solairua ? HudKoloreak.Zuria : new Color(1f, 1f, 1f, 0.4f);

            if (Time.unscaledTime < _hurrengoEguneraketa)
            {
                return;
            }

            _hurrengoEguneraketa = Time.unscaledTime + EguneratzeTartea;
            _oinarria.texture = _testurak[Solairua];
            _azpikoa.texture = _testurak[0];
            _azpikoa.gameObject.SetActive(Solairua > 0);
            foreach ((Text testua, int solairua) in _etiketak)
            {
                testua.gameObject.SetActive(solairua == Solairua);
            }

            IbilbideaMarraztu();
            IkonoakMarraztu();
            AlboaIdatzi();
        }

        private float JokalariYaw()
        {
            Camera kamera = Camera.main;
            return kamera != null ? kamera.transform.eulerAngles.y : 0f;
        }

        private RectTransform Elementua(string izena)
        {
            RectTransform rect = _f.Elementua(izena, _eremua);
            return rect;
        }

        private int SolairuaKalkulatu(float y) => y >= SolairuMuga && _testurak.Length > 1 ? 1 : 0;

        private Vector2 MapaPuntua(Vector3 mundua)
        {
            return new Vector2(mundua.x - _mapaMin.x, mundua.z - _mapaMin.y) * _eskala + _desplazamendua;
        }

        private Texture2D TesturaMargotu(int solairua, Vector2 mapaTamaina)
        {
            int zabalera = Mathf.CeilToInt(mapaTamaina.x * PixelakMetroan);
            int altuera = Mathf.CeilToInt(mapaTamaina.y * PixelakMetroan);
            var pixelak = new Color32[zabalera * altuera];

            void Laukia(float x0, float z0, float x1, float z1, Color32 kolorea)
            {
                int px0 = Mathf.Clamp(Mathf.RoundToInt((x0 - _mapaMin.x) * PixelakMetroan), 0, zabalera);
                int px1 = Mathf.Clamp(Mathf.RoundToInt((x1 - _mapaMin.x) * PixelakMetroan), 0, zabalera);
                int pz0 = Mathf.Clamp(Mathf.RoundToInt((z0 - _mapaMin.y) * PixelakMetroan), 0, altuera);
                int pz1 = Mathf.Clamp(Mathf.RoundToInt((z1 - _mapaMin.y) * PixelakMetroan), 0, altuera);
                for (int z = pz0; z < pz1; z++)
                {
                    for (int x = px0; x < px1; x++)
                    {
                        pixelak[z * zabalera + x] = kolorea;
                    }
                }
            }

            // Sarerik gabe (edo beheko solairuan) gelen kaxak oinarri dira: mapak beti balio du orientatzeko.
            if (solairua == 0)
            {
                foreach (GelaDatua g in _datuak.Gelak)
                {
                    Laukia(g.Kaxa.min.x, g.Kaxa.min.z, g.Kaxa.max.x, g.Kaxa.max.z, GelaFondoa);
                }
            }

            if (_datuak.Erabilgarria)
            {
                NabigazioSarea sarea = _datuak.Sarea;
                const float Erdia = NabigazioEraikitzailea.Urratsa * 0.5f;
                var betetakoak = new HashSet<(int, int)>();
                for (int n = 0; n < sarea.NodoKopurua; n++)
                {
                    Vector3 p = sarea.Posizioa(n);
                    if (SolairuaKalkulatu(p.y) != solairua)
                    {
                        continue;
                    }

                    betetakoak.Add((Mathf.RoundToInt(p.x / NabigazioEraikitzailea.Urratsa), Mathf.RoundToInt(p.z / NabigazioEraikitzailea.Urratsa)));
                    int gela = _datuak.NodoGelak[n];
                    bool pasilloa = gela >= 0 && _datuak.Gelak[gela].Pasilloa;
                    Laukia(p.x - Erdia, p.z - Erdia, p.x + Erdia, p.z + Erdia, sarea.MakurtuBehar(n) ? MakurGunea : pasilloa ? PasilloIbilgarria : GelaIbilgarria);
                }

                // Ertzak: ibili ezin den gelaxka baten ondoan marra argia. Horrela hormak eta oztopoak irakurtzen dira.
                const float Lodiera = 1f / PixelakMetroan;
                foreach ((int x, int z) in betetakoak)
                {
                    float wx = x * NabigazioEraikitzailea.Urratsa;
                    float wz = z * NabigazioEraikitzailea.Urratsa;
                    if (!betetakoak.Contains((x - 1, z)))
                    {
                        Laukia(wx - Erdia, wz - Erdia, wx - Erdia + Lodiera, wz + Erdia, Ertza);
                    }

                    if (!betetakoak.Contains((x + 1, z)))
                    {
                        Laukia(wx + Erdia - Lodiera, wz - Erdia, wx + Erdia, wz + Erdia, Ertza);
                    }

                    if (!betetakoak.Contains((x, z - 1)))
                    {
                        Laukia(wx - Erdia, wz - Erdia, wx + Erdia, wz - Erdia + Lodiera, Ertza);
                    }

                    if (!betetakoak.Contains((x, z + 1)))
                    {
                        Laukia(wx - Erdia, wz + Erdia - Lodiera, wx + Erdia, wz + Erdia, Ertza);
                    }
                }
            }

            Texture2D testura = _f.Erregistratu(new Texture2D(zabalera, altuera, TextureFormat.RGBA32, false) { name = $"Mapa_{solairua}", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp });
            testura.SetPixels32(pixelak);
            testura.Apply(false, true);
            return testura;
        }

        private void EtiketakSortu()
        {
            for (int g = 0; g < _datuak.Gelak.Count; g++)
            {
                GelaDatua gela = _datuak.Gelak[g];
                if (gela.Pasilloa)
                {
                    continue;
                }

                // Gelaren goiko ertzean, ikonoak eta ibilbidea estali gabe. Gurutzeak txikiak dira: izena erdian.
                bool txikia = gela.Kaxa.size.x < 5f || gela.Kaxa.size.z < 5f;
                Vector3 lekua = txikia ? gela.Kaxa.center : new Vector3(gela.Kaxa.center.x, 0f, gela.Kaxa.max.z - 14f / _eskala);
                int solairuak = 1;
                if (_datuak.Erabilgarria && _testurak.Length > 1)
                {
                    for (int n = 0; n < _datuak.NodoGelak.Length && solairuak == 1; n++)
                    {
                        if (_datuak.NodoGelak[n] == g && _datuak.Sarea.Posizioa(n).y >= SolairuMuga)
                        {
                            solairuak = 2;
                        }
                    }
                }

                for (int s = 0; s < solairuak; s++)
                {
                    Text testua = _f.Testua($"Etiketa_{gela.Izena}", _eremua, txikia ? 15 : 18, HudKoloreak.Zuria, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                    testua.text = gela.Izena.ToUpperInvariant();
                    HudFabrika.Kokatu(testua.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), MapaPuntua(lekua), new Vector2(240f, 26f));
                    _etiketak.Add((testua, s));
                }
            }
        }

        private void IbilbideaMarraztu()
        {
            if (_datuak.Erabilgarria && _helburuak.Egoera == BideEgoera.Ondo)
            {
                _datuak.Sarea.Sinplifikatu(_helburuak.Bidea, _helburuak.BlokeoItxiak, _puntuak);
            }
            else
            {
                _puntuak.Clear();
            }

            int erabiliak = 0;
            for (int i = 1; i < _puntuak.Count; i++)
            {
                if (erabiliak == _tarteak.Count)
                {
                    Image berria = _f.Irudia("Tartea", _ibilbideGeruza, _f.Karratua, HudKoloreak.Anbarra);
                    berria.rectTransform.anchorMin = Vector2.zero;
                    berria.rectTransform.anchorMax = Vector2.zero;
                    berria.rectTransform.pivot = new Vector2(0f, 0.5f);
                    _tarteak.Add(berria);
                }

                Image tartea = _tarteak[erabiliak++];
                Vector2 a = MapaPuntua(_puntuak[i - 1]);
                Vector2 b = MapaPuntua(_puntuak[i]);
                Vector2 alde = b - a;
                bool solairuHonetan = SolairuaKalkulatu((_puntuak[i - 1].y + _puntuak[i].y) * 0.5f) == Solairua;
                tartea.gameObject.SetActive(true);
                tartea.color = solairuHonetan ? HudKoloreak.Anbarra : new Color(HudKoloreak.Anbarra.r, HudKoloreak.Anbarra.g, HudKoloreak.Anbarra.b, 0.3f);
                tartea.rectTransform.anchoredPosition = a;
                tartea.rectTransform.sizeDelta = new Vector2(alde.magnitude + 3f, 6f);
                tartea.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(alde.y, alde.x) * Mathf.Rad2Deg);
            }

            for (int i = erabiliak; i < _tarteak.Count; i++)
            {
                _tarteak[i].gameObject.SetActive(false);
            }
        }

        private void IkonoakMarraztu()
        {
            _ikonoErabiliak = 0;
            MisioLogika logika = _kudeatzailea.Logika;
            foreach (BlokeoDatua b in _datuak.Blokeoak)
            {
                if (b.Atea == null || !b.Atea.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector2 tamaina = new Vector2(b.Kaxa.size.x, b.Kaxa.size.z) * _eskala;
                Ikonoa(_f.Karratua, HudKoloreak.Gorria, b.Kaxa.center, new Vector2(Mathf.Max(tamaina.x, 6f), Mathf.Max(tamaina.y, 6f)));
                Ikonoa(_f.Gurutzea, HudKoloreak.Gorria, b.Kaxa.center, Vector2.one * 26f);
            }

            foreach (MisioBilduma b in _helburuak.BildumaAktiboak)
            {
                Ikonoa(_f.Eraztuna, HudKoloreak.Ziana, b.transform.position, Vector2.one * 18f);
            }

            Vector3? irteera = _helburuak.IrteeraPosizioa;
            if (irteera.HasValue)
            {
                Ikonoa(_f.Irteera, logika.Amaituta ? HudKoloreak.Zuria : HudKoloreak.Gorria, irteera.Value, Vector2.one * 34f);
            }

            foreach (HelburuHautagaia h in _helburuak.Hautatuak)
            {
                if (h.Mota == HelburuMota.Terminala)
                {
                    bool nagusia = _helburuak.NagusiaDago && _helburuak.Nagusia.Id == h.Id;
                    Ikonoa(_f.Erronboa, HudKoloreak.Anbarra, h.Posizioa, Vector2.one * (nagusia ? 38f : 28f));
                }
            }

            for (int i = _ikonoErabiliak; i < _ikonoak.Count; i++)
            {
                _ikonoak[i].gameObject.SetActive(false);
            }
        }

        private void Ikonoa(Sprite sprite, Color kolorea, Vector3 mundua, Vector2 tamaina)
        {
            if (_ikonoErabiliak == _ikonoak.Count)
            {
                Image berria = _f.Irudia("Ikonoa", _ikonoGeruza, sprite, kolorea);
                berria.rectTransform.anchorMin = Vector2.zero;
                berria.rectTransform.anchorMax = Vector2.zero;
                _ikonoak.Add(berria);
            }

            Image ikonoa = _ikonoak[_ikonoErabiliak++];
            kolorea.a = SolairuaKalkulatu(mundua.y) == Solairua ? 1f : 0.4f;
            ikonoa.gameObject.SetActive(true);
            ikonoa.sprite = sprite;
            ikonoa.color = kolorea;
            ikonoa.rectTransform.anchoredPosition = MapaPuntua(mundua);
            ikonoa.rectTransform.sizeDelta = tamaina;
        }

        private void AlboaIdatzi()
        {
            _solairuTestua.text = _testurak.Length > 1
                ? $"Solairua: {(Solairua == 0 ? "behea" : "goia")}   [Geziak] aldatu"
                : "Solairu bakarra";

            MisioLogika logika = _kudeatzailea.Logika;
            var testua = new StringBuilder();
            testua.Append(Nabarmendu(_kudeatzailea.MisioIzena, HudKoloreak.Ziana)).Append('\n');
            testua.Append(logika.Amaituta ? "Joan irteerara" : logika.UnekoDeskribapena).Append("\n\n");

            testua.Append(Nabarmendu("HELBURUA", HudKoloreak.Anbarra)).Append('\n');
            testua.Append(_helburuak.NagusiaDago ? _helburuak.Nagusia.Izena : "Helbururik ez").Append('\n');
            switch (_helburuak.Egoera)
            {
                case BideEgoera.Ondo:
                    testua.Append($"Ibilbidea: {_helburuak.BideLuzera:0} m\n");
                    testua.Append(BidekoGelak());
                    if (_helburuak.BideanMakurtu)
                    {
                        testua.Append('\n').Append(Nabarmendu("Ibilbidean makurtu behar da [Ctrl]", HudKoloreak.Anbarra));
                    }

                    break;
                case BideEgoera.BiderikEz:
                    testua.Append(Nabarmendu("Ez dago ibilbide ziurrik hemendik. Helburua mapan dago, baina ez da lerrorik marrazten.", HudKoloreak.Gorria));
                    break;
                default:
                    if (!_datuak.Erabilgarria)
                    {
                        testua.Append(Nabarmendu("Sarea ez dago eskuragarri: mapak gelak bakarrik erakusten ditu.", HudKoloreak.Gorria));
                    }

                    break;
            }

            testua.Append("\n\n");
            testua.Append(logika.Amaituta ? Nabarmendu("IRTEERA IREKITA", HudKoloreak.Ziana) : Nabarmendu("IRTEERA ITXITA", HudKoloreak.Gorria));
            _alboTestua.text = testua.ToString();
        }

        private string BidekoGelak()
        {
            var gelak = new List<string>();
            int aurrekoa = -1;
            bool solairuAldaketa = false;
            foreach (int n in _helburuak.Bidea)
            {
                solairuAldaketa |= SolairuaKalkulatu(_datuak.Sarea.Posizioa(n).y) != SolairuaKalkulatu(_helburuak.JokalariPosizioa.y);
                int g = _datuak.NodoGelak[n];
                if (g < 0 || g == aurrekoa || _datuak.Gelak[g].Pasilloa)
                {
                    continue;
                }

                aurrekoa = g;
                gelak.Add(_datuak.Gelak[g].Izena);
            }

            string emaitza = gelak.Count > GelaGehienez
                ? string.Join(" > ", gelak.Take(GelaGehienez)) + " > ..."
                : string.Join(" > ", gelak);
            return solairuAldaketa ? $"{emaitza}\n{Nabarmendu("Ibilbideak solairuz aldatzen du", HudKoloreak.Anbarra)}" : emaitza;
        }

        private static string Nabarmendu(string testua, Color kolorea) => $"<color=#{ColorUtility.ToHtmlStringRGB(kolorea)}><b>{testua}</b></color>";

        private void LegendaSortu(RectTransform albokoa)
        {
            (Sprite Ikonoa, Color Kolorea, string Testua, float Altuera)[] lerroak =
            {
                (_f.Gezia, HudKoloreak.Zuria, "Zu", 24f),
                (_f.Erronboa, HudKoloreak.Anbarra, "Helburu aktiboa", 24f),
                (_f.Karratua, HudKoloreak.Anbarra, "Ibilbide gomendatua", 6f),
                (_f.Karratua, MakurGunea, "Makurtzeko gunea", 18f),
                (_f.Eraztuna, HudKoloreak.Ziana, "Datu-nukleoa", 24f),
                (_f.Irteera, HudKoloreak.Zuria, "Irteera (gorria: itxita)", 24f),
                (_f.Gurutzea, HudKoloreak.Gorria, "Hesi itxia", 24f),
                (_f.Gezia, HudKoloreak.Grisa, "I: iparra", 24f),
            };

            for (int i = 0; i < lerroak.Length; i++)
            {
                float y = 360f - i * 38f;
                Image ikonoa = _f.Irudia("Legenda", albokoa, lerroak[i].Ikonoa, lerroak[i].Kolorea);
                HudFabrika.Kokatu(ikonoa.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(44f, y), new Vector2(24f, lerroak[i].Altuera));
                Text testua = _f.Testua("LegendaTestua", albokoa, 22, HudKoloreak.Grisa, TextAnchor.MiddleLeft);
                testua.text = lerroak[i].Testua;
                HudFabrika.Kokatu(testua.rectTransform, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(70f, y), new Vector2(410f, 32f));
            }

            Text itxi = _f.Testua("Itxi", albokoa, 24, HudKoloreak.Grisa, TextAnchor.MiddleLeft);
            itxi.text = "[M] / [Esc] Itxi";
            HudFabrika.Kokatu(itxi.rectTransform, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(28f, 40f), new Vector2(440f, 34f));
        }
    }
}
