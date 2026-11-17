using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Sektoreen atrezzoa (A3, C blokea): pieza propioak, Unity-ren primitiboekin kitbash-a eta materialeko malla bakarra.
    /// Konbentzioa: jatorria lurrean, oinatzaren erdian; +Z = aurrealdea (gelara begira); atzealdea z = -Sakonera/2 (hormaren kontra).
    /// Hormakoek (Horma) ez dute kolisiorik: ezin dute biderik itxi. Lurrekoek BoxCollider soil bat.
    /// </summary>
    internal static class AtrezzoPiezak
    {
        public const string DatuKarpeta = "Assets/_Eclipse/Datuak/Atrezzo";
        public const string PiezaKarpeta = "Assets/Resources/Atrezzo/Piezak";

        internal sealed class Zatia
        {
            public string Mota;
            public Vector3 P;
            public Vector3 R;
            public Vector3 S;
            public string Mat;
        }

        internal sealed class PiezaDef
        {
            public string Izena;
            public string Sektorea;
            public bool Horma;
            public bool Korridorea;
            public bool Lurra;
            public Vector3 Oina;
            public Zatia[] Zatiak;
        }

        /// <summary>AssetDatabase-eko karpeta-katea ziurtatu (Directory.CreateDirectory ez da nahikoa CreateAsset/SaveAsPrefabAsset-entzat).</summary>
        public static void KarpetaZiurtatu(string karpeta)
        {
            karpeta = karpeta.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(karpeta))
            {
                return;
            }

            string gurasoa = Path.GetDirectoryName(karpeta).Replace('\\', '/');
            KarpetaZiurtatu(gurasoa);
            AssetDatabase.CreateFolder(gurasoa, Path.GetFileName(karpeta));
        }

        internal static Zatia K(float x, float y, float z, float sx, float sy, float sz, string mat, float rx = 0f, float ry = 0f, float rz = 0f) =>
            new Zatia { Mota = "Cube", P = new Vector3(x, y, z), S = new Vector3(sx, sy, sz), R = new Vector3(rx, ry, rz), Mat = mat };

        /// <summary>Zilindroa: d diametroa, h altuera (Y ardatzean, biraketarik gabe).</summary>
        internal static Zatia Z(float x, float y, float z, float d, float h, string mat, float rx = 0f, float ry = 0f, float rz = 0f) =>
            new Zatia { Mota = "Cylinder", P = new Vector3(x, y, z), S = new Vector3(d, h * 0.5f, d), R = new Vector3(rx, ry, rz), Mat = mat };

        internal static Zatia E(float x, float y, float z, float sx, float sy, float sz, string mat) =>
            new Zatia { Mota = "Sphere", P = new Vector3(x, y, z), S = new Vector3(sx, sy, sz), Mat = mat };

        /// <summary>Laukia (aurrealdea +Z-ra begira, edo lurrean gora: lurra=true).</summary>
        internal static Zatia Q(float x, float y, float z, float sx, float sy, string mat, bool lurra = false, float ry = 0f) =>
            new Zatia { Mota = "Quad", P = new Vector3(x, y, z), S = new Vector3(sx, sy, 1f), R = lurra ? new Vector3(90f, ry, 0f) : new Vector3(0f, 180f + ry, 0f), Mat = mat };

        private static IEnumerable<Zatia> Hankak(float w, float d, float h, float lodiera, string mat)
        {
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sz in new[] { -1f, 1f })
                {
                    yield return K(sx * (w * 0.5f - lodiera), h * 0.5f, sz * (d * 0.5f - lodiera), lodiera, h, lodiera, mat);
                }
            }
        }

        /// <summary>Zain/tentakulu organikoak: segmentu txikien kateak, uhinduta, hormaren kontra (zilindro zuzenen ordez).</summary>
        private static void Zainak(List<Zatia> l, System.Random r, int kopurua, float x0, float x1, float y0, float y1, float z)
        {
            for (int k = 0; k < kopurua; k++)
            {
                float x = Mathf.Lerp(x0, x1, (k + 0.5f) / kopurua) + ((float)r.NextDouble() - 0.5f) * 0.2f;
                float fasea = (float)r.NextDouble() * 6f;
                float lodiera = 0.05f + (float)r.NextDouble() * 0.05f;
                int segmentuak = 14;
                Vector3 aurrekoa = new Vector3(x, y0, z);
                for (int i = 1; i <= segmentuak; i++)
                {
                    float t = (float)i / segmentuak;
                    Vector3 p = new Vector3(x + Mathf.Sin(t * 5f + fasea) * 0.18f, Mathf.Lerp(y0, y1 * (0.7f + 0.3f * (float)r.NextDouble()), t), z);
                    Vector3 erdia = (aurrekoa + p) * 0.5f;
                    Vector3 d = p - aurrekoa;
                    float angelua = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                    float lod = lodiera * (1.1f - t * 0.6f);
                    l.Add(new Zatia { Mota = "Sphere", P = erdia, R = new Vector3(0f, 0f, -angelua), S = new Vector3(lod, d.magnitude * 1.25f, lod * 0.8f), Mat = i % 4 == 0 ? "zainak" : "haragia" });
                    aurrekoa = p;
                }
            }
        }

        /// <summary>FNV-1a: hash egonkorra prozesu eta exekuzio guztietan (string.GetHashCode ez da beti).</summary>
        public static int Fnv(string testua)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in testua)
                {
                    h = (h ^ c) * 16777619;
                }

                return (int)h;
            }
        }

        /// <summary>Definizioaren laburpena: aldatzen bada, tresnak pieza berreraikitzen du (idenpotentzia).</summary>
        public static string Bertsioa(PiezaDef def)
        {
            unchecked
            {
                int h = 17;
                foreach (Zatia z in def.Zatiak)
                {
                    h = h * 31 + Fnv($"{z.Mota}{z.P:F4}{z.R:F3}{z.S:F4}{z.Mat}");
                }

                h = h * 31 + Fnv($"{def.Oina:F3}{def.Horma}{def.Lurra}");
                return (h & 0x7fffffff).ToString("x8");
            }
        }

        /// <summary>Pieza guztiak. Izenak euskaraz; sektorea: M1 laborategia, M2 energia, M3 erreaktorea.</summary>
        public static List<PiezaDef> Definizioak()
        {
            var l = new List<PiezaDef>();

            // ---------- M1: LABORATEGIA (zuria, ziana, beira, garbia) ----------
            l.Add(new PiezaDef
            {
                Izena = "PF_At_LabMahaia", Sektorea = "M1", Oina = new Vector3(2f, 1.25f, 0.9f),
                Zatiak = new[]
                {
                    K(0, 0.9f, 0, 2f, 0.06f, 0.9f, "zuria"), K(0, 0.25f, 0, 1.9f, 0.04f, 0.8f, "altzairua"),
                    K(0, 0.6f, -0.43f, 1.9f, 0.6f, 0.03f, "zuria"),
                    // mikroskopioa
                    K(-0.55f, 0.95f, 0.05f, 0.22f, 0.04f, 0.28f, "iluna"), K(-0.55f, 1.08f, -0.05f, 0.05f, 0.24f, 0.05f, "iluna", 15f),
                    K(-0.55f, 1.2f, 0.02f, 0.08f, 0.08f, 0.18f, "iluna", -20f), Z(-0.55f, 1.12f, 0.12f, 0.04f, 0.12f, "altzairua"),
                    // probetak
                    Z(0.1f, 1.0f, 0.15f, 0.08f, 0.18f, "beira"), Z(0.1f, 0.97f, 0.15f, 0.065f, 0.1f, "likido_berdea"),
                    Z(0.25f, 0.99f, 0.1f, 0.07f, 0.15f, "beira"), Z(0.25f, 0.96f, 0.1f, 0.055f, 0.08f, "likido_ziana"),
                    Z(0.38f, 1.02f, 0.2f, 0.06f, 0.22f, "beira"), Z(0.38f, 0.98f, 0.2f, 0.045f, 0.12f, "likido_berdea"),
                    // pantaila txikia
                    K(0.7f, 1.12f, -0.2f, 0.5f, 0.32f, 0.04f, "iluna", -10f), Q(0.7f, 1.12f, -0.175f, 0.46f, 0.28f, "pantaila", false),
                    K(0.7f, 0.95f, -0.25f, 0.12f, 0.06f, 0.12f, "iluna"),
                }.Concat(Hankak(2f, 0.9f, 0.9f, 0.05f, "altzairua")).ToArray(),
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_LaginTankea", Sektorea = "M1", Oina = new Vector3(0.95f, 2.2f, 0.95f),
                Zatiak = new[]
                {
                    Z(0, 0.15f, 0, 0.95f, 0.3f, "altzairua"), Z(0, 1.05f, 0, 0.8f, 1.5f, "beira"), Z(0, 0.95f, 0, 0.72f, 1.25f, "likido_berdea"),
                    E(0, 1.05f, 0, 0.28f, 0.5f, 0.24f, "haragia"), Z(0, 1.95f, 0, 0.95f, 0.22f, "altzairua"), Z(0, 2.1f, 0, 0.5f, 0.1f, "iluna"),
                    K(0.42f, 1.05f, 0, 0.04f, 1.6f, 0.04f, "iluna"), K(-0.42f, 1.05f, 0, 0.04f, 1.6f, 0.04f, "iluna"), K(0, 1.05f, -0.42f, 0.04f, 1.6f, 0.04f, "iluna"),
                    Z(0, 0.32f, 0, 0.82f, 0.03f, "argi_ziana"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Erakustokia", Sektorea = "M1", Oina = new Vector3(1.4f, 1.85f, 0.7f),
                Zatiak = new[]
                {
                    K(0, 0.4f, 0, 1.4f, 0.8f, 0.7f, "zuria"), K(0, 1.25f, 0, 1.36f, 0.9f, 0.66f, "beira"), K(0, 1.73f, 0, 1.4f, 0.06f, 0.7f, "zuria"),
                    K(0, 1.69f, 0, 1.3f, 0.02f, 0.6f, "argi_ziana"), E(-0.3f, 0.95f, 0, 0.32f, 0.22f, 0.26f, "haragia"), E(0.3f, 0.92f, 0.05f, 0.18f, 0.14f, 0.16f, "haragia"),
                    K(0, 0.6f, 0.36f, 0.5f, 0.12f, 0.01f, "pantaila"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_OheMedikoa", Sektorea = "M1", Oina = new Vector3(2.0f, 1.6f, 0.95f),
                Zatiak = new[]
                {
                    K(0, 0.6f, 0.05f, 1.9f, 0.08f, 0.85f, "altzairua"), K(0, 0.7f, 0.05f, 1.85f, 0.12f, 0.8f, "zuria"), K(-0.75f, 0.82f, 0.05f, 0.35f, 0.1f, 0.6f, "zuria"),
                    K(0.85f, 1.25f, -0.38f, 0.04f, 1.2f, 0.04f, "altzairua"), K(0.85f, 1.55f, -0.25f, 0.45f, 0.3f, 0.05f, "iluna"), Q(0.85f, 1.55f, -0.22f, 0.4f, 0.25f, "pantaila"),
                }.Concat(Hankak(1.9f, 0.85f, 0.56f, 0.05f, "altzairua")).ToArray(),
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Apalategia", Sektorea = "M1", Oina = new Vector3(1.6f, 2.0f, 0.45f),
                Zatiak = new[]
                {
                    K(-0.77f, 1f, 0, 0.05f, 2f, 0.45f, "altzairua"), K(0.77f, 1f, 0, 0.05f, 2f, 0.45f, "altzairua"),
                    K(0, 0.1f, 0, 1.5f, 0.03f, 0.43f, "zuria"), K(0, 0.65f, 0, 1.5f, 0.03f, 0.43f, "zuria"), K(0, 1.2f, 0, 1.5f, 0.03f, 0.43f, "zuria"), K(0, 1.75f, 0, 1.5f, 0.03f, 0.43f, "zuria"),
                    K(-0.45f, 0.24f, 0, 0.35f, 0.25f, 0.35f, "zuria"), K(0.2f, 0.22f, 0, 0.4f, 0.2f, 0.3f, "iluna"),
                    Z(-0.5f, 0.76f, 0.05f, 0.08f, 0.2f, "beira"), Z(-0.35f, 0.76f, 0.05f, 0.08f, 0.2f, "beira"), Z(-0.2f, 0.76f, 0.05f, 0.08f, 0.2f, "likido_ziana"),
                    K(0.4f, 0.8f, 0, 0.5f, 0.28f, 0.35f, "zuria"), K(-0.2f, 1.33f, 0, 0.8f, 0.24f, 0.35f, "iluna"), K(0.5f, 1.88f, 0, 0.3f, 0.22f, 0.3f, "zuria"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Kontsola", Sektorea = "M1", Oina = new Vector3(1.6f, 1.35f, 0.8f),
                Zatiak = new[]
                {
                    K(0, 0.42f, -0.05f, 1.6f, 0.84f, 0.7f, "zuria"), K(0, 0.88f, 0.12f, 1.6f, 0.06f, 0.5f, "iluna", -12f), Q(0, 0.915f, 0.12f, 1.4f, 0.4f, "teklatua", true),
                    K(-0.4f, 1.15f, -0.3f, 0.7f, 0.45f, 0.05f, "iluna", -8f), Q(-0.4f, 1.15f, -0.27f, 0.64f, 0.4f, "pantaila", false, 8f),
                    K(0.4f, 1.15f, -0.3f, 0.7f, 0.45f, 0.05f, "iluna", -8f), Q(0.4f, 1.15f, -0.27f, 0.64f, 0.4f, "pantaila", false, -8f),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_HormaMonitorea", Sektorea = "M1", Horma = true, Korridorea = true, Oina = new Vector3(1.6f, 2.1f, 0.1f),
                Zatiak = new[] { K(0, 1.65f, 0, 1.6f, 0.95f, 0.08f, "iluna"), Q(0, 1.65f, 0.045f, 1.5f, 0.85f, "pantaila"), K(0.72f, 1.18f, 0.03f, 0.06f, 0.06f, 0.04f, "argi_ziana") },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Seinalea_Bioarriskua", Sektorea = "M1", Horma = true, Korridorea = true, Oina = new Vector3(1.4f, 2.2f, 0.06f),
                Zatiak = new[] { K(0, 1.85f, 0, 1.44f, 0.4f, 0.03f, "zuria"), Q(0, 1.85f, 0.02f, 1.4f, 0.35f, "seinalea_bioarriskua") },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Seinalea_Protokoloa", Sektorea = "M1", Horma = true, Korridorea = true, Oina = new Vector3(1.4f, 2.2f, 0.06f),
                Zatiak = new[] { K(0, 1.85f, 0, 1.44f, 0.4f, 0.03f, "zuria"), Q(0, 1.85f, 0.02f, 1.4f, 0.35f, "seinalea_protokoloa") },
            });

            // ---------- M2: ENERGIA (horia, anbarra, makina astuna, larrialdia) ----------
            var sorgailua = new List<Zatia>
            {
                K(0, 0.15f, 0, 2.3f, 0.3f, 2.3f, "iluna"), Z(0, 1.25f, 0, 1.9f, 1.9f, "horia"), E(0, 2.2f, 0, 1.9f, 0.7f, 1.9f, "altzairua"),
                Z(0, 1.6f, 0, 1.96f, 0.1f, "argi_anbarra"), Z(0, 2.55f, 0, 0.35f, 0.3f, "iluna"), K(0, 0.9f, 1.0f, 0.9f, 0.5f, 0.1f, "marrak"),
                Z(0.9f, 1.2f, -0.9f, 0.25f, 2.4f, "altzairua"), Z(-0.9f, 1.2f, -0.9f, 0.25f, 2.4f, "altzairua"),
            };
            for (int i = 0; i < 6; i++)
            {
                sorgailua.Add(Z(0, 0.5f + i * 0.17f, 0, 2.15f, 0.04f, "iluna"));
            }

            l.Add(new PiezaDef { Izena = "PF_At_Sorgailua", Sektorea = "M2", Oina = new Vector3(2.4f, 2.7f, 2.4f), Zatiak = sorgailua.ToArray() });
            var trafoa = new List<Zatia>
            {
                K(0, 0.9f, 0, 1.3f, 1.8f, 0.9f, "berde_grisa"), K(0, 1.85f, 0, 1.36f, 0.1f, 0.96f, "iluna"),
                Z(-0.4f, 2.1f, 0, 0.16f, 0.4f, "isolatzailea"), Z(0, 2.1f, 0, 0.16f, 0.4f, "isolatzailea"), Z(0.4f, 2.1f, 0, 0.16f, 0.4f, "isolatzailea"),
                K(0, 1.2f, 0.46f, 0.5f, 0.32f, 0.01f, "seinalea_tentsioa"),
            };
            for (int i = 0; i < 7; i++)
            {
                trafoa.Add(K(0.7f, 0.25f + i * 0.22f, 0, 0.12f, 0.03f, 0.8f, "altzairua"));
                trafoa.Add(K(-0.7f, 0.25f + i * 0.22f, 0, 0.12f, 0.03f, 0.8f, "altzairua"));
            }

            l.Add(new PiezaDef { Izena = "PF_At_Transformadorea", Sektorea = "M2", Oina = new Vector3(1.6f, 2.3f, 1.0f), Zatiak = trafoa.ToArray() });
            var koadroa = new List<Zatia> { K(0, 1.0f, 0, 1.2f, 2.0f, 0.5f, "altzairua"), K(0, 1.0f, 0.255f, 1.1f, 1.8f, 0.01f, "iluna") };
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    string m = (r + c) % 3 == 0 ? "argi_gorria" : (r + c) % 3 == 1 ? "argi_anbarra" : "argi_berdea";
                    koadroa.Add(K(-0.4f + c * 0.2f, 1.45f + r * 0.12f, 0.265f, 0.06f, 0.05f, 0.02f, m));
                }
            }

            koadroa.Add(Z(-0.25f, 0.95f, 0.27f, 0.22f, 0.02f, "pantaila", 90f));
            koadroa.Add(Z(0.25f, 0.95f, 0.27f, 0.22f, 0.02f, "pantaila", 90f));
            koadroa.Add(K(0, 0.45f, 0.27f, 0.9f, 0.18f, 0.02f, "marrak"));
            l.Add(new PiezaDef { Izena = "PF_At_KontrolKoadroa", Sektorea = "M2", Oina = new Vector3(1.2f, 2.0f, 0.55f), Zatiak = koadroa.ToArray() });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Barrera", Sektorea = "M2", Oina = new Vector3(1.6f, 1.05f, 0.45f),
                Zatiak = new[]
                {
                    K(-0.7f, 0.5f, 0, 0.08f, 1.0f, 0.08f, "iluna"), K(0.7f, 0.5f, 0, 0.08f, 1.0f, 0.08f, "iluna"), K(0, 0.8f, 0, 1.5f, 0.28f, 0.04f, "marrak"),
                    K(0, 0.4f, 0, 1.5f, 0.28f, 0.04f, "marrak"), K(-0.7f, 0.03f, 0, 0.4f, 0.06f, 0.45f, "iluna"), K(0.7f, 0.03f, 0, 0.4f, 0.06f, 0.45f, "iluna"),
                    E(-0.7f, 1.04f, 0, 0.1f, 0.1f, 0.1f, "argi_anbarra"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_HornidurKaxak", Sektorea = "M2", Oina = new Vector3(1.5f, 1.3f, 1.0f),
                Zatiak = new[]
                {
                    K(-0.35f, 0.3f, 0, 0.8f, 0.6f, 0.9f, "berdea_kaxa"), K(0.42f, 0.25f, 0.05f, 0.65f, 0.5f, 0.8f, "berdea_kaxa", 0, 8f),
                    K(-0.3f, 0.9f, 0.02f, 0.7f, 0.6f, 0.8f, "berdea_kaxa", 0, -6f), K(-0.35f, 0.3f, 0.455f, 0.5f, 0.15f, 0.01f, "marrak"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_HormaHodiak", Sektorea = "M2", Horma = true, Korridorea = true, Oina = new Vector3(4f, 2.8f, 0.4f),
                Zatiak = new[]
                {
                    Z(0, 2.25f, 0, 0.22f, 4f, "horia", 0, 0, 90f), Z(0, 2.55f, 0.02f, 0.16f, 4f, "altzairua", 0, 0, 90f),
                    K(-1.5f, 2.4f, -0.12f, 0.08f, 0.6f, 0.15f, "iluna"), K(1.5f, 2.4f, -0.12f, 0.08f, 0.6f, 0.15f, "iluna"),
                    Z(0.6f, 2.25f, 0.14f, 0.3f, 0.04f, "gorria", 90f), Z(0.6f, 2.25f, 0.1f, 0.06f, 0.1f, "iluna", 90f),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_KableSorta", Sektorea = "M2", Horma = true, Korridorea = true, Oina = new Vector3(3.5f, 0.2f, 0.3f),
                Zatiak = new[]
                {
                    Z(0, 0.05f, -0.05f, 0.1f, 3.5f, "kautxua", 0, 0, 90f), Z(0, 0.05f, 0.06f, 0.08f, 3.4f, "kautxua", 0, 0, 90f),
                    Z(0.2f, 0.13f, 0, 0.07f, 3.2f, "horia", 0, 0, 90f), K(-1.4f, 0.08f, 0, 0.12f, 0.18f, 0.3f, "iluna"), K(1.4f, 0.08f, 0, 0.12f, 0.18f, 0.3f, "iluna"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Seinalea_Tentsioa", Sektorea = "M2", Horma = true, Korridorea = true, Oina = new Vector3(1.4f, 2.2f, 0.06f),
                Zatiak = new[] { K(0, 1.85f, 0, 1.44f, 0.4f, 0.03f, "horia"), Q(0, 1.85f, 0.02f, 1.4f, 0.35f, "seinalea_tentsioa") },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Seinalea_Ebakuazioa", Sektorea = "M2", Horma = true, Korridorea = true, Oina = new Vector3(1.4f, 2.4f, 0.2f),
                Zatiak = new[]
                {
                    K(0, 1.85f, 0, 1.44f, 0.4f, 0.03f, "iluna"), Q(0, 1.85f, 0.02f, 1.4f, 0.35f, "seinalea_ebakuazioa"),
                    Z(0, 2.25f, 0.08f, 0.18f, 0.14f, "argi_gorria", 90f),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Erretura", Sektorea = "M2", Horma = true, Korridorea = true, Lurra = true, Oina = new Vector3(2.2f, 0.05f, 2.2f),
                Zatiak = new[] { Q(0, 0.012f, 0, 2.2f, 2.2f, "erretura", true) },
            });

            // ---------- M3: ERREAKTOREA (gorria, haragia, hondamena, kutsadura) ----------
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Hondakinak", Sektorea = "M3", Oina = new Vector3(2.0f, 1.0f, 1.4f),
                Zatiak = new[]
                {
                    K(-0.3f, 0.3f, -0.2f, 1.2f, 0.06f, 0.8f, "iluna", 25f, 10f, 15f), K(0.4f, 0.25f, 0.1f, 0.9f, 0.05f, 0.7f, "altzairua", -15f, -20f, 30f),
                    K(0.6f, 0.15f, -0.3f, 0.4f, 0.3f, 0.4f, "zuria", 0, 35f, 10f), K(-0.7f, 0.12f, 0.35f, 0.3f, 0.24f, 0.3f, "iluna", 10f, 50f, 0),
                    Z(0.1f, 0.18f, 0.4f, 0.18f, 1.4f, "altzairua", 0, 70f, 85f), K(0, 0.6f, -0.55f, 1.6f, 0.9f, 0.05f, "iluna", -35f, 0, 8f),
                    Q(0.1f, 0.012f, 0.1f, 2.0f, 1.4f, "odola", true),
                },
            });
            var hazkundea = new List<Zatia>();
            var r3 = new System.Random(7);
            // Hormari itsatsitako masa organikoa: elipsoide zapalduak (ez bolak), behean zabalagoa.
            for (int i = 0; i < 14; i++)
            {
                float y = (float)r3.NextDouble();
                float x = ((float)r3.NextDouble() - 0.5f) * (1.5f - y * 0.8f);
                float s3 = 0.35f + (float)r3.NextDouble() * 0.4f * (1.2f - y);
                hazkundea.Add(E(x, 0.1f + y * 1.6f, -0.38f + (float)r3.NextDouble() * 0.12f, s3, s3 * (0.6f + (float)r3.NextDouble() * 0.5f), s3 * 0.35f, "haragia"));
            }

            Zainak(hazkundea, r3, 5, -0.7f, 0.7f, 0.1f, 1.85f, -0.3f);
            for (int i = 0; i < 6; i++)
            {
                hazkundea.Add(E(-0.55f + i * 0.22f, 0.25f + (i % 3) * 0.35f, -0.22f, 0.09f, 0.09f, 0.07f, "pustula"));
            }

            hazkundea.Add(Q(0, 0.012f, 0.15f, 1.8f, 1.2f, "odola", true));
            l.Add(new PiezaDef { Izena = "PF_At_InfestazioHazkundea", Sektorea = "M3", Oina = new Vector3(1.6f, 1.9f, 0.9f), Zatiak = hazkundea.ToArray() });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_KontenzioHautsia", Sektorea = "M3", Oina = new Vector3(1.2f, 2.3f, 1.2f),
                Zatiak = new[]
                {
                    Z(0, 0.15f, 0, 1.1f, 0.3f, "altzairua"), Z(0, 2.05f, 0, 1.1f, 0.25f, "altzairua"),
                    K(0.4f, 0.9f, 0.1f, 0.35f, 1.2f, 0.02f, "beira", 0, 30f, 4f), K(-0.38f, 0.7f, 0.15f, 0.3f, 0.8f, 0.02f, "beira", 0, -35f, -6f),
                    K(0, 1.4f, -0.45f, 0.5f, 1.0f, 0.02f, "beira", 0, 5f, 2f), K(0.5f, 0.05f, 0.5f, 0.4f, 0.02f, 0.25f, "beira", 0, 40f, 0),
                    Q(0.2f, 0.013f, 0.5f, 1.6f, 1.1f, "likido_lurra", true), Q(-0.1f, 0.015f, 0.2f, 1.1f, 0.9f, "odola", true, 40f),
                    K(0.5f, 1.05f, -0.5f, 0.05f, 1.8f, 0.05f, "iluna"), K(-0.5f, 1.05f, -0.5f, 0.05f, 1.8f, 0.05f, "iluna"),
                },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Barrikada", Sektorea = "M3", Oina = new Vector3(2.0f, 1.2f, 0.9f),
                Zatiak = new[]
                {
                    K(-0.55f, 0.3f, 0, 0.8f, 0.6f, 0.8f, "berdea_kaxa", 0, 10f), K(0.45f, 0.3f, 0.05f, 0.75f, 0.6f, 0.75f, "berdea_kaxa", 0, -5f),
                    K(0, 0.85f, -0.1f, 1.8f, 0.06f, 0.8f, "zuria", -70f, 0, 4f), K(0.2f, 0.75f, 0.2f, 0.6f, 0.5f, 0.6f, "iluna", 0, 25f, 0),
                    Q(0, 0.013f, 0.35f, 1.6f, 0.9f, "odola", true, 15f),
                },
            });
            var zainak = new List<Zatia>();
            var r4 = new System.Random(11);
            Zainak(zainak, r4, 7, -1.1f, 1.1f, 0.05f, 2.6f, -0.08f);
            for (int i = 0; i < 6; i++)
            {
                float x = ((float)r4.NextDouble() - 0.5f) * 2f;
                float y = 0.2f + (float)r4.NextDouble() * 2.2f;
                float s4 = 0.2f + (float)r4.NextDouble() * 0.3f;
                zainak.Add(E(x, y, -0.06f, s4, s4 * 0.7f, s4 * 0.25f, "haragia"));
            }

            zainak.Add(E(0.3f, 1.1f, 0.0f, 0.1f, 0.1f, 0.08f, "pustula"));
            zainak.Add(E(-0.7f, 1.9f, 0.0f, 0.08f, 0.08f, 0.06f, "pustula"));
            l.Add(new PiezaDef { Izena = "PF_At_HormaZainak", Sektorea = "M3", Horma = true, Korridorea = true, Oina = new Vector3(2.4f, 2.8f, 0.3f), Zatiak = zainak.ToArray() });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_OdolOrbana", Sektorea = "M3", Horma = true, Korridorea = true, Lurra = true, Oina = new Vector3(1.8f, 0.05f, 1.8f),
                Zatiak = new[] { Q(0, 0.012f, 0, 1.8f, 1.8f, "odola", true), Q(0.4f, 0.014f, 0.3f, 0.9f, 0.7f, "odola", true, 60f) },
            });
            l.Add(new PiezaDef
            {
                Izena = "PF_At_Seinalea_Kontenzioa", Sektorea = "M3", Horma = true, Korridorea = true, Oina = new Vector3(1.4f, 2.4f, 0.2f),
                Zatiak = new[]
                {
                    K(0, 1.85f, 0, 1.44f, 0.4f, 0.03f, "iluna"), Q(0, 1.85f, 0.02f, 1.4f, 0.35f, "seinalea_kontenzioa"),
                    Z(-0.5f, 2.25f, 0.08f, 0.18f, 0.14f, "argi_gorria", 90f), Z(0.5f, 2.25f, 0.08f, 0.18f, 0.14f, "argi_gorria", 90f),
                },
            });
            return l;
        }

        // ---------- Materialak eta testurak (propioak, prozeduralak) ----------

        private static readonly Dictionary<string, (Color Kolorea, float Metala, float Leuntasuna, Color? Emisioa, string Testura, bool Gardena, bool Argigabea)> MaterialDefs =
            new Dictionary<string, (Color, float, float, Color?, string, bool, bool)>
            {
                ["zuria"] = (new Color(0.86f, 0.89f, 0.91f), 0.15f, 0.65f, null, null, false, false),
                ["iluna"] = (new Color(0.1f, 0.11f, 0.12f), 0.7f, 0.5f, null, null, false, false),
                ["altzairua"] = (new Color(0.55f, 0.57f, 0.6f), 0.9f, 0.6f, null, null, false, false),
                ["horia"] = (new Color(0.85f, 0.62f, 0.08f), 0.3f, 0.45f, null, null, false, false),
                ["gorria"] = (new Color(0.7f, 0.08f, 0.05f), 0.3f, 0.4f, null, null, false, false),
                ["berde_grisa"] = (new Color(0.3f, 0.36f, 0.33f), 0.5f, 0.35f, null, null, false, false),
                ["berdea_kaxa"] = (new Color(0.24f, 0.3f, 0.18f), 0.1f, 0.25f, null, null, false, false),
                ["kautxua"] = (new Color(0.03f, 0.03f, 0.035f), 0f, 0.3f, null, null, false, false),
                ["isolatzailea"] = (new Color(0.75f, 0.72f, 0.65f), 0f, 0.8f, null, null, false, false),
                ["haragia"] = (new Color(0.2f, 0.03f, 0.035f), 0f, 0.35f, null, null, false, false),
                ["zainak"] = (new Color(0.2f, 0.02f, 0.03f), 0f, 0.7f, new Color(0.9f, 0.08f, 0.05f), null, false, false),
                ["pustula"] = (new Color(0.6f, 0.55f, 0.15f), 0f, 0.85f, new Color(0.6f, 0.5f, 0.05f), null, false, false),
                ["beira"] = (new Color(0.75f, 0.9f, 0.95f, 0.22f), 0f, 0.95f, null, null, true, false),
                ["likido_berdea"] = (new Color(0.2f, 0.9f, 0.4f, 0.75f), 0f, 0.9f, new Color(0.15f, 1.2f, 0.35f), null, true, false),
                ["likido_ziana"] = (new Color(0.2f, 0.8f, 0.95f, 0.75f), 0f, 0.9f, new Color(0.15f, 0.9f, 1.3f), null, true, false),
                ["argi_ziana"] = (Color.black, 0f, 0.5f, new Color(0.3f, 1.8f, 2.2f), null, false, false),
                ["argi_anbarra"] = (Color.black, 0f, 0.5f, new Color(2.4f, 1.1f, 0.15f), null, false, false),
                ["argi_gorria"] = (Color.black, 0f, 0.5f, new Color(2.8f, 0.2f, 0.12f), null, false, false),
                ["argi_berdea"] = (Color.black, 0f, 0.5f, new Color(0.2f, 2.0f, 0.4f), null, false, false),
                ["pantaila"] = (Color.white, 0f, 0.8f, Color.white * 1.4f, "T_At_Pantaila", false, false),
                ["teklatua"] = (Color.white, 0.4f, 0.5f, Color.white * 0.4f, "T_At_Teklatua", false, false),
                ["marrak"] = (Color.white, 0.2f, 0.4f, null, "T_At_Marrak", false, false),
                ["odola"] = (new Color(0.45f, 0.02f, 0.02f, 0.92f), 0f, 0.3f, null, "T_At_Odola", true, true),
                ["erretura"] = (new Color(0.02f, 0.02f, 0.02f, 0.85f), 0f, 0.1f, null, "T_At_Erretura", true, true),
                ["likido_lurra"] = (new Color(0.2f, 0.8f, 0.3f, 0.7f), 0f, 0.9f, null, "T_At_Odola", true, true),
                ["seinalea_bioarriskua"] = (Color.white, 0f, 0.5f, Color.white * 0.6f, "T_At_Seinalea_BIOARRISKUA", false, false),
                ["seinalea_protokoloa"] = (Color.white, 0f, 0.5f, Color.white * 0.6f, "T_At_Seinalea_PROTOKOLOA_7", false, false),
                ["seinalea_tentsioa"] = (Color.white, 0f, 0.5f, Color.white * 0.6f, "T_At_Seinalea_TENTSIO_ALTUA", false, false),
                ["seinalea_ebakuazioa"] = (Color.white, 0f, 0.5f, Color.white * 0.7f, "T_At_Seinalea_EBAKUAZIOA", false, false),
                ["seinalea_kontenzioa"] = (Color.white, 0f, 0.5f, Color.white * 0.7f, "T_At_Seinalea_KONTENZIOA_HUTSA", false, false),
            };

        private delegate Color Pixela(float u, float v);

        private static byte[] Testura(int w, int h, Pixela f)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var p = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    p[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
                }
            }

            t.SetPixels(p);
            t.Apply();
            byte[] png = t.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(t);
            return png;
        }

        private static float Zarata(float x, float y) => Mathf.PerlinNoise(x, y);

        private static byte[] TesturaSortu(string izena)
        {
            if (izena.StartsWith("T_At_Seinalea_", StringComparison.Ordinal))
            {
                return TesturaProzedurala.Seinalea(izena.Substring("T_At_Seinalea_".Length).Replace('_', ' '));
            }

            switch (izena)
            {
                case "T_At_Pantaila":
                    return Testura(512, 288, (u, v) =>
                    {
                        Color c = new Color(0.02f, 0.06f, 0.1f);
                        if (Mathf.Abs(Mathf.Repeat(u * 16f, 1f)) < 0.02f || Mathf.Abs(Mathf.Repeat(v * 9f, 1f)) < 0.02f) c += new Color(0.03f, 0.08f, 0.1f);
                        float g1 = 0.55f + 0.18f * Mathf.Sin(u * 22f) * Mathf.Cos(u * 7f);
                        float g2 = 0.25f + 0.1f * Mathf.Sin(u * 41f + 1f);
                        if (Mathf.Abs(v - g1) < 0.008f) c = new Color(0.3f, 0.95f, 1f);
                        if (Mathf.Abs(v - g2) < 0.008f) c = new Color(1f, 0.6f, 0.2f);
                        if (u > 0.78f && v > 0.7f && Mathf.Repeat(v * 24f, 1f) < 0.45f && u < 0.78f + 0.2f * Zarata(v * 30f, 3f)) c = new Color(0.25f, 0.85f, 0.95f);
                        return c;
                    });
                case "T_At_Teklatua":
                    return Testura(256, 128, (u, v) => Mathf.Repeat(u * 14f, 1f) < 0.85f && Mathf.Repeat(v * 5f, 1f) < 0.8f ? new Color(0.15f, 0.17f, 0.2f) : new Color(0.02f, 0.25f, 0.3f));
                case "T_At_Marrak":
                    return Testura(256, 256, (u, v) => Mathf.Repeat((u + v) * 4f, 1f) < 0.5f ? new Color(0.9f, 0.7f, 0.05f) : new Color(0.05f, 0.05f, 0.05f));
                case "T_At_Odola":
                    return Testura(512, 512, (u, v) =>
                    {
                        float dx = u - 0.5f;
                        float dy = v - 0.5f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float n = Zarata(u * 9f, v * 9f) * 0.25f + Zarata(u * 23f + 5f, v * 23f) * 0.12f;
                        float a = Mathf.Clamp01((0.36f + n - r) * 9f);
                        float tantak = Zarata(u * 41f + 11f, v * 41f) > 0.78f && r < 0.48f ? 1f : 0f;
                        return new Color(1f, 1f, 1f, Mathf.Max(a, tantak));
                    });
                case "T_At_Erretura":
                    return Testura(512, 512, (u, v) =>
                    {
                        float dx = u - 0.5f;
                        float dy = v - 0.5f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float n = Zarata(u * 7f, v * 7f) * 0.3f;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01((0.42f + n - r) * 3f) * 0.9f);
                    });
                default:
                    throw new ArgumentException(izena);
            }
        }

        private static Texture2D TesturaLortu(string izena, bool aplikatu, List<string> aldaketak)
        {
            string bidea = $"{DatuKarpeta}/Testurak/{izena}.png";
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(bidea);
            if (t != null || !aplikatu)
            {
                if (t == null)
                {
                    aldaketak.Add($"Testura berria: {bidea}");
                }

                return t;
            }

            KarpetaZiurtatu(Path.GetDirectoryName(bidea));
            File.WriteAllBytes(bidea, TesturaSortu(izena));
            AssetDatabase.ImportAsset(bidea);
            aldaketak.Add($"Testura berria: {bidea}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(bidea);
        }

        public static Dictionary<string, Material> Materialak(bool aplikatu, List<string> aldaketak)
        {
            var emaitza = new Dictionary<string, Material>();
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            foreach (var kv in MaterialDefs)
            {
                string bidea = $"{DatuKarpeta}/Materialak/M_At_{kv.Key}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(bidea);
                var d = kv.Value;
                if (m != null && !d.Argigabea && (m.GetColor("_BaseColor") != d.Kolorea || !Mathf.Approximately(m.GetFloat("_Smoothness"), d.Leuntasuna)))
                {
                    // Definizioa aldatu da: kolorea eta leuntasuna eguneratu (idenpotentea).
                    aldaketak.Add($"Materiala eguneratua: {bidea}");
                    if (aplikatu)
                    {
                        m.SetColor("_BaseColor", d.Kolorea);
                        m.SetFloat("_Smoothness", d.Leuntasuna);
                        EditorUtility.SetDirty(m);
                    }
                }

                if (m != null || !aplikatu)
                {
                    if (m == null)
                    {
                        aldaketak.Add($"Materiala berria: {bidea}");
                    }

                    emaitza[kv.Key] = m;
                    continue;
                }

                m = new Material(d.Argigabea ? unlit : lit) { name = $"M_At_{kv.Key}" };
                m.SetColor("_BaseColor", d.Kolorea);
                if (!d.Argigabea)
                {
                    m.SetFloat("_Metallic", d.Metala);
                    m.SetFloat("_Smoothness", d.Leuntasuna);
                }

                if (d.Testura != null)
                {
                    m.SetTexture("_BaseMap", TesturaLortu(d.Testura, aplikatu, aldaketak));
                }

                if (d.Emisioa.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", d.Emisioa.Value);
                    if (d.Testura != null)
                    {
                        m.SetTexture("_EmissionMap", m.GetTexture("_BaseMap"));
                    }

                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                if (d.Gardena)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.renderQueue = (int)RenderQueue.Transparent;
                }

                KarpetaZiurtatu(Path.GetDirectoryName(bidea));
                AssetDatabase.CreateAsset(m, bidea);
                aldaketak.Add($"Materiala berria: {bidea}");
                emaitza[kv.Key] = m;
            }

            return emaitza;
        }

        // ---------- Mallak eta aurrefabrikatuak ----------

        private static Mesh Primitiboa(string mota) => Resources.GetBuiltinResource<Mesh>($"{mota}.fbx");

        /// <summary>Pieza baten aurrefabrikatua: materialeko azpimalla bat (draw call gutxi), BoxCollider soila lurrekoetan.</summary>
        public static GameObject AurrefabrikatuaSortu(PiezaDef def, Dictionary<string, Material> materialak, bool aplikatu, List<string> aldaketak, string karpeta = null)
        {
            string piezaKarpeta = karpeta ?? PiezaKarpeta;
            string bidea = $"{piezaKarpeta}/{def.Izena}.prefab";
            string mallaIzena = $"SM_{def.Izena.Substring(3)}_{Bertsioa(def)}";
            var dagoena = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
            Mesh dagoenMalla = dagoena != null ? dagoena.GetComponent<MeshFilter>()?.sharedMesh : null;
            if ((dagoena != null && dagoenMalla != null && dagoenMalla.name == mallaIzena) || !aplikatu)
            {
                if (dagoena == null || dagoenMalla == null || dagoenMalla.name != mallaIzena)
                {
                    aldaketak.Add($"Pieza {(dagoena == null ? "berria" : "eguneratua")}: {bidea}");
                }

                return dagoena;
            }

            if (dagoenMalla != null)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dagoenMalla));
            }

            string[] materialIzenak = def.Zatiak.Select(z => z.Mat).Distinct().ToArray();
            var azpimallak = new List<CombineInstance>();
            foreach (string mat in materialIzenak)
            {
                CombineInstance[] zatiak = def.Zatiak.Where(z => z.Mat == mat).Select(z => new CombineInstance
                {
                    mesh = Primitiboa(z.Mota),
                    transform = Matrix4x4.TRS(z.P, Quaternion.Euler(z.R), z.S),
                }).ToArray();
                var m = new Mesh();
                m.CombineMeshes(zatiak, true, true);
                azpimallak.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });
            }

            var malla = new Mesh { name = mallaIzena };
            malla.CombineMeshes(azpimallak.ToArray(), false, true);
            malla.RecalculateBounds();
            foreach (CombineInstance c in azpimallak)
            {
                UnityEngine.Object.DestroyImmediate(c.mesh);
            }

            string mallaBidea = $"{DatuKarpeta}/Mallak/{malla.name}.asset";
            KarpetaZiurtatu(Path.GetDirectoryName(mallaBidea));
            KarpetaZiurtatu(piezaKarpeta);
            AssetDatabase.CreateAsset(malla, mallaBidea);

            Scene aurrebista = EditorSceneManager.NewPreviewScene();
            try
            {
                var o = new GameObject(def.Izena);
                SceneManager.MoveGameObjectToScene(o, aurrebista);
                o.AddComponent<MeshFilter>().sharedMesh = malla;
                var r = o.AddComponent<MeshRenderer>();
                r.sharedMaterials = materialIzenak.Select(n => materialak[n]).ToArray();
                r.shadowCastingMode = def.Lurra ? ShadowCastingMode.Off : ShadowCastingMode.On;
                if (!def.Horma)
                {
                    var k = o.AddComponent<BoxCollider>();
                    k.center = new Vector3(0f, def.Oina.y * 0.5f, 0f);
                    k.size = def.Oina;
                }

                PrefabUtility.SaveAsPrefabAsset(o, bidea, out bool gordeta);
                if (!gordeta)
                {
                    throw new InvalidOperationException($"Ezin izan da gorde {bidea}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(aurrebista);
            }

            aldaketak.Add($"Pieza berria: {bidea} ({malla.triangles.Length / 3} triangelu, {materialIzenak.Length} material)");
            return AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
        }
    }
}
