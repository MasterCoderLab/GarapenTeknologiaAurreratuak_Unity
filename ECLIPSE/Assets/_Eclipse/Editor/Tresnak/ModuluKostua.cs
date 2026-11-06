using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Eclipse.EditorTresnak
{
    /// <summary>Irakurtzeko bakarrik: kit-eko prefab bakoitzaren kostua (triangeluak, renderer-ak, collider-ak eta UV2), mailak diseinatzeko.</summary>
    public static class ModuluKostua
    {
        private const string Marka = "ECL_MODULUKOSTUA";
        private static readonly string[] Karpetak =
        {
            "Assets/ThirdParty/Sci Fi Modular Pack/Prefabs",
            "Assets/ThirdParty/Data/Sci-fi_assets/Sci-Fi Old rusty props PBR/Prefabs",
            "Assets/_Eclipse/Prefabak/Propioak",
        };

        private static readonly string[] Xehetasunak = { "Profile1", "ProfileDoor", "ProfileEnd", "ProfileWindow", "Floor1" };

        [MenuItem("ECLIPSE/QA/Moduluen kostua")]
        public static void Exekutatu()
        {
            var testua = new StringBuilder();
            testua.AppendLine("| Prefab | Triangeluak | Renderer | Collider | Rigidbody | Argiak | UV2 | Bounds (1:1) |");
            testua.AppendLine("|---|---|---|---|---|---|---|---|");
            var xehetasunak = new StringBuilder();
            foreach (string bidea in AssetDatabase.FindAssets("t:Prefab", Karpetak).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(b => b, StringComparer.Ordinal))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
                if (prefab.name.StartsWith("PF_Seinalea_", StringComparison.Ordinal))
                {
                    continue;
                }

                MeshFilter[] iragazkiak = prefab.GetComponentsInChildren<MeshFilter>(true);
                long tri = iragazkiak.Sum(Tri);
                bool uv2 = iragazkiak.Where(f => f.sharedMesh != null).All(f => f.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1));
                Renderer[] rendererrak = prefab.GetComponentsInChildren<Renderer>(true);
                Bounds kaxa = rendererrak.Length > 0 ? rendererrak[0].bounds : new Bounds();
                foreach (Renderer r in rendererrak)
                {
                    kaxa.Encapsulate(r.bounds);
                }

                testua.AppendLine($"| {prefab.name} | {tri} | {rendererrak.Length} | {prefab.GetComponentsInChildren<Collider>(true).Length} | {(prefab.GetComponentInChildren<Rigidbody>(true) != null ? "bai" : "ez")} | {prefab.GetComponentsInChildren<Light>(true).Length} | {(uv2 ? "bai" : "EZ")} | {kaxa.size.x:0.00}x{kaxa.size.y:0.00}x{kaxa.size.z:0.00} |");
                if (Xehetasunak.Contains(prefab.name))
                {
                    xehetasunak.AppendLine($"\n### {prefab.name}");
                    foreach (MeshFilter f in iragazkiak.OrderByDescending(Tri))
                    {
                        Bounds b = f.GetComponent<Renderer>() != null ? f.GetComponent<Renderer>().bounds : new Bounds();
                        xehetasunak.AppendLine($"- {f.name} ({f.transform.parent?.name}): {Tri(f)} tri, malla {f.sharedMesh?.name}, {b.size.x:0.00}x{b.size.y:0.00}x{b.size.z:0.00} @ {b.center.x:0.00},{b.center.y:0.00},{b.center.z:0.00}");
                    }
                }
            }

            string fitxategia = $"docs/qa/tresnak/ModuluKostua_{DateTime.Now:yyyy-MM-dd}.md";
            System.IO.File.WriteAllText(fitxategia, "# Moduluen kostua\n\n" + testua + "\n## Xehetasunak\n" + xehetasunak);
            Debug.Log($"{Marka}_OK {fitxategia}");
        }

        /// <summary>Eszena batean erabilitako malla estatikoen UV2: tartea, azalera eta eskala. Atlas-a inbaditzen duten mallak aurkitzeko.</summary>
        public static void BatchUV2()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string eszena = args[Array.IndexOf(args, "-eclipseEszena") + 1];
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(eszena, UnityEditor.SceneManagement.OpenSceneMode.Single);
                var taula = new StringBuilder("| Malla | Kop. | Tri | UV2 min | UV2 max | Azalera UV2 | GI |\n|---|---|---|---|---|---|---|\n");
                var taldeak = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude)
                    .Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null)
                    .GroupBy(f => f.sharedMesh);
                foreach (var taldea in taldeak.OrderByDescending(t => t.Count()))
                {
                    Mesh m = taldea.Key;
                    int gi = taldea.Count(f => GameObjectUtility.AreStaticEditorFlagsSet(f.gameObject, StaticEditorFlags.ContributeGI));
                    var uv = new List<Vector2>();
                    string tartea = "irakurri ezin";
                    float azalera = 0f;
                    string min = "?";
                    string max = "?";
                    if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
                    {
                        Vector2[] uv2 = IrakurriUV2(m);
                        int[] t = IrakurriIndizeak(m);
                        if (uv2 != null && uv2.Length > 0)
                        {
                            Vector2 mi = uv2[0];
                            Vector2 ma = uv2[0];
                            foreach (Vector2 u in uv2)
                            {
                                mi = Vector2.Min(mi, u);
                                ma = Vector2.Max(ma, u);
                            }

                            min = $"{mi.x:0.000},{mi.y:0.000}";
                            max = $"{ma.x:0.000},{ma.y:0.000}";
                            if (t != null)
                            {
                                for (int i = 0; i + 2 < t.Length; i += 3)
                                {
                                    Vector2 a = uv2[t[i]];
                                    Vector2 b = uv2[t[i + 1]];
                                    Vector2 c = uv2[t[i + 2]];
                                    azalera += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
                                }
                            }
                        }
                    }
                    else
                    {
                        tartea = "UV2 gabe";
                        min = tartea;
                        max = tartea;
                    }

                    taula.AppendLine($"| {m.name} | {taldea.Count()} | {m.triangles.Length / 3} | {min} | {max} | {azalera:0.000} | {gi} |");
                }

                string fitxategia = $"docs/qa/tresnak/UV2_{System.IO.Path.GetFileNameWithoutExtension(eszena)}.md";
                System.IO.File.WriteAllText(fitxategia, taula.ToString());
                Debug.Log($"{Marka}_OK {fitxategia}");
                EditorApplication.Exit(0);
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static Vector2[] IrakurriUV2(Mesh m)
        {
            if (m.isReadable)
            {
                return m.uv2;
            }

            Mesh kopia = KopiaIrakurgarria(m);
            return kopia != null ? kopia.uv2 : null;
        }

        private static int[] IrakurriIndizeak(Mesh m)
        {
            if (m.isReadable)
            {
                return m.triangles;
            }

            Mesh kopia = KopiaIrakurgarria(m);
            return kopia != null ? kopia.triangles : null;
        }

        private static readonly Dictionary<Mesh, Mesh> Kopiak = new Dictionary<Mesh, Mesh>();

        private static Mesh KopiaIrakurgarria(Mesh m)
        {
            if (Kopiak.TryGetValue(m, out Mesh kopia))
            {
                return kopia;
            }

            kopia = new Mesh();
            Mesh.MeshDataArray datuak = Mesh.AcquireReadOnlyMeshData(m);
            try
            {
                Mesh.MeshData d = datuak[0];
                var uv = new Unity.Collections.NativeArray<Vector2>(d.vertexCount, Unity.Collections.Allocator.Temp);
                var v = new Unity.Collections.NativeArray<Vector3>(d.vertexCount, Unity.Collections.Allocator.Temp);
                d.GetVertices(v);
                d.GetUVs(1, uv);
                kopia.SetVertices(v);
                kopia.SetUVs(1, uv);
                uv.Dispose();
                v.Dispose();
                var indizeak = new List<int>();
                for (int s = 0; s < d.subMeshCount; s++)
                {
                    var i = new Unity.Collections.NativeArray<int>(d.GetSubMesh(s).indexCount, Unity.Collections.Allocator.Temp);
                    d.GetIndices(i, s);
                    indizeak.AddRange(i);
                    i.Dispose();
                }

                kopia.SetTriangles(indizeak, 0);
            }
            finally
            {
                datuak.Dispose();
            }

            Kopiak[m] = kopia;
            return kopia;
        }

        public static void Batch()
        {
            try
            {
                Exekutatu();
                EditorApplication.Exit(0);
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static long Tri(MeshFilter f)
        {
            Mesh m = f.sharedMesh;
            if (m == null)
            {
                return 0;
            }

            long indizeak = 0;
            for (int i = 0; i < m.subMeshCount; i++)
            {
                indizeak += m.GetIndexCount(i);
            }

            return indizeak / 3;
        }
    }
}
