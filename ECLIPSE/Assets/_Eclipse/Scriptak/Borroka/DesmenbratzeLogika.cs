using System;
using System.Collections.Generic;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Desmenbratzearen ondorioak (logika hutsa): galdutako besoak eta hankak → mugimendu- eta eraso-faktoreak.
    /// Hanka bat: herren (erdi abiadura). Biak: narrastaka. Beso bakoitzak erasoa ahultzen du. Burua eta enborra ez dira bereizten.
    /// </summary>
    public sealed class DesmenbratzeLogika
    {
        private readonly HashSet<KalteZona> _galduak = new HashSet<KalteZona>();

        public int HankaGalduak { get; private set; }

        public int BesoGalduak { get; private set; }

        public IReadOnlyCollection<KalteZona> Galduak => _galduak;

        public bool Narrastaka => HankaGalduak >= 2;

        public float AbiaduraFaktorea => HankaGalduak switch
        {
            0 => 1f,
            1 => 0.5f,
            _ => 0.3f,
        };

        public float ErasoFaktorea => BesoGalduak switch
        {
            0 => 1f,
            1 => 0.6f,
            _ => 0.25f,
        };

        public static bool Hanka(KalteZona zona) => zona == KalteZona.HankaEzkerra || zona == KalteZona.HankaEskuina;

        public static bool Besoa(KalteZona zona) => zona == KalteZona.BesoEzkerra || zona == KalteZona.BesoEskuina;

        /// <summary>Zati bat galdu. False: ez da bereizgarria edo lehendik galduta zegoen (ez da ezer aldatzen).</summary>
        public bool Galdu(KalteZona zona)
        {
            if (!GorputzKalteLogika.Bereizgarria(zona))
            {
                return false;
            }

            if (!_galduak.Add(zona))
            {
                return false;
            }

            if (Hanka(zona))
            {
                HankaGalduak++;
            }
            else if (Besoa(zona))
            {
                BesoGalduak++;
            }
            else
            {
                throw new InvalidOperationException($"Zona bereizgarri ezezaguna: {zona}");
            }

            return true;
        }
    }
}
