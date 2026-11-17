using System;
using Eclipse.Borroka;
using Eclipse.Npc;
using NUnit.Framework;

namespace Eclipse.Tests
{
    public sealed class NpcEgoeraMakinaTests
    {
        private static NpcPertzepzioa Mehatxua(float distantzia, bool ikusten = true) =>
            new NpcPertzepzioa(float.PositiveInfinity, false, distantzia, ikusten, false);

        [Test]
        public void Munstroa_EzDa_NpcMakina_EtaEtsaiaDa()
        {
            Assert.Throws<ArgumentException>(() => new NpcEgoeraMakina(NpcRola.Munstroa, false));
            Assert.That(NpcEgoeraMakina.Etsaia(NpcRola.Munstroa), Is.True);
            Assert.That(NpcEgoeraMakina.Etsaia(NpcRola.Zientzialaria), Is.False);
            Assert.That(NpcEgoeraMakina.Etsaia(NpcRola.Soldadua), Is.False);
        }

        [Test]
        public void Lasai_LanLekuarekin_Lanean_EtaGabe_Patruila()
        {
            var zientzialaria = new NpcEgoeraMakina(NpcRola.Zientzialaria, lanLekuaDu: true);
            var soldadua = new NpcEgoeraMakina(NpcRola.Soldadua, lanLekuaDu: false);
            NpcErabakia z = zientzialaria.Eguneratu(0.1f, NpcPertzepzioa.Lasaia());
            NpcErabakia s = soldadua.Eguneratu(0.1f, NpcPertzepzioa.Lasaia());
            Assert.That(z.Egoera, Is.EqualTo(NpcEgoera.Lanean));
            Assert.That(z.Abiadura, Is.EqualTo(NpcAbiadura.Geldi));
            Assert.That(s.Egoera, Is.EqualTo(NpcEgoera.Patruila));
            Assert.That(s.Abiadura, Is.EqualTo(NpcAbiadura.Oinez));
        }

        [Test]
        public void JokalariaHurbil_Begiratu_EtaGeroLanera()
        {
            var m = new NpcEgoeraMakina(NpcRola.Zientzialaria, true);
            NpcErabakia e = m.Eguneratu(0.1f, NpcPertzepzioa.Lasaia(2f, true));
            Assert.That(e.Egoera, Is.EqualTo(NpcEgoera.Begiratu));
            Assert.That(e.JokalariaBegiratu, Is.True);
            Assert.That(e.EgoeraAldatu, Is.True);
            // Urrundu ondoren, begiratzen jarraitzen du denbora batez.
            Assert.That(m.Eguneratu(1f, NpcPertzepzioa.Lasaia(8f, true)).Egoera, Is.EqualTo(NpcEgoera.Begiratu));
            Assert.That(m.Eguneratu(1f, NpcPertzepzioa.Lasaia(8f, true)).Egoera, Is.EqualTo(NpcEgoera.Lanean));
        }

        [Test]
        public void JokalariaHurbil_BainaIkusiGabe_EzDuBegiratzen()
        {
            var m = new NpcEgoeraMakina(NpcRola.Soldadua, false);
            Assert.That(m.Eguneratu(0.1f, NpcPertzepzioa.Lasaia(2f, false)).Egoera, Is.EqualTo(NpcEgoera.Patruila));
        }

        [Test]
        public void Zientzialaria_MunstroaIkusita_IhesKorrika_EtaGeroBueltatu()
        {
            var m = new NpcEgoeraMakina(NpcRola.Zientzialaria, true);
            NpcErabakia e = m.Eguneratu(0.1f, Mehatxua(10f));
            Assert.That(e.Egoera, Is.EqualTo(NpcEgoera.Ihes));
            Assert.That(e.Abiadura, Is.EqualTo(NpcAbiadura.Korrika));
            Assert.That(e.Tiro, Is.False);
            // Mehatxua desagertuta: IkaraIraupena igaro arte ihesi.
            Assert.That(m.Eguneratu(NpcEgoeraMakina.IkaraIraupena - 0.5f, NpcPertzepzioa.Lasaia()).Egoera, Is.EqualTo(NpcEgoera.Ihes));
            Assert.That(m.Eguneratu(1f, NpcPertzepzioa.Lasaia()).Egoera, Is.EqualTo(NpcEgoera.Lanean));
        }

        [Test]
        public void Zientzialaria_MunstroaUrrunEdoIkusiGabe_EzDuIhesEgiten()
        {
            var m = new NpcEgoeraMakina(NpcRola.Zientzialaria, true);
            Assert.That(m.Eguneratu(0.1f, Mehatxua(NpcEgoeraMakina.IkaraDistantzia + 1f)).Egoera, Is.EqualTo(NpcEgoera.Lanean));
            Assert.That(m.Eguneratu(0.1f, Mehatxua(3f, ikusten: false)).Egoera, Is.EqualTo(NpcEgoera.Lanean));
        }

