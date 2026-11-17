using System;

namespace Eclipse.Borroka
{
    public enum KalteEmaitza
    {
        /// <summary>Kaltea hartu du eta bizirik dago.</summary>
        Hartua,

        /// <summary>Kalte honekin hil da (behin bakarrik).</summary>
        Hilda,

        /// <summary>Ez du ezer egin: hilda zegoen, kopurua ≤ 0 edo ukiezina da.</summary>
        Ezikusia,
    }

    /// <summary>Bizitza logika hutsa (jokalaria eta etsaiak): kaltea, sendatzea, ukiezintasun laburra eta heriotza.</summary>
    public sealed class BizitzaLogika
    {
        public BizitzaLogika(float gehienez, float ukiezinDenbora = 0f)
        {
            if (gehienez <= 0f || ukiezinDenbora < 0f)
            {
                throw new ArgumentException("Bizitzaren balio baliogabeak");
            }

            Gehienez = gehienez;
            Unekoa = gehienez;
            UkiezinDenbora = ukiezinDenbora;
        }

        public float Gehienez { get; }

        public float Unekoa { get; private set; }

        public float UkiezinDenbora { get; }

        public bool Bizirik => Unekoa > 0f;

        public float Ehunekoa => Unekoa / Gehienez;

        private float _ukiezinAmaiera = float.NegativeInfinity;

        public KalteEmaitza KalteaHartu(float kopurua, float orain)
        {
            if (!Bizirik || kopurua <= 0f || orain < _ukiezinAmaiera)
            {
                return KalteEmaitza.Ezikusia;
            }

            Unekoa = Math.Max(0f, Unekoa - kopurua);
            _ukiezinAmaiera = orain + UkiezinDenbora;
            return Bizirik ? KalteEmaitza.Hartua : KalteEmaitza.Hilda;
        }

        /// <summary>Botikina: bizirik badago bakarrik, gehienezkoraino. Benetan sendatutakoa itzultzen du.</summary>
        public float Sendatu(float kopurua)
        {
            if (!Bizirik || kopurua <= 0f)
            {
                return 0f;
            }

            float aurrekoa = Unekoa;
            Unekoa = Math.Min(Gehienez, Unekoa + kopurua);
            return Unekoa - aurrekoa;
        }

        /// <summary>Berpiztu (misioa berrabiaraztean edo probetan): bizitza osoa.</summary>
        public void Berrezarri()
        {
            Unekoa = Gehienez;
            _ukiezinAmaiera = float.NegativeInfinity;
        }
    }
}
