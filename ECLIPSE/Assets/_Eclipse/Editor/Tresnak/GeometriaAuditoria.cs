using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Eszenan erabiltzen diren malla eta testuren auditoria, irakurtzeko bakarrik: topologia (ertz irekiak,
    /// non-manifold), normalak, UV-ak, pibotea, eskala eta testurak. Datuak mailetatik zuzenean ateratzen dira.
    /// </summary>
    internal static class GeometriaAuditoria
    {
        private const string Tresna = "GeometriaAuditoria";
        private const string Marka = "ECL_GEOMETRIAAUDITORIA";

        [MenuItem("ECLIPSE/QA/GeometriaAuditoria (ez du ezer aldatzen)")]
        private static void Menua() => Exekutatu();

        internal static bool Exekutatu()
        {
            Debug.Log($"{Marka}_HASI");
            Scene eszena = SceneManager.GetActiveScene();
            var informea = new Informea(Tresna, Marka, "Auditoria", eszena.path, "Eszenako malla eta testura guztiak");
            try
            {
                var erabilerak = new Dictionary<Mesh, int>();
                var materialak = new HashSet<Material>();
                foreach (GameObject erroa in eszena.GetRootGameObjects())
                {
                    foreach (MeshFilter f in erroa.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (f.sharedMesh != null)
                        {
                            erabilerak.TryGetValue(f.sharedMesh, out int n);
                            erabilerak[f.sharedMesh] = n + 1;
                        }
                    }

                    foreach (Renderer r in erroa.GetComponentsInChildren<Renderer>(true))
                    {
                        materialak.UnionWith(r.sharedMaterials.Where(m => m != null));
                    }
                }

                int ezinIrakurri = 0;
                informea.Lerroa(string.Empty);
                informea.Lerroa("## Mailak");
                informea.Lerroa(string.Empty);
                informea.Lerroa("| Malla | Jatorria | Inst. | Tri | Ertz irekiak | Non-manifold | Degeneratuak | Normal | UV0 tartea | UV2 | Pibotea → kaxaren erdia | Tamaina | Inport. eskala |");
                informea.Lerroa("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
                foreach (KeyValuePair<Mesh, int> e in erabilerak.OrderBy(e => e.Key.name, StringComparer.Ordinal))
                {
                    Mesh m = e.Key;
                    string bidea = AssetDatabase.GetAssetPath(m);
                    string jatorria = bidea.StartsWith("Assets/ThirdParty", StringComparison.Ordinal) ? "kit" : bidea.StartsWith("Assets/_Eclipse", StringComparison.Ordinal) ? "propioa" : "Unity";
                    string eskala = AssetImporter.GetAtPath(bidea) is ModelImporter inp
                        ? $"{F(inp.globalScale)} × {(inp.useFileScale ? F(inp.fileScale) : "1")}"
                        : "—";

                    if (!Topologia(m, out int irekiak, out int nonManifold, out int degeneratuak, out string normalak, out string uv0))
                    {
                        ezinIrakurri++;
                        informea.Lerroa($"| {m.name} | {jatorria} | {e.Value} | {TriKopurua(m)} | ? | ? | ? | ? | ? | {Bai(m.HasVertexAttribute(VertexAttribute.TexCoord1))} | {V(m.bounds.center)} | {V(m.bounds.size)} | {eskala} |");
                        continue;
                    }

                    informea.Lerroa($"| {m.name} | {jatorria} | {e.Value} | {TriKopurua(m)} | {irekiak} | {nonManifold} | {degeneratuak} | {normalak} | {uv0} | {Bai(m.HasVertexAttribute(VertexAttribute.TexCoord1))} | {V(m.bounds.center)} | {V(m.bounds.size)} | {eskala} |");
                }

                informea.Lerroa(string.Empty);
                informea.Lerroa("## Testurak");
                informea.Lerroa(string.Empty);
                informea.Lerroa("| Materiala | Shader | Base map | Tamaina | Wrap | Tiling | Normal map | Emisioa | Metala | Leuntasuna |");
                informea.Lerroa("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
                foreach (Material m in materialak.OrderBy(m => m.name, StringComparer.Ordinal))
                {
                    Texture t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                    string normal = m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null ? m.GetTexture("_BumpMap").name : "—";
                    string emisioa = m.IsKeywordEnabled("_EMISSION") ? "bai" : "ez";
                    string metala = m.HasProperty("_Metallic") ? F(m.GetFloat("_Metallic")) : "—";
                    string leuna = m.HasProperty("_Smoothness") ? F(m.GetFloat("_Smoothness")) : "—";
                    string tiling = t != null && m.HasProperty("_BaseMap") ? $"{F(m.GetTextureScale("_BaseMap").x)}×{F(m.GetTextureScale("_BaseMap").y)}" : "—";
                    informea.Lerroa($"| {m.name} | {m.shader.name} | {(t != null ? t.name : "—")} | {(t != null ? $"{t.width}×{t.height}" : "—")} | {(t != null ? t.wrapMode.ToString() : "—")} | {tiling} | {normal} | {emisioa} | {metala} | {leuna} |");
                }

                if (ezinIrakurri > 0)
                {
                    informea.Lerroa(string.Empty);
                    informea.Lerroa($"**Oharra:** {ezinIrakurri} mailaren erpinak ezin izan dira irakurri (Read/Write desaktibatuta): '?' markatuta. Kit-a ez da aldatzen hori saihesteko.");
                }

                informea.AmaituOndo($"{erabilerak.Count} malla, {materialak.Count} material; {ezinIrakurri} irakurri ezinik");
                return true;
            }
            catch (Exception salbuespena)
            {
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }
        }

        /// <summary>
        /// Erpinak posizioz batzen dira (ertz gogorrek eta UV-ek erpinak bikoizten dituzte). Ertz bat:
        /// triangelu 1 = irekia; &gt; 2 = non-manifold.
        /// </summary>
        private static bool Topologia(Mesh m, out int irekiak, out int nonManifold, out int degeneratuak, out string normalak, out string uv0)
        {
            irekiak = nonManifold = degeneratuak = 0;
            normalak = uv0 = "?";
            Vector3[] v;
            Vector3[] n;
            Vector2[] uv;
            int[] t;
            if (m.isReadable)
            {
                v = m.vertices;
                n = m.normals;
                uv = m.uv;
                t = m.triangles;
            }
            else if (BufferretikIrakurri(m, out v, out t))
            {
                // Read/Write gabeko kit-eko malla: posizioak eta indizeak memoriako bufferretatik; ez da ezer gordetzen.
                n = null;
                uv = null;
                normalak = m.HasVertexAttribute(VertexAttribute.Normal) ? "bai" : "ez";
            }
            else
            {
                return false;
            }

            var indizeak = new Dictionary<(int, int, int), int>();
            var batuak = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var gakoa = (Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
                if (!indizeak.TryGetValue(gakoa, out int indizea))
                {
                    indizea = indizeak.Count;
                    indizeak.Add(gakoa, indizea);
                }

                batuak[i] = indizea;
            }

            var ertzak = new Dictionary<(int, int), int>();
            for (int i = 0; i < t.Length; i += 3)
            {
                int a = batuak[t[i]];
                int b = batuak[t[i + 1]];
                int c = batuak[t[i + 2]];
                if (a == b || b == c || a == c)
                {
                    degeneratuak++;
                    continue;
                }

                foreach ((int p, int q) in new[] { (a, b), (b, c), (c, a) })
                {
                    var gakoa = p < q ? (p, q) : (q, p);
                    ertzak.TryGetValue(gakoa, out int kop);
                    ertzak[gakoa] = kop + 1;
                }
            }

            irekiak = ertzak.Values.Count(k => k == 1);
            nonManifold = ertzak.Values.Count(k => k > 2);

            if (n == null)
            {
                return true;
            }

            if (n.Length == v.Length && n.Length > 0)
            {
                int gaizki = n.Count(x => Mathf.Abs(x.magnitude - 1f) > 0.01f);
                normalak = gaizki == 0 ? "ondo" : $"{gaizki} oker";
            }
            else
            {
                normalak = "ez";
            }

            if (uv.Length > 0)
            {
                uv0 = $"{F(uv.Min(x => x.x))}..{F(uv.Max(x => x.x))} / {F(uv.Min(x => x.y))}..{F(uv.Max(x => x.y))}";
            }

            return true;
        }

        private static int TriKopurua(Mesh m)
        {
            long indizeak = 0;
            for (int i = 0; i < m.subMeshCount; i++)
            {
                indizeak += m.GetIndexCount(i);
            }

            return (int)(indizeak / 3);
        }

        /// <summary>Read/Write gabeko mallak: GPU-ko bufferrak memorian irakurri. Asetak ez dira aldatzen.</summary>
        private static bool BufferretikIrakurri(Mesh m, out Vector3[] erpinak, out int[] triangeluak)
        {
            erpinak = null;
            triangeluak = null;
            if (m.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32)
            {
                return false;
            }

            GraphicsBuffer vb = null;
            GraphicsBuffer ib = null;
            try
            {
                m.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                m.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                int korrontea = m.GetVertexAttributeStream(VertexAttribute.Position);
                int desplazamendua = m.GetVertexAttributeOffset(VertexAttribute.Position);
                int urratsa = m.GetVertexBufferStride(korrontea);
                vb = m.GetVertexBuffer(korrontea);
                ib = m.GetIndexBuffer();
                if (vb == null || ib == null)
                {
                    return false;
                }

                var byteak = new byte[vb.count * vb.stride];
                vb.GetData(byteak);
                erpinak = new Vector3[m.vertexCount];
                for (int i = 0; i < erpinak.Length; i++)
                {
                    int o = i * urratsa + desplazamendua;
                    erpinak[i] = new Vector3(BitConverter.ToSingle(byteak, o), BitConverter.ToSingle(byteak, o + 4), BitConverter.ToSingle(byteak, o + 8));
                }

                var indizeBytes = new byte[ib.count * ib.stride];
                ib.GetData(indizeBytes);
                bool hamaseibit = m.indexFormat == IndexFormat.UInt16;
                var lista = new List<int>(TriKopurua(m) * 3);
                for (int sub = 0; sub < m.subMeshCount; sub++)
                {
                    int hasiera = (int)m.GetIndexStart(sub);
                    int kop = (int)m.GetIndexCount(sub);
                    int oinarria = (int)m.GetBaseVertex(sub);
                    for (int i = hasiera; i < hasiera + kop; i++)
                    {
                        int balioa = hamaseibit ? BitConverter.ToUInt16(indizeBytes, i * 2) : BitConverter.ToInt32(indizeBytes, i * 4);
                        lista.Add(balioa + oinarria);
                    }
                }

                triangeluak = lista.ToArray();
                return true;
            }
            catch (Exception salbuespena) when (salbuespena is InvalidOperationException || salbuespena is ArgumentException || salbuespena is IndexOutOfRangeException)
            {
                Debug.LogWarning($"{Marka} {m.name}: bufferrak ezin izan dira irakurri: {salbuespena.Message}");
                return false;
            }
            finally
            {
                vb?.Dispose();
                ib?.Dispose();
            }
        }

        private static string Bai(bool balioa) => balioa ? "bai" : "ez";

        private static string F(float balioa) => balioa.ToString("0.00", CultureInfo.InvariantCulture);

        private static string V(Vector3 v) => $"({F(v.x)}, {F(v.y)}, {F(v.z)})";
    }
}
