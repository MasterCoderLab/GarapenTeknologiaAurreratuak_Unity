using System.Collections.Generic;
using System.Linq;
using Eclipse.Misioak;
using Eclipse.Nabigazioa;
using UnityEngine;

namespace Eclipse.Hud
{
    public enum BideEgoera
    {
        /// <summary>Ez dago helburu nagusirik edo sarerik.</summary>
        Ezezaguna,
        Ondo,

        /// <summary>Helburua badago, baina hesi batek edo sareak ez dute ibilbiderik uzten. Ez da lerrorik marrazten.</summary>
        BiderikEz,
    }

    /// <summary>
    /// HUD-aren, markatzaileen eta maparen iturri bakarra: eszenako objektuetatik hautagaiak irakurri, MisioLogika-rekin iragazi eta
    /// helburu nagusirako ibilbidea kalkulatu. Helburu-sistemarik ez du bikoizten: egoera LevelManager-ena da.
    /// </summary>
    public sealed class HelburuKudeatzailea
    {
        private const float TerminalAltuera = 1.4f;
        private const float HelburuErradioa = 3.5f;
        private const float JokalariErradioa = 3f;
        private const float BirkalkuluTartea = 1f;
        private const float HautaketaTartea = 0.2f;

        private readonly LevelManager _kudeatzailea;
        private readonly Transform _jokalaria;
        private readonly MisioTerminala[] _terminalak;
        private readonly MisioBilduma[] _bildumak;
        private readonly MisioIrteera _irteera;
        private readonly bool[] _blokeoItxiak;
        private readonly List<int> _proba = new List<int>();
        private float _hurrengoKalkulua;
        private float _hurrengoHautaketa;

        public HelburuKudeatzailea(LevelManager kudeatzailea, NabigazioDatuak datuak, Transform jokalaria)
        {
            _kudeatzailea = kudeatzailea;
            _jokalaria = jokalaria;
            Datuak = datuak;
            _terminalak = Object.FindObjectsByType<MisioTerminala>(FindObjectsInactive.Include);
            _bildumak = Object.FindObjectsByType<MisioBilduma>(FindObjectsInactive.Include);
            _irteera = Object.FindAnyObjectByType<MisioIrteera>(FindObjectsInactive.Include);
            _blokeoItxiak = new bool[datuak.Blokeoak.Count];
        }

        public NabigazioDatuak Datuak { get; }

        public List<HelburuHautagaia> Hautatuak { get; private set; } = new List<HelburuHautagaia>();

        /// <summary>Oraingo helburu nagusia: fase honetako terminal hurbilena (ibilbidez) edo irteera. Id-a null bada, ez dago.</summary>
        public HelburuHautagaia Nagusia { get; private set; }

        public bool NagusiaDago => Nagusia.Id != null;

        public BideEgoera Egoera { get; private set; }

        public List<int> Bidea { get; } = new List<int>();

        public float BideLuzera { get; private set; }

        public IReadOnlyList<bool> BlokeoItxiak => _blokeoItxiak;

        public Vector3 JokalariPosizioa => _jokalaria != null ? _jokalaria.position : Vector3.zero;

        public IEnumerable<MisioBilduma> BildumaAktiboak => _bildumak.Where(b => b != null && b.gameObject.activeInHierarchy);

        public Vector3? IrteeraPosizioa => _irteera != null ? _irteera.transform.position : (Vector3?)null;

        /// <summary>Jokalaria barruan duen terminala, eta oraindik bete gabe badago; bestela null.</summary>
        public MisioTerminala TerminalBarruan()
        {
            MisioLogika logika = _kudeatzailea.Logika;
            foreach (MisioTerminala terminala in _terminalak)
            {
                if (terminala != null && terminala.JokalariaBarruan && !logika.BetetaDago(terminala.Helburua))
                {
                    return terminala;
                }
            }

            return null;
        }

