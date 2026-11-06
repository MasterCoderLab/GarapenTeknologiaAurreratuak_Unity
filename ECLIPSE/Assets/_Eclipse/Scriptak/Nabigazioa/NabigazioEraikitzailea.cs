using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Eclipse.Misioak;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Eclipse.Nabigazioa
{
    /// <summary>Gela_* edo Gela_C_Pasilloa_* baten datuak mapan erabiltzeko.</summary>
    public sealed class GelaDatua
    {
        public string Izena;
        public bool Pasilloa;
        public Bounds Kaxa;
    }

    /// <summary>Din_Blokeoa_* baten datuak. Indizea sareko alderdien blokeo-zenbakia da.</summary>
    public sealed class BlokeoDatua
    {
        public BlokeoAtea Atea;
        public Bounds Kaxa;
        public int Alderdiak;
    }

    /// <summary>Eraikuntzaren emaitza: sarea, gelak, blokeoak eta diagnostikoa. Sarea null da jokalaria ez badago zoru baten gainean.</summary>
    public sealed class NabigazioDatuak
    {
        public NabigazioSarea Sarea;
        public List<GelaDatua> Gelak = new List<GelaDatua>();
        public List<BlokeoDatua> Blokeoak = new List<BlokeoDatua>();
        public int[] NodoGelak = Array.Empty<int>();
        public List<string> Diagnostikoa = new List<string>();
        public long Milisegundoak;

        public bool Erabilgarria => Sarea != null && Sarea.NodoKopurua > 0;
    }

    /// <summary>
    /// Sarea Play-n eraikitzen du, benetako kolisioekin, BatchMailaProbatu-ren uholde bera erabiliz: 0,5 m-ko sarea,
    /// jokalariaren kapsula, 0,3 m-ko mailak eta erorketak norabide bakarrean. Hormak eta barruko hormak horrela errespetatzen dira,
    /// eta ez da konexiorik asmatzen: iristen ez dena ez dago sarean, eta diagnostikoan agertzen da.
    /// </summary>
    public static class NabigazioEraikitzailea
    {
        public const float Urratsa = 0.5f;

        private const float KapsulaErradioa = 0.3f;
        private const float KapsulaAltuera = 1.8f;
        private const float MailaIgoera = 0.3f;
        private const float OinErradioa = 0.25f;
        private const int Azpiurratsak = 5;
        private const int NodoGehienez = 60000;
        private const float AztarnaTartea = 1.6f;

        private static readonly (int X, int Z)[] Auzokoak =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        public static NabigazioDatuak Eraiki(Vector3 hasiera)
        {
            var neurgailua = Stopwatch.StartNew();
            var datuak = new NabigazioDatuak();
            GelakIrakurri(datuak);
            BlokeoakIrakurri(datuak);

            // Hesiak ez dira hormak: une batean irekiko dira. Uholdea hesirik gabe egiten da, eta gero alderdi horiek markatzen dira.
            // Fotograma berean itzultzen dira, eta fisikak ez du urratsik ematen bitartean.
            List<Collider> hesiak = datuak.Blokeoak.SelectMany(b => b.Atea.GetComponentsInChildren<Collider>()).Where(k => k.enabled && !k.isTrigger).ToList();
            var nodoak = new List<Vector3>();
            var alderdiak = new List<SareAlderdia>();
            try
            {
                hesiak.ForEach(k => k.enabled = false);
                Physics.SyncTransforms();
                Uholdea(hasiera, nodoak, alderdiak, datuak.Diagnostikoa);
            }
            finally
            {
                hesiak.ForEach(k => k.enabled = true);
                Physics.SyncTransforms();
            }

            if (nodoak.Count > 0)
            {
                BlokeoakMarkatu(datuak, nodoak, alderdiak);
                datuak.Sarea = new NabigazioSarea(Urratsa, nodoak, alderdiak, datuak.Blokeoak.Count);
                NodoakGeletanSartu(datuak);
                Diagnostikatu(datuak, alderdiak);
            }
            else
            {
                datuak.Diagnostikoa.Add("Ez da sarerik eraiki: jokalaria ez dago zoru baten gainean. Mapak gelak bakarrik erakutsiko ditu, ibilbiderik gabe.");
            }

            datuak.Milisegundoak = neurgailua.ElapsedMilliseconds;
            string laburpena = $"{SceneManager.GetActiveScene().name}: {nodoak.Count} nodo, {alderdiak.Count} alderdi, {datuak.Gelak.Count} gela, {datuak.Blokeoak.Count} blokeo, {datuak.Milisegundoak} ms, {datuak.Diagnostikoa.Count} ohar";
            if (datuak.Erabilgarria)
            {
                Debug.Log($"ECL_NABIGAZIOA_OK {laburpena}");
            }
            else
            {
                Debug.LogWarning($"ECL_NABIGAZIOA_ERROR {laburpena}");
            }

            foreach (string oharra in datuak.Diagnostikoa)
            {
                Debug.LogWarning($"ECL_NABIGAZIOA_OHARRA {oharra}");
            }

            return datuak;
        }

        private static void GelakIrakurri(NabigazioDatuak datuak)
        {
            foreach (BoxCollider kaxa in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include))
            {
                if (!kaxa.name.StartsWith("Gela_", StringComparison.Ordinal))
                {
                    continue;
                }

                // Bounds-ek kolisionatzailea aktibo behar du: zentroa eta tamaina zuzenean irakurtzen dira.
                Vector3 tamaina = Vector3.Scale(kaxa.size, kaxa.transform.lossyScale);
                bool pasilloa = kaxa.name.StartsWith("Gela_C_", StringComparison.Ordinal);
                datuak.Gelak.Add(new GelaDatua
                {
                    Izena = kaxa.name.Substring(pasilloa ? "Gela_C_".Length : "Gela_".Length),
                    Pasilloa = pasilloa,
                    Kaxa = new Bounds(kaxa.transform.TransformPoint(kaxa.center), new Vector3(Mathf.Abs(tamaina.x), Mathf.Abs(tamaina.y), Mathf.Abs(tamaina.z))),
                });
            }

            // Lehenik gelak: nodo bat bi kaxatan badago, gelarena da.
            datuak.Gelak.Sort((a, b) => a.Pasilloa == b.Pasilloa ? string.CompareOrdinal(a.Izena, b.Izena) : a.Pasilloa ? 1 : -1);
        }

        private static void BlokeoakIrakurri(NabigazioDatuak datuak)
        {
            foreach (BlokeoAtea atea in UnityEngine.Object.FindObjectsByType<BlokeoAtea>(FindObjectsInactive.Exclude).OrderBy(a => a.name, StringComparer.Ordinal))
            {
                Collider kaxa = atea.GetComponent<Collider>();
                if (kaxa == null)
                {
                    datuak.Diagnostikoa.Add($"{atea.name}: ez du kolisionatzailerik; ezin da mapan kokatu.");
                    continue;
                }

                datuak.Blokeoak.Add(new BlokeoDatua { Atea = atea, Kaxa = kaxa.bounds });
            }
        }

        private static bool KapsulaLibre(Vector3 oinak)
        {
            return !Physics.CheckCapsule(
                    oinak + Vector3.up * (MailaIgoera + KapsulaErradioa + 0.02f),
                    oinak + Vector3.up * (KapsulaAltuera - KapsulaErradioa + 0.05f),
                    KapsulaErradioa, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !Physics.CheckBox(
                    oinak + Vector3.up * (MailaIgoera + 0.06f),
                    new Vector3(0.3f, 0.04f, 0.3f),
                    Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Jokalariak hasieratik irits daitekeen guztia. BatchMailaProbatu-k eta robotak balioztatutako algoritmo bera.</summary>
        private static void Uholdea(Vector3 hasiera, List<Vector3> nodoak, List<SareAlderdia> alderdiak, List<string> diagnostikoa)
        {
            if (!Physics.Raycast(hasiera + Vector3.up * 0.5f, Vector3.down, out RaycastHit lehena, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var ikusiak = new Dictionary<(int, int, int), int>();
            var ilara = new Queue<int>();
            var gelaxkak = new List<(int X, int Z)>();

            int Gehitu(int x, int z, float y)
            {
                nodoak.Add(new Vector3(x * Urratsa, y, z * Urratsa));
                gelaxkak.Add((x, z));
                ilara.Enqueue(nodoak.Count - 1);
                return nodoak.Count - 1;
            }

            int erroX = Mathf.RoundToInt(hasiera.x / Urratsa);
            int erroZ = Mathf.RoundToInt(hasiera.z / Urratsa);
            ikusiak[(erroX, erroZ, Mathf.RoundToInt(lehena.point.y * 10f))] = Gehitu(erroX, erroZ, lehena.point.y);

            while (ilara.Count > 0)
            {
                if (nodoak.Count >= NodoGehienez)
                {
                    diagnostikoa.Add($"Sarea {NodoGehienez} nodotan moztu da: maila osoa ez dago sarean.");
                    return;
                }

                int u = ilara.Dequeue();
                Vector3 up = nodoak[u];
                (int ux, int uz) = gelaxkak[u];
                foreach ((int dx, int dz) in Auzokoak)
                {
                    int x = ux + dx;
                    int z = uz + dz;
                    var goian = new Vector3(x * Urratsa, up.y, z * Urratsa);

                    // Azpiurratsetan ibili: mailadi bateko maila bakoitza banan-banan igo behar da, ez sarearen jauzi osoan.
                    float y = up.y;
                    bool ondo = true;
                    for (int k = 1; k <= Azpiurratsak && ondo; k++)
                    {
                        Vector3 p = Vector3.Lerp(up, goian, (float)k / Azpiurratsak);
                        p.y = y;
                        if (!Physics.SphereCast(p + Vector3.up * (MailaIgoera + 0.05f + OinErradioa), OinErradioa, Vector3.down, out RaycastHit jo, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                            || jo.point.y - y > MailaIgoera)
                        {
                            ondo = false;
                            break;
                        }

                        y = jo.point.y;
                        if (k % 2 == 1 && !KapsulaLibre(new Vector3(p.x, y, p.z)))
                        {
                            ondo = false;
                        }
                    }

                    if (!ondo || !KapsulaLibre(new Vector3(goian.x, y, goian.z)))
                    {
                        continue;
                    }

                    (int, int, int) gakoa = (x, z, Mathf.RoundToInt(y * 10f));
                    if (!ikusiak.TryGetValue(gakoa, out int bestea))
                    {
                        bestea = Gehitu(x, z, y);
                        ikusiak[gakoa] = bestea;
                    }

                    alderdiak.Add(new SareAlderdia(u, bestea));
                }
            }
        }

        /// <summary>Hesi baten kaxa (kapsularen erradioz handituta) zeharkatzen duen alderdi oro hesi horren menpe geratzen da.</summary>
        private static void BlokeoakMarkatu(NabigazioDatuak datuak, List<Vector3> nodoak, List<SareAlderdia> alderdiak)
        {
            for (int b = 0; b < datuak.Blokeoak.Count; b++)
            {
                BlokeoDatua blokeoa = datuak.Blokeoak[b];
                Bounds kaxa = blokeoa.Kaxa;
                kaxa.Expand(new Vector3(KapsulaErradioa * 2f, 0f, KapsulaErradioa * 2f));
                for (int i = 0; i < alderdiak.Count; i++)
                {
                    SareAlderdia a = alderdiak[i];
                    Vector3 p0 = nodoak[a.Nondik] + Vector3.up;
                    Vector3 p1 = nodoak[a.Nora] + Vector3.up;
                    Vector3 alde = p1 - p0;
                    if (kaxa.Contains(p0) || kaxa.Contains(p1)
                        || (kaxa.IntersectRay(new Ray(p0, alde), out float d) && d <= alde.magnitude))
                    {
                        alderdiak[i] = new SareAlderdia(a.Nondik, a.Nora, b);
                        blokeoa.Alderdiak++;
                    }
                }
            }
        }

        private static void NodoakGeletanSartu(NabigazioDatuak datuak)
        {
            NabigazioSarea sarea = datuak.Sarea;
            datuak.NodoGelak = new int[sarea.NodoKopurua];
            var kaxak = datuak.Gelak.Select(g => g.Kaxa).ToArray();
            int kanpoan = 0;
            Vector3 lehenKanpoan = Vector3.zero;
            for (int n = 0; n < sarea.NodoKopurua; n++)
            {
                Vector3 p = sarea.Posizioa(n);
                int aurkitua = -1;
                float onena = AztarnaTartea;
                for (int g = 0; g < kaxak.Length; g++)
                {
                    if (p.y < kaxak[g].min.y - 0.5f || p.y > kaxak[g].max.y)
                    {
                        continue;
                    }

                    float dx = Mathf.Max(kaxak[g].min.x - p.x, 0f, p.x - kaxak[g].max.x);
                    float dz = Mathf.Max(kaxak[g].min.z - p.z, 0f, p.z - kaxak[g].max.z);
                    float dist = Mathf.Max(dx, dz);
                    if (dist <= 0f)
                    {
                        aurkitua = g;
                        break;
                    }

                    if (dist < onena)
                    {
                        onena = dist;
                        aurkitua = g;
                    }
                }

                datuak.NodoGelak[n] = aurkitua;
                if (aurkitua < 0)
                {
                    if (kanpoan == 0)
                    {
                        lehenKanpoan = p;
                    }

                    kanpoan++;
                }
            }

            if (kanpoan > 0)
            {
                datuak.Diagnostikoa.Add($"{kanpoan} nodo daude gela guztietatik kanpo (lehena {lehenKanpoan}).");
            }
        }

        /// <summary>Izenek diotena eta sareak aurkitu duena alderatu: Gela_C_Pasilloa_A_B-k A eta B lotu behar ditu benetan.</summary>
        private static void Diagnostikatu(NabigazioDatuak datuak, List<SareAlderdia> alderdiak)
        {
            var nodoKopurua = new int[datuak.Gelak.Count];
            foreach (int g in datuak.NodoGelak)
            {
                if (g >= 0)
                {
                    nodoKopurua[g]++;
                }
            }

            for (int g = 0; g < datuak.Gelak.Count; g++)
            {
                if (nodoKopurua[g] == 0)
                {
                    datuak.Diagnostikoa.Add($"{datuak.Gelak[g].Izena}: ez du nodo eskuragarririk; ez dago ibilbiderik hara.");
                }
            }

            var loturak = new HashSet<(int, int)>();
            foreach (SareAlderdia a in alderdiak)
            {
                int g0 = datuak.NodoGelak[a.Nondik];
                int g1 = datuak.NodoGelak[a.Nora];
                if (g0 >= 0 && g1 >= 0 && g0 != g1)
                {
                    loturak.Add((g0, g1));
                }
            }

            var gelaIndizeak = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int g = 0; g < datuak.Gelak.Count; g++)
            {
                if (!datuak.Gelak[g].Pasilloa)
                {
                    gelaIndizeak[datuak.Gelak[g].Izena] = g;
                }
            }

            const string Aurrizkia = "Pasilloa_";
            for (int g = 0; g < datuak.Gelak.Count; g++)
            {
                GelaDatua gela = datuak.Gelak[g];
                if (!gela.Pasilloa || !gela.Izena.StartsWith(Aurrizkia, StringComparison.Ordinal))
                {
                    continue;
                }

                // Gela-izenek ez dute azpimarrarik: A lehen zatia da, eta B bigarrena, "itsua" ez bada.
                string[] zatiak = gela.Izena.Substring(Aurrizkia.Length).Split('_');
                foreach (string izena in zatiak.Take(zatiak.Length > 1 && zatiak[1] == "itsua" ? 1 : 2))
                {
                    if (!gelaIndizeak.TryGetValue(izena, out int bestea))
                    {
                        datuak.Diagnostikoa.Add($"{gela.Izena}: '{izena}' gela ez da aurkitu; lotura hau ez da egiaztatu.");
                    }
                    else if (!loturak.Contains((g, bestea)) && !loturak.Contains((bestea, g)))
                    {
                        datuak.Diagnostikoa.Add($"{gela.Izena}: ez du {izena} gelarekin lotura ibilgarririk sarean.");
                    }
                }
            }

            foreach (BlokeoDatua blokeoa in datuak.Blokeoak.Where(b => b.Alderdiak == 0))
            {
                datuak.Diagnostikoa.Add($"{blokeoa.Atea.name}: ez du alderdirik mozten; mapan agertuko da, baina ibilbideak ez du kontuan hartuko.");
            }
        }
    }
}
