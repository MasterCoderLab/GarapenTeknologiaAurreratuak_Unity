using System;
using Eclipse.Borroka;
using NUnit.Framework;

namespace Eclipse.Tests
{
    public sealed class ArmaLogikaTests
    {
        [Test]
        public void Tiroak_KargadoreaHustu_EtaHutsik()
        {
            var arma = new ArmaLogika(kargadorea: 3, erreserba: 0, kadentzia: 0.1f);
            Assert.That(arma.Tiro(0f), Is.EqualTo(TiroEmaitza.Tiro));
            Assert.That(arma.Tiro(0.2f), Is.EqualTo(TiroEmaitza.Tiro));
            Assert.That(arma.Tiro(0.4f), Is.EqualTo(TiroEmaitza.Tiro));
            Assert.That(arma.Kargadorean, Is.EqualTo(0));
            Assert.That(arma.Tiro(0.6f), Is.EqualTo(TiroEmaitza.Hutsik));
        }

        [Test]
        public void Kadentzia_Errespetatzen_Da()
        {
            var arma = new ArmaLogika(kadentzia: 0.3f);
            Assert.That(arma.Tiro(1f), Is.EqualTo(TiroEmaitza.Tiro));
            Assert.That(arma.Tiro(1.1f), Is.EqualTo(TiroEmaitza.Itxaron));
            Assert.That(arma.Kargadorean, Is.EqualTo(7));
            Assert.That(arma.Tiro(1.31f), Is.EqualTo(TiroEmaitza.Tiro));
        }

        [Test]
        public void Birkarga_DenboraBehar_Du_EtaErreserbatikHartzen_Du()
        {
            var arma = new ArmaLogika(kargadorea: 8, erreserba: 5, birkargaDenbora: 1.5f, kadentzia: 0f);
            for (int i = 0; i < 6; i++)
            {
                arma.Tiro(i);
            }

            Assert.That(arma.BirkargaHasi(10f), Is.True);
            Assert.That(arma.Tiro(10.5f), Is.EqualTo(TiroEmaitza.Birkargatzen));
            arma.Eguneratu(11.6f);
            Assert.That(arma.Kargadorean, Is.EqualTo(7));
            Assert.That(arma.Erreserba, Is.EqualTo(0));
            Assert.That(arma.Birkargatzen, Is.False);
        }

        [Test]
        public void Birkarga_EzDa_Hasten_Beteta_Edo_ErreserbarikGabe()
        {
            var arma = new ArmaLogika(kargadorea: 8, erreserba: 0);
            Assert.That(arma.BirkargaHasi(0f), Is.False, "beteta");
            arma.Tiro(1f);
            Assert.That(arma.BirkargaHasi(2f), Is.False, "erreserbarik gabe");
        }

        [Test]
        public void Munizioa_MugaraArte()
        {
            var arma = new ArmaLogika(erreserba: 60, erreserbaGehienez: 64);
            Assert.That(arma.MunizioaGehitu(10), Is.EqualTo(4));
            Assert.That(arma.Erreserba, Is.EqualTo(64));
        }

        [Test]
        public void BalioBaliogabeak_Salbuespena()
        {
            Assert.Throws<ArgumentException>(() => new ArmaLogika(kargadorea: 0));
        }
    }

    public sealed class BizitzaLogikaTests
    {
        [Test]
        public void Kaltea_EtaHeriotza_BehinBakarrik()
        {
            var bizitza = new BizitzaLogika(100f);
            Assert.That(bizitza.KalteaHartu(60f, 0f), Is.EqualTo(KalteEmaitza.Hartua));
            Assert.That(bizitza.KalteaHartu(60f, 1f), Is.EqualTo(KalteEmaitza.Hilda));
            Assert.That(bizitza.Unekoa, Is.EqualTo(0f));
            Assert.That(bizitza.KalteaHartu(10f, 2f), Is.EqualTo(KalteEmaitza.Ezikusia));
        }

