using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Atrezzo;
using Eclipse.Misioak;
using Eclipse.Npc;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A4: misio bakoitzaren eduki dinamikoa (Resources/Misioak/ME_&lt;eszena&gt;.asset) sortu edo eguneratu. Aurrebista / Aplikatu; idenpotentea.
    /// Narratiba (Ion, 14/11): M1 zientzialariak eta soldaduak, borrokarik ez; M2 soldaduak eta lehen munstroa (2 etsai), EMP pistola;
    /// M3 munstro guztiak eta amaieran 07_AmaieraZinematika. NPC-en kokapenak eszenatik kalkulatzen dira (lan-lekuak kontsolen ondoan,
    /// patruilak gelen artean) eta misioaren NavMesh-arekin egiaztatzen (bide osoak, interes-puntuetatik urrun). Eszenak ez dira gordetzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string MeTresna = "MisioEdukiTresna";
        private const string MeMarka = "ECL_MISIOEDUKITRESNA";
        private const string MeKarpeta = "Assets/Resources/" + MisioEdukia.Karpeta;
        internal const string AmaieraEszenaIzena = "07_AmaieraZinematika";

        private sealed class NpcPlana
        {
            public NpcRola Rola;
            public string[] LanGelak = Array.Empty<string>();
            public string[][] PatruilaGelak = Array.Empty<string[]>();
        }

        private sealed class MisioEdukiPlana
        {
            public bool Atrezzoa = true;
            public bool Borroka;
            public int EtsaiKopurua = -1;
            public string AmaieraEszena;
            public NpcPlana[] Npcak = Array.Empty<NpcPlana>();
        }

        /// <summary>Narratiba misioka. Gelen izenak MailaDatuak-ekoak dira (Gela_&lt;izena&gt; kolisionatzaileak); lehenengo baliozkoa hartzen da.</summary>
        private static MisioEdukiPlana EdukiPlana(string eszena) => eszena switch
        {
            "Mision01_Laborategia" => new MisioEdukiPlana
            {
                Borroka = false,
                Npcak = new[]
                {
                    new NpcPlana { Rola = NpcRola.Zientzialaria, LanGelak = new[] { "Lab1", "Harrera" } },
                    new NpcPlana { Rola = NpcRola.Zientzialaria, LanGelak = new[] { "Biologia", "Zerbitzariak", "Kontrola", "Harrera", "Lab1" } },
                    new NpcPlana { Rola = NpcRola.Zientzialaria, LanGelak = new[] { "LabNagusia", "Zerbitzariak" } },
                    new NpcPlana { Rola = NpcRola.Soldadua, PatruilaGelak = new[] { new[] { "Harrera", "Lab1" }, new[] { "Harrera", "J1" } } },
                    new NpcPlana { Rola = NpcRola.Soldadua, PatruilaGelak = new[] { new[] { "Borroka", "LabNagusia" }, new[] { "Borroka", "Kontrola" } } },
                },
            },
            "Mision02_Energia" => new MisioEdukiPlana
            {
                Borroka = true, EtsaiKopurua = 2,
                Npcak = new[]
                {
                    new NpcPlana { Rola = NpcRola.Soldadua, PatruilaGelak = new[] { new[] { "J1", "Biltegia" }, new[] { "J1", "Sorgailuak" }, new[] { "Biltegia", "J2" } } },
                    new NpcPlana { Rola = NpcRola.Soldadua, PatruilaGelak = new[] { new[] { "Kontrola", "Osagaiak" }, new[] { "Kontrola", "J2" } } },
                },
            },
            _ => new MisioEdukiPlana { Borroka = true, EtsaiKopurua = -1, AmaieraEszena = AmaieraEszenaIzena },
        };

        [MenuItem("ECLIPSE/Misioak/Edukia: aurrebista")]
        private static void MeMenuAurrebista() => MisioEdukiExekutatu(false);

        [MenuItem("ECLIPSE/Misioak/Edukia: aplikatu")]
        private static void MeMenuAplikatu()
        {
            if (EditorUtility.DisplayDialog(MeTresna, "Misioen edukia (Resources/Misioak/ME_*) sortu edo eguneratu? Eszenak ez dira gordetzen.", "Aplikatu", "Utzi"))
            {
                MisioEdukiExekutatu(true);
            }
        }

        public static void BatchMisioEdukia()
        {
            EditorApplication.Exit(MisioEdukiExekutatu(Environment.GetCommandLineArgs().Contains("-eclipseAplikatu")) ? 0 : 1);
        }

        private static bool MisioEdukiExekutatu(bool aplikatu)
        {
            var informea = new Informea(MeTresna, MeMarka, aplikatu ? "Aplikatu" : "Aurrebista", MeKarpeta, "Misioen edukia (A4)");
            var aldaketak = new List<string>();
            var lerroak = new List<string>();
            string aurrekoa = SceneManager.GetActiveScene().path;
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException("Irekitako eszena gorde gabe dago.");
                }

                if (aplikatu)
                {
                    AtrezzoPiezak.KarpetaZiurtatu(MeKarpeta);
                }

                foreach (Func<MailaDef> sortzailea in Mailak.Values)
                {
                    MailaDef d = sortzailea();
                    lerroak.Add($"## {d.Izena}");
                    MisioEdukia berria = MisioEdukiaKalkulatu(d, lerroak);
                    string bidea = $"{MeKarpeta}/ME_{d.Izena}.asset";
                    var dagoena = AssetDatabase.LoadAssetAtPath<MisioEdukia>(bidea);
                    if (dagoena != null && dagoena.Sinadura() == berria.Sinadura())
                    {
                        lerroak.Add($"- 0 aldaketa: {bidea}");
                        UnityEngine.Object.DestroyImmediate(berria);
                        continue;
                    }

                    aldaketak.Add($"{(dagoena == null ? "Berria" : "Eguneratua")}: {bidea} ({berria.Npcak.Count} NPC, borroka {berria.BorrokaAktibo}, etsaiak {berria.EtsaiKopurua}, amaiera '{berria.AmaieraEszena}')");
                    if (!aplikatu)
                    {
                        UnityEngine.Object.DestroyImmediate(berria);
                        continue;
                    }

                    if (dagoena == null)
                    {
                        berria.hideFlags = HideFlags.None;
                        AssetDatabase.CreateAsset(berria, bidea);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(berria, dagoena);
                        EditorUtility.SetDirty(dagoena);
                        UnityEngine.Object.DestroyImmediate(berria);
                    }
                }

                if (aplikatu)
                {
                    AssetDatabase.SaveAssets();
                }

                informea.Atala(aplikatu ? "Aplikatutako aldaketak" : "Aplikatuko liratekeen aldaketak", aldaketak);
                informea.Atala("Xehetasunak", lerroak);
                int hutsak = lerroak.Count(l => l.StartsWith("- HUTS", StringComparison.Ordinal));
                if (hutsak > 0)
                {
                    informea.AmaituErrorearekin($"{hutsak} huts kokapenetan");
                    return false;
                }

                informea.AmaituOndo($"{aldaketak.Count} aldaketa");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.Atala("Xehetasunak", lerroak);
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(aurrekoa) && SceneManager.GetActiveScene().path != aurrekoa)
                {
                    EditorSceneManager.OpenScene(aurrekoa, OpenSceneMode.Single);
                }
            }
        }

        /// <summary>Edukia kalkulatu eta ScriptableObject-a eszena aldaketen ONDOREN sortu (OpenScene-k eszenako objektu solteak suntsitzen ditu).</summary>
        private static MisioEdukia MisioEdukiaKalkulatu(MailaDef d, List<string> lerroak)
        {
            MisioEdukiPlana plana = EdukiPlana(d.Izena);
            var npcak = new List<MisioEdukia.NpcKokapena>();
            if (plana.Npcak.Length > 0)
            {
                EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                Physics.SyncTransforms();
                var navMesh = AssetDatabase.LoadAssetAtPath<NavMeshData>($"Assets/Resources/Nabigazioa/NM_{d.Izena}.asset");
                if (navMesh == null)
                {
                    lerroak.Add($"- HUTS: NavMesh-a falta da (NabigazioTresna aplikatu): Assets/Resources/Nabigazioa/NM_{d.Izena}.asset");
                }
                else
                {
                    NavMeshDataInstance ins = NavMesh.AddNavMeshData(navMesh);
                    try
                    {
                        npcak = NpcKokapenak(d, plana, lerroak);
                    }
                    finally
                    {
                        NavMesh.RemoveNavMeshData(ins);
                    }
                }
            }

            var edukia = ScriptableObject.CreateInstance<MisioEdukia>();
            edukia.hideFlags = HideFlags.DontSaveInEditor;
            edukia.Ezarri(d.Izena, plana.Atrezzoa, plana.Borroka, plana.EtsaiKopurua, "Borroka/PF_Etsaia_Proba", npcak, plana.AmaieraEszena);
            return edukia;
        }

        private static List<MisioEdukia.NpcKokapena> NpcKokapenak(MailaDef d, MisioEdukiPlana plana, List<string> lerroak)
        {
            var emaitza = new List<MisioEdukia.NpcKokapena>();
            GameObject erroa = SceneManager.GetActiveScene().GetRootGameObjects().First(o => o.name == d.Erroa);
            Dictionary<string, BoxCollider> gelak = erroa.GetComponentsInChildren<BoxCollider>(true)
                .Where(k => k.isTrigger && k.name.StartsWith("Gela_", StringComparison.Ordinal))
                .GroupBy(k => k.name.Substring(5)).ToDictionary(g => g.Key, g => g.First());
            LevelManager km = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            Vector3 spawn = km != null ? km.SpawnPuntua : Vector3.zero;

            // Interes-puntuak: hauetatik urrun (horizontalean).
            var poiak = new List<(Vector3 P, float Muga, string Izena)> { (spawn, 3f, "spawn") };
            poiak.AddRange(UnityEngine.Object.FindObjectsByType<MisioTerminala>(FindObjectsInactive.Include).Select(t => (t.transform.position, 2f, t.name)));
            poiak.AddRange(UnityEngine.Object.FindObjectsByType<MisioBilduma>(FindObjectsInactive.Include).Select(t => (t.transform.position, 1.5f, t.name)));
            poiak.AddRange(UnityEngine.Object.FindObjectsByType<BlokeoAtea>(FindObjectsInactive.Include).Select(t => (t.transform.position, 2.6f, t.name)));
            poiak.AddRange(erroa.GetComponentsInChildren<Transform>(true).Where(t => t.name.IndexOf("Door", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(t => (t.position, 2f, t.name)));
            var hartuak = new List<Vector3>();

            bool Librea(Vector3 p, out string arrazoia)
            {
                foreach ((Vector3 q, float muga, string izena) in poiak)
                {
                    if (Mathf.Abs(q.y - p.y) < 2.5f && Horiz(p, q) < muga)
                    {
                        arrazoia = izena;
                        return false;
                    }
                }

                if (hartuak.Any(h => Horiz(h, p) < 1.2f))
                {
                    arrazoia = "beste NPC bat";
                    return false;
                }

                arrazoia = null;
                return true;
            }

            bool Lagina(Vector3 p, float erradioa, out Vector3 emaitzaP)
            {
                bool ondo = NavMesh.SamplePosition(p, out NavMeshHit jo, erradioa, NavMesh.AllAreas);
                emaitzaP = ondo ? jo.position : p;
                return ondo;
            }

            bool BideOsoa(Vector3 a, Vector3 b)
            {
                var bidea = new NavMeshPath();
                return NavMesh.CalculatePath(a, b, NavMesh.AllAreas, bidea) && bidea.status == NavMeshPathStatus.PathComplete;
            }

            Vector3 GelaZorua(BoxCollider g)
            {
                Vector3 c = g.bounds.center;
                return Physics.Raycast(new Vector3(c.x, g.bounds.max.y - 0.1f, c.z), Vector3.down, out RaycastHit lurra, g.bounds.size.y + 1f, ~(1 << 2), QueryTriggerInteraction.Ignore)
                    ? lurra.point : new Vector3(c.x, g.bounds.min.y, c.z);
            }

            Transform[] propak = erroa.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.IndexOf("Old_computer", StringComparison.Ordinal) >= 0 || t.name.IndexOf("Rack", StringComparison.Ordinal) >= 0).ToArray();

            foreach (NpcPlana np in plana.Npcak)
            {
                MisioEdukia.NpcKokapena kokapena = null;
                string azkenArrazoia = "gela ezezaguna";
                if (np.Rola == NpcRola.Zientzialaria)
                {
                    foreach (string gelaIzena in np.LanGelak)
                    {
                        if (!gelak.TryGetValue(gelaIzena, out BoxCollider gela))
                        {
                            continue;
                        }

                        Vector3 erdia = GelaZorua(gela);
                        // Hautagaiak: gelako kontsola/apal bakoitzaren aurrean (erdirantz 1 m), gero gelaren erdia.
                        var hautagaiak = propak.Where(p => gela.bounds.Contains(p.position + Vector3.up * 0.5f))
                            .Select(p => (Lekua: p.position + (erdia - p.position).normalized * 1.0f, Begira: p.position)).ToList();
                        hautagaiak.Add((erdia, erdia + gela.transform.forward));
                        foreach ((Vector3 lekua, Vector3 begira) in hautagaiak)
                        {
                            if (!Lagina(new Vector3(lekua.x, erdia.y, lekua.z), 1.5f, out Vector3 p))
                            {
                                azkenArrazoia = $"{gelaIzena}: NavMesh-ik ez {V(lekua)}";
                                continue;
                            }

                            if (!Librea(p, out string arrazoia))
                            {
                                azkenArrazoia = $"{gelaIzena}: {arrazoia}";
                                continue;
                            }

                            if (!BideOsoa(spawn, p))
                            {
                                azkenArrazoia = $"{gelaIzena}: biderik ez spawn-etik {V(p)}";
                                continue;
                            }

                            float yaw = Quaternion.LookRotation(new Vector3(begira.x - p.x, 0f, begira.z - p.z).normalized).eulerAngles.y;
                            kokapena = new MisioEdukia.NpcKokapena { Rola = np.Rola, Lekua = p, Yaw = yaw, LanLekuaDu = true, LanLekua = p, LanYaw = yaw };
                            lerroak.Add($"- ONDO: {np.Rola} {gelaIzena}-n lanean {V(p)}");
                            break;
                        }

                        if (kokapena != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foreach (string[] bikotea in np.PatruilaGelak)
                    {
                        if (bikotea.Length < 2 || !gelak.TryGetValue(bikotea[0], out BoxCollider ga) || !gelak.TryGetValue(bikotea[1], out BoxCollider gb))
                        {
                            continue;
                        }

                        Vector3 ea = GelaZorua(ga), eb = GelaZorua(gb);
                        // Erditik pixka bat alboratuta, terminalen eta ateen gainean ez geratzeko.
                        Vector3[] ak = { ea, ea + new Vector3(-0.9f, 0f, 0.9f), ea + new Vector3(0.9f, 0f, -0.9f) };
                        Vector3[] bk = { eb, eb + new Vector3(0.9f, 0f, 0.9f), eb + new Vector3(-0.9f, 0f, -0.9f) };
                        foreach (Vector3 a0 in ak)
                        {
                            foreach (Vector3 b0 in bk)
                            {
                                if (!Lagina(a0, 2.5f, out Vector3 a) || !Lagina(b0, 2.5f, out Vector3 b))
                                {
                                    azkenArrazoia = $"{bikotea[0]}/{bikotea[1]}: NavMesh-ik ez";
                                    continue;
                                }

                                string arrazoiB = null;
                                if (!Librea(a, out string arrazoiA) || !Librea(b, out arrazoiB))
                                {
                                    azkenArrazoia = $"{bikotea[0]}/{bikotea[1]}: {arrazoiA ?? arrazoiB} ({V(a)} / {V(b)})";
                                    continue;
                                }

                                if (!BideOsoa(spawn, a) || !BideOsoa(a, b) || Horiz(a, b) < 3f)
                                {
                                    azkenArrazoia = $"{bikotea[0]}/{bikotea[1]}: biderik ez edo laburregia";
                                    continue;
                                }

                                {
                                    float yaw = Quaternion.LookRotation(new Vector3(b.x - a.x, 0f, b.z - a.z).normalized).eulerAngles.y;
                                    kokapena = new MisioEdukia.NpcKokapena { Rola = np.Rola, Lekua = a, Yaw = yaw, Patruila = new[] { a, b } };
                                    hartuak.Add(b);
                                    lerroak.Add($"- ONDO: {np.Rola} patruilan {bikotea[0]} ↔ {bikotea[1]} ({Horiz(a, b):0.0} m)");
                                    break;
                                }
                            }

                            if (kokapena != null)
                            {
                                break;
                            }
                        }

                        if (kokapena != null)
                        {
                            break;
                        }
                    }
                }

                if (kokapena == null)
                {
                    lerroak.Add($"- HUTS: {np.Rola} ez da kokatu ({azkenArrazoia}); gelak: {string.Join("/", np.LanGelak.Concat(np.PatruilaGelak.SelectMany(x => x)))}");
                    continue;
                }

                hartuak.Add(kokapena.Lekua);
                emaitza.Add(kokapena);
            }

            return emaitza;
        }

        private static float Horiz(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
