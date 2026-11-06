using Eclipse.Misioak;
using NUnit.Framework;

namespace Eclipse.Tests
{
    public sealed class MisioLogikaTests
    {
        private static MisioLogika Sortu()
        {
            return new MisioLogika(new[]
            {
                new MisioFasea("Kodea lortu", new[] { "kodea" }),
                new MisioFasea("Sorgailuak", new[] { "s1", "s2", "s3" }),
                new MisioFasea("Kontrola", new[] { "kontrola" }),
            });
        }

        [Test]
        public void Hasieran_LehenFasean_Dago()
        {
            MisioLogika logika = Sortu();
            Assert.That(logika.UnekoFasea, Is.EqualTo(0));
            Assert.That(logika.Amaituta, Is.False);
            Assert.That(logika.UnekoDeskribapena, Is.EqualTo("Kodea lortu"));
        }

        [Test]
        public void BesteFasekoHelburua_EzDuEgoeraAldatzen()
        {
            MisioLogika logika = Sortu();
            Assert.That(logika.Bete("s1"), Is.EqualTo(BeteEmaitza.EzDagokio));
            Assert.That(logika.UnekoFasea, Is.EqualTo(0));
            Assert.That(logika.BetetaDago("s1"), Is.False);
        }

        [Test]
        public void IdEzezaguna_EtaHutsa_EzDituztEgoeraAldatzen()
        {
            MisioLogika logika = Sortu();
            Assert.That(logika.Bete("ez-dago"), Is.EqualTo(BeteEmaitza.Ezezaguna));
            Assert.That(logika.Bete(null), Is.EqualTo(BeteEmaitza.Ezezaguna));
            Assert.That(logika.Bete(string.Empty), Is.EqualTo(BeteEmaitza.Ezezaguna));
            Assert.That(logika.UnekoFasea, Is.EqualTo(0));
        }

        [Test]
        public void BiAldizBetetzea_JadaBeteta_DaBigarrenean()
        {
            MisioLogika logika = Sortu();
            logika.Bete("kodea");
            Assert.That(logika.Bete("kodea"), Is.EqualTo(BeteEmaitza.JadaBeteta));
            Assert.That(logika.UnekoFasea, Is.EqualTo(1));
        }

        [Test]
        public void FasekoHelburuak_EdozeinOrdenatan_EtaGuztiakBehar()
        {
            MisioLogika logika = Sortu();
            logika.Bete("kodea");
            logika.Bete("s3");
            logika.Bete("s1");
            Assert.That(logika.UnekoFasea, Is.EqualTo(1));
            Assert.That(logika.FasekoBeteak, Is.EqualTo(2));
            Assert.That(logika.FasekoGuztiak, Is.EqualTo(3));
            logika.Bete("s2");
            Assert.That(logika.UnekoFasea, Is.EqualTo(2));
        }

        [Test]
        public void AzkenHelburuarekin_Amaitzen_Da()
        {
            MisioLogika logika = Sortu();
            foreach (string id in new[] { "kodea", "s1", "s2", "s3", "kontrola" })
            {
                Assert.That(logika.Bete(id), Is.EqualTo(BeteEmaitza.Onartua));
            }

            Assert.That(logika.Amaituta, Is.True);
            Assert.That(logika.UnekoDeskribapena, Is.Empty);
            Assert.That(logika.Bete("kontrola"), Is.EqualTo(BeteEmaitza.JadaBeteta));
        }

        [Test]
        public void FaseHutsak_SaltatuEgitenDira()
        {
            var logika = new MisioLogika(new[]
            {
                new MisioFasea("Hutsa", new string[0]),
                new MisioFasea("Benetakoa", new[] { "a" }),
            });
            Assert.That(logika.UnekoFasea, Is.EqualTo(1));
        }

        [Test]
        public void Aktibo_UnekoFasekoBeteGabeentzat_Bakarrik()
        {
            MisioLogika logika = Sortu();
            Assert.That(logika.Aktibo("kodea"), Is.True);
            Assert.That(logika.Aktibo("s1"), Is.False);
            logika.Bete("kodea");
            Assert.That(logika.Aktibo("kodea"), Is.False);
            Assert.That(logika.Aktibo("s1"), Is.True);
        }

        [Test]
        public void FaserikGabe_AmaitutaHasten_Da()
        {
            Assert.That(new MisioLogika(null).Amaituta, Is.True);
        }
    }
}