        /// <summary>Hautaketa segundoan bost aldiz eta ibilbidea behin, memoria eta denbora aurrezteko; biak berehala, behartuta (helburu bat bete denean edo mapa irekitzean).</summary>
        public void Eguneratu(bool ibilbideaBehartu)
        {
            if (_jokalaria == null)
            {
                return;
            }

            if (!ibilbideaBehartu && Time.unscaledTime < _hurrengoHautaketa)
            {
                return;
            }

            _hurrengoHautaketa = Time.unscaledTime + HautaketaTartea;
            for (int b = 0; b < _blokeoItxiak.Length; b++)
            {
                BlokeoAtea atea = Datuak.Blokeoak[b].Atea;
                _blokeoItxiak[b] = atea != null && atea.gameObject.activeInHierarchy;
            }

            Hautatuak = HelburuHautaketa.Hautatu(_kudeatzailea.Logika, _jokalaria.position, Hautagaiak());
            if (ibilbideaBehartu || Time.unscaledTime >= _hurrengoKalkulua)
            {
                _hurrengoKalkulua = Time.unscaledTime + BirkalkuluTartea;
                IbilbideaKalkulatu();
            }
        }

        private IEnumerable<HelburuHautagaia> Hautagaiak()
        {
            foreach (MisioTerminala t in _terminalak)
            {
                if (t != null)
                {
                    yield return new HelburuHautagaia(HelburuMota.Terminala, t.Helburua, t.Izena, t.transform.position + Vector3.up * TerminalAltuera);
                }
            }

            foreach (MisioBilduma b in _bildumak)
            {
                if (b != null)
                {
                    yield return new HelburuHautagaia(HelburuMota.Bilduma, b.name, "Datu-nukleoa", b.transform.position, b.gameObject.activeInHierarchy);
                }
            }

            foreach (BlokeoDatua b in Datuak.Blokeoak)
            {
                if (b.Atea != null)
                {
                    yield return new HelburuHautagaia(HelburuMota.Blokeoa, b.Atea.name, "Hesia itxita", b.Kaxa.center, b.Atea.gameObject.activeInHierarchy);
                }
            }

            if (_irteera != null)
            {
                yield return new HelburuHautagaia(HelburuMota.Irteera, _irteera.name, "Irteera", _irteera.transform.position);
            }
        }

        private void IbilbideaKalkulatu()
        {
            List<HelburuHautagaia> hautagaiak = Hautatuak.Where(h => h.Mota == HelburuMota.Terminala || h.Mota == HelburuMota.Irteera).ToList();
            Bidea.Clear();
            BideLuzera = 0f;
            Nagusia = hautagaiak.FirstOrDefault();
            Egoera = NagusiaDago ? BideEgoera.BiderikEz : BideEgoera.Ezezaguna;
            if (!NagusiaDago || !Datuak.Erabilgarria)
            {
                Egoera = BideEgoera.Ezezaguna;
                return;
            }

            NabigazioSarea sarea = Datuak.Sarea;
            int hasiera = sarea.Hurbilena(_jokalaria.position, JokalariErradioa);
            float onena = float.MaxValue;
            foreach (HelburuHautagaia h in hautagaiak)
            {
                // Terminalaren markatzailea altuago dago: oinen mailan bilatu. Irteeraren kaxa zoruaren gainean dago eta altuerak ez du pisatzen.
                int helmuga = h.Mota == HelburuMota.Terminala
                    ? sarea.Hurbilena(h.Posizioa + Vector3.down * TerminalAltuera, HelburuErradioa)
                    : sarea.Hurbilena(h.Posizioa, HelburuErradioa, 1f);
                if (!sarea.BideaBilatu(hasiera, helmuga, _blokeoItxiak, _proba))
                {
                    continue;
                }

                float luzera = sarea.BideLuzera(_proba);
                if (luzera < onena)
                {
                    onena = luzera;
                    Nagusia = h;
                    Bidea.Clear();
                    Bidea.AddRange(_proba);
                    BideLuzera = luzera;
                    Egoera = BideEgoera.Ondo;
                }
            }
        }
    }
}
