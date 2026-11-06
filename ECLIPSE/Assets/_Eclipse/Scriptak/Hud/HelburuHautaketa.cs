using System.Collections.Generic;
using System.Linq;
using Eclipse.Misioak;
using UnityEngine;

namespace Eclipse.Hud
{
    public enum HelburuMota
    {
        Terminala,
        Bilduma,
        Blokeoa,
        Irteera,
    }

    /// <summary>Markatzaile bat izan dezakeen eszenako puntua. Aktibo: bildu gabe edo itxita.</summary>
    public readonly struct HelburuHautagaia
    {
        public HelburuHautagaia(HelburuMota mota, string id, string izena, Vector3 posizioa, bool aktibo = true)
        {
            Mota = mota;
            Id = id;
            Izena = izena;
            Posizioa = posizioa;
            Aktibo = aktibo;
        }

        public HelburuMota Mota { get; }

        public string Id { get; }

        public string Izena { get; }

        public Vector3 Posizioa { get; }

        public bool Aktibo { get; }
    }

    /// <summary>
    /// Zer erakutsi une bakoitzean, Unity-ko objekturik gabe: uneko faseko terminalak, irteera misioa amaitzean,
    /// eta datu-nukleo eta hesi hurbilak bakarrik, pantaila ez betetzeko.
    /// </summary>
    public static class HelburuHautaketa
    {
        public const float BildumaMuga = 25f;
        public const float BlokeoMuga = 15f;
        public const int BildumaGehienez = 2;

        public static List<HelburuHautagaia> Hautatu(MisioLogika logika, Vector3 jokalaria, IEnumerable<HelburuHautagaia> hautagaiak)
        {
            var emaitza = new List<HelburuHautagaia>();
            var bildumak = new List<HelburuHautagaia>();
            foreach (HelburuHautagaia h in hautagaiak)
            {
                float distantzia = Vector3.Distance(h.Posizioa, jokalaria);
                switch (h.Mota)
                {
                    case HelburuMota.Terminala when logika.Aktibo(h.Id):
                    case HelburuMota.Irteera when logika.Amaituta:
                    case HelburuMota.Blokeoa when h.Aktibo && distantzia <= BlokeoMuga:
                        emaitza.Add(h);
                        break;
                    case HelburuMota.Bilduma when h.Aktibo && distantzia <= BildumaMuga:
                        bildumak.Add(h);
                        break;
                }
            }

            emaitza.AddRange(bildumak.OrderBy(h => Vector3.Distance(h.Posizioa, jokalaria)).Take(BildumaGehienez));
            return emaitza;
        }
    }

    /// <summary>Markatzailearen lekua pantailan: barruan puntuaren gainean; bestela ertzean, helburuaren norabidean.</summary>
    public readonly struct MarkatzaileLekua
    {
        public MarkatzaileLekua(Vector2 posizioa, bool barruan, float angelua)
        {
            Posizioa = posizioa;
            Barruan = barruan;
            Angelua = angelua;
        }

        /// <summary>Viewport koordenatuak (0-1).</summary>
        public Vector2 Posizioa { get; }

        public bool Barruan { get; }

        /// <summary>Gezia pantailaren ertzean: graduak, 0 gora eta erlojuaren kontra.</summary>
        public float Angelua { get; }
    }

    public static class MarkatzaileKokapena
    {
        /// <param name="viewport">Camera.WorldToViewportPoint-en emaitza; z &lt; 0 kameraren atzean dago.</param>
        /// <param name="tartea">Ertzetik utzi beharreko tartea, viewport unitateetan.</param>
        public static MarkatzaileLekua Kalkulatu(Vector3 viewport, Vector2 tartea)
        {
            bool atzean = viewport.z < 0f;
            var norabidea = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            if (!atzean
                && viewport.x >= tartea.x && viewport.x <= 1f - tartea.x
                && viewport.y >= tartea.y && viewport.y <= 1f - tartea.y)
            {
                return new MarkatzaileLekua(new Vector2(viewport.x, viewport.y), true, 0f);
            }

            // Kameraren atzean proiekzioa ispilatuta dago: norabidea alderantzikatu.
            if (atzean)
            {
                norabidea = -norabidea;
            }

            // Zehatz-mehatz atzean edo aurrean badago, ez dago norabiderik: behera seinalatu (buelta eman).
            if (norabidea.sqrMagnitude < 1e-6f)
            {
                norabidea = Vector2.down;
            }

            float eskalaX = (0.5f - tartea.x) / Mathf.Max(Mathf.Abs(norabidea.x), 1e-5f);
            float eskalaY = (0.5f - tartea.y) / Mathf.Max(Mathf.Abs(norabidea.y), 1e-5f);
            Vector2 ertza = norabidea * Mathf.Min(eskalaX, eskalaY) + new Vector2(0.5f, 0.5f);
            return new MarkatzaileLekua(ertza, false, Mathf.Atan2(norabidea.x, norabidea.y) * -Mathf.Rad2Deg);
        }
    }
}
