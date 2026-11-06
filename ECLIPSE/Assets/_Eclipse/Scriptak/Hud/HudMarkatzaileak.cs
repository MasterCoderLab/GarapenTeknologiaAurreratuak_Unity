using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Hud
{
    /// <summary>Helburuen markatzaileak: ikonoa, izena eta distantzia. Pantailatik kanpo badaude, ertzean gezi batekin.</summary>
    internal sealed class HudMarkatzaileak
    {
        private const float SolairuAldea = 2.5f;
        private const float IzenaHurbil = 10f;
        private const float DistantziaMuga = 25f;

        private static readonly Vector2 Tartea = new Vector2(0.07f, 0.11f);

        private readonly HudFabrika _fabrika;
        private readonly RectTransform _geruza;
        private readonly List<Markatzailea> _guztiak = new List<Markatzailea>();

        public HudMarkatzaileak(HudFabrika fabrika, Transform gurasoa)
        {
            _fabrika = fabrika;
            _geruza = fabrika.Elementua("Markatzaileak", gurasoa);
            HudFabrika.Bete(_geruza);
        }

        /// <summary>Probetarako: orain ikusten diren markatzaileen kopurua.</summary>
        public int Ikusgai { get; private set; }

        public void Eguneratu(Camera kamera, HelburuKudeatzailea helburuak)
        {
            Ikusgai = 0;
            if (kamera == null)
            {
                Ezkutatu(0);
                return;
            }

            Vector2 tamaina = _geruza.rect.size;
            Vector3 jokalaria = helburuak.JokalariPosizioa;
            List<HelburuHautagaia> hautatuak = helburuak.Hautatuak;
            for (int i = 0; i < hautatuak.Count; i++)
            {
                if (i == _guztiak.Count)
                {
                    _guztiak.Add(Sortu());
                }

                HelburuHautagaia h = hautatuak[i];
                Markatzailea m = _guztiak[i];
                MarkatzaileLekua lekua = MarkatzaileKokapena.Kalkulatu(kamera.WorldToViewportPoint(h.Posizioa), Tartea);
                bool nagusia = helburuak.NagusiaDago && helburuak.Nagusia.Id == h.Id && helburuak.Nagusia.Mota == h.Mota;
                Color kolorea = HudFabrika.HelburuKolorea(h.Mota);
                float distantzia = nagusia && helburuak.Egoera == BideEgoera.Ondo ? helburuak.BideLuzera : Vector3.Distance(jokalaria, h.Posizioa);
                float altuera = h.Posizioa.y - jokalaria.y;
                string solairua = altuera > SolairuAldea ? " (goian)" : altuera < -SolairuAldea ? " (behean)" : string.Empty;

                m.Erroa.gameObject.SetActive(true);
                m.Erroa.anchoredPosition = Vector2.Scale(lekua.Posizioa, tamaina);
                m.Ikonoa.sprite = _fabrika.HelburuIkonoa(h.Mota);
                m.Ikonoa.color = kolorea;
                m.Ikonoa.rectTransform.sizeDelta = Vector2.one * (nagusia ? 34f : 24f);
                m.Gezia.gameObject.SetActive(!lekua.Barruan);
                m.Gezia.color = kolorea;
                m.Gezia.rectTransform.localEulerAngles = new Vector3(0f, 0f, lekua.Angelua);

                // Testua metro bat aldatzean bakarrik: fotograma bakoitzean ez da katerik sortzen.
                int metroak = Mathf.RoundToInt(distantzia);
                // Izena helburu nagusian edo hurbil bakarrik, eta urrunekoek ikonoa soilik: hainbat helburu pantailako puntu berean egon daitezke.
                bool izenarekin = nagusia || distantzia <= IzenaHurbil;
                if (m.Metroak != metroak || m.Id != h.Id || m.Solairua != solairua || m.Izenarekin != izenarekin)
                {
                    m.Metroak = metroak;
                    m.Id = h.Id;
                    m.Solairua = solairua;
                    m.Izenarekin = izenarekin;
                    m.Testua.text = izenarekin ? $"{h.Izena}{solairua}\n{metroak} m" : distantzia <= DistantziaMuga ? $"{metroak} m{solairua}" : string.Empty;
                }

                m.Testua.color = nagusia ? HudKoloreak.Anbarra : HudKoloreak.Zuria;
                Ikusgai++;
            }

            Ezkutatu(hautatuak.Count);
        }

        private void Ezkutatu(int hemendik)
        {
            for (int i = hemendik; i < _guztiak.Count; i++)
            {
                _guztiak[i].Erroa.gameObject.SetActive(false);
            }
        }

        private Markatzailea Sortu()
        {
            RectTransform erroa = HudFabrika.Kokatu(_fabrika.Elementua("Markatzailea", _geruza), Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            Image ikonoa = _fabrika.Irudia("Ikonoa", erroa, _fabrika.Erronboa, Color.white);
            HudFabrika.Kokatu(ikonoa.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));

            // Gezia ikonoaren inguruan biratzen da: pibota erdigunetik urrun, ertzerantz seinalatzeko.
            Image gezia = _fabrika.Irudia("Gezia", erroa, _fabrika.Gezia, Color.white);
            HudFabrika.Kokatu(gezia.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, -1.6f), Vector2.zero, new Vector2(22f, 22f));

            Text testua = _fabrika.Testua("Testua", erroa, 20, HudKoloreak.Zuria, TextAnchor.UpperCenter, FontStyle.Bold, true);
            HudFabrika.Kokatu(testua.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(300f, 54f));
            return new Markatzailea { Erroa = erroa, Ikonoa = ikonoa, Gezia = gezia, Testua = testua };
        }

        private sealed class Markatzailea
        {
            public RectTransform Erroa;
            public Image Ikonoa;
            public Image Gezia;
            public Text Testua;
            public int Metroak = -1;
            public string Id;
            public string Solairua;
            public bool Izenarekin;
        }
    }
}