        [Test]
        public void Ukiezintasuna_KolpeJarraiakIragazten_Ditu()
        {
            var bizitza = new BizitzaLogika(100f, ukiezinDenbora: 0.5f);
            bizitza.KalteaHartu(20f, 0f);
            Assert.That(bizitza.KalteaHartu(20f, 0.2f), Is.EqualTo(KalteEmaitza.Ezikusia));
            Assert.That(bizitza.KalteaHartu(20f, 0.6f), Is.EqualTo(KalteEmaitza.Hartua));
            Assert.That(bizitza.Unekoa, Is.EqualTo(60f));
        }

        [Test]
        public void Sendatu_GehienezkoraArte_EtaHildakoakEz()
        {
            var bizitza = new BizitzaLogika(100f);
            bizitza.KalteaHartu(30f, 0f);
            Assert.That(bizitza.Sendatu(50f), Is.EqualTo(30f));
            bizitza.KalteaHartu(200f, 1f);
            Assert.That(bizitza.Sendatu(50f), Is.EqualTo(0f));
            bizitza.Berrezarri();
            Assert.That(bizitza.Bizirik && bizitza.Unekoa == 100f, Is.True);
        }
    }

    public sealed class GorputzKalteLogikaTests
    {
        [Test]
        public void Biderkatzaileak_ZonaBakoitzean()
        {
            var gorputza = new GorputzKalteLogika();
            Assert.That(gorputza.Aplikatu(KalteZona.Burua, 25f).Kaltea, Is.EqualTo(50f).Within(1e-4f));
            Assert.That(gorputza.Aplikatu(KalteZona.Enborra, 25f).Kaltea, Is.EqualTo(25f).Within(1e-4f));
            Assert.That(gorputza.Aplikatu(KalteZona.BesoEskuina, 25f).Kaltea, Is.EqualTo(15f).Within(1e-4f));
        }

        [Test]
        public void Besoa_MugaGainditzean_BehinHausten_Da()
        {
            var gorputza = new GorputzKalteLogika(adarMuga: 40f);
            Assert.That(gorputza.Aplikatu(KalteZona.BesoEzkerra, 25f).ZatiaHautsiDa, Is.False);
            Assert.That(gorputza.Aplikatu(KalteZona.BesoEzkerra, 25f).ZatiaHautsiDa, Is.False);
            Assert.That(gorputza.Aplikatu(KalteZona.BesoEzkerra, 25f).ZatiaHautsiDa, Is.True, "15+15+15 ≥ 40");
            Assert.That(gorputza.Aplikatu(KalteZona.BesoEzkerra, 25f).ZatiaHautsiDa, Is.False, "behin bakarrik");
            Assert.That(gorputza.Hautsita(KalteZona.BesoEzkerra) && !gorputza.Hautsita(KalteZona.BesoEskuina), Is.True);
        }

        [Test]
        public void EnborraEtaBurua_EzDira_Bereizten()
        {
            var gorputza = new GorputzKalteLogika(adarMuga: 1f);
            Assert.That(gorputza.Aplikatu(KalteZona.Burua, 100f).ZatiaHautsiDa, Is.False);
            Assert.That(gorputza.Aplikatu(KalteZona.Enborra, 100f).ZatiaHautsiDa, Is.False);
        }
    }

    public sealed class EtsaiEgoeraMakinaTests
    {
        private static EtsaiErabakia Iragan(EtsaiEgoeraMakina m, float segundoak, bool ikusten, float distantzia, bool helburuan = false)
        {
            EtsaiErabakia e = default;
            for (float t = 0f; t < segundoak; t += 0.05f)
            {
                e = m.Eguneratu(0.05f, ikusten, distantzia, helburuan);
            }

            return e;
        }

