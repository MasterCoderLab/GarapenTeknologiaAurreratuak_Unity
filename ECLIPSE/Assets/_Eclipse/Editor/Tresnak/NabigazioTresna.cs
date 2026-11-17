using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Borroka;
using Eclipse.Misioak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3: misioen NavMesh-a, aktibo bereizi gisa (Assets/Resources/Nabigazioa/NM_&lt;eszena&gt;.asset). Eszena irekitzen da baina EZ DA GORDETZEN:
    /// misioak, argiztapena eta lightmap-ak ez dira ukitzen. Iturriak: kolisio fisikoak, trigger-ak (Gela_*, terminalak, irteera) eta
    /// Ignore Raycast geruza baztertuta. Egiaztapena: spawn-ean eta etsai-markatzaile guztietan NavMesh-a dago, eta markatzaile
    /// bakoitzetik spawn-erako bide osoa. Aurrebista lehenetsia; -eclipseAplikatu-rekin gordetzen da. Idenpotentea (sinadura).
    /// Misio-demoaren menua ere hemen dago (EditorPrefs; build-etan ez du eraginik).
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string NabMarka = "ECL_NABIGAZIOTRESNA";
        private const string NabKarpeta = "Assets/Resources/Nabigazioa";

        [MenuItem("ECLIPSE/Borroka/Misio-demoa: aktibatu (Editor)")]
        private static void DemoaAktibatu()
        {
            EditorPrefs.SetBool(BorrokaDemoa.EditorGakoa, true);
            Debug.Log($"{NabMarka}_OK Misio-demoa AKTIBO: hurrengo Play-ean M2 eta M3-k armak eta etsaiak izango dituzte (eszenak aldatu gabe).");
        }

        [MenuItem("ECLIPSE/Borroka/Misio-demoa: desaktibatu")]
        private static void DemoaDesaktibatu()
        {
            EditorPrefs.SetBool(BorrokaDemoa.EditorGakoa, false);
            Debug.Log($"{NabMarka}_OK Misio-demoa desaktibatuta: misioak A2-n bezala.");
        }

        [MenuItem("ECLIPSE/Borroka/Misio-demoa: aktibatu (Editor)", true)]
        private static bool DemoaAktibatuBalidatu() => !EditorPrefs.GetBool(BorrokaDemoa.EditorGakoa, false);

        [MenuItem("ECLIPSE/Borroka/Misio-demoa: desaktibatu", true)]
        private static bool DemoaDesaktibatuBalidatu() => EditorPrefs.GetBool(BorrokaDemoa.EditorGakoa, false);

        /// <summary>Batchmode: hiru misioak. Aurrebista lehenetsia; -eclipseAplikatu gordetzeko.</summary>
        public static void BatchNavMeshMisioak()
        {
            bool aplikatu = Environment.GetCommandLineArgs().Contains("-eclipseAplikatu");
            int hutsak = 0;
            foreach (MailaDef d in new[] { Mision01(), Mision02(), Mision03() })
            {
                var lerroak = new List<string>();
                var informea = new Informea("NabigazioTresna", NabMarka, $"{d.Izena}: NavMesh-a ({(aplikatu ? "Aplikatu" : "Aurrebista")})", d.Bidea, "Mision osoa");
                try
                {
                    bool ondo = MisioNavMesh(d, aplikatu, lerroak);
                    hutsak += ondo ? 0 : 1;
                    informea.Atala("Emaitzak", lerroak);
                    if (ondo)
                    {
                        informea.AmaituOndo(lerroak.FirstOrDefault(l => l.StartsWith("NavMesh", StringComparison.Ordinal)) ?? "ondo");
                    }
                    else
                    {
                        informea.AmaituErrorearekin("egiaztapenak huts");
                    }
                }
                catch (Exception salbuespena)
                {
                    hutsak++;
                    informea.Atala("Emaitzak", lerroak);
                    informea.AmaituErrorearekin(salbuespena.ToString());
                }
            }

            EditorApplication.Exit(hutsak == 0 ? 0 : 1);
        }

        /// <summary>
        /// Irekitako eszenaren NavMesh-a: kolisio fisikoak (trigger-ak eta Ignore Raycast baztertuta), etsaiaren neurriekin
        /// (0,35 × 1,8 m; Humanoid-en 0,5 m-k ateak ixten ditu). Proiektuko agente-motak ez dira aldatzen. AtrezzoTresna-k ere erabiltzen du.
        /// </summary>
        internal static NavMeshData EszenaNavMesh(List<string> lerroak)
        {
            var iturriak = new List<NavMeshBuildSource>();
            Bounds mugak = default;
            bool lehena = true;
            foreach (Collider k in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (lehena)
                {
                    mugak = k.bounds;
                    lehena = false;
                }
                else
                {
                    mugak.Encapsulate(k.bounds);
                }
            }

            mugak.Expand(4f);
            NavMeshBuilder.CollectSources(mugak, ~(1 << 2), NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), iturriak);
            int guztira = iturriak.Count;
            iturriak.RemoveAll(i => i.component is Collider c && c.isTrigger);
            NavMeshBuildSettings ezarpenak = NavMesh.GetSettingsByID(0);
            ezarpenak.agentRadius = 0.35f;
            ezarpenak.agentHeight = 1.8f;
            ezarpenak.agentClimb = 0.4f;
            ezarpenak.overrideVoxelSize = true;
            ezarpenak.voxelSize = 0.1f;
            lerroak?.Add($"Iturriak: {iturriak.Count} kolisio fisiko ({guztira - iturriak.Count} trigger baztertuta); agentea 0,35 × 1,8 m, voxel 0,1 m");
            return NavMeshBuilder.BuildNavMeshData(ezarpenak, iturriak, mugak, Vector3.zero, Quaternion.identity);
        }

        private static bool MisioNavMesh(MailaDef d, bool aplikatu, List<string> lerroak)
        {
            Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
            NavMeshData datuak = EszenaNavMesh(lerroak);
            NavMeshDataInstance instantzia = NavMesh.AddNavMeshData(datuak);
            try
            {
                NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                lerroak.Add($"NavMesh: {tri.indices.Length / 3} triangelu, {tri.vertices.Length} erpin");

                LevelManager km = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                Vector3 spawnPuntua = km != null ? km.SpawnPuntua : Vector3.zero;
                bool spawnOndo = NavMesh.SamplePosition(spawnPuntua, out NavMeshHit spawnJo, 1f, NavMesh.AllAreas);
                lerroak.Add($"{(spawnOndo ? "ONDO" : "HUTS")}: spawn-ean NavMesh {spawnPuntua}");

                MailaMarka[] markak = UnityEngine.Object.FindObjectsByType<MailaMarka>(FindObjectsInactive.Exclude).Where(m => m.Mota == MarkaMota.Etsaia).ToArray();
                int bideak = 0;
                foreach (MailaMarka m in markak)
                {
                    bool laginda = NavMesh.SamplePosition(m.transform.position, out NavMeshHit jo, 1.5f, NavMesh.AllAreas);
                    var bidea = new NavMeshPath();
                    bool osoa = laginda && spawnOndo && NavMesh.CalculatePath(jo.position, spawnJo.position, NavMesh.AllAreas, bidea) && bidea.status == NavMeshPathStatus.PathComplete;
                    bideak += osoa ? 1 : 0;
                    lerroak.Add($"{(laginda && osoa ? "ONDO" : "HUTS")}: {m.name} {m.transform.position}: NavMesh {laginda}, spawn-erako bidea {(osoa ? "osoa" : bidea.status.ToString())}");
                }

                lerroak.Add($"DATUA: {markak.Length} etsai-markatzaile, {bideak} bide oso");
                string bidea2 = $"{NabKarpeta}/NM_{d.Izena}.asset";
                var dagoena = AssetDatabase.LoadAssetAtPath<NavMeshData>(bidea2);
                // Idenpotentzia: gordetako NavMesh-aren triangulazioa berriarekin alderatu.
                string sinadura = $"{tri.indices.Length}:{tri.vertices.Length}";
                string zaharra = null;
                if (dagoena != null)
                {
                    NavMesh.RemoveNavMeshData(instantzia);
                    NavMeshDataInstance z = NavMesh.AddNavMeshData(dagoena);
                    NavMeshTriangulation zt = NavMesh.CalculateTriangulation();
                    zaharra = $"{zt.indices.Length}:{zt.vertices.Length}";
                    NavMesh.RemoveNavMeshData(z);
                    instantzia = NavMesh.AddNavMeshData(datuak);
                }

                if (zaharra == sinadura)
                {
                    lerroak.Add($"0 aldaketa: {bidea2} bat dator");
                }
                else if (aplikatu)
                {
                    if (!AssetDatabase.IsValidFolder(NabKarpeta))
                    {
                        AssetDatabase.CreateFolder("Assets/Resources", "Nabigazioa");
                    }

                    if (dagoena != null)
                    {
                        AssetDatabase.DeleteAsset(bidea2);
                    }

                    AssetDatabase.CreateAsset(datuak, bidea2);
                    AssetDatabase.SaveAssets();
                    lerroak.Add($"Gordeta: {bidea2}");
                }
                else
                {
                    lerroak.Add($"Aplikatuko litzateke: {bidea2}");
                }

                return spawnOndo && bideak == markak.Length;
            }
            finally
            {
                NavMesh.RemoveNavMeshData(instantzia);
                if (eszena.isDirty)
                {
                    lerroak.Add("OHARRA: eszena zikin markatu da (ez da gordetzen)");
                }
            }
        }
    }
}
