using System;

namespace Eclipse.Borroka
{
    public enum TiroEmaitza
    {
        /// <summary>Tiroa egin da: bala bat gutxiago kargadorean.</summary>
        Tiro,

        /// <summary>Kargadorea hutsik: klik bat, ez dago tirorik.</summary>
        Hutsik,

        /// <summary>Kadentzia: aurreko tirotik denbora gutxiegi.</summary>
        Itxaron,

        /// <summary>Birkargatzen ari da.</summary>
        Birkargatzen,
    }

    /// <summary>
    /// Arma baten logika hutsa (EMP pistola): kargadorea, erreserba, kadentzia eta birkarga.
    /// Denbora kanpotik ematen da (segundoak), testetan errepikagarria izateko.
    /// </summary>
    public sealed class ArmaLogika
    {
        public ArmaLogika(int kargadorea = 8, int erreserba = 24, int erreserbaGehienez = 64, float kadentzia = 0.3f, float birkargaDenbora = 1.5f)
        {
            if (kargadorea <= 0 || erreserba < 0 || erreserbaGehienez < erreserba || kadentzia < 0f || birkargaDenbora < 0f)
            {
                throw new ArgumentException("Armaren balio baliogabeak");
            }

            KargadoreTamaina = kargadorea;
            Kargadorean = kargadorea;
            Erreserba = erreserba;
            ErreserbaGehienez = erreserbaGehienez;
            Kadentzia = kadentzia;
            BirkargaDenbora = birkargaDenbora;
        }

        public int KargadoreTamaina { get; }

        public int Kargadorean { get; private set; }

        public int Erreserba { get; private set; }

        public int ErreserbaGehienez { get; }

        public float Kadentzia { get; }

        public float BirkargaDenbora { get; }

        public bool Birkargatzen => _birkargaAmaiera > 0f;

        private float _azkenTiroa = float.NegativeInfinity;
        private float _birkargaAmaiera;

        /// <summary>Birkarga amaitzen da denbora iristean. Fotograma bakoitzean deitu.</summary>
        public void Eguneratu(float orain)
        {
            if (Birkargatzen && orain >= _birkargaAmaiera)
            {
                int behar = KargadoreTamaina - Kargadorean;
                int hartu = Math.Min(behar, Erreserba);
                Kargadorean += hartu;
                Erreserba -= hartu;
                _birkargaAmaiera = 0f;
            }
        }

        public TiroEmaitza Tiro(float orain)
        {
            Eguneratu(orain);
            if (Birkargatzen)
            {
                return TiroEmaitza.Birkargatzen;
            }

            if (orain - _azkenTiroa < Kadentzia)
            {
                return TiroEmaitza.Itxaron;
            }

            if (Kargadorean == 0)
            {
                _azkenTiroa = orain;
                return TiroEmaitza.Hutsik;
            }

            Kargadorean--;
            _azkenTiroa = orain;
            return TiroEmaitza.Tiro;
        }

        /// <summary>Birkarga hasi: kargadorea beteta edo erreserba hutsik badago, ez.</summary>
        public bool BirkargaHasi(float orain)
        {
            Eguneratu(orain);
            if (Birkargatzen || Kargadorean == KargadoreTamaina || Erreserba == 0)
            {
                return false;
            }

            _birkargaAmaiera = orain + BirkargaDenbora;
            return true;
        }

        /// <summary>Munizio-kaxa: erreserbara, mugaraino. Benetan gehitutakoa itzultzen du.</summary>
        public int MunizioaGehitu(int kopurua)
        {
            int gehitua = Math.Max(0, Math.Min(kopurua, ErreserbaGehienez - Erreserba));
            Erreserba += gehitua;
            return gehitua;
        }
    }
}
