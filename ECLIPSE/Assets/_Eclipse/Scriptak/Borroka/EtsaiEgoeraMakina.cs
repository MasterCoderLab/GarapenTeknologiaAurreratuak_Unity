namespace Eclipse.Borroka
{
    public enum EtsaiEgoera
    {
        /// <summary>Bere lekuan, ezer antzeman gabe.</summary>
        Zain,

        /// <summary>Jokalaria ikusten du (edo entzun/kaltea jaso du): haren atzetik.</summary>
        Jarraitu,

        /// <summary>Erasoa: irismenean; kolpea erdian aplikatzen da.</summary>
        Eraso,

        /// <summary>EMP-ak edo kolpe batek geldiarazita.</summary>
        Txundituta,

        /// <summary>Jokalaria galdu du: azken lekura joan eta itxaron.</summary>
        Bilatu,

        Hilda,
    }

    /// <summary>Fotograma bateko erabakia: egokitzaileak NavMeshAgent-a eta Animator-a mugitzen ditu honen arabera.</summary>
    public readonly struct EtsaiErabakia
    {
        public EtsaiErabakia(EtsaiEgoera egoera, bool mugitu, bool kolpea, bool egoeraAldatu)
        {
            Egoera = egoera;
            Mugitu = mugitu;
            Kolpea = kolpea;
            EgoeraAldatu = egoeraAldatu;
        }

        public EtsaiEgoera Egoera { get; }

        /// <summary>Helburura mugitu behar du (jokalaria edo azken lekua).</summary>
        public bool Mugitu { get; }

        /// <summary>Fotograma honetan erasoaren kolpea aplikatu behar da (eraso bakoitzeko behin).</summary>
        public bool Kolpea { get; }

        public bool EgoeraAldatu { get; }
    }

    /// <summary>
    /// Etsaiaren egoera-makina txikia (logika hutsa, EditMode testekin). Sarrerak: ikusten duen, distantzia, denbora.
    /// Ez daki ezer NavMesh-ez, animazioez edo eredu bisualez: etsai-mota guztietarako balio du (behin-behinekoa eta munstroak).
    /// </summary>
    public sealed class EtsaiEgoeraMakina
    {
        public EtsaiEgoeraMakina(float erasoTartea = 1.6f, float erasoIraupena = 1.1f, float kolpeUnea = 0.45f, float txundiDenbora = 0.5f, float bilatuDenbora = 4f)
        {
            ErasoTartea = erasoTartea;
            ErasoIraupena = erasoIraupena;
            KolpeUnea = kolpeUnea;
            TxundiDenbora = txundiDenbora;
            BilatuDenbora = bilatuDenbora;
        }

        public float ErasoTartea { get; }

        public float ErasoIraupena { get; }

        public float KolpeUnea { get; }

        public float TxundiDenbora { get; }

        public float BilatuDenbora { get; }

        public EtsaiEgoera Egoera { get; private set; } = EtsaiEgoera.Zain;

        /// <summary>Uneko egoeran igarotako denbora.</summary>
        public float EgoeraDenbora { get; private set; }

        private bool _kolpeaEginda;

        /// <summary>Kaltea jaso du: txundituta geratzen da (hil ez bada) eta jokalariaz jabetzen da.</summary>
        public void KalteaJaso()
        {
            if (Egoera != EtsaiEgoera.Hilda)
            {
                Aldatu(EtsaiEgoera.Txundituta);
            }
        }

        /// <summary>Zarata (tiroa): zain edo bilatzen badago, jarraitzera.</summary>
        public void Entzun()
        {
            if (Egoera == EtsaiEgoera.Zain || Egoera == EtsaiEgoera.Bilatu)
            {
                Aldatu(EtsaiEgoera.Jarraitu);
            }
        }

        public void Hil() => Aldatu(EtsaiEgoera.Hilda);

        /// <summary>Helburua ezin da iritsi (trabatuta): bilatzera, ez zain geratu gabe jarraitzen.</summary>
        public void Galdu()
        {
            if (Egoera == EtsaiEgoera.Jarraitu)
            {
                Aldatu(EtsaiEgoera.Bilatu);
            }
        }

        /// <param name="ikusten">Jokalaria ikusmen-lerroan eta eremuan.</param>
        /// <param name="distantzia">Jokalarirainoko distantzia horizontala (m).</param>
        /// <param name="helburuanDago">Bilatzean, azken lekura iritsi da.</param>
        public EtsaiErabakia Eguneratu(float dt, bool ikusten, float distantzia, bool helburuanDago)
        {
            EtsaiEgoera aurrekoa = Egoera;
            EgoeraDenbora += dt;
            bool kolpea = false;
            switch (Egoera)
            {
                case EtsaiEgoera.Zain:
                    if (ikusten)
                    {
                        Aldatu(EtsaiEgoera.Jarraitu);
                    }

                    break;

                case EtsaiEgoera.Jarraitu:
                    if (ikusten && distantzia <= ErasoTartea)
                    {
                        Aldatu(EtsaiEgoera.Eraso);
                    }
                    else if (!ikusten)
                    {
                        Aldatu(EtsaiEgoera.Bilatu);
                    }

                    break;

                case EtsaiEgoera.Eraso:
                    if (!_kolpeaEginda && EgoeraDenbora >= KolpeUnea)
                    {
                        _kolpeaEginda = true;
                        kolpea = distantzia <= ErasoTartea + 0.4f;
                    }

                    if (EgoeraDenbora >= ErasoIraupena)
                    {
                        Aldatu(ikusten && distantzia <= ErasoTartea ? EtsaiEgoera.Eraso : EtsaiEgoera.Jarraitu);
                    }

                    break;

                case EtsaiEgoera.Txundituta:
                    if (EgoeraDenbora >= TxundiDenbora)
                    {
                        Aldatu(EtsaiEgoera.Jarraitu);
                    }

                    break;

                case EtsaiEgoera.Bilatu:
                    if (ikusten)
                    {
                        Aldatu(EtsaiEgoera.Jarraitu);
                    }
                    else if (helburuanDago && EgoeraDenbora >= BilatuDenbora)
                    {
                        Aldatu(EtsaiEgoera.Zain);
                    }

                    break;
            }

            bool mugitu = Egoera == EtsaiEgoera.Jarraitu || (Egoera == EtsaiEgoera.Bilatu && !helburuanDago);
            return new EtsaiErabakia(Egoera, mugitu, kolpea, Egoera != aurrekoa);
        }

        private void Aldatu(EtsaiEgoera berria)
        {
            if (Egoera == EtsaiEgoera.Hilda)
            {
                return;
            }

            // Eraso batetik bestera ere denbora berrabiarazten da (eraso berria).
            Egoera = berria;
            EgoeraDenbora = 0f;
            _kolpeaEginda = false;
        }
    }
}
