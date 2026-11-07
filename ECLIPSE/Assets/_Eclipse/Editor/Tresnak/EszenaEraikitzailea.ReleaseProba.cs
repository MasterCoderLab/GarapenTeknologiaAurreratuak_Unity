using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Development gabeko bi build erreal, repotik kanpo: QA (ECLIPSE_AUTOPROBA ikurrarekin, misioak zuzenean osatzen ditu) eta Release (ikurrik gabe).
    /// Ez du EditorBuildSettings, Build Profile edo ProjectSettings aldatzen: eszenak eta ikurra build bakoitzaren aukeretan doaz.
    /// Interakzioa simulatua da edo ez dago: teklatu errealarekin Ion-ek probatzen du.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string ReleaseMarka = "ECL_RELEASEPROBA";
        private const string AutoProbaIkurra = "ECLIPSE_AUTOPROBA";
        private const string JokalariMota = "Eclipse.Proba.EsploratzaileKontrola";
        private const string AutoProbaMota = "Eclipse.Proba.AutoProbaQA";
        private const string Exekutagarria = "ECLIPSE_proba.x86_64";
        private const string BuildGabeMarka = "-eclipseBuildGabe";
        private const int QaDenbora = 240;
        private const int ReleaseDenbora = 120;
        private const int ReleaseItxaronaldia = 8;
        private const float NodoTolerantzia = 0.01f;

        private static readonly Dictionary<string, int> EditorrekoNodoak = new Dictionary<string, int>
        {
            { "Mision01_Laborategia", 9743 },
            { "Mision02_Energia", 11013 },
            { "Mision03_Erreaktorea", 9627 },
        };

        /// <summary>Batchmode (Unity itxita). -eclipseBuildGabe: lehendik dauden binarioak berrerabili, baliozkotzailea bakarrik exekutatzeko.</summary>
        public static void BatchReleaseProba()
        {
            var lerroak = new List<string>();
            bool ondo = false;
            try
            {
                ondo = ReleaseProbaExekutatu(lerroak);
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{ReleaseMarka}_ERROR {salbuespena}");
                lerroak.Add($"HUTS: salbuespena: {salbuespena.Message}");
            }

            var informea = new Informea(Tresna, ReleaseMarka, "Release eta QA build-ak (Development gabe)", "M1, M2, M3", "StandaloneLinux64");
            informea.Atala("Emaitzak", lerroak);
            int hutsak = lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal));
            if (ondo && hutsak == 0)
            {
                informea.AmaituOndo($"{lerroak.Count} lerro, 0 huts");
            }
            else
            {
                informea.AmaituErrorearekin($"{hutsak} huts");
            }

            EditorApplication.Exit(ondo && hutsak == 0 ? 0 : 1);
        }

        private static bool ReleaseProbaExekutatu(List<string> lerroak)
        {
            MailaDef[] mailak = { Mision01(), Mision02(), Mision03() };
            string erroa = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ECLIPSE_babeskopia", "proba_release");
            string qa = Path.Combine(erroa, "qa");
            string release = Path.Combine(erroa, "release");
            BuildTarget hasierakoa = EditorUserBuildSettings.activeBuildTarget;
            lerroak.Add($"INFO: activeBuildTarget hasieran: {hasierakoa}");

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            {
                lerroak.Add("HUTS: Linux modulua falta da. Alternatiba: Windows Release + Wine.");
                return false;
            }

            if (!Environment.GetCommandLineArgs().Contains(BuildGabeMarka))
            {
                if (!BuildEgin(qa, mailak, new[] { AutoProbaIkurra }, "QA", lerroak) || !BuildEgin(release, mailak, Array.Empty<string>(), "Release", lerroak))
                {
                    return false;
                }
            }
            else
            {
                lerroak.Add("INFO: binarioak berrerabilita (-eclipseBuildGabe).");
            }

            lerroak.Add($"INFO: activeBuildTarget amaieran: {EditorUserBuildSettings.activeBuildTarget} (ez da inoiz automatikoki leheneratzen)");

            DllakEgiaztatu(qa, release, mailak.Length, lerroak);
            QaEgiaztatu(qa, mailak, lerroak);
            QaKontrolaEgiaztatu(qa, mailak[0], lerroak);
            ReleaseEgiaztatu(release, mailak[0], lerroak);
            return true;
        }

        private static bool BuildEgin(string karpeta, MailaDef[] mailak, string[] ikurrak, string izena, List<string> lerroak)
        {
            Directory.CreateDirectory(karpeta);
            var aukerak = new BuildPlayerOptions
            {
                scenes = mailak.Select(m => m.Bidea).ToArray(),
                locationPathName = Path.Combine(karpeta, Exekutagarria),
                target = BuildTarget.StandaloneLinux64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
                extraScriptingDefines = ikurrak,
            };

            Debug.Log($"{ReleaseMarka}_BUILD_HASI {izena}");
            BuildSummary laburpena = BuildPipeline.BuildPlayer(aukerak).summary;
            bool ondo = laburpena.result == BuildResult.Succeeded;
            lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: build {izena}: {laburpena.result}, {laburpena.totalTime.TotalMinutes:0.0} min, {laburpena.totalSize / (1024 * 1024)} MB, {laburpena.totalErrors} errore, aukerak {laburpena.options}");
            return ondo;
        }

        private static string DatuKarpeta(string karpeta) => Path.Combine(karpeta, Path.GetFileNameWithoutExtension(Exekutagarria) + "_Data");

        private static string RuntimeDll(string karpeta) => Path.Combine(DatuKarpeta(karpeta), "Managed", "Eclipse.Runtime.dll");

        /// <summary>Bi froga independente: islapena eta metadatuen testua. Bat ez badatoz, salbuespena: ezin da emaitzaz fidatu.</summary>
        private static bool DllMotaDu(string dll, string mota)
        {
            byte[] datuak = File.ReadAllBytes(dll);
            bool islapena = Assembly.Load(datuak).GetType(mota, false) != null;
            byte[] izena = Encoding.UTF8.GetBytes(mota.Substring(mota.LastIndexOf('.') + 1));
            bool testua = Enumerable.Range(0, datuak.Length - izena.Length).Any(i => datuak[i] == izena[0] && izena.Select((b, j) => datuak[i + j] == b).All(x => x));
            if (islapena != testua)
            {
                throw new InvalidOperationException($"{dll}: {mota}: islapena={islapena}, testua={testua}");
            }

            return islapena;
        }

        private static HashSet<string> DllMotak(string dll)
        {
            Type[] motak;
            try
            {
                motak = Assembly.Load(File.ReadAllBytes(dll)).GetTypes();
            }
            catch (ReflectionTypeLoadException salbuespena)
            {
                motak = salbuespena.Types;
            }

            return new HashSet<string>(motak.Where(m => m != null).Select(m => m.FullName), StringComparer.Ordinal);
        }

        private static void DllakEgiaztatu(string qa, string release, int eszenak, List<string> lerroak)
        {
            void Oharra(string izena, bool ondo, string xehetasuna = "") => lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena} {xehetasuna}");

            string qaDll = RuntimeDll(qa);
            string releaseDll = RuntimeDll(release);
            Oharra("[Sin interacción] QA: EsploratzaileKontrola DLL-an", DllMotaDu(qaDll, JokalariMota));
            Oharra("[Sin interacción] QA: AutoProbaQA DLL-an", DllMotaDu(qaDll, AutoProbaMota));
            Oharra("[Sin interacción] Release: EsploratzaileKontrola DLL-an", DllMotaDu(releaseDll, JokalariMota));
            Oharra("[Sin interacción] Release: AutoProbaQA EZ dago DLL-an", !DllMotaDu(releaseDll, AutoProbaMota));

            HashSet<string> qaMotak = DllMotak(qaDll);
            HashSet<string> releaseMotak = DllMotak(releaseDll);
            List<string> soilikQa = qaMotak.Except(releaseMotak).ToList();
            List<string> soilikRelease = releaseMotak.Except(qaMotak).ToList();
            Oharra("[Sin interacción] Release = QA - AutoProbaQA (motak)",
                soilikRelease.Count == 0 && soilikQa.Count > 0 && soilikQa.All(m => m.StartsWith(AutoProbaMota, StringComparison.Ordinal)),
                $"{releaseMotak.Count} mota Release-n; soilik QA: {string.Join(", ", soilikQa)}; soilik Release: {string.Join(", ", soilikRelease)}");

            foreach ((string izena, string karpeta) in new[] { ("QA", qa), ("Release", release) })
            {
                bool guztiak = Enumerable.Range(0, eszenak).All(i => File.Exists(Path.Combine(DatuKarpeta(karpeta), $"level{i}")));
                bool gehiagorik = File.Exists(Path.Combine(DatuKarpeta(karpeta), $"level{eszenak}"));
                Oharra($"[Sin interacción] {izena}: {eszenak} eszenak bakarrik", guztiak && !gehiagorik);
            }
        }

        /// <summary>Player-a abiarazi. itxaronMarka ematen bada, marka hori agertu eta itxaronaldiaren ondoren gelditzen da (Release-k ez du bere kabuz amaitzen).</summary>
        private static (int? Kodea, string Log, bool Geldituta) PlayerExekutatu(string karpeta, string logIzena, string argumentuak, int segundoak, string itxaronMarka)
        {
            string log = Path.Combine(karpeta, logIzena);
            File.Delete(log);
            var startInfo = new ProcessStartInfo(Path.Combine(karpeta, Exekutagarria), $"-batchmode -nographics -logFile \"{log}\" {argumentuak}")
            {
                UseShellExecute = false,
                WorkingDirectory = karpeta,
            };

            using (Process prozesua = Process.Start(startInfo))
            {
                var denbora = Stopwatch.StartNew();
                float? markaNoiz = null;
                while (!prozesua.HasExited && denbora.Elapsed.TotalSeconds < segundoak)
                {
                    Thread.Sleep(500);
                    if (itxaronMarka != null && markaNoiz == null && LogIrakurri(log).Contains(itxaronMarka))
                    {
                        markaNoiz = (float)denbora.Elapsed.TotalSeconds;
                    }

                    if (markaNoiz != null && denbora.Elapsed.TotalSeconds > markaNoiz + ReleaseItxaronaldia)
                    {
                        break;
                    }
                }

                bool geldituta = !prozesua.HasExited;
                if (geldituta)
                {
                    prozesua.Kill();
                    prozesua.WaitForExit(5000);
                }

                return (geldituta ? (int?)null : prozesua.ExitCode, LogIrakurri(log), geldituta);
            }
        }

        private static string LogIrakurri(string bidea)
        {
            if (!File.Exists(bidea))
            {
                return string.Empty;
            }

            using (var fluxua = new FileStream(bidea, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var irakurlea = new StreamReader(fluxua))
            {
                return irakurlea.ReadToEnd();
            }
        }

        private static List<string> Markak(string log)
        {
            return log.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("ECL_", StringComparison.Ordinal)).ToList();
        }

        private static List<string> LogAkatsak(string log, List<string> markak)
        {
            var akatsak = markak.Where(m => Regex.IsMatch(m, @"^ECL_[A-Z_]*ERROR\b")).ToList();
            akatsak.AddRange(log.Split('\n').Where(l => Regex.IsMatch(l, @"\b\w*Exception\b:|^Exception\b")).Select(l => l.Trim()));
            return akatsak;
        }

        private static void NodoakEgiaztatu(string eszena, string marka, string izena, List<string> lerroak)
        {
            Match m = Regex.Match(marka, @"(\d+) nodo");
            int espero = EditorrekoNodoak[eszena];
            int nodoak = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            bool ondo = Mathf.Abs(nodoak - espero) <= espero * NodoTolerantzia;
            lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: [Sin interacción] {izena} {eszena}: grafoaren nodoak {nodoak} (Editorean {espero})");
        }

        /// <summary>
        /// Kausalitateak bermatzen duen ordena bakarrik eskatzen da: Start sekuentziala da, eta autoprobatzaileak HudAktibo ikusi ondoren idazten du ESZENA.
        /// Tarteko beste markak onartzen dira. Log osoa informean geratzen da, ordena errealarekin.
        /// </summary>
        private static void QaEgiaztatu(string qa, MailaDef[] mailak, List<string> lerroak)
        {
            (int? kodea, string log, bool geldituta) = PlayerExekutatu(qa, "player_qa.log", "-eclipseAutoProba", QaDenbora, null);
            List<string> markak = Markak(log);
            lerroak.Add($"{(kodea == 0 && !geldituta ? "ONDO" : "HUTS")}: [Simulado] QA: irteera-kodea {kodea?.ToString() ?? "(denbora agortuta, gelditua)"}");
            List<string> akatsak = LogAkatsak(log, markak);
            lerroak.Add($"{(akatsak.Count == 0 ? "ONDO" : "HUTS")}: [Simulado] QA: 0 akats log-ean {string.Join(" | ", akatsak.Take(5))}");

            int kurtsorea = 0;
            bool Hurrengoa(string izena, Func<string, bool> baldintza, out string aurkitua)
            {
                aurkitua = null;
                for (int i = kurtsorea; i < markak.Count; i++)
                {
                    if (baldintza(markak[i]))
                    {
                        aurkitua = markak[i];
                        kurtsorea = i + 1;
                        lerroak.Add($"ONDO: [Simulado] QA, ordena: {markak[i]}");
                        return true;
                    }
                }

                lerroak.Add($"HUTS: [Simulado] QA, ordena: ez da aurkitu '{izena}' {kurtsorea}. markaren ondoren");
                return false;
            }

            Hurrengoa("HASI debug=False", m => m.StartsWith("ECL_AUTOPROBA_HASI", StringComparison.Ordinal) && m.Contains("debug=False"), out _);
            foreach (MailaDef maila in mailak)
            {
                string eszena = maila.Izena;
                Hurrengoa("SORTUTA cc=True debug=False", m => m.StartsWith("ECL_JOKALARIA_SORTUTA", StringComparison.Ordinal) && m.Contains("cc=True") && m.Contains("debug=False"), out _);
                if (Hurrengoa($"NABIGAZIOA_OK {eszena}", m => m.StartsWith($"ECL_NABIGAZIOA_OK {eszena}:", StringComparison.Ordinal), out string nabigazioa))
                {
                    NodoakEgiaztatu(eszena, nabigazioa, "QA", lerroak);
                }

                Hurrengoa($"MISIOA_HASI {maila.Misioa.Izena}", m => m == $"ECL_MISIOA_HASI {maila.Misioa.Izena}", out _);
                Hurrengoa($"ESZENA {eszena}", m => m == $"ECL_AUTOPROBA_ESZENA {eszena}", out _);
                Hurrengoa($"JOKALARIA {eszena}", m => m == $"ECL_AUTOPROBA_JOKALARIA {eszena} cc=True kontrola=True", out _);
                int helburuak = maila.Misioa.Faseak.Sum(fasea => fasea.Helburuak.Length);
                for (int h = 0; h < helburuak; h++)
                {
                    Hurrengoa("MISIOA_HELBURUA", m => m.StartsWith("ECL_MISIOA_HELBURUA", StringComparison.Ordinal), out _);
                }

                Hurrengoa($"MISIOA_OK {maila.Misioa.Izena}", m => m == $"ECL_MISIOA_OK {maila.Misioa.Izena}", out _);
            }

            Hurrengoa("AMAITUTA", m => m.StartsWith("ECL_AUTOPROBA_AMAITUTA", StringComparison.Ordinal), out _);
            lerroak.Add($"INFO: QA, marken ordena erreala ({markak.Count}): {string.Join(" -> ", markak.Select(m => m.Split(' ')[0]))}");
        }

        /// <summary>Kontrol negatiboa: jokalaria kenduta, probak huts egin behar du. Bestela, egiaztapenak ez du ezer balio.</summary>
        private static void QaKontrolaEgiaztatu(string qa, MailaDef lehena, List<string> lerroak)
        {
            (int? kodea, string log, bool _) = PlayerExekutatu(qa, "player_qa_kontrola.log", "-eclipseAutoProba -eclipseAutoProbaKontrola", QaDenbora, null);
            List<string> markak = Markak(log);
            bool detektatua = markak.Contains($"ECL_AUTOPROBA_JOKALARIA {lehena.Izena} cc=False kontrola=False");
            bool errorea = markak.Any(m => m.StartsWith("ECL_AUTOPROBA_ERROR", StringComparison.Ordinal));
            bool amaitu = markak.Any(m => m.StartsWith("ECL_MISIOA_OK", StringComparison.Ordinal) || m.StartsWith("ECL_AUTOPROBA_AMAITUTA", StringComparison.Ordinal));
            lerroak.Add($"{(detektatua && errorea && !amaitu && kodea == 1 ? "ONDO" : "HUTS")}: [Simulado] Kontrol negatiboa: jokalaririk gabe proba eten egiten da (detektatua={detektatua}, errorea={errorea}, misioa amaitu={amaitu}, kodea={kodea?.ToString() ?? "gelditua"})");
        }

        /// <summary>Release: ez dago autoprobatzailerik, -eclipseAutoProba pasatuta ere. M1-ean abiatzen da eta gelditu egiten da.</summary>
        private static void ReleaseEgiaztatu(string release, MailaDef lehena, List<string> lerroak)
        {
            string marka = $"ECL_MISIOA_HASI {lehena.Misioa.Izena}";
            (int? _, string log, bool _) = PlayerExekutatu(release, "player_release.log", "-eclipseAutoProba", ReleaseDenbora, marka);
            List<string> markak = Markak(log);
            void Oharra(string izena, bool ondo, string xehetasuna = "") => lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: [Sin interacción] Release: {izena} {xehetasuna}");

            string sortuta = markak.FirstOrDefault(m => m.StartsWith("ECL_JOKALARIA_SORTUTA", StringComparison.Ordinal));
            Oharra("jokalaria sortuta, cc=True debug=False", sortuta != null && sortuta.Contains("cc=True") && sortuta.Contains("debug=False"), sortuta);
            string nabigazioa = markak.FirstOrDefault(m => m.StartsWith($"ECL_NABIGAZIOA_OK {lehena.Izena}:", StringComparison.Ordinal));
            Oharra("nabigazioa M1-ean", nabigazioa != null);
            if (nabigazioa != null)
            {
                NodoakEgiaztatu(lehena.Izena, nabigazioa, "Release", lerroak);
            }

            Oharra("misioa hasi da", markak.Contains(marka));
            Oharra("autoprobatzailerik ez, -eclipseAutoProba pasatuta ere", !markak.Any(m => m.StartsWith("ECL_AUTOPROBA", StringComparison.Ordinal)));
            Oharra("ez du helbururik bete bere kabuz", !markak.Any(m => m.StartsWith("ECL_MISIOA_HELBURUA", StringComparison.Ordinal) || m.StartsWith("ECL_MISIOA_OK", StringComparison.Ordinal)));
            List<string> akatsak = LogAkatsak(log, markak);
            Oharra("0 akats log-ean", akatsak.Count == 0, string.Join(" | ", akatsak.Take(5)));
            lerroak.Add($"INFO: Release, markak: {string.Join(" -> ", markak.Select(m => m.Split(' ')[0]))}");
        }
    }
}
