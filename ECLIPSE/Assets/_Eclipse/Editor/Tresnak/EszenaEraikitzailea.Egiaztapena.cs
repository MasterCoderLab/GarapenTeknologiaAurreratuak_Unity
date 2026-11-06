using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Eclipse.Misioak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// Fase 3ko egiaztapenak, batchmode-n eta eszenarik gorde gabe: oklusioa pixelez pixel, denborak, argiztapen-auditoria,
    /// tresnen arauak eta sarrera simulatuarekin interakzioa. Probek ez dute ezer gordetzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string EgiaztapenMarka = "ECL_EGIAZTAPENA";
        private static int _azkenAldaketak;

        // ---------- Oklusioa eta denborak ----------

        /// <summary>Ikuspegi bakoitza oklusioarekin eta gabe errendatu eta alderatu. Ezberdintasunak oker desagertutako geometria dira.</summary>
        public static void BatchOklusioProba()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                GameObject erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
                var informea = new Informea(Tresna, EgiaztapenMarka, $"{d.Izena}: Oklusioa", d.Bidea, d.Erroa);
                Camera kamera = EbidentziaKapturak.ProbaKamera();
                Renderer[] rendererrak = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

                // Jokalaria egon daitekeen puntuak bakarrik: solido baten barruan oklusioak dena ezkutatu dezake, eta hori ez da akatsa.
                var ikuspegiak = new List<(string Izena, Vector3 Begia, Quaternion Biraketa)>();
                Physics.SyncTransforms();
                int baztertuak = 0;
                foreach (BoxCollider gela in erroa.GetComponentsInChildren<BoxCollider>(true).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)))
                {
                    Bounds b = gela.bounds;
                    float x = Mathf.Max(0.4f, b.extents.x * 0.6f);
                    float z = Mathf.Max(0.4f, b.extents.z * 0.6f);
                    int zenbakia = 0;
                    foreach (Vector3 aldea in new[] { Vector3.zero, new Vector3(x, 0f, z), new Vector3(-x, 0f, z), new Vector3(x, 0f, -z), new Vector3(-x, 0f, -z) })
                    {
                        zenbakia++;
                        Vector3 puntua = new Vector3(b.center.x, 0.02f, b.center.z) + aldea;
                        if (!Proba.EsploratzaileKontrola.PosizioaLibre(puntua, out Vector3 oinak))
                        {
                            baztertuak++;
                            continue;
                        }

                        Vector3 burua = oinak + Vector3.up * 1.65f;
                        for (int yaw = 0; yaw < 360; yaw += 90)
                        {
                            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                            ikuspegiak.Add(($"{gela.name}_{zenbakia}_{yaw}", burua, q));

                            // Hirugarren pertsonako kamera, esploratzailearen arau berarekin.
                            Vector3 atzera = q * Vector3.back;
                            float distantzia = 3f;
                            if (Physics.SphereCast(burua, 0.2f, atzera, out RaycastHit jo, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                            {
                                distantzia = Mathf.Max(jo.distance - 0.05f, 0f);
                            }

                            ikuspegiak.Add(($"{gela.name}_{zenbakia}_{yaw}_3p", burua + atzera * distantzia, q));
                        }
                    }
                }

                const int Zabalera = 640;
                const int Altuera = 360;
                int ezberdinak = 0;
                var lerroak = new List<string>();
                var okerrenak = new List<(int Pixelak, string Izena)>();
                foreach ((string izena, Vector3 begia, Quaternion biraketa) in ikuspegiak)
                {
                    kamera.transform.SetPositionAndRotation(begia, biraketa);
                    kamera.useOcclusionCulling = false;
                    Texture2D gabe = EbidentziaKapturak.ProbaArgazkia(kamera, Zabalera, Altuera);
                    kamera.useOcclusionCulling = true;
                    Texture2D gaz = EbidentziaKapturak.ProbaArgazkia(kamera, Zabalera, Altuera);

                    Color32[] a = gabe.GetPixels32();
                    Color32[] b = gaz.GetPixels32();
                    int pixelak = 0;
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (Mathf.Abs(a[i].r - b[i].r) > 12 || Mathf.Abs(a[i].g - b[i].g) > 12 || Mathf.Abs(a[i].b - b[i].b) > 12)
                        {
                            pixelak++;
                        }
                    }

                    if (pixelak > 0)
                    {
                        ezberdinak++;
                        okerrenak.Add((pixelak, izena));
                        if (pixelak >= 300 && okerrenak.Count(o => o.Pixelak >= 300) <= 4)
                        {
                            EbidentziaKapturak.ProbaGorde(EbidentziaKapturak.ProbaAlboz(gabe, gaz), $"{d.Aurrizkia}_oklusioa_{izena}.png");
                        }
                    }

                    UnityEngine.Object.DestroyImmediate(gabe);
                    UnityEngine.Object.DestroyImmediate(gaz);
                }

                lerroak.AddRange(okerrenak.OrderByDescending(o => o.Pixelak).Take(15).Select(o => $"{o.Izena}: {o.Pixelak} pixel"));
                informea.Lerroa($"- **Ikuspegiak:** {ikuspegiak.Count} (oinez eskuragarriak; {baztertuak} puntu baztertuta), {Zabalera}x{Altuera}; **ezberdintasunekin:** {ezberdinak}; **pixel gehienez:** {(okerrenak.Count > 0 ? okerrenak.Max(o => o.Pixelak) : 0)}");
                informea.Atala("Ezberdintasunak dituzten ikuspegiak", lerroak);

                if (Environment.GetCommandLineArgs().Contains("-eclipseDenborarikGabe"))
                {
                    informea.AmaituOndo($"{ikuspegiak.Count} ikuspegi, {ezberdinak} ezberdintasunekin");
                    EditorApplication.Exit(0);
                    return;
                }

                // Denborak: 1280x720, itxaron gabeko estatistika, txandaka. Batch-eko neurketa: ez dira FPS errealak.
                var denborak = new List<string>();
                const int Txandak = 25;
                var neurketak = MailaKamerak(d).ToDictionary(k => k.Izena, k => (Gabe: new List<double>(), Gaz: new List<double>()));
                var lehenak = new Dictionary<string, double>();
                foreach ((string izena, Vector3 begia, Vector3 helburua) in MailaKamerak(d))
                {
                    kamera.transform.SetPositionAndRotation(begia, Quaternion.LookRotation(helburua - begia));
                    kamera.useOcclusionCulling = true;
                    lehenak[izena] = Neurtu(kamera);
                }

                for (int txanda = 0; txanda < Txandak; txanda++)
                {
                    foreach ((string izena, Vector3 begia, Vector3 helburua) in MailaKamerak(d))
                    {
                        kamera.transform.SetPositionAndRotation(begia, Quaternion.LookRotation(helburua - begia));
                        kamera.useOcclusionCulling = false;
                        neurketak[izena].Gabe.Add(Neurtu(kamera));
                        kamera.useOcclusionCulling = true;
                        neurketak[izena].Gaz.Add(Neurtu(kamera));
                    }
                }

                foreach ((string izena, Vector3 begia, Vector3 helburua) in MailaKamerak(d))
                {
                    kamera.transform.SetPositionAndRotation(begia, Quaternion.LookRotation(helburua - begia));
                    Plane[] planoak = GeometryUtility.CalculateFrustumPlanes(kamera);
                    int argiak = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)
                        .Count(l => l.enabled && l.lightmapBakeType != LightmapBakeType.Baked && GeometryUtility.TestPlanesAABB(planoak, new Bounds(l.transform.position, Vector3.one * l.range * 2f)));
                    (List<double> gabe, List<double> gaz) = neurketak[izena];
                    denborak.Add($"{izena}: lehena {lehenak[izena]:0.0} ms; oklusioarekin mediana {Mediana(gaz):0.0}, p95 {Pertzentila(gaz, 0.95):0.0}, max {gaz.Max():0.0}; gabe mediana {Mediana(gabe):0.0}, p95 {Pertzentila(gabe, 0.95):0.0}; denbora errealeko argiak bistan {argiak}");
                }

                informea.Atala("Denborak batch-ean, 1280x720, 25 txanda (ez dira FPS errealak)", denborak);
                informea.AmaituOndo($"{ikuspegiak.Count} ikuspegi, {ezberdinak} ezberdintasunekin");
                kodea = 0;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{EgiaztapenMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        private static double Neurtu(Camera kamera)
        {
            var kronometroa = Stopwatch.StartNew();
            Texture2D irudia = EbidentziaKapturak.ProbaArgazkia(kamera, 1280, 720);
            kronometroa.Stop();
            UnityEngine.Object.DestroyImmediate(irudia);
            return kronometroa.Elapsed.TotalMilliseconds;
        }

        private static double Mediana(List<double> balioak) => Pertzentila(balioak, 0.5);

        private static double Pertzentila(List<double> balioak, double p)
        {
            List<double> ordenatuak = balioak.OrderBy(x => x).ToList();
            return ordenatuak[Mathf.Clamp((int)(p * ordenatuak.Count), 0, ordenatuak.Count - 1)];
        }

        // ---------- Argiztapen-auditoria ----------

        /// <summary>Irakurtzeko bakarrik: lightmap-ak, argien moduak, probe-ak eta hormen barruan geratutako piezak.</summary>
        public static void BatchArgiAuditoria()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                GameObject erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
                var informea = new Informea(Tresna, EgiaztapenMarka, $"{d.Izena}: Argiztapen-auditoria", d.Bidea, d.Erroa);
                Transform geometria = SemeaBilatu(erroa.transform, Geometria);

                int salaOndo = 0;
                var salaGabe = new List<string>();
                int pasilloAtlasean = 0;
                int pasilloProbe = 0;
                int eskalaOkerra = 0;
                foreach (MeshRenderer r in geometria.GetComponentsInChildren<MeshRenderer>(false))
                {
                    bool pasilloan = PasillokoaDa(r.transform);
                    bool gi = GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI);
                    if (pasilloan)
                    {
                        pasilloAtlasean += r.lightmapIndex >= 0 && r.lightmapIndex < 65534 ? 1 : 0;
                        pasilloProbe += r.lightProbeUsage != UnityEngine.Rendering.LightProbeUsage.Off ? 1 : 0;
                    }
                    else if (gi)
                    {
                        if (r.lightmapIndex >= 0 && r.lightmapIndex < 65534)
                        {
                            salaOndo++;
                            if (r.lightmapScaleOffset.x <= 0f || r.lightmapScaleOffset.y <= 0f)
                            {
                                eskalaOkerra++;
                            }
                        }
                        else if (salaGabe.Count < 15)
                        {
                            salaGabe.Add(TresnaLaguntzailea.Bidea(r.transform, erroa.transform));
                        }
                    }
                }

                Light[] argiak = erroa.GetComponentsInChildren<Light>(true);
                int hornituak = argiak.Count(a => a.enabled && a.lightmapBakeType == LightmapBakeType.Baked);
                var hornituGabe = argiak.Where(a => a.enabled && a.lightmapBakeType == LightmapBakeType.Baked && !a.bakingOutput.isBaked).Select(a => a.name).Take(15).ToList();
                int denboraErreala = argiak.Count(a => a.enabled && a.gameObject.activeInHierarchy && a.lightmapBakeType == LightmapBakeType.Realtime);
                int itzalReal = argiak.Count(a => a.enabled && a.lightmapBakeType == LightmapBakeType.Realtime && a.shadows != LightShadows.None);

                ReflectionProbe[] probeak = erroa.GetComponentsInChildren<ReflectionProbe>(true);
                int rpGabe = probeak.Count(p => p.bakedTexture == null);
                LightProbes lp = LightmapSettings.lightProbes;
                int lpBeltzak = 0;
                if (lp != null)
                {
                    foreach (UnityEngine.Rendering.SphericalHarmonicsL2 sh in lp.bakedProbes)
                    {
                        if (Mathf.Abs(sh[0, 0]) + Mathf.Abs(sh[1, 0]) + Mathf.Abs(sh[2, 0]) < 0.001f)
                        {
                            lpBeltzak++;
                        }
                    }
                }

                // Probe ia beltzak non dauden.
                var probeGelak = new Dictionary<string, int>();
                if (lp != null)
                {
                    BoxCollider[] gelaKaxak = erroa.GetComponentsInChildren<BoxCollider>(true).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)).ToArray();
                    for (int i = 0; i < lp.count; i++)
                    {
                        UnityEngine.Rendering.SphericalHarmonicsL2 sh = lp.bakedProbes[i];
                        if (Mathf.Abs(sh[0, 0]) + Mathf.Abs(sh[1, 0]) + Mathf.Abs(sh[2, 0]) >= 0.001f)
                        {
                            continue;
                        }

                        BoxCollider non = gelaKaxak.FirstOrDefault(k => k.bounds.Contains(lp.positions[i]));
                        string izena = non != null ? non.name : "gelatik kanpo";
                        probeGelak.TryGetValue(izena, out int kop);
                        probeGelak[izena] = kop + 1;
                    }
                }

                informea.Atala("Probe ia beltzak gelaka", probeGelak.Select(x => $"{x.Key}: {x.Value}").ToList());

                // Pantailak hormaren barruan: gela-kaxaren barrutik kanpo geratzen direnak.
                List<Bounds> gelak = erroa.GetComponentsInChildren<BoxCollider>(true).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)).Select(k => k.bounds).ToList();
                var hormanBarruan = new List<string>();
                foreach (Transform t in geometria.GetComponentsInChildren<Transform>(false).Where(t => t.name.StartsWith("PF_Pantaila", StringComparison.Ordinal)))
                {
                    Vector3 p = t.GetComponent<Renderer>().bounds.center;
                    if (!gelak.Any(g => g.Contains(p)))
                    {
                        hormanBarruan.Add($"{TresnaLaguntzailea.Bidea(t, erroa.transform)} {V(p)}");
                    }
                }

                informea.Lerroa($"- **Lightmap-ak:** {LightmapSettings.lightmaps.Length}; **salako pieza hornituak:** {salaOndo}; **eskala okerra:** {eskalaOkerra}; **GI izan eta atlasik gabe:** {salaGabe.Count}");
                informea.Lerroa($"- **Pasilloetako piezak atlasean (0 espero):** {pasilloAtlasean}; **Light Probe erabiltzen:** {pasilloProbe}");
                informea.Lerroa($"- **Argi hornituak:** {hornituak}; **hornitu gabe geratuak:** {hornituGabe.Count}; **denbora errealekoak aktibo:** {denboraErreala}; **haietatik itzalekin (0 espero):** {itzalReal}");
                informea.Lerroa($"- **Reflection Probe:** {probeak.Length}; **testurarik gabe:** {rpGabe}; **Light Probe:** {lp?.count ?? 0}; **ia beltzak:** {lpBeltzak}");
                informea.Atala("GI bai, lightmap ez", salaGabe);
                informea.Atala("Argi hornitu gabeak", hornituGabe);
                informea.Atala("Pantailak hormaren barruan", hormanBarruan);

                // Horma-orban obalatuak: islapen-probe-ek eragiten dituzten egiaztatu.
                Camera kamera = EbidentziaKapturak.ProbaKamera();
                foreach (MailaKamera k in d.Kamerak.Where(k => k.Izena == "Irteera" || k.Izena == "Laberintoa"))
                {
                    float c = ProtoGelaxka();
                    var begia = new Vector3(k.Bx * c, k.By, k.Bz * c);
                    var helburua = new Vector3(k.Hx * c, k.Hy, k.Hz * c);
                    kamera.transform.SetPositionAndRotation(begia, Quaternion.LookRotation(helburua - begia));
                    Texture2D bai = EbidentziaKapturak.ProbaArgazkia(kamera, 960, 540);
                    foreach (ReflectionProbe p in probeak)
                    {
                        p.enabled = false;
                    }

                    Texture2D ez = EbidentziaKapturak.ProbaArgazkia(kamera, 960, 540);
                    foreach (ReflectionProbe p in probeak)
                    {
                        p.enabled = true;
                    }

                    EbidentziaKapturak.ProbaGorde(EbidentziaKapturak.ProbaAlboz(bai, ez), $"{d.Aurrizkia}_RP_{k.Izena}.png");
                }

                informea.AmaituOndo($"{salaOndo} pieza hornituta, {salaGabe.Count} faltan, {hornituGabe.Count} argi hornitu gabe, {hormanBarruan.Count} pantaila hormaren barruan");
                kodea = 0;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{EgiaztapenMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        /// <summary>Argazkiak bakarrik, uholderik gabe: aldaketa bisualak egiaztatzeko.</summary>
        public static void BatchMailaArgazkiak()
        {
            int kodea = 1;
            try
            {
                MailaDef d = BatchMailaLortu();
                EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                EbidentziaKapturak.MailaArgazkiak(d.Aurrizkia, d.Erroa, MailaKamerak(d));
                kodea = 0;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{EgiaztapenMarka}_ERROR {salbuespena}");
            }

            EditorApplication.Exit(kodea);
        }

        // ---------- Tresnen arauak ----------

        /// <summary>Aurrebistak ez du ezer aldatzen; Aplikatu-k talde batean aldatzen du eta desegin daiteke; bigarrenak 0 aldaketa. Ez da ezer gordetzen.</summary>
        public static void BatchTresnaProba()
        {
            int kodea = 1;
            var lerroak = new List<string>();
            MailaDef d = null;
            try
            {
                d = BatchMailaLortu();
                Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                GameObject erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
                bool ondo = true;

                void Egiaztatu(string izena, bool baldintza, string xehetasuna = "")
                {
                    lerroak.Add($"{(baldintza ? "ONDO" : "HUTS")}: {izena} {xehetasuna}");
                    ondo &= baldintza;
                }

                int Kopurua() => erroa.GetComponentsInChildren<Transform>(true).Length;
                int hasiera = Kopurua();

                // 1. Aurrebista ukitu gabeko eszenan.
                bool aurre = MailaExekutatu(d, false);
                Egiaztatu("Aurrebistak amaitzen du", aurre, $"{_azkenAldaketak} aldaketa aurreikusita");
                Egiaztatu("Aurrebistaren ondoren eszena ez dago zikin", !eszena.isDirty);
                Egiaztatu("Aurrebistak ez du objekturik sortu", Kopurua() == hasiera, $"{hasiera} -> {Kopurua()}");
                int oinarrizkoAldaketak = _azkenAldaketak;

                // 2. Aplikatu aldaketarik gabe (idempotentzia), datuak eta eszena bat badatoz.
                if (oinarrizkoAldaketak == 0)
                {
                    bool bi = MailaExekutatu(d, true);
                    Egiaztatu("Aplikatu (0 aldaketa espero)", bi && _azkenAldaketak == 0, $"{_azkenAldaketak} aldaketa");
                    Egiaztatu("Aplikatu hutsak eszena ez du zikintzen", !eszena.isDirty);
                }
                else
                {
                    lerroak.Add($"OHARRA: datuek eta eszenak {oinarrizkoAldaketak} ezberdintasun dituzte; idempotentzia-proba ez da egin.");
                }

                // 3. Pieza bat mugitu eta kanpoko objektua gehitu: aurrebistak ikusi behar du, baina ez konpondu.
                Transform pieza = SemeaBilatu(erroa.transform, Geometria).GetComponentsInChildren<Transform>(true).First(t => t.name == "Wall12__000");
                Vector3 jatorria = pieza.localPosition;
                pieza.localPosition = jatorria + Vector3.up;
                var kanpokoa = new GameObject("Ion_Proba");
                kanpokoa.transform.SetParent(erroa.transform, false);
                Physics.SyncTransforms();

                MailaExekutatu(d, false);
                Egiaztatu("Aurrebistak mugitutako pieza detektatzen du", _azkenAldaketak == oinarrizkoAldaketak + 1, $"{_azkenAldaketak} aldaketa");
                Egiaztatu("Aurrebistak ez du pieza konpondu", (pieza.localPosition - (jatorria + Vector3.up)).sqrMagnitude < 0.0001f);

                // 4. Aplikatu konpontzen du; kanpoko objektua ez du ukitzen.
                bool konpondu = MailaExekutatu(d, true);
                Egiaztatu("Aplikatu-k pieza konpontzen du", konpondu && (pieza.localPosition - jatorria).sqrMagnitude < 0.0001f, $"{_azkenAldaketak} aldaketa");
                Egiaztatu("Aplikatu-k erabiltzailearen objektua errespetatzen du", kanpokoa != null);

                // 5. Ctrl+Z bakarrak dena desegiten du (talde bakarra).
                Undo.PerformUndo();
                Egiaztatu("Desegin bakarrak Aplikatu-ren aldaketa desegiten du", (pieza.localPosition - (jatorria + Vector3.up)).sqrMagnitude < 0.0001f, V(pieza.localPosition));
                Undo.PerformRedo();
                Egiaztatu("Berregin", (pieza.localPosition - jatorria).sqrMagnitude < 0.0001f, V(pieza.localPosition));

                UnityEngine.Object.DestroyImmediate(kanpokoa);

                // 6. Beste tresnen aurrebistak: ez dute zikintzen. Eszena zikin dagoenez, berriro ireki.
                eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
                erroa = eszena.GetRootGameObjects().First(o => o.name == d.Erroa);
                bool preset = ArgiztapenPreseta.AurrebistaErroan(erroa);
                bool probe = ProbeJartzailea.AurrebistaErroan(erroa);
                Egiaztatu("ArgiztapenPreseta eta ProbeJartzailea: aurrebistak ez du zikintzen", preset && probe && !eszena.isDirty);

                // 7. Eszena irekita ez dagoenean, aurrebistak ez du ezer sortzen.
                MailaDef bestea = Mailak.Values.Select(f => f()).First(m => m.Izena != d.Izena);
                Scene bestaldea = EditorSceneManager.OpenScene(bestea.Bidea, OpenSceneMode.Single);
                bool itxita = MailaExekutatu(d, false);
                Egiaztatu("Beste eszena batetik aurrebista: ez du irekitako eszena zikintzen", itxita && !bestaldea.isDirty);

                var informea = new Informea(Tresna, EgiaztapenMarka, $"{d.Izena}: Tresnen arauak", d.Bidea, d.Erroa);
                informea.Atala("Emaitzak", lerroak);
                if (ondo)
                {
                    informea.AmaituOndo($"{lerroak.Count} egiaztapen");
                    kodea = 0;
                }
                else
                {
                    informea.AmaituErrorearekin($"{lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal))} huts");
                }
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{EgiaztapenMarka}_ERROR {salbuespena}\n{string.Join("\n", lerroak)}");
            }

            // Eszenak ez dira gordetzen: bi aldiz ez galdetzeko, editorea zuzenean ixten da.
            EditorApplication.Exit(kodea);
        }

        // ---------- Interakzioa, sarrera simulatuarekin ----------

        private static MailaDef _interakzioMaila;
        private static double _interakzioHasiera;
        private const double InterakzioDenbora = 280.0;

        /// <summary>Batchmode: Play-n sartu eta teklatu eta sagu birtualekin esploratzailea probatu.</summary>
        public static void BatchInterakzioProba()
        {
            try
            {
                _interakzioMaila = BatchMailaLortu();
                EditorSceneManager.OpenScene(_interakzioMaila.Bidea, OpenSceneMode.Single);
                _interakzioHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += InterakzioaEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{EgiaztapenMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void InterakzioaEguneratu()
        {
            if (EditorApplication.timeSinceStartup - _interakzioHasiera > InterakzioDenbora)
            {
                InterakzioaAmaitu(false, new List<string> { "HUTS: denbora agortuta" });
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            InterakzioKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<InterakzioKontrola>();
            CharacterController cc = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            if (kontrola == null && cc != null)
            {
                kontrola = new GameObject("InterakzioKontrola").AddComponent<InterakzioKontrola>();
                kontrola.Hasi(cc);
            }

            if (kontrola != null && kontrola.Amaituta)
            {
                InterakzioaAmaitu(kontrola.Lerroak.All(l => !l.StartsWith("HUTS", StringComparison.Ordinal)), kontrola.Lerroak);
            }
        }

        private static void InterakzioaAmaitu(bool ondo, List<string> lerroak)
        {
            EditorApplication.update -= InterakzioaEguneratu;
            var informea = new Informea(Tresna, EgiaztapenMarka, $"{_interakzioMaila.Izena}: Interakzioa (sarrera simulatua)", _interakzioMaila.Bidea, _interakzioMaila.Erroa);
            informea.Atala("Emaitzak", lerroak);
            if (ondo)
            {
                informea.AmaituOndo($"{lerroak.Count} egiaztapen");
            }
            else
            {
                informea.AmaituErrorearekin($"{lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal))} huts");
            }

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(ondo ? 0 : 1);
        }

        private sealed class InterakzioKontrola : MonoBehaviour
        {
            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;

            private CharacterController _cc;
            private Keyboard _teklatua;
            private Mouse _sagua;
            private Transform _burua;
            private Proba.EsploratzaileKontrola _esploratzailea;

            public void Hasi(CharacterController cc)
            {
                _cc = cc;
                StartCoroutine(Exekutatu());
            }

            private void Oharra(string izena, bool ondo, string xehetasuna = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena} {xehetasuna}");

            private void Teklak(params Key[] teklak) => InputSystem.QueueStateEvent(_teklatua, new KeyboardState(teklak));

            private IEnumerator Itxaron(float segundoak)
            {
                float amaiera = Time.realtimeSinceStartup + segundoak;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    yield return null;
                }
            }

            private IEnumerator Kokatu(Vector3 posizioa, float yaw = 0f)
            {
                _cc.enabled = false;
                _cc.transform.SetPositionAndRotation(posizioa, Quaternion.Euler(0f, yaw, 0f));
                _cc.enabled = true;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            private IEnumerator Sakatu(Key tekla)
            {
                Teklak(tekla);
                yield return null;
                yield return null;
                Teklak();
                yield return null;
                yield return null;
            }

            /// <summary>
            /// Batch-ean kurtsorea ezin da blokeatu eta esploratzaileak ez du teklatua irakurtzen: Mugitu bera deitzen da fotograma bakoitzean,
            /// benetako teklatu-egoerarekin. Blokeoaren baldintza eskuzko probarako geratzen da.
            /// </summary>
            private IEnumerator Ibili(float segundoak, params Key[] teklak)
            {
                Teklak(teklak);
                yield return null;
                float amaiera = Time.realtimeSinceStartup + segundoak;
                while (Time.realtimeSinceStartup < amaiera)
                {
                    Metodoa("Mugitu").Invoke(_esploratzailea, new object[] { _teklatua });
                    yield return null;
                }

                Teklak();
                yield return null;
            }

            private System.Reflection.MethodInfo Metodoa(string izena)
            {
                return typeof(Proba.EsploratzaileKontrola).GetMethod(izena, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            private IEnumerator Exekutatu()
            {
                // Bakarrik frogarako: fokurik gabeko batch-ean sarrera ez galtzeko. Ez da gordetzen.
                InputSettings ezarpenak = ScriptableObject.CreateInstance<InputSettings>();
                ezarpenak.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                ezarpenak.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings = ezarpenak;

                _teklatua = InputSystem.AddDevice<Keyboard>("ProbaTeklatua");
                _sagua = InputSystem.AddDevice<Mouse>("ProbaSagua");
                _teklatua.MakeCurrent();
                _sagua.MakeCurrent();
                yield return null;
                yield return null;

                Transform t = _cc.transform;
                _burua = t.Find("Burua");
                _esploratzailea = t.GetComponent<Proba.EsploratzaileKontrola>();
                LevelManager kudeatzailea = FindAnyObjectByType<LevelManager>();
                Oharra("Esploratzailea eta kudeatzailea daude", _burua != null && kudeatzailea != null);

                Cursor.lockState = CursorLockMode.Locked;
                yield return null;
                Lerroak.Add($"OHARRA: Cursor.lockState batch-ean: {Cursor.lockState}");

                // Sala handi batean probatu: Gela handienaren erdia.
                BoxCollider gela = FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude)
                    .Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal) && !k.name.StartsWith("Gela_C_", StringComparison.Ordinal))
                    .OrderByDescending(k => k.bounds.size.x * k.bounds.size.z)
                    .First();

                // Mugimendua.
                Vector3 libre = EskuragarriaBilatu(gela.bounds);

                // Probak talkarik gabe: 8 m libre dituen puntu eta norabide bat, oztoporik gabe abiadura neurtzeko.
                float yawLibre = 0f;
                bool aurkitua = false;
                foreach (BoxCollider k in FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)).OrderByDescending(k => k.bounds.size.x * k.bounds.size.z))
                {
                    foreach (Vector3 lagina in LagineztatuGela(k.bounds))
                    {
                        if (!EsploratzaileLibre(lagina, out Vector3 oinak))
                        {
                            continue;
                        }

                        for (int yaw = 0; yaw < 360 && !aurkitua; yaw += 90)
                        {
                            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                            Vector3 hasiera = oinak + Vector3.up * 0.9f;
                            bool garbia = !Physics.SphereCast(hasiera, 0.35f, q * Vector3.forward, out RaycastHit _, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                                          && !Physics.SphereCast(hasiera, 0.35f, q * Vector3.right, out RaycastHit _, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                                          && !Physics.SphereCast(hasiera, 0.35f, q * Vector3.left, out RaycastHit _, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                                          && !Physics.SphereCast(hasiera, 0.35f, q * Vector3.back, out RaycastHit _, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                            if (garbia)
                            {
                                libre = oinak;
                                yawLibre = yaw;
                                aurkitua = true;
                            }
                        }

                        if (aurkitua)
                        {
                            break;
                        }
                    }

                    if (aurkitua)
                    {
                        break;
                    }
                }

                Lerroak.Add($"OHARRA: mugimendu-proba {libre} puntutik, yaw {yawLibre}; {(aurkitua ? "8 m libre" : "ez da puntu librerik aurkitu")}");
                yield return Kokatu(libre, yawLibre);
                Vector3 p0 = t.position;
                float denbora = Time.time;
                yield return Ibili(1f, Key.W);
                float abiadura = Vector3.Distance(Xz(p0), Xz(t.position)) / Mathf.Max(0.01f, Time.time - denbora);
                float aurrera = Vector3.Dot(t.position - p0, t.forward);
                Oharra("W: aurrera, ~3 m/s", abiadura > 2f && abiadura < 3.6f && aurrera > 0f, $"{abiadura:0.00} m/s");

                yield return Kokatu(libre, yawLibre);
                p0 = t.position;
                denbora = Time.time;
                yield return Ibili(1f, Key.W, Key.LeftShift);
                float korrika = Vector3.Distance(Xz(p0), Xz(t.position)) / Mathf.Max(0.01f, Time.time - denbora);
                Oharra("Shift+W: korrika, ~6 m/s", korrika > 4f && korrika < 7f, $"{korrika:0.00} m/s");

                yield return Kokatu(libre, yawLibre);
                p0 = t.position;
                yield return Ibili(0.5f, Key.S);
                Oharra("S: atzera", Vector3.Dot(t.position - p0, t.forward) < -0.3f);
                yield return Kokatu(libre, yawLibre);
                p0 = t.position;
                yield return Ibili(0.5f, Key.D);
                Oharra("D: eskuinera", Vector3.Dot(t.position - p0, t.right) > 0.3f);
                yield return Kokatu(libre, yawLibre);
                p0 = t.position;
                yield return Ibili(0.5f, Key.A);
                Oharra("A: ezkerrera", Vector3.Dot(t.position - p0, t.right) < -0.3f);

                // Begirada: esploratzailearen metodo bera, sentikortasun errealarekin.
                yield return Kokatu(libre, 0f);
                float yaw0 = t.eulerAngles.y;
                Metodoa("Begiratu").Invoke(_esploratzailea, new object[] { new Vector2(100f, 0f) });
                float yawAldea = Mathf.DeltaAngle(yaw0, t.eulerAngles.y);
                Oharra("Begiratu X: yaw, sentikortasuna 0,1", Mathf.Abs(yawAldea - 10f) < 0.1f, $"{yawAldea:0.0}°");
                Metodoa("Begiratu").Invoke(_esploratzailea, new object[] { new Vector2(0f, -5000f) });
                float pitch = Mathf.DeltaAngle(0f, _burua.localEulerAngles.x);
                Oharra("Begiratu Y: pitch mugatuta 85°-ra", Mathf.Abs(Mathf.Abs(pitch) - 85f) < 0.1f, $"{pitch:0.0}°");
                Metodoa("Begiratu").Invoke(_esploratzailea, new object[] { new Vector2(0f, 5000f) });
                pitch = Mathf.DeltaAngle(0f, _burua.localEulerAngles.x);
                Oharra("Begiratu Y: beste muga", Mathf.Abs(Mathf.Abs(pitch) - 85f) < 0.1f, $"{pitch:0.0}°");
                Metodoa("Begiratu").Invoke(_esploratzailea, new object[] { new Vector2(0f, -850f) });
                yield return null;

                // Horma: ez du zeharkatzen.
                yield return Kokatu(libre, 0f);
                yield return Ibili(6f, Key.W, Key.LeftShift);
                Bounds hedatua = gela.bounds;
                hedatua.Expand(new Vector3(1f, 20f, 1f));
                Oharra("Horma ez da zeharkatzen (6 s korrika aurrera)", hedatua.Contains(t.position) || HodiBatean(t.position), $"{t.position}");

                // Terminalak E teklarekin: benetako teklatuko gertaera.
                MisioLogika logika = kudeatzailea.Logika;
                MisioTerminala[] terminalak = FindObjectsByType<MisioTerminala>(FindObjectsInactive.Exclude);
                MisioTerminala aktiboa = terminalak.First(x => logika.Aktibo(x.Helburua));
                MisioTerminala besteFasekoa = terminalak.FirstOrDefault(x => !logika.Aktibo(x.Helburua) && !logika.BetetaDago(x.Helburua));
                int fasea = logika.UnekoFasea;

                yield return Kokatu(libre);
                yield return Sakatu(Key.E);
                Oharra("E terminaletik urrun: ez du ezer egiten", logika.UnekoFasea == fasea && !logika.BetetaDago(aktiboa.Helburua));

                if (besteFasekoa != null)
                {
                    yield return TerminalaraJoan(besteFasekoa);
                    Oharra("Beste faseko terminalaren trigger-a", besteFasekoa.JokalariaBarruan);
                    yield return Sakatu(Key.E);
                    Oharra("E beste faseko terminalean: ez du betetzen", !logika.BetetaDago(besteFasekoa.Helburua) && logika.UnekoFasea == fasea);
                }

                yield return TerminalaraJoan(aktiboa);
                Oharra("Terminal aktiboaren trigger-a", aktiboa.JokalariaBarruan);
                Light adierazlea = aktiboa.GetComponentInChildren<Light>();
                Color kolorea = adierazlea.color;
                yield return Sakatu(Key.E);
                Oharra("E terminal aktiboan: helburua betetzen du", logika.BetetaDago(aktiboa.Helburua));
                Oharra("Adierazlea kolorez aldatzen da", adierazlea.color != kolorea, $"{kolorea} -> {adierazlea.color}");

                // Kamera: V eta hirugarren pertsona.
                Camera kamera = Camera.main;
                Renderer gorputza = t.Find("Gorputza").GetComponent<Renderer>();
                yield return Kokatu(libre, 0f);
                Oharra("Lehen pertsonan hasi: gorputza ezkutuan", !gorputza.enabled);
                Metodoa("KameraModuaEzarri").Invoke(_esploratzailea, new object[] { true });
                yield return null;
                yield return null;
                float urruntasuna = Vector3.Distance(kamera.transform.position, _burua.position);
                Oharra("Hirugarren pertsona: gorputza ikusgai, kamera atzean", gorputza.enabled && urruntasuna > 0.5f && urruntasuna <= 3.05f, $"{urruntasuna:0.00} m");

                // Kamera hormen barruan ez dela sartzen: gela eta pasillo guztietan, 8 norabide eta 3 pitch.
                int ikuspegiak = 0;
                int zeharkatuak = 0;
                int solidoan = 0;
                int hurbilak = 0;
                var solidoKasuak = new List<string>();
                float gutxienekoa = float.MaxValue;
                var okerrak = new List<string>();
                BoxCollider[] gelak = FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude).Where(k => k.name.StartsWith("Gela_", StringComparison.Ordinal)).ToArray();
                foreach (BoxCollider g in gelak)
                {
                    foreach (Vector3 puntua in LagineztatuGela(g.bounds))
                    {
                        if (!EsploratzaileLibre(puntua, out Vector3 oinak))
                        {
                            continue;
                        }

                        yield return Kokatu(oinak);
                        foreach (float pitchHelburua in new[] { 0f, -35f, 35f })
                        {
                            for (int yaw = 0; yaw < 360; yaw += 45)
                            {
                                t.rotation = Quaternion.Euler(0f, yaw, 0f);
                                _burua.localRotation = Quaternion.Euler(pitchHelburua, 0f, 0f);
                                yield return null;
                                Vector3 k = kamera.transform.position;
                                Vector3 b = _burua.position;
                                ikuspegiak++;
                                float d = Vector3.Distance(k, b);
                                gutxienekoa = Mathf.Min(gutxienekoa, d);
                                hurbilak += d < 1f ? 1 : 0;
                                if (Physics.Linecast(b, k, out RaycastHit jo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                                {
                                    zeharkatuak++;
                                    if (okerrak.Count < 8)
                                    {
                                        okerrak.Add($"{g.name} yaw {yaw} pitch {pitchHelburua}: {jo.collider.name}");
                                    }
                                }

                                if (Physics.CheckSphere(k, 0.04f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                                {
                                    solidoan++;
                                    if (solidoKasuak.Count < 5)
                                    {
                                        Collider[] ukituak = Physics.OverlapSphere(k, 0.04f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                                        solidoKasuak.Add($"{g.name} oinak {t.position} yaw {yaw} pitch {pitchHelburua}: {string.Join(",", ukituak.Select(c => c.transform.parent != null ? c.transform.parent.name + "/" + c.name : c.name))}, burutik {d:0.00} m");
                                    }
                                }
                            }
                        }
                    }
                }

                _burua.localRotation = Quaternion.identity;
                Oharra("Kamera ez da hormatik pasatzen", zeharkatuak == 0, $"{ikuspegiak} ikuspegi, {zeharkatuak} oker; {string.Join("; ", okerrak)}");
                Oharra("Kamera ez dago solido baten barruan", solidoan == 0, $"{solidoan} kasu; gutxieneko urruntasuna burutik {gutxienekoa:0.00} m; 1 m baino gutxiago {hurbilak} ikuspegitan {string.Join(" | ", solidoKasuak)}");

                Metodoa("KameraModuaEzarri").Invoke(_esploratzailea, new object[] { false });
                yield return null;
                yield return null;
                Oharra("Lehen pertsonara itzuli", !gorputza.enabled && Vector3.Distance(kamera.transform.position, _burua.position) < 0.01f);
                Lerroak.Add("OHARRA: V, Esc eta klik teklak Cursor.lockState-ren menpe daude; batch-ean ezin dira probatu: Ion-en proba.");

                // Bilduma.
                MisioBilduma bilduma = FindAnyObjectByType<MisioBilduma>();
                if (bilduma != null)
                {
                    Vector3 p = bilduma.transform.position;
                    p.y = 0f;
                    if (EsploratzaileLibreHurbil(p, out Vector3 oinak))
                    {
                        yield return Kokatu(oinak);
                        yield return new WaitForSeconds(0.1f);
                        Oharra("Bilduma biltzen da trigger-arekin", bilduma == null || !bilduma.gameObject.activeSelf);
                    }
                }

                InputSystem.RemoveDevice(_teklatua);
                InputSystem.RemoveDevice(_sagua);
                Amaituta = true;
            }

            private IEnumerator TerminalaraJoan(MisioTerminala terminala)
            {
                Bounds kaxa = terminala.GetComponent<BoxCollider>().bounds;
                Vector3 c = terminala.transform.position;
                foreach (Vector3 aldea in new[] { Vector3.back, Vector3.forward, Vector3.left, Vector3.right, new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, -1f) })
                {
                    Vector3 p = c + aldea * 1.6f;
                    if (kaxa.Contains(p + Vector3.up) && EsploratzaileLibre(p, out Vector3 oinak))
                    {
                        yield return Kokatu(oinak);
                        yield return new WaitForFixedUpdate();
                        yield return null;
                        yield break;
                    }
                }
            }

            private static Vector3 Xz(Vector3 v) => new Vector3(v.x, 0f, v.z);

            private static bool HodiBatean(Vector3 p)
            {
                return FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude).Any(k => k.name.StartsWith("Gela_", StringComparison.Ordinal) && k.bounds.Contains(p + Vector3.up));
            }

            private static bool EsploratzaileLibre(Vector3 p, out Vector3 oinak)
            {
                return Proba.EsploratzaileKontrola.PosizioaLibre(new Vector3(p.x, p.y + 0.02f, p.z), out oinak);
            }

            private static bool EsploratzaileLibreHurbil(Vector3 p, out Vector3 oinak)
            {
                foreach (Vector3 aldea in new[] { Vector3.zero, Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
                {
                    if (EsploratzaileLibre(p + aldea * 0.5f, out oinak))
                    {
                        return true;
                    }
                }

                oinak = p;
                return false;
            }

            private static Vector3 EskuragarriaBilatu(Bounds kaxa)
            {
                Vector3 erdia = kaxa.center;
                erdia.y = 0f;
                for (float r = 0f; r < 8f; r += 0.5f)
                {
                    foreach (Vector3 aldea in new[] { Vector3.zero, Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
                    {
                        if (EsploratzaileLibre(erdia + aldea * r, out Vector3 oinak))
                        {
                            return oinak;
                        }
                    }
                }

                return erdia;
            }

            private static IEnumerable<Vector3> LagineztatuGela(Bounds b)
            {
                Vector3 c = b.center;
                c.y = 0f;
                yield return c;
                float x = Mathf.Max(0.5f, b.extents.x - 0.45f);
                float z = Mathf.Max(0.5f, b.extents.z - 0.45f);
                yield return c + new Vector3(x, 0f, 0f);
                yield return c + new Vector3(-x, 0f, 0f);
                yield return c + new Vector3(0f, 0f, z);
                yield return c + new Vector3(0f, 0f, -z);
            }
        }
    }
}
