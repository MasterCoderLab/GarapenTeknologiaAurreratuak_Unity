using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Borrokaren HUD txikia: erretikula (gurutzea, ertz ilunarekin; edozein hondotan irakurgarria), jotze-markatzailea,
    /// munizioa (kargadorea barra eta zenbakiak), bizitza (barra eta zenbakia) eta kalte-distira gorria.
    /// Bere oihal propioa du; misioetan HudKudeatzailea-rekin batera bizi daiteke. Testuak euskaraz.
    /// </summary>
    public sealed class BorrokaHud : MonoBehaviour
    {
        private static readonly Color Ziana = new Color(0.55f, 0.95f, 1f, 0.95f);
        private static readonly Color Iluna = new Color(0f, 0f, 0f, 0.65f);

        private Canvas _oihala;
        private RectTransform _erretikula;
        private RectTransform _markatzailea;
        private Image[] _markatzaileZatiak;
        private Text _munizioa;
        private Image _kargadoreBarra;
        private Text _bizitzaTestua;
        private Image _bizitzaBarra;
        private Image _distira;
        private float _distiraAlfa;
        private float _markaAlfa;
        private Color _markaKolorea = Color.white;
        private float _zabalera = 10f;

        public Canvas Oihala => _oihala;

        public string MunizioTestua => _munizioa != null ? _munizioa.text : string.Empty;

        public string BizitzaTestua => _bizitzaTestua != null ? _bizitzaTestua.text : string.Empty;

        public float BizitzaBetetzea => _bizitzaBarra != null ? _bizitzaBarra.fillAmount : 0f;

        public float DistiraAlfa => _distiraAlfa;

        public float MarkaAlfa => _markaAlfa;

        /// <summary>Erretikularen tamaina pantailan (px, 1080p erreferentzia): probetarako.</summary>
        public float ErretikulaTamaina => _erretikula != null ? _erretikula.sizeDelta.x : 0f;

        public static BorrokaHud Sortu()
        {
            var objektua = new GameObject("BorrokaHud");
            var hud = objektua.AddComponent<BorrokaHud>();
            hud.Eraiki();
            return hud;
        }

        private void Eraiki()
        {
            _oihala = gameObject.AddComponent<Canvas>();
            _oihala.renderMode = RenderMode.ScreenSpaceOverlay;
            _oihala.sortingOrder = 50;
            var eskalatzailea = gameObject.AddComponent<CanvasScaler>();
            eskalatzailea.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            eskalatzailea.referenceResolution = new Vector2(1920f, 1080f);
            Font letra = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _distira = Irudia("Distira", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.6f, 0f, 0f, 0f));

            // Erretikula: lau marra eta puntua, bakoitza ertz ilunarekin (hondo argian eta ilunean ikusten da).
            _erretikula = Erdian("Erretikula", 64f);
            Gurutzea(_erretikula, Ziana, 3f, 12f, 6f, true, true);
            _markatzailea = Erdian("JotzeMarkatzailea", 64f);
            _markatzailea.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _markatzaileZatiak = Gurutzea(_markatzailea, Color.white, 3f, 10f, 9f, false, false);

            // Munizioa: behean eskuinean.
            Irudia("Munizio_Atzea", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-400f, 34f), new Vector2(360f, 72f), Iluna, new Vector2(0f, 0f));
            _munizioa = Testua("Munizioa", letra, 34, TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(-54f, 62f), new Vector2(330f, 44f));
            _munizioa.color = Ziana;
            Irudia("Kargadore_Atzea", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-388f, 42f), new Vector2(336f, 8f), new Color(0.1f, 0.2f, 0.22f, 0.9f), new Vector2(0f, 0f));
            _kargadoreBarra = Irudia("Kargadorea", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-388f, 42f), new Vector2(336f, 8f), Ziana, new Vector2(0f, 0f));
            Betegarria(_kargadoreBarra);

            // Bizitza: behean ezkerrean.
            Irudia("Bizitza_Atzea", transform, Vector2.zero, Vector2.zero, new Vector2(40f, 34f), new Vector2(380f, 72f), Iluna, Vector2.zero);
            _bizitzaTestua = Testua("BizitzaTestua", letra, 26, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(56f, 66f), new Vector2(340f, 36f));
            _bizitzaTestua.color = Ziana;
            Irudia("Bizitza_Barra_Atzea", transform, Vector2.zero, Vector2.zero, new Vector2(54f, 42f), new Vector2(352f, 14f), new Color(0.1f, 0.2f, 0.22f, 0.9f), Vector2.zero);
            _bizitzaBarra = Irudia("Bizitza", transform, Vector2.zero, Vector2.zero, new Vector2(54f, 42f), new Vector2(352f, 14f), new Color(0.2f, 0.85f, 0.95f), Vector2.zero);
            Betegarria(_bizitzaBarra);
        }

        private RectTransform Erdian(string izena, float tamaina)
        {
            var o = new GameObject(izena, typeof(RectTransform));
            o.transform.SetParent(transform, false);
            var r = (RectTransform)o.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(tamaina, tamaina);
            return r;
        }

        /// <summary>Gurutzea: lau marra erdigunetik "tartea"-ra, "luzera" luze; ertz ilun bat azpian.</summary>
        private Image[] Gurutzea(RectTransform gurasoa, Color kolorea, float lodiera, float luzera, float tartea, bool puntua, bool ertza)
        {
            var zatiak = new System.Collections.Generic.List<Image>();
            foreach (Vector2 n in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
            {
                Vector2 erdia = n * (tartea + luzera * 0.5f);
                Vector2 tamaina = n.x != 0f ? new Vector2(luzera, lodiera) : new Vector2(lodiera, luzera);
                if (ertza)
                {
                    Irudia("Ertza", gurasoa, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), erdia, tamaina + new Vector2(2f, 2f), Iluna, new Vector2(0.5f, 0.5f));
                }

                zatiak.Add(Irudia("Marra", gurasoa, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), erdia, tamaina, kolorea, new Vector2(0.5f, 0.5f)));
            }

            if (puntua)
            {
                Irudia("Puntua_Ertza", gurasoa, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f), Iluna, new Vector2(0.5f, 0.5f));
                zatiak.Add(Irudia("Puntua", gurasoa, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3f, 3f), kolorea, new Vector2(0.5f, 0.5f)));
            }

            return zatiak.ToArray();
        }

        private static void Betegarria(Image irudia)
        {
            irudia.type = Image.Type.Filled;
            irudia.fillMethod = Image.FillMethod.Horizontal;
            irudia.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
        }

        private Text Testua(string izena, Font letra, int tamaina, TextAnchor lerrokatzea, Vector2 aingura, Vector2 kokapena, Vector2 tamainaRect)
        {
            var o = new GameObject(izena, typeof(RectTransform));
            o.transform.SetParent(transform, false);
            var r = (RectTransform)o.transform;
            r.anchorMin = r.anchorMax = aingura;
            r.pivot = new Vector2(aingura.x, 0.5f);
            r.anchoredPosition = kokapena;
            r.sizeDelta = tamainaRect;
            var t = o.AddComponent<Text>();
            t.font = letra;
            t.fontSize = tamaina;
            t.alignment = lerrokatzea;
            t.raycastTarget = false;
            return t;
        }

        private Image Irudia(string izena, Transform gurasoa, Vector2 min, Vector2 max, Vector2 kokapena, Vector2 tamaina, Color kolorea, Vector2? pibota = null)
        {
            var o = new GameObject(izena, typeof(RectTransform));
            o.transform.SetParent(gurasoa, false);
            var r = (RectTransform)o.transform;
            r.anchorMin = min;
            r.anchorMax = max;
            r.pivot = pibota ?? Vector2.zero;
            r.anchoredPosition = kokapena;
            r.sizeDelta = tamaina;
            var i = o.AddComponent<Image>();
            i.color = kolorea;
            i.raycastTarget = false;
            return i;
        }

        public void Eguneratu(ArmaLogika arma, BizitzaLogika bizitza, bool apuntatzen)
        {
            if (arma != null)
            {
                _munizioa.text = arma.Birkargatzen ? "EMP  BIRKARGATZEN…" : $"EMP  {arma.Kargadorean} / {arma.Erreserba}";
                _kargadoreBarra.fillAmount = (float)arma.Kargadorean / arma.KargadoreTamaina;
                _kargadoreBarra.color = arma.Kargadorean > 2 ? Ziana : new Color(1f, 0.55f, 0.15f);
            }

            if (bizitza != null)
            {
                _bizitzaBarra.fillAmount = bizitza.Ehunekoa;
                bool kritikoa = bizitza.Ehunekoa <= 0.35f;
                _bizitzaBarra.color = kritikoa ? new Color(0.95f, 0.25f, 0.15f) : new Color(0.2f, 0.85f, 0.95f);
                _bizitzaTestua.text = $"BIZITZA  {Mathf.CeilToInt(bizitza.Unekoa)}";
                _bizitzaTestua.color = kritikoa ? new Color(1f, 0.45f, 0.35f) : Ziana;
            }

            // Apuntatzean erretikula estuagoa (zehaztasuna adierazteko).
            _zabalera = Mathf.MoveTowards(_zabalera, apuntatzen ? 4f : 10f, Time.deltaTime * 60f);
            float tamaina = 24f + _zabalera * 4f;
            _erretikula.sizeDelta = new Vector2(tamaina, tamaina);
            float eskala = tamaina / 64f;
            _erretikula.localScale = new Vector3(eskala, eskala, 1f);
        }

        /// <summary>Jotze-markatzailea: zuria jotzean, gorria hiltzean.</summary>
        public void Jo(bool hilda)
        {
            _markaAlfa = 1f;
            _markaKolorea = hilda ? new Color(1f, 0.25f, 0.2f) : Color.white;
        }

        /// <summary>Kaltea jasotzean: pantaila gorri distira laburra.</summary>
        public void Distira(float indarra)
        {
            _distiraAlfa = Mathf.Clamp01(Mathf.Max(_distiraAlfa, indarra));
        }

        private void Update()
        {
            _distiraAlfa = Mathf.MoveTowards(_distiraAlfa, 0f, Time.deltaTime * 1.5f);
            Color k = _distira.color;
            k.a = _distiraAlfa * 0.45f;
            _distira.color = k;
            _markaAlfa = Mathf.MoveTowards(_markaAlfa, 0f, Time.deltaTime * 3f);
            foreach (Image zatia in _markatzaileZatiak)
            {
                Color m = _markaKolorea;
                m.a = _markaAlfa;
                zatia.color = m;
            }
        }
    }
}
