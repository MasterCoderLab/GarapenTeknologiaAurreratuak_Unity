using Eclipse.Inbentarioak;
using NUnit.Framework;

namespace Eclipse.Tests
{
    public sealed class InbentarioaTests
    {
        private static readonly ElementuDatuak Arma = new ElementuDatuak("proba_arma", "Arma", ElementuMota.Arma);
        private static readonly ElementuDatuak Botikina = new ElementuDatuak("botikina", "Botikina", ElementuMota.Kontsumigarria, 3);

        [Test]
        public void HutsikHasten_Da()
        {
            var inbentarioa = new Inbentarioa();
            Assert.That(inbentarioa.Bizkorrak.Count, Is.EqualTo(4));
            Assert.That(inbentarioa.Motxila.Count, Is.EqualTo(Inbentarioa.MotxilaKopurua));
            Assert.That(inbentarioa.Hautatua, Is.EqualTo(-1));
            Assert.That(inbentarioa.Badago(ElementuDatuak.EmpPistolaId), Is.False, "EMP pistola ez dago inplementatuta: ez da eman behar");
        }

        [Test]
        public void Arma_BizkorSarbidean_Kontsumigarria_Motxilan()
        {
            var inbentarioa = new Inbentarioa();
            Assert.That(inbentarioa.Gehitu(Arma), Is.EqualTo(GehituEmaitza.Onartua));
            Assert.That(inbentarioa.Gehitu(Botikina), Is.EqualTo(GehituEmaitza.Onartua));
            Assert.That(inbentarioa.Bizkorrak[0].Elementua, Is.SameAs(Arma));
            Assert.That(inbentarioa.Motxila[0].Elementua, Is.SameAs(Botikina));
        }

        [Test]
        public void BizkorrakBeteta_MotxilaraDoa()
        {
            var inbentarioa = new Inbentarioa();
            for (int i = 0; i < 5; i++)
            {
                inbentarioa.Gehitu(Arma);
            }

            Assert.That(inbentarioa.Motxila[0].Kopurua, Is.EqualTo(1));
            Assert.That(inbentarioa.Kopurua(Arma.Id), Is.EqualTo(5));
        }

        [Test]
        public void Pilak_LehenBeteTaGeroRanuraBerria()
        {
            var inbentarioa = new Inbentarioa();
            inbentarioa.Gehitu(Botikina, 4);
            Assert.That(inbentarioa.Motxila[0].Kopurua, Is.EqualTo(3));
            Assert.That(inbentarioa.Motxila[1].Kopurua, Is.EqualTo(1));
        }

        [Test]
        public void Beteta_EzDuEzerAldatzen()
        {
            var inbentarioa = new Inbentarioa();
            var bakarra = new ElementuDatuak("x", "X", ElementuMota.MisioObjektua);
            for (int i = 0; i < Inbentarioa.MotxilaKopurua; i++)
            {
                Assert.That(inbentarioa.Gehitu(bakarra), Is.EqualTo(GehituEmaitza.Onartua));
            }

            int bertsioa = inbentarioa.Bertsioa;
            Assert.That(inbentarioa.Gehitu(bakarra), Is.EqualTo(GehituEmaitza.Beteta));
            Assert.That(inbentarioa.Kopurua("x"), Is.EqualTo(Inbentarioa.MotxilaKopurua));
            Assert.That(inbentarioa.Bertsioa, Is.EqualTo(bertsioa));
        }

        [Test]
        public void Baliogabeak_EzDuteEzerAldatzen()
        {
            var inbentarioa = new Inbentarioa();
            Assert.That(inbentarioa.Gehitu(null), Is.EqualTo(GehituEmaitza.Baliogabea));
            Assert.That(inbentarioa.Gehitu(Botikina, 0), Is.EqualTo(GehituEmaitza.Baliogabea));
            Assert.That(inbentarioa.Kendu("ez-dago"), Is.False);
            Assert.That(inbentarioa.BizkorraHautatu(9), Is.False);
            Assert.That(inbentarioa.BizkorraHautatu(0), Is.False, "hutsik dagoen ranura ezin da hautatu");
        }

        [Test]
        public void Kendu_NahikoaEzBadago_EzDuEzerAldatzen()
        {
            var inbentarioa = new Inbentarioa();
            inbentarioa.Gehitu(Botikina, 2);
            Assert.That(inbentarioa.Kendu(Botikina.Id, 3), Is.False);
            Assert.That(inbentarioa.Kopurua(Botikina.Id), Is.EqualTo(2));
            Assert.That(inbentarioa.Kendu(Botikina.Id, 2), Is.True);
            Assert.That(inbentarioa.Motxila[0].Hutsik, Is.True);
        }

        [Test]
        public void HautatutakoaKentzean_HautapenaGarbitzenDa()
        {
            var inbentarioa = new Inbentarioa();
            inbentarioa.Gehitu(Arma);
            Assert.That(inbentarioa.BizkorraHautatu(0), Is.True);
            Assert.That(inbentarioa.Hautatua, Is.EqualTo(0));
            inbentarioa.Kendu(Arma.Id);
            Assert.That(inbentarioa.Hautatua, Is.EqualTo(-1));
        }

        [Test]
        public void GameSession_DatuNukleoaBirritanEzDaZenbatzen()
        {
            var saioa = new GameSession();
            Assert.That(saioa.BildumaErregistratu("M1/Din_Bilduma_00"), Is.True);
            Assert.That(saioa.BildumaErregistratu("M1/Din_Bilduma_00"), Is.False);
            Assert.That(saioa.BildumaErregistratu("M2/Din_Bilduma_00"), Is.True);
            Assert.That(saioa.BildumaErregistratu(null), Is.False);
            Assert.That(saioa.DatuNukleoak, Is.EqualTo(2));
        }
    }
}
