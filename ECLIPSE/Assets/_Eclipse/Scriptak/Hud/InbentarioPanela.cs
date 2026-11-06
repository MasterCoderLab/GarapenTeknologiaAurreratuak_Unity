using System.Collections.Generic;
using Eclipse.Inbentarioak;
using Eclipse.Misioak;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Hud
{
    /// <summary>Inbentarioaren bista (Tab) eta jokoko bizkor-barra. Araurik ez: egoera guztia Inbentarioa eta GameSession-ena da.</summary>
    internal sealed class InbentarioPanela
    {
        private readonly GameSession _saioa;
        private readonly LevelManager _kudeatzailea;
        private readonly RectTransform _erroa;
        private readonly List<RanuraBista> _bizkorrak = new List<RanuraBista>();
        private readonly List<RanuraBista> _motxila = new List<RanuraBista>();
        private readonly List<RanuraBista> _barra = new List<RanuraBista>();
        private readonly Text _nukleoak;
        private int _bertsioa = -1;
        private int _nukleoKopurua = -1;
        private int _misioNukleoak = -1;

        public InbentarioPanela(HudFabrika f, Transform gurasoa, Transform jokoGeruza, GameSession saioa, LevelManager kudeatzailea)
        {
            _saioa = saioa;
            _kudeatzailea = kudeatzailea;

            Image estalkia = f.Irudia("Inbentarioa", gurasoa, f.Karratua, HudKoloreak.FondoIluna);
            _erroa = estalkia.rectTransform;
            HudFabrika.Bete(_erroa);

            RectTransform kaxa = f.Panela("Kaxa", _erroa, HudKoloreak.Ziana);
            HudFabrika.Kokatu(kaxa, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 800f));

            Text izenburua = f.Testua("Izenburua", kaxa, 40, HudKoloreak.Ziana, TextAnchor.UpperLeft, FontStyle.Bold);
            izenburua.text = "INBENTARIOA";
            HudFabrika.Kokatu(izenburua.rectTransform, Vector2.up, Vector2.up, new Vector2(40f, -28f), new Vector2(600f, 54f));

            Text itxi = f.Testua("Itxi", kaxa, 24, HudKoloreak.Grisa, TextAnchor.UpperRight);
            itxi.text = "[Tab] / [Esc] Itxi";
            HudFabrika.Kokatu(itxi.rectTransform, Vector2.one, Vector2.one, new Vector2(-40f, -38f), new Vector2(400f, 34f));

            Atala(f, kaxa, "BIZKOR-SARBIDEAK", "Armak eta ekipoa. Hautatu 1-4 teklekin.", -100f);
            for (int i = 0; i < Inbentarioa.BizkorKopurua; i++)
            {
                _bizkorrak.Add(Ranura(f, kaxa, Vector2.up, new Vector2(40f + i * 180f, -180f), new Vector2(165f, 130f), (i + 1).ToString(), 22, "Hutsik"));
            }

            Atala(f, kaxa, "MOTXILA", "Objektuak eta kontsumigarriak.", -330f);
            const int Zutabeak = 6;
            for (int i = 0; i < Inbentarioa.MotxilaKopurua; i++)
            {
                _motxila.Add(Ranura(f, kaxa, Vector2.up, new Vector2(40f + i % Zutabeak * 174f, -410f - i / Zutabeak * 130f), new Vector2(160f, 118f), null, 20, "Hutsik"));
            }

            _nukleoak = f.Testua("DatuNukleoak", kaxa, 28, HudKoloreak.Zuria, TextAnchor.UpperLeft, FontStyle.Bold);
            HudFabrika.Kokatu(_nukleoak.rectTransform, Vector2.up, Vector2.up, new Vector2(40f, -690f), new Vector2(1040f, 80f));

            RectTransform barra = HudFabrika.Kokatu(f.Elementua("BizkorBarra", jokoGeruza), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(4f * 78f, 66f));
            for (int i = 0; i < Inbentarioa.BizkorKopurua; i++)
            {
                _barra.Add(Ranura(f, barra, Vector2.up, new Vector2(i * 78f, 0f), new Vector2(72f, 66f), (i + 1).ToString(), 16, "-"));
            }

            Erakutsi(false);
        }

        public bool Irekita => _erroa.gameObject.activeSelf;

        public void Erakutsi(bool ikusgai)
        {
            _erroa.gameObject.SetActive(ikusgai);
            _bertsioa = -1;
        }

        public void Eguneratu()
        {
            Inbentarioa inbentarioa = _saioa.Inbentarioa;
            if (_bertsioa == inbentarioa.Bertsioa && _nukleoKopurua == _saioa.DatuNukleoak && _misioNukleoak == _kudeatzailea.Bildumak)
            {
                return;
            }

            _bertsioa = inbentarioa.Bertsioa;
            _nukleoKopurua = _saioa.DatuNukleoak;
            _misioNukleoak = _kudeatzailea.Bildumak;
            for (int i = 0; i < Inbentarioa.BizkorKopurua; i++)
            {
                _bizkorrak[i].Erakutsi(inbentarioa.Bizkorrak[i], inbentarioa.Hautatua == i);
                _barra[i].Erakutsi(inbentarioa.Bizkorrak[i], inbentarioa.Hautatua == i);
            }

            for (int i = 0; i < Inbentarioa.MotxilaKopurua; i++)
            {
                _motxila[i].Erakutsi(inbentarioa.Motxila[i], false);
            }

            _nukleoak.text = _kudeatzailea.BildumakGuztira > 0
                ? $"Datu-nukleoak: {_misioNukleoak} / {_kudeatzailea.BildumakGuztira} misio honetan  |  {_nukleoKopurua} guztira"
                : $"Datu-nukleoak: {_nukleoKopurua} guztira";
        }

        private static void Atala(HudFabrika f, RectTransform kaxa, string izena, string azalpena, float y)
        {
            Text izenburua = f.Testua(izena, kaxa, 24, HudKoloreak.Anbarra, TextAnchor.UpperLeft, FontStyle.Bold);
            izenburua.text = izena;
            HudFabrika.Kokatu(izenburua.rectTransform, Vector2.up, Vector2.up, new Vector2(40f, y), new Vector2(1040f, 32f));
            Text testua = f.Testua($"{izena}_azalpena", kaxa, 20, HudKoloreak.Grisa);
            testua.text = azalpena;
            HudFabrika.Kokatu(testua.rectTransform, Vector2.up, Vector2.up, new Vector2(40f, y - 32f), new Vector2(1040f, 30f));
        }

        private static RanuraBista Ranura(HudFabrika f, RectTransform gurasoa, Vector2 aingura, Vector2 pos, Vector2 tamaina, string zenbakia, int letra, string hutsik)
        {
            Image fondoa = f.Irudia("Ranura", gurasoa, f.Karratua, HudKoloreak.Ranura);
            HudFabrika.Kokatu(fondoa.rectTransform, aingura, Vector2.up, pos, tamaina);
            if (zenbakia != null)
            {
                Text zenbakiTestua = f.Testua("Zenbakia", fondoa.transform, letra, HudKoloreak.Ziana, TextAnchor.UpperLeft, FontStyle.Bold);
                zenbakiTestua.text = zenbakia;
                HudFabrika.Kokatu(zenbakiTestua.rectTransform, Vector2.up, Vector2.up, new Vector2(8f, -4f), new Vector2(40f, letra + 8f));
            }

            Text izena = f.Testua("Izena", fondoa.transform, letra, HudKoloreak.Grisa, TextAnchor.MiddleCenter);
            RectTransform izenaRect = izena.rectTransform;
            HudFabrika.Bete(izenaRect);
            izenaRect.offsetMin = new Vector2(6f, 4f);
            izenaRect.offsetMax = new Vector2(-6f, -4f);
            return new RanuraBista(fondoa, izena, hutsik);
        }

        private sealed class RanuraBista
        {
            private readonly Image _fondoa;
            private readonly Text _izena;
            private readonly string _hutsik;

            public RanuraBista(Image fondoa, Text izena, string hutsik)
            {
                _hutsik = hutsik;
                _fondoa = fondoa;
                _izena = izena;
            }

            public void Erakutsi(InbentarioRanura ranura, bool hautatua)
            {
                _fondoa.color = hautatua ? HudKoloreak.ZianIluna : HudKoloreak.Ranura;
                _izena.color = ranura.Hutsik ? HudKoloreak.Grisa : HudKoloreak.Zuria;
                _izena.text = ranura.Hutsik ? _hutsik : ranura.Kopurua > 1 ? $"{ranura.Elementua.Izena}\nx{ranura.Kopurua}" : ranura.Elementua.Izena;
            }
        }
    }
}
