using System.Collections.Generic;
using Eclipse.Misioak;
using UnityEngine;

namespace Eclipse.EditorTresnak
{
    /// <summary>Hiru mailen datuak. Sarea: X ekialdera, Z iparrera; koordenatuak gelaxketan (3,75 m). Luzera-araua: atarik gabe edo bi aterekin n &gt;= 3; ate bakarrarekin n &gt;= 6.</summary>
    public static partial class EszenaEraikitzailea
    {
        private const Aldea E = Aldea.Ekialdea;
        private const Aldea M = Aldea.Mendebaldea;
        private const Aldea I = Aldea.Iparra;
        private const Aldea H = Aldea.Hegoa;

        private static MailaGela G(string izena, int x, int z, int w, int d, string seinalea, Color kolorea, float intentsitatea,
            int ilarak = 2, float altuera = 0f, bool zoruKit = false, string panela = "PF_ArgiPanela_Hotza", float tartea = 2f, bool pultsua = false)
        {
            return new MailaGela
            {
                Izena = izena, X0 = x, Z0 = z, W = w, D = d, Seinalea = seinalea, Kolorea = kolorea, Intentsitatea = intentsitatea,
                Ilarak = ilarak, Altuera = altuera, ZoruKit = zoruKit, Panela = panela, PanelTartea = tartea, Pultsua = pultsua,
            };
        }

        private static MailaLotura L(string a, Aldea alde, int gurutzea, string b, bool ateaA, bool ateaB, HodiArgia argia, Color kolorea,
            int blokeoa = -1, string seinaleaA = null, string seinaleaB = null, string hodiak = null)
        {
            return new MailaLotura
            {
                A = a, AldeA = alde, Gurutzea = gurutzea, B = b, AteaA = ateaA, AteaB = ateaB, Argia = argia, Kolorea = kolorea,
                BlokeoFasea = blokeoa, SeinaleaA = seinaleaA, SeinaleaB = seinaleaB, Hodiak = hodiak,
            };
        }

        private static MailaLotura Itsua(string a, Aldea alde, int gurutzea, int luzera, bool ateaA, HodiArgia argia, Color kolorea, string hodiak = null)
        {
            return new MailaLotura { A = a, AldeA = alde, Gurutzea = gurutzea, Mutur = luzera, AteaA = ateaA, Argia = argia, Kolorea = kolorea, Hodiak = hodiak };
        }

        private static ProtoProp P(string sala, string prefab, float u, float v, float yaw = 0f, bool kolisioa = false)
        {
            return new ProtoProp(sala, string.Empty, prefab, u, v, yaw, kolisioa);
        }

        /// <summary>X sare-lerroko horma, (x-1,z) eta (x,z) gelaxken artean.</summary>
        private static MailaHorma HV(string sala, int x, int z) => new MailaHorma { Sala = sala, Vertikala = true, X = x, Z = z };

        /// <summary>Z sare-lerroko horma, (x,z-1) eta (x,z) gelaxken artean.</summary>
        private static MailaHorma HH(string sala, int x, int z) => new MailaHorma { Sala = sala, Vertikala = false, X = x, Z = z };

        private static MailaLeihoa W(string sala, Aldea alde, int gurutzea) => new MailaLeihoa { Sala = sala, Alde = alde, Gurutzea = gurutzea };

        private static MailaApaingarri A(string sala, string prefab, float x, float y, float z, float yaw = 0f, float fx = 1f, float fy = 1f, float fz = 1f, bool kolisioa = false)
        {
            return new MailaApaingarri { Sala = sala, Prefab = prefab, X = x, Y = y, Z = z, Yaw = yaw, Faktoreak = new Vector3(fx, fy, fz), Kolisioa = kolisioa };
        }

        private static MailaKamera K(string izena, float bx, float bz, float hx, float hz, float by = 1.65f, float hy = 1.5f)
        {
            return new MailaKamera { Izena = izena, Bx = bx, By = by, Bz = bz, Hx = hx, Hy = hy, Hz = hz };
        }

        /// <summary>Probeta bat: oina, beirazko zilindroa eta barruko nukleo argitsua, kolisioarekin.</summary>
        private static IEnumerable<MailaApaingarri> Probeta(string sala, float x, float z)
        {
            yield return A(sala, "PF_Oina", x, 0.2f, z, 0f, 1.7f, 0.4f, 1.7f, true);
            yield return A(sala, "PF_Nukleoa", x, 1.45f, z, 0f, 0.5f, 2.1f, 0.5f);
            yield return A(sala, "PF_Beira_Zilindroa", x, 1.65f, z, 0f, 1.4f, 2.5f, 1.4f, true);
            yield return A(sala, "PF_Oina", x, 3.05f, z, 0f, 1.7f, 0.3f, 1.7f);
        }

        // ========== 1. MISIOA: LABORATEGIA ==========

