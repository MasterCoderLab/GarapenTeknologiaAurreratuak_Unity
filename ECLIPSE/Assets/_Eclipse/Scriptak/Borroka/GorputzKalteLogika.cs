using System;
using System.Collections.Generic;

namespace Eclipse.Borroka
{
    /// <summary>Kalte-zonak: tiroaren eragina eta, etorkizunean, desmenbratzeko zatiak.</summary>
    public enum KalteZona
    {
        Enborra,
        Burua,
        BesoEzkerra,
        BesoEskuina,
        HankaEzkerra,
        HankaEskuina,
    }

    /// <summary>Zona batek jasotako kaltea: zenbat kendu zaion bizitzari eta zati bat hautsi den (lehen aldiz).</summary>
    public readonly struct ZonaKaltea
    {
        public ZonaKaltea(KalteZona zona, float kaltea, bool zatiaHautsiDa)
        {
            Zona = zona;
            Kaltea = kaltea;
            ZatiaHautsiDa = zatiaHautsiDa;
        }

        public KalteZona Zona { get; }

        public float Kaltea { get; }

        /// <summary>Kolpe honekin zona horren muga gainditu da: desmenbratzeko gertaera (bisuala geroago).</summary>
        public bool ZatiaHautsiDa { get; }
    }

    /// <summary>
    /// Kalte lokalizatua (logika hutsa): zona bakoitzak biderkatzailea du, eta gorputz-adarrek (besoak, hankak) kalte-muga bat.
    /// Muga gainditzean zatia "hautsita" geratzen da: desmenbratze bisualak eta IA-ren aldaketak (adib. hankarik gabe motelago)
    /// gertaera honi entzungo diote. A3-ko lehen blokean gertaera bakarrik sortzen da; ez dago zati bisualik.
    /// </summary>
    public sealed class GorputzKalteLogika
    {
        private readonly Dictionary<KalteZona, float> _metatua = new Dictionary<KalteZona, float>();
        private readonly HashSet<KalteZona> _hautsiak = new HashSet<KalteZona>();
        private readonly float _adarMuga;

        /// <param name="adarMuga">Beso edo hanka bat hausteko metatutako kaltea (biderkatzailearen ondoren).</param>
        public GorputzKalteLogika(float adarMuga = 40f)
        {
            if (adarMuga <= 0f)
            {
                throw new ArgumentException("Muga positiboa izan behar da");
            }

            _adarMuga = adarMuga;
        }

        public static float Biderkatzailea(KalteZona zona) => zona switch
        {
            KalteZona.Burua => 2f,
            KalteZona.Enborra => 1f,
            KalteZona.BesoEzkerra or KalteZona.BesoEskuina => 0.6f,
            _ => 0.75f,
        };

        public static bool Bereizgarria(KalteZona zona) => zona != KalteZona.Enborra && zona != KalteZona.Burua;

        public bool Hautsita(KalteZona zona) => _hautsiak.Contains(zona);

        public IReadOnlyCollection<KalteZona> Hautsiak => _hautsiak;

        /// <summary>Oinarrizko kaltea zona batean: biderkatzailea aplikatu eta adarra hautsi den kalkulatu.</summary>
        public ZonaKaltea Aplikatu(KalteZona zona, float oinarria)
        {
            float kaltea = Math.Max(0f, oinarria) * Biderkatzailea(zona);
            bool hautsiBerria = false;
            if (Bereizgarria(zona) && !_hautsiak.Contains(zona))
            {
                _metatua.TryGetValue(zona, out float aurrekoa);
                float berria = aurrekoa + kaltea;
                _metatua[zona] = berria;
                if (berria >= _adarMuga)
                {
                    _hautsiak.Add(zona);
                    hautsiBerria = true;
                }
            }

            return new ZonaKaltea(zona, kaltea, hautsiBerria);
        }
    }
}
