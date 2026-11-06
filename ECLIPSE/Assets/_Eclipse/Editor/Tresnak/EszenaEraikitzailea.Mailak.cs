using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Eclipse.Efektuak;
using Eclipse.Misioak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Hiru mailen eraikitzailea: maila bakoitza datu-definizio bat da (MailaDef) eta eszena independente bat sortzen du.
    /// Prototipoaren sistema bera (gelaxka = 3,75 m), hobekuntzekin: altuera aldakorra, barne-hormak, leihoak, plataformak,
    /// eskailerak, misioa eta triangelu gutxiago. 01_Instalazioa eta 02_PrototipoDiseinua ez dira ukitzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string MailaMenua = "ECLIPSE/Mailak/";
        private const string EszenaKarpeta = "Assets/_Eclipse/Eszenak/";
        private const string MisioaIzena = "Misioa";
        private const string ZoruPropioa = "PF_Zorua_Maila";
        private const string Plaka = "PF_Plaka";
        private const string Barra = "PF_Barra";
        private const float ZoruTile = 8f;
        private const float ZoruLodiera = 0.15f;

        // Profile1-ek 109.000 triangelu ditu eta 88.000 lau lauza hauenak dira: Floor1 biz ordezkatzen dira, itxura berarekin.
        private const string HodiZoruAstunak = "Floor2|Floor5 (1)|Floor4 (2)|Floor4 (3)";

        private static readonly Dictionary<string, Func<MailaDef>> Mailak = new Dictionary<string, Func<MailaDef>>
        {
            ["1"] = Mision01,
            ["2"] = Mision02,
            ["3"] = Mision03,
        };

        [MenuItem(MailaMenua + "Mision01_Laborategia/Aurrebista")]
        private static void M1Aurrebista() => MailaExekutatu(Mision01(), false);

        [MenuItem(MailaMenua + "Mision01_Laborategia/Aplikatu")]
        private static void M1Aplikatu() => MailaExekutatu(Mision01(), true);

        [MenuItem(MailaMenua + "Mision01_Laborategia/Probak (ez du ezer gordetzen)")]
        private static void M1Probak() => MailaProbaMenua(Mision01());

        [MenuItem(MailaMenua + "Mision02_Energia/Aurrebista")]
        private static void M2Aurrebista() => MailaExekutatu(Mision02(), false);

        [MenuItem(MailaMenua + "Mision02_Energia/Aplikatu")]
        private static void M2Aplikatu() => MailaExekutatu(Mision02(), true);

        [MenuItem(MailaMenua + "Mision02_Energia/Probak (ez du ezer gordetzen)")]
        private static void M2Probak() => MailaProbaMenua(Mision02());

        [MenuItem(MailaMenua + "Mision03_Erreaktorea/Aurrebista")]
        private static void M3Aurrebista() => MailaExekutatu(Mision03(), false);

        [MenuItem(MailaMenua + "Mision03_Erreaktorea/Aplikatu")]
        private static void M3Aplikatu() => MailaExekutatu(Mision03(), true);

        [MenuItem(MailaMenua + "Mision03_Erreaktorea/Probak (ez du ezer gordetzen)")]
        private static void M3Probak() => MailaProbaMenua(Mision03());

        private static void MailaProbaMenua(MailaDef d)
        {
            if (SceneManager.GetActiveScene().name != d.Izena)
            {
                Debug.LogError($"{Marka}_ERROR Ireki lehenago {d.Izena} eszena.");
                return;
            }

            MailaProbatu(d);
        }

        private static MailaDef BatchMailaLortu()
        {
            string[] argumentuak = Environment.GetCommandLineArgs();
            int indizea = Array.IndexOf(argumentuak, "-eclipseMaila");
            if (indizea < 0 || indizea + 1 >= argumentuak.Length || !Mailak.TryGetValue(argumentuak[indizea + 1], out Func<MailaDef> sortzailea))
            {
                throw new ArgumentException("Erabili: -eclipseMaila 1|2|3");
            }

            return sortzailea();
        }

        /// <summary>Batchmode (Unity itxita): aktiboak, aurrebista, aplikatu eta gorde.</summary>
        public static void BatchMailaEraiki()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                int aktiboak = EszenaPropioa.MailaAktiboak(true);
                Debug.Log($"{Marka}_AKTIBOAK {aktiboak} aldaketa");
                if (MailaExekutatu(d, false) && MailaExekutatu(d, true))
                {
                    kodea = EditorSceneManager.SaveScene(SceneManager.GetActiveScene()) ? 0 : 1;
                    AssetDatabase.SaveAssets();
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Batchmode: proba automatikoak eta argazkiak. Eszena ez da gordetzen.</summary>
        public static void BatchMailaProbatu()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                bool ondo = MailaProbatu(d);
                List<string> ikuspegiak = EbidentziaKapturak.MailaArgazkiak(d.Aurrizkia, d.Erroa, MailaKamerak(d));
                var errendimendua = new Informea(Tresna, Marka, $"{d.Izena}: Errendimendua", d.Bidea, d.Erroa);
                errendimendua.Atala("Ikuspegiak 1280x720", ikuspegiak);
                errendimendua.AmaituOndo($"{ikuspegiak.Count} ikuspegi neurtuta");
                kodea = ondo ? 0 : 1;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        // ---------- Exekuzioa ----------

        private static bool MailaExekutatu(MailaDef d, bool aplikatu)
        {
            string modua = $"{d.Izena}: {(aplikatu ? "Aplikatu" : "Aurrebista")}";
            Debug.Log($"{Marka}_HASI {modua}");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"{Marka}_ERROR Play moduan ezin da exekutatu.");
                return false;
            }

            var informea = new Informea(Tresna, Marka, modua, d.Bidea, d.Erroa);
            try
            {
                if (aplikatu)
                {
                    EszenaPropioa.MailaAktiboak(true);
                }

                var plana = new Plana { ErroakSortu = true };
                MailaPlanaEraiki(d, plana, aplikatu, out List<ProtoArgi> argiak, out string planoa, out MisioPlana misioa);
                informea.Lerroa(string.Empty);
                informea.Lerroa("```text");
                informea.Lerroa(planoa);
                informea.Lerroa("```");
                informea.Atala("Salak", plana.SalaLerroak);
                informea.Atala("Oharrak", plana.Oharrak);
                if (plana.Erroreak.Count > 0)
                {
                    informea.Atala("Erroreak", plana.Erroreak);
                    informea.AmaituErrorearekin($"{plana.Erroreak.Count} errore: ez da ezer aldatu");
                    return false;
                }

                SektorePlana sektorea = plana.Sektoreak[0];
                informea.Lerroa($"- **Piezak:** {sektorea.Piezak.Count}; **kolisioak:** {sektorea.Kolisioak.Count}; **argiak:** {argiak.Count}");

                if (!aplikatu)
                {
                    Scene irekita = SceneManager.GetActiveScene();
                    if (irekita.path != d.Bidea)
                    {
                        _azkenAldaketak = -1;
                        informea.AmaituOndo($"{sektorea.Piezak.Count} pieza planean; eszena ez dago irekita (ez da ezer aldatu)");
                        return true;
                    }

                    var aurreikusiak = new Aldaketak();
                    Sinkronizatu(plana, false, irekita, aurreikusiak);
                    GameObject dagoena = irekita.GetRootGameObjects().FirstOrDefault(o => o.name == d.Erroa);
                    int argiAurreikusiak = dagoena != null
                        ? HodiArgiakSinkronizatu(sektorea, dagoena.transform, false) + ProtoArgiakSinkronizatu(argiak, dagoena.transform, false)
                        : argiak.Count;
                    _azkenAldaketak = aurreikusiak.Guztira + argiAurreikusiak;
                    InformatuAldaketak(informea, aurreikusiak);
                    informea.Lerroa($"- **Argi-aldaketak:** {argiAurreikusiak}. Misioaren objektuak Aplikatu-n bakarrik sinkronizatzen dira, eta aurrebistak ez ditu zenbatzen.");
                    informea.AmaituOndo($"{_azkenAldaketak} aldaketa aplikatzeko (ez da ezer aldatu)");
                    return true;
                }

                if (!MailaEszenaPrestatu(d, out Scene eszena))
                {
                    informea.AmaituErrorearekin("Eszena ez da prestatu: erabiltzaileak utzi du");
                    return false;
                }

                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName($"ECLIPSE {d.Izena}");
                int taldea = Undo.GetCurrentGroup();
                var aldaketak = new Aldaketak();
                Sinkronizatu(plana, true, eszena, aldaketak);
                Transform erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa).transform;
                int hodiArgiak = HodiArgiakSinkronizatu(sektorea, erroa);
                int argiAldaketak = ProtoArgiakSinkronizatu(argiak, erroa);
                int pultsuak = PultsuakSinkronizatu(argiak, erroa);
                int misioAldaketak = MisioaSinkronizatu(d, misioa, erroa, eszena);
                bool ingurunea = MailaInguruneaEzarri(d);
                Undo.CollapseUndoOperations(taldea);
                int guztira = aldaketak.Guztira + hodiArgiak + argiAldaketak + pultsuak + misioAldaketak;
                _azkenAldaketak = guztira;
                if (guztira > 0 || ingurunea)
                {
                    EditorSceneManager.MarkSceneDirty(eszena);
                }

                InformatuAldaketak(informea, aldaketak);
                informea.Lerroa($"- **Hodi-argiak:** {hodiArgiak}; **argi propioak:** {argiAldaketak}; **misioa:** {misioAldaketak}");

                List<string> hutsak = Egiaztatu(plana, eszena);
                informea.Atala("Egiaztapena: pieza hauek ez daude espero zen tokian", hutsak);
                var bigarrena = new Aldaketak();
                Sinkronizatu(plana, false, eszena, bigarrena);
                int bigarrenak = HodiArgiakSinkronizatu(sektorea, erroa, false) + ProtoArgiakSinkronizatu(argiak, erroa, false) + MisioaSinkronizatu(d, misioa, erroa, eszena);
                if (hutsak.Count > 0 || bigarrena.Guztira + bigarrenak > 0)
                {
                    informea.AmaituErrorearekin($"{hutsak.Count} pieza okerreko tokian; {bigarrena.Guztira + bigarrenak} aldaketa geratzen dira");
                    return false;
                }

                informea.AmaituOndo($"{guztira} aldaketa ({aldaketak.Laburpena})");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
        }

        private static bool MailaEszenaPrestatu(MailaDef d, out Scene eszena)
        {
            eszena = SceneManager.GetActiveScene();
            if (eszena.path == d.Bidea)
            {
                return true;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            if (File.Exists(d.Bidea))
            {
                eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                return true;
            }

            eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return EditorSceneManager.SaveScene(eszena, d.Bidea);
        }

        private static bool MailaInguruneaEzarri(MailaDef d)
        {
            var zerua = AssetDatabase.LoadAssetAtPath<Material>(ZeruBidea);
            if (RenderSettings.skybox == zerua && RenderSettings.ambientMode == AmbientMode.Flat && RenderSettings.ambientLight == d.Giroa)
            {
                return false;
            }

            RenderSettings.skybox = zerua;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = d.Giroa;
            return true;
        }

        // ---------- Plana ----------

        private static void MailaPlanaEraiki(MailaDef d, Plana plana, bool aplikatu, out List<ProtoArgi> argiak, out string planoa, out MisioPlana misioa)
        {
            argiak = new List<ProtoArgi>();
            misioa = new MisioPlana();
            planoa = MailaSareaBalidatu(d, plana);
            if (plana.Erroreak.Count > 0)
            {
                return;
            }

            Dictionary<string, Modulua> m = MailaModuluak(d, plana, aplikatu);
            if (plana.Erroreak.Count > 0)
            {
                return;
            }

            Neurriak n = NeurriakLortu(m, plana);
            if (n == null)
            {
                return;
            }

            var sektorea = new SektorePlana { Izena = d.Erroa, Posizioa = Vector3.zero, Geometria = true };
            plana.Sektoreak.Add(sektorea);

            Dictionary<string, MailaGela> gelak = d.Gelak.ToDictionary(g => g.Izena);
            var ateak = new Dictionary<(string, Aldea), Dictionary<int, string>>();
            foreach (MailaGela g in d.Gelak)
            {
                foreach (Aldea a in Enum.GetValues(typeof(Aldea)))
                {
                    ateak[(g.Izena, a)] = new Dictionary<int, string>();
                }
            }

            foreach (MailaLotura l in d.Loturak)
            {
                ateak[(l.A, l.AldeA)].Add(l.Gurutzea, l.SeinaleaA ?? (l.B != null ? gelak[l.B].Seinalea : HelmugaItsua));
                if (l.B != null)
                {
                    ateak[(l.B, Kontrakoa(l.AldeA))].Add(l.Gurutzea, l.SeinaleaB ?? gelak[l.A].Seinalea);
                }
            }

            foreach (MailaGela g in d.Gelak)
            {
                MailaSala(sektorea, d, g, m, n, ateak, argiak, plana);
            }

            foreach (MailaLotura l in d.Loturak)
            {
                MailaKorridorea(sektorea, d, l, gelak, m, n, argiak, misioa, plana);
            }

            foreach (MailaHorma h in d.BarneHormak)
            {
                MailaBarneHorma(sektorea, gelak[h.Sala], h, m, n, plana);
            }

            foreach (MailaPlataforma p in d.Plataformak)
            {
                MailaPlataformaPlanifikatu(sektorea, p, m, n, plana);
            }

            foreach (MailaBaranda b in d.Barandak)
            {
                MailaBarandaPlanifikatu(sektorea, b, m, n, plana);
            }

            foreach (MailaEskailera e in d.Eskailerak)
            {
                MailaEskaileraPlanifikatu(sektorea, e, m, n, plana);
            }

            foreach (MailaApaingarri a in d.Apaingarriak)
            {
                Vector3 zentroa = new Vector3(a.X * n.Panela, a.Y, a.Z * n.Panela);
                if (a.Etzanda != '\0')
                {
                    // Hodi etzana: Faktoreak.y da luzera metrotan eta Faktoreak.x diametroa.
                    float luze = a.Faktoreak.y;
                    float diam = a.Faktoreak.x;
                    bool xAlde = a.Etzanda == 'X';
                    PiezaGehituEtzanda(sektorea, a.Sala, m[a.Prefab], xAlde ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(diam, luze, diam), xAlde ? new Vector3(luze, diam, diam) : new Vector3(diam, diam, luze), zentroa, plana);
                    if (a.Kolisioa)
                    {
                        KolisioGehitu(sektorea, a.Sala, zentroa, xAlde ? new Vector3(luze, diam, diam) : new Vector3(diam, diam, luze));
                    }

                    continue;
                }

                Quaternion biraketa = Quaternion.Euler(0f, a.Yaw, 0f);
                PiezaGehitu(sektorea, a.Sala, Propak, m[a.Prefab], biraketa, a.Faktoreak, zentroa, plana);
                if (a.Kolisioa)
                {
                    KolisioGehitu(sektorea, a.Sala, zentroa, KaxaKalkulatu(m[a.Prefab], biraketa, a.Faktoreak).size);
                }
            }

            foreach (MailaKolisio k in d.KolisioGehigarriak)
            {
                KolisioGehitu(sektorea, k.Sala, new Vector3(k.X * n.Panela, k.Y, k.Z * n.Panela), k.Tamaina);
            }

            foreach (MailaArgi a in d.ArgiGehigarriak)
            {
                argiak.Add(new ProtoArgi
                {
                    Izena = a.Izena,
                    Posizioa = new Vector3(a.X * n.Panela, a.Y, a.Z * n.Panela),
                    Kolorea = a.Kolorea,
                    Intentsitatea = a.Intentsitatea,
                    Tartea = a.Tartea,
                    Pultsua = a.Pultsua,
                });
            }

            MailaPropak(sektorea, d, gelak, m, n, ateak, plana);
            MisioaPlanifikatu(sektorea, d, m, n, misioa, plana);

            // Azkenean planifikatzen dira: lehendik dauden Kolisioa__NNN izenak ez dira aldatzen eta eszenan kolisioak gehitzea besterik ez da.
            foreach (MailaKolisio k in d.KolisioAzkenak)
            {
                KolisioGehitu(sektorea, k.Sala, new Vector3(k.X * n.Panela, k.Y, k.Z * n.Panela), k.Tamaina);
            }
        }

        private static float SalaAltuera(MailaGela g, Neurriak n) => g.Altuera > 0f ? g.Altuera : n.PanelAltuera * g.Ilarak;

        private static int Ertza(MailaGela g, Aldea a)
        {
            switch (a)
            {
                case Aldea.Ekialdea: return g.X0 + g.W;
                case Aldea.Mendebaldea: return g.X0;
                case Aldea.Iparra: return g.Z0 + g.D;
                default: return g.Z0;
            }
        }

        private static bool GurutzeaBarruan(MailaGela g, Aldea a, int gurutzea)
        {
            return XArdatza(a)
                ? gurutzea >= g.Z0 && gurutzea < g.Z0 + g.D
                : gurutzea >= g.X0 && gurutzea < g.X0 + g.W;
        }

        private static string MailaSareaBalidatu(MailaDef d, Plana plana)
        {
            Dictionary<string, MailaGela> gelak = d.Gelak.ToDictionary(g => g.Izena);
            var sarea = new Dictionary<(int, int), char>();

            void Margotu(int x, int z, char karakterea, string nor)
            {
                if (sarea.TryGetValue((x, z), out char aurrekoa) && aurrekoa != karakterea)
                {
                    plana.Erroreak.Add($"{nor}: ({x},{z}) gelaxka okupatuta dago ('{aurrekoa}').");
                }

                sarea[(x, z)] = karakterea;
            }

            foreach (MailaGela g in d.Gelak)
            {
                char c = g.W == 1 && g.D == 1 ? '+' : g.Izena[0];
                for (int x = g.X0; x < g.X0 + g.W; x++)
                {
                    for (int z = g.Z0; z < g.Z0 + g.D; z++)
                    {
                        if (sarea.ContainsKey((x, z)))
                        {
                            plana.Erroreak.Add($"{g.Izena}: ({x},{z}) gelaxka okupatuta dago ('{sarea[(x, z)]}').");
                        }

                        sarea[(x, z)] = c;
                    }
                }
            }

            foreach (MailaLotura l in d.Loturak)
            {
                if (!gelak.TryGetValue(l.A, out MailaGela a))
                {
                    plana.Erroreak.Add($"Lotura: '{l.A}' sala ez dago.");
                    continue;
                }

                int ertzA = Ertza(a, l.AldeA);
                int noranzkoa = l.AldeA == Aldea.Ekialdea || l.AldeA == Aldea.Iparra ? 1 : -1;
                int luzera = l.Mutur;
                if (l.B != null)
                {
                    if (!gelak.TryGetValue(l.B, out MailaGela b))
                    {
                        plana.Erroreak.Add($"Lotura: '{l.B}' sala ez dago.");
                        continue;
                    }

                    luzera = (Ertza(b, Kontrakoa(l.AldeA)) - ertzA) * noranzkoa;
                    if (!GurutzeaBarruan(b, l.AldeA, l.Gurutzea))
                    {
                        plana.Erroreak.Add($"{l.A}→{l.B}: {l.Gurutzea} gelaxka ez dago {l.B} salaren horman.");
                    }
                }

                if (!GurutzeaBarruan(a, l.AldeA, l.Gurutzea))
                {
                    plana.Erroreak.Add($"{l.A}→{l.B}: {l.Gurutzea} gelaxka ez dago {l.A} salaren horman.");
                }

                if (luzera < 1)
                {
                    plana.Erroreak.Add($"{l.A}→{l.B}: luzera {luzera}; sala bat gainean edo atzean dago.");
                    continue;
                }

                char hodia = XArdatza(l.AldeA) ? '-' : '|';
                for (int i = 0; i < luzera; i++)
                {
                    int s = noranzkoa > 0 ? ertzA + i : ertzA - 1 - i;
                    if (XArdatza(l.AldeA))
                    {
                        Margotu(s, l.Gurutzea, hodia, $"{l.A}→{l.B}");
                    }
                    else
                    {
                        Margotu(l.Gurutzea, s, hodia, $"{l.A}→{l.B}");
                    }
                }
            }

            foreach (MailaLeihoa w in d.Leihoak.Where(w => !gelak.ContainsKey(w.Sala)))
            {
                plana.Erroreak.Add($"Leihoa: '{w.Sala}' sala ez dago.");
            }

            int xMax = sarea.Keys.Max(k => k.Item1);
            int zMax = sarea.Keys.Max(k => k.Item2);
            int xMin = sarea.Keys.Min(k => k.Item1);
            int zMin = sarea.Keys.Min(k => k.Item2);
            var testua = new StringBuilder();
            for (int z = zMax; z >= zMin; z--)
            {
                testua.Append($"{z,3} ");
                for (int x = xMin; x <= xMax; x++)
                {
                    testua.Append(sarea.TryGetValue((x, z), out char c) ? c : ' ');
                    testua.Append(' ');
                }

                if (z > zMin)
                {
                    testua.AppendLine();
                }
            }

            return testua.ToString();
        }

        private static string ModuluBilatu(string izena)
        {
            foreach (string karpeta in new[] { PropioKarpeta, RustyKarpeta, MagixBoxKarpeta, $"{MagixBoxKarpeta}/Starter" })
            {
                string bidea = $"{karpeta}/{izena}.prefab";
                if (File.Exists(bidea))
                {
                    return bidea;
                }
            }

            return null;
        }

        private static Dictionary<string, Modulua> MailaModuluak(MailaDef d, Plana plana, bool aplikatu)
        {
            var izenak = new HashSet<string>(StringComparer.Ordinal)
            {
                Zorua, Horma, Sabaia, Markoa, SekzioMuturra, Sekzioa, SekzioAtea, ZoruPropioa, Plaka, Barra,
                "PF_Beira", "PF_Barrera", "PF_Zutabea", "Old_computer", "PF_Din_Bilduma", "Container_Big",
            };
            izenak.UnionWith(d.Gelak.Select(g => g.Panela));
            izenak.UnionWith(d.Propak.Select(p => p.Prefab));
            izenak.UnionWith(d.Apaingarriak.Select(a => a.Prefab));
            izenak.UnionWith(d.Misioa.Mugikorrak.Select(a => a.Prefab));

            var seinaleak = new HashSet<string>(d.Gelak.Select(g => g.Seinalea), StringComparer.Ordinal) { HelmugaItsua };
            seinaleak.UnionWith(d.Loturak.Where(l => l.SeinaleaA != null).Select(l => l.SeinaleaA));
            seinaleak.UnionWith(d.Loturak.Where(l => l.SeinaleaB != null).Select(l => l.SeinaleaB));

            var prefabak = new List<GameObject>();
            foreach (string izena in izenak)
            {
                string bidea = ModuluBilatu(izena);
                GameObject prefab = bidea != null ? AssetDatabase.LoadAssetAtPath<GameObject>(bidea) : null;
                if (prefab == null)
                {
                    plana.Erroreak.Add($"Ez da aurkitu: {izena}");
                }
                else
                {
                    prefabak.Add(prefab);
                }
            }

            var faltaDirenak = new List<string>();
            GameObject eredua = AssetDatabase.LoadAssetAtPath<GameObject>(SeinaleEredua);
            foreach (string testua in seinaleak)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropioKarpeta}/PF_Seinalea_{testua}.prefab");
                if (prefab == null && aplikatu)
                {
                    prefab = EszenaPropioa.SeinaleaPrefab(testua);
                }

                if (prefab != null)
                {
                    prefabak.Add(prefab);
                }
                else
                {
                    faltaDirenak.Add(testua);
                }
            }

            if (plana.Erroreak.Count > 0)
            {
                return new Dictionary<string, Modulua>();
            }

            prefabak.Add(eredua);
            Dictionary<string, Modulua> moduluak = NeurtuGuztiak(prefabak.Distinct(), true).ToDictionary(x => x.Izena);
            Modulua seinalea = moduluak[eredua.name];
            foreach (string testua in faltaDirenak)
            {
                string izena = $"PF_Seinalea_{testua}";
                moduluak[izena] = new Modulua
                {
                    Izena = izena,
                    Prefab = seinalea.Prefab,
                    Kaxa = seinalea.Kaxa,
                    Zatiak = seinalea.Zatiak,
                    ErroBiraketa = seinalea.ErroBiraketa,
                    ErroEskala = seinalea.ErroEskala,
                };
                plana.Oharrak.Add($"{izena}: aplikatzean sortuko da.");
            }

            return moduluak;
        }

        private static void MailaSala(SektorePlana s, MailaDef d, MailaGela g, Dictionary<string, Modulua> m, Neurriak n,
            Dictionary<(string, Aldea), Dictionary<int, string>> ateak, List<ProtoArgi> argiak, Plana plana)
        {
            float c = n.Panela;
            float xMin = g.X0 * c;
            float xMax = (g.X0 + g.W) * c;
            float zMin = g.Z0 * c;
            float zMax = (g.Z0 + g.D) * c;
            float cx = (xMin + xMax) * 0.5f;
            float cz = (zMin + zMax) * 0.5f;
            float lx = xMax - xMin;
            float lz = zMax - zMin;
            float h = SalaAltuera(g, n);

            // Zorua: kit-eko lauzak sala berezietan; bestela bi triangeluko lauza propioa.
            if (g.ZoruKit)
            {
                MailaZoruLauzak(s, g.Izena, m[Zorua], xMin, zMin, lx, lz, n.Lauza.x, n.Lauza.z, -n.Lauza.y * 0.5f, plana);
            }
            else
            {
                MailaZoruLauzak(s, g.Izena, m[ZoruPropioa], xMin, zMin, lx, lz, ZoruTile, ZoruTile, 0f, plana);
            }

            // Sabaia.
            int nx = Mathf.Max(1, Mathf.RoundToInt(lx / n.Lauza.x));
            int nz = Mathf.Max(1, Mathf.RoundToInt(lz / n.Lauza.z));
            float tx = lx / nx;
            float tz = lz / nz;
            Modulua sabaia = m[Sabaia];
            Quaternion iraulita = Quaternion.Euler(180f, 0f, 0f);
            var sabaiF = new Vector3(tx / sabaia.Kaxa.size.x, 1f, tz / sabaia.Kaxa.size.z);
            float sabaiLodiera = KaxaKalkulatu(sabaia, iraulita, sabaiF).size.y;
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    PiezaGehitu(s, g.Izena, Sabaiak, sabaia, iraulita, sabaiF,
                        new Vector3(xMin + (ix + 0.5f) * tx, h + sabaiLodiera * 0.5f, zMin + (iz + 0.5f) * tz), plana);
                }
            }

            foreach (Aldea alde in Enum.GetValues(typeof(Aldea)))
            {
                MailaHormaPlanifikatu(s, d, g, alde, m, n, ateak[(g.Izena, alde)], h, plana);
            }

            KolisioGehitu(s, g.Izena, new Vector3(cx, -KolisioLodiera * 0.5f, cz), new Vector3(lx, KolisioLodiera, lz));
            KolisioGehitu(s, g.Izena, new Vector3(cx, h + KolisioLodiera * 0.5f, cz), new Vector3(lx, KolisioLodiera, lz));

            s.Gelak.Add(new GelaPlana
            {
                Izena = $"Gela_{g.Izena}",
                Posizioa = new Vector3(cx, 0f, cz),
                Zentroa = new Vector3(0f, h * 0.5f, 0f),
                Tamaina = new Vector3(lx - 2f * n.HormaLodiera, h, lz - 2f * n.HormaLodiera),
            });
            plana.SalaLerroak.Add($"{g.Izena}: {g.W} x {g.D} gelaxka = {F(lx)} x {F(lz)} m, altuera {F(h)} m");

            // Panelak eta argiak.
            int px = Mathf.Max(1, Mathf.RoundToInt(g.W / g.PanelTartea));
            int pz = Mathf.Max(1, Mathf.RoundToInt(g.D / g.PanelTartea));
            Modulua panela = m[g.Panela];
            for (int ix = 0; ix < px; ix++)
            {
                for (int iz = 0; iz < pz; iz++)
                {
                    float x = xMin + (ix + 0.5f) * lx / px;
                    float z = zMin + (iz + 0.5f) * lz / pz;
                    PiezaGehitu(s, g.Izena, Sabaiak, panela, Quaternion.identity, Vector3.one, new Vector3(x, h - 0.03f, z), plana);
                    argiak.Add(new ProtoArgi
                    {
                        Izena = $"Argia_{g.Izena}_{ix}{iz}",
                        Posizioa = new Vector3(x, h - Mathf.Min(1.2f, h * 0.3f), z),
                        Kolorea = Color.Lerp(g.Kolorea, Color.white, d.KoloreArintzea),
                        Intentsitatea = g.Intentsitatea,
                        Tartea = Mathf.Max(lx / px, lz / pz) * 1.6f + h * 0.8f,
                        Pultsua = g.Pultsua,
                    });
                }
            }
        }

        private static void MailaZoruLauzak(SektorePlana s, string sala, Modulua lauza, float xMin, float zMin, float lx, float lz, float tileX, float tileZ, float y, Plana plana)
        {
            int nx = Mathf.Max(1, Mathf.RoundToInt(lx / tileX));
            int nz = Mathf.Max(1, Mathf.RoundToInt(lz / tileZ));
            float tx = lx / nx;
            float tz = lz / nz;
            var faktoreak = new Vector3(tx / lauza.Kaxa.size.x, 1f, tz / lauza.Kaxa.size.z);
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    PiezaGehitu(s, sala, Zoruak, lauza, Quaternion.identity, faktoreak, new Vector3(xMin + (ix + 0.5f) * tx, y, zMin + (iz + 0.5f) * tz), plana);
                }
            }
        }

        private static void MailaHormaPlanifikatu(SektorePlana s, MailaDef d, MailaGela g, Aldea alde, Dictionary<string, Modulua> m, Neurriak n, Dictionary<int, string> ateak, float h, Plana plana)
        {
            float c = n.Panela;
            bool xArdatza = XArdatza(alde);
            Vector3 normala = -Norabidea(alde);
            float ertza = Ertza(g, alde) * c;
            int hasiera = xArdatza ? g.Z0 : g.X0;
            int kopurua = xArdatza ? g.D : g.W;
            float zentroNormalean = ertza + n.HormaLodiera * 0.5f * Zeinua(normala);
            Modulua horma = m[Horma];
            int ilarak = Mathf.Max(1, Mathf.RoundToInt(h / n.PanelAltuera));
            float ilara = h / ilarak;

            for (int i = hasiera; i < hasiera + kopurua; i++)
            {
                float a0 = i * c;
                float a1 = (i + 1) * c;

                if (ateak.TryGetValue(i, out string helmuga))
                {
                    MailaAtea(s, g, helmuga, normala, ertza, (a0 + a1) * 0.5f, m, n, h, ilara, plana);
                    continue;
                }

                // E/M hormak I/H hormen artean: izkinetan gainjarri gabe.
                float a = a0;
                float b = a1;
                if (xArdatza)
                {
                    a = Mathf.Max(a, g.Z0 * c + n.HormaLodiera);
                    b = Mathf.Min(b, (g.Z0 + g.D) * c - n.HormaLodiera);
                }

                HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, a0, a1, 0f, h);
                if (d.Leihoak.Any(w => w.Sala == g.Izena && w.Alde == alde && w.Gurutzea == i))
                {
                    MailaLeihoPlanifikatu(s, g, normala, ertza, a, b, m, n, h, ilara, plana);
                    continue;
                }

                for (int k = 0; k < ilarak; k++)
                {
                    HormaPieza(s, g.Izena, horma, n, normala, ertza, a, b, k * ilara, (k + 1) * ilara, plana);
                }
            }
        }

        /// <summary>Hormako leihoa: beheko panela, beira eta markoa, eta goiko panelak. Kolisioa horma osoarena da.</summary>
        private static void MailaLeihoPlanifikatu(SektorePlana s, MailaGela g, Vector3 normala, float ertza, float a, float b, Dictionary<string, Modulua> m, Neurriak n, float h, float ilara, Plana plana)
        {
            const float Behea = 1f;
            const float Goia = 2.9f;
            Modulua horma = m[Horma];
            HormaPieza(s, g.Izena, horma, n, normala, ertza, a, b, 0f, Behea, plana);
            float y = Goia;
            for (int k = Mathf.FloorToInt(Goia / ilara) + 1; y < h - 0.01f; k++)
            {
                float hurrengoa = Mathf.Min(k * ilara, h);
                HormaPieza(s, g.Izena, horma, n, normala, ertza, a, b, y, hurrengoa, plana);
                y = hurrengoa;
            }

            bool xNormala = Mathf.Abs(normala.x) > 0.5f;
            float erdia = (a + b) * 0.5f;
            float luzera = b - a;
            float normalean = ertza + n.HormaLodiera * 0.5f * Zeinua(normala);
            Vector3 Tamaina(float lodiera, float altuera, float luze) => xNormala ? new Vector3(lodiera, altuera, luze) : new Vector3(luze, altuera, lodiera);

            PiezaGehitu(s, g.Izena, Hormak, m["PF_Beira"], Quaternion.identity, Tamaina(0.04f, Goia - Behea, luzera), HormaZentroa(normala, normalean, erdia, (Behea + Goia) * 0.5f), plana);
            Modulua barra = m[Barra];
            PiezaGehitu(s, g.Izena, Hormak, barra, Quaternion.identity, Tamaina(n.HormaLodiera, 0.08f, luzera), HormaZentroa(normala, normalean, erdia, Behea), plana);
            PiezaGehitu(s, g.Izena, Hormak, barra, Quaternion.identity, Tamaina(n.HormaLodiera, 0.08f, luzera), HormaZentroa(normala, normalean, erdia, Goia), plana);
            PiezaGehitu(s, g.Izena, Hormak, barra, Quaternion.identity, Tamaina(n.HormaLodiera, Goia - Behea, 0.08f), HormaZentroa(normala, normalean, erdia, (Behea + Goia) * 0.5f), plana);
        }

        private static void MailaAtea(SektorePlana s, MailaGela g, string helmuga, Vector3 normala, float ertza, float erdia, Dictionary<string, Modulua> m, Neurriak n, float h, float ilara, Plana plana)
        {
            Modulua markoa = m[Markoa];
            Quaternion biraketa = HormaBiraketa(markoa, false, normala);
            Bounds neurria = KaxaKalkulatu(markoa, biraketa, Vector3.one);
            bool xNormala = Mathf.Abs(normala.x) > 0.5f;
            float barrukoAurpegia = ertza + n.HormaLodiera * Zeinua(normala);
            float lodiera = xNormala ? neurria.size.x : neurria.size.z;
            float normalean = barrukoAurpegia - lodiera * 0.5f * Zeinua(normala);
            float luzetarakoa = erdia + (xNormala ? neurria.center.z : neurria.center.x);
            PiezaGehitu(s, g.Izena, Hormak, markoa, biraketa, Vector3.one,
                HormaZentroa(normala, normalean, luzetarakoa, n.HodiErdiaY + neurria.center.y), plana);

            float markoGoia = n.HodiErdiaY + neurria.center.y + neurria.size.y * 0.5f;
            float a0 = erdia - n.Panela * 0.5f;
            float a1 = erdia + n.Panela * 0.5f;
            float y = markoGoia;
            for (int k = Mathf.FloorToInt(markoGoia / ilara) + 1; y < h - 0.01f; k++)
            {
                float hurrengoa = Mathf.Min(k * ilara, h);
                HormaPieza(s, g.Izena, m[Horma], n, normala, ertza, a0, a1, y, hurrengoa, plana);
                y = hurrengoa;
            }

            float zentroNormalean = ertza + n.HormaLodiera * 0.5f * Zeinua(normala);
            HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, a0, erdia - n.HodiBarruErdia, 0f, h);
            HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, erdia + n.HodiBarruErdia, a1, 0f, h);
            HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, erdia - n.HodiBarruErdia, erdia + n.HodiBarruErdia, n.HodiAltuera, h);

            // Seinalea, tokia badago: atearen gainean, salaren barrura begira.
            if (h >= SeinaleAltuera + 0.5f)
            {
                PiezaGehitu(s, g.Izena, Hormak, m[$"PF_Seinalea_{helmuga}"], Quaternion.LookRotation(-normala), Vector3.one,
                    HormaZentroa(normala, barrukoAurpegia + 0.03f * Zeinua(normala), erdia, SeinaleAltuera), plana);
            }
        }

        /// <summary>Barne-horma sare-lerro batean: bi aurpegi eta kolisio bat. Laberintoak eta tarteko hormak.</summary>
        private static void MailaBarneHorma(SektorePlana s, MailaGela g, MailaHorma w, Dictionary<string, Modulua> m, Neurriak n, Plana plana)
        {
            float c = n.Panela;
            float h = SalaAltuera(g, n);
            int ilarak = Mathf.Max(1, Mathf.RoundToInt(h / n.PanelAltuera));
            float ilara = h / ilarak;
            Vector3 normala = w.Vertikala ? Vector3.right : Vector3.forward;
            float lerroa = (w.Vertikala ? w.X : w.Z) * c;
            float a = (w.Vertikala ? w.Z : w.X) * c;
            float b = a + c;
            for (int k = 0; k < ilarak; k++)
            {
                HormaPieza(s, g.Izena, m[Horma], n, normala, lerroa, a, b, k * ilara, (k + 1) * ilara, plana);
                HormaPieza(s, g.Izena, m[Horma], n, -normala, lerroa, a, b, k * ilara, (k + 1) * ilara, plana);
            }

            HormaKolisioa(s, g.Izena, n, normala, lerroa, a, b, 0f, h);
        }

        private static void MailaKorridorea(SektorePlana s, MailaDef d, MailaLotura l, Dictionary<string, MailaGela> gelak, Dictionary<string, Modulua> m, Neurriak n, List<ProtoArgi> argiak, MisioPlana misioa, Plana plana)
        {
            float c = n.Panela;
            MailaGela a = gelak[l.A];
            bool xArdatza = XArdatza(l.AldeA);
            float noranzkoa = l.AldeA == Aldea.Ekialdea || l.AldeA == Aldea.Iparra ? 1f : -1f;
            float s0 = Ertza(a, l.AldeA) * c;
            float s1 = l.B != null ? Ertza(gelak[l.B], Kontrakoa(l.AldeA)) * c : s0 + l.Mutur * c * noranzkoa;
            float gurutzea = (l.Gurutzea + 0.5f) * c;
            string sala = l.B != null ? $"Pasilloa_{l.A}_{l.B}" : $"Pasilloa_{l.A}_itsua_{l.Gurutzea}";
            Quaternion biraketa = Quaternion.Euler(0f, xArdatza ? 90f : 0f, 0f);
            float lotura = n.MarkoLodiera - n.HormaLodiera;
            float luzeraGuztira = Mathf.Abs(s1 - s0);

            Vector3 Puntua(float distantzia, float y, float zeharka = 0f)
            {
                float luzetara = s0 + distantzia * noranzkoa;
                return xArdatza ? new Vector3(luzetara, y, gurutzea + zeharka) : new Vector3(gurutzea + zeharka, y, luzetara);
            }

            float d0 = lotura;
            float d1 = l.B != null ? luzeraGuztira - lotura : luzeraGuztira - n.MuturLuzera;

            Modulua plaka = m[Plaka];
            Vector3 atalaseF = xArdatza ? new Vector3(lotura, ZoruLodiera, c) : new Vector3(c, ZoruLodiera, lotura);
            PiezaGehitu(s, sala, Zoruak, plaka, Quaternion.identity, atalaseF, Puntua(lotura * 0.5f, -ZoruLodiera * 0.5f), plana);
            if (l.B != null)
            {
                PiezaGehitu(s, sala, Zoruak, plaka, Quaternion.identity, atalaseF, Puntua(luzeraGuztira - lotura * 0.5f, -ZoruLodiera * 0.5f), plana);
            }

            if (l.AteaA)
            {
                ProtoHodiAtea(s, sala, m[SekzioAtea], n, biraketa, Puntua(d0 + n.AteLuzera * 0.5f, n.HodiErdiaY), xArdatza, plana);
                if (l.BlokeoFasea >= 0)
                {
                    Vector3 oina = Puntua(d0 + n.AteLuzera * 0.5f, n.AteAltuera * 0.5f);
                    misioa.Blokeoak.Add(new BlokeoPlana
                    {
                        Izena = $"Din_Blokeoa_{sala}",
                        Posizioa = oina,
                        Tamaina = xArdatza ? new Vector3(0.2f, n.AteAltuera, n.AteZabalera) : new Vector3(n.AteZabalera, n.AteAltuera, 0.2f),
                        Fasea = l.BlokeoFasea,
                    });
                }

                d0 += n.AteLuzera;
            }
            else if (l.BlokeoFasea >= 0)
            {
                plana.Erroreak.Add($"{sala}: blokeoak A aldeko atea behar du.");
            }

            if (l.B != null && l.AteaB)
            {
                ProtoHodiAtea(s, sala, m[SekzioAtea], n, biraketa, Puntua(d1 - n.AteLuzera * 0.5f, n.HodiErdiaY), xArdatza, plana);
                d1 -= n.AteLuzera;
            }

            float tartea = d1 - d0;
            if (tartea < 1f)
            {
                plana.Erroreak.Add($"{sala}: {F(tartea)} m bakarrik geratzen dira tarteentzat; luzeagoa behar du.");
                return;
            }

            int tarteak = Mathf.Max(1, Mathf.RoundToInt(tartea / n.SekzioLuzera));
            float tarteLuzera = tartea / tarteak;
            float f = tarteLuzera / n.SekzioLuzera;
            if (Mathf.Abs(f - 1f) > LuzeraTolerantzia)
            {
                plana.Erroreak.Add($"{sala}: {Sekzioa} x{f:0.00} luzeran; aldatu pasilloaren gelaxka-kopurua edo ateak.");
            }

            for (int i = 0; i < tarteak; i++)
            {
                var faktoreak = new Vector3(1f, 1f, f);
                Bounds neurria = KaxaKalkulatu(m[Sekzioa], biraketa, faktoreak);
                Vector3 erroa = Puntua(d0 + (i + 0.5f) * tarteLuzera, n.HodiErdiaY);
                PiezaGehitu(s, sala, Hodiak, m[Sekzioa], biraketa, faktoreak, erroa + neurria.center, plana, HodiZoruAstunak);
                Pieza pieza = s.Piezak[s.Piezak.Count - 1];
                pieza.HodiArgiaPiztuta = l.Argia == HodiArgia.Argitua ? i % 2 == 0 : l.Argia == HodiArgia.Erdia && i % 3 == 1;
                pieza.HodiArgiKolorea = l.Kolorea;

                PiezaGehitu(s, sala, Zoruak, m[Zorua], biraketa, faktoreak, Puntua(d0 + (i + 0.25f) * tarteLuzera, -n.Lauza.y * 0.5f), plana);
                PiezaGehitu(s, sala, Zoruak, m[Zorua], biraketa, faktoreak, Puntua(d0 + (i + 0.75f) * tarteLuzera, -n.Lauza.y * 0.5f), plana);
            }

            // Hodiak goiko ertzetan: ingurune industriala, buruaren gainetik.
            if (l.Hodiak != null)
            {
                Vector3 hodiF = new Vector3(0.14f, tartea, 0.14f);
                Quaternion etzanda = xArdatza ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.Euler(90f, 0f, 0f);
                Vector3 hodiNeurria = xArdatza ? new Vector3(tartea, 0.14f, 0.14f) : new Vector3(0.14f, 0.14f, tartea);
                foreach (float albo in new[] { 1.05f, -1.05f })
                {
                    PiezaGehituEtzanda(s, sala, m[l.Hodiak], etzanda, hodiF, hodiNeurria, Puntua(d0 + tartea * 0.5f, n.HodiAltuera - 0.3f, albo), plana);
                }
            }

            if (l.B == null)
            {
                Quaternion muturra = Quaternion.LookRotation(-Norabidea(l.AldeA));
                Bounds neurria = KaxaKalkulatu(m[SekzioMuturra], muturra, Vector3.one);
                Vector3 erroa = Puntua(luzeraGuztira - n.MuturLuzera * 0.5f, n.HodiErdiaY);
                PiezaGehitu(s, sala, Hodiak, m[SekzioMuturra], muturra, Vector3.one, erroa + neurria.center, plana);
                ProtoKutxa(s, sala, Puntua(luzeraGuztira - n.MuturLuzera * 0.5f, n.HodiAltuera * 0.5f), xArdatza, n.MuturLuzera, n.HodiAltuera, n.HodiBarruErdia * 2f);

                // Itsuak arrazoia du: biltegiratze-kutxa bat argi ahul baten azpian.
                Modulua kutxa = m["Container_Big"];
                Vector3 tamaina = KaxaKalkulatu(kutxa, Quaternion.identity, Vector3.one).size;
                float luzetara = xArdatza ? tamaina.x : tamaina.z;
                float zeharka = xArdatza ? tamaina.z : tamaina.x;
                PiezaGehitu(s, sala, Propak, kutxa, Quaternion.identity, Vector3.one,
                    Puntua(luzeraGuztira - n.MuturLuzera - luzetara * 0.5f - 0.05f, tamaina.y * 0.5f, n.HodiBarruErdia - zeharka * 0.5f - 0.05f), plana);
                argiak.Add(new ProtoArgi
                {
                    Izena = $"Argia_{sala}",
                    Posizioa = Puntua(luzeraGuztira - n.MuturLuzera - 1.5f, n.HodiAltuera - 0.5f),
                    Kolorea = l.Kolorea,
                    Intentsitatea = 2.5f,
                    Tartea = 7f,
                });
            }

            // Kolisioak eta gela: probe-ak eta ihes-detektagailuak erabiltzen dute.
            float erdia = luzeraGuztira * 0.5f;
            float zabalera = n.HodiBarruErdia * 2f + KolisioLodiera * 2f;
            ProtoKutxa(s, sala, Puntua(erdia, -KolisioLodiera * 0.5f), xArdatza, luzeraGuztira, KolisioLodiera, zabalera);
            ProtoKutxa(s, sala, Puntua(erdia, n.HodiAltuera + KolisioLodiera * 0.5f), xArdatza, luzeraGuztira, KolisioLodiera, zabalera);
            float albokoa = n.HodiBarruErdia + KolisioLodiera * 0.5f;
            ProtoKutxa(s, sala, Puntua(erdia, n.HodiAltuera * 0.5f, albokoa), xArdatza, luzeraGuztira, n.HodiAltuera, KolisioLodiera);
            ProtoKutxa(s, sala, Puntua(erdia, n.HodiAltuera * 0.5f, -albokoa), xArdatza, luzeraGuztira, n.HodiAltuera, KolisioLodiera);

            Vector3 erdiPuntua = Puntua(erdia, 0f);
            s.Gelak.Add(new GelaPlana
            {
                Izena = $"Gela_C_{sala}",
                Posizioa = erdiPuntua,
                Zentroa = new Vector3(0f, n.HodiAltuera * 0.5f, 0f),
                Tamaina = xArdatza ? new Vector3(luzeraGuztira, n.HodiAltuera, n.HodiBarruErdia * 2f) : new Vector3(n.HodiBarruErdia * 2f, n.HodiAltuera, luzeraGuztira),
            });
        }

        /// <summary>Zilindro etzana: PiezaGehitu-ren eskala-kalkuluak 90°-ko erro-biraketa soilak bakarrik onartzen ditu, beraz eskala hemen ematen da.</summary>
        private static void PiezaGehituEtzanda(SektorePlana s, string sala, Modulua m, Quaternion biraketa, Vector3 lokalF, Vector3 neurria, Vector3 zentroa, Plana plana)
        {
            string bidea = $"{Geometria}/{sala}/{Propak}";
            string gakoa = $"{bidea}/{m.Izena}";
            s.Kontagailuak.TryGetValue(gakoa, out int indizea);
            s.Kontagailuak[gakoa] = indizea + 1;
            s.Piezak.Add(new Pieza
            {
                Kontainerra = bidea,
                Izena = $"{m.Izena}__{indizea:000}",
                Modulua = m,
                Posizioa = zentroa,
                Biraketa = biraketa,
                Eskala = Vector3.Scale(m.ErroEskala, lokalF),
                Helburua = new Bounds(zentroa, neurria),
            });
        }

        private static void MailaPlataformaPlanifikatu(SektorePlana s, MailaPlataforma p, Dictionary<string, Modulua> m, Neurriak n, Plana plana)
        {
            float c = n.Panela;
            float xMin = p.X0 * c;
            float zMin = p.Z0 * c;
            float lx = (p.X1 - p.X0) * c;
            float lz = (p.Z1 - p.Z0) * c;
            int nx = Mathf.Max(1, Mathf.CeilToInt(lx / c));
            int nz = Mathf.Max(1, Mathf.CeilToInt(lz / c));
            float tx = lx / nx;
            float tz = lz / nz;
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    var zentroa = new Vector3(xMin + (ix + 0.5f) * tx, p.Y - ZoruLodiera * 0.5f, zMin + (iz + 0.5f) * tz);
                    PiezaGehitu(s, p.Sala, Propak, m[Plaka], Quaternion.identity, new Vector3(tx, ZoruLodiera, tz), zentroa, plana);
                }
            }

            KolisioGehitu(s, p.Sala, new Vector3(xMin + lx * 0.5f, p.Y - ZoruLodiera * 0.5f, zMin + lz * 0.5f), new Vector3(lx, ZoruLodiera, lz));

            // Euskarriak ertzetan, gutxi gorabehera bi gelaxkatik behin.
            const float Lodiera = 0.28f;
            float altuera = p.Y - ZoruLodiera;
            int zx = Mathf.Max(1, Mathf.RoundToInt(lx / (2f * c)));
            int zz = Mathf.Max(1, Mathf.RoundToInt(lz / (2f * c)));
            for (int ix = 0; ix <= zx; ix++)
            {
                for (int iz = 0; iz <= zz; iz++)
                {
                    float x = Mathf.Lerp(xMin + 0.2f, xMin + lx - 0.2f, (float)ix / zx);
                    float z = Mathf.Lerp(zMin + 0.2f, zMin + lz - 0.2f, (float)iz / zz);
                    var zentroa = new Vector3(x, altuera * 0.5f, z);
                    PiezaGehitu(s, p.Sala, Propak, m["PF_Zutabea"], Quaternion.identity, new Vector3(Lodiera, altuera, Lodiera), zentroa, plana);
                    KolisioGehitu(s, p.Sala, zentroa, new Vector3(Lodiera, altuera, Lodiera));
                }
            }
        }

        private static void MailaBarandaPlanifikatu(SektorePlana s, MailaBaranda b, Dictionary<string, Modulua> m, Neurriak n, Plana plana)
        {
            float c = n.Panela;
            var hasiera = new Vector3(b.X0 * c, b.Y, b.Z0 * c);
            var amaiera = new Vector3(b.X1 * c, b.Y, b.Z1 * c);
            float luzera = Vector3.Distance(hasiera, amaiera);
            bool xArdatza = Mathf.Abs(amaiera.x - hasiera.x) > Mathf.Abs(amaiera.z - hasiera.z);
            Vector3 erdia = (hasiera + amaiera) * 0.5f;
            Vector3 Luze(float lodiera, float altuera) => xArdatza ? new Vector3(luzera, altuera, lodiera) : new Vector3(lodiera, altuera, luzera);
            PiezaGehitu(s, b.Sala, Propak, m[Barra], Quaternion.identity, Luze(0.05f, 0.05f), erdia + Vector3.up * 1.08f, plana);
            PiezaGehitu(s, b.Sala, Propak, m[Barra], Quaternion.identity, Luze(0.03f, 0.03f), erdia + Vector3.up * 0.55f, plana);
            int zutoinak = Mathf.Max(1, Mathf.RoundToInt(luzera / 1.875f));
            for (int i = 0; i <= zutoinak; i++)
            {
                Vector3 p = Vector3.Lerp(hasiera, amaiera, (float)i / zutoinak);
                PiezaGehitu(s, b.Sala, Propak, m[Barra], Quaternion.identity, new Vector3(0.05f, 1.08f, 0.05f), p + Vector3.up * 0.54f, plana);
            }

            KolisioGehitu(s, b.Sala, erdia + Vector3.up * 0.6f, Luze(0.12f, 1.2f));
        }

        private static void MailaEskaileraPlanifikatu(SektorePlana s, MailaEskailera e, Dictionary<string, Modulua> m, Neurriak n, Plana plana)
        {
            const float Sakonera = 0.3f;
            const float MailaGehienez = 0.2f;
            const float Xafla = 0.35f;
            const float XaflaLodiera = 0.08f;
            float c = n.Panela;
            float igoera = e.Y1 - e.Y0;
            int mailak = Mathf.CeilToInt(igoera / MailaGehienez);
            float maila = igoera / mailak;
            Vector3 norabidea = Norabidea(e.Alde);
            bool xArdatza = XArdatza(e.Alde);
            // X eta Z mailadiaren GOIKO muturra da, plataformaren ertzean; hortik atzera eraikitzen da.
            var goia = new Vector3(e.X * c, e.Y0, e.Z * c);
            for (int i = 1; i <= mailak; i++)
            {
                float mailaGoia = e.Y0 + i * maila;
                Vector3 p = goia - norabidea * ((mailak - i + 0.5f) * Sakonera);
                float altuera = mailaGoia - e.Y0;
                Vector3 tamaina = xArdatza ? new Vector3(Sakonera, altuera, e.Zabalera) : new Vector3(e.Zabalera, altuera, Sakonera);
                var zentroa = new Vector3(p.x, e.Y0 + altuera * 0.5f, p.z);
                PiezaGehitu(s, e.Sala, Propak, m[Plaka], Quaternion.identity, tamaina, zentroa, plana);
                KolisioGehitu(s, e.Sala, zentroa, tamaina);

                // Alboko xaflak: mailadira aurretik bakarrik igotzen da, ez alboetatik.
                float xaflaAltuera = altuera + Xafla;
                foreach (float albo in new[] { -1f, 1f })
                {
                    float zeharka = albo * (e.Zabalera * 0.5f + XaflaLodiera * 0.5f);
                    Vector3 xaflaZentroa = new Vector3(p.x + (xArdatza ? 0f : zeharka), e.Y0 + xaflaAltuera * 0.5f, p.z + (xArdatza ? zeharka : 0f));
                    Vector3 xaflaTamaina = xArdatza ? new Vector3(Sakonera, xaflaAltuera, XaflaLodiera) : new Vector3(XaflaLodiera, xaflaAltuera, Sakonera);
                    PiezaGehitu(s, e.Sala, Propak, m[Barra], Quaternion.identity, xaflaTamaina, xaflaZentroa, plana);
                    KolisioGehitu(s, e.Sala, xaflaZentroa, xaflaTamaina);
                }
            }
        }

        private static void MailaPropak(SektorePlana s, MailaDef d, Dictionary<string, MailaGela> gelak, Dictionary<string, Modulua> m, Neurriak n,
            Dictionary<(string, Aldea), Dictionary<int, string>> ateak, Plana plana)
        {
            float c = n.Panela;
            var hartuak = new Dictionary<string, List<Bounds>>();
            foreach (ProtoProp prop in d.Propak)
            {
                MailaGela g = gelak[prop.Sala];
                string izena = $"{prop.Sala}/{prop.Prefab}";
                Modulua modulua = m[prop.Prefab];
                if (modulua.RigidbodyDu)
                {
                    plana.Erroreak.Add($"{izena}: Rigidbody du; ez erabili estatiko gisa.");
                    continue;
                }

                Quaternion biraketa = Quaternion.Euler(0f, prop.Yaw, 0f);
                Vector3 tamaina = KaxaKalkulatu(modulua, biraketa, Vector3.one).size;
                float erdiX = g.W * c * 0.5f - n.HormaLodiera;
                float erdiZ = g.D * c * 0.5f - n.HormaLodiera;
                float h = SalaAltuera(g, n);
                if (tamaina.x > erdiX * 2f || tamaina.z > erdiZ * 2f || tamaina.y > h)
                {
                    plana.Erroreak.Add($"{izena}: ez da sartzen ({V(tamaina)} m).");
                    continue;
                }

                var zentroa = new Vector3(
                    (g.X0 + g.W * 0.5f) * c + prop.U * (erdiX - tamaina.x * 0.5f),
                    tamaina.y * 0.5f,
                    (g.Z0 + g.D * 0.5f) * c + prop.V * (erdiZ - tamaina.z * 0.5f));
                var kaxa = new Bounds(zentroa, tamaina);

                if (MailaAteaBlokeatzenDu(kaxa, g, ateak, n))
                {
                    plana.Erroreak.Add($"{izena}: atearen pasabidea blokeatzen du (U {prop.U}, V {prop.V}).");
                    continue;
                }

                if (!hartuak.TryGetValue(prop.Sala, out List<Bounds> salakoak))
                {
                    salakoak = new List<Bounds>();
                    hartuak.Add(prop.Sala, salakoak);
                }

                var marjinarekin = new Bounds(zentroa, tamaina + Vector3.one * PropMarjina);
                if (salakoak.Any(hartua => hartua.Intersects(marjinarekin)))
                {
                    plana.Erroreak.Add($"{izena}: beste prop batekin gainjartzen da (U {prop.U}, V {prop.V}).");
                    continue;
                }

                salakoak.Add(kaxa);
                PiezaGehitu(s, prop.Sala, Propak, modulua, biraketa, Vector3.one, zentroa, plana);
                if (prop.KolisioaBehar)
                {
                    KolisioGehitu(s, prop.Sala, zentroa, tamaina);
                }
            }
        }

        private static bool MailaAteaBlokeatzenDu(Bounds kaxa, MailaGela g, Dictionary<(string, Aldea), Dictionary<int, string>> ateak, Neurriak n)
        {
            float c = n.Panela;
            foreach (Aldea alde in Enum.GetValues(typeof(Aldea)))
            {
                foreach (int gurutzea in ateak[(g.Izena, alde)].Keys)
                {
                    Vector3 barrura = -Norabidea(alde);
                    float ertza = Ertza(g, alde) * c;
                    float erdia = (gurutzea + 0.5f) * c;
                    float normalean = ertza + (n.HormaLodiera + AteGuneSakonera * 0.5f) * Zeinua(barrura);
                    Vector3 zentroa = HormaZentroa(barrura, normalean, erdia, kaxa.center.y);
                    Vector3 tamaina = XArdatza(alde)
                        ? new Vector3(AteGuneSakonera, kaxa.size.y, c + 1f)
                        : new Vector3(c + 1f, kaxa.size.y, AteGuneSakonera);
                    if (new Bounds(zentroa, tamaina).Intersects(kaxa))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ---------- Misioa ----------

        private static void MisioaPlanifikatu(SektorePlana s, MailaDef d, Dictionary<string, Modulua> m, Neurriak n, MisioPlana misioa, Plana plana)
        {
            float c = n.Panela;
            MisioDef md = d.Misioa;
            misioa.Spawn = new Vector3(md.SpawnX * c, md.SpawnY, md.SpawnZ * c);
            misioa.SpawnYaw = md.SpawnYaw;
            misioa.Irteera = new Vector3(md.IrteeraX * c, 1.4f, md.IrteeraZ * c);
            misioa.Gelaxka = c;
            Modulua konputagailua = m["Old_computer"];
            HashSet<string> helburuak = md.Faseak.SelectMany(f => f.Helburuak).ToHashSet();
            foreach (MisioTerminalDef t in md.Terminalak)
            {
                if (!helburuak.Contains(t.Id))
                {
                    plana.Erroreak.Add($"Terminala {t.Id}: ez dago inongo fasetan.");
                }

                Quaternion biraketa = Quaternion.Euler(0f, t.Yaw, 0f);
                Vector3 tamaina = KaxaKalkulatu(konputagailua, biraketa, Vector3.one).size;
                var oina = new Vector3(t.X * c, t.Y, t.Z * c);
                PiezaGehitu(s, t.Sala, Propak, konputagailua, biraketa, Vector3.one, oina + Vector3.up * (tamaina.y * 0.5f), plana);
            }

            foreach (string id in helburuak.Where(id => md.Terminalak.All(t => t.Id != id)))
            {
                plana.Erroreak.Add($"Helburua {id}: ez du terminalik.");
            }
        }

        private static int MisioaSinkronizatu(MailaDef d, MisioPlana plana, Transform erroa, Scene eszena)
        {
            int aldaketak = 0;
            float c = plana.Gelaxka;
            MisioDef md = d.Misioa;

            Transform Ume(Transform gurasoa, string izena, Vector3 posizioa, Quaternion biraketa)
            {
                Transform t = SemeaBilatu(gurasoa, izena);
                if (t == null)
                {
                    var berria = new GameObject(izena);
                    berria.transform.SetParent(gurasoa, false);
                    Undo.RegisterCreatedObjectUndo(berria, izena);
                    t = berria.transform;
                    aldaketak++;
                }

                if ((t.localPosition - posizioa).sqrMagnitude > Tolerantzia || Quaternion.Angle(t.localRotation, biraketa) > 0.05f)
                {
                    Undo.RecordObject(t, izena);
                    t.localPosition = posizioa;
                    t.localRotation = biraketa;
                    aldaketak++;
                }

                return t;
            }

            T Osagaia<T>(Transform t) where T : Component
            {
                T osagaia = t.GetComponent<T>();
                if (osagaia == null)
                {
                    osagaia = Undo.AddComponent<T>(t.gameObject);
                    aldaketak++;
                }

                return osagaia;
            }

            void Kaxa(Transform t, Vector3 zentroa, Vector3 tamaina, bool trigger)
            {
                BoxCollider kaxa = Osagaia<BoxCollider>(t);
                if (kaxa.isTrigger != trigger || (kaxa.center - zentroa).sqrMagnitude > Tolerantzia || (kaxa.size - tamaina).sqrMagnitude > Tolerantzia)
                {
                    Undo.RecordObject(kaxa, t.name);
                    kaxa.isTrigger = trigger;
                    kaxa.center = zentroa;
                    kaxa.size = tamaina;
                    aldaketak++;
                }
            }

            void Konfig(UnityEngine.Object objektua, Func<bool> ekintza)
            {
                Undo.RecordObject(objektua, objektua.name);
                if (ekintza())
                {
                    EditorUtility.SetDirty(objektua);
                    aldaketak++;
                }
            }

            Transform Instantzia(Transform gurasoa, string izena, GameObject prefab, Vector3 posizioa, Quaternion biraketa, Vector3 eskala)
            {
                Transform t = SemeaBilatu(gurasoa, izena);
                if (t != null && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != prefab)
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    t = null;
                }

                if (t == null)
                {
                    var instantzia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, eszena);
                    instantzia.name = izena;
                    instantzia.transform.SetParent(gurasoa, false);
                    Undo.RegisterCreatedObjectUndo(instantzia, izena);
                    t = instantzia.transform;
                    aldaketak++;
                }

                if ((t.localPosition - posizioa).sqrMagnitude > Tolerantzia || Quaternion.Angle(t.localRotation, biraketa) > 0.05f || (t.localScale - eskala).sqrMagnitude > Tolerantzia)
                {
                    Undo.RecordObject(t, izena);
                    t.localPosition = posizioa;
                    t.localRotation = biraketa;
                    t.localScale = eskala;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    aldaketak++;
                }

                return t;
            }

            void ZaharrakKendu(Transform gurasoa, HashSet<string> izenak)
            {
                for (int i = gurasoa.childCount - 1; i >= 0; i--)
                {
                    Transform t = gurasoa.GetChild(i);
                    if (!izenak.Contains(t.name))
                    {
                        Undo.DestroyObjectImmediate(t.gameObject);
                        aldaketak++;
                    }
                }
            }

            GameObject Prefab(string izena) => AssetDatabase.LoadAssetAtPath<GameObject>(ModuluBilatu(izena));

            Transform misioa = Ume(erroa, MisioaIzena, Vector3.zero, Quaternion.identity);
            LevelManager kudeatzailea = Osagaia<LevelManager>(misioa);
            Transform spawn = Ume(misioa, "Spawn_Jokalaria", plana.Spawn, Quaternion.Euler(0f, plana.SpawnYaw, 0f));
            List<LevelManager.FaseDatuak> faseak = md.Faseak.Select(f => new LevelManager.FaseDatuak { Deskribapena = f.Deskribapena, Helburuak = f.Helburuak }).ToList();
            Konfig(kudeatzailea, () => kudeatzailea.Konfiguratu(md.Izena, d.Erroa, md.Hurrengoa, spawn, faseak));

            Transform terminalak = Ume(misioa, "Terminalak", Vector3.zero, Quaternion.identity);
            foreach (MisioTerminalDef t in md.Terminalak)
            {
                Transform terminala = Ume(terminalak, $"Din_Terminala_{t.Id}", new Vector3(t.X * c, t.Y, t.Z * c), Quaternion.Euler(0f, t.Yaw, 0f));
                Kaxa(terminala, Vector3.up * 1.2f, new Vector3(4.2f, 2.4f, 4.2f), true);
                Transform adierazlea = Ume(terminala, "Adierazlea", Vector3.up * 2.1f, Quaternion.identity);
                Light argia = Osagaia<Light>(adierazlea);
                if (argia.type != LightType.Point || argia.shadows != LightShadows.None || Mathf.Abs(argia.range - 5f) > Tolerantzia || Mathf.Abs(argia.intensity - 2.5f) > Tolerantzia)
                {
                    Undo.RecordObject(argia, adierazlea.name);
                    argia.type = LightType.Point;
                    argia.lightmapBakeType = LightmapBakeType.Realtime;
                    argia.shadows = LightShadows.None;
                    argia.range = 5f;
                    argia.intensity = 2.5f;
                    argia.color = new Color(1f, 0.25f, 0.15f);
                    aldaketak++;
                }

                MisioTerminala osagaia = Osagaia<MisioTerminala>(terminala);
                Konfig(osagaia, () => osagaia.Konfiguratu(t.Id, t.Izena, kudeatzailea, argia));
            }

            ZaharrakKendu(terminalak, md.Terminalak.Select(t => $"Din_Terminala_{t.Id}").ToHashSet());

            Transform blokeoak = Ume(misioa, "Blokeoak", Vector3.zero, Quaternion.identity);
            GameObject barrera = Prefab("PF_Barrera");
            foreach (BlokeoPlana b in plana.Blokeoak)
            {
                Transform blokeoa = Ume(blokeoak, b.Izena, b.Posizioa, Quaternion.identity);
                Kaxa(blokeoa, Vector3.zero, b.Tamaina, false);
                Instantzia(blokeoa, "Hesia", barrera, Vector3.zero, Quaternion.identity, b.Tamaina);
                BlokeoAtea osagaia = Osagaia<BlokeoAtea>(blokeoa);
                Konfig(osagaia, () => osagaia.Konfiguratu(b.Fasea, kudeatzailea));
            }

            ZaharrakKendu(blokeoak, plana.Blokeoak.Select(b => b.Izena).ToHashSet());

            Transform irteera = Ume(misioa, "Din_Irteera", plana.Irteera, Quaternion.identity);
            Kaxa(irteera, Vector3.zero, new Vector3(3f, 2.8f, 3f), true);
            MisioIrteera irteeraOsagaia = Osagaia<MisioIrteera>(irteera);
            Konfig(irteeraOsagaia, () => irteeraOsagaia.Konfiguratu(kudeatzailea));

            Transform bildumak = Ume(misioa, "Bildumak", Vector3.zero, Quaternion.identity);
            GameObject bildumaPrefab = Prefab("PF_Din_Bilduma");
            var bildumaIzenak = new HashSet<string>();
            for (int i = 0; i < md.Bildumak.Count; i++)
            {
                Vector3 p = md.Bildumak[i];
                string izena = $"Din_Bilduma_{i:00}";
                bildumaIzenak.Add(izena);
                Transform bilduma = Instantzia(bildumak, izena, bildumaPrefab, new Vector3(p.x * c, p.y, p.z * c), Quaternion.identity, Vector3.one * 0.35f);
                Kaxa(bilduma, Vector3.zero, Vector3.one * 5f, true);
                MisioBilduma osagaia = Osagaia<MisioBilduma>(bilduma);
                Konfig(osagaia, () => osagaia.Konfiguratu(kudeatzailea));
            }

            ZaharrakKendu(bildumak, bildumaIzenak);

            Transform markak = Ume(misioa, "Markak", Vector3.zero, Quaternion.identity);
            var markaIzenak = new HashSet<string>();
            for (int i = 0; i < md.Markak.Count; i++)
            {
                (MarkaMota mota, Vector3 p) = md.Markak[i];
                string izena = $"Din_Marka_{mota}_{i:00}";
                markaIzenak.Add(izena);
                Transform marka = Ume(markak, izena, new Vector3(p.x * c, p.y, p.z * c), Quaternion.identity);
                MailaMarka osagaia = Osagaia<MailaMarka>(marka);
                Konfig(osagaia, () => osagaia.Konfiguratu(mota));
            }

            ZaharrakKendu(markak, markaIzenak);

            Transform mugikorrak = Ume(misioa, "Mugikorrak", Vector3.zero, Quaternion.identity);
            var mugikorIzenak = new HashSet<string>();
            for (int i = 0; i < md.Mugikorrak.Count; i++)
            {
                MailaMugikor g = md.Mugikorrak[i];
                string izena = $"Din_{g.Prefab}_{i:00}";
                mugikorIzenak.Add(izena);
                Transform t = Instantzia(mugikorrak, izena, Prefab(g.Prefab), new Vector3(g.X * c, g.Y, g.Z * c), Quaternion.Euler(g.Biraketa), g.Eskala);
                Biraketa osagaia = Osagaia<Biraketa>(t);
                Konfig(osagaia, () => osagaia.Konfiguratu(g.Abiadura));
            }

            ZaharrakKendu(mugikorrak, mugikorIzenak);
            return aldaketak;
        }

        private static int PultsuakSinkronizatu(List<ProtoArgi> argiak, Transform erroa)
        {
            int aldaketak = 0;
            Transform kontainerra = SemeaBilatu(erroa, ProtoArgiak);
            foreach (ProtoArgi a in argiak.Where(a => a.Pultsua))
            {
                Transform t = kontainerra != null ? SemeaBilatu(kontainerra, a.Izena) : null;
                if (t != null && t.GetComponent<ArgiPultsua>() == null)
                {
                    Undo.AddComponent<ArgiPultsua>(t.gameObject);
                    aldaketak++;
                }
            }

            return aldaketak;
        }

        private static List<(string Izena, Vector3 Begia, Vector3 Helburua)> MailaKamerak(MailaDef d)
        {
            float c = ProtoGelaxka();
            return d.Kamerak
                .Select(k => (k.Izena, new Vector3(k.Bx * c, k.By, k.Bz * c), new Vector3(k.Hx * c, k.Hy, k.Hz * c)))
                .ToList();
        }

        // ---------- Datu-egiturak ----------

        private sealed class MailaDef
        {
            public string Izena;
            public string Erroa;
            public string Aurrizkia;
            public Color Giroa = new Color(0.07f, 0.08f, 0.1f);
            public float KoloreArintzea = 0.3f;
            public MailaGela[] Gelak = Array.Empty<MailaGela>();
            public MailaLotura[] Loturak = Array.Empty<MailaLotura>();
            public ProtoProp[] Propak = Array.Empty<ProtoProp>();
            public MailaHorma[] BarneHormak = Array.Empty<MailaHorma>();
            public MailaLeihoa[] Leihoak = Array.Empty<MailaLeihoa>();
            public MailaPlataforma[] Plataformak = Array.Empty<MailaPlataforma>();
            public MailaBaranda[] Barandak = Array.Empty<MailaBaranda>();
            public MailaEskailera[] Eskailerak = Array.Empty<MailaEskailera>();
            public MailaApaingarri[] Apaingarriak = Array.Empty<MailaApaingarri>();
            public MailaKolisio[] KolisioGehigarriak = Array.Empty<MailaKolisio>();
            public MailaKolisio[] KolisioAzkenak = Array.Empty<MailaKolisio>();
            public MailaArgi[] ArgiGehigarriak = Array.Empty<MailaArgi>();
            public MailaKamera[] Kamerak = Array.Empty<MailaKamera>();
            public MisioDef Misioa = new MisioDef();

            public string Bidea => $"{EszenaKarpeta}{Izena}.unity";
        }

        private sealed class MailaGela
        {
            public string Izena;
            public int X0;
            public int Z0;
            public int W;
            public int D;
            public string Seinalea;
            public Color Kolorea;
            public float Intentsitatea;
            public int Ilarak = 2;
            public float Altuera;
            public bool ZoruKit;
            public string Panela = "PF_ArgiPanela_Hotza";
            public float PanelTartea = 2f;
            public bool Pultsua;
        }

        private sealed class MailaLotura
        {
            public string A;
            public Aldea AldeA;
            public int Gurutzea;
            public string B;
            public int Mutur;
            public bool AteaA;
            public bool AteaB;
            public HodiArgia Argia;
            public Color Kolorea;
            public int BlokeoFasea = -1;
            public string SeinaleaA;
            public string SeinaleaB;
            public string Hodiak;
        }

        private sealed class MailaHorma
        {
            public string Sala;
            public bool Vertikala;
            public int X;
            public int Z;
        }

        private sealed class MailaLeihoa
        {
            public string Sala;
            public Aldea Alde;
            public int Gurutzea;
        }

        private sealed class MailaPlataforma
        {
            public string Sala;
            public float X0;
            public float Z0;
            public float X1;
            public float Z1;
            public float Y;
        }

        private sealed class MailaBaranda
        {
            public string Sala;
            public float X0;
            public float Z0;
            public float X1;
            public float Z1;
            public float Y;
        }

        private sealed class MailaEskailera
        {
            public string Sala;
            public float X;
            public float Z;
            public Aldea Alde;
            public float Y0;
            public float Y1;
            public float Zabalera;
        }

        private sealed class MailaApaingarri
        {
            public string Sala;
            public string Prefab;
            public float X;
            public float Y;
            public float Z;
            public float Yaw;
            public Vector3 Faktoreak = Vector3.one;
            public bool Kolisioa;
            public char Etzanda;
        }

        private sealed class MailaKolisio
        {
            public string Sala;
            public float X;
            public float Y;
            public float Z;
            public Vector3 Tamaina;
        }

        private sealed class MailaArgi
        {
            public string Izena;
            public float X;
            public float Y;
            public float Z;
            public Color Kolorea;
            public float Intentsitatea;
            public float Tartea;
            public bool Pultsua;
        }

        private sealed class MailaKamera
        {
            public string Izena;
            public float Bx;
            public float By;
            public float Bz;
            public float Hx;
            public float Hy;
            public float Hz;
        }

        private sealed class MailaMugikor
        {
            public string Prefab;
            public float X;
            public float Y;
            public float Z;
            public Vector3 Biraketa;
            public Vector3 Eskala;
            public Vector3 Abiadura;
        }

        private sealed class MisioFaseDef
        {
            public string Deskribapena;
            public string[] Helburuak;
        }

        private sealed class MisioTerminalDef
        {
            public string Id;
            public string Izena;
            public string Sala;
            public float X;
            public float Y;
            public float Z;
            public float Yaw;
        }

        private sealed class MisioDef
        {
            public string Izena;
            public string Hurrengoa;
            public float SpawnX;
            public float SpawnY;
            public float SpawnZ;
            public float SpawnYaw;
            public float IrteeraX;
            public float IrteeraZ;
            public MisioFaseDef[] Faseak = Array.Empty<MisioFaseDef>();
            public MisioTerminalDef[] Terminalak = Array.Empty<MisioTerminalDef>();
            public List<Vector3> Bildumak = new List<Vector3>();
            public List<(MarkaMota, Vector3)> Markak = new List<(MarkaMota, Vector3)>();
            public List<MailaMugikor> Mugikorrak = new List<MailaMugikor>();
        }

        private sealed class MisioPlana
        {
            public float Gelaxka;
            public Vector3 Spawn;
            public float SpawnYaw;
            public Vector3 Irteera;
            public readonly List<BlokeoPlana> Blokeoak = new List<BlokeoPlana>();
        }

        private sealed class BlokeoPlana
        {
            public string Izena;
            public Vector3 Posizioa;
            public Vector3 Tamaina;
            public int Fasea;
        }
    }
}