        private static MailaDef Mision01()
        {
            Color zuria = new Color(0.82f, 0.92f, 1f);
            Color urdina = new Color(0.35f, 0.62f, 1f);
            Color ziana = new Color(0.3f, 0.85f, 1f);
            Color iluna = new Color(0.25f, 0.4f, 0.8f);
            Color gorria = new Color(1f, 0.15f, 0.1f);

            var apaingarriak = new List<MailaApaingarri>();
            // Laborategi nagusia: probetak mendebaldeko eta ekialdeko lerroetan.
            apaingarriak.AddRange(Probeta("LabNagusia", 14.7f, 12.7f));
            apaingarriak.AddRange(Probeta("LabNagusia", 14.7f, 13.5f));
            apaingarriak.AddRange(Probeta("LabNagusia", 16.4f, 12.5f));
            apaingarriak.AddRange(Probeta("LabNagusia", 16.4f, 13.3f));
            // 1. laborategia eta biologia.
            apaingarriak.AddRange(Probeta("Lab1", 16.7f, 2.3f));
            apaingarriak.AddRange(Probeta("Lab1", 18.3f, 2.3f));
            apaingarriak.AddRange(Probeta("Biologia", 22.4f, 0.6f));
            apaingarriak.AddRange(Probeta("Biologia", 22.4f, 1.5f));
            // Zerbitzariak: datu-nukleoen ilarak.
            foreach (float x in new[] { 17.6f, 19.6f })
            {
                foreach (float z in new[] { 10.8f, 12.1f })
                {
                    apaingarriak.Add(A("Zerbitzariak", "PF_DatuNukleoa", x, 1.75f, z));
                }
            }

            // Pantailak.
            apaingarriak.Add(A("Kontrola", "PF_Pantaila_Urdina", 16.5f, 3.2f, 27.95f, 0f, 7f, 1.8f, 0.05f));
            apaingarriak.Add(A("Harrera", "PF_Pantaila_Urdina", 11f, 2.6f, 2.95f, 0f, 3f, 1.2f, 0.05f));
            apaingarriak.Add(A("LabNagusia", "PF_Pantaila_Urdina", 15.5f, 4.5f, 10.05f, 0f, 5f, 2f, 0.05f));
            apaingarriak.Add(A("Esnatzea", "PF_Pantaila_Gorria", 0.05f, 2.4f, 1.5f, 0f, 0.05f, 0.8f, 2.4f));

            return new MailaDef
            {
                Izena = "Mision01_Laborategia",
                Erroa = "Sektorea1",
                Aurrizkia = "M1",
                Giroa = new Color(0.07f, 0.09f, 0.13f),
                Gelak = new[]
                {
                    G("Esnatzea", 0, 0, 3, 3, "ESNATZEA", zuria, 3.5f, zoruKit: true),
                    G("Harrera", 9, 0, 4, 3, "HARRERA", urdina, 4f),
                    G("Mantentzea", 0, 6, 5, 5, "MANTENTZEA", iluna, 1.6f, altuera: 3.2f, panela: "PF_ArgiPanela_Ziana"),
                    G("J1", 10, 6, 1, 1, "J1", ziana, 3f, zoruKit: true),
                    G("J2", 10, 11, 1, 1, "J2", urdina, 3f, zoruKit: true),
                    G("J3", 16, 6, 1, 1, "J3", ziana, 3f, zoruKit: true),
                    G("J4", 20, 6, 1, 1, "J4", gorria, 2.2f, zoruKit: true, panela: "PF_ArgiPanela_Gorria", pultsua: true),
                    G("J5", 10, 17, 1, 1, "J5", gorria, 2.2f, zoruKit: true, panela: "PF_ArgiPanela_Gorria", pultsua: true),
                    G("J6", 22, 26, 1, 1, "J6", gorria, 2.5f, zoruKit: true, panela: "PF_ArgiPanela_Gorria"),
                    G("Lab1", 16, 0, 3, 3, "LAB1", zuria, 4.5f),
                    G("Biologia", 20, 0, 3, 3, "BIOLOGIA", iluna, 2.2f),
                    G("LabNagusia", 14, 10, 3, 4, "LABNAGUSIA", zuria, 8f, ilarak: 3, panela: "PF_ArgiPanela_Ziana"),
                    G("Zerbitzariak", 17, 10, 4, 3, "ZERBITZARIAK", iluna, 3f, panela: "PF_ArgiPanela_Ziana"),
                    G("Biltegia", 0, 14, 5, 4, "BILTEGIA", urdina, 3.5f),
                    G("Borroka", 14, 17, 5, 5, "BORROKA", zuria, 6.5f, ilarak: 3),
                    G("Kontrola", 14, 25, 5, 3, "KONTROLA", urdina, 5f),
                    G("Irteera", 21, 20, 4, 3, "IRTEERA", ziana, 4f),
                },
                Loturak = new[]
                {
                    L("Esnatzea", E, 1, "Harrera", true, false, HodiArgia.Argitua, zuria),
                    L("Esnatzea", I, 1, "Mantentzea", true, true, HodiArgia.Iluna, iluna),
                    L("Harrera", I, 10, "J1", false, false, HodiArgia.Argitua, urdina),
                    L("Mantentzea", E, 6, "J1", false, false, HodiArgia.Iluna, iluna),
                    L("J1", E, 6, "J3", false, false, HodiArgia.Erdia, ziana),
                    L("Harrera", E, 1, "Lab1", false, false, HodiArgia.Argitua, zuria),
                    L("J3", H, 16, "Lab1", true, true, HodiArgia.Erdia, zuria),
                    L("J3", E, 6, "J4", false, false, HodiArgia.Iluna, gorria),
                    L("J4", H, 20, "Biologia", true, true, HodiArgia.Iluna, iluna),
                    Itsua("J4", E, 6, 4, true, HodiArgia.Iluna, gorria),
                    L("J4", I, 20, "Zerbitzariak", true, true, HodiArgia.Erdia, iluna),
                    L("J3", I, 16, "LabNagusia", false, false, HodiArgia.Argitua, zuria),
                    L("J1", I, 10, "J2", false, false, HodiArgia.Erdia, urdina),
                    L("J2", E, 11, "LabNagusia", false, false, HodiArgia.Argitua, zuria),
                    L("J2", I, 10, "J5", false, false, HodiArgia.Iluna, gorria),
                    L("J5", E, 17, "Borroka", false, false, HodiArgia.Erdia, zuria),
                    L("LabNagusia", I, 15, "Borroka", true, true, HodiArgia.Argitua, zuria),
                    L("Zerbitzariak", I, 18, "Borroka", true, true, HodiArgia.Iluna, iluna),
                    L("J5", M, 17, "Biltegia", false, false, HodiArgia.Iluna, gorria),
                    L("Mantentzea", I, 2, "Biltegia", true, true, HodiArgia.Iluna, iluna),
                    L("Borroka", I, 16, "Kontrola", true, true, HodiArgia.Argitua, urdina),
                    L("Kontrola", E, 26, "J6", true, true, HodiArgia.Erdia, gorria, blokeoa: 2, seinaleaA: "ENERGIA"),
                    L("J6", H, 22, "Irteera", false, false, HodiArgia.Erdia, ziana, seinaleaA: "ENERGIA"),
                },
                BarneHormak = new[]
                {
                    // Mantentze-laberintoa: mendebaldeko sugea eta ekialdeko zutabea, bi itsurekin.
                    HH("Mantentzea", 1, 7), HH("Mantentzea", 2, 7), HH("Mantentzea", 3, 7),
                    HV("Mantentzea", 1, 7), HV("Mantentzea", 1, 8),
                    HH("Mantentzea", 0, 10), HH("Mantentzea", 1, 10),
                    HV("Mantentzea", 2, 8), HV("Mantentzea", 2, 9),
                    HH("Mantentzea", 2, 8), HV("Mantentzea", 3, 8),
                    HV("Mantentzea", 4, 8), HV("Mantentzea", 4, 9),
                    HH("Mantentzea", 3, 10),
                },
                Leihoak = new[]
                {
                    W("LabNagusia", E, 11), W("LabNagusia", E, 12),
                    W("Zerbitzariak", M, 11), W("Zerbitzariak", M, 12),
                },
                Propak = new[]
                {
                    P("Esnatzea", "Container_small", -0.8f, 0.8f),
                    P("Esnatzea", "Box1", 0.75f, -0.8f, 0f, true),
                    P("Esnatzea", "Old_computer", 0.85f, 0.95f),
                    P("Harrera", "Rack", 0.15f, 0.4f, 0f),
                    P("Harrera", "Old_computer", -0.85f, 0.85f),
                    P("Harrera", "Container_small", 0.85f, -0.85f),
                    P("Lab1", "Rack", -0.1f, -0.2f),
                    P("Lab1", "Old_computer", -0.85f, -0.85f),
                    P("Biologia", "Rack", -0.15f, 0.1f),
                    P("Biologia", "Container_BigOpen", -0.85f, -0.85f),
                    P("LabNagusia", "Rack", -0.05f, -0.3f),
                    P("LabNagusia", "Old_computer", -0.85f, -0.85f),
                    P("Biltegia", "Supply_box", -1f, 1f),
                    P("Biltegia", "Oxygen_sint", 1f, -1f, 90f),
                    P("Biltegia", "Container_Big", -0.2f, 0.2f),
                    P("Biltegia", "Container_Big", 0.1f, 0.2f),
                    P("Biltegia", "Container_small", 0.1f, -0.25f, 90f),
                    P("Biltegia", "Container_BigOpen", -0.45f, -0.5f),
                    P("Biltegia", "Rack", -1f, -0.5f),
                    P("Biltegia", "Tubes", 0.35f, 0.45f),
                    P("Borroka", "Container_Big", -0.35f, 0.3f),
                    P("Borroka", "Container_BigOpen", 0.3f, -0.3f),
                    P("Borroka", "Box1", 0.1f, 0.45f, 0f, true),
                    P("Borroka", "Container_small", -0.5f, -0.25f, 90f),
                    P("Borroka", "Supply_box", 1f, 0.45f),
                    P("Borroka", "Rack", -1f, 0.85f),
                    P("Borroka", "Old_computer", -0.2f, -0.65f, 90f),
                    P("Borroka", "Container_Big", 0.55f, -0.1f),
                    P("Kontrola", "Old_computer", -0.75f, 0.85f),
                    P("Kontrola", "Rack", 0.55f, 0.75f),
                    P("Kontrola", "Container_small", -0.85f, -0.85f),
                    P("Irteera", "Vents", 1f, 1f),
                    P("Irteera", "Container_Big", -1f, -1f),
                },
                Apaingarriak = apaingarriak.ToArray(),
                Kamerak = new[]
                {
                    K("Esnatzea", 0.4f, 0.4f, 2.5f, 2.6f),
                    K("Korridorea", 3.6f, 1.5f, 9f, 1.5f),
                    K("Harrera", 9.3f, 0.5f, 12.5f, 2.5f),
                    K("LabNagusia", 14.3f, 10.4f, 16.5f, 13.5f, 1.65f, 2f),
                    K("Leihoa", 16f, 11.5f, 19f, 11.5f),
                    K("Zerbitzariak", 20.5f, 10.5f, 17.5f, 12.5f),
                    K("Laberintoa", 1.5f, 7.5f, 1.5f, 9.8f),
                    K("Biltegia", 4.5f, 14.4f, 0.5f, 17.5f),
                    K("Borroka", 14.3f, 17.5f, 19f, 21f),
                    K("Kontrola", 16.5f, 25.3f, 16.5f, 27.8f, 1.65f, 2.5f),
                    K("Pasillo_iluna", 7f, 17.5f, 5f, 17.5f),
                },
                Misioa = new MisioDef
                {
                    Izena = "1. misioa: Laborategia",
                    Hurrengoa = "Mision02_Energia",
                    SpawnX = 1.5f,
                    SpawnZ = 1.5f,
                    SpawnYaw = 90f,
                    IrteeraX = 24.3f,
                    IrteeraZ = 21.5f,
                    Faseak = new[]
                    {
                        new MisioFaseDef { Deskribapena = "Lortu segurtasun-kodea zerbitzari-gelan", Helburuak = new[] { "kodea" } },
                        new MisioFaseDef { Deskribapena = "Aktibatu segurtasun-terminala kontrol-gelan", Helburuak = new[] { "segurtasuna" } },
                    },
                    Terminalak = new[]
                    {
                        new MisioTerminalDef { Id = "kodea", Izena = "Segurtasun-kodea deskargatu", Sala = "Zerbitzariak", X = 20.6f, Z = 12.5f, Yaw = 0f },
                        new MisioTerminalDef { Id = "segurtasuna", Izena = "Energiarako sarbidea desblokeatu", Sala = "Kontrola", X = 16.5f, Z = 27.4f, Yaw = 0f },
                    },
                    Bildumak = new List<Vector3>
                    {
                        new Vector3(2.5f, 1f, 8.5f),
                        new Vector3(23.8f, 1f, 6.5f),
                        new Vector3(21.5f, 1f, 1.5f),
                        new Vector3(0.5f, 1f, 16f),
                    },
                    Markak = new List<(MarkaMota, Vector3)>
                    {
                        (MarkaMota.Etsaia, new Vector3(16.5f, 0f, 21f)),
                        (MarkaMota.Etsaia, new Vector3(18f, 0f, 19f)),
                        (MarkaMota.Etsaia, new Vector3(15f, 0f, 19.5f)),
                        (MarkaMota.Etsaia, new Vector3(2f, 0f, 15.5f)),
                        (MarkaMota.Etsaia, new Vector3(14.6f, 0f, 11f)),
                        (MarkaMota.Kobertura, new Vector3(15.6f, 0f, 19.2f)),
                        (MarkaMota.Kobertura, new Vector3(17.5f, 0f, 18.4f)),
                        (MarkaMota.Arriskua, new Vector3(13f, 0f, 6.5f)),
                    },
                },
            };
        }

