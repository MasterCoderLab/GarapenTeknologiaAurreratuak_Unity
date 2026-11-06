using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Sektore baten geometria bake-erako prestatzen du: flag estatikoak (Undo bakarrean) eta,
    /// ekintza bereizi batean, "Generate Lightmap UVs" UV2rik ez duten modeloetan.
    /// </summary>
    internal static class ArgiztapenPreseta
    {
        private const string Tresna = "ArgiztapenPreseta";
        private const string Marka = "ECL_ARGIZTAPENPRESETA";
        private const string Menua = "ECLIPSE/Argiztapena/ArgiztapenPreseta: ";

        /// <summary>
        /// ContributeGI lightmap-erako; ReflectionProbeStatic behar da, bestela Reflection Probe
        /// horneatuak ez du geometria hori harrapatzen.
        /// </summary>
        private const StaticEditorFlags BeharrezkoFlagak =
            StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;

        /// <summary>Din_* edo kit-eko atea (Door, Doors, "Door (1)"...). DoorFrame eta WallDoor ez dira sartzen.</summary>
        private static readonly Regex DinamikoIzena = new Regex(@"^(Din_.*|Doors?(\s*\(\d+\))?)$");

        private sealed class Analisia
        {
            public readonly List<GameObject> Aldatzekoak = new List<GameObject>();
            public readonly List<string> AldatzekoBideak = new List<string>();
            public readonly List<string> Dinamikoak = new List<string>();
            public readonly List<string> Oharrak = new List<string>();

            /// <summary>Modeloaren bidea → UV2rik gabeko mesh-en izenak.</summary>
            public readonly SortedDictionary<string, SortedSet<string>> UV2Modeloak =
                new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

            public int RendererKopurua;
        }

        [MenuItem(Menua + "Aurrebista")]
        private static void Aurrebista()
        {
            if (!TresnaLaguntzailea.LortuSektoreErroa(out GameObject erroa, out string errorea))
            {
                Debug.LogError($"{Marka}_ERROR {errorea}");
                return;
            }

            AurrebistaErroan(erroa);
        }

        internal static bool AurrebistaErroan(GameObject erroa)
        {
            Analisia analisia = Analizatu(erroa);
            var informea = new Informea(Tresna, Marka, "Aurrebista", erroa);
            InformeaBete(informea, analisia, "Aldatuko diren objektuak");
            informea.AmaituOndo($"{erroa.name}: {analisia.Aldatzekoak.Count} aldaketa flag-etan, {analisia.UV2Modeloak.Count} modelo UV2rik gabe (ez da ezer aldatu)");
            return true;
        }

        [MenuItem(Menua + "Aplikatu flagak")]
        private static void AplikatuFlagak()
        {
            if (!TresnaLaguntzailea.LortuSektoreErroa(out GameObject erroa, out string errorea))
            {
                Debug.LogError($"{Marka}_ERROR {errorea}");
                return;
            }

            FlagakErroan(erroa);
        }

        internal static bool FlagakErroan(GameObject erroa)
        {
            Analisia analisia = Analizatu(erroa);
            var informea = new Informea(Tresna, Marka, "Aplikatu flagak", erroa);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("ECLIPSE ArgiztapenPreseta");
            int taldea = Undo.GetCurrentGroup();
            try
            {
                foreach (GameObject objektua in analisia.Aldatzekoak)
                {
                    Undo.RecordObject(objektua, Tresna);
                    StaticEditorFlags flagak = GameObjectUtility.GetStaticEditorFlags(objektua);
                    GameObjectUtility.SetStaticEditorFlags(objektua, flagak | BeharrezkoFlagak);
                    if (PrefabUtility.IsPartOfPrefabInstance(objektua))
                    {
                        // Override-a eszenan geratzen da; ThirdParty-ko prefab-a ez da ukitzen.
                        PrefabUtility.RecordPrefabInstancePropertyModifications(objektua);
                    }
                }

                Undo.CollapseUndoOperations(taldea);
            }
            catch (Exception salbuespena)
            {
                Undo.RevertAllDownToGroup(taldea);
                InformeaBete(informea, analisia, "Aldatu nahi ziren objektuak (desegin dira)");
                informea.AmaituErrorearekin(salbuespena.Message);
                return false;
            }

            if (analisia.Aldatzekoak.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(erroa.scene);
            }

            InformeaBete(informea, analisia, "Aldatutako objektuak");
            informea.AmaituOndo($"{erroa.name}: {analisia.Aldatzekoak.Count} aldaketa flag-etan");
            return true;
        }

        [MenuItem(Menua + "Aktibatu UV2 (.meta aldatzen du)")]
        private static void AktibatuUV2()
        {
            if (!TresnaLaguntzailea.LortuSektoreErroa(out GameObject erroa, out string errorea))
            {
                Debug.LogError($"{Marka}_ERROR {errorea}");
                return;
            }

            if (Application.isBatchMode)
            {
                Debug.LogError($"{Marka}_ERROR UV2 aktibatzeak Ion-en baieztapena behar du: ezin da batchmode-n exekutatu.");
                return;
            }

            UV2Erroan(erroa, false);
        }

        /// <param name="baimenduta">True: Ion-ek komando-lerroan baimendu du (batch); false: elkarrizketa erakutsi.</param>
        internal static bool UV2Erroan(GameObject erroa, bool baimenduta)
        {
            Analisia analisia = Analizatu(erroa);
            var informea = new Informea(Tresna, Marka, "Aktibatu UV2", erroa);
            List<string> modeloak = analisia.UV2Modeloak.Keys.ToList();

            if (modeloak.Count == 0)
            {
                informea.AmaituOndo($"{erroa.name}: 0 aldaketa: ez dago UV2 behar duen modelorik");
                return true;
            }

            if (!baimenduta && !Baieztatu(modeloak))
            {
                informea.Atala("UV2 behar duten modeloak (ez da ezer aldatu)", UV2Lerroak(analisia));
                informea.AmaituOndo("Utzita: 0 aldaketa");
                return true;
            }

            var aldatuak = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string bidea in modeloak)
                    {
                        var inportatzailea = (ModelImporter)AssetImporter.GetAtPath(bidea);
                        inportatzailea.generateSecondaryUV = true;
                        inportatzailea.SaveAndReimport();
                        aldatuak.Add(bidea);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
            catch (Exception salbuespena)
            {
                informea.Atala("Aldatutako modeloak (.meta)", aldatuak);
                informea.AmaituErrorearekin(salbuespena.Message);
                return false;
            }

            List<string> hutsak = modeloak.Where(bidea => !UV2Zuzena(bidea)).ToList();
            informea.Atala("Aldatutako modeloak (.meta, Ctrl+Z gabe)", aldatuak);
            informea.Lerroa(string.Empty);
            informea.Lerroa("Bake-a errepikatu behar da. Kit-a jatorrizko paketetik berrezarriz gero, UV2 hauek galtzen dira.");
            if (hutsak.Count > 0)
            {
                informea.Atala("Inportatu ondoren UV2rik gabe", hutsak);
                informea.AmaituErrorearekin($"{hutsak.Count} modeloak ez du UV2rik inportatu ondoren");
                return false;
            }

            informea.AmaituOndo($"{erroa.name}: {aldatuak.Count} modelo aldatuta eta egiaztatuta");
            return true;
        }

        private static Analisia Analizatu(GameObject erroa)
        {
            var analisia = new Analisia();
            Transform erroTransform = erroa.transform;

            foreach (MeshRenderer renderer in erroa.GetComponentsInChildren<MeshRenderer>(true))
            {
                analisia.RendererKopurua++;
                GameObject objektua = renderer.gameObject;
                string bidea = TresnaLaguntzailea.Bidea(objektua.transform, erroTransform);
                StaticEditorFlags flagak = GameObjectUtility.GetStaticEditorFlags(objektua);

                string dinamikoArrazoia = DinamikoArrazoia(objektua.transform, erroTransform);
                if (dinamikoArrazoia != null)
                {
                    analisia.Dinamikoak.Add($"{bidea}: {dinamikoArrazoia}");
                    if ((flagak & BeharrezkoFlagak) != 0)
                    {
                        analisia.Oharrak.Add($"{bidea}: dinamikoa da ({dinamikoArrazoia}) baina flag estatikoak ditu; ez zaizkio kendu, begiratu eskuz.");
                    }

                    continue;
                }

                if ((flagak & BeharrezkoFlagak) != BeharrezkoFlagak)
                {
                    analisia.Aldatzekoak.Add(objektua);
                    analisia.AldatzekoBideak.Add(bidea);
                }

                UV2Aztertu(renderer, bidea, analisia);
            }

            return analisia;
        }

        /// <summary>Objektua edo sektoreko arbaso bat dinamikoa bada, arrazoia; bestela null.</summary>
        private static string DinamikoArrazoia(Transform objektua, Transform erroa)
        {
            for (Transform uneko = objektua; uneko != null; uneko = uneko.parent)
            {
                if (uneko.GetComponent<Rigidbody>() != null)
                {
                    return $"Rigidbody '{uneko.name}'-n";
                }

                if (uneko.GetComponent<Animator>() != null)
                {
                    return $"Animator '{uneko.name}'-n";
                }

                if (uneko.GetComponent<Animation>() != null)
                {
                    return $"Animation '{uneko.name}'-n";
                }

                if (DinamikoIzena.IsMatch(uneko.name))
                {
                    return $"izena '{uneko.name}'";
                }

                if (uneko == erroa)
                {
                    break;
                }
            }

            return null;
        }

        private static void UV2Aztertu(MeshRenderer renderer, string bidea, Analisia analisia)
        {
            var filtroa = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filtroa != null ? filtroa.sharedMesh : null;
            if (mesh == null)
            {
                analisia.Oharrak.Add($"{bidea}: ez du mesh-ik.");
                return;
            }

            if (mesh.HasVertexAttribute(VertexAttribute.TexCoord1))
            {
                return;
            }

            string modeloBidea = AssetDatabase.GetAssetPath(mesh);
            if (!(AssetImporter.GetAtPath(modeloBidea) is ModelImporter inportatzailea))
            {
                analisia.Oharrak.Add($"{bidea}: '{mesh.name}' mesh-ak ez du UV2rik eta ez dator modelo batetik; omitituta.");
                return;
            }

            if (inportatzailea.generateSecondaryUV)
            {
                analisia.Oharrak.Add($"{bidea}: {modeloBidea} modeloak UV2 aktibatuta du baina '{mesh.name}' mesh-ak ez du; inportazioa begiratu.");
                return;
            }

            if (!analisia.UV2Modeloak.TryGetValue(modeloBidea, out SortedSet<string> meshak))
            {
                meshak = new SortedSet<string>(StringComparer.Ordinal);
                analisia.UV2Modeloak.Add(modeloBidea, meshak);
            }

            meshak.Add(mesh.name);
        }

        /// <summary>
        /// Baieztapena lehenetsitako "Utzi"-rekin: Enter (0) eta Esc (1) ez dute ezer aldatzen;
        /// hirugarren botoiak bakarrik aktibatzen du.
        /// </summary>
        private static bool Baieztatu(List<string> modeloak)
        {
            const int Erakutsi = 20;
            string zerrenda = string.Join("\n", modeloak.Take(Erakutsi).Select(Path.GetFileName));
            if (modeloak.Count > Erakutsi)
            {
                zerrenda += $"\n... eta beste {modeloak.Count - Erakutsi} (ikusi aurrebistaren informea)";
            }

            string babeskopia = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ECLIPSE_babeskopia");
            bool babeskopiaDago = Directory.Exists(babeskopia) && Directory.GetFiles(babeskopia, "*.tgz").Length > 0;
            string babesMezua = babeskopiaDago
                ? $"Babeskopia: {babeskopia}"
                : $"KONTUZ: ez dago babeskopiarik hemen: {babeskopia}";

            string mezua = $"\"Generate Lightmap UVs\" aktibatuko da {modeloak.Count} modelotan:\n\n{zerrenda}\n\n"
                           + "ThirdParty-ko .meta fitxategiak aldatzen ditu: Ctrl+Z ez du desegiten eta ez daude Git-en.\n"
                           + $"{babesMezua}\n\nOndoren, bake-a errepikatu behar da.";

            return EditorUtility.DisplayDialogComplex(Tresna, mezua, "Utzi", "Itxi", "Aktibatu UV2") == 2;
        }

        private static bool UV2Zuzena(string modeloBidea)
        {
            var inportatzailea = AssetImporter.GetAtPath(modeloBidea) as ModelImporter;
            return inportatzailea != null
                   && inportatzailea.generateSecondaryUV
                   && AssetDatabase.LoadAllAssetsAtPath(modeloBidea)
                       .OfType<Mesh>()
                       .All(mesh => mesh.HasVertexAttribute(VertexAttribute.TexCoord1));
        }

        private static List<string> UV2Lerroak(Analisia analisia)
        {
            return analisia.UV2Modeloak
                .Select(modeloa => $"{modeloa.Key}: {string.Join(", ", modeloa.Value)}")
                .ToList();
        }

        private static void InformeaBete(Informea informea, Analisia analisia, string aldaketenIzenburua)
        {
            informea.Lerroa($"- **MeshRenderer-ak:** {analisia.RendererKopurua}");
            informea.Lerroa($"- **Aldaketak flag-etan:** {analisia.Aldatzekoak.Count}");
            informea.Lerroa($"- **Flag gehituak:** {BeharrezkoFlagak}");
            informea.Atala(aldaketenIzenburua, analisia.AldatzekoBideak);
            informea.Atala("UV2 behar duten modeloak (ekintza bereizia, .meta aldatzen du)", UV2Lerroak(analisia));
            informea.Atala("Dinamikoak (omitituta)", analisia.Dinamikoak);
            informea.Atala("Oharrak", analisia.Oharrak);
        }
    }
}