        [Test]
        public void Zientzialaria_TiroHotsa_Ihes()
        {
            var m = new NpcEgoeraMakina(NpcRola.Zientzialaria, true);
            var alarma = new NpcPertzepzioa(float.PositiveInfinity, false, float.PositiveInfinity, false, true);
            Assert.That(m.Eguneratu(0.1f, alarma).Egoera, Is.EqualTo(NpcEgoera.Ihes));
        }

        [Test]
        public void Soldadua_Munstroa_Zaindu_EtaTiroTartearekin()
        {
            var m = new NpcEgoeraMakina(NpcRola.Soldadua, false);
            NpcErabakia lehena = m.Eguneratu(0.1f, Mehatxua(10f));
            Assert.That(lehena.Egoera, Is.EqualTo(NpcEgoera.Zaindu));
            Assert.That(lehena.MehatxuaBegiratu, Is.True);
            Assert.That(lehena.Abiadura, Is.EqualTo(NpcAbiadura.Geldi));
            Assert.That(lehena.Tiro, Is.True);
            Assert.That(m.Eguneratu(0.3f, Mehatxua(10f)).Tiro, Is.False);
            Assert.That(m.Eguneratu(NpcEgoeraMakina.TiroTartea, Mehatxua(10f)).Tiro, Is.True);
        }

        [Test]
        public void Soldadua_MehatxuaGaldu_ZainduDenbora_EtaPatruilara()
        {
            var m = new NpcEgoeraMakina(NpcRola.Soldadua, false);
            m.Eguneratu(0.1f, Mehatxua(8f));
            NpcErabakia e = m.Eguneratu(1f, NpcPertzepzioa.Lasaia());
            Assert.That(e.Egoera, Is.EqualTo(NpcEgoera.Zaindu));
            Assert.That(e.Tiro, Is.False, "Mehatxua ikusi gabe ez du tirorik egiten");
            Assert.That(m.Eguneratu(NpcEgoeraMakina.ZainduIraupena, NpcPertzepzioa.Lasaia()).Egoera, Is.EqualTo(NpcEgoera.Patruila));
        }

        [Test]
        public void Soldadua_TiroHotsa_ZainduBainaTirorikEz()
        {
            var m = new NpcEgoeraMakina(NpcRola.Soldadua, false);
            NpcErabakia e = m.Eguneratu(0.1f, new NpcPertzepzioa(5f, true, float.PositiveInfinity, false, true));
            Assert.That(e.Egoera, Is.EqualTo(NpcEgoera.Zaindu));
            Assert.That(e.Tiro, Is.False);
        }

        [Test]
        public void InoizEzDu_TiroEgiten_MehatxurikGabe()
        {
            // Jokalaria edozein distantziatan eta ikusita: zientzialariak eta soldaduak ez dute inoiz tirorik egiten.
            foreach (NpcRola rola in new[] { NpcRola.Zientzialaria, NpcRola.Soldadua })
            {
                var m = new NpcEgoeraMakina(rola, false);
                for (int i = 0; i < 100; i++)
                {
                    var p = new NpcPertzepzioa(i * 0.2f, true, float.PositiveInfinity, false, i % 7 == 0);
                    Assert.That(m.Eguneratu(0.25f, p).Tiro, Is.False, $"{rola}, {i}");
                }
            }
        }
    }

    public sealed class DesmenbratzeLogikaTests
    {
        [Test]
        public void Hankak_Herren_EtaNarrastaka()
        {
            var d = new DesmenbratzeLogika();
            Assert.That(d.AbiaduraFaktorea, Is.EqualTo(1f));
            Assert.That(d.Galdu(KalteZona.HankaEzkerra), Is.True);
            Assert.That(d.AbiaduraFaktorea, Is.EqualTo(0.5f));
            Assert.That(d.Narrastaka, Is.False);
            Assert.That(d.Galdu(KalteZona.HankaEskuina), Is.True);
            Assert.That(d.AbiaduraFaktorea, Is.EqualTo(0.3f));
            Assert.That(d.Narrastaka, Is.True);
            Assert.That(d.ErasoFaktorea, Is.EqualTo(1f));
        }

        [Test]
        public void Besoak_ErasoaAhultzen_Dute()
        {
            var d = new DesmenbratzeLogika();
            d.Galdu(KalteZona.BesoEskuina);
            Assert.That(d.ErasoFaktorea, Is.EqualTo(0.6f));
            d.Galdu(KalteZona.BesoEzkerra);
            Assert.That(d.ErasoFaktorea, Is.EqualTo(0.25f));
            Assert.That(d.AbiaduraFaktorea, Is.EqualTo(1f));
        }

        [Test]
        public void BuruaEtaEnborra_EzDira_Bereizten_EtaBikoitzak_EzDuEzerAldatzen()
        {
            var d = new DesmenbratzeLogika();
            Assert.That(d.Galdu(KalteZona.Burua), Is.False);
            Assert.That(d.Galdu(KalteZona.Enborra), Is.False);
            Assert.That(d.Galdu(KalteZona.HankaEzkerra), Is.True);
            Assert.That(d.Galdu(KalteZona.HankaEzkerra), Is.False);
            Assert.That(d.HankaGalduak, Is.EqualTo(1));
            Assert.That(d.Galduak.Count, Is.EqualTo(1));
        }
    }
}
