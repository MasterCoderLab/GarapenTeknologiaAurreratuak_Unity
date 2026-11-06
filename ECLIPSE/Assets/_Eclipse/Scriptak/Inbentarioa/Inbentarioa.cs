using System;
using System.Collections.Generic;
using System.Linq;

namespace Eclipse.Inbentarioak
{
    public enum ElementuMota
    {
        Arma,
        Ekipoa,
        Kontsumigarria,
        MisioObjektua,
    }

    public enum GehituEmaitza
    {
        Onartua,
        Beteta,
        Baliogabea,
    }

    /// <summary>Elementu baten datu aldaezinak. Etorkizunean ScriptableObject batetik bete daitezke.</summary>
    public sealed class ElementuDatuak
    {
        /// <summary>EMP pistolarako gordetako id-a. Oraindik ez da inon ematen: arma ez dago inplementatuta.</summary>
        public const string EmpPistolaId = "emp_pistola";

        public ElementuDatuak(string id, string izena, ElementuMota mota, int pilaGehienez = 1)
        {
            Id = id;
            Izena = izena;
            Mota = mota;
            PilaGehienez = Math.Max(1, pilaGehienez);
        }

        public string Id { get; }

        public string Izena { get; }

        public ElementuMota Mota { get; }

        public int PilaGehienez { get; }

        public bool BizkorSarbidekoa => Mota == ElementuMota.Arma || Mota == ElementuMota.Ekipoa;
    }

    /// <summary>Ranura bat: elementu bat eta haren kantitatea.</summary>
    public sealed class InbentarioRanura
    {
        public ElementuDatuak Elementua { get; internal set; }

        public int Kopurua { get; internal set; }

        public bool Hutsik => Elementua == null;

        internal void Hustu()
        {
            Elementua = null;
            Kopurua = 0;
        }
    }

    /// <summary>
    /// Lau bizkor-sarbide (armak eta ekipoa) eta motxila (objektuak eta kontsumigarriak), Unity-tik kanpo.
    /// Datu-nukleoak ez dira hemen sartzen: GameSession-ek zenbatzen ditu.
    /// </summary>
    public sealed class Inbentarioa
    {
        public const int BizkorKopurua = 4;
        public const int MotxilaKopurua = 12;

        private readonly InbentarioRanura[] _bizkorrak = Enumerable.Range(0, BizkorKopurua).Select(_ => new InbentarioRanura()).ToArray();
        private readonly InbentarioRanura[] _motxila = Enumerable.Range(0, MotxilaKopurua).Select(_ => new InbentarioRanura()).ToArray();

        public IReadOnlyList<InbentarioRanura> Bizkorrak => _bizkorrak;

        public IReadOnlyList<InbentarioRanura> Motxila => _motxila;

        /// <summary>Hautatutako bizkor-sarbidea (0-3); -1 ezer ez badago hautatuta.</summary>
        public int Hautatua { get; private set; } = -1;

        /// <summary>Aldaketa bakoitzean handitzen da: interfazeak hemendik daki noiz eguneratu.</summary>
        public int Bertsioa { get; private set; }

        /// <summary>Armak eta ekipoa bizkor-sarbide librean, bestela motxilan; besteak motxilan pilatzen dira. Sartzen ez bada, ez da ezer aldatzen.</summary>
        public GehituEmaitza Gehitu(ElementuDatuak elementua, int kopurua = 1)
        {
            if (elementua == null || kopurua <= 0)
            {
                return GehituEmaitza.Baliogabea;
            }

            if (elementua.BizkorSarbidekoa && kopurua == 1)
            {
                InbentarioRanura librea = _bizkorrak.FirstOrDefault(r => r.Hutsik);
                if (librea != null)
                {
                    librea.Elementua = elementua;
                    librea.Kopurua = 1;
                    Bertsioa++;
                    return GehituEmaitza.Onartua;
                }
            }

            int tokia = _motxila.Sum(r => r.Hutsik ? elementua.PilaGehienez : r.Elementua.Id == elementua.Id ? elementua.PilaGehienez - r.Kopurua : 0);
            if (tokia < kopurua)
            {
                return GehituEmaitza.Beteta;
            }

            int geratzen = kopurua;
            foreach (InbentarioRanura ranura in _motxila.Where(r => !r.Hutsik && r.Elementua.Id == elementua.Id).Concat(_motxila.Where(r => r.Hutsik)))
            {
                if (geratzen == 0)
                {
                    break;
                }

                int sartu = Math.Min(geratzen, elementua.PilaGehienez - ranura.Kopurua);
                ranura.Elementua = elementua;
                ranura.Kopurua += sartu;
                geratzen -= sartu;
            }

            Bertsioa++;
            return GehituEmaitza.Onartua;
        }

        public int Kopurua(string id)
        {
            return _bizkorrak.Concat(_motxila).Where(r => !r.Hutsik && r.Elementua.Id == id).Sum(r => r.Kopurua);
        }

        public bool Badago(string id) => Kopurua(id) > 0;

        /// <summary>Nahikoa badago bakarrik kentzen du; bestela ez du ezer aldatzen.</summary>
        public bool Kendu(string id, int kopurua = 1)
        {
            if (kopurua <= 0 || Kopurua(id) < kopurua)
            {
                return false;
            }

            int geratzen = kopurua;
            foreach (InbentarioRanura ranura in _motxila.Concat(_bizkorrak).Where(r => !r.Hutsik && r.Elementua.Id == id))
            {
                int kendu = Math.Min(geratzen, ranura.Kopurua);
                ranura.Kopurua -= kendu;
                geratzen -= kendu;
                if (ranura.Kopurua == 0)
                {
                    ranura.Hustu();
                }

                if (geratzen == 0)
                {
                    break;
                }
            }

            if (Hautatua >= 0 && _bizkorrak[Hautatua].Hutsik)
            {
                Hautatua = -1;
            }

            Bertsioa++;
            return true;
        }

        /// <summary>Bizkor-sarbide bat hautatu. Hutsik badago, ez da ezer aldatzen eta false itzultzen du.</summary>
        public bool BizkorraHautatu(int indizea)
        {
            if (indizea < 0 || indizea >= BizkorKopurua || _bizkorrak[indizea].Hutsik)
            {
                return false;
            }

            Hautatua = indizea;
            Bertsioa++;
            return true;
        }
    }
}
