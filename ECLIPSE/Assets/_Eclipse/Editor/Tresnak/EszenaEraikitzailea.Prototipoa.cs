using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Maila-diseinuaren prototipoa, 02_PrototipoDiseinua eszenan. Sarea: gelaxka 1 = horma-panel 1 (3,75 m, 1,25 eskalan).
    /// Salak edozein tamainatakoak dira eta edozein hormatan izan ditzakete ateak; L, T eta + gurutzaketak 1x1 salak dira.
    /// Sarearen eta gainjartzeen balidazioa eraiki aurretik egiten da. 01_Instalazioa ez da ukitzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string ProtoMenua = "ECLIPSE/Prototipoa/";
        private const string ProtoEszena = "Assets/_Eclipse/Eszenak/02_PrototipoDiseinua.unity";
        private const string ProtoErroa = "Prototipoa";
        private const string ProtoArgiak = "Argiak";
        private const string PropioKarpeta = "Assets/_Eclipse/Prefabak/Propioak";
        private const string SeinaleEredua = PropioKarpeta + "/PF_Seinalea_SARRERA.prefab";
        private const string ZeruBidea = "Assets/_Eclipse/Datuak/Argiztapena/M_Zerua.mat";
        private const float SeinaleAltuera = 3.95f;
        private const float PanelTartea = 2f;
        private const float LuzeraTolerantzia = 0.1f;

        // Zonaren koloreak orientatu behar du, ez materialak estali.
        private const float ZonaKoloreArintzea = 0.45f;
        private const string HelmugaItsua = "ITSUA";

        private enum Aldea
        {
            Ekialdea,
            Mendebaldea,
            Iparra,
            Hegoa,
        }

        private enum HodiArgia
        {
            Argitua,
            Erdia,
            Iluna,
        }

        private static readonly Color Epela = Mathf.CorrelatedColorTemperatureToRGB(3800f);
        private static readonly Color Hotza = Mathf.CorrelatedColorTemperatureToRGB(6500f);

        // Sala nagusiak eta gurutzaketak. Koordenatuak gelaxketan: X0, Z0, zabalera, sakonera.
        private static readonly ProtoGela[] ProtoGelak =
        {
            new ProtoGela("Hasiera", 0, 6, 3, 3, "HASIERA", Epela, 5f),
            new ProtoGela("J1", 9, 7, 1, 1, "J1", new Color(0.2f, 0.85f, 1f), 3.5f),
            new ProtoGela("J2", 9, 11, 1, 1, "J2", new Color(0.3f, 1f, 0.35f), 3.5f),
            new ProtoGela("J3", 9, 3, 1, 1, "J3", new Color(1f, 0.65f, 0.1f), 3.5f),
            new ProtoGela("J4", 15, 17, 1, 1, "J4", new Color(0.9f, 0.3f, 1f), 3.5f),
            new ProtoGela("J5", 1, 17, 1, 1, "J5", new Color(1f, 0.15f, 0.12f), 3.5f),
            new ProtoGela("J6", 9, 17, 1, 1, "J6", new Color(0.75f, 0.85f, 1f), 3.5f),
            new ProtoGela("Borroka", 13, 8, 5, 6, "BORROKA", Hotza, 7f),
            new ProtoGela("Biltegia", 13, 2, 2, 3, "BILTEGIA", new Color(1f, 0.6f, 0.2f), 4f),
            new ProtoGela("Teknikoa", 8, 21, 3, 2, "TEKNIKOA", new Color(0.4f, 1f, 0.5f), 4f),
            new ProtoGela("Kutxa", 9, -2, 2, 2, "KUTXA", new Color(1f, 0.2f, 0.15f), 2.5f),
        };

        // Pasilloak. Gurutzeko koordenatua: E/M aldeetan Z gelaxka, I/H aldeetan X gelaxka. B-rik gabe: itsua, ProfileEnd-ekin.
        private static readonly ProtoLotura[] ProtoLoturak =
        {
            new ProtoLotura("Hasiera", Aldea.Ekialdea, 7, "J1", true, false, HodiArgia.Argitua, Hotza),
            new ProtoLotura("J1", Aldea.Iparra, 9, "J2", false, false, HodiArgia.Erdia, new Color(0.3f, 1f, 0.35f)),
            new ProtoLotura("J3", Aldea.Iparra, 9, "J1", false, false, HodiArgia.Erdia, new Color(1f, 0.65f, 0.1f)),
            new ProtoLotura("J2", Aldea.Ekialdea, 11, "Borroka", false, false, HodiArgia.Argitua, Hotza),
            new ProtoLotura("J3", Aldea.Ekialdea, 3, "Biltegia", false, false, HodiArgia.Erdia, new Color(1f, 0.65f, 0.1f)),
            new ProtoLotura("Biltegia", Aldea.Iparra, 13, "Borroka", true, true, HodiArgia.Iluna, Hotza),
            new ProtoLotura("Borroka", Aldea.Iparra, 15, "J4", true, true, HodiArgia.Erdia, new Color(0.9f, 0.3f, 1f)),
            new ProtoLotura("J6", Aldea.Ekialdea, 17, "J4", false, false, HodiArgia.Iluna, new Color(0.9f, 0.3f, 1f)),
            new ProtoLotura("J2", Aldea.Iparra, 9, "J6", false, false, HodiArgia.Erdia, new Color(0.75f, 0.85f, 1f)),
            new ProtoLotura("J5", Aldea.Ekialdea, 17, "J6", false, false, HodiArgia.Iluna, new Color(1f, 0.15f, 0.12f)),
            new ProtoLotura("Hasiera", Aldea.Iparra, 1, "J5", true, false, HodiArgia.Erdia, new Color(1f, 0.15f, 0.12f)),
            new ProtoLotura("J6", Aldea.Iparra, 9, "Teknikoa", true, true, HodiArgia.Argitua, new Color(0.4f, 1f, 0.5f)),
            new ProtoLotura("Kutxa", Aldea.Iparra, 9, "J3", true, true, HodiArgia.Iluna, new Color(1f, 0.2f, 0.15f)),
            new ProtoLotura("J3", Aldea.Mendebaldea, 3, 4, true, HodiArgia.Iluna, new Color(1f, 0.65f, 0.1f)),
        };

        // Kotak salaren barruan, -1etik 1era: ±1 hormaren kontra.
        private static readonly ProtoProp[] ProtoPropak =
        {
            new ProtoProp("Hasiera", RustyKarpeta, "Container_small", -0.8f, -0.8f, 0f, false),
            new ProtoProp("Hasiera", MagixBoxKarpeta, "Box3", 0.3f, -0.8f, 0f, true),
            new ProtoProp("Borroka", RustyKarpeta, "Container_Big", -0.4f, 0.5f, 0f, false),
            new ProtoProp("Borroka", RustyKarpeta, "Container_BigOpen", 0.45f, -0.5f, 0f, false),
            new ProtoProp("Borroka", MagixBoxKarpeta, "Box1", 0.22f, -0.1f, 0f, true),
            new ProtoProp("Borroka", RustyKarpeta, "Container_small", -0.35f, -0.45f, 90f, false),
            new ProtoProp("Borroka", MagixBoxKarpeta, "Box3", 0.65f, 0f, 0f, true),
            new ProtoProp("Borroka", RustyKarpeta, "Supply_box", 1f, 0.7f, 0f, false),
            new ProtoProp("Borroka", RustyKarpeta, "Rack", -1f, 1f, 0f, false),
            new ProtoProp("Borroka", RustyKarpeta, "Old_computer", -1f, -0.2f, 0f, false),
            new ProtoProp("Borroka", RustyKarpeta, "Oxygen_sint", 1f, -1f, 0f, false),
            new ProtoProp("Biltegia", RustyKarpeta, "Container_Big", 1f, 1f, 0f, false),
            new ProtoProp("Biltegia", RustyKarpeta, "Container_small", 1f, -1f, 0f, false),
            new ProtoProp("Biltegia", MagixBoxKarpeta, "Box1", 0.5f, -0.7f, 0f, true),
            new ProtoProp("Teknikoa", RustyKarpeta, "Old_computer", -1f, 1f, 0f, false),
            new ProtoProp("Teknikoa", RustyKarpeta, "Rack", 1f, 1f, 0f, false),
            new ProtoProp("Kutxa", MagixBoxKarpeta, "Box3", 1f, -1f, 0f, true),
        };

        // ---------- Menuak eta batch ----------

        [MenuItem(ProtoMenua + "Aurrebista")]
        private static void ProtoAurrebistaMenua() => ProtoExekutatu(false);

        [MenuItem(ProtoMenua + "Aplikatu")]
        private static void ProtoAplikatuMenua() => ProtoExekutatu(true);

        [MenuItem(ProtoMenua + "Ibilbideak probatu (ez du ezer aldatzen)")]
        private static void ProtoProbaMenua()
        {
            if (SceneManager.GetActiveScene().path == ProtoEszena)
            {
                ProtoProba();
            }
            else
            {
                Debug.LogError($"{Marka}_ERROR Ireki lehenago {ProtoEszena}.");
            }
        }

        /// <summary>Batchmode (Unity itxita): aurrebista, aplikatu eta, ondo bukatuz gero, gorde.</summary>
        public static void BatchPrototipoa()
        {
            int kodea = 1;
            try
            {
                if (ProtoExekutatu(false) && ProtoExekutatu(true))
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

        /// <summary>Batchmode: ibilbideen proba eta argazkiak. Ez du eszena gordetzen.</summary>
        public static void BatchPrototipoProba()
        {
            int kodea = 1;
            try
            {
                EditorSceneManager.OpenScene(ProtoEszena, OpenSceneMode.Single);
                bool ondo = ProtoProba();
                List<string> ikuspegiak = EbidentziaKapturak.PrototipoArgazkiak(ProtoKamerak());
                var errendimendua = new Informea(Tresna, Marka, "Prototipoa: Errendimendua", ProtoEszena, ProtoErroa);
                errendimendua.Atala("Ikuspegiak 1280x720: ikus-eremuko triangeluak (oklusiorik gabe) eta errendatze-denbora batch-ean", ikuspegiak);
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

        private static bool ProtoExekutatu(bool aplikatu)
        {
            string modua = aplikatu ? "Prototipoa: Aplikatu" : "Prototipoa: Aurrebista";
            Debug.Log($"{Marka}_HASI {modua}");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"{Marka}_ERROR Play moduan ezin da exekutatu.");
                return false;
            }

            var informea = new Informea(Tresna, Marka, modua, ProtoEszena, ProtoErroa);
            try
            {
                var plana = new Plana { ErroakSortu = true };
                ProtoPlanaEraiki(plana, aplikatu, out List<ProtoArgi> argiak, out string planoa);
                informea.Lerroa(string.Empty);
                informea.Lerroa("```text");
                informea.Lerroa(planoa);
                informea.Lerroa("```");
                informea.Atala("Salak", plana.SalaLerroak);
                informea.Atala("Modulu erabiliak", plana.ModuluLerroak);
                informea.Atala("Oharrak", plana.Oharrak);
                if (plana.Erroreak.Count > 0)
                {
                    informea.Atala("Erroreak", plana.Erroreak);
                    informea.AmaituErrorearekin($"{plana.Erroreak.Count} errore: ez da ezer aldatu");
                    return false;
                }

                SektorePlana sektorea = plana.Sektoreak[0];
                informea.Lerroa($"- **Piezak:** {sektorea.Piezak.Count}; **kolisioak:** {sektorea.Kolisioak.Count}; **argi propioak:** {argiak.Count}");

                if (!aplikatu)
                {
                    Scene irekita = SceneManager.GetActiveScene();
                    if (irekita.path != ProtoEszena)
                    {
                        informea.AmaituOndo($"{sektorea.Piezak.Count} pieza planean; eszena ez dago irekita (ez da ezer aldatu)");
                        return true;
                    }

                    var aurreikusiak = new Aldaketak();
                    Sinkronizatu(plana, false, irekita, aurreikusiak);
                    GameObject dagoena = irekita.GetRootGameObjects().FirstOrDefault(o => o.name == ProtoErroa);
                    int argiAurreikusiak = dagoena != null
                        ? HodiArgiakSinkronizatu(sektorea, dagoena.transform, false) + ProtoArgiakSinkronizatu(argiak, dagoena.transform, false)
                        : argiak.Count;
                    InformatuAldaketak(informea, aurreikusiak);
                    informea.AmaituOndo($"{aurreikusiak.Guztira + argiAurreikusiak} aldaketa aplikatzeko (ez da ezer aldatu)");
                    return true;
                }

                if (!ProtoEszenaPrestatu(out Scene eszena))
                {
                    informea.AmaituErrorearekin("Eszena ez da prestatu: erabiltzaileak utzi du");
                    return false;
                }

                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("ECLIPSE Prototipoa");
                int taldea = Undo.GetCurrentGroup();
                var aldaketak = new Aldaketak();
                Sinkronizatu(plana, true, eszena, aldaketak);
                Transform erroa = eszena.GetRootGameObjects().First(o => o.name == ProtoErroa).transform;
                int hodiArgiak = HodiArgiakSinkronizatu(sektorea, erroa);
                int argiAldaketak = ProtoArgiakSinkronizatu(argiak, erroa);
                bool ingurunea = InguruneaEzarri();
                Undo.CollapseUndoOperations(taldea);
                if (aldaketak.Guztira + hodiArgiak + argiAldaketak > 0 || ingurunea)
                {
                    EditorSceneManager.MarkSceneDirty(eszena);
                }

                InformatuAldaketak(informea, aldaketak);
                informea.Lerroa($"- **Hodi-argi aldatuak:** {hodiArgiak}; **argi propio aldatuak:** {argiAldaketak}");

                List<string> hutsak = Egiaztatu(plana, eszena);
                informea.Atala("Egiaztapena: pieza hauek ez daude espero zen tokian", hutsak);
                var bigarrena = new Aldaketak();
                Sinkronizatu(plana, false, eszena, bigarrena);
                int bigarrenArgiak = HodiArgiakSinkronizatu(sektorea, erroa, false) + ProtoArgiakSinkronizatu(argiak, erroa, false);
                if (hutsak.Count > 0 || bigarrena.Guztira + bigarrenArgiak > 0)
                {
                    informea.AmaituErrorearekin($"{hutsak.Count} pieza okerreko tokian; {bigarrena.Guztira + bigarrenArgiak} aldaketa geratzen dira");
                    return false;
                }

                informea.AmaituOndo($"{aldaketak.Guztira + hodiArgiak + argiAldaketak} aldaketa ({aldaketak.Laburpena})");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
        }

        private static bool ProtoEszenaPrestatu(out Scene eszena)
        {
            eszena = SceneManager.GetActiveScene();
            if (eszena.path == ProtoEszena)
            {
                return true;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            if (File.Exists(ProtoEszena))
            {
                eszena = EditorSceneManager.OpenScene(ProtoEszena, OpenSceneMode.Single);
                return true;
            }

            eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return EditorSceneManager.SaveScene(eszena, ProtoEszena);
        }

        /// <summary>Giro-argi laua eta ahula: bake-rik gabe ere fidagarria, eta tarte ilunek benetan ilun jarraitzen dute.</summary>
        private static bool InguruneaEzarri()
        {
            var giroa = new Color(0.07f, 0.08f, 0.1f);
            var zerua = AssetDatabase.LoadAssetAtPath<Material>(ZeruBidea);
            if (RenderSettings.skybox == zerua && RenderSettings.ambientMode == AmbientMode.Flat && RenderSettings.ambientLight == giroa)
            {
                return false;
            }

            RenderSettings.skybox = zerua;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = giroa;
            return true;
        }

        // ---------- Plana ----------

        private static void ProtoPlanaEraiki(Plana plana, bool aplikatu, out List<ProtoArgi> argiak, out string planoa)
        {
            argiak = new List<ProtoArgi>();
            planoa = ProtoSareaBalidatu(plana);
            if (plana.Erroreak.Count > 0)
            {
                return;
            }

            Dictionary<string, Modulua> m = ProtoModuluak(plana, aplikatu);
            if (plana.Erroreak.Count > 0)
            {
                return;
            }

            Neurriak n = NeurriakLortu(m, plana);
            if (n == null)
            {
                return;
            }

            foreach (Modulua modulua in m.Values.OrderBy(x => x.Izena, StringComparer.Ordinal))
            {
                plana.ModuluLerroak.Add($"{modulua.Izena}: {V(modulua.Kaxa.size)} m");
            }

            var sektorea = new SektorePlana { Izena = ProtoErroa, Posizioa = Vector3.zero, Geometria = true };
            plana.Sektoreak.Add(sektorea);

            Dictionary<string, ProtoGela> gelak = ProtoGelak.ToDictionary(g => g.Izena);
            // Ateak alde bakoitzean: gurutzeko gelaxka eta seinalean agertuko den helmuga.
            var ateak = new Dictionary<(string, Aldea), Dictionary<int, string>>();
            foreach (ProtoGela g in ProtoGelak)
            {
                foreach (Aldea a in Enum.GetValues(typeof(Aldea)))
                {
                    ateak[(g.Izena, a)] = new Dictionary<int, string>();
                }
            }

            foreach (ProtoLotura l in ProtoLoturak)
            {
                ateak[(l.A, l.AldeA)].Add(l.Gurutzea, l.B != null ? gelak[l.B].Seinalea : HelmugaItsua);
                if (l.B != null)
                {
                    ateak[(l.B, Kontrakoa(l.AldeA))].Add(l.Gurutzea, gelak[l.A].Seinalea);
                }
            }

            foreach (ProtoGela g in ProtoGelak)
            {
                ProtoSalaPlanifikatu(sektorea, g, m, n, ateak, argiak, plana);
            }

            foreach (ProtoLotura l in ProtoLoturak)
            {
                ProtoKorridorea(sektorea, l, gelak, m, n, argiak, plana);
            }

            ProtoPropakPlanifikatu(sektorea, gelak, m, n, ateak, plana);
        }

        private static Aldea Kontrakoa(Aldea a)
        {
            switch (a)
            {
                case Aldea.Ekialdea: return Aldea.Mendebaldea;
                case Aldea.Mendebaldea: return Aldea.Ekialdea;
                case Aldea.Iparra: return Aldea.Hegoa;
                default: return Aldea.Iparra;
            }
        }

        private static bool XArdatza(Aldea a) => a == Aldea.Ekialdea || a == Aldea.Mendebaldea;

        private static Vector3 Norabidea(Aldea a)
        {
            switch (a)
            {
                case Aldea.Ekialdea: return Vector3.right;
                case Aldea.Mendebaldea: return Vector3.left;
                case Aldea.Iparra: return Vector3.forward;
                default: return Vector3.back;
            }
        }

        /// <summary>Hormako ertza gelaxketan, sala baten alde batean.</summary>
        private static int Ertza(ProtoGela g, Aldea a)
        {
            switch (a)
            {
                case Aldea.Ekialdea: return g.X0 + g.W;
                case Aldea.Mendebaldea: return g.X0;
                case Aldea.Iparra: return g.Z0 + g.D;
                default: return g.Z0;
            }
        }

        /// <summary>Sarea egiaztatu eta planoa marraztu: pasilloak zuzenak, atea hormaren barruan, eta gelaxka bat elementu bakarrarentzat.</summary>
        private static string ProtoSareaBalidatu(Plana plana)
        {
            Dictionary<string, ProtoGela> gelak = ProtoGelak.ToDictionary(g => g.Izena);
            var sarea = new Dictionary<(int, int), char>();

            void Margotu(int x, int z, char karakterea, string nor)
            {
                if (sarea.TryGetValue((x, z), out char aurrekoa) && aurrekoa != karakterea)
                {
                    plana.Erroreak.Add($"{nor}: ({x},{z}) gelaxka okupatuta dago ('{aurrekoa}').");
                }

                sarea[(x, z)] = karakterea;
            }

            foreach (ProtoGela g in ProtoGelak)
            {
                char c = g.W == 1 && g.D == 1 ? '+' : g.Izena[0];
                for (int x = g.X0; x < g.X0 + g.W; x++)
                {
                    for (int z = g.Z0; z < g.Z0 + g.D; z++)
                    {
                        Margotu(x, z, c, g.Izena);
                    }
                }
            }

            foreach (ProtoLotura l in ProtoLoturak)
            {
                if (!gelak.TryGetValue(l.A, out ProtoGela a))
                {
                    plana.Erroreak.Add($"Lotura: '{l.A}' sala ez dago.");
                    continue;
                }

                int ertzA = Ertza(a, l.AldeA);
                int noranzkoa = l.AldeA == Aldea.Ekialdea || l.AldeA == Aldea.Iparra ? 1 : -1;
                int luzera = l.Mutur;
                if (l.B != null)
                {
                    if (!gelak.TryGetValue(l.B, out ProtoGela b))
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

                char hodiKarakterea = XArdatza(l.AldeA) ? '-' : '|';
                for (int i = 0; i < luzera; i++)
                {
                    int s = noranzkoa > 0 ? ertzA + i : ertzA - 1 - i;
                    if (XArdatza(l.AldeA))
                    {
                        Margotu(s, l.Gurutzea, hodiKarakterea, $"{l.A}→{l.B}");
                    }
                    else
                    {
                        Margotu(l.Gurutzea, s, hodiKarakterea, $"{l.A}→{l.B}");
                    }
                }
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

        private static bool GurutzeaBarruan(ProtoGela g, Aldea a, int gurutzea)
        {
            return XArdatza(a)
                ? gurutzea >= g.Z0 && gurutzea < g.Z0 + g.D
                : gurutzea >= g.X0 && gurutzea < g.X0 + g.W;
        }

        /// <summary>Prefab-ak kargatu eta neurtu. Aurrebistan seinale berriak ez dira sortzen: dagoen baten neurria erabiltzen da, malla bera baitute.</summary>
        private static Dictionary<string, Modulua> ProtoModuluak(Plana plana, bool aplikatu)
        {
            var bideak = new List<string>
            {
                $"{MagixBoxKarpeta}/{Zorua}.prefab",
                $"{MagixBoxKarpeta}/{Horma}.prefab",
                $"{MagixBoxKarpeta}/{Sabaia}.prefab",
                $"{MagixBoxKarpeta}/{Markoa}.prefab",
                $"{MagixBoxKarpeta}/{SekzioMuturra}.prefab",
                $"{MagixBoxKarpeta}/Starter/{Sekzioa}.prefab",
                $"{MagixBoxKarpeta}/Starter/{SekzioAtea}.prefab",
                $"{PropioKarpeta}/PF_ArgiPanela_Hotza.prefab",
            };
            bideak.AddRange(ProtoPropak.Select(p => $"{p.Karpeta}/{p.Prefab}.prefab").Distinct());
            bideak.Add(SeinaleEredua);
            bideak.AddRange(ProtoGelak.Select(g => g.Seinalea).Append(HelmugaItsua).Select(testua => $"{PropioKarpeta}/PF_Seinalea_{testua}.prefab"));

            var faltaDirenak = new List<string>();
            var prefabak = new List<GameObject>();
            foreach (string bidea in bideak)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
                if (prefab == null && bidea.Contains("PF_Seinalea_"))
                {
                    string testua = Path.GetFileNameWithoutExtension(bidea).Substring("PF_Seinalea_".Length);
                    if (!aplikatu)
                    {
                        faltaDirenak.Add(testua);
                        continue;
                    }

                    prefab = EszenaPropioa.SeinaleaPrefab(testua);
                }

                if (prefab == null)
                {
                    plana.Erroreak.Add($"Ez da aurkitu: {bidea}");
                }
                else
                {
                    prefabak.Add(prefab);
                }
            }

            if (plana.Erroreak.Count > 0)
            {
                return new Dictionary<string, Modulua>();
            }

            Dictionary<string, Modulua> moduluak = NeurtuGuztiak(prefabak.Distinct(), true).ToDictionary(x => x.Izena);
            Modulua eredua = moduluak[Path.GetFileNameWithoutExtension(SeinaleEredua)];
            foreach (string testua in faltaDirenak)
            {
                string izena = $"PF_Seinalea_{testua}";
                moduluak[izena] = new Modulua
                {
                    Izena = izena,
                    Prefab = eredua.Prefab,
                    Kaxa = eredua.Kaxa,
                    Zatiak = eredua.Zatiak,
                    ErroBiraketa = eredua.ErroBiraketa,
                    ErroEskala = eredua.ErroEskala,
                    RendererKop = eredua.RendererKop,
                    ColliderKop = eredua.ColliderKop,
                    RigidbodyDu = eredua.RigidbodyDu,
                    ArgiKop = eredua.ArgiKop,
                };
                plana.Oharrak.Add($"{izena}: aplikatzean sortuko da.");
            }

            return moduluak;
        }

        /// <summary>Sala: zorua, sabaia, gelaxka bakoitzeko horma edo atea, argiak, seinaleak eta kolisioak. Lauzak zehatz egokitzen dira gelaxketara.</summary>
        private static void ProtoSalaPlanifikatu(SektorePlana s, ProtoGela g, Dictionary<string, Modulua> m, Neurriak n,
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
            float h = n.SalaAltuera;

            int nx = Mathf.Max(1, Mathf.RoundToInt(lx / n.Lauza.x));
            int nz = Mathf.Max(1, Mathf.RoundToInt(lz / n.Lauza.z));
            float tx = lx / nx;
            float tz = lz / nz;
            Modulua zorua = m[Zorua];
            Modulua sabaia = m[Sabaia];
            Quaternion iraulita = Quaternion.Euler(180f, 0f, 0f);
            var zoruF = new Vector3(tx / n.Lauza.x, 1f, tz / n.Lauza.z);
            var sabaiF = new Vector3(tx / sabaia.Kaxa.size.x, 1f, tz / sabaia.Kaxa.size.z);
            float sabaiLodiera = KaxaKalkulatu(sabaia, iraulita, sabaiF).size.y;
            for (int ix = 0; ix < nx; ix++)
            {
                for (int iz = 0; iz < nz; iz++)
                {
                    float x = xMin + (ix + 0.5f) * tx;
                    float z = zMin + (iz + 0.5f) * tz;
                    PiezaGehitu(s, g.Izena, Zoruak, zorua, Quaternion.identity, zoruF, new Vector3(x, -n.Lauza.y * 0.5f, z), plana);
                    PiezaGehitu(s, g.Izena, Sabaiak, sabaia, iraulita, sabaiF, new Vector3(x, h + sabaiLodiera * 0.5f, z), plana);
                }
            }

            foreach (Aldea alde in Enum.GetValues(typeof(Aldea)))
            {
                ProtoHorma(s, g, alde, m, n, ateak[(g.Izena, alde)], plana);
            }

            KolisioGehitu(s, g.Izena, new Vector3(cx, -KolisioLodiera * 0.5f, cz), new Vector3(lx, KolisioLodiera, lz));
            KolisioGehitu(s, g.Izena, new Vector3(cx, h + KolisioLodiera * 0.5f, cz), new Vector3(lx, KolisioLodiera, lz));

            // Gela_* bolumena, probe-etarako eta bake-erako prest.
            s.Gelak.Add(new GelaPlana
            {
                Izena = $"Gela_P_{g.Izena}",
                Posizioa = new Vector3(cx, 0f, cz),
                Zentroa = new Vector3(0f, h * 0.5f, 0f),
                Tamaina = new Vector3(lx - 2f * n.HormaLodiera, h, lz - 2f * n.HormaLodiera),
            });
            plana.SalaLerroak.Add($"{g.Izena}: {g.W} x {g.D} gelaxka = {F(lx)} x {F(lz)} m, altuera {F(h)} m");

            // Panelak eta argiak. Sala txikietan bakarra; handietan, 2 gelaxkatik behin.
            int px = Mathf.Max(1, Mathf.RoundToInt(g.W / PanelTartea));
            int pz = Mathf.Max(1, Mathf.RoundToInt(g.D / PanelTartea));
            Modulua panela = m["PF_ArgiPanela_Hotza"];
            for (int ix = 0; ix < px; ix++)
            {
                for (int iz = 0; iz < pz; iz++)
                {
                    float x = xMin + (ix + 0.5f) * lx / px;
                    float z = zMin + (iz + 0.5f) * lz / pz;
                    Vector3 panelNeurria = KaxaKalkulatu(panela, Quaternion.identity, Vector3.one).size;
                    PiezaGehitu(s, g.Izena, Sabaiak, panela, Quaternion.identity, Vector3.one, new Vector3(x, h - 0.03f - panelNeurria.y * 0.5f, z), plana);
                    argiak.Add(new ProtoArgi
                    {
                        Izena = $"Argia_{g.Izena}_{ix}{iz}",
                        Posizioa = new Vector3(x, h - 1.2f, z),
                        Kolorea = Color.Lerp(g.Kolorea, Color.white, ZonaKoloreArintzea),
                        Intentsitatea = g.Intentsitatea,
                        Tartea = Mathf.Max(lx / px, lz / pz) * 1.6f + 3f,
                    });
                }
            }
        }

        private static void ProtoHorma(SektorePlana s, ProtoGela g, Aldea alde, Dictionary<string, Modulua> m, Neurriak n, Dictionary<int, string> ateak, Plana plana)
        {
            float c = n.Panela;
            bool xArdatza = XArdatza(alde);
            Vector3 normala = -Norabidea(alde);
            float ertza = Ertza(g, alde) * c;
            int hasiera = xArdatza ? g.Z0 : g.X0;
            int kopurua = xArdatza ? g.D : g.W;
            float zentroNormalean = ertza + n.HormaLodiera * 0.5f * Zeinua(normala);
            Modulua horma = m[Horma];

            for (int i = hasiera; i < hasiera + kopurua; i++)
            {
                float a0 = i * c;
                float a1 = (i + 1) * c;

                if (ateak.TryGetValue(i, out string helmuga))
                {
                    ProtoAtea(s, g, helmuga, normala, ertza, (a0 + a1) * 0.5f, m, n, plana);
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

                for (int ilara = 0; ilara < HormaIlarak; ilara++)
                {
                    float y0 = ilara * n.PanelAltuera;
                    HormaPieza(s, g.Izena, horma, n, normala, ertza, a, b, y0, y0 + n.PanelAltuera, plana);
                }

                HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, a0, a1, 0f, n.SalaAltuera);
            }
        }

        /// <summary>Atea hormaren gelaxka batean: markoa, gaineko panela, nora daraman dioen seinalea eta albo/goiko kolisioak.</summary>
        private static void ProtoAtea(SektorePlana s, ProtoGela g, string helmuga, Vector3 normala, float ertza, float erdia, Dictionary<string, Modulua> m, Neurriak n, Plana plana)
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
            if (n.SalaAltuera - markoGoia > 0.01f)
            {
                HormaPieza(s, g.Izena, m[Horma], n, normala, ertza, a0, a1, markoGoia, n.SalaAltuera, plana);
            }

            float zentroNormalean = ertza + n.HormaLodiera * 0.5f * Zeinua(normala);
            HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, a0, erdia - n.HodiBarruErdia, 0f, n.SalaAltuera);
            HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, erdia + n.HodiBarruErdia, a1, 0f, n.SalaAltuera);
            HormaKolisioa(s, g.Izena, n, normala, zentroNormalean, erdia - n.HodiBarruErdia, erdia + n.HodiBarruErdia, n.HodiAltuera, n.SalaAltuera);

            // Seinalea atearen gainean, salaren barrura begira.
            Modulua seinalea = m[$"PF_Seinalea_{helmuga}"];
            Quaternion seinaleBiraketa = Quaternion.LookRotation(-normala);
            Vector3 seinaleZentroa = HormaZentroa(normala, barrukoAurpegia + 0.03f * Zeinua(normala), erdia, SeinaleAltuera);
            PiezaGehitu(s, g.Izena, Hormak, seinalea, seinaleBiraketa, Vector3.one, seinaleZentroa, plana);
        }

        /// <summary>Pasillo zuzena: atalasea, atea (nahi bada), Profile1 tarteak luzerara egokituta, eta atea, atalasea edo ProfileEnd.</summary>
        private static void ProtoKorridorea(SektorePlana s, ProtoLotura l, Dictionary<string, ProtoGela> gelak, Dictionary<string, Modulua> m, Neurriak n, List<ProtoArgi> argiak, Plana plana)
        {
            float c = n.Panela;
            ProtoGela a = gelak[l.A];
            bool xArdatza = XArdatza(l.AldeA);
            float noranzkoa = l.AldeA == Aldea.Ekialdea || l.AldeA == Aldea.Iparra ? 1f : -1f;
            float s0 = Ertza(a, l.AldeA) * c;
            float s1 = l.B != null
                ? Ertza(gelak[l.B], Kontrakoa(l.AldeA)) * c
                : s0 + l.Mutur * c * noranzkoa;
            float gurutzea = (l.Gurutzea + 0.5f) * c;
            string sala = l.B != null ? $"Pasilloa_{l.A}_{l.B}" : $"Pasilloa_{l.A}_itsua";
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

            // Atalaseak.
            ProtoAtalasea(s, sala, m, n, Puntua(lotura * 0.5f, -n.Lauza.y * 0.5f), lotura, xArdatza, plana);
            if (l.B != null)
            {
                ProtoAtalasea(s, sala, m, n, Puntua(luzeraGuztira - lotura * 0.5f, -n.Lauza.y * 0.5f), lotura, xArdatza, plana);
            }

            if (l.AteaA)
            {
                ProtoHodiAtea(s, sala, m[SekzioAtea], n, biraketa, Puntua(d0 + n.AteLuzera * 0.5f, n.HodiErdiaY), xArdatza, plana);
                d0 += n.AteLuzera;
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
            plana.Oharrak.Add($"{sala}: {tarteak} x {Sekzioa}, luzeran x{f:0.000}");
            if (Mathf.Abs(f - 1f) > LuzeraTolerantzia)
            {
                // Mailak modulu osoekin handitzen dira: pasilloa ez da modulua luzatuz egokitzen.
                plana.Erroreak.Add($"{sala}: {Sekzioa} x{f:0.00} luzeran; aldatu pasilloaren gelaxka-kopurua edo ateak.");
            }

            for (int i = 0; i < tarteak; i++)
            {
                Vector3 faktoreak = new Vector3(1f, 1f, f);
                Bounds neurria = KaxaKalkulatu(m[Sekzioa], biraketa, faktoreak);
                Vector3 erroa = Puntua(d0 + (i + 0.5f) * tarteLuzera, n.HodiErdiaY);
                PiezaGehitu(s, sala, Hodiak, m[Sekzioa], biraketa, faktoreak, erroa + neurria.center, plana);
                Pieza pieza = s.Piezak[s.Piezak.Count - 1];
                pieza.HodiArgiaPiztuta = l.Argia == HodiArgia.Argitua ? i % 2 == 0 : l.Argia == HodiArgia.Erdia && i % 3 == 1;
                pieza.HodiArgiKolorea = l.Kolorea;
            }

            if (l.B == null)
            {
                Quaternion muturra = Quaternion.LookRotation(-Norabidea(l.AldeA));
                Bounds neurria = KaxaKalkulatu(m[SekzioMuturra], muturra, Vector3.one);
                Vector3 erroa = Puntua(luzeraGuztira - n.MuturLuzera * 0.5f, n.HodiErdiaY);
                PiezaGehitu(s, sala, Hodiak, m[SekzioMuturra], muturra, Vector3.one, erroa + neurria.center, plana);
                ProtoKutxa(s, sala, Puntua(luzeraGuztira - n.MuturLuzera * 0.5f, n.HodiAltuera * 0.5f), xArdatza, n.MuturLuzera, n.HodiAltuera, n.HodiBarruErdia * 2f);

                // Itsuak arrazoia du: atearen atzean, argi ahul baten azpian, biltegiratze-kutxa bat.
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

            // Kolisioak: zorua, bi hormak eta sabaia, mutur batetik bestera.
            float erdia = luzeraGuztira * 0.5f;
            float zabalera = n.HodiBarruErdia * 2f + KolisioLodiera * 2f;
            ProtoKutxa(s, sala, Puntua(erdia, -KolisioLodiera * 0.5f), xArdatza, luzeraGuztira, KolisioLodiera, zabalera);
            ProtoKutxa(s, sala, Puntua(erdia, n.HodiAltuera + KolisioLodiera * 0.5f), xArdatza, luzeraGuztira, KolisioLodiera, zabalera);
            float albo = n.HodiBarruErdia + KolisioLodiera * 0.5f;
            ProtoKutxa(s, sala, Puntua(erdia, n.HodiAltuera * 0.5f, albo), xArdatza, luzeraGuztira, n.HodiAltuera, KolisioLodiera);
            ProtoKutxa(s, sala, Puntua(erdia, n.HodiAltuera * 0.5f, -albo), xArdatza, luzeraGuztira, n.HodiAltuera, KolisioLodiera);
        }

        private static void ProtoAtalasea(SektorePlana s, string sala, Dictionary<string, Modulua> m, Neurriak n, Vector3 zentroa, float lotura, bool xArdatza, Plana plana)
        {
            Vector3 faktoreak = xArdatza
                ? new Vector3(lotura / n.Lauza.x, 1f, n.Panela / n.Lauza.z)
                : new Vector3(n.Panela / n.Lauza.x, 1f, lotura / n.Lauza.z);
            PiezaGehitu(s, sala, Zoruak, m[Zorua], Quaternion.identity, faktoreak, zentroa, plana);
        }

        private static void ProtoHodiAtea(SektorePlana s, string sala, Modulua m, Neurriak n, Quaternion biraketa, Vector3 erroa, bool xArdatza, Plana plana)
        {
            Bounds neurria = KaxaKalkulatu(m, biraketa, Vector3.one);
            PiezaGehitu(s, sala, Hodiak, m, biraketa, Vector3.one, erroa + neurria.center, plana, AteOrria);
            Pieza pieza = s.Piezak[s.Piezak.Count - 1];
            pieza.HodiArgiaPiztuta = false;

            float alboa = n.HodiBarruErdia - n.AteZabalera * 0.5f;
            float albokoa = n.AteZabalera * 0.5f + alboa * 0.5f;
            Vector3 zeharka = xArdatza ? Vector3.forward : Vector3.right;
            Vector3 oina = new Vector3(erroa.x, n.HodiAltuera * 0.5f, erroa.z);
            ProtoKutxa(s, sala, oina + zeharka * albokoa, xArdatza, n.AteLuzera, n.HodiAltuera, alboa);
            ProtoKutxa(s, sala, oina - zeharka * albokoa, xArdatza, n.AteLuzera, n.HodiAltuera, alboa);
            float goia = n.HodiAltuera - n.AteAltuera;
            if (goia > 0.01f)
            {
                ProtoKutxa(s, sala, new Vector3(erroa.x, n.AteAltuera + goia * 0.5f, erroa.z), xArdatza, n.AteLuzera, goia, n.AteZabalera);
            }
        }

        private static void ProtoKutxa(SektorePlana s, string sala, Vector3 zentroa, bool xArdatza, float luzera, float altuera, float zabalera)
        {
            KolisioGehitu(s, sala, zentroa, xArdatza ? new Vector3(luzera, altuera, zabalera) : new Vector3(zabalera, altuera, luzera));
        }

        private static void ProtoPropakPlanifikatu(SektorePlana s, Dictionary<string, ProtoGela> gelak, Dictionary<string, Modulua> m, Neurriak n,
            Dictionary<(string, Aldea), Dictionary<int, string>> ateak, Plana plana)
        {
            float c = n.Panela;
            var hartuak = new Dictionary<string, List<Bounds>>();
            foreach (ProtoProp prop in ProtoPropak)
            {
                ProtoGela g = gelak[prop.Sala];
                string izena = $"{prop.Sala}/{prop.Prefab}";
                Modulua modulua = m[prop.Prefab];
                if (modulua.RigidbodyDu)
                {
                    plana.Oharrak.Add($"{izena}: Rigidbody du; omitituta.");
                    continue;
                }

                Quaternion biraketa = Quaternion.Euler(0f, prop.Yaw, 0f);
                Vector3 tamaina = KaxaKalkulatu(modulua, biraketa, Vector3.one).size;
                float erdiX = g.W * c * 0.5f - n.HormaLodiera;
                float erdiZ = g.D * c * 0.5f - n.HormaLodiera;
                if (tamaina.x > erdiX * 2f || tamaina.z > erdiZ * 2f || tamaina.y > n.SalaAltuera)
                {
                    plana.Oharrak.Add($"{izena}: ez da sartzen ({V(tamaina)} m); omitituta.");
                    continue;
                }

                var zentroa = new Vector3(
                    (g.X0 + g.W * 0.5f) * c + prop.U * (erdiX - tamaina.x * 0.5f),
                    tamaina.y * 0.5f,
                    (g.Z0 + g.D * 0.5f) * c + prop.V * (erdiZ - tamaina.z * 0.5f));
                var kaxa = new Bounds(zentroa, tamaina);

                if (ProtoAteaBlokeatzenDu(kaxa, g, ateak, n))
                {
                    plana.Oharrak.Add($"{izena}: atearen pasabidea blokeatzen du; omitituta.");
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
                    plana.Oharrak.Add($"{izena}: beste prop batekin gainjartzen da; omitituta.");
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

        private static bool ProtoAteaBlokeatzenDu(Bounds kaxa, ProtoGela g, Dictionary<(string, Aldea), Dictionary<int, string>> ateak, Neurriak n)
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

        // ---------- Argiak (Realtime, oraingoz; bake-a sektoreetan) ----------

        /// <summary>Profile1-ek bere puntu-argia dakar itzalekin: itzalak kendu eta tarteka piztu, errendimendua mantentzeko.</summary>
        private static int HodiArgiakSinkronizatu(SektorePlana s, Transform erroa, bool aplikatu = true)
        {
            int aldaketak = 0;
            foreach (Pieza pieza in s.Piezak.Where(p => p.HodiArgiaPiztuta != null))
            {
                Transform kontainerra = erroa.Find(pieza.Kontainerra);
                Transform aurkitua = kontainerra != null ? SemeaBilatu(kontainerra, pieza.Izena) : null;
                Light argia = aurkitua != null ? aurkitua.GetComponentInChildren<Light>(true) : null;
                if (argia == null)
                {
                    continue;
                }

                bool piztuta = pieza.HodiArgiaPiztuta.Value;
                bool koloreOna = pieza.HodiArgiKolorea == null || KoloreBerdina(argia.color, pieza.HodiArgiKolorea.Value);
                if (argia.enabled == piztuta && argia.shadows == LightShadows.None && koloreOna)
                {
                    continue;
                }

                aldaketak++;
                if (aplikatu)
                {
                    Undo.RecordObject(argia, pieza.Izena);
                    argia.enabled = piztuta;
                    argia.shadows = LightShadows.None;
                    if (pieza.HodiArgiKolorea != null)
                    {
                        argia.color = pieza.HodiArgiKolorea.Value;
                    }

                    PrefabUtility.RecordPrefabInstancePropertyModifications(argia);
                }
            }

            return aldaketak;
        }

        private static bool KoloreBerdina(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }

        private static int ProtoArgiakSinkronizatu(List<ProtoArgi> argiak, Transform erroa, bool aplikatu = true)
        {
            int aldaketak = 0;
            Transform kontainerra = SemeaBilatu(erroa, ProtoArgiak);
            if (kontainerra == null)
            {
                aldaketak += argiak.Count;
                if (!aplikatu)
                {
                    return aldaketak;
                }

                var berria = new GameObject(ProtoArgiak);
                berria.transform.SetParent(erroa, false);
                Undo.RegisterCreatedObjectUndo(berria, ProtoArgiak);
                kontainerra = berria.transform;
                aldaketak = 0;
            }

            var izenak = new HashSet<string>(argiak.Select(a => a.Izena));
            foreach (ProtoArgi d in argiak)
            {
                Transform t = SemeaBilatu(kontainerra, d.Izena);
                Light argia = t != null ? t.GetComponent<Light>() : null;
                if (argia != null
                    && (t.localPosition - d.Posizioa).sqrMagnitude < Tolerantzia
                    && KoloreBerdina(argia.color, d.Kolorea)
                    && Mathf.Abs(argia.intensity - d.Intentsitatea) < Tolerantzia
                    && Mathf.Abs(argia.range - d.Tartea) < Tolerantzia)
                {
                    continue;
                }

                aldaketak++;
                if (!aplikatu)
                {
                    continue;
                }

                if (t == null)
                {
                    var berria = new GameObject(d.Izena);
                    berria.transform.SetParent(kontainerra, false);
                    Undo.RegisterCreatedObjectUndo(berria, d.Izena);
                    t = berria.transform;
                }

                if (argia == null)
                {
                    argia = Undo.AddComponent<Light>(t.gameObject);
                }

                Undo.RecordObject(t, d.Izena);
                Undo.RecordObject(argia, d.Izena);
                t.localPosition = d.Posizioa;
                argia.type = LightType.Point;
                argia.lightmapBakeType = LightmapBakeType.Realtime;
                argia.shadows = LightShadows.None;
                argia.color = d.Kolorea;
                argia.intensity = d.Intentsitatea;
                argia.range = d.Tartea;
            }

            for (int i = kontainerra.childCount - 1; i >= 0; i--)
            {
                Transform t = kontainerra.GetChild(i);
                if (!izenak.Contains(t.name))
                {
                    aldaketak++;
                    if (aplikatu)
                    {
                        Undo.DestroyObjectImmediate(t.gameObject);
                    }
                }
            }

            return aldaketak;
        }

        // ---------- Probak ----------

        /// <summary>
        /// Kapsula (1,8 m, 0,3 m) lotura guztietan zehar, salaren erditik bestearenera eta bi noranzkoetan baliokidea;
        /// zorua pauso bakoitzean, sala eta pasilloak itxita, bucle-kopurua eta errendimenduaren datuak.
        /// </summary>
        private static bool ProtoProba()
        {
            const float Erradioa = 0.3f;
            const float Altuera = 1.8f;
            const float Urratsa = 0.05f;
            const float ItsuTartea = 3.5f;

            Physics.SyncTransforms();
            var informea = new Informea(Tresna, Marka, "Prototipoa: Proba", ProtoEszena, ProtoErroa);
            var hutsak = new List<string>();
            var ibilbideak = new List<string>();
            Dictionary<string, ProtoGela> gelak = ProtoGelak.ToDictionary(g => g.Izena);
            float c = ProtoGelaxka();
            int urratsakGuztira = 0;

            void Ibili(string izena, IList<Vector3> puntuak)
            {
                int talkak = 0;
                int zoruGabe = 0;
                int urratsak = 0;
                string lehena = null;
                for (int i = 0; i + 1 < puntuak.Count; i++)
                {
                    float luzera = Vector3.Distance(puntuak[i], puntuak[i + 1]);
                    int pausoak = Mathf.Max(1, Mathf.CeilToInt(luzera / Urratsa));
                    for (int k = 0; k <= pausoak; k++)
                    {
                        Vector3 p = Vector3.Lerp(puntuak[i], puntuak[i + 1], (float)k / pausoak);
                        urratsak++;
                        if (Physics.CheckCapsule(p + Vector3.up * (Erradioa + 0.02f), p + Vector3.up * (Altuera - Erradioa + 0.02f), Erradioa, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            talkak++;
                            lehena ??= V(p);
                        }

                        if (!Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out RaycastHit jo, 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || Mathf.Abs(jo.point.y) > 0.05f)
                        {
                            zoruGabe++;
                            lehena ??= V(p);
                        }
                    }
                }

                urratsakGuztira += urratsak;
                ibilbideak.Add($"{izena}: {urratsak} urrats, {talkak} talka, {zoruGabe} zorurik gabe{(lehena != null ? $" (lehena {lehena})" : string.Empty)}");
                if (talkak + zoruGabe > 0)
                {
                    hutsak.Add(ibilbideak[ibilbideak.Count - 1]);
                }
            }

            foreach (ProtoLotura l in ProtoLoturak)
            {
                ProtoGela a = gelak[l.A];
                float gurutzea = (l.Gurutzea + 0.5f) * c;
                bool x = XArdatza(l.AldeA);
                Vector3 Lerroan(float s) => x ? new Vector3(s, 0f, gurutzea) : new Vector3(gurutzea, 0f, s);
                Vector3 erdiA = new Vector3((a.X0 + a.W * 0.5f) * c, 0f, (a.Z0 + a.D * 0.5f) * c);
                Vector3 proiekzioA = Lerroan(x ? erdiA.x : erdiA.z);

                var puntuak = new List<Vector3> { erdiA, proiekzioA };
                if (l.B != null)
                {
                    ProtoGela b = gelak[l.B];
                    Vector3 erdiB = new Vector3((b.X0 + b.W * 0.5f) * c, 0f, (b.Z0 + b.D * 0.5f) * c);
                    puntuak.Add(Lerroan(x ? erdiB.x : erdiB.z));
                    puntuak.Add(erdiB);
                    Ibili($"{l.A}→{l.B}", puntuak);
                }
                else
                {
                    float noranzkoa = l.AldeA == Aldea.Ekialdea || l.AldeA == Aldea.Iparra ? 1f : -1f;
                    float s = Ertza(a, l.AldeA) * c + (l.Mutur * c - ItsuTartea) * noranzkoa;
                    puntuak.Add(Lerroan(s));
                    Ibili($"{l.A}→itsua", puntuak);
                }
            }

            // Itxitura: sala bakoitzaren erditik 32 izpi horizontal eta bat gora.
            int ihesak = 0;
            foreach (ProtoGela g in ProtoGelak)
            {
                Vector3 jatorria = new Vector3((g.X0 + g.W * 0.5f) * c, 1f, (g.Z0 + g.D * 0.5f) * c);
                for (int i = 0; i < 32; i++)
                {
                    Vector3 norabidea = Quaternion.Euler(0f, i * 360f / 32f, 0f) * Vector3.forward;
                    if (!Physics.Raycast(jatorria, norabidea, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        ihesak++;
                    }
                }

                if (!Physics.Raycast(jatorria, Vector3.up, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    ihesak++;
                }
            }

            if (ihesak > 0)
            {
                hutsak.Add($"{ihesak} izpik ihes egiten dute.");
            }

            // Prop bakoitzak talka egin behar du: kit-ekoak bere collider-arekin, Box1 eta Box3 Kolisioak kaxekin.
            GameObject protoErroa = SceneManager.GetActiveScene().GetRootGameObjects().First(o => o.name == ProtoErroa);
            int propak = 0;
            foreach (Transform kontainerra in protoErroa.GetComponentsInChildren<Transform>(true).Where(t => t.name == Propak))
            {
                foreach (Transform prop in kontainerra)
                {
                    Renderer[] propRendererrak = prop.GetComponentsInChildren<Renderer>();
                    if (propRendererrak.Length == 0)
                    {
                        continue;
                    }

                    Bounds kaxa = propRendererrak[0].bounds;
                    foreach (Renderer r in propRendererrak)
                    {
                        kaxa.Encapsulate(r.bounds);
                    }

                    propak++;
                    if (!Physics.CheckBox(kaxa.center, kaxa.extents * 0.9f, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        hutsak.Add($"{prop.name}: ez du talkarik ({kontainerra.parent.name}).");
                    }
                }
            }

            // Grafoa: konexioa eta bucleak.
            var bisitatuak = new HashSet<string>();
            var pila = new Stack<string>();
            pila.Push(ProtoGelak[0].Izena);
            while (pila.Count > 0)
            {
                string uneko = pila.Pop();
                if (!bisitatuak.Add(uneko))
                {
                    continue;
                }

                foreach (ProtoLotura l in ProtoLoturak.Where(x => x.B != null))
                {
                    if (l.A == uneko)
                    {
                        pila.Push(l.B);
                    }
                    else if (l.B == uneko)
                    {
                        pila.Push(l.A);
                    }
                }
            }

            if (bisitatuak.Count != ProtoGelak.Length)
            {
                hutsak.Add($"Grafoa ez dago konektatuta: {bisitatuak.Count} / {ProtoGelak.Length} sala.");
            }

            int ertzak = ProtoLoturak.Count(l => l.B != null);
            int bucleak = ertzak - ProtoGelak.Length + 1;

            // Errendimendua: kopuruak eszenan.
            Renderer[] rendererrak = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            long triangeluak = rendererrak.Sum(EbidentziaKapturak.Triangeluak);
            List<string> prefabKostua = rendererrak
                .GroupBy(r =>
                {
                    GameObject instantzia = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
                    return instantzia != null ? PiezaIzena.Replace(instantzia.name, string.Empty) : r.name;
                })
                .Select(t => (Izena: t.Key, Tri: t.Sum(EbidentziaKapturak.Triangeluak), Kop: t.Count()))
                .OrderByDescending(t => t.Tri)
                .Take(12)
                .Select(t => $"{t.Izena}: {t.Tri} triangelu, {t.Kop} renderer")
                .ToList();
            Light[] argiak = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            int piztuta = argiak.Count(a => a.enabled);
            int itzalak = argiak.Count(a => a.enabled && a.shadows != LightShadows.None);

            string laburpena = $"{ProtoGelak.Length} sala, {ProtoLoturak.Length} pasillo, {bucleak} bucle, {urratsakGuztira} urrats, {ihesak} ihes, {propak} prop; {rendererrak.Length} renderer, {triangeluak} triangelu, {piztuta} argi piztuta, {itzalak} itzalekin";
            informea.Lerroa($"- **Laburpena:** {laburpena}");
            informea.Atala("Ibilbideak", ibilbideak);
            informea.Atala("Triangeluak prefab-en arabera", prefabKostua);
            informea.Atala("HUTSAK", hutsak);
            if (hutsak.Count > 0)
            {
                informea.AmaituErrorearekin($"{hutsak.Count} huts; {laburpena}");
                return false;
            }

            informea.AmaituOndo($"0 huts; {laburpena}");
            return true;
        }

        private static float ProtoGelaxka()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{MagixBoxKarpeta}/{Horma}.prefab");
            Modulua horma = NeurtuGuztiak(new[] { prefab }, true)[0];
            return horma.Kaxa.size[LuzeraArdatza(horma)];
        }

        private static List<(string Izena, Vector3 Begia, Vector3 Helburua)> ProtoKamerak()
        {
            float c = ProtoGelaxka();
            Vector3 P(float x, float y, float z) => new Vector3(x * c, y, z * c);
            return new List<(string, Vector3, Vector3)>
            {
                ("P_Hasiera_atera", P(0.8f, 1.65f, 7.5f), P(5f, 1.5f, 7.5f)),
                ("P_Korridorea_luzea", P(3.6f, 1.65f, 7.5f), P(9f, 1.5f, 7.5f)),
                ("P_J1_gurutzea", P(9.5f, 1.65f, 7.2f), P(9.5f, 1.5f, 12f)),
                ("P_J2_T", P(9.5f, 1.65f, 11.5f), P(13f, 1.5f, 11.5f)),
                ("P_Borroka_sarrera", P(13.3f, 1.65f, 11.5f), P(18f, 1.5f, 10.5f)),
                ("P_Borroka_erdia", P(15.5f, 1.65f, 9.2f), P(15.5f, 1.5f, 14f)),
                ("P_Biltegia", P(13.4f, 1.65f, 2.5f), P(14.8f, 1.2f, 4.8f)),
                ("P_Pasillo_iluna", P(11f, 1.65f, 17.5f), P(15f, 1.5f, 17.5f)),
                ("P_Itsua", P(8.2f, 1.65f, 3.5f), P(5f, 1.5f, 3.5f)),
                ("P_Teknikoa", P(8.5f, 1.65f, 21.5f), P(10.8f, 1.2f, 22.8f)),
            };
        }

        // ---------- Datuak ----------

        private sealed class ProtoGela
        {
            public readonly string Izena;
            public readonly int X0;
            public readonly int Z0;
            public readonly int W;
            public readonly int D;
            public readonly string Seinalea;
            public readonly Color Kolorea;
            public readonly float Intentsitatea;

            public ProtoGela(string izena, int x0, int z0, int w, int d, string seinalea, Color kolorea, float intentsitatea)
            {
                Izena = izena;
                X0 = x0;
                Z0 = z0;
                W = w;
                D = d;
                Seinalea = seinalea;
                Kolorea = kolorea;
                Intentsitatea = intentsitatea;
            }
        }

        private sealed class ProtoLotura
        {
            public readonly string A;
            public readonly Aldea AldeA;
            public readonly int Gurutzea;
            public readonly string B;
            public readonly int Mutur;
            public readonly bool AteaA;
            public readonly bool AteaB;
            public readonly HodiArgia Argia;
            public readonly Color Kolorea;

            public ProtoLotura(string a, Aldea alde, int gurutzea, string b, bool ateaA, bool ateaB, HodiArgia argia, Color kolorea)
            {
                A = a;
                AldeA = alde;
                Gurutzea = gurutzea;
                B = b;
                AteaA = ateaA;
                AteaB = ateaB;
                Argia = argia;
                Kolorea = kolorea;
            }

            public ProtoLotura(string a, Aldea alde, int gurutzea, int mutur, bool ateaA, HodiArgia argia, Color kolorea)
            {
                A = a;
                AldeA = alde;
                Gurutzea = gurutzea;
                Mutur = mutur;
                AteaA = ateaA;
                Argia = argia;
                Kolorea = kolorea;
            }
        }

        private sealed class ProtoProp
        {
            public readonly string Sala;
            public readonly string Karpeta;
            public readonly string Prefab;
            public readonly float U;
            public readonly float V;
            public readonly float Yaw;
            public readonly bool KolisioaBehar;

            public ProtoProp(string sala, string karpeta, string prefab, float u, float v, float yaw, bool kolisioaBehar)
            {
                Sala = sala;
                Karpeta = karpeta;
                Prefab = prefab;
                U = u;
                V = v;
                Yaw = yaw;
                KolisioaBehar = kolisioaBehar;
            }
        }

        private sealed class ProtoArgi
        {
            public string Izena;
            public Vector3 Posizioa;
            public Color Kolorea;
            public float Intentsitatea;
            public float Tartea;
            public bool Pultsua;
        }
    }
}
