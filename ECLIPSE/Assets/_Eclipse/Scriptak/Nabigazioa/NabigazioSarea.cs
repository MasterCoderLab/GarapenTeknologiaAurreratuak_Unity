using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eclipse.Nabigazioa
{
    /// <summary>Sareko alderdi norabidedun bat: A nodotik B nodora ibil daiteke; blokeoa itxita badago, ez.</summary>
    public readonly struct SareAlderdia
    {
        public const int BlokeorikEz = -1;

        public SareAlderdia(int nondik, int nora, int blokeoa = BlokeorikEz)
        {
            Nondik = nondik;
            Nora = nora;
            Blokeoa = blokeoa;
        }

        public int Nondik { get; }

        public int Nora { get; }

        public int Blokeoa { get; }
    }

    /// <summary>
    /// Grafo nabigagarria, Unity-ren eszenatik kanpo: nodoak ibil daitezkeen sare-puntuak dira eta alderdiak norabidedunak
    /// (erorketa batek ez du itzulera ematen). A* bilaketak blokeo itxiak saihesten ditu; ibilbiderik ez badago, ez du bat asmatzen.
    /// Makurtzea eskatzen duten nodoak garestiagoak dira: ibilbidea haietatik doa, beharrezkoa bada bakarrik.
    /// </summary>
    public sealed class NabigazioSarea
    {
        private const float MakurKostua = 2f;
        private const float AlboTartea = 0.49f;
        private const float AlboAltuera = 0.6f;

        private readonly Vector3[] _nodoak;
        private readonly bool[] _makurtu;
        private readonly int[] _hasierak;
        private readonly int[] _helmugak;
        private readonly float[] _kostuak;
        private readonly int[] _blokeoak;
        private readonly Dictionary<(int, int), List<int>> _gelaxkak = new Dictionary<(int, int), List<int>>();
        private readonly float[] _g;
        private readonly int[] _aurrekoa;
        private readonly int[] _ikusia;
        private readonly bool[] _itxita;
        private readonly List<(float F, int Nodoa)> _pila = new List<(float, int)>();
        private int _bilaketa;

        public NabigazioSarea(float urratsa, IReadOnlyList<Vector3> nodoak, IReadOnlyList<SareAlderdia> alderdiak, int blokeoKopurua = 0, IReadOnlyList<bool> makurtuBehar = null)
        {
            if (urratsa <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(urratsa));
            }

            Urratsa = urratsa;
            BlokeoKopurua = blokeoKopurua;
            _nodoak = new Vector3[nodoak.Count];
            _makurtu = new bool[nodoak.Count];
            for (int i = 0; i < nodoak.Count; i++)
            {
                _nodoak[i] = nodoak[i];
                _makurtu[i] = makurtuBehar != null && makurtuBehar[i];
                (int, int) gakoa = Gelaxka(nodoak[i]);
                if (!_gelaxkak.TryGetValue(gakoa, out List<int> zerrenda))
                {
                    zerrenda = new List<int>(1);
                    _gelaxkak[gakoa] = zerrenda;
                }

                zerrenda.Add(i);
            }

            // Bilaketa azkarrerako, nodo bakoitzaren alderdiak jarraian gordetzen dira.
            _hasierak = new int[_nodoak.Length + 1];
            for (int i = 0; i < alderdiak.Count; i++)
            {
                _hasierak[alderdiak[i].Nondik + 1]++;
            }

            for (int i = 0; i < _nodoak.Length; i++)
            {
                _hasierak[i + 1] += _hasierak[i];
            }

            _helmugak = new int[alderdiak.Count];
            _kostuak = new float[alderdiak.Count];
            _blokeoak = new int[alderdiak.Count];
            var idazteko = new int[_nodoak.Length];
            for (int i = 0; i < alderdiak.Count; i++)
            {
                SareAlderdia alderdia = alderdiak[i];
                int lekua = _hasierak[alderdia.Nondik] + idazteko[alderdia.Nondik]++;
                _helmugak[lekua] = alderdia.Nora;
                _kostuak[lekua] = Vector3.Distance(_nodoak[alderdia.Nondik], _nodoak[alderdia.Nora]) * (_makurtu[alderdia.Nora] ? MakurKostua : 1f);
                _blokeoak[lekua] = alderdia.Blokeoa;
            }

            _g = new float[_nodoak.Length];
            _aurrekoa = new int[_nodoak.Length];
            _ikusia = new int[_nodoak.Length];
            _itxita = new bool[_nodoak.Length];
        }

        public float Urratsa { get; }

        public int BlokeoKopurua { get; }

        public int NodoKopurua => _nodoak.Length;

        public int AlderdiKopurua => _helmugak.Length;

        public Vector3 Posizioa(int nodoa) => _nodoak[nodoa];

        /// <summary>Nodo honetan ezin da zutik egon: makurtuta bakarrik pasatzen da.</summary>
        public bool MakurtuBehar(int nodoa) => _makurtu[nodoa];

        /// <summary>Nodo hurbilena erradio horretan, edo -1. Altuera gehiago pisatzen du, solairuen artean nahastu ez dadin.</summary>
        public int Hurbilena(Vector3 puntua, float erradioa, float altueraPisua = 3f)
        {
            int tartea = Mathf.CeilToInt(erradioa / Urratsa);
            (int x, int z) = Gelaxka(puntua);
            int onena = -1;
            float onenaDist = erradioa * erradioa;
            for (int dx = -tartea; dx <= tartea; dx++)
            {
                for (int dz = -tartea; dz <= tartea; dz++)
                {
                    if (!_gelaxkak.TryGetValue((x + dx, z + dz), out List<int> zerrenda))
                    {
                        continue;
                    }

                    foreach (int nodoa in zerrenda)
                    {
                        Vector3 alde = _nodoak[nodoa] - puntua;
                        alde.y *= altueraPisua;
                        float dist = alde.sqrMagnitude;
                        if (dist < onenaDist)
                        {
                            onenaDist = dist;
                            onena = nodoa;
                        }
                    }
                }
            }

            return onena;
        }

        /// <summary>A*: bidea betetzen du eta true itzultzen du ibilbide bat badago. Bestela bidea hutsik geratzen da.</summary>
        public bool BideaBilatu(int hasiera, int helmuga, IReadOnlyList<bool> blokeoItxiak, List<int> bidea)
        {
            bidea.Clear();
            if (hasiera < 0 || helmuga < 0 || hasiera >= _nodoak.Length || helmuga >= _nodoak.Length)
            {
                return false;
            }

            _bilaketa++;
            _pila.Clear();
            _g[hasiera] = 0f;
            _aurrekoa[hasiera] = -1;
            _ikusia[hasiera] = _bilaketa;
            _itxita[hasiera] = false;
            PilaGehitu(Vector3.Distance(_nodoak[hasiera], _nodoak[helmuga]), hasiera);

            while (_pila.Count > 0)
            {
                int uneko = PilaAtera();
                if (_itxita[uneko])
                {
                    continue;
                }

                if (uneko == helmuga)
                {
                    for (int n = helmuga; n >= 0; n = _aurrekoa[n])
                    {
                        bidea.Add(n);
                    }

                    bidea.Reverse();
                    return true;
                }

                _itxita[uneko] = true;
                for (int a = _hasierak[uneko]; a < _hasierak[uneko + 1]; a++)
                {
                    if (AlderdiaItxita(a, blokeoItxiak))
                    {
                        continue;
                    }

                    int bestea = _helmugak[a];
                    float g = _g[uneko] + _kostuak[a];
                    if (_ikusia[bestea] == _bilaketa && g >= _g[bestea])
                    {
                        continue;
                    }

                    _ikusia[bestea] = _bilaketa;
                    _itxita[bestea] = false;
                    _g[bestea] = g;
                    _aurrekoa[bestea] = uneko;
                    PilaGehitu(g + Vector3.Distance(_nodoak[bestea], _nodoak[helmuga]), bestea);
                }
            }

            return false;
        }

        public float BideLuzera(IReadOnlyList<int> bidea)
        {
            float guztira = 0f;
            for (int i = 1; i < bidea.Count; i++)
            {
                guztira += Vector3.Distance(_nodoak[bidea[i - 1]], _nodoak[bidea[i]]);
            }

            return guztira;
        }

        /// <summary>
        /// Bidearen puntu marrazgarriak: puntu bat kentzen da haren ordezko zuzena sareko alderdi erabilgarrietatik bakarrik badoa.
        /// Horrela lerroak ez du inoiz hormarik zeharkatzen, ezta barruko hormarik ere.
        /// </summary>
        public void Sinplifikatu(IReadOnlyList<int> bidea, IReadOnlyList<bool> blokeoItxiak, List<Vector3> puntuak)
        {
            puntuak.Clear();
            if (bidea.Count == 0)
            {
                return;
            }

            int aingura = 0;
            puntuak.Add(_nodoak[bidea[0]]);
            while (aingura < bidea.Count - 1)
            {
                int azkena = aingura + 1;
                bool makurtuBidean = _makurtu[bidea[azkena]];
                for (int j = azkena + 1; j < bidea.Count; j++)
                {
                    // Atajo batek ez du makurtzea ekarri behar, jatorrizko bideak ere eskatzen ez badu.
                    makurtuBidean |= _makurtu[bidea[j]];
                    if (!LerroaLibre(bidea[aingura], bidea[j], blokeoItxiak, makurtuBidean))
                    {
                        break;
                    }

                    azkena = j;
                }

                puntuak.Add(_nodoak[bidea[azkena]]);
                aingura = azkena;
            }
        }

        /// <summary>
        /// Zuzena bi nodoen artean, gelaxkaz gelaxka: pauso bakoitzak benetako alderdi ibilgarri bat behar du,
        /// eta lerroaren bi aldeetako gelaxkek ibilgarriak izan behar dute, oztopoak ez ukitzeko.
        /// </summary>
        public bool LerroaLibre(int nondik, int nora, IReadOnlyList<bool> blokeoItxiak, bool makurtuOnartu = true)
        {
            Vector3 a = _nodoak[nondik];
            Vector3 b = _nodoak[nora];
            float luzera = new Vector2(b.x - a.x, b.z - a.z).magnitude;
            int pausoak = Mathf.Max(1, Mathf.CeilToInt(luzera / (Urratsa * 0.25f)));
            int uneko = nondik;
            (int X, int Z) unekoGelaxka = Gelaxka(a);
            Vector3 alboa = luzera > 0f ? new Vector3(b.z - a.z, 0f, a.x - b.x) / luzera * (Urratsa * AlboTartea) : Vector3.zero;
            for (int i = 1; i <= pausoak; i++)
            {
                Vector3 puntua = Vector3.Lerp(a, b, (float)i / pausoak);
                if (!Ibilgarria(puntua + alboa) || !Ibilgarria(puntua - alboa))
                {
                    return false;
                }

                (int X, int Z) gelaxka = Gelaxka(puntua);
                if (gelaxka == unekoGelaxka)
                {
                    continue;
                }

                int hurrengoa = Auzokoa(uneko, gelaxka, blokeoItxiak);
                if (hurrengoa < 0 || (!makurtuOnartu && _makurtu[hurrengoa]))
                {
                    return false;
                }

                uneko = hurrengoa;
                unekoGelaxka = gelaxka;
            }

            return uneko == nora;
        }

        /// <summary>Puntu honen gelaxkan badago nodo bat altuera berean: beste solairu bateko zoruak ez du balio.</summary>
        private bool Ibilgarria(Vector3 puntua)
        {
            if (!_gelaxkak.TryGetValue(Gelaxka(puntua), out List<int> zerrenda))
            {
                return false;
            }

            foreach (int nodoa in zerrenda)
            {
                if (Mathf.Abs(_nodoak[nodoa].y - puntua.y) <= AlboAltuera)
                {
                    return true;
                }
            }

            return false;
        }

        private int Auzokoa(int nodoa, (int X, int Z) gelaxka, IReadOnlyList<bool> blokeoItxiak)
        {
            for (int a = _hasierak[nodoa]; a < _hasierak[nodoa + 1]; a++)
            {
                if (!AlderdiaItxita(a, blokeoItxiak) && Gelaxka(_nodoak[_helmugak[a]]) == gelaxka)
                {
                    return _helmugak[a];
                }
            }

            return -1;
        }

        private bool AlderdiaItxita(int alderdia, IReadOnlyList<bool> blokeoItxiak)
        {
            int blokeoa = _blokeoak[alderdia];
            return blokeoa != SareAlderdia.BlokeorikEz && blokeoItxiak != null && blokeoa < blokeoItxiak.Count && blokeoItxiak[blokeoa];
        }

        private (int X, int Z) Gelaxka(Vector3 p) => (Mathf.RoundToInt(p.x / Urratsa), Mathf.RoundToInt(p.z / Urratsa));

        private void PilaGehitu(float f, int nodoa)
        {
            _pila.Add((f, nodoa));
            int i = _pila.Count - 1;
            while (i > 0)
            {
                int gurasoa = (i - 1) / 2;
                if (_pila[gurasoa].F <= _pila[i].F)
                {
                    break;
                }

                (_pila[gurasoa], _pila[i]) = (_pila[i], _pila[gurasoa]);
                i = gurasoa;
            }
        }

        private int PilaAtera()
        {
            int emaitza = _pila[0].Nodoa;
            int azkena = _pila.Count - 1;
            _pila[0] = _pila[azkena];
            _pila.RemoveAt(azkena);
            int i = 0;
            while (true)
            {
                int ezker = i * 2 + 1;
                int eskuin = ezker + 1;
                int txikiena = i;
                if (ezker < azkena && _pila[ezker].F < _pila[txikiena].F)
                {
                    txikiena = ezker;
                }

                if (eskuin < azkena && _pila[eskuin].F < _pila[txikiena].F)
                {
                    txikiena = eskuin;
                }

                if (txikiena == i)
                {
                    return emaitza;
                }

                (_pila[txikiena], _pila[i]) = (_pila[i], _pila[txikiena]);
                i = txikiena;
            }
        }
    }
}