        private static MailaApaingarri HodiX(string sala, string prefab, float x0, float x1, float y, float z, float diametroa, bool kolisioa = false)
        {
            return new MailaApaingarri { Sala = sala, Prefab = prefab, X = (x0 + x1) * 0.5f, Y = y, Z = z, Faktoreak = new Vector3(diametroa, (x1 - x0) * Gelaxka, diametroa), Etzanda = 'X', Kolisioa = kolisioa };
        }

        private static MailaApaingarri HodiZ(string sala, string prefab, float x, float z0, float z1, float y, float diametroa, bool kolisioa = false)
        {
            return new MailaApaingarri { Sala = sala, Prefab = prefab, X = x, Y = y, Z = (z0 + z1) * 0.5f, Faktoreak = new Vector3(diametroa, (z1 - z0) * Gelaxka, diametroa), Etzanda = 'Z', Kolisioa = kolisioa };
        }

        private static MailaPlataforma Plat(string sala, float x0, float z0, float x1, float z1, float y)
        {
            return new MailaPlataforma { Sala = sala, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y = y };
        }

        private static MailaBaranda Bar(string sala, float x0, float z0, float x1, float z1, float y)
        {
            return new MailaBaranda { Sala = sala, X0 = x0, Z0 = z0, X1 = x1, Z1 = z1, Y = y };
        }

        private const float Gelaxka = 3.75f;

        /// <summary>Sorgailu handia: oin astuna, nukleo iluna, banda argitsuak eta barra-kaiola, kolisioarekin.</summary>
        private static IEnumerable<MailaApaingarri> Sorgailua(string sala, float x, float z, float diametroa, float altuera)
        {
            float oina = 0.7f;
            float goia = oina + altuera;
            yield return A(sala, "PF_Oina", x, oina * 0.5f, z, 0f, diametroa + 0.5f, oina, diametroa + 0.5f, true);
            yield return A(sala, "PF_Oina", x, oina + altuera * 0.5f, z, 0f, diametroa * 0.82f, altuera, diametroa * 0.82f, true);
            int bandak = Mathf.Max(2, Mathf.RoundToInt(altuera / 1.05f));
            for (int i = 0; i < bandak; i++)
            {
                float y = oina + altuera * (i + 0.5f) / bandak;
                yield return A(sala, "PF_Bobina", x, y, z, 0f, diametroa * 0.9f, 0.32f, diametroa * 0.9f);
            }

            const int Barrak = 8;
            float erradioa = diametroa * 0.5f / Gelaxka;
            for (int i = 0; i < Barrak; i++)
            {
                float angelua = (i + 0.5f) * Mathf.PI * 2f / Barrak;
                yield return A(sala, "PF_Zutabea", x + Mathf.Cos(angelua) * erradioa, oina + altuera * 0.5f, z + Mathf.Sin(angelua) * erradioa, 0f, 0.13f, altuera, 0.13f);
            }

            yield return A(sala, "PF_Oina", x, goia + 0.25f, z, 0f, diametroa + 0.3f, 0.5f, diametroa + 0.3f);
            yield return A(sala, "PF_Hodia_Grisa", x, goia + 1.2f, z, 0f, 0.5f, 1.4f, 0.5f);
        }

        // ========== 2. MISIOA: ENERGIA ==========

