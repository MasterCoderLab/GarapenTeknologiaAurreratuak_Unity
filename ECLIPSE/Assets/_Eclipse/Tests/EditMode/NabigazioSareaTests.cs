using System.Collections.Generic;
using System.Linq;
using Eclipse.Nabigazioa;
using NUnit.Framework;
using UnityEngine;

namespace Eclipse.Tests
{
    public sealed class NabigazioSareaTests
    {
        /// <summary>Sare laua; "#" gelaxkak horma dira. Alderdiak 8 norabidetan, izkinak moztu gabe. Lehen lerroa iparra da.</summary>
        private static NabigazioSarea Mapa(int blokeoX = -1, params string[] lerroak)
        {
            var nodoak = new List<Vector3>();
            var indizeak = new Dictionary<(int, int), int>();
            int altuera = lerroak.Length;
            for (int l = 0; l < altuera; l++)
            {
                for (int x = 0; x < lerroak[l].Length; x++)
                {
                    if (lerroak[l][x] != '#')
                    {
                        int z = altuera - 1 - l;
                        indizeak[(x, z)] = nodoak.Count;
                        nodoak.Add(new Vector3(x, 0f, z));
                    }
                }
            }

            var alderdiak = new List<SareAlderdia>();
            foreach (KeyValuePair<(int X, int Z), int> n in indizeak)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        (int, int) bestea = (n.Key.X + dx, n.Key.Z + dz);
                        bool diagonala = dx != 0 && dz != 0;
                        if ((dx == 0 && dz == 0) || !indizeak.TryGetValue(bestea, out int b)
                            || (diagonala && (!indizeak.ContainsKey((n.Key.X + dx, n.Key.Z)) || !indizeak.ContainsKey((n.Key.X, n.Key.Z + dz)))))
                        {
                            continue;
                        }

                        bool blokeoa = blokeoX >= 0 && (n.Key.X == blokeoX || n.Key.X + dx == blokeoX);
                        alderdiak.Add(new SareAlderdia(n.Value, b, blokeoa ? 0 : SareAlderdia.BlokeorikEz));
                    }
                }
            }

            return new NabigazioSarea(1f, nodoak, alderdiak, blokeoX >= 0 ? 1 : 0);
        }

        private static int Nodoa(NabigazioSarea sarea, float x, float z) => sarea.Hurbilena(new Vector3(x, 0f, z), 0.1f);

        [Test]
        public void ZuzenekoBidea_LaburrenaDa()
        {
            NabigazioSarea sarea = Mapa(-1, ".....");
            var bidea = new List<int>();
            Assert.That(sarea.BideaBilatu(Nodoa(sarea, 0, 0), Nodoa(sarea, 4, 0), null, bidea), Is.True);
            Assert.That(bidea.Count, Is.EqualTo(5));
            Assert.That(sarea.BideLuzera(bidea), Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void HormaInguratzen_Du_EtaEzDuZeharkatzen()
        {
            NabigazioSarea sarea = Mapa(-1,
                ".....",
                ".###.",
                ".....");
            var bidea = new List<int>();
            Assert.That(sarea.BideaBilatu(Nodoa(sarea, 2, 0), Nodoa(sarea, 2, 2), null, bidea), Is.True);
            foreach (int n in bidea)
            {
                Vector3 p = sarea.Posizioa(n);
                Assert.That(p.z == 1f && p.x >= 1f && p.x <= 3f, Is.False, $"{p} hormaren barruan dago");
            }
        }

        [Test]
        public void KonexiorikEz_EzDuBiderikAsmatzen()
        {
            NabigazioSarea sarea = Mapa(-1, "..#..");
            var bidea = new List<int> { 99 };
            Assert.That(sarea.BideaBilatu(Nodoa(sarea, 0, 0), Nodoa(sarea, 4, 0), null, bidea), Is.False);
            Assert.That(bidea, Is.Empty);
        }

        [Test]
        public void BlokeoItxia_MoztuEginDu_EtaIrekitakoan_BideaItzultzenDa()
        {
            NabigazioSarea sarea = Mapa(2, ".....");
            var bidea = new List<int>();
            int a = Nodoa(sarea, 0, 0);
            int b = Nodoa(sarea, 4, 0);
            Assert.That(sarea.BideaBilatu(a, b, new[] { true }, bidea), Is.False);
            Assert.That(sarea.BideaBilatu(a, b, new[] { false }, bidea), Is.True);
        }

        [Test]
        public void BlokeoItxiarekin_BesteBidetikDoa()
        {
            // Bi pasillo: goikoan blokeoa dago (x=2), baina behekoa ere x=2-tik doa; hemen beste sare bat behar da.
            var nodoak = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(2, 0, 0), new Vector3(1, 0, 1) };
            var alderdiak = new List<SareAlderdia>
            {
                new SareAlderdia(0, 1, 0), new SareAlderdia(1, 2, 0),
                new SareAlderdia(0, 3), new SareAlderdia(3, 2),
            };
            var sarea = new NabigazioSarea(1f, nodoak, alderdiak, 1);
            var bidea = new List<int>();
            Assert.That(sarea.BideaBilatu(0, 2, new[] { false }, bidea), Is.True);
            Assert.That(bidea, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(sarea.BideaBilatu(0, 2, new[] { true }, bidea), Is.True);
            Assert.That(bidea, Is.EqualTo(new[] { 0, 3, 2 }));
        }

        [Test]
        public void AlderdiNorabidedunak_ErorketakEzDuItzulerarik()
        {
            var nodoak = new List<Vector3> { new Vector3(0, 3, 0), new Vector3(1, 0, 0) };
            var sarea = new NabigazioSarea(1f, nodoak, new[] { new SareAlderdia(0, 1) });
            var bidea = new List<int>();
            Assert.That(sarea.BideaBilatu(0, 1, null, bidea), Is.True);
            Assert.That(sarea.BideaBilatu(1, 0, null, bidea), Is.False);
        }

        [Test]
        public void Hurbilena_AltueraGehiagoPisatzenDu_SolairuakEzNahasteko()
        {
            var nodoak = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1.5f, 3, 0) };
            var sarea = new NabigazioSarea(0.5f, nodoak, new SareAlderdia[0]);
            Assert.That(sarea.Hurbilena(new Vector3(0, 3, 0), 12f), Is.EqualTo(1));
            Assert.That(sarea.Hurbilena(new Vector3(0, 3, 0), 1f), Is.EqualTo(-1));
        }

        /// <summary>Bi bide 0-tik 3-ra: goikoa labur eta makurtuta (1), behekoa luzeago eta zutik (2).</summary>
        private static NabigazioSarea MakurSarea(bool goikoaDago = true)
        {
            var nodoak = new List<Vector3>
            {
                new Vector3(0, 0, 0), new Vector3(2, 0, 1), new Vector3(2, 0, -2), new Vector3(4, 0, 0),
            };
            var alderdiak = new List<SareAlderdia> { new SareAlderdia(0, 2), new SareAlderdia(2, 3) };
            if (goikoaDago)
            {
                alderdiak.Add(new SareAlderdia(0, 1));
                alderdiak.Add(new SareAlderdia(1, 3));
            }

            return new NabigazioSarea(1f, nodoak, alderdiak, 0, new[] { false, true, false, false });
        }

        [Test]
        public void Makurtu_ZutikekoBideaLehenesten_Da_KostuAgatik()
        {
            var bidea = new List<int>();
            Assert.That(MakurSarea().BideaBilatu(0, 3, null, bidea), Is.True);
            Assert.That(bidea, Is.EqualTo(new[] { 0, 2, 3 }), "bide laburragoa makurtuta dago eta garestiagoa da");
        }

        [Test]
        public void Makurtu_BideBakarraBada_EzDaEzinezkoa()
        {
            var nodoak = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(2, 0, 0) };
            var sarea = new NabigazioSarea(1f, nodoak, new[] { new SareAlderdia(0, 1), new SareAlderdia(1, 2) }, 0, new[] { false, true, false });
            var bidea = new List<int>();
            Assert.That(sarea.BideaBilatu(0, 2, null, bidea), Is.True);
            Assert.That(bidea.Any(sarea.MakurtuBehar), Is.True);
            Assert.That(sarea.BideLuzera(bidea), Is.EqualTo(2f).Within(0.001f), "luzera metroetan da, ez kostuan");
        }

        [Test]
        public void Makurtu_AtajoakEzDakar_BideakEskatzenEzBadu()
        {
            // Zuzena 1. gelaxkatik doa, makurtuta; zutikeko bideak inguratu egiten du.
            var nodoak = new List<Vector3>
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(2, 0, 0),
                new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(2, 0, 1),
                new Vector3(0, 0, -1), new Vector3(1, 0, -1), new Vector3(2, 0, -1),
            };
            var alderdiak = new List<SareAlderdia>();
            for (int a = 0; a < nodoak.Count; a++)
            {
                for (int b = 0; b < nodoak.Count; b++)
                {
                    if (a != b && Vector3.Distance(nodoak[a], nodoak[b]) < 1.5f)
                    {
                        alderdiak.Add(new SareAlderdia(a, b));
                    }
                }
            }

            var makurtu = new bool[nodoak.Count];
            makurtu[1] = true;
            var sarea = new NabigazioSarea(1f, nodoak, alderdiak, 0, makurtu);
            Assert.That(sarea.LerroaLibre(0, 2, null, false), Is.False);
            Assert.That(sarea.LerroaLibre(0, 2, null), Is.True);
        }

        [Test]
        public void IndizeBaliogabeak_EzDuteHutsEgiten()
        {
            NabigazioSarea sarea = Mapa(-1, "..");
            var bidea = new List<int>();
            Assert.That(sarea.BideaBilatu(-1, 0, null, bidea), Is.False);
            Assert.That(sarea.BideaBilatu(0, 99, null, bidea), Is.False);
        }

        [Test]
        public void Sinplifikatu_Zuzena_BiPuntutanGeratzenDa()
        {
            NabigazioSarea sarea = Mapa(-1, ".......");
            var bidea = new List<int>();
            sarea.BideaBilatu(Nodoa(sarea, 0, 0), Nodoa(sarea, 6, 0), null, bidea);
            var puntuak = new List<Vector3>();
            sarea.Sinplifikatu(bidea, null, puntuak);
            Assert.That(puntuak.Count, Is.EqualTo(2));
        }

        [Test]
        public void Sinplifikatu_EzDuHormaZeharkatzen()
        {
            NabigazioSarea sarea = Mapa(-1,
                ".......",
                "...#...",
                "...#...",
                "...#...",
                ".......");
            var bidea = new List<int>();
            sarea.BideaBilatu(Nodoa(sarea, 0, 2), Nodoa(sarea, 6, 2), null, bidea);
            var puntuak = new List<Vector3>();
            sarea.Sinplifikatu(bidea, null, puntuak);
            Assert.That(puntuak.Count, Is.GreaterThan(2));
            for (int i = 1; i < puntuak.Count; i++)
            {
                // Tarte bakoitza laginduta: ez da inoiz hormako gelaxka batetik pasatzen.
                for (float t = 0f; t <= 1f; t += 0.02f)
                {
                    Vector3 p = Vector3.Lerp(puntuak[i - 1], puntuak[i], t);
                    bool horman = Mathf.RoundToInt(p.x) == 3 && Mathf.RoundToInt(p.z) >= 1 && Mathf.RoundToInt(p.z) <= 3;
                    Assert.That(horman, Is.False, $"{p} horman dago");
                }
            }
        }

        [Test]
        public void Sinplifikatu_EzDuOztopoaUkitzen()
        {
            NabigazioSarea sarea = Mapa(-1,
                ".........",
                ".........",
                "....#....",
                ".........",
                ".........");
            var bidea = new List<int>();
            sarea.BideaBilatu(Nodoa(sarea, 0, 2), Nodoa(sarea, 8, 2), null, bidea);
            var puntuak = new List<Vector3>();
            sarea.Sinplifikatu(bidea, null, puntuak);
            for (int i = 1; i < puntuak.Count; i++)
            {
                for (float t = 0f; t <= 1f; t += 0.01f)
                {
                    Vector3 p = Vector3.Lerp(puntuak[i - 1], puntuak[i], t);
                    float dx = Mathf.Max(3.5f - p.x, 0f, p.x - 4.5f);
                    float dz = Mathf.Max(1.5f - p.z, 0f, p.z - 2.5f);
                    Assert.That(new Vector2(dx, dz).magnitude, Is.GreaterThan(0.4f), $"{p} oztopoa ukitzen du");
                }
            }
        }

        [Test]
        public void Sinplifikatu_BesteSolairukoZoruakEzDuBalio()
        {
            // Pasilloa z=0-n; z=1 gelaxka 3 m gorago dago (pasabidea): alboko gelaxka gisa ez du balio.
            var nodoak = new List<Vector3>();
            var alderdiak = new List<SareAlderdia>();
            for (int x = 0; x < 5; x++)
            {
                nodoak.Add(new Vector3(x, 0f, 0f));
                if (x > 0)
                {
                    alderdiak.Add(new SareAlderdia(x - 1, x));
                }
            }

            nodoak.Add(new Vector3(2f, 3f, 1f));
            var sarea = new NabigazioSarea(1f, nodoak, alderdiak);
            Assert.That(sarea.LerroaLibre(0, 4, null), Is.True);
            Assert.That(sarea.Hurbilena(new Vector3(2f, 3f, 1f), 0.5f), Is.EqualTo(5));
        }

        [Test]
        public void Sinplifikatu_EzDuBlokeoItxirikZeharkatzen()
        {
            NabigazioSarea sarea = Mapa(3, "......");
            Assert.That(sarea.LerroaLibre(Nodoa(sarea, 0, 0), Nodoa(sarea, 5, 0), new[] { true }), Is.False);
            Assert.That(sarea.LerroaLibre(Nodoa(sarea, 0, 0), Nodoa(sarea, 5, 0), new[] { false }), Is.True);
        }

        [Test]
        public void BilaketaBerriak_EzDuAurrekoEmaitzarikErabiltzen()
        {
            NabigazioSarea sarea = Mapa(-1, ".....");
            var bidea = new List<int>();
            sarea.BideaBilatu(Nodoa(sarea, 0, 0), Nodoa(sarea, 4, 0), null, bidea);
            sarea.BideaBilatu(Nodoa(sarea, 4, 0), Nodoa(sarea, 2, 0), null, bidea);
            Assert.That(bidea.Select(n => sarea.Posizioa(n).x), Is.EqualTo(new[] { 4f, 3f, 2f }));
        }
    }
}
