using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Eclipse.Hud
{
    /// <summary>ECLIPSE-ren paleta: laborategi futurista iluna. Zian informazioa, anbar helburua, gorri alerta.</summary>
    internal static class HudKoloreak
    {
        public static readonly Color Fondoa = new Color(0.03f, 0.06f, 0.09f, 0.82f);
        public static readonly Color FondoIluna = new Color(0.01f, 0.03f, 0.05f, 0.93f);
        public static readonly Color FondoOsoa = new Color(0.01f, 0.03f, 0.05f, 1f);
        public static readonly Color Ranura = new Color(0.08f, 0.15f, 0.2f, 0.9f);
        public static readonly Color Ziana = new Color(0.25f, 0.85f, 1f);
        public static readonly Color ZianIluna = new Color(0.12f, 0.35f, 0.45f);
        public static readonly Color Zuria = new Color(0.93f, 0.97f, 1f);
        public static readonly Color Grisa = new Color(0.6f, 0.7f, 0.76f);
        public static readonly Color Anbarra = new Color(1f, 0.72f, 0.2f);
        public static readonly Color Gorria = new Color(1f, 0.28f, 0.22f);
    }

    /// <summary>uGUI elementuak kodez sortzen ditu, kanpoko baliabiderik gabe: LegacyRuntime letra-tipoa eta sortutako ikonoak. TMP-k Essential Resources inportatzea eskatzen du.</summary>
    internal sealed class HudFabrika : IDisposable
    {
        private const int IkonoTamaina = 64;

        private readonly List<Object> _sortuak = new List<Object>();

        public HudFabrika()
        {
            Letra = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Karratua = Ikonoa("Karratua", _ => -1f);
            Zirkulua = Ikonoa("Zirkulua", p => p.magnitude - 0.9f);
            Eraztuna = Ikonoa("Eraztuna", p => Mathf.Abs(p.magnitude - 0.7f) - 0.2f);
            Erronboa = Ikonoa("Erronboa", p => (Mathf.Abs(p.x) + Mathf.Abs(p.y) - 0.9f) * 0.707f);
            Gezia = Ikonoa("Gezia", p => Mathf.Max(
                Poligonoa(p, new Vector2(0f, 0.92f), new Vector2(-0.72f, -0.85f), new Vector2(0.72f, -0.85f)),
                -Poligonoa(p, new Vector2(0f, -0.35f), new Vector2(-0.8f, -1.3f), new Vector2(0.8f, -1.3f))));
            Gurutzea = Ikonoa("Gurutzea", p =>
            {
                float a = (p.x + p.y) * 0.707f;
                float b = (p.x - p.y) * 0.707f;
                return Mathf.Min(Mathf.Max(Mathf.Abs(a) - 0.2f, Mathf.Abs(b) - 0.88f), Mathf.Max(Mathf.Abs(b) - 0.2f, Mathf.Abs(a) - 0.88f));
            });
            Irteera = Ikonoa("Irteera", p => Mathf.Min(
                Mathf.Abs(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - 0.8f) - 0.1f,
                Poligonoa(p, new Vector2(0.5f, 0f), new Vector2(-0.3f, 0.45f), new Vector2(-0.3f, -0.45f))));
        }

        public Font Letra { get; }

        public Sprite Karratua { get; }

        public Sprite Zirkulua { get; }

        public Sprite Eraztuna { get; }

        public Sprite Erronboa { get; }

        public Sprite Gezia { get; }

        public Sprite Gurutzea { get; }

        public Sprite Irteera { get; }

        public Sprite HelburuIkonoa(HelburuMota mota)
        {
            switch (mota)
            {
                case HelburuMota.Bilduma: return Eraztuna;
                case HelburuMota.Blokeoa: return Gurutzea;
                case HelburuMota.Irteera: return Irteera;
                default: return Erronboa;
            }
        }

        public static Color HelburuKolorea(HelburuMota mota)
        {
            switch (mota)
            {
                case HelburuMota.Bilduma: return HudKoloreak.Ziana;
                case HelburuMota.Blokeoa: return HudKoloreak.Gorria;
                case HelburuMota.Irteera: return HudKoloreak.Zuria;
                default: return HudKoloreak.Anbarra;
            }
        }

        public RectTransform Elementua(string izena, Transform gurasoa)
        {
            var objektua = new GameObject(izena, typeof(RectTransform));
            objektua.transform.SetParent(gurasoa, false);
            return (RectTransform)objektua.transform;
        }

        /// <summary>Aingura bakarra: pos aingurarekiko da eta tamaina pixeletan (1920×1080 erreferentzian).</summary>
        public static RectTransform Kokatu(RectTransform rect, Vector2 aingura, Vector2 pibota, Vector2 pos, Vector2 tamaina)
        {
            rect.anchorMin = aingura;
            rect.anchorMax = aingura;
            rect.pivot = pibota;
            rect.anchoredPosition = pos;
            rect.sizeDelta = tamaina;
            return rect;
        }

        public static void Bete(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public Image Irudia(string izena, Transform gurasoa, Sprite sprite, Color kolorea)
        {
            RectTransform rect = Elementua(izena, gurasoa);
            var irudia = rect.gameObject.AddComponent<Image>();
            irudia.sprite = sprite;
            irudia.color = kolorea;
            irudia.raycastTarget = false;
            return irudia;
        }

        public Text Testua(string izena, Transform gurasoa, int tamaina, Color kolorea, TextAnchor lerrokatzea = TextAnchor.UpperLeft, FontStyle estiloa = FontStyle.Normal, bool itzala = false)
        {
            RectTransform rect = Elementua(izena, gurasoa);
            var testua = rect.gameObject.AddComponent<Text>();
            testua.font = Letra;
            testua.fontSize = tamaina;
            testua.fontStyle = estiloa;
            testua.color = kolorea;
            testua.alignment = lerrokatzea;
            testua.horizontalOverflow = HorizontalWrapMode.Wrap;
            testua.verticalOverflow = VerticalWrapMode.Overflow;
            testua.raycastTarget = false;
            if (itzala)
            {
                var efektua = rect.gameObject.AddComponent<Shadow>();
                efektua.effectColor = new Color(0f, 0f, 0f, 0.9f);
                efektua.effectDistance = new Vector2(1.5f, -1.5f);
            }

            return testua;
        }

        /// <summary>Atzealde iluna eta ezkerreko marra argia, HUD osoan berdin.</summary>
        public RectTransform Panela(string izena, Transform gurasoa, Color marra)
        {
            Image fondoa = Irudia(izena, gurasoa, Karratua, HudKoloreak.Fondoa);
            Image marraIrudia = Irudia("Marra", fondoa.transform, Karratua, marra);
            RectTransform marraRect = marraIrudia.rectTransform;
            marraRect.anchorMin = Vector2.zero;
            marraRect.anchorMax = Vector2.up;
            marraRect.pivot = Vector2.zero;
            marraRect.anchoredPosition = Vector2.zero;
            marraRect.sizeDelta = new Vector2(5f, 0f);
            return fondoa.rectTransform;
        }

        /// <summary>Destroy-rako erregistratu: testurak ez dira automatikoki askatzen.</summary>
        public T Erregistratu<T>(T objektua) where T : Object
        {
            _sortuak.Add(objektua);
            return objektua;
        }

        public void Dispose()
        {
            foreach (Object objektua in _sortuak)
            {
                if (objektua != null)
                {
                    Object.Destroy(objektua);
                }
            }

            _sortuak.Clear();
        }

        /// <summary>Poligono ganbilaren distantzia hurbildua; erpinak erlojuaren kontrako ordenan.</summary>
        private static float Poligonoa(Vector2 p, params Vector2[] erpinak)
        {
            float emaitza = float.MinValue;
            for (int i = 0; i < erpinak.Length; i++)
            {
                Vector2 a = erpinak[i];
                Vector2 b = erpinak[(i + 1) % erpinak.Length];
                Vector2 norabidea = (b - a).normalized;
                emaitza = Mathf.Max(emaitza, norabidea.y * (p.x - a.x) - norabidea.x * (p.y - a.y));
            }

            return emaitza;
        }

        private Sprite Ikonoa(string izena, Func<Vector2, float> distantzia)
        {
            var testura = new Texture2D(IkonoTamaina, IkonoTamaina, TextureFormat.RGBA32, false) { name = izena, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixelak = new Color32[IkonoTamaina * IkonoTamaina];
            for (int y = 0; y < IkonoTamaina; y++)
            {
                for (int x = 0; x < IkonoTamaina; x++)
                {
                    var p = new Vector2((x + 0.5f) / IkonoTamaina * 2f - 1f, (y + 0.5f) / IkonoTamaina * 2f - 1f);
                    float alfa = Mathf.Clamp01(0.5f - distantzia(p) * IkonoTamaina * 0.5f);
                    pixelak[y * IkonoTamaina + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
                }
            }

            testura.SetPixels32(pixelak);
            testura.Apply(false, true);
            _sortuak.Add(testura);
            return Erregistratu(Sprite.Create(testura, new Rect(0f, 0f, IkonoTamaina, IkonoTamaina), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect));
        }
    }
}