        private static MailaDef Mision02()
        {
            Color anbarra = new Color(1f, 0.62f, 0.2f);
            Color laranja = new Color(1f, 0.42f, 0.1f);
            Color horia = new Color(1f, 0.82f, 0.35f);
            Color gorria = new Color(1f, 0.18f, 0.08f);
            Color iluna = new Color(0.8f, 0.38f, 0.12f);
            const string HL = "PF_Hodia_Laranja";
            const string HG = "PF_Hodia_Grisa";

            var ap = new List<MailaApaingarri>();
            // Sorgailuen sala: lau sorgailu handi, atarien pasabideetatik kanpo.
            ap.AddRange(Sorgailua("Sorgailuak", 14.3f, 9.8f, 3.2f, 4.2f));
            ap.AddRange(Sorgailua("Sorgailuak", 17.0f, 9.5f, 2.4f, 3.2f));
            ap.AddRange(Sorgailua("Sorgailuak", 14.1f, 12.5f, 2.4f, 3.2f));
            ap.AddRange(Sorgailua("Sorgailuak", 16.9f, 12.0f, 2.4f, 3.2f));
            ap.Add(HodiX("Sorgailuak", HL, 13f, 18f, 6.6f, 9.8f, 0.5f));
            ap.Add(HodiX("Sorgailuak", HG, 13f, 18f, 6.9f, 12f, 0.35f));
            ap.Add(HodiZ("Sorgailuak", HG, 15.5f, 8f, 13f, 6.2f, 0.35f));
            ap.Add(A("Sorgailuak", "PF_Pantaila_Laranja", 13.05f, 5.8f, 10.5f, 0f, 0.04f, 1.5f, 6f));

            // Hodien ganbera: hodi bertikalak eta etzanak, maila baxuan kobertura gisa.
            foreach ((float x, float z) in new[] { (0.7f, 3.8f), (2.2f, 4.4f), (0.8f, 6.3f), (2.3f, 5.9f) })
            {
                ap.Add(A("Hodiak", HL, x, 3.75f, z, 0f, 0.8f, 7.5f, 0.8f, true));
            }

            ap.Add(HodiX("Hodiak", HG, 0f, 2.1f, 0.6f, 5.9f, 0.7f, true));
            ap.Add(HodiZ("Hodiak", HL, 1.5f, 3f, 5f, 3.2f, 0.45f));
            ap.Add(HodiX("Hodiak", HL, 0f, 3f, 4.8f, 3.6f, 0.5f));
            ap.Add(HodiX("Hodiak", HG, 0f, 3f, 5.6f, 6.4f, 0.4f));
            // Fusible-gelak: hormako koadroak eta hodi txikiak.
            foreach (float x in new[] { 7.6f, 8.6f, 10.4f, 11.4f })
            {
                ap.Add(A("Fusibleak1", "PF_Pantaila_Laranja", x, 2.2f, 22.95f, 0f, 1.5f, 1.4f, 0.05f));
                ap.Add(A("Fusibleak2", "PF_Pantaila_Laranja", x, 2.2f, 0.95f, 0f, 1.5f, 1.4f, 0.05f));
            }

            ap.Add(A("Kontrola", "PF_Pantaila_Laranja", 15.5f, 3.2f, 18.95f, 0f, 7f, 1.8f, 0.05f));
            ap.Add(A("Sarbidea", "PF_Pantaila_Gorria", 0.05f, 2.4f, 11.5f, 0f, 0.05f, 0.8f, 2.4f));
            ap.Add(HodiX("Osagaiak", HL, 15f, 19f, 4.4f, 24.7f, 0.4f));
            ap.Add(HodiX("Mantentzea", HG, 19f, 24f, 2.8f, -2.7f, 0.2f));

            return new MailaDef
            {
                Izena = "Mision02_Energia",
                Erroa = "Sektorea2",
                Aurrizkia = "M2",
                Giroa = new Color(0.09f, 0.07f, 0.05f),
                KoloreArintzea = 0.2f,
                Gelak = new[]
                {
                    G("Sarbidea", 0, 10, 3, 3, "SARBIDEA", anbarra, 3.2f, zoruKit: true, panela: "PF_ArgiPanela_Anbarra"),
                    G("J1", 9, 11, 1, 1, "J1", anbarra, 3f, zoruKit: true, panela: "PF_ArgiPanela_Anbarra"),
                    G("J2", 9, 16, 1, 1, "J2", horia, 3f, zoruKit: true, panela: "PF_ArgiPanela_Anbarra"),
                    G("J3", 9, 4, 1, 1, "J3", laranja, 3f, zoruKit: true, panela: "PF_ArgiPanela_Anbarra"),
                    G("J4", 15, 4, 1, 1, "J4", gorria, 2.2f, zoruKit: true, panela: "PF_ArgiPanela_Gorria", pultsua: true),
                    G("J5", 22, 4, 1, 1, "J5", laranja, 2.6f, zoruKit: true, panela: "PF_ArgiPanela_Anbarra"),
                    G("J6", 21, 17, 1, 1, "J6", gorria, 2.4f, zoruKit: true, panela: "PF_ArgiPanela_Gorria"),
                    G("Sorgailuak", 13, 8, 5, 5, "SORGAILUAK", anbarra, 7f, ilarak: 3, panela: "PF_ArgiPanela_Anbarra"),
                    G("Kontrola", 13, 16, 5, 3, "KONTROLA", horia, 4.5f, panela: "PF_ArgiPanela_Anbarra"),
                    G("Biltegia", 0, 16, 4, 3, "BILTEGIA", iluna, 2.6f, panela: "PF_ArgiPanela_Anbarra"),
                    G("Hodiak", 0, 3, 3, 4, "HODIAK", laranja, 3.5f, ilarak: 3, panela: "PF_ArgiPanela_Anbarra"),
                    G("Fusibleak1", 7, 20, 5, 3, "FUSIBLEAK1", horia, 3.2f, panela: "PF_ArgiPanela_Anbarra"),
                    G("Fusibleak2", 7, -2, 5, 3, "FUSIBLEAK2", laranja, 3.2f, panela: "PF_ArgiPanela_Anbarra"),
                    G("Bobinak", 21, 8, 4, 5, "BOBINAK", iluna, 3f, panela: "PF_ArgiPanela_Anbarra"),
                    G("Osagaiak", 15, 22, 4, 3, "OSAGAIAK", iluna, 2.6f, panela: "PF_ArgiPanela_Anbarra"),
                    G("Mantentzea", 19, -3, 5, 4, "MANTENTZEA", gorria, 1.4f, altuera: 3.2f, panela: "PF_ArgiPanela_Gorria"),
                    G("Irteera", 19, 21, 5, 3, "IRTEERA", gorria, 3.5f, panela: "PF_ArgiPanela_Gorria", pultsua: true),
                },
                Loturak = new[]
                {
                    L("Sarbidea", E, 11, "J1", true, false, HodiArgia.Argitua, anbarra, hodiak: HL),
                    L("J1", E, 11, "Sorgailuak", false, false, HodiArgia.Argitua, anbarra, hodiak: HL),
                    L("J1", I, 9, "J2", false, false, HodiArgia.Erdia, horia, hodiak: HG),
                    L("J3", I, 9, "J1", false, false, HodiArgia.Erdia, laranja, hodiak: HG),
                    L("Sarbidea", I, 1, "Biltegia", true, true, HodiArgia.Iluna, iluna),
                    L("J2", M, 16, "Biltegia", false, false, HodiArgia.Iluna, iluna, hodiak: HG),
                    L("J2", I, 9, "Fusibleak1", true, true, HodiArgia.Erdia, horia),
                    L("J2", E, 16, "Kontrola", false, false, HodiArgia.Argitua, horia, hodiak: HL),
                    L("Sorgailuak", I, 15, "Kontrola", true, true, HodiArgia.Argitua, anbarra),
                    L("Sarbidea", H, 1, "Hodiak", true, true, HodiArgia.Iluna, laranja),
                    L("Hodiak", E, 4, "J3", false, false, HodiArgia.Iluna, laranja, hodiak: HL),
                    L("J3", H, 9, "Fusibleak2", true, true, HodiArgia.Erdia, laranja),
                    L("J3", E, 4, "J4", false, false, HodiArgia.Iluna, gorria, hodiak: HG),
                    L("J4", I, 15, "Sorgailuak", false, false, HodiArgia.Erdia, anbarra),
                    L("J4", E, 4, "J5", false, false, HodiArgia.Iluna, laranja, hodiak: HL),
                    L("J5", I, 22, "Bobinak", true, true, HodiArgia.Erdia, iluna),
                    L("Sorgailuak", E, 10, "Bobinak", true, true, HodiArgia.Argitua, anbarra),
                    L("J5", H, 22, "Mantentzea", false, false, HodiArgia.Iluna, gorria),
                    L("Fusibleak2", E, -1, "Mantentzea", false, false, HodiArgia.Iluna, gorria, hodiak: HL),
                    L("Fusibleak1", E, 22, "Osagaiak", false, false, HodiArgia.Erdia, iluna),
                    L("Kontrola", I, 15, "Osagaiak", true, true, HodiArgia.Iluna, iluna),
                    L("Kontrola", E, 17, "J6", true, true, HodiArgia.Erdia, gorria, blokeoa: 2, seinaleaA: "ERREAKTOREA"),
                    L("J6", I, 21, "Irteera", false, false, HodiArgia.Erdia, gorria, seinaleaA: "ERREAKTOREA"),
                },
                BarneHormak = new[]
                {
                    // Mantentze-laberintoa: hegoaldeko bi ilaretan bidezidor bihurria, iparraldeko ate-tartea libre.
                    HH("Mantentzea", 20, -2), HH("Mantentzea", 21, -2), HH("Mantentzea", 22, -2),
                    HV("Mantentzea", 23, -3), HV("Mantentzea", 23, -2),
                    HV("Mantentzea", 20, -1), HH("Mantentzea", 21, 0), HH("Mantentzea", 20, 0),
                    HV("Mantentzea", 22, -1),
                },
                Plataformak = new[]
                {
                    // Sorgailuen salako pasabidea: hegoaldetik ekialdera eta iparraldera, 4,6 m-ra.
                    Plat("Sorgailuak", 15.3f, 8f, 18f, 8.64f, 4.6f),
                    Plat("Sorgailuak", 17.36f, 8.64f, 18f, 13f, 4.6f),
                    Plat("Sorgailuak", 13f, 12.36f, 17.36f, 13f, 4.6f),
                },
                Barandak = new[]
                {
                    Bar("Sorgailuak", 15.3f, 8.64f, 17.36f, 8.64f, 4.6f),
                    Bar("Sorgailuak", 17.36f, 8.64f, 17.36f, 12.36f, 4.6f),
                    Bar("Sorgailuak", 13f, 12.36f, 17.36f, 12.36f, 4.6f),
                },
                Eskailerak = new[]
                {
                    new MailaEskailera { Sala = "Sorgailuak", X = 15.3f, Z = 8.32f, Alde = E, Y0 = 0f, Y1 = 4.6f, Zabalera = 2.4f },
                },
                Propak = new[]
                {
                    P("Sarbidea", "Container_small", -0.85f, 0.85f),
                    P("Sarbidea", "Vents", -1f, -0.55f),
                    P("Sarbidea", "Old_computer", 0.85f, -0.95f),
                    P("Biltegia", "Supply_box", 1f, 1f),
                    P("Biltegia", "Container_Big", -0.75f, -0.5f),
                    P("Biltegia", "Container_Big", -0.45f, -0.5f),
                    P("Biltegia", "Container_BigOpen", -0.6f, 0.3f),
                    P("Biltegia", "Rack", 0.3f, -0.3f),
                    P("Osagaiak", "Rack", -0.4f, 0.75f),
                    P("Osagaiak", "Rack", 0.5f, 0.75f),
                    P("Osagaiak", "Container_small", -0.85f, -0.15f),
                    P("Osagaiak", "Container_Big", 0.8f, -0.35f),
                    P("Osagaiak", "Box1", 0.1f, 0.05f, 0f, true),
                    P("Fusibleak1", "Tubes", -0.9f, 0.3f),
                    P("Fusibleak1", "Container_small", 0.65f, 0.1f),
                    P("Fusibleak2", "Tubes", -0.9f, -0.7f),
                    P("Fusibleak2", "Container_small", -0.35f, 0.15f, 90f),
                    P("Kontrola", "Old_computer", -0.8f, 0.85f),
                    P("Kontrola", "Rack", 0.5f, 0.75f),
                    P("Bobinak", "Oxygen_sint", 0f, 1f),
                    P("Bobinak", "Container_BigOpen", 0.85f, -0.3f),
                    P("Bobinak", "Geo_base", 0.25f, -0.4f, 90f),
                    P("Sorgailuak", "Container_Big", -0.25f, -0.05f),
                    P("Sorgailuak", "Container_small", 0.2f, 0.28f, 90f),
                    P("Sorgailuak", "Box1", 0.15f, -0.32f, 0f, true),
                    P("Irteera", "Container_Big", -1f, 0.9f),
                },
                Apaingarriak = ap.ToArray(),
                KolisioAzkenak = new[]
                {
                    // Hirugarren sorgailuaren hodi bertikalak iparraldeko pasabidea zeharkatzen du (4,4-5,8 m): oztopoa da eta alboan 1,6 m libre geratzen dira.
                    new MailaKolisio { Sala = "Sorgailuak", X = 14.1f, Y = 5.1f, Z = 12.5f, Tamaina = new Vector3(0.5f, 1.4f, 0.5f) },
                    // Hegoaldeko eta iparraldeko pasabideak zeharkatzen dituen hodi etzana (6,025-6,375 m; pasabidea 4,6 m-ra): 1,425 m libre azpian.
                    // Zutik (1,8 m) talka egiten du eta makurtuta (1,1 m) pasatzen da. Hodi hau ez da mugitu.
                    new MailaKolisio { Sala = "Sorgailuak", X = 15.5f, Y = 6.2f, Z = 10.5f, Tamaina = new Vector3(0.35f, 0.35f, 18.75f) },
                },
                Kamerak = new[]
                {
                    K("Sarbidea", 0.4f, 10.4f, 2.5f, 12.5f),
                    K("Pasilloa", 3.6f, 11.5f, 9f, 11.5f),
                    K("Sorgailuak", 13.4f, 11.5f, 17.5f, 9.5f, 1.65f, 3.5f),
                    K("Pasabidea", 17.7f, 9f, 17.7f, 12.5f, 6.25f, 4f),
                    K("Pasabidea_behera", 14.5f, 12.7f, 16.5f, 9f, 6.25f, 1f),
                    K("Hodiak", 2.7f, 3.3f, 0.5f, 6.5f, 1.65f, 3f),
                    K("Fusibleak", 9.5f, 20.3f, 9.5f, 22.8f),
                    K("Bobinak", 21.3f, 9f, 24.5f, 12f),
                    K("Laberintoa", 22.5f, 0.3f, 20.5f, -2.5f),
                    K("Kontrola", 15.5f, 16.3f, 15.5f, 18.8f, 1.65f, 2.5f),
                },
                Misioa = new MisioDef
                {
                    Izena = "2. misioa: Energia",
                    Hurrengoa = "Mision03_Erreaktorea",
                    SpawnX = 1.5f,
                    SpawnZ = 11.5f,
                    SpawnYaw = 90f,
                    IrteeraX = 23.5f,
                    IrteeraZ = 22.5f,
                    Faseak = new[]
                    {
                        new MisioFaseDef { Deskribapena = "Berrezarri hiru energia-puntuak", Helburuak = new[] { "fusibleak1", "fusibleak2", "pasabidea" } },
                        new MisioFaseDef { Deskribapena = "Itzuli kontrol-gelara eta aktibatu sarea", Helburuak = new[] { "sarea" } },
                    },
                    Terminalak = new[]
                    {
                        new MisioTerminalDef { Id = "fusibleak1", Izena = "1. fusible-koadroa aktibatu", Sala = "Fusibleak1", X = 8f, Z = 22.45f },
                        new MisioTerminalDef { Id = "fusibleak2", Izena = "2. fusible-koadroa aktibatu", Sala = "Fusibleak2", X = 11f, Z = 0.45f },
                        new MisioTerminalDef { Id = "pasabidea", Izena = "Sorgailu nagusia aktibatu", Sala = "Sorgailuak", X = 13.45f, Y = 4.6f, Z = 12.78f, Yaw = 90f },
                        new MisioTerminalDef { Id = "sarea", Izena = "Energia-sarea berrabiarazi", Sala = "Kontrola", X = 15.5f, Z = 18.45f },
                    },
                    Bildumak = new List<Vector3>
                    {
                        new Vector3(19.4f, 1f, -2.6f),
                        new Vector3(1.5f, 1f, 6.6f),
                        new Vector3(0.5f, 1f, 18.5f),
                        new Vector3(24.5f, 1f, 12.5f),
                    },
                    Markak = new List<(MarkaMota, Vector3)>
                    {
                        (MarkaMota.Etsaia, new Vector3(15.5f, 0f, 10.8f)),
                        (MarkaMota.Etsaia, new Vector3(15.5f, 4.6f, 8.3f)),
                        (MarkaMota.Etsaia, new Vector3(23f, 0f, 9f)),
                        (MarkaMota.Etsaia, new Vector3(9.5f, 0f, 21f)),
                        (MarkaMota.Etsaia, new Vector3(16f, 0f, 24f)),
                        (MarkaMota.Kobertura, new Vector3(14.9f, 0f, 10.2f)),
                        (MarkaMota.Kobertura, new Vector3(16.1f, 0f, 10f)),
                        (MarkaMota.Arriskua, new Vector3(12f, 0f, 4.5f)),
                        (MarkaMota.Arriskua, new Vector3(1.5f, 0f, 4f)),
                    },
                },
            };
        }

