using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Eszenaren nortasuna: kit-etik kanpoko aset propioak (SM_/T_/M_/PF_), argiak eta zerua.
    /// Unique texture: datu-nukleoa eta seinaleak. Tileable: laborategiko zoru metalikoa. Trim sheet: bandak.
    /// Aset-ak kodez sortzen dira (ez dago YAML editatzerik) eta ez dute ThirdParty ukitzen.
    /// Aset-en aldaketak ez dira Ctrl+Z-rekin desegiten; eszenakoak bai, Undo talde bakarrean.
    /// </summary>
    public static partial class EszenaPropioa
    {
        private const string Tresna = "EszenaPropioa";
        private const string Marka = "ECL_ESZENAPROPIOA";
        private const string Menua = "ECLIPSE/Argiztapena/EszenaPropioa: ";
        private const string EszenaBidea = "Assets/_Eclipse/Eszenak/01_Instalazioa.unity";

        private const string Erroa = "Assets/_Eclipse/Prefabak/Propioak";
        private const string EreduKarpeta = Erroa + "/Eredua";
        private const string TesturaKarpeta = Erroa + "/Testurak";
        private const string MaterialKarpeta = Erroa + "/Materialak";
        private const string ArgiKarpeta = "Assets/_Eclipse/Datuak/Argiztapena";
        private const string LightingBidea = ArgiKarpeta + "/ECLIPSE_Argiztapena.lighting";
        private const string ZeruBidea = ArgiKarpeta + "/M_Zerua.mat";

        private const string ArgiakKontainerra = "Argiak";
        private const string PropioakKontainerra = "Propioak";
        private const float Tolerantzia = 0.001f;
        private const float BlockoutLightmapEskala = 0.5f;

        private static readonly (string Sala, string Testua)[] Seinaleak =
        {
            ("Gela_S1_Sarrera", "SARRERA"),
            ("Gela_S1_Laborategia", "LABORATEGIA"),
            ("Gela_S2_Fusibleak", "FUSIBLEAK"),
            ("Gela_S2_Sorgailua", "SORGAILUA"),
            ("Gela_S3_Erreaktorea", "ERREAKTOREA"),
        };

        [MenuItem(Menua + "Aurrebista")]
        private static void Aurrebista() => Exekutatu(false);

        [MenuItem(Menua + "Aplikatu")]
        private static void Aplikatu() => Exekutatu(true);

        /// <summary>Batchmode: aurrebista bakarrik; ez du ezer gordetzen. Idenpotentzia egiaztatzeko.</summary>
        public static void BatchAurrebista()
        {
            int kodea = 1;
            try
            {
                EditorSceneManager.OpenScene(EszenaBidea, OpenSceneMode.Single);
                kodea = Exekutatu(false) ? 0 : 1;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{Marka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Seinale berri bat (testura, materiala eta prefab-a), mailen eraikitzaileentzat. Dagoeneko badago, hura itzultzen du.</summary>
        private const int SeinaleLetrak = 8;

        internal static GameObject SeinaleaPrefab(string testua)
        {
            var aldaketak = new List<string>();
            KarpetaSortu(TesturaKarpeta);
            KarpetaSortu(MaterialKarpeta);
            string tIzena = $"T_Seinalea_{testua}";
            // Testu laburrak erdian, letrak zabaldu gabe: zuriuneak hutsik marrazten dira.
            string betea = testua.PadLeft((SeinaleLetrak + testua.Length) / 2).PadRight(SeinaleLetrak);
            Testura(tIzena, TesturaProzedurala.Seinalea(betea), true, TextureWrapMode.Clamp, true, aldaketak);
            Material m = Materiala($"M_Seinalea_{testua}", tIzena, tIzena, Color.white, new Color(1.2f, 1.2f, 1.2f), 0.1f, 0.3f, Vector2.one, MaterialGlobalIlluminationFlags.RealtimeEmissive, true, aldaketak);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{EreduKarpeta}/SM_Seinalea.asset");
            return mesh != null ? PrefabGorde($"PF_Seinalea_{testua}", mesh, m, false, true, aldaketak) : null;
        }

        internal static bool Exekutatu(bool aplikatu)
        {
            string modua = aplikatu ? "Aplikatu" : "Aurrebista";
            Debug.Log($"{Marka}_HASI {modua}");
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != EszenaBidea)
            {
                Debug.LogError($"{Marka}_ERROR Eszena aktiboak {EszenaBidea} izan behar du, Play modutik kanpo.");
                return false;
            }

            var informea = new Informea(Tresna, Marka, modua, EszenaBidea, "Aset propioak, argiak eta zerua");
            var aldaketak = new List<string>();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("ECLIPSE EszenaPropioa");
            int taldea = Undo.GetCurrentGroup();
            try
            {
                Dictionary<string, GameObject> prefabak = AsetakSinkronizatu(aplikatu, aldaketak);
                InguruneaSinkronizatu(aplikatu, aldaketak);
                EszenaSinkronizatu(prefabak, aplikatu, aldaketak);
                Undo.CollapseUndoOperations(taldea);
                if (aplikatu && aldaketak.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    AssetDatabase.SaveAssets();
                }
            }
            catch (Exception salbuespena)
            {
                Undo.RevertAllDownToGroup(taldea);
                informea.Atala("Aldaketak (eszenakoak desegin dira)", aldaketak);
                informea.AmaituErrorearekin(salbuespena.ToString());
                return false;
            }

            informea.Atala(aplikatu ? "Aldaketak" : "Aldatuko litzatekeena", aldaketak);
            informea.Lerroa(string.Empty);
            informea.Lerroa($"**Aldaketak guztira: {aldaketak.Count}**");
            informea.AmaituOndo($"{aldaketak.Count} aldaketa{(aplikatu ? string.Empty : " (ez da ezer aldatu)")}");
            return true;
        }

        // ---------- Aset-ak ----------

        private static Dictionary<string, GameObject> AsetakSinkronizatu(bool aplikatu, List<string> a)
        {
            if (aplikatu)
            {
                foreach (string karpeta in new[] { EreduKarpeta, TesturaKarpeta, MaterialKarpeta, ArgiKarpeta })
                {
                    KarpetaSortu(karpeta);
                }
            }

            // Testurak.
            Testura("T_DatuNukleoa_Albedo", TesturaProzedurala.NukleoAlbedo(), true, TextureWrapMode.Clamp, aplikatu, a);
            Testura("T_DatuNukleoa_Emisioa", TesturaProzedurala.NukleoEmisioa(), true, TextureWrapMode.Clamp, aplikatu, a);
            Testura("T_Zorua_Tile", TesturaProzedurala.ZoruTile(), true, TextureWrapMode.Repeat, aplikatu, a);
            Testura("T_Eclipse_Trim_Albedo", TesturaProzedurala.TrimAlbedo(), true, TextureWrapMode.Repeat, aplikatu, a);
            Testura("T_Eclipse_Trim_Emisioa", TesturaProzedurala.TrimEmisioa(), true, TextureWrapMode.Repeat, aplikatu, a);
            foreach ((string _, string testua) in Seinaleak)
            {
                Testura($"T_Seinalea_{testua}", TesturaProzedurala.Seinalea(testua), true, TextureWrapMode.Clamp, aplikatu, a);
            }

            // Materialak.
            Material nukleoa = Materiala("M_DatuNukleoa", "T_DatuNukleoa_Albedo", "T_DatuNukleoa_Emisioa", Color.white, new Color(1.6f, 1.6f, 1.6f), 0.7f, 0.55f, Vector2.one, MaterialGlobalIlluminationFlags.BakedEmissive, aplikatu, a);
            Material zorua = Materiala("M_Zorua_Metala", "T_Zorua_Tile", null, Color.white, Color.black, 0.9f, 0.82f, Vector2.one, MaterialGlobalIlluminationFlags.EmissiveIsBlack, aplikatu, a);
            Material trim = Materiala("M_Eclipse_Trim", "T_Eclipse_Trim_Albedo", "T_Eclipse_Trim_Emisioa", Color.white, new Color(1.4f, 1.4f, 1.4f), 0.5f, 0.45f, Vector2.one, MaterialGlobalIlluminationFlags.RealtimeEmissive, aplikatu, a);
            // URP-k _EMISSION kentzen du AnyEmissive ez badago: RealtimeEmissive = distira ikusgai, bake-ari gehitu gabe.
            Material hotza = Materiala("M_ArgiPanela_Hotza", null, null, Color.white, Kolorea(6500f) * 3f, 0f, 0.2f, Vector2.one, MaterialGlobalIlluminationFlags.RealtimeEmissive, aplikatu, a);
            Material epela = Materiala("M_ArgiPanela_Epela", null, null, Color.white, Kolorea(3800f) * 3f, 0f, 0.2f, Vector2.one, MaterialGlobalIlluminationFlags.RealtimeEmissive, aplikatu, a);

            // Mailak.
            var mailak = new Dictionary<string, Mesh>
            {
                ["SM_DatuNukleoa"] = MeshProzedurala.DatuNukleoa(),
                ["SM_Zorua_8x8"] = MeshProzedurala.ZoruPlaka(8f, 8f, 4f),
                ["SM_Trim_Hazard"] = MeshProzedurala.ZoruPlaka(3.4f, 1.2f, 0f, 0.01f, 0.24f, 1f),
                ["SM_Trim_Banda_18"] = MeshProzedurala.Banda(18f, 0.3f, 0.51f, 0.74f),
                ["SM_Trim_Banda_11"] = MeshProzedurala.Banda(11f, 0.3f, 0.51f, 0.74f),
                ["SM_ArgiPanela"] = MeshProzedurala.Panela(2f, 1f),
                ["SM_Seinalea"] = MeshProzedurala.Seinalea(3.2f, 0.8f),
            };

            var meshak = new Dictionary<string, Mesh>();
            foreach (KeyValuePair<string, Mesh> maila in mailak)
            {
                meshak[maila.Key] = MeshGorde(maila.Key, maila.Value, aplikatu, a);
            }

            // Prefab-ak.
            var prefabak = new Dictionary<string, GameObject>
            {
                ["PF_DatuNukleoa"] = PrefabGorde("PF_DatuNukleoa", meshak["SM_DatuNukleoa"], nukleoa, true, aplikatu, a),
                ["PF_Zorua_8x8"] = PrefabGorde("PF_Zorua_8x8", meshak["SM_Zorua_8x8"], zorua, false, aplikatu, a),
                ["PF_Trim_Hazard"] = PrefabGorde("PF_Trim_Hazard", meshak["SM_Trim_Hazard"], trim, false, aplikatu, a),
                ["PF_Trim_Banda_18"] = PrefabGorde("PF_Trim_Banda_18", meshak["SM_Trim_Banda_18"], trim, false, aplikatu, a),
                ["PF_Trim_Banda_11"] = PrefabGorde("PF_Trim_Banda_11", meshak["SM_Trim_Banda_11"], trim, false, aplikatu, a),
                ["PF_ArgiPanela_Hotza"] = PrefabGorde("PF_ArgiPanela_Hotza", meshak["SM_ArgiPanela"], hotza, false, aplikatu, a),
                ["PF_ArgiPanela_Epela"] = PrefabGorde("PF_ArgiPanela_Epela", meshak["SM_ArgiPanela"], epela, false, aplikatu, a),
            };

            foreach ((string _, string testua) in Seinaleak)
            {
                string tIzena = $"T_Seinalea_{testua}";
                Material m = Materiala($"M_Seinalea_{testua}", tIzena, tIzena, Color.white, new Color(1.2f, 1.2f, 1.2f), 0.1f, 0.3f, Vector2.one, MaterialGlobalIlluminationFlags.RealtimeEmissive, aplikatu, a);
                prefabak[$"PF_Seinalea_{testua}"] = PrefabGorde($"PF_Seinalea_{testua}", meshak["SM_Seinalea"], m, false, aplikatu, a);
            }

            return prefabak;
        }

        private static void KarpetaSortu(string bidea)
        {
            if (AssetDatabase.IsValidFolder(bidea))
            {
                return;
            }

            string gurasoa = Path.GetDirectoryName(bidea)?.Replace('\\', '/');
            KarpetaSortu(gurasoa);
            AssetDatabase.CreateFolder(gurasoa, Path.GetFileName(bidea));
        }

        private static void Testura(string izena, byte[] png, bool srgb, TextureWrapMode bilketa, bool aplikatu, List<string> a)
        {
            string bidea = $"{TesturaKarpeta}/{izena}.png";
            string osoa = Path.GetFullPath(bidea);
            bool berdina = File.Exists(osoa) && File.ReadAllBytes(osoa).SequenceEqual(png);
            if (!berdina)
            {
                a.Add($"{bidea}: {(File.Exists(osoa) ? "eguneratu" : "sortu")}");
                if (!aplikatu)
                {
                    return;
                }

                File.WriteAllBytes(osoa, png);
                AssetDatabase.ImportAsset(bidea, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetImporter.GetAtPath(bidea) is TextureImporter inportatzailea
                && (inportatzailea.wrapMode != bilketa || inportatzailea.sRGBTexture != srgb || inportatzailea.maxTextureSize != 2048))
            {
                a.Add($"{bidea}: inportazio-ezarpenak");
                if (aplikatu)
                {
                    inportatzailea.wrapMode = bilketa;
                    inportatzailea.sRGBTexture = srgb;
                    inportatzailea.maxTextureSize = 2048;
                    inportatzailea.SaveAndReimport();
                }
            }
        }

        private static Material Materiala(string izena, string albedo, string emisioa, Color oinarria, Color emisioKolorea, float metala,
            float leuntasuna, Vector2 tile, MaterialGlobalIlluminationFlags gi, bool aplikatu, List<string> a)
        {
            string bidea = $"{MaterialKarpeta}/{izena}.mat";
            var materiala = AssetDatabase.LoadAssetAtPath<Material>(bidea);
            Texture2D tAlbedo = albedo != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TesturaKarpeta}/{albedo}.png") : null;
            Texture2D tEmisioa = emisioa != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TesturaKarpeta}/{emisioa}.png") : null;
            bool emititzen = emisioKolorea.maxColorComponent > 0f;

            bool zuzena = materiala != null
                          && materiala.GetTexture("_BaseMap") == tAlbedo
                          && materiala.GetTexture("_EmissionMap") == tEmisioa
                          && materiala.GetColor("_BaseColor") == oinarria
                          && KoloreBerdina(materiala.GetColor("_EmissionColor"), emisioKolorea)
                          && Mathf.Abs(materiala.GetFloat("_Metallic") - metala) < Tolerantzia
                          && Mathf.Abs(materiala.GetFloat("_Smoothness") - leuntasuna) < Tolerantzia
                          && materiala.globalIlluminationFlags == gi
                          && materiala.IsKeywordEnabled("_EMISSION") == emititzen;
            if (zuzena)
            {
                return materiala;
            }

            a.Add($"{bidea}: {(materiala != null ? "eguneratu" : "sortu")}");
            if (!aplikatu)
            {
                return materiala;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP/Lit shader-a ez da aurkitu.");
            }

            bool berria = materiala == null;
            materiala ??= new Material(shader);
            materiala.SetTexture("_BaseMap", tAlbedo);
            materiala.SetColor("_BaseColor", oinarria);
            materiala.SetTexture("_EmissionMap", tEmisioa);
            materiala.SetColor("_EmissionColor", emisioKolorea);
            materiala.SetFloat("_Metallic", metala);
            materiala.SetFloat("_Smoothness", leuntasuna);
            materiala.SetTextureScale("_BaseMap", tile);
            if (emititzen)
            {
                materiala.EnableKeyword("_EMISSION");
            }
            else
            {
                materiala.DisableKeyword("_EMISSION");
            }

            materiala.globalIlluminationFlags = gi;
            if (berria)
            {
                AssetDatabase.CreateAsset(materiala, bidea);
            }
            else
            {
                EditorUtility.SetDirty(materiala);
            }

            return materiala;
        }

        private static bool KoloreBerdina(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
        }

        private static Mesh MeshGorde(string izena, Mesh mesh, bool aplikatu, List<string> a)
        {
            string bidea = $"{EreduKarpeta}/{izena}.asset";
            mesh.name = izena;

            // UV2-k erpinak bana ditzake: konparatu aurretik sortu, bestela malla beti desberdina dirudi.
            Unwrapping.GenerateSecondaryUVSet(mesh);
            var zaharra = AssetDatabase.LoadAssetAtPath<Mesh>(bidea);
            bool berdina = zaharra != null
                           && zaharra.vertices.SequenceEqual(mesh.vertices)
                           && zaharra.triangles.SequenceEqual(mesh.triangles)
                           && zaharra.uv.SequenceEqual(mesh.uv)
                           && zaharra.normals.SequenceEqual(mesh.normals)
                           && zaharra.HasVertexAttribute(VertexAttribute.TexCoord1);
            if (berdina)
            {
                return zaharra;
            }

            a.Add($"{bidea}: {(zaharra != null ? "eguneratu" : "sortu")}");
            if (!aplikatu)
            {
                return zaharra;
            }

            if (zaharra == null)
            {
                AssetDatabase.CreateAsset(mesh, bidea);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, zaharra);
            EditorUtility.SetDirty(zaharra);
            return zaharra;
        }

        private static GameObject PrefabGorde(string izena, Mesh mesh, Material materiala, bool kolisioa, bool aplikatu, List<string> a)
        {
            string bidea = $"{Erroa}/{izena}.prefab";
            var zaharra = AssetDatabase.LoadAssetAtPath<GameObject>(bidea);
            bool zuzena = zaharra != null
                          && mesh != null
                          && zaharra.GetComponent<MeshFilter>()?.sharedMesh == mesh
                          && zaharra.GetComponent<MeshRenderer>()?.sharedMaterial == materiala
                          && (zaharra.GetComponent<MeshCollider>() != null) == kolisioa;
            if (zuzena)
            {
                return zaharra;
            }

            a.Add($"{bidea}: {(zaharra != null ? "eguneratu" : "sortu")}");
            if (!aplikatu)
            {
                return zaharra;
            }

            var objektua = new GameObject(izena);
            try
            {
                objektua.AddComponent<MeshFilter>().sharedMesh = mesh;
                objektua.AddComponent<MeshRenderer>().sharedMaterial = materiala;
                if (kolisioa)
                {
                    objektua.AddComponent<MeshCollider>().sharedMesh = mesh;
                }

                GameObjectUtility.SetStaticEditorFlags(objektua,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic);
                return PrefabUtility.SaveAsPrefabAsset(objektua, bidea);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objektua);
            }
        }

        // ---------- Ingurunea: lighting settings, zerua, argi nagusia ----------

        private static void InguruneaSinkronizatu(bool aplikatu, List<string> a)
        {
            var ezarpenak = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingBidea);
            if (ezarpenak == null)
            {
                a.Add($"{LightingBidea}: sortu");
                if (aplikatu)
                {
                    ezarpenak = new LightingSettings { name = "ECLIPSE_Argiztapena" };
                    LightingEzarri(ezarpenak);
                    AssetDatabase.CreateAsset(ezarpenak, LightingBidea);
                }
            }
            else if (!LightingZuzena(ezarpenak))
            {
                a.Add($"{LightingBidea}: eguneratu");
                if (aplikatu)
                {
                    LightingEzarri(ezarpenak);
                    EditorUtility.SetDirty(ezarpenak);
                }
            }

            // lightingSettings irakurtzeak salbuespena ematen du eszenak asetik ez badu: TryGet erabili.
            bool badu = Lightmapping.TryGetLightingSettings(out LightingSettings unekoa);
            if (ezarpenak != null && (!badu || unekoa != ezarpenak))
            {
                a.Add("Eszena: ECLIPSE_Argiztapena esleitu");
                if (aplikatu)
                {
                    Lightmapping.lightingSettings = ezarpenak;
                }
            }

            var zerua = AssetDatabase.LoadAssetAtPath<Material>(ZeruBidea);
            if (zerua == null)
            {
                a.Add($"{ZeruBidea}: sortu");
                if (aplikatu)
                {
                    Shader shader = Shader.Find("Skybox/Procedural") ?? throw new InvalidOperationException("Skybox/Procedural ez da aurkitu.");
                    zerua = new Material(shader);
                    zerua.SetFloat("_SunSize", 0.02f);
                    zerua.SetFloat("_AtmosphereThickness", 0.35f);
                    zerua.SetColor("_SkyTint", new Color(0.18f, 0.24f, 0.42f));
                    zerua.SetColor("_GroundColor", new Color(0.03f, 0.03f, 0.05f));
                    zerua.SetFloat("_Exposure", 0.45f);
                    AssetDatabase.CreateAsset(zerua, ZeruBidea);
                }
            }

            bool zeruZuzena = zerua != null
                              && RenderSettings.skybox == zerua
                              && RenderSettings.ambientMode == AmbientMode.Skybox
                              && Mathf.Abs(RenderSettings.ambientIntensity - 0.35f) < Tolerantzia;
            if (!zeruZuzena)
            {
                a.Add("Eszena: M_Zerua eta inguruneko argia (Ctrl+Z gabe)");
                if (aplikatu)
                {
                    RenderSettings.skybox = zerua;
                    RenderSettings.ambientMode = AmbientMode.Skybox;
                    RenderSettings.ambientIntensity = 0.35f;
                }
            }

            GameObject argiObjektua = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.name == "Directional Light");
            Light argia = argiObjektua != null ? argiObjektua.GetComponent<Light>() : null;
            if (argia == null)
            {
                a.Add("OHARRA: 'Directional Light' ez dago eszenan; omitituta.");
                return;
            }

            Color ilargia = new Color(0.55f, 0.65f, 0.95f);
            bool argiZuzena = argia.lightmapBakeType == LightmapBakeType.Mixed
                              && Mathf.Abs(argia.intensity - 0.25f) < Tolerantzia
                              && KoloreBerdina(argia.color, ilargia)
                              && RenderSettings.sun == argia;
            if (!argiZuzena)
            {
                a.Add("Directional Light: Mixed, ilargi-argia");
                if (aplikatu)
                {
                    Undo.RecordObject(argia, Tresna);
                    argia.lightmapBakeType = LightmapBakeType.Mixed;
                    argia.intensity = 0.25f;
                    argia.color = ilargia;
                    RenderSettings.sun = argia;
                }
            }
        }

        private static bool LightingZuzena(LightingSettings e)
        {
            var erreferentzia = new LightingSettings();
            LightingEzarri(erreferentzia);
            bool zuzena = e.lightmapper == erreferentzia.lightmapper
                          && Mathf.Abs(e.lightmapResolution - erreferentzia.lightmapResolution) < Tolerantzia
                          && e.lightmapMaxSize == erreferentzia.lightmapMaxSize
                          && e.lightmapPadding == erreferentzia.lightmapPadding
                          && e.maxBounces == erreferentzia.maxBounces
                          && e.directSampleCount == erreferentzia.directSampleCount
                          && e.indirectSampleCount == erreferentzia.indirectSampleCount
                          && e.environmentSampleCount == erreferentzia.environmentSampleCount
                          && e.mixedBakeMode == erreferentzia.mixedBakeMode
                          && e.bakedGI
                          && !e.realtimeGI;
            UnityEngine.Object.DestroyImmediate(erreferentzia);
            return zuzena;
        }

        private static void LightingEzarri(LightingSettings e)
        {
            e.lightmapper = LightingSettings.Lightmapper.UnityComputeGPU;
            e.lightmapResolution = 32f;
            e.lightmapMaxSize = 2048;
            e.lightmapPadding = 4;
            e.maxBounces = 3;
            e.directSampleCount = 64;
            e.indirectSampleCount = 256;
            e.environmentSampleCount = 128;
            e.mixedBakeMode = MixedLightingMode.IndirectOnly;
            e.bakedGI = true;
            e.realtimeGI = false;
        }

        // ---------- Eszena: kokapenak eta argiak ----------

        private static void EszenaSinkronizatu(Dictionary<string, GameObject> prefabak, bool aplikatu, List<string> a)
        {
            Scene eszena = SceneManager.GetActiveScene();
            Transform s1 = SektoreaLortu(eszena, "Sektorea1");
            Transform s2 = SektoreaLortu(eszena, "Sektorea2");
            Transform s3 = SektoreaLortu(eszena, "Sektorea3");

            Gela lab = GelaLortu(s1, "Gela_S1_Laborategia");
            Gela sarrera = GelaLortu(s1, "Gela_S1_Sarrera");
            Gela fusibleak = GelaLortu(s2, "Gela_S2_Fusibleak");
            Gela sorgailua = GelaLortu(s2, "Gela_S2_Sorgailua");
            Gela erreaktorea = GelaLortu(s3, "Gela_S3_Erreaktorea");

            float h = lab.Tamaina.y;
            var kokapenak = new List<Kokapena>();
            var argiak = new List<ArgiDef>();

            // --- Sektorea1: Laborategia. Nukleo propioa zoru metalikoaren gainean.
            var nukleoPos = new Vector3(lab.Erdia.x + 0.5f, 0f, 3.5f);
            kokapenak.Add(new Kokapena(s1, "Laborategia", "PF_Zorua_8x8", new Vector3(lab.Erdia.x + 0.5f, 0.012f, 3f), Quaternion.identity));
            kokapenak.Add(new Kokapena(s1, "Laborategia", "PF_DatuNukleoa", nukleoPos, Quaternion.identity));
            kokapenak.Add(new Kokapena(s1, "Laborategia", "PF_Trim_Banda_18", new Vector3(lab.Erdia.x, 2.5f, lab.ZMax - 0.02f), Quaternion.Euler(0f, 0f, 0f)));
            kokapenak.Add(new Kokapena(s1, "Laborategia", "PF_Trim_Banda_18", new Vector3(lab.Erdia.x, 2.5f, lab.ZMin + 0.02f), Quaternion.Euler(0f, 180f, 0f)));
            kokapenak.Add(new Kokapena(s1, "Laborategia", "PF_Trim_Hazard", new Vector3(lab.XMin + 0.6f, 0.012f, 0f), Quaternion.Euler(0f, 90f, 0f)));
            kokapenak.Add(new Kokapena(s1, "Sarrera", "PF_Trim_Hazard", new Vector3(sarrera.XMax - 0.6f, 0.012f, 0f), Quaternion.Euler(0f, 90f, 0f)));
            kokapenak.Add(new Kokapena(s1, "Sarrera", "PF_Trim_Banda_11", new Vector3(sarrera.Erdia.x, 2.5f, sarrera.ZMax - 0.02f), Quaternion.Euler(0f, 0f, 0f)));
            kokapenak.Add(new Kokapena(s1, "Sarrera", "PF_Trim_Banda_11", new Vector3(sarrera.Erdia.x, 2.5f, sarrera.ZMin + 0.02f), Quaternion.Euler(0f, 180f, 0f)));

            // Laborategia: 3x3 panel hotz horneatu (argi nagusia).
            for (int ix = -1; ix <= 1; ix++)
            {
                for (int iz = -1; iz <= 1; iz++)
                {
                    var pos = new Vector3(lab.Erdia.x + ix * 6f, h - 0.03f, iz * 6f);
                    kokapenak.Add(new Kokapena(s1, "Laborategia", "PF_ArgiPanela_Hotza", pos, Quaternion.identity));
                    argiak.Add(ArgiDef.Panela(s1, "Laborategia", $"Argia_Baked_Panela_{ix + 1}{iz + 1}", pos + Vector3.down * 0.03f, Kolorea(6500f), 18f));
                }
            }

            // Sarrera: 2x2 panel epel horneatu.
            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    var pos = new Vector3(sarrera.Erdia.x + ix * 3f, h - 0.03f, iz * 3f);
                    kokapenak.Add(new Kokapena(s1, "Sarrera", "PF_ArgiPanela_Epela", pos, Quaternion.identity));
                    argiak.Add(ArgiDef.Panela(s1, "Sarrera", $"Argia_Baked_Panela_{ix + 1}{iz + 1}", pos + Vector3.down * 0.03f, Kolorea(3800f), 14f));
                }
            }

            // Realtime: nukleoaren distira zianoa eta ordenagailuaren gaineko foku epela (itzal dinamikoekin).
            argiak.Add(ArgiDef.Puntua(s1, "Laborategia", "Argia_Realtime_Nukleoa", nukleoPos + Vector3.up * 1.6f, new Color(0.25f, 0.85f, 1f), 2.2f, 7f, LightmapBakeType.Realtime, LightShadows.None));
            Transform ordenagailua = s1.Find("Geometria/Laborategia/Propak/Old_computer__000");
            if (ordenagailua != null)
            {
                Bounds k = KaxaLortu(ordenagailua.gameObject);
                argiak.Add(ArgiDef.Fokua(s1, "Laborategia", "Argia_Realtime_Terminala", new Vector3(k.center.x, 3.6f, k.center.z), Quaternion.Euler(90f, 0f, 0f), Kolorea(3000f), 9f, 7f, 55f, LightmapBakeType.Realtime, LightShadows.Soft));
            }
            else
            {
                a.Add("OHARRA: Old_computer__000 ez dago; terminalaren fokua omitituta.");
            }

            // --- Sektorea2: Energia. Itzalaldia: argi gorri horneatuak.
            Color gorria = new Color(1f, 0.12f, 0.08f);
            SalaArgiak(s2, "Fusibleak", fusibleak, 1, 3.2f, gorria, 5f, 9f, argiak);
            SalaArgiak(s2, "Sorgailua", sorgailua, 1, 5.5f, gorria, 7f, 12f, argiak);

            // --- Sektorea3: Erreaktorea. Oinarri laranja horneatua eta bi baliza realtime.
            Color laranja = new Color(1f, 0.48f, 0.12f);
            SalaArgiak(s3, "Erreaktorea", erreaktorea, 1, 6f, laranja, 9f, 15f, argiak);
            argiak.Add(ArgiDef.Fokua(s3, "Erreaktorea", "Argia_Realtime_Baliza_0", new Vector3(erreaktorea.Erdia.x - 5f, 4.2f, -5f), Quaternion.Euler(60f, 45f, 0f), new Color(1f, 0.1f, 0.05f), 6f, 16f, 45f, LightmapBakeType.Realtime, LightShadows.None));
            argiak.Add(ArgiDef.Fokua(s3, "Erreaktorea", "Argia_Realtime_Baliza_1", new Vector3(erreaktorea.Erdia.x + 5f, 4.2f, 5f), Quaternion.Euler(60f, 225f, 0f), new Color(1f, 0.1f, 0.05f), 6f, 16f, 45f, LightmapBakeType.Realtime, LightShadows.None));

            // Seinaleak: atearen gainean, salaren barruan.
            kokapenak.Add(Seinalea(s1, "Sarrera", "SARRERA", sarrera, true));
            kokapenak.Add(Seinalea(s1, "Laborategia", "LABORATEGIA", lab, false));
            kokapenak.Add(Seinalea(s2, "Fusibleak", "FUSIBLEAK", fusibleak, true));
            kokapenak.Add(Seinalea(s2, "Sorgailua", "SORGAILUA", sorgailua, false));
            kokapenak.Add(Seinalea(s3, "Erreaktorea", "ERREAKTOREA", erreaktorea, false));

            var mantendu = new HashSet<Transform>();
            var kontagailuak = new Dictionary<string, int>();
            foreach (Kokapena k in kokapenak)
            {
                KokapenaSinkronizatu(k, prefabak, kontagailuak, mantendu, aplikatu, a);
            }

            foreach (ArgiDef argi in argiak)
            {
                ArgiaSinkronizatu(argi, mantendu, aplikatu, a);
            }

            // S2 eta S3 blockout dira: lightmap-eko bereizmena S1-erako gorde.
            int eskalatuak = 0;
            foreach (Transform sektorea in new[] { s2, s3 })
            {
                foreach (MeshRenderer renderer in sektorea.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (Mathf.Abs(renderer.scaleInLightmap - BlockoutLightmapEskala) < Tolerantzia)
                    {
                        continue;
                    }

                    eskalatuak++;
                    if (aplikatu)
                    {
                        Undo.RecordObject(renderer, Tresna);
                        renderer.scaleInLightmap = BlockoutLightmapEskala;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                }
            }

            if (eskalatuak > 0)
            {
                a.Add($"S2 eta S3: {eskalatuak} renderer, lightmap eskala {BlockoutLightmapEskala}");
            }

            foreach (Transform sektorea in new[] { s1, s2, s3 })
            {
                ZaharrakEzabatu(sektorea, ArgiakKontainerra, "Argia_", mantendu, aplikatu, a);
                ZaharrakEzabatu(sektorea, PropioakKontainerra, "PF_", mantendu, aplikatu, a);
            }
        }

        private static Kokapena Seinalea(Transform sektorea, string sala, string testua, Gela gela, bool ateaEkialdean)
        {
            float x = ateaEkialdean ? gela.XMax - 0.03f : gela.XMin + 0.03f;
            // Malla -Z aldera begira dago: LookRotation-ek barrurantz jartzen du.
            Quaternion biraketa = Quaternion.LookRotation(ateaEkialdean ? Vector3.right : Vector3.left);
            return new Kokapena(sektorea, sala, $"PF_Seinalea_{testua}", new Vector3(x, 3.95f, 0f), biraketa);
        }

        private static void SalaArgiak(Transform sektorea, string sala, Gela gela, int erdia, float tartea, Color kolorea, float intentsitatea, float tartea2, List<ArgiDef> argiak)
        {
            for (int ix = -erdia; ix <= erdia; ix++)
            {
                for (int iz = -erdia; iz <= erdia; iz++)
                {
                    var pos = new Vector3(gela.Erdia.x + ix * tartea, 4.2f, iz * tartea);
                    argiak.Add(ArgiDef.Puntua(sektorea, sala, $"Argia_Baked_{ix + erdia}{iz + erdia}", pos, kolorea, intentsitatea, tartea2, LightmapBakeType.Baked, LightShadows.Soft));
                }
            }
        }

        private static Transform SektoreaLortu(Scene eszena, string izena)
        {
            GameObject erroa = eszena.GetRootGameObjects().FirstOrDefault(o => o.name == izena);
            if (erroa == null)
            {
                throw new InvalidOperationException($"{izena} ez dago: exekutatu lehenago ECLIPSE/Eszena/Dena eraiki.");
            }

            return erroa.transform;
        }

        private static Gela GelaLortu(Transform sektorea, string izena)
        {
            Transform t = sektorea.Find(izena) ?? throw new InvalidOperationException($"{sektorea.name}/{izena} ez dago.");
            var k = t.GetComponent<BoxCollider>() ?? throw new InvalidOperationException($"{izena}: ez du BoxCollider-ik.");
            Vector3 erdia = t.localPosition + k.center;
            return new Gela
            {
                Erdia = erdia,
                Tamaina = k.size,
                XMin = erdia.x - k.size.x * 0.5f,
                XMax = erdia.x + k.size.x * 0.5f,
                ZMin = erdia.z - k.size.z * 0.5f,
                ZMax = erdia.z + k.size.z * 0.5f,
            };
        }

        private static Bounds KaxaLortu(GameObject objektua)
        {
            Renderer[] rendererrak = objektua.GetComponentsInChildren<Renderer>(true);
            Bounds kaxa = rendererrak[0].bounds;
            foreach (Renderer renderer in rendererrak)
            {
                kaxa.Encapsulate(renderer.bounds);
            }

            return kaxa;
        }

        private static Transform KontainerraLortu(Transform erroa, string[] bidea, bool aplikatu, List<string> a)
        {
            Transform unekoa = erroa;
            foreach (string zatia in bidea)
            {
                Transform hurrengoa = unekoa != null ? unekoa.Find(zatia) : null;
                if (hurrengoa == null)
                {
                    if (!aplikatu)
                    {
                        return null;
                    }

                    var berria = new GameObject(zatia);
                    berria.transform.SetParent(unekoa, false);
                    Undo.RegisterCreatedObjectUndo(berria, zatia);
                    hurrengoa = berria.transform;
                }

                unekoa = hurrengoa;
            }

            return unekoa;
        }

        private static void KokapenaSinkronizatu(Kokapena k, Dictionary<string, GameObject> prefabak, Dictionary<string, int> kontagailuak,
            HashSet<Transform> mantendu, bool aplikatu, List<string> a)
        {
            string gakoa = $"{k.Sektorea.name}/{k.Sala}/{k.Prefab}";
            kontagailuak.TryGetValue(gakoa, out int indizea);
            kontagailuak[gakoa] = indizea + 1;
            string izena = $"{k.Prefab}__{indizea:000}";
            string bidea = $"{k.Sektorea.name}/{PropioakKontainerra}/{k.Sala}/{izena}";

            Transform kontainerra = KontainerraLortu(k.Sektorea, new[] { PropioakKontainerra, k.Sala }, aplikatu, a);
            Transform aurkitua = kontainerra != null ? kontainerra.Find(izena) : null;
            prefabak.TryGetValue(k.Prefab, out GameObject prefab);

            if (aurkitua != null && prefab != null && PrefabUtility.GetCorrespondingObjectFromSource(aurkitua.gameObject) != prefab)
            {
                a.Add($"{bidea}: ordezkatu");
                if (aplikatu)
                {
                    Undo.DestroyObjectImmediate(aurkitua.gameObject);
                }

                aurkitua = null;
            }

            if (aurkitua == null)
            {
                a.Add($"{bidea}: sortu");
                if (aplikatu)
                {
                    if (prefab == null)
                    {
                        throw new InvalidOperationException($"{k.Prefab} prefab-a ez dago.");
                    }

                    var instantzia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, k.Sektorea.gameObject.scene);
                    instantzia.name = izena;
                    instantzia.transform.SetParent(kontainerra, false);
                    instantzia.transform.SetLocalPositionAndRotation(k.Posizioa, k.Biraketa);
                    Undo.RegisterCreatedObjectUndo(instantzia, izena);
                    mantendu.Add(instantzia.transform);
                }

                return;
            }

            mantendu.Add(aurkitua);
            if ((aurkitua.localPosition - k.Posizioa).sqrMagnitude < Tolerantzia * Tolerantzia
                && Quaternion.Angle(aurkitua.localRotation, k.Biraketa) < 0.05f)
            {
                return;
            }

            a.Add($"{bidea}: eguneratu");
            if (aplikatu)
            {
                Undo.RecordObject(aurkitua, izena);
                aurkitua.SetLocalPositionAndRotation(k.Posizioa, k.Biraketa);
                PrefabUtility.RecordPrefabInstancePropertyModifications(aurkitua);
            }
        }

        private static void ArgiaSinkronizatu(ArgiDef d, HashSet<Transform> mantendu, bool aplikatu, List<string> a)
        {
            string bidea = $"{d.Sektorea.name}/{ArgiakKontainerra}/{d.Sala}/{d.Izena}";
            Transform kontainerra = KontainerraLortu(d.Sektorea, new[] { ArgiakKontainerra, d.Sala }, aplikatu, a);
            Transform aurkitua = kontainerra != null ? kontainerra.Find(d.Izena) : null;
            Light argia = aurkitua != null ? aurkitua.GetComponent<Light>() : null;

            if (aurkitua == null)
            {
                a.Add($"{bidea}: sortu ({d.Bake}, {d.Mota})");
                if (aplikatu)
                {
                    var berria = new GameObject(d.Izena);
                    berria.transform.SetParent(kontainerra, false);
                    d.Ezarri(berria.transform, berria.AddComponent<Light>());
                    Undo.RegisterCreatedObjectUndo(berria, d.Izena);
                    mantendu.Add(berria.transform);
                }

                return;
            }

            mantendu.Add(aurkitua);
            if (argia != null && d.Berdina(aurkitua, argia))
            {
                return;
            }

            a.Add($"{bidea}: eguneratu");
            if (aplikatu)
            {
                Undo.RecordObject(aurkitua, d.Izena);
                argia ??= Undo.AddComponent<Light>(aurkitua.gameObject);
                Undo.RecordObject(argia, d.Izena);
                d.Ezarri(aurkitua, argia);
            }
        }

        private static void ZaharrakEzabatu(Transform sektorea, string kontainerra, string aurrizkia, HashSet<Transform> mantendu, bool aplikatu, List<string> a)
        {
            Transform erroa = sektorea.Find(kontainerra);
            if (erroa == null)
            {
                return;
            }

            foreach (Transform t in erroa.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.StartsWith(aurrizkia, StringComparison.Ordinal) && !mantendu.Contains(t))
                {
                    a.Add($"{sektorea.name}/{kontainerra}/{t.name}: ezabatu");
                    if (aplikatu)
                    {
                        Undo.DestroyObjectImmediate(t.gameObject);
                    }
                }
            }
        }

        private static Color Kolorea(float kelvin) => Mathf.CorrelatedColorTemperatureToRGB(kelvin);

        // ---------- Datuak ----------

        private struct Gela
        {
            public Vector3 Erdia;
            public Vector3 Tamaina;
            public float XMin;
            public float XMax;
            public float ZMin;
            public float ZMax;
        }

        private readonly struct Kokapena
        {
            public readonly Transform Sektorea;
            public readonly string Sala;
            public readonly string Prefab;
            public readonly Vector3 Posizioa;
            public readonly Quaternion Biraketa;

            public Kokapena(Transform sektorea, string sala, string prefab, Vector3 posizioa, Quaternion biraketa)
            {
                Sektorea = sektorea;
                Sala = sala;
                Prefab = prefab;
                Posizioa = posizioa;
                Biraketa = biraketa;
            }
        }

        private sealed class ArgiDef
        {
            public Transform Sektorea;
            public string Sala;
            public string Izena;
            public LightType Mota;
            public LightmapBakeType Bake;
            public Vector3 Posizioa;
            public Quaternion Biraketa;
            public Color Kolore;
            public float Intentsitatea;
            public float Tartea = 10f;
            public float Angelua = 30f;
            public Vector2 Tamaina = Vector2.one;
            public LightShadows Itzalak = LightShadows.Soft;

            public static ArgiDef Panela(Transform s, string sala, string izena, Vector3 pos, Color kolorea, float intentsitatea) => new ArgiDef
            {
                Sektorea = s, Sala = sala, Izena = izena, Mota = LightType.Rectangle, Bake = LightmapBakeType.Baked,
                Posizioa = pos, Biraketa = Quaternion.Euler(90f, 0f, 0f), Kolore = kolorea, Intentsitatea = intentsitatea,
                Tartea = 18f, Tamaina = new Vector2(2f, 1f),
            };

            public static ArgiDef Puntua(Transform s, string sala, string izena, Vector3 pos, Color kolorea, float intentsitatea, float tartea, LightmapBakeType bake, LightShadows itzalak) => new ArgiDef
            {
                Sektorea = s, Sala = sala, Izena = izena, Mota = LightType.Point, Bake = bake, Posizioa = pos,
                Biraketa = Quaternion.identity, Kolore = kolorea, Intentsitatea = intentsitatea, Tartea = tartea, Itzalak = itzalak,
            };

            public static ArgiDef Fokua(Transform s, string sala, string izena, Vector3 pos, Quaternion biraketa, Color kolorea, float intentsitatea, float tartea, float angelua, LightmapBakeType bake, LightShadows itzalak) => new ArgiDef
            {
                Sektorea = s, Sala = sala, Izena = izena, Mota = LightType.Spot, Bake = bake, Posizioa = pos, Biraketa = biraketa,
                Kolore = kolorea, Intentsitatea = intentsitatea, Tartea = tartea, Angelua = angelua, Itzalak = itzalak,
            };

            public void Ezarri(Transform t, Light l)
            {
                t.SetLocalPositionAndRotation(Posizioa, Biraketa);
                l.type = Mota;
                l.lightmapBakeType = Bake;
                l.color = Kolore;
                l.intensity = Intentsitatea;
                l.range = Tartea;
                l.shadows = Itzalak;
                if (Mota == LightType.Spot)
                {
                    l.spotAngle = Angelua;
                }

                if (Mota == LightType.Rectangle)
                {
                    l.areaSize = Tamaina;
                }
            }

            public bool Berdina(Transform t, Light l)
            {
                return (t.localPosition - Posizioa).sqrMagnitude < Tolerantzia * Tolerantzia
                       && Quaternion.Angle(t.localRotation, Biraketa) < 0.05f
                       && l.type == Mota
                       && l.lightmapBakeType == Bake
                       && KoloreBerdina(l.color, Kolore)
                       && Mathf.Abs(l.intensity - Intentsitatea) < Tolerantzia
                       && Mathf.Abs(l.range - Tartea) < Tolerantzia
                       && l.shadows == Itzalak
                       && (Mota != LightType.Spot || Mathf.Abs(l.spotAngle - Angelua) < Tolerantzia)
                       && (Mota != LightType.Rectangle || (l.areaSize - Tamaina).sqrMagnitude < Tolerantzia);
            }
        }
    }

    /// <summary>Malla propioak: pibota oinarrian, 1 unitate = 1 m, normal esplizituak eta ertz gogorrak erpinak banatuta.</summary>
    internal static class MeshProzedurala
    {
        private sealed class Eraikitzailea
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            /// <summary>Erpinak erlojuaren orratzen noranzkoan, aurpegia ikusten den aldetik.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                Vector3 normala = Vector3.Cross(b - a, c - a).normalized;
                int i = V.Count;
                V.AddRange(new[] { a, b, c, d });
                N.AddRange(new[] { normala, normala, normala, normala });
                UV.AddRange(new[] { ua, ub, uc, ud });
                T.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            /// <summary>Triangelua: erpinak erlojuaren orratzen noranzkoan, aurpegia ikusten den aldetik.</summary>
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                Vector3 normala = Vector3.Cross(b - a, c - a).normalized;
                int i = V.Count;
                V.AddRange(new[] { a, b, c });
                N.AddRange(new[] { normala, normala, normala });
                UV.AddRange(new[] { ua, ub, uc });
                T.AddRange(new[] { i, i + 1, i + 2 });
            }

            public Mesh Mesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(V);
                mesh.SetNormals(N);
                mesh.SetUVs(0, UV);
                mesh.SetTriangles(T, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }

        private const int Aldeak = 8;

        private static Vector3 Puntua(int i, float r, float y)
        {
            float angelua = (i + 0.5f) * Mathf.PI * 2f / Aldeak;
            return new Vector3(Mathf.Sin(angelua) * r, y, Mathf.Cos(angelua) * r);
        }

        private static void Eraztuna(Eraikitzailea e, float r0, float r1, float y0, float y1, float v0, float v1)
        {
            for (int i = 0; i < Aldeak; i++)
            {
                float u0 = (float)i / Aldeak;
                float u1 = (float)(i + 1) / Aldeak;
                // Kanpotik ikusita: behe-ezkerra, goi-ezkerra, goi-eskuina, behe-eskuina.
                e.Quad(Puntua(i, r0, y0), Puntua(i, r1, y1), Puntua(i + 1, r1, y1), Puntua(i + 1, r0, y0),
                    new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0));
            }
        }

        /// <summary>Tapa: triangeluak erdiko erpinetik, ertz bera erabiliz eraztunarekin (T-junturarik gabe).</summary>
        private static void Estalkia(Eraikitzailea e, float r, float y, float v0, float v1, bool goian)
        {
            var erdia = new Vector3(0f, y, 0f);
            for (int i = 0; i < Aldeak; i++)
            {
                Vector3 a = Puntua(i, r, y);
                Vector3 b = Puntua(i + 1, r, y);
                float u0 = (float)i / Aldeak;
                float u1 = (float)(i + 1) / Aldeak;
                var uvErdia = new Vector2((u0 + u1) * 0.5f, v0);
                if (goian)
                {
                    e.Tri(erdia, a, b, uvErdia, new Vector2(u0, v1), new Vector2(u1, v1));
                }
                else
                {
                    e.Tri(erdia, b, a, uvErdia, new Vector2(u1, v1), new Vector2(u0, v1));
                }
            }
        }

        /// <summary>Oinarria, zutabea eta burua; UV-ak testura bakarrean, gainjarri gabe (unique texture).</summary>
        public static Mesh DatuNukleoa()
        {
            var e = new Eraikitzailea();
            Eraztuna(e, 1.2f, 1.2f, 0f, 0.25f, 0.00f, 0.08f);
            Eraztuna(e, 1.2f, 0.8f, 0.25f, 0.45f, 0.08f, 0.16f);
            Eraztuna(e, 0.8f, 0.8f, 0.45f, 3.05f, 0.16f, 0.80f);
            Eraztuna(e, 0.8f, 1.05f, 3.05f, 3.25f, 0.80f, 0.87f);
            Eraztuna(e, 1.05f, 1.05f, 3.25f, 3.5f, 0.87f, 0.94f);
            Estalkia(e, 1.05f, 3.5f, 0.94f, 1f, true);
            Estalkia(e, 1.2f, 0f, 0.94f, 1f, false);
            return e.Mesh();
        }

        /// <summary>Zoru-plaka: tile &gt; 0 bada, UV-ak errepikatzen dira (tileable); bestela lerro bat trim sheet-etik.</summary>
        public static Mesh ZoruPlaka(float x, float z, float tile, float v0 = 0f, float v1 = 1f, float uLuzera = 1f)
        {
            var e = new Eraikitzailea();
            float hx = x * 0.5f;
            float hz = z * 0.5f;
            float u1 = tile > 0f ? tile : uLuzera;
            float v1Plaka = tile > 0f ? tile * z / x : v1;
            float v0Plaka = tile > 0f ? 0f : v0;
            e.Quad(new Vector3(-hx, 0f, -hz), new Vector3(-hx, 0f, hz), new Vector3(hx, 0f, hz), new Vector3(hx, 0f, -hz),
                new Vector2(0f, v0Plaka), new Vector2(0f, v1Plaka), new Vector2(u1, v1Plaka), new Vector2(u1, v0Plaka));
            return e.Mesh();
        }

        /// <summary>Horma-banda: -Z aldera begira, U errepikatuz eta V trim sheet-eko lerroan.</summary>
        public static Mesh Banda(float luzera, float altuera, float v0, float v1)
        {
            var e = new Eraikitzailea();
            float hx = luzera * 0.5f;
            float hy = altuera * 0.5f;
            e.Quad(new Vector3(-hx, -hy, 0f), new Vector3(-hx, hy, 0f), new Vector3(hx, hy, 0f), new Vector3(hx, -hy, 0f),
                new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(luzera, v1), new Vector2(luzera, v0));
            return e.Mesh();
        }

        /// <summary>Sabaiko panela: behera begira.</summary>
        public static Mesh Panela(float x, float z)
        {
            var e = new Eraikitzailea();
            float hx = x * 0.5f;
            float hz = z * 0.5f;
            e.Quad(new Vector3(-hx, 0f, hz), new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, hz),
                Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            return e.Mesh();
        }

        /// <summary>Seinalea: -Z aldera begira, testura osoarekin.</summary>
        public static Mesh Seinalea(float x, float y)
        {
            var e = new Eraikitzailea();
            float hx = x * 0.5f;
            float hy = y * 0.5f;
            e.Quad(new Vector3(-hx, -hy, 0f), new Vector3(-hx, hy, 0f), new Vector3(hx, hy, 0f), new Vector3(hx, -hy, 0f),
                Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
            return e.Mesh();
        }
    }

    /// <summary>Testura propioak kodez: deterministak (PNG bera exekuzio guztietan), lizentziarik gabe.</summary>
    internal static class TesturaProzedurala
    {
        private delegate Color Pixela(float u, float v);

        private static byte[] Sortu(int w, int h, Pixela funtzioa)
        {
            var testura = new Texture2D(w, h, TextureFormat.RGB24, false);
            var pixelak = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixelak[y * w + x] = funtzioa((x + 0.5f) / w, (y + 0.5f) / h);
                }
            }

            testura.SetPixels(pixelak);
            testura.Apply();
            byte[] png = testura.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(testura);
            return png;
        }

        private static float Zarata(float u, float v, float eskala) => Mathf.PerlinNoise(u * eskala + 13.1f, v * eskala + 7.7f);

        // --- Letra-tipo propioa, 5x7: ez dago lizentziarik eta ez du Font-ik behar.
        private static readonly Dictionary<char, string> Letrak = new Dictionary<char, string>
        {
            ['A'] = "01110 10001 10001 11111 10001 10001 10001", ['B'] = "11110 10001 10001 11110 10001 10001 11110",
            ['C'] = "01111 10000 10000 10000 10000 10000 01111", ['D'] = "11110 10001 10001 10001 10001 10001 11110",
            ['E'] = "11111 10000 10000 11110 10000 10000 11111", ['F'] = "11111 10000 10000 11110 10000 10000 10000",
            ['G'] = "01111 10000 10000 10111 10001 10001 01111", ['I'] = "11111 00100 00100 00100 00100 00100 11111",
            ['K'] = "10001 10010 10100 11000 10100 10010 10001", ['L'] = "10000 10000 10000 10000 10000 10000 11111",
            ['O'] = "01110 10001 10001 10001 10001 10001 01110", ['P'] = "11110 10001 10001 11110 10000 10000 10000",
            ['R'] = "11110 10001 10001 11110 10100 10010 10001", ['S'] = "01111 10000 10000 01110 00001 00001 11110",
            ['T'] = "11111 00100 00100 00100 00100 00100 00100", ['U'] = "10001 10001 10001 10001 10001 10001 01110",
            ['Z'] = "11111 00001 00010 00100 01000 10000 11111",
            ['V'] = "10001 10001 10001 10001 10001 01010 00100", ['W'] = "10001 10001 10001 10101 10101 11011 10001",
            ['X'] = "10001 10001 01010 00100 01010 10001 10001", ['Y'] = "10001 10001 01010 00100 00100 00100 00100",
            ['H'] = "10001 10001 10001 11111 10001 10001 10001", ['J'] = "00111 00010 00010 00010 00010 10010 01100",
            ['M'] = "10001 11011 10101 10101 10001 10001 10001", ['N'] = "10001 11001 10101 10011 10001 10001 10001",
            ['0'] = "01110 10001 10011 10101 11001 10001 01110", ['1'] = "00100 01100 00100 00100 00100 00100 01110",
            ['2'] = "01110 10001 00001 00010 00100 01000 11111", ['3'] = "11110 00001 00001 01110 00001 00001 11110",
            ['4'] = "00010 00110 01010 10010 11111 00010 00010", ['5'] = "11111 10000 11110 00001 00001 10001 01110",
            ['6'] = "00110 01000 10000 11110 10001 10001 01110", ['7'] = "11111 00001 00010 00100 01000 01000 01000",
            ['8'] = "01110 10001 10001 01110 10001 10001 01110", ['9'] = "01110 10001 10001 01111 00001 00010 01100",
        };

        /// <summary>Testua (u,v) puntuan, laukizuzen batean zentratuta. Letra ezezaguna: hutsik.</summary>
        private static bool TestuanDago(string testua, float u, float v, float u0, float u1, float v0, float v1)
        {
            if (u < u0 || u >= u1 || v < v0 || v >= v1)
            {
                return false;
            }

            float zutabea = (u - u0) / (u1 - u0) * (testua.Length * 6 - 1);
            int letra = (int)(zutabea / 6f);
            int px = (int)zutabea - letra * 6;
            int py = 6 - (int)((v - v0) / (v1 - v0) * 7f);
            if (letra >= testua.Length || px >= 5 || py < 0 || py > 6 || !Letrak.TryGetValue(testua[letra], out string lerroak))
            {
                return false;
            }

            return lerroak.Split(' ')[py][px] == '1';
        }

        // --- Datu-nukleoa: unique texture. V banda bakoitza mallaren atal bat da.
        private static bool NukleoArgia(float u, float v, out bool testua)
        {
            testua = false;
            if (v < 0.16f || v >= 0.80f)
            {
                return false;
            }

            float aurpegia = u * 8f;
            int zenbakia = (int)aurpegia;
            float lokala = aurpegia - zenbakia;
            float altuera = (v - 0.16f) / 0.64f;
            if (zenbakia % 2 == 0)
            {
                bool zirrikitua = lokala > 0.42f && lokala < 0.58f && altuera > 0.06f && altuera < 0.94f;
                bool etena = Mathf.Repeat(altuera * 14f, 1f) < 0.12f;
                return zirrikitua && !etena;
            }

            testua = TestuanDago("ECLIPSE", 1f - altuera, lokala, 0.08f, 0.92f, 0.28f, 0.72f);
            return testua;
        }

        public static byte[] NukleoAlbedo() => Sortu(1024, 1024, (u, v) =>
        {
            float z = Zarata(u, v, 22f) * 0.5f + Zarata(u, v, 70f) * 0.5f;
            Color metala = Color.Lerp(new Color(0.16f, 0.18f, 0.21f), new Color(0.34f, 0.37f, 0.41f), z);
            bool ertza = Mathf.Abs(Mathf.Repeat(u * 8f, 1f) - 0.5f) > 0.47f;
            if (ertza)
            {
                metala *= 0.45f;
            }

            if (v < 0.16f && Mathf.Repeat(u * 32f, 1f) < 0.5f == Mathf.Repeat(v * 12f, 1f) < 0.5f)
            {
                metala = Color.Lerp(metala, new Color(0.85f, 0.65f, 0.1f), 0.8f);
            }

            return NukleoArgia(u, v, out _) ? new Color(0.75f, 0.95f, 1f) : metala;
        });

        public static byte[] NukleoEmisioa() => Sortu(1024, 1024, (u, v) =>
            NukleoArgia(u, v, out _) ? new Color(0.2f, 0.85f, 1f) : Color.black);

        // --- Tileable: ertzak bat datoz, funtzio periodikoak bakarrik erabiltzen baititu.
        public static byte[] ZoruTile() => Sortu(1024, 1024, (u, v) =>
        {
            float tau = Mathf.PI * 2f;
            float diamantea = Mathf.Sin((u * 8f + v * 8f) * tau) * Mathf.Sin((u * 8f - v * 8f) * tau);
            float marradura = 0.5f + 0.5f * Mathf.Sin(u * tau * 3f) * Mathf.Cos(v * tau * 5f);
            float argia = Mathf.Lerp(0.2f, 0.34f, marradura) + (diamantea > 0.55f ? 0.1f : 0f);
            bool juntura = Mathf.Repeat(u, 1f) < 0.008f || Mathf.Repeat(v, 1f) < 0.008f || u > 0.992f || v > 0.992f;
            return juntura ? new Color(0.05f, 0.05f, 0.06f) : new Color(argia, argia * 1.04f, argia * 1.1f);
        });

        // --- Trim sheet: lau lerro horizontal, U-n errepikagarriak.
        private static bool TrimArgia(float v) => v >= 0.5f && v < 0.75f && Mathf.Abs(v - 0.625f) < 0.05f;

        public static byte[] TrimAlbedo() => Sortu(1024, 1024, (u, v) =>
        {
            if (v < 0.25f)
            {
                // Hazard: marra diagonalak, 16 aldiz errepikatuta.
                return Mathf.Repeat(u * 16f + v * 8f * 2f, 1f) < 0.5f ? new Color(0.95f, 0.72f, 0.05f) : new Color(0.06f, 0.06f, 0.07f);
            }

            if (v < 0.5f)
            {
                float m = 0.42f + 0.1f * Mathf.Sin(u * Mathf.PI * 2f * 8f);
                return Mathf.Abs(v - 0.375f) < 0.09f ? new Color(m, m, m * 1.05f) : new Color(0.15f, 0.15f, 0.17f);
            }

            if (v < 0.75f)
            {
                return TrimArgia(v) ? new Color(0.8f, 0.95f, 1f) : new Color(0.1f, 0.11f, 0.13f);
            }

            float saihetsa = Mathf.Repeat(u * 32f, 1f) < 0.5f ? 0.3f : 0.14f;
            return new Color(saihetsa, saihetsa, saihetsa * 1.1f);
        });

        public static byte[] TrimEmisioa() => Sortu(1024, 1024, (u, v) =>
            TrimArgia(v) ? new Color(0.45f, 0.9f, 1f) : Color.black);

        // --- Seinaleak: unique texture.
        public static byte[] Seinalea(string testua) => Sortu(1024, 256, (u, v) =>
        {
            bool ertza = u < 0.012f || u > 0.988f || v < 0.05f || v > 0.95f;
            if (ertza)
            {
                return new Color(0.95f, 0.72f, 0.05f);
            }

            return TestuanDago(testua, u, v, 0.05f, 0.95f, 0.2f, 0.8f) ? new Color(0.9f, 0.97f, 1f) : new Color(0.02f, 0.03f, 0.04f);
        });
    }
}
