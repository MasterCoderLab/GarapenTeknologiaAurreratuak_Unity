using System.Collections.Generic;
using System.Linq;
using Eclipse.Hud;
using Eclipse.Misioak;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests
{
    public sealed class HelburuHautaketaTests
    {
        private static MisioLogika Logika()
        {
            return new MisioLogika(new[]
            {
                new MisioFasea("Lehena", new[] { "a", "b" }),
                new MisioFasea("Bigarrena", new[] { "c" }),
            });
        }

        private static List<HelburuHautagaia> Terminalak()
        {
            return new List<HelburuHautagaia>
            {
                new HelburuHautagaia(HelburuMota.Terminala, "a", "A", Vector3.zero),
                new HelburuHautagaia(HelburuMota.Terminala, "b", "B", Vector3.right),
                new HelburuHautagaia(HelburuMota.Terminala, "c", "C", Vector3.forward),
                new HelburuHautagaia(HelburuMota.Irteera, "irteera", "Irteera", Vector3.back),
            };
        }

        private static string[] Idak(IEnumerable<HelburuHautagaia> hautatuak) => hautatuak.Select(h => h.Id).ToArray();

        [Test]
        public void UnekoFasekoTerminalak_Bakarrik()
        {
            Assert.That(Idak(HelburuHautaketa.Hautatu(Logika(), Vector3.zero, Terminalak())), Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void BetetakoTerminala_Desagertu_EtaHurrengoFaseaAgertzenDa()
        {
            MisioLogika logika = Logika();
            logika.Bete("a");
            Assert.That(Idak(HelburuHautaketa.Hautatu(logika, Vector3.zero, Terminalak())), Is.EqualTo(new[] { "b" }));
            logika.Bete("b");
            Assert.That(Idak(HelburuHautaketa.Hautatu(logika, Vector3.zero, Terminalak())), Is.EqualTo(new[] { "c" }));
        }

        [Test]
        public void Irteera_MisioaAmaitzean_Bakarrik()
        {
            MisioLogika logika = Logika();
            Assert.That(Idak(HelburuHautaketa.Hautatu(logika, Vector3.zero, Terminalak())), Does.Not.Contain("irteera"));
            logika.Bete("a");
            logika.Bete("b");
            logika.Bete("c");
            Assert.That(Idak(HelburuHautaketa.Hautatu(logika, Vector3.zero, Terminalak())), Is.EqualTo(new[] { "irteera" }));
        }

        [Test]
        public void Bildumak_HurbilekoBiak_Bakarrik_EtaBildutakoakEz()
        {
            var hautagaiak = new[]
            {
                new HelburuHautagaia(HelburuMota.Bilduma, "1", "D", new Vector3(5, 0, 0)),
                new HelburuHautagaia(HelburuMota.Bilduma, "2", "D", new Vector3(3, 0, 0)),
                new HelburuHautagaia(HelburuMota.Bilduma, "3", "D", new Vector3(4, 0, 0)),
                new HelburuHautagaia(HelburuMota.Bilduma, "bildua", "D", new Vector3(1, 0, 0), false),
                new HelburuHautagaia(HelburuMota.Bilduma, "urrun", "D", new Vector3(HelburuHautaketa.BildumaMuga + 1f, 0, 0)),
            };
            Assert.That(Idak(HelburuHautaketa.Hautatu(Logika(), Vector3.zero, hautagaiak)), Is.EqualTo(new[] { "2", "3" }));
        }

        [Test]
        public void HesiItxia_Hurbil_Bakarrik_EtaIrekiaEz()
        {
            var hautagaiak = new[]
            {
                new HelburuHautagaia(HelburuMota.Blokeoa, "hurbil", "H", new Vector3(5, 0, 0)),
                new HelburuHautagaia(HelburuMota.Blokeoa, "irekia", "H", new Vector3(5, 0, 0), false),
                new HelburuHautagaia(HelburuMota.Blokeoa, "urrun", "H", new Vector3(HelburuHautaketa.BlokeoMuga + 1f, 0, 0)),
            };
            Assert.That(Idak(HelburuHautaketa.Hautatu(Logika(), Vector3.zero, hautagaiak)), Is.EqualTo(new[] { "hurbil" }));
        }

        private static readonly Vector2 Tartea = new Vector2(0.05f, 0.08f);

        [Test]
        public void PantailaBarruan_PuntuanGeratzenDa()
        {
            MarkatzaileLekua lekua = MarkatzaileKokapena.Kalkulatu(new Vector3(0.3f, 0.6f, 10f), Tartea);
            Assert.That(lekua.Barruan, Is.True);
            Assert.That(lekua.Posizioa, Is.EqualTo(new Vector2(0.3f, 0.6f)));
        }

        [Test]
        public void Atzean_EzDaInoizPantailaBarruan_EtaNorabideaAlderantzikatzenDu()
        {
            MarkatzaileLekua lekua = MarkatzaileKokapena.Kalkulatu(new Vector3(0.7f, 0.5f, -3f), Tartea);
            Assert.That(lekua.Barruan, Is.False);
            Assert.That(lekua.Posizioa.x, Is.LessThan(0.5f), "kameraren atzean eta eskuinean proiektatuta: ezkerrera seinalatu behar du");
        }

        [Test]
        public void ZehatzAtzean_BeheraSeinalatzenDu()
        {
            MarkatzaileLekua lekua = MarkatzaileKokapena.Kalkulatu(new Vector3(0.5f, 0.5f, -5f), Tartea);
            Assert.That(lekua.Barruan, Is.False);
            Assert.That(lekua.Posizioa.y, Is.EqualTo(Tartea.y).Within(0.001f));
        }

        [Test]
        public void Ertzean_GeziakBenetakoNorabideaSeinalatzenDu()
        {
            // Angelua: 0 gora, positiboa erlojuaren kontra. Ezkerrera = +90.
            Assert.That(MarkatzaileKokapena.Kalkulatu(new Vector3(2f, 0.5f, 5f), Tartea).Angelua, Is.EqualTo(-90f).Within(0.01f));
            Assert.That(MarkatzaileKokapena.Kalkulatu(new Vector3(-1f, 0.5f, 5f), Tartea).Angelua, Is.EqualTo(90f).Within(0.01f));
            Assert.That(MarkatzaileKokapena.Kalkulatu(new Vector3(0.5f, 2f, 5f), Tartea).Angelua, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void BiraOsoan_EzDaInoizPantailatikKanpoGeratzen()
        {
            var kamera = new GameObject("ProbaKamera").AddComponent<Camera>();
            try
            {
                kamera.aspect = 16f / 9f;
                kamera.fieldOfView = 60f;
                var helburua = new Vector3(0f, 2f, 20f);
                int barruan = 0;
                for (int yaw = 0; yaw < 360; yaw += 5)
                {
                    kamera.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    MarkatzaileLekua lekua = MarkatzaileKokapena.Kalkulatu(kamera.WorldToViewportPoint(helburua), Tartea);
                    Assert.That(lekua.Posizioa.x, Is.InRange(0f, 1f), $"yaw {yaw}");
                    Assert.That(lekua.Posizioa.y, Is.InRange(0f, 1f), $"yaw {yaw}");
                    Vector3 lokala = kamera.transform.InverseTransformPoint(helburua);
                    if (lekua.Barruan)
                    {
                        barruan++;
                        Assert.That(lokala.z, Is.GreaterThan(0f), $"yaw {yaw}: atzean dago eta barruan agertzen da");
                    }
                    else if (Mathf.Abs(lokala.x) > 5f)
                    {
                        // Ertzak helburua dagoen aldea seinalatu behar du.
                        Assert.That(Mathf.Sign(lekua.Posizioa.x - 0.5f), Is.EqualTo(Mathf.Sign(lokala.x)), $"yaw {yaw}: alde okerra");
                    }
                }

                Assert.That(barruan, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(kamera.gameObject);
            }
        }
    }
}
