using Eclipse.Jokalaria;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests
{
    public sealed class JokalariAnimLogikaTests
    {
        private const float Tartea = 0.01f;

        private static JokalariAnimLogika Sortu(float yaw = 0f)
        {
            var logika = new JokalariAnimLogika();
            logika.Hasieratu(yaw);
            return logika;
        }

        /// <summary>Segundo bat fotogramaka: biraketa osoa egiteko denbora nahikoa.</summary>
        private static AnimParametroak Iragan(JokalariAnimLogika logika, Vector3 abiadura, bool lurrean = true, bool makurtuta = false,
            float begirada = 0f, OrientazioModua modua = OrientazioModua.Mugimendua, int fotogramak = 60)
        {
            AnimParametroak p = default;
            for (int i = 0; i < fotogramak; i++)
            {
                p = logika.Eguneratu(abiadura, lurrean, makurtuta, begirada, modua, 1f / 60f);
            }

            return p;
        }

        [Test]
        public void Mugitzean_EreduaNorabidera_Biratzen_Da()
        {
            JokalariAnimLogika logika = Sortu();
            Iragan(logika, new Vector3(3f, 0f, 0f));
            Assert.That(logika.Yaw, Is.EqualTo(90f).Within(Tartea));
        }

        [Test]
        public void Atzera_Mugitzean_EreduaBiratzen_Da_Kontrolatzailea_Ez()
        {
            JokalariAnimLogika logika = Sortu();
            AnimParametroak p = Iragan(logika, new Vector3(0f, 0f, -3f));
            Assert.That(Mathf.DeltaAngle(logika.Yaw, 180f), Is.EqualTo(0f).Within(Tartea));
            Assert.That(p.AbiaduraZ, Is.EqualTo(3f).Within(Tartea), "Ereduaren aurrera mugitzen da");
        }

        [Test]
        public void Geldirik_EreduakNorabidea_Gordetzen_Du()
        {
            JokalariAnimLogika logika = Sortu();
            Iragan(logika, new Vector3(-3f, 0f, 0f));
            Iragan(logika, Vector3.zero, begirada: 45f);
            Assert.That(logika.Yaw, Is.EqualTo(270f).Within(Tartea));
        }

        [Test]
        public void MugaAzpikoAbiadurak_EzDu_Biratzen()
        {
            JokalariAnimLogika logika = Sortu(10f);
            Iragan(logika, new Vector3(0.1f, 0f, 0f));
            Assert.That(logika.Yaw, Is.EqualTo(10f).Within(Tartea));
        }

        [Test]
        public void Biraketa_AbiaduraMugatuta_Dago()
        {
            JokalariAnimLogika logika = Sortu();
            logika.Eguneratu(new Vector3(0f, 0f, -3f), true, false, 0f, OrientazioModua.Mugimendua, 0.1f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, logika.Yaw)), Is.EqualTo(JokalariAnimLogika.BiraAbiadura * 0.1f).Within(Tartea));
        }

        [Test]
        public void KameraModuan_Begiradari_Jarraitzen_Dio()
        {
            JokalariAnimLogika logika = Sortu();
            AnimParametroak p = Iragan(logika, new Vector3(3f, 0f, 0f), begirada: 0f, modua: OrientazioModua.Kamera);
            Assert.That(logika.Yaw, Is.EqualTo(0f).Within(Tartea));
            Assert.That(p.AbiaduraX, Is.EqualTo(3f).Within(Tartea), "Alboko mugimendua A3-rako");
            Assert.That(p.AbiaduraZ, Is.EqualTo(0f).Within(Tartea));
        }

        [Test]
        public void Abiadura_Horizontala_Bakarrik()
        {
            JokalariAnimLogika logika = Sortu();
            AnimParametroak p = logika.Eguneratu(new Vector3(0f, -5f, 4f), true, false, 0f, OrientazioModua.Mugimendua, 1f / 60f);
            Assert.That(p.Abiadura, Is.EqualTo(4f).Within(Tartea));
        }

        [Test]
        public void Airea_ItxaronEgiten_Du_EskaileretanDardararikEz()
        {
            JokalariAnimLogika logika = Sortu();
            AnimParametroak laburra = logika.Eguneratu(new Vector3(0f, -2f, 0f), false, false, 0f, OrientazioModua.Mugimendua, 0.1f);
            Assert.That(laburra.Airean, Is.False);
            AnimParametroak luzea = logika.Eguneratu(new Vector3(0f, -2f, 0f), false, false, 0f, OrientazioModua.Mugimendua, 0.1f);
            Assert.That(luzea.Airean, Is.True);
            AnimParametroak lurrean = logika.Eguneratu(Vector3.zero, true, false, 0f, OrientazioModua.Mugimendua, 0.02f);
            Assert.That(lurrean.Airean, Is.False);
        }

        [Test]
        public void Jauzia_Berehala_Airean()
        {
            JokalariAnimLogika logika = Sortu();
            AnimParametroak p = logika.Eguneratu(new Vector3(0f, 6f, 0f), false, false, 0f, OrientazioModua.Mugimendua, 0.02f);
            Assert.That(p.Airean, Is.True);
        }

        [Test]
        public void Makurtuta_Pasatzen_Da()
        {
            JokalariAnimLogika logika = Sortu();
            Assert.That(logika.Eguneratu(Vector3.zero, true, true, 0f, OrientazioModua.Mugimendua, 0.02f).Makurtuta, Is.True);
        }
    }
}