        // ========== 3. MISIOA: ERREAKTOREA ==========

        private static MailaDef Mision03()
        {
            Color gorria = new Color(1f, 0.1f, 0.06f);
            Color ziana = new Color(0.2f, 0.9f, 1f);
            Color anbarra = new Color(1f, 0.5f, 0.15f);
            Color iluna = new Color(0.7f, 0.1f, 0.1f);
            Color zuria = new Color(0.8f, 0.9f, 1f);
            const string Ga = "PF_ArgiPanela_Gorria";
            const string Zi = "PF_ArgiPanela_Ziana";
            const string HG = "PF_Hodia_Grisa";
            const float Pl = 3f;

            // Erreaktorea: ganberaren erdian (21,10). Instalazio osoko elementurik ezagunena.
            const float Rx = 21f;
            const float Rz = 10f;
            var ap = new List<MailaApaingarri>
            {
                A("Erreaktorea", "PF_Oina", Rx, 0.6f, Rz, 0f, 5f, 1.2f, 5f),
                A("Erreaktorea", "PF_Zutabea", Rx, 1.4f, Rz, 0f, 4f, 0.4f, 4f),
                A("Erreaktorea", "PF_Nukleoa", Rx, 4.9f, Rz, 0f, 2.1f, 7f, 2.1f),
                A("Erreaktorea", "PF_Beira_Zilindroa", Rx, 4.9f, Rz, 0f, 3f, 6.8f, 3f),
                A("Erreaktorea", "PF_Oina", Rx, 8.6f, Rz, 0f, 4.2f, 0.8f, 4.2f),
                A("Erreaktorea", "PF_Zutabea", Rx, 9.4f, Rz, 0f, 1.2f, 1.2f, 1.2f),
            };

            foreach ((float dx, float dz) in new[] { (0.45f, 0.45f), (-0.45f, 0.45f), (0.45f, -0.45f), (-0.45f, -0.45f) })
            {
                ap.Add(A("Erreaktorea", "PF_Zutabea", Rx + dx, 4.9f, Rz + dz, 0f, 0.3f, 7.4f, 0.3f));
            }

            // Hormetako hodiak eta pantailak.
            ap.Add(HodiX("Erreaktorea", HG, 18f, 24f, 8.4f, 7.3f, 0.6f));
            ap.Add(HodiX("Erreaktorea", HG, 18f, 24f, 8.4f, 12.7f, 0.6f));
            ap.Add(HodiZ("Erreaktorea", HG, 18.3f, 7f, 13f, 7.6f, 0.45f));
            ap.Add(HodiZ("Erreaktorea", HG, 23.7f, 7f, 13f, 7.6f, 0.45f));
            ap.Add(A("Erreaktorea", "PF_Pantaila_Gorria", 21f, 7f, 7.05f, 0f, 10f, 2f, 0.05f));
            ap.Add(A("Erreaktorea", "PF_Pantaila_Gorria", 21f, 7f, 12.95f, 0f, 10f, 2f, 0.05f));
            ap.Add(A("Erreaktorea", "PF_Pantaila_Urdina", 18.05f, 7f, 10f, 0f, 0.05f, 2f, 10f));
            ap.Add(A("Erreaktorea", "PF_Pantaila_Urdina", 23.95f, 7f, 10f, 0f, 0.05f, 2f, 10f));

            // Kontrol-gelak.
            ap.Add(A("Kontrola1", "PF_Pantaila_Urdina", 8f, 3.2f, 17.95f, 0f, 7f, 1.8f, 0.05f));
            ap.Add(A("Kontrola2", "PF_Pantaila_Gorria", 8f, 3.2f, 4.95f, 0f, 7f, 1.8f, 0.05f));
            ap.Add(A("Segurtasuna", "PF_Pantaila_Gorria", 0.05f, 2.4f, 10.5f, 0f, 0.05f, 0.8f, 2.4f));
            // Deskontaminazioa: dutxa-zutabeak.
            foreach (float z in new[] { 9.35f, 11.65f })
            {
                foreach (float x in new[] { 9.6f, 10.4f })
                {
                    ap.Add(A("Deskontaminazioa", "PF_Zutabea", x, 2.5f, z, 0f, 0.3f, 5f, 0.3f, true));
                    ap.Add(A("Deskontaminazioa", "PF_Bobina", x, 2.2f, z, 0f, 0.42f, 0.6f, 0.42f));
                }
            }

            // Azken borroka: hodi handiak kobertura gisa.
            ap.Add(HodiZ("AzkenBorroka", HG, 27f, 13f, 15.4f, 0.5f, 0.9f, true));
            ap.Add(HodiX("AzkenBorroka", HG, 25f, 32f, 4.3f, 17.6f, 0.5f));

            float erdi = 0.75f;
            float x0 = Rx - 1.5f;
            float x1 = Rx + 1.5f;
            float z0 = Rz - 1.5f;
            float z1 = Rz + 1.5f;
            float hx0 = x0 + erdi;
            float hx1 = x1 - erdi;
            float hz0 = z0 + erdi;
            float hz1 = z1 - erdi;
            float stairW = 0.4f;
            float wZ = 9f;
            float eZ = 11f;

            return new MailaDef
            {
                Izena = "Mision03_Erreaktorea",
                Erroa = "Sektorea3",
                Aurrizkia = "M3",
                Giroa = new Color(0.08f, 0.04f, 0.05f),
                KoloreArintzea = 0.3f,
                Gelak = new[]
                {
                    G("Segurtasuna", 0, 9, 3, 3, "SEGURTASUNA", gorria, 2.8f, zoruKit: true, panela: Ga),
                    G("Deskontaminazioa", 9, 9, 2, 3, "DESKONTAMINAZIOA", zuria, 4f, panela: Zi),
                    G("J1", 14, 10, 1, 1, "J1", gorria, 2.6f, zoruKit: true, panela: Ga, pultsua: true),
                    G("J2", 14, 16, 1, 1, "J2", anbarra, 2.6f, zoruKit: true, panela: Ga),
                    G("J3", 14, 3, 1, 1, "J3", anbarra, 2.6f, zoruKit: true, panela: Ga),
                    G("J4", 20, 16, 1, 1, "J4", gorria, 2.6f, zoruKit: true, panela: Ga, pultsua: true),
                    G("J5", 20, 3, 1, 1, "J5", gorria, 2.6f, zoruKit: true, panela: Ga, pultsua: true),
                    G("J6", 28, 9, 1, 1, "J6", anbarra, 2.6f, zoruKit: true, panela: Ga),
                    G("Erreaktorea", 18, 7, 6, 6, "ERREAKTOREA", iluna, 4.5f, ilarak: 4, panela: Ga, tartea: 3f),
                    G("Kontrola1", 6, 15, 4, 3, "KONTROLA1", ziana, 4f, panela: Zi),
                    G("Kontrola2", 6, 2, 4, 3, "KONTROLA2", gorria, 3f, panela: Ga),
                    G("Mantentzea", 26, 1, 5, 5, "MANTENTZEA", iluna, 1.3f, altuera: 3.2f, panela: Ga),
                    G("AzkenBorroka", 25, 13, 7, 5, "AZKENBORROKA", gorria, 5.5f, ilarak: 3, panela: Ga, pultsua: true),
                    G("Irteera", 35, 14, 4, 3, "IRTEERA", zuria, 5f, panela: Zi),
                },
                Loturak = new[]
                {
                    L("Segurtasuna", E, 10, "Deskontaminazioa", true, false, HodiArgia.Erdia, gorria),
                    L("Deskontaminazioa", E, 10, "J1", true, true, HodiArgia.Argitua, zuria),
                    L("J1", E, 10, "Erreaktorea", false, false, HodiArgia.Argitua, ziana, hodiak: HG),
                    L("J1", I, 14, "J2", false, false, HodiArgia.Iluna, gorria, hodiak: HG),
                    L("J3", I, 14, "J1", false, false, HodiArgia.Iluna, gorria, hodiak: HG),
                    L("J2", E, 16, "J4", false, false, HodiArgia.Erdia, gorria),
                    L("J3", E, 3, "J5", false, false, HodiArgia.Erdia, gorria),
                    L("J4", H, 20, "Erreaktorea", true, true, HodiArgia.Argitua, ziana, seinaleaB: "J4"),
                    L("J5", I, 20, "Erreaktorea", true, true, HodiArgia.Argitua, ziana),
                    L("J2", M, 16, "Kontrola1", false, false, HodiArgia.Erdia, ziana),
                    L("J3", M, 3, "Kontrola2", false, false, HodiArgia.Iluna, gorria),
                    L("J5", E, 3, "Mantentzea", false, false, HodiArgia.Iluna, iluna, hodiak: HG),
                    L("Erreaktorea", E, 9, "J6", true, true, HodiArgia.Erdia, anbarra),
                    L("J6", H, 28, "Mantentzea", false, false, HodiArgia.Iluna, iluna),
                    L("J6", I, 28, "AzkenBorroka", false, false, HodiArgia.Erdia, gorria),
                    L("J4", E, 16, "AzkenBorroka", false, false, HodiArgia.Iluna, gorria, hodiak: HG),
                    L("AzkenBorroka", E, 15, "Irteera", true, true, HodiArgia.Argitua, zuria, blokeoa: 2, seinaleaA: "IHESA", seinaleaB: "AZKENBORROKA"),
                },
                BarneHormak = new[]
                {
                    // Mantentze-laberintoa: ekialdetik iparraldera doan bide bihurria, bi itsurekin.
                    HV("Mantentzea", 27, 2), HV("Mantentzea", 27, 3), HV("Mantentzea", 27, 4),
                    HH("Mantentzea", 27, 4), HH("Mantentzea", 28, 4),
                    HV("Mantentzea", 29, 3), HV("Mantentzea", 29, 2),
                    HH("Mantentzea", 28, 2), HH("Mantentzea", 29, 3),
                    HV("Mantentzea", 30, 4), HV("Mantentzea", 30, 5),
                },
                Plataformak = new[]
                {
                    Plat("Erreaktorea", x0, z0, x1, hz0, Pl),
                    Plat("Erreaktorea", x0, hz1, x1, z1, Pl),
                    Plat("Erreaktorea", x0, hz0, hx0, hz1, Pl),
                    Plat("Erreaktorea", hx1, hz0, x1, hz1, Pl),
                },
                Barandak = new[]
                {
                    // Barnekoak, nukleora begira.
                    Bar("Erreaktorea", hx0, hz0, hx1, hz0, Pl),
                    Bar("Erreaktorea", hx0, hz1, hx1, hz1, Pl),
                    Bar("Erreaktorea", hx0, hz0, hx0, hz1, Pl),
                    Bar("Erreaktorea", hx1, hz0, hx1, hz1, Pl),
                    // Kanpokoak, eskaileren tarteak utzita.
                    Bar("Erreaktorea", x0, z0, x1, z0, Pl),
                    Bar("Erreaktorea", x0, z1, x1, z1, Pl),
                    Bar("Erreaktorea", x0, z0, x0, wZ - stairW, Pl),
                    Bar("Erreaktorea", x0, wZ + stairW, x0, z1, Pl),
                    Bar("Erreaktorea", x1, z0, x1, eZ - stairW, Pl),
                    Bar("Erreaktorea", x1, eZ + stairW, x1, z1, Pl),
                },
                Eskailerak = new[]
                {
                    new MailaEskailera { Sala = "Erreaktorea", X = x0, Z = wZ, Alde = E, Y0 = 0f, Y1 = Pl, Zabalera = stairW * 2f * Gelaxka },
                    new MailaEskailera { Sala = "Erreaktorea", X = x1, Z = eZ, Alde = M, Y0 = 0f, Y1 = Pl, Zabalera = stairW * 2f * Gelaxka },
                },
                KolisioGehigarriak = new[]
                {
                    // Erreaktorearen oina: oktogonoa bi kutxekin.
                    new MailaKolisio { Sala = "Erreaktorea", X = Rx, Y = 5f, Z = Rz, Tamaina = new Vector3(4.8f, 10f, 3.2f) },
                    new MailaKolisio { Sala = "Erreaktorea", X = Rx, Y = 5f, Z = Rz, Tamaina = new Vector3(3.2f, 10f, 4.8f) },
                },
                ArgiGehigarriak = new[]
                {
                    new MailaArgi { Izena = "Argia_Nukleoa_Behea", X = Rx + 0.7f, Y = 2f, Z = Rz, Kolorea = ziana, Intentsitatea = 10f, Tartea = 14f, Pultsua = true },
                    new MailaArgi { Izena = "Argia_Nukleoa_Goia", X = Rx - 0.7f, Y = 6f, Z = Rz, Kolorea = ziana, Intentsitatea = 14f, Tartea = 16f },
                    new MailaArgi { Izena = "Argia_Alarma_1", X = 18.5f, Y = 8.5f, Z = 7.5f, Kolorea = gorria, Intentsitatea = 7f, Tartea = 14f, Pultsua = true },
                    new MailaArgi { Izena = "Argia_Alarma_2", X = 23.5f, Y = 8.5f, Z = 12.5f, Kolorea = gorria, Intentsitatea = 7f, Tartea = 14f, Pultsua = true },
                },
                Propak = new[]
                {
                    P("Segurtasuna", "Container_small", -0.85f, 0.85f),
                    P("Segurtasuna", "Old_computer", 0.85f, 0.95f),
                    P("Segurtasuna", "Vents", -1f, -0.5f),
                    P("Erreaktorea", "Container_Big", -0.8f, -0.75f),
                    P("Erreaktorea", "Container_BigOpen", 0.8f, 0.8f),
                    P("Erreaktorea", "Box1", -0.7f, 0.75f, 0f, true),
                    P("Erreaktorea", "Container_small", 0.75f, -0.7f),
                    P("Erreaktorea", "Container_Big", -0.75f, 0.3f),
                    P("Erreaktorea", "Container_small", 0.25f, 0.92f, 90f),
                    P("Kontrola1", "Rack", 0.6f, -0.3f),
                    P("Kontrola1", "Old_computer", -0.8f, 0.85f),
                    P("Kontrola2", "Rack", -0.6f, 0.3f),
                    P("Kontrola2", "Old_computer", 0.1f, 0.85f),
                    P("AzkenBorroka", "Supply_box", 0.1f, 0.1f),
                    P("AzkenBorroka", "Container_Big", -0.3f, 0.5f),
                    P("AzkenBorroka", "Container_BigOpen", 0.55f, -0.45f),
                    P("AzkenBorroka", "Box1", 0.75f, 0.55f, 0f, true),
                    P("AzkenBorroka", "Container_small", -0.2f, -0.6f, 90f),
                    P("AzkenBorroka", "Rack", 0.95f, -0.95f),
                    P("Irteera", "Container_Big", -0.9f, 0.9f),
                },
                Apaingarriak = ap.ToArray(),
                Kamerak = new[]
                {
                    K("Segurtasuna", 0.4f, 9.4f, 2.5f, 11.5f),
                    K("Deskontaminazioa", 9.15f, 9.3f, 10.8f, 11.8f),
                    K("Pasilloa", 15.3f, 10.5f, 18.5f, 10.5f),
                    K("Erreaktorea_sarrera", 18.3f, 10.5f, Rx, Rz, 1.65f, 4f),
                    K("Erreaktorea_behean", 18.6f, 8f, Rx, Rz, 1.3f, 6f),
                    K("Erreaktorea_gainean", 19.9f, 11.1f, Rx, Rz, Pl + 1.65f, 5f),
                    K("Erreaktorea_kanpo", 23.7f, 7.4f, Rx, Rz, 9.2f, 3f),
                    K("Kontrola", 8f, 15.3f, 8f, 17.8f, 1.65f, 2.5f),
                    K("Laberintoa", 30.6f, 1.5f, 26.4f, 1.5f),
                    K("AzkenBorroka", 25.3f, 13.5f, 31f, 17f),
                    K("Irteera", 35.3f, 15.5f, 38.8f, 15.5f),
                },
                Misioa = new MisioDef
                {
                    Izena = "3. misioa: Erreaktorea",
                    Hurrengoa = null,
                    SpawnX = 1.5f,
                    SpawnZ = 10.5f,
                    SpawnYaw = 90f,
                    IrteeraX = 38.3f,
                    IrteeraZ = 15.5f,
                    Faseak = new[]
                    {
                        new MisioFaseDef { Deskribapena = "Egonkortu erreaktorea hiru kontsoletatik", Helburuak = new[] { "behea", "iparra", "hegoa" } },
                        new MisioFaseDef { Deskribapena = "Desaktibatu larrialdi-protokoloa kontrol-gelan", Helburuak = new[] { "protokoloa" } },
                    },
                    Terminalak = new[]
                    {
                        new MisioTerminalDef { Id = "behea", Izena = "Beheko kontsola egonkortu", Sala = "Erreaktorea", X = 21f, Z = 7.45f },
                        new MisioTerminalDef { Id = "iparra", Izena = "Iparreko kontsola egonkortu", Sala = "Erreaktorea", X = 21f, Y = Pl, Z = 11.3f, Yaw = 90f },
                        new MisioTerminalDef { Id = "hegoa", Izena = "Hegoaldeko kontsola egonkortu", Sala = "Erreaktorea", X = 21f, Y = Pl, Z = 8.7f, Yaw = 90f },
                        new MisioTerminalDef { Id = "protokoloa", Izena = "Larrialdi-protokoloa desaktibatu", Sala = "Kontrola1", X = 8f, Z = 17.45f },
                    },
                    Bildumak = new List<Vector3>
                    {
                        new Vector3(26.5f, 1f, 1.5f),
                        new Vector3(8f, 1f, 2.5f),
                        new Vector3(23.4f, 1f, 7.5f),
                        new Vector3(38f, 1f, 16.5f),
                    },
                    Markak = new List<(MarkaMota, Vector3)>
                    {
                        (MarkaMota.Etsaia, new Vector3(19f, 0f, 8f)),
                        (MarkaMota.Etsaia, new Vector3(23f, 0f, 12f)),
                        (MarkaMota.Etsaia, new Vector3(19.9f, Pl, 11f)),
                        (MarkaMota.Etsaia, new Vector3(27f, 0f, 15f)),
                        (MarkaMota.Etsaia, new Vector3(30f, 0f, 16.5f)),
                        (MarkaMota.Etsaia, new Vector3(30.5f, 0f, 14f)),
                        (MarkaMota.Kobertura, new Vector3(27.5f, 0f, 16f)),
                        (MarkaMota.Kobertura, new Vector3(19f, 0f, 11.2f)),
                        (MarkaMota.Arriskua, new Vector3(21f, 0f, 7.8f)),
                    },
                    Mugikorrak = new List<MailaMugikor>
                    {
                        new MailaMugikor { Prefab = "PF_Din_Eraztuna_Ziana", X = Rx, Y = 2.4f, Z = Rz, Biraketa = Vector3.zero, Eskala = new Vector3(4.4f, 2.5f, 4.4f), Abiadura = new Vector3(0f, 25f, 0f) },
                        new MailaMugikor { Prefab = "PF_Din_Eraztuna_Gorria", X = Rx, Y = 5.4f, Z = Rz, Biraketa = new Vector3(12f, 0f, 0f), Eskala = new Vector3(4.6f, 2.5f, 4.6f), Abiadura = new Vector3(0f, -35f, 0f) },
                        new MailaMugikor { Prefab = "PF_Din_Eraztuna_Ziana", X = Rx, Y = 7.8f, Z = Rz, Biraketa = Vector3.zero, Eskala = new Vector3(4f, 2.5f, 4f), Abiadura = new Vector3(0f, 45f, 0f) },
                    },
                },
            };
        }
    }
}
