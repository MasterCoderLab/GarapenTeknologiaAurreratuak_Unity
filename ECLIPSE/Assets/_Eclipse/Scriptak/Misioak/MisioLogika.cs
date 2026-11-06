using System;
using System.Collections.Generic;
using System.Linq;

namespace Eclipse.Misioak
{
    /// <summary>Helburu bat betetzen saiatzearen emaitza esplizitua.</summary>
    public enum BeteEmaitza
    {
        Onartua,
        JadaBeteta,
        EzDagokio,
        Ezezaguna,
    }

    /// <summary>Misioaren fase bat: testua eta bete beharreko helburuen id-ak.</summary>
    public sealed class MisioFasea
    {
        public MisioFasea(string deskribapena, IEnumerable<string> helburuak)
        {
            Deskribapena = deskribapena ?? string.Empty;
            Helburuak = helburuak?.ToArray() ?? Array.Empty<string>();
        }

        public string Deskribapena { get; }

        public IReadOnlyList<string> Helburuak { get; }
    }

    /// <summary>
    /// Misioaren arauak, Unity-tik kanpo: faseak ordenan betetzen dira, fase bateko helburuak edozein ordenatan.
    /// Id ezezagunek edo beste fase batekoek ez dute egoerarik aldatzen.
    /// </summary>
    public sealed class MisioLogika
    {
        private readonly List<MisioFasea> _faseak;
        private readonly HashSet<string> _beteak = new HashSet<string>(StringComparer.Ordinal);

        public MisioLogika(IEnumerable<MisioFasea> faseak)
        {
            _faseak = faseak?.ToList() ?? new List<MisioFasea>();
            Aurreratu();
        }

        public int FaseKopurua => _faseak.Count;

        /// <summary>Uneko fasearen indizea; misioa amaituta dagoenean, FaseKopurua.</summary>
        public int UnekoFasea { get; private set; }

        public bool Amaituta => UnekoFasea >= _faseak.Count;

        public string UnekoDeskribapena => Amaituta ? string.Empty : _faseak[UnekoFasea].Deskribapena;

        public int FasekoBeteak => Amaituta ? 0 : _faseak[UnekoFasea].Helburuak.Count(_beteak.Contains);

        public int FasekoGuztiak => Amaituta ? 0 : _faseak[UnekoFasea].Helburuak.Count;

        public bool BetetaDago(string id) => id != null && _beteak.Contains(id);

        public BeteEmaitza Bete(string id)
        {
            if (string.IsNullOrEmpty(id) || !_faseak.Any(fasea => fasea.Helburuak.Contains(id)))
            {
                return BeteEmaitza.Ezezaguna;
            }

            if (_beteak.Contains(id))
            {
                return BeteEmaitza.JadaBeteta;
            }

            if (Amaituta || !_faseak[UnekoFasea].Helburuak.Contains(id))
            {
                return BeteEmaitza.EzDagokio;
            }

            _beteak.Add(id);
            Aurreratu();
            return BeteEmaitza.Onartua;
        }

        /// <summary>Helburu hau uneko fasekoa da eta oraindik ez dago beteta.</summary>
        public bool Aktibo(string id)
        {
            return !Amaituta && id != null && !_beteak.Contains(id) && _faseak[UnekoFasea].Helburuak.Contains(id);
        }

        private void Aurreratu()
        {
            while (!Amaituta && _faseak[UnekoFasea].Helburuak.All(_beteak.Contains))
            {
                UnekoFasea++;
            }
        }
    }
}