        [Test]
        public void Ikustean_Jarraitu_EtaHurbilEraso()
        {
            var m = new EtsaiEgoeraMakina();
            Assert.That(m.Eguneratu(0.05f, false, 10f, false).Egoera, Is.EqualTo(EtsaiEgoera.Zain));
            EtsaiErabakia e = m.Eguneratu(0.05f, true, 10f, false);
            Assert.That(e.Egoera == EtsaiEgoera.Jarraitu && e.Mugitu, Is.True);
            Assert.That(m.Eguneratu(0.05f, true, 1.2f, false).Egoera, Is.EqualTo(EtsaiEgoera.Eraso));
        }

        [Test]
        public void Erasoak_KolpeBakarra_Ematen_Du()
        {
            var m = new EtsaiEgoeraMakina(erasoIraupena: 1.1f, kolpeUnea: 0.45f);
            m.Eguneratu(0.05f, true, 1f, false);
            m.Eguneratu(0.05f, true, 1f, false);
            int kolpeak = 0;
            for (float t = 0f; t < 1.05f; t += 0.05f)
            {
                kolpeak += m.Eguneratu(0.05f, true, 1f, false).Kolpea ? 1 : 0;
            }

            Assert.That(kolpeak, Is.EqualTo(1));
        }

        [Test]
        public void Erasoan_JokalariaUrrun_KolpeakEzDu_Jotzen()
        {
            var m = new EtsaiEgoeraMakina();
            m.Eguneratu(0.05f, true, 1f, false);
            m.Eguneratu(0.05f, true, 1f, false);
            bool kolpea = false;
            for (float t = 0f; t < 1f; t += 0.05f)
            {
                kolpea |= m.Eguneratu(0.05f, true, 5f, false).Kolpea;
            }

            Assert.That(kolpea, Is.False);
        }

        [Test]
        public void Kaltea_Txunditzen_Du_EtaGeroJarraitzen()
        {
            var m = new EtsaiEgoeraMakina(txundiDenbora: 0.5f);
            m.KalteaJaso();
            Assert.That(m.Eguneratu(0.1f, false, 10f, false).Mugitu, Is.False);
            Assert.That(Iragan(m, 0.5f, true, 10f).Egoera, Is.EqualTo(EtsaiEgoera.Jarraitu));
        }

        [Test]
        public void Galtzean_Bilatu_EtaZainera_Itzuli()
        {
            var m = new EtsaiEgoeraMakina(bilatuDenbora: 1f);
            m.Eguneratu(0.05f, true, 10f, false);
            Assert.That(m.Eguneratu(0.05f, false, 10f, false).Egoera, Is.EqualTo(EtsaiEgoera.Bilatu));
            Assert.That(Iragan(m, 0.5f, false, 10f, helburuan: false).Mugitu, Is.True, "azken lekura doa");
            Assert.That(Iragan(m, 1.2f, false, 10f, helburuan: true).Egoera, Is.EqualTo(EtsaiEgoera.Zain));
        }

        [Test]
        public void Trabatuta_Galdu_BilatzeraDoa()
        {
            var m = new EtsaiEgoeraMakina();
            m.Eguneratu(0.05f, true, 10f, false);
            m.Galdu();
            Assert.That(m.Egoera, Is.EqualTo(EtsaiEgoera.Bilatu));
            var zain = new EtsaiEgoeraMakina();
            zain.Galdu();
            Assert.That(zain.Egoera, Is.EqualTo(EtsaiEgoera.Zain), "Zain dagoenean ez du ezer aldatzen");
        }

        [Test]
        public void Zarata_Entzutean_Jarraitu()
        {
            var m = new EtsaiEgoeraMakina();
            m.Entzun();
            Assert.That(m.Egoera, Is.EqualTo(EtsaiEgoera.Jarraitu));
        }

        [Test]
        public void Hilda_EzDa_Aldatzen()
        {
            var m = new EtsaiEgoeraMakina();
            m.Hil();
            m.KalteaJaso();
            m.Entzun();
            Assert.That(Iragan(m, 1f, true, 1f).Egoera, Is.EqualTo(EtsaiEgoera.Hilda));
        }
    }
}
