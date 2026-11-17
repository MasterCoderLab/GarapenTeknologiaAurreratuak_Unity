using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Misioak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A4 auditoria (irakurketa hutsa): misio bakoitzeko gela guztien irisgarritasuna NavMesh-ean (Resources/Nabigazioa/NM_*),
    /// spawn-etik. Iristen ez den gelan: bidea non amaitzen den eta gelako NavMesh-a irla bereizia den. Eszenarik ez da gordetzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string NaMarka = "ECL_NAVMESHAUDITORIA";

        public static void BatchNavMeshAuditoria()
        {
            var informea = new Informea("NavMeshAuditoria", NaMarka, "Gelen irisgarritasuna (irakurketa hutsa)", "Mision01–03", "NavMesh");
            var lerroak = new List<string>();
            int iritsiEzinak = 0;
            try
            {
                foreach (MailaDef d in new[] { Mision01(), Mision02(), Mision03() })
                {
                    EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                    Physics.SyncTransforms();
                    var datuak = AssetDatabase.LoadAssetAtPath<NavMeshData>($"Assets/Resources/Nabigazioa/NM_{d.Izena}.asset");
                    if (datuak == null)
                    {
                        lerroak.Add($"HUTS: {d.Izena}: NavMesh-a falta da");
                        iritsiEzinak++;
                        continue;
                    }

                    NavMeshDataInstance ins = NavMesh.AddNavMeshData(datuak);
                    try
                    {
                        LevelManager km = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                        NavMesh.SamplePosition(km.SpawnPuntua, out NavMeshHit hasiera, 1.5f, NavMesh.AllAreas);
                        BoxCollider[] gelak = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude)
                            .Where(k => k.isTrigger && k.name.StartsWith("Gela_", StringComparison.Ordinal) && !k.name.StartsWith("Gela_C_", StringComparison.Ordinal))
                            .OrderBy(k => k.name, StringComparer.Ordinal).ToArray();
                        lerroak.Add($"## {d.Izena} ({gelak.Length} gela)");
                        foreach (BoxCollider gela in gelak)
                        {
                            Bounds b = gela.bounds;
                            // Gelako 9 lagin (3×3 sareta, zoruaren parean): NavMesh-ean daudenak eta spawn-etik iristen direnak.
                            int laginak = 0, iristen = 0;
                            Vector3 adibidea = Vector3.zero, amaiera = Vector3.zero;
                            for (int ix = -1; ix <= 1; ix++)
                            {
                                for (int iz = -1; iz <= 1; iz++)
                                {
                                    var p = new Vector3(b.center.x + ix * b.extents.x * 0.6f, b.min.y + 0.3f, b.center.z + iz * b.extents.z * 0.6f);
                                    if (!NavMesh.SamplePosition(p, out NavMeshHit jo, 1.2f, NavMesh.AllAreas) || !b.Contains(new Vector3(jo.position.x, b.center.y, jo.position.z)))
                                    {
                                        continue;
                                    }

                                    laginak++;
                                    var bidea = new NavMeshPath();
                                    if (NavMesh.CalculatePath(hasiera.position, jo.position, NavMesh.AllAreas, bidea) && bidea.status == NavMeshPathStatus.PathComplete)
                                    {
                                        iristen++;
                                    }
                                    else if (bidea.corners.Length > 0)
                                    {
                                        adibidea = jo.position;
                                        amaiera = bidea.corners[bidea.corners.Length - 1];
                                    }
                                }
                            }

                            string izena = gela.name.Substring(5);
                            if (laginak == 0)
                            {
                                lerroak.Add($"HUTS: {izena}: gelan ez dago NavMesh-ik (9 laginetik 0)");
                                iritsiEzinak++;
                            }
                            else if (iristen == 0)
                            {
                                lerroak.Add($"HUTS: {izena}: NavMesh-a badu ({laginak}/9 lagin) baina spawn-etik EZ da iristen; bide partziala {V(amaiera)} puntuan amaitzen da, helburutik {Vector3.Distance(amaiera, adibidea):0.0} m-ra ({V(adibidea)})");
                                iritsiEzinak++;
                            }
                            else
                            {
                                lerroak.Add($"ONDO: {izena}: {iristen}/{laginak} lagin iristen dira{(iristen < laginak ? " (zati bat irla da)" : string.Empty)}");
                            }
                        }
                    }
                    finally
                    {
                        NavMesh.RemoveNavMeshData(ins);
                    }
                }

                informea.Atala("Emaitzak", lerroak);
                if (iritsiEzinak == 0)
                {
                    informea.AmaituOndo("gela guztiak irisgarriak");
                }
                else
                {
                    informea.AmaituErrorearekin($"{iritsiEzinak} gela iritsi ezinik");
                }

                EditorApplication.Exit(0);
            }
            catch (Exception salbuespena)
            {
                informea.Atala("Emaitzak", lerroak);
                informea.AmaituErrorearekin(salbuespena.ToString());
                EditorApplication.Exit(1);
            }
        }
    }
}
