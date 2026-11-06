using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Eclipse.EditorTresnak
{
    /// <summary>Hiru mailetarako aktibo propio arinak: oinarrizko mallak (kutxa, zilindroa, eraztuna), material partekatuak eta prefab-ak.</summary>
    public static partial class EszenaPropioa
    {
        private const MaterialGlobalIlluminationFlags Baked = MaterialGlobalIlluminationFlags.BakedEmissive;
        private const MaterialGlobalIlluminationFlags Realtime = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        // Emisiorik gabeko materialetan Unity-k balio hau jartzen du: besterik jarriz gero, beti aldatuta legoke.
        private const MaterialGlobalIlluminationFlags BeltzaGI = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

        /// <summary>Aktibo guztiak sortu edo egiaztatu. Aurrebistan ez da ezer idazten; itzultzen du zenbat aldaketa dauden.</summary>
        internal static int MailaAktiboak(bool aplikatu)
        {
            var a = new List<string>();
            KarpetaSortu(EreduKarpeta);
            KarpetaSortu(MaterialKarpeta);

            Mesh kaxa = MeshGorde("SM_Kaxa", MailaMeshak.Kaxa(), aplikatu, a);
            Mesh zilindroa = MeshGorde("SM_Zilindroa", MailaMeshak.Zilindroa(24), aplikatu, a);
            Mesh eraztuna = MeshGorde("SM_Eraztuna", MailaMeshak.Eraztuna(40, 8, 0.05f), aplikatu, a);
            Mesh panela = AssetDatabase.LoadAssetAtPath<Mesh>($"{EreduKarpeta}/SM_ArgiPanela.asset");

            Material Mat(string izena, Color oinarria, Color emisioa, float metala, float leuntasuna, MaterialGlobalIlluminationFlags gi)
            {
                return Materiala(izena, null, null, oinarria, emisioa, metala, leuntasuna, Vector2.one, gi, aplikatu, a);
            }

            Color beltza = Color.black;
            Material iluna = Mat("M_Metal_Iluna", new Color(0.12f, 0.13f, 0.15f), beltza, 0.8f, 0.55f, BeltzaGI);
            Material argia = Mat("M_Metal_Argia", new Color(0.55f, 0.58f, 0.62f), beltza, 0.7f, 0.5f, BeltzaGI);
            Material sareta = Mat("M_Sareta", new Color(0.25f, 0.27f, 0.3f), beltza, 0.85f, 0.35f, BeltzaGI);
            Material laranja = Mat("M_Hodia_Laranja", new Color(0.75f, 0.35f, 0.06f), beltza, 0.4f, 0.45f, BeltzaGI);
            Material pUrdina = Mat("M_Pantaila_Urdina", new Color(0.02f, 0.05f, 0.1f), new Color(0.25f, 0.65f, 1f) * 1.1f, 0f, 0.8f, Baked);
            Material pLaranja = Mat("M_Pantaila_Laranja", new Color(0.1f, 0.05f, 0.01f), new Color(1f, 0.55f, 0.1f) * 1.1f, 0f, 0.8f, Baked);
            Material pGorria = Mat("M_Pantaila_Gorria", new Color(0.1f, 0.01f, 0.01f), new Color(1f, 0.1f, 0.06f) * 1.1f, 0f, 0.8f, Baked);
            Material nukleoa = Mat("M_Nukleoa", new Color(0.3f, 1f, 1f), new Color(0.2f, 0.9f, 1.1f) * 1.7f, 0f, 0.9f, Baked);
            Material eGorria = Mat("M_Eraztuna_Gorria", new Color(0.3f, 0.02f, 0.02f), new Color(1f, 0.08f, 0.04f) * 3f, 0.3f, 0.7f, Realtime);
            Material eZiana = Mat("M_Eraztuna_Ziana", new Color(0.02f, 0.25f, 0.3f), new Color(0.15f, 0.9f, 1f) * 3f, 0.3f, 0.7f, Realtime);
            Material bobina = Mat("M_Bobina", new Color(0.3f, 0.15f, 0.02f), new Color(1f, 0.55f, 0.1f) * 1.3f, 0.3f, 0.7f, Baked);
            Material bilduma = Mat("M_Bilduma", new Color(0.2f, 0.8f, 1f), new Color(0.2f, 0.85f, 1f) * 3f, 0f, 0.8f, Realtime);
            Material pGorriaArgia = Mat("M_ArgiPanela_Gorria", Color.white, new Color(1f, 0.12f, 0.08f) * 3f, 0f, 0.2f, Baked);
            Material pAnbarra = Mat("M_ArgiPanela_Anbarra", Color.white, new Color(1f, 0.62f, 0.18f) * 3f, 0f, 0.2f, Baked);
            Material pZiana = Mat("M_ArgiPanela_Ziana", Color.white, new Color(0.35f, 0.9f, 1f) * 3f, 0f, 0.2f, Baked);
            // Zoru metaliko gutxiagoa: metal hutsak ia ez du argi hornitua erakusten. Testura bera, 01_Instalazioa-ren materiala ukitu gabe.
            Material zorua = Materiala("M_Zorua_Maila", "T_Zorua_Tile", null, Color.white, beltza, 0.1f, 0.42f, Vector2.one, BeltzaGI, aplikatu, a);
            Material beira = Mat("M_Beira", new Color(0.55f, 0.85f, 1f, 0.16f), beltza, 0.1f, 0.95f, BeltzaGI);
            Material barrera = Mat("M_Barrera", new Color(1f, 0.12f, 0.08f, 0.35f), new Color(1f, 0.1f, 0.05f) * 1.5f, 0f, 0.9f, Realtime);
            if (aplikatu)
            {
                GardenaEzarri(beira);
                GardenaEzarri(barrera);
            }

            var estatikoa = StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic;
            GameObject P(string izena, Mesh mesh, Material materiala, StaticEditorFlags? flagak = null)
            {
                return PrefabFlagekin(izena, mesh, materiala, flagak ?? estatikoa, aplikatu, a);
            }

            P("PF_Zorua_Maila", AssetDatabase.LoadAssetAtPath<Mesh>($"{EreduKarpeta}/SM_Zorua_8x8.asset"), zorua);
            P("PF_Plaka", kaxa, sareta);
            P("PF_Barra", kaxa, iluna);
            P("PF_Metala", kaxa, argia);
            P("PF_Oina", zilindroa, iluna);
            P("PF_Zutabea", zilindroa, argia);
            P("PF_Hodia_Laranja", zilindroa, laranja);
            P("PF_Hodia_Grisa", zilindroa, argia);
            P("PF_Pantaila_Urdina", kaxa, pUrdina);
            P("PF_Pantaila_Laranja", kaxa, pLaranja);
            P("PF_Pantaila_Gorria", kaxa, pGorria);
            P("PF_Nukleoa", zilindroa, nukleoa);
            P("PF_Bobina", zilindroa, bobina);
            P("PF_ArgiPanela_Gorria", panela, pGorriaArgia);
            P("PF_ArgiPanela_Anbarra", panela, pAnbarra);
            P("PF_ArgiPanela_Ziana", panela, pZiana);
            P("PF_Beira", kaxa, beira, 0);
            P("PF_Beira_Zilindroa", zilindroa, beira, 0);
            P("PF_Barrera", kaxa, barrera, 0);
            P("PF_Din_Eraztuna_Gorria", eraztuna, eGorria, 0);
            P("PF_Din_Eraztuna_Ziana", eraztuna, eZiana, 0);
            P("PF_Din_Bilduma", kaxa, bilduma, 0);

            if (aplikatu)
            {
                AssetDatabase.SaveAssets();
            }

            foreach (string lerroa in a)
            {
                Debug.Log($"{Marka}_AKTIBOA {lerroa}");
            }

            return a.Count;
        }

        private static void GardenaEzarri(Material m)
        {
            if (m == null || (Mathf.Approximately(m.GetFloat("_Surface"), 1f) && m.renderQueue == 3000))
            {
                return;
            }

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", 5f);
            m.SetFloat("_DstBlend", 10f);
            m.SetFloat("_SrcBlendAlpha", 1f);
            m.SetFloat("_DstBlendAlpha", 10f);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
        }

        private static GameObject PrefabFlagekin(string izena, Mesh mesh, Material materiala, StaticEditorFlags flagak, bool aplikatu, List<string> a)
        {
            string bidea = $"{Erroa}/{izena}.prefab";
            var zaharra = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
            bool zuzena = zaharra != null && mesh != null
                          && zaharra.GetComponent<MeshFilter>()?.sharedMesh == mesh
                          && zaharra.GetComponent<MeshRenderer>()?.sharedMaterial == materiala
                          && GameObjectUtility.GetStaticEditorFlags(zaharra) == flagak;
            if (zuzena)
            {
                return zaharra;
            }

            a.Add($"{bidea}: {(zaharra != null ? "eguneratu" : "sortu")}");
            if (!aplikatu)
            {
                return zaharra;
            }

            if (mesh == null || materiala == null)
            {
                throw new InvalidOperationException($"{izena}: malla edo materiala falta da.");
            }

            var objektua = new GameObject(izena);
            try
            {
                objektua.AddComponent<MeshFilter>().sharedMesh = mesh;
                objektua.AddComponent<MeshRenderer>().sharedMaterial = materiala;
                GameObjectUtility.SetStaticEditorFlags(objektua, flagak);
                return PrefabUtility.SaveAsPrefabAsset(objektua, bidea);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objektua);
            }
        }
    }

    /// <summary>Oinarrizko malla prozeduralak, jatorria erdian eta tamaina 1 m-koa.</summary>
    internal static class MailaMeshak
    {
        public static Mesh Kaxa()
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            void Aurpegia(Vector3 normala, Vector3 ardatzA, Vector3 ardatzB)
            {
                int i = v.Count;
                Vector3 erdia = normala * 0.5f;
                v.Add(erdia - ardatzA * 0.5f - ardatzB * 0.5f);
                v.Add(erdia - ardatzA * 0.5f + ardatzB * 0.5f);
                v.Add(erdia + ardatzA * 0.5f + ardatzB * 0.5f);
                v.Add(erdia + ardatzA * 0.5f - ardatzB * 0.5f);
                n.AddRange(new[] { normala, normala, normala, normala });
                uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            Aurpegia(Vector3.up, Vector3.right, Vector3.forward);
            Aurpegia(Vector3.down, Vector3.forward, Vector3.right);
            Aurpegia(Vector3.right, Vector3.forward, Vector3.up);
            Aurpegia(Vector3.left, Vector3.up, Vector3.forward);
            Aurpegia(Vector3.forward, Vector3.up, Vector3.right);
            Aurpegia(Vector3.back, Vector3.right, Vector3.up);
            return Eraiki(v, n, uv, t);
        }

        public static Mesh Zilindroa(int aldeak)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int i = 0; i <= aldeak; i++)
            {
                float u = (float)i / aldeak;
                float angelua = u * Mathf.PI * 2f;
                var normala = new Vector3(Mathf.Cos(angelua), 0f, Mathf.Sin(angelua));
                v.Add(normala * 0.5f + Vector3.down * 0.5f);
                v.Add(normala * 0.5f + Vector3.up * 0.5f);
                n.Add(normala);
                n.Add(normala);
                uv.Add(new Vector2(u, 0f));
                uv.Add(new Vector2(u, 1f));
                if (i < aldeak)
                {
                    int a = i * 2;
                    t.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
                }
            }

            foreach (float y in new[] { 0.5f, -0.5f })
            {
                int erdia = v.Count;
                Vector3 normala = y > 0f ? Vector3.up : Vector3.down;
                v.Add(Vector3.up * y);
                n.Add(normala);
                uv.Add(Vector2.one * 0.5f);
                for (int i = 0; i <= aldeak; i++)
                {
                    float angelua = (float)i / aldeak * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(angelua) * 0.5f, y, Mathf.Sin(angelua) * 0.5f);
                    v.Add(p);
                    n.Add(normala);
                    uv.Add(new Vector2(p.x + 0.5f, p.z + 0.5f));
                    if (i < aldeak)
                    {
                        int k = erdia + 1 + i;
                        if (y > 0f)
                        {
                            t.AddRange(new[] { erdia, k + 1, k });
                        }
                        else
                        {
                            t.AddRange(new[] { erdia, k, k + 1 });
                        }
                    }
                }
            }

            return Eraiki(v, n, uv, t);
        }

        public static Mesh Eraztuna(int nagusia, int txikia, float erradioTxikia)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            float erradioa = 0.5f - erradioTxikia;
            for (int i = 0; i <= nagusia; i++)
            {
                float a = (float)i / nagusia * Mathf.PI * 2f;
                var erdia = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int j = 0; j <= txikia; j++)
                {
                    float b = (float)j / txikia * Mathf.PI * 2f;
                    Vector3 normala = erdia * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    v.Add(erdia * erradioa + normala * erradioTxikia);
                    n.Add(normala);
                    uv.Add(new Vector2((float)i / nagusia, (float)j / txikia));
                    if (i < nagusia && j < txikia)
                    {
                        int p = i * (txikia + 1) + j;
                        int q = p + txikia + 1;
                        t.AddRange(new[] { p, p + 1, q, q, p + 1, q + 1 });
                    }
                }
            }

            return Eraiki(v, n, uv, t);
        }

        private static Mesh Eraiki(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
