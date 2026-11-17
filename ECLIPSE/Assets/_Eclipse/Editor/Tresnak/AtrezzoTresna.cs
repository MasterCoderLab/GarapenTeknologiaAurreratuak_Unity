using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Atrezzo;
using Eclipse.Misioak;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3, C blokea: sektore bakoitzaren identitatea atrezzo propioarekin, EZ ESZENAK ALDATUZ.
    /// - Piezak (AtrezzoPiezak) eta haien kokapena misio bakoitzean: hormen kontra, ate-hutsuneetan ez, helburuetatik urrun.
    /// - Egiaztapena NavMesh-arekin: spawn → terminalak / datu-nukleoak / irteera / markatzaileak bideak osorik eta luzatu gabe.
    /// - Datuak Resources/Atrezzo/AD_&lt;eszena&gt;-n; runtime-n AtrezzoKargatzailea-k jartzen ditu demoa aktibo dagoenean.
    /// - Aurretik/ondoren kapturak misio bakoitzeko ikuspegietatik, eta erakusketa-eszena neutroa (05).
    /// Aurrebista lehenetsia; -eclipseAplikatu gordetzeko. Eszenak irekitzen dira baina ez dira inoiz gordetzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string AtMarka = "ECL_ATREZZO";
        private const string AtDatuKarpeta = "Assets/Resources/Atrezzo";
        internal const string ErakusketaEszena = "Assets/_Eclipse/Eszenak/Proba/05_AtrezzoErakusketa.unity";

        [MenuItem("ECLIPSE/Atrezzo/Demoa: aktibatu (Editor)")]
        private static void AtrezzoAktibatu()
        {
            EditorPrefs.SetBool(AtrezzoKargatzailea.EditorGakoa, true);
            Debug.Log($"{AtMarka}_OK Atrezzo-demoa AKTIBO: hurrengo Play-ean misioek sektoreko atrezzoa izango dute (eszenak aldatu gabe).");
        }

        [MenuItem("ECLIPSE/Atrezzo/Demoa: desaktibatu")]
        private static void AtrezzoDesaktibatu()
        {
            EditorPrefs.SetBool(AtrezzoKargatzailea.EditorGakoa, false);
            Debug.Log($"{AtMarka}_OK Atrezzo-demoa desaktibatuta.");
        }

        [MenuItem("ECLIPSE/Atrezzo/Demoa: aktibatu (Editor)", true)]
        private static bool AtrezzoAktibatuBalidatu() => !EditorPrefs.GetBool(AtrezzoKargatzailea.EditorGakoa, false);

        [MenuItem("ECLIPSE/Atrezzo/Demoa: desaktibatu", true)]
        private static bool AtrezzoDesaktibatuBalidatu() => EditorPrefs.GetBool(AtrezzoKargatzailea.EditorGakoa, false);

        public static void BatchAtrezzo()
        {
            bool aplikatu = Environment.GetCommandLineArgs().Contains("-eclipseAplikatu");
            var aldaketak = new List<string>();
            var informea = new Informea("AtrezzoTresna", AtMarka, aplikatu ? "Aplikatu" : "Aurrebista", "M1, M2, M3 (gorde gabe) + 05", "Atrezzoa");
            int hutsak = 0;
            try
            {
                Dictionary<string, Material> materialak = AtrezzoPiezak.Materialak(aplikatu, aldaketak);
                List<AtrezzoPiezak.PiezaDef> defs = AtrezzoPiezak.Definizioak();
                var piezak = new Dictionary<string, GameObject>();
                foreach (AtrezzoPiezak.PiezaDef def in defs)
                {
                    piezak[def.Izena] = AtrezzoPiezak.AurrefabrikatuaSortu(def, materialak, aplikatu, aldaketak);
                }

                informea.Atala("Piezak eta materialak", aldaketak);
                if (aplikatu || piezak.Values.All(p => p != null))
                {
                    foreach (MailaDef d in new[] { Mision01(), Mision02(), Mision03() })
                    {
                        var lerroak = new List<string>();
                        hutsak += MisioAtrezzoa(d, defs, piezak, aplikatu, lerroak) ? 0 : 1;
                        informea.Atala(d.Izena, lerroak);
                    }

                    if (aplikatu)
                    {
                        informea.Atala("Erakusketa", new List<string> { Erakusketa(defs, piezak) });
                    }
                }

                if (hutsak == 0)
                {
                    informea.AmaituOndo($"{aldaketak.Count} pieza/material aldaketa");
                }
                else
                {
                    informea.AmaituErrorearekin($"{hutsak} misiotan egiaztapenak huts");
                }
            }
            catch (Exception salbuespena)
            {
                hutsak++;
                informea.AmaituErrorearekin(salbuespena.ToString());
            }

            EditorApplication.Exit(hutsak == 0 ? 0 : 1);
        }

        private sealed class Puntua
        {
            public Vector3 P;
            public float Muga;
            public string Izena;
            /// <summary>Kolisiodun piezei bakarrik eragiten die (hormakoek eta orbanek ez dute kolisiorik).</summary>
            public bool LurrekoakBakarrik;
        }

        private static bool MisioAtrezzoa(MailaDef d, List<AtrezzoPiezak.PiezaDef> defs, Dictionary<string, GameObject> piezak, bool aplikatu, List<string> lerroak)
        {
            Scene eszena = EditorSceneManager.OpenScene(d.Bidea, OpenSceneMode.Single);
            Physics.SyncTransforms();
            const int Geruzak = ~(1 << 2);

            // Interes-puntuak eta haien distantzia minimoa (oinatzaren erditik, horizontalean).
            var puntuak = new List<Puntua>();
            LevelManager km = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            Vector3 spawn = km != null ? km.SpawnPuntua : Vector3.zero;
            puntuak.Add(new Puntua { P = spawn, Muga = 3f, Izena = "spawn" });
            puntuak.AddRange(UnityEngine.Object.FindObjectsByType<MisioTerminala>(FindObjectsInactive.Include).Select(t => new Puntua { P = t.transform.position, Muga = 2f, Izena = t.name }));
            puntuak.AddRange(UnityEngine.Object.FindObjectsByType<MisioBilduma>(FindObjectsInactive.Include).Select(t => new Puntua { P = t.transform.position, Muga = 1.5f, Izena = t.name }));
            puntuak.AddRange(UnityEngine.Object.FindObjectsByType<BlokeoAtea>(FindObjectsInactive.Include).Select(t => new Puntua { P = t.transform.position, Muga = 2.6f, Izena = t.name }));
            puntuak.AddRange(UnityEngine.Object.FindObjectsByType<MailaMarka>(FindObjectsInactive.Include).Select(t => new Puntua { P = t.transform.position, Muga = 1.2f, Izena = t.name }));
            puntuak.AddRange(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(t => t.name.IndexOf("Door", StringComparison.OrdinalIgnoreCase) >= 0).Select(t => new Puntua { P = t.position, Muga = 2f, Izena = t.name }));
            // Robotaren ibilbidea (Play-ren aurretik kalkulatua, atrezzorik gabe): lurreko piezak ez dira haren ondoan jartzen.
            var robotHutsak = new List<string>();
            List<Vector3> robotBidea = RobotIbilbidePuntuak(d, robotHutsak);
            lerroak.Add(robotHutsak.Count == 0 && robotBidea.Count > 0
                ? $"ONDO: robotaren ibilbidea kalkulatuta ({robotBidea.Count} puntu, {Luzera(robotBidea):0} m)"
                : $"HUTS: robotaren ibilbidea: {robotBidea.Count} puntu; {string.Join(" | ", robotHutsak.Take(3))}");
            puntuak.AddRange(robotBidea.Select(r => new Puntua { P = r, Muga = 0.8f, Izena = "robot-ibilbidea", LurrekoakBakarrik = true }));
            Bounds[] irteerak = UnityEngine.Object.FindObjectsByType<MisioIrteera>(FindObjectsInactive.Include)
                .Select(i => i.GetComponent<Collider>()).Where(c => c != null).Select(c => { Bounds b = c.bounds; b.Expand(3f); return b; }).ToArray();

            // Helburuak bideak egiaztatzeko.
            var helburuak = UnityEngine.Object.FindObjectsByType<MisioTerminala>(FindObjectsInactive.Include).Select(t => (t.name, t.transform.position))
                .Concat(UnityEngine.Object.FindObjectsByType<MisioBilduma>(FindObjectsInactive.Include).Select(t => (t.name, t.transform.position)))
                .Concat(UnityEngine.Object.FindObjectsByType<MisioIrteera>(FindObjectsInactive.Include).Select(t => (t.name, t.transform.position)))
                .Concat(UnityEngine.Object.FindObjectsByType<MailaMarka>(FindObjectsInactive.Include).Where(m => m.Mota == MarkaMota.Etsaia).Select(t => (t.name, t.transform.position)))
                .ToList();

            string sektorea = d.Aurrizkia;
            List<AtrezzoPiezak.PiezaDef> lurrekoak = defs.Where(p => p.Sektorea == sektorea && !p.Horma).ToList();
            List<AtrezzoPiezak.PiezaDef> hormakoak = defs.Where(p => p.Sektorea == sektorea && p.Horma && !p.Lurra).ToList();
            List<AtrezzoPiezak.PiezaDef> lurOrbanak = defs.Where(p => p.Sektorea == sektorea && p.Lurra).ToList();
            BoxCollider[] gelak = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude)
                .Where(k => k.isTrigger && k.name.StartsWith("Gela_", StringComparison.Ordinal)).OrderBy(k => k.name, StringComparer.Ordinal).ToArray();

            var hautatuak = new List<(AtrezzoPiezak.PiezaDef Def, Vector3 P, float Yaw, string Gela, float Erradioa)>();
            // Misio osoko mugak mota bakoitzeko: errepikapenik ez (seinaleak eta orbanak bereziki).
            var kopuruak = new Dictionary<string, int>();
            int Muga(AtrezzoPiezak.PiezaDef p) => p.Izena.Contains("Seinalea") ? 5 : p.Lurra ? 10 : p.Horma ? 9 : 8;
            AtrezzoPiezak.PiezaDef Aukeratu(List<AtrezzoPiezak.PiezaDef> zerrenda, System.Random r)
            {
                List<AtrezzoPiezak.PiezaDef> libreak = zerrenda.Where(p => (kopuruak.TryGetValue(p.Izena, out int k) ? k : 0) < Muga(p)).ToList();
                return libreak.Count == 0 ? null : libreak[r.Next(libreak.Count)];
            }

            int baztertuak = 0;
            foreach (BoxCollider gela in gelak)
            {
                bool korridorea = gela.name.IndexOf("Pasillo", StringComparison.Ordinal) >= 0 || gela.name.IndexOf("Korridore", StringComparison.Ordinal) >= 0;
                Bounds b = gela.bounds;
                if (!Physics.Raycast(new Vector3(b.center.x, b.max.y - 0.1f, b.center.z), Vector3.down, out RaycastHit lurra, b.size.y + 1f, Geruzak, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                float zoruY = lurra.point.y;
                var ausazkoa = new System.Random(AtrezzoPiezak.Fnv(gela.name) ^ 0x5eed);
                int lurMuga = korridorea ? 0 : Mathf.Clamp(Mathf.RoundToInt(b.size.x * b.size.z / 14f), 1, 5);
                int hormaMuga = korridorea ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(b.size.x, b.size.z) / 7f), 1, 3) : 2;
                int orbanMuga = lurOrbanak.Count > 0 ? (korridorea ? 1 : 2) : 0;
                int lurKop = 0, hormaKop = 0, orbanKop = 0;
                Vector3 erdia = new Vector3(b.center.x, zoruY + 1.2f, b.center.z);
                foreach (Vector3 norabidea in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back }.OrderBy(_ => ausazkoa.Next()))
                {
                    float luzera = Mathf.Abs(Vector3.Dot(b.extents, norabidea)) + 1.5f;
                    if (!Physics.Raycast(erdia, norabidea, out RaycastHit horma, luzera, Geruzak, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    Vector3 tangentea = Vector3.Cross(Vector3.up, norabidea);
                    float erdiLuzera = Mathf.Abs(Vector3.Dot(b.extents, tangentea)) - 0.8f;
                    float yaw = Quaternion.LookRotation(-norabidea).eulerAngles.y;
                    for (float s = -erdiLuzera; s <= erdiLuzera; s += 1.4f)
                    {
                        // Mota aukeratu: lurrekoa / hormakoa / orbana, mugen arabera.
                        AtrezzoPiezak.PiezaDef def = null;
                        int aukera = ausazkoa.Next(10);
                        if (lurKop < lurMuga && lurrekoak.Count > 0 && aukera < 6)
                        {
                            def = Aukeratu(lurrekoak, ausazkoa);
                        }
                        else if (hormaKop < hormaMuga && hormakoak.Count > 0 && aukera < 9)
                        {
                            def = Aukeratu(hormakoak, ausazkoa);
                        }
                        else if (orbanKop < orbanMuga)
                        {
                            def = Aukeratu(lurOrbanak, ausazkoa);
                        }

                        if (def == null)
                        {
                            continue;
                        }

                        Vector3 hormaPuntua = new Vector3(horma.point.x, zoruY, horma.point.z) + tangentea * s;
                        float sakonera = def.Lurra ? def.Oina.z + 0.6f : def.Oina.z;
                        Vector3 p = hormaPuntua - norabidea * (sakonera * 0.5f + 0.08f);
                        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                        float erradioa = Mathf.Max(def.Oina.x, def.Oina.z) * 0.5f;
                        if (!Egokia(def, p, q, norabidea, tangentea, erradioa, puntuak, irteerak, hautatuak.Select(h => (h.P, h.Erradioa)).ToList(), Geruzak, out string arrazoia))
                        {
                            baztertuak++;
                            continue;
                        }

                        hautatuak.Add((def, p, yaw, gela.name, erradioa));
                        kopuruak[def.Izena] = (kopuruak.TryGetValue(def.Izena, out int zenbat) ? zenbat : 0) + 1;
                        if (def.Lurra) orbanKop++;
                        else if (def.Horma) hormaKop++;
                        else lurKop++;
                    }
                }
            }

            lerroak.Add($"DATUA: {gelak.Length} gela/korridore, {hautatuak.Count} pieza hautatuta ({hautatuak.Count(h => !h.Def.Horma)} lurrean kolisioarekin), {baztertuak} hautagai baztertuta");

            // NavMesh-a piezarik gabe eta piezekin: bide guztiek berdin jarraitu behar dute.
            NavMeshData oinarria = EszenaNavMesh(null);
            Dictionary<string, (NavMeshPathStatus Egoera, float Luzera)> aurretik = Bideak(oinarria, spawn, helburuak);
            var erroa = new GameObject(AtrezzoKargatzailea.ErroIzena);
            var instantziak = new List<(GameObject O, int I)>();
            for (int i = 0; i < hautatuak.Count; i++)
            {
                var o = (GameObject)PrefabUtility.InstantiatePrefab(piezak[hautatuak[i].Def.Izena], eszena);
                o.transform.SetPositionAndRotation(hautatuak[i].P, Quaternion.Euler(0f, hautatuak[i].Yaw, 0f));
                o.transform.SetParent(erroa.transform, true);
                instantziak.Add((o, i));
            }

            Physics.SyncTransforms();
            int kendutakoak = 0;
            for (int saioa = 0; saioa < 4; saioa++)
            {
                Dictionary<string, (NavMeshPathStatus Egoera, float Luzera)> ondoren = Bideak(EszenaNavMesh(null), spawn, helburuak);
                List<string> okerrak = aurretik.Where(kv => kv.Value.Egoera == NavMeshPathStatus.PathComplete
                    && (ondoren[kv.Key].Egoera != NavMeshPathStatus.PathComplete || ondoren[kv.Key].Luzera > kv.Value.Luzera * 1.1f + 1f)).Select(kv => kv.Key).ToList();
                if (okerrak.Count == 0)
                {
                    break;
                }

                // Hondatutako helburuetara doazen bideen inguruko lurreko piezak kendu.
                var kentzeko = new HashSet<int>();
                NavMeshDataInstance ins = NavMesh.AddNavMeshData(oinarria);
                foreach (string izena in okerrak)
                {
                    Vector3 h = helburuak.First(x => x.name == izena).position;
                    var bidea = new NavMeshPath();
                    if (NavMesh.SamplePosition(spawn, out NavMeshHit a, 1f, NavMesh.AllAreas) && NavMesh.SamplePosition(h, out NavMeshHit z, 1.5f, NavMesh.AllAreas)
                        && NavMesh.CalculatePath(a.position, z.position, NavMesh.AllAreas, bidea))
                    {
                        foreach ((GameObject o, int i) in instantziak.Where(x => x.O != null && !hautatuak[x.I].Def.Horma))
                        {
                            if (BidetikDistantzia(bidea.corners, o.transform.position) < hautatuak[i].Erradioa + 1.2f)
                            {
                                kentzeko.Add(i);
                            }
                        }
                    }
                }

                NavMesh.RemoveNavMeshData(ins);
                lerroak.Add($"OHARRA: {okerrak.Count} bide hondatuta ({string.Join(", ", okerrak.Take(4))}); {kentzeko.Count} pieza kenduta");
                foreach ((GameObject o, int i) in instantziak.Where(x => kentzeko.Contains(x.I) && x.O != null).ToList())
                {
                    UnityEngine.Object.DestroyImmediate(o);
                    kendutakoak++;
                }

                instantziak.RemoveAll(x => x.O == null);
                Physics.SyncTransforms();
                if (kentzeko.Count == 0)
                {
                    break;
                }
            }

            Dictionary<string, (NavMeshPathStatus Egoera, float Luzera)> azkena = Bideak(EszenaNavMesh(null), spawn, helburuak);
            int osoakAurretik = aurretik.Count(kv => kv.Value.Egoera == NavMeshPathStatus.PathComplete);
            int mantenduak = aurretik.Count(kv => kv.Value.Egoera == NavMeshPathStatus.PathComplete && azkena[kv.Key].Egoera == NavMeshPathStatus.PathComplete
                                                   && azkena[kv.Key].Luzera <= kv.Value.Luzera * 1.1f + 1f);
            bool bideOndo = mantenduak == osoakAurretik;
            lerroak.Add($"{(bideOndo ? "ONDO" : "HUTS")}: bideak spawn → {helburuak.Count} helburu: {osoakAurretik} oso piezarik gabe, {mantenduak} oso eta luzatu gabe piezekin ({kendutakoak} pieza kenduta)");

            List<AtrezzoDatuak.Elementua> elementuak = instantziak.Select(x => new AtrezzoDatuak.Elementua
            {
                Pieza = hautatuak[x.I].Def.Izena, Lekua = hautatuak[x.I].P, Yaw = hautatuak[x.I].Yaw, Gela = hautatuak[x.I].Gela,
            }).ToList();
            lerroak.Add($"DATUA: piezak motaka: {string.Join(", ", elementuak.GroupBy(e => e.Pieza).Select(g => $"{g.Key.Replace("PF_At_", string.Empty)} {g.Count()}"))}");

            // Aurretik / ondoren kapturak (Asset Store-ko kit-a: Ion-ek begiratzen ditu).
            Camera kamera = EbidentziaKapturak.ProbaKamera();
            string karpeta = QaKapturak.Azpikarpeta("Atrezzo");
            int kapturak = 0;
            foreach ((string izena, Vector3 begia, Vector3 helburua) in MailaKamerak(d))
            {
                kamera.transform.position = begia;
                kamera.transform.LookAt(helburua);
                erroa.SetActive(false);
                Texture2D a = EbidentziaKapturak.ProbaArgazkia(kamera, 960, 540);
                erroa.SetActive(true);
                Texture2D b = EbidentziaKapturak.ProbaArgazkia(kamera, 960, 540);
                Texture2D alboz = EbidentziaKapturak.ProbaAlboz(a, b);
                QaKapturak.Gorde(alboz.EncodeToPNG(), karpeta, $"ion_{d.Aurrizkia}_{izena}_aurretik_ondoren.png");
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
                UnityEngine.Object.DestroyImmediate(alboz);
                kapturak++;
            }

            UnityEngine.Object.DestroyImmediate(kamera.gameObject);
            UnityEngine.Object.DestroyImmediate(erroa);
            lerroak.Add($"DATUA: {kapturak} aurretik/ondoren kaptura: {karpeta}");

            // Datuak gorde (idenpotentea).
            string bidea2 = $"{AtDatuKarpeta}/AD_{d.Izena}.asset";
            var dagoena = AssetDatabase.LoadAssetAtPath<AtrezzoDatuak>(bidea2);
            bool berdina = dagoena != null && dagoena.Elementuak.Count == elementuak.Count
                           && dagoena.Elementuak.Zip(elementuak, (x, y) => x.Pieza == y.Pieza && (x.Lekua - y.Lekua).sqrMagnitude < 1e-6f && Mathf.Approximately(x.Yaw, y.Yaw)).All(v => v);
            if (berdina)
            {
                lerroak.Add($"0 aldaketa: {bidea2}");
            }
            else if (aplikatu)
            {
                if (!AssetDatabase.IsValidFolder(AtDatuKarpeta))
                {
                    AssetDatabase.CreateFolder("Assets/Resources", "Atrezzo");
                }

                if (dagoena == null)
                {
                    dagoena = ScriptableObject.CreateInstance<AtrezzoDatuak>();
                    dagoena.Ezarri(d.Izena, elementuak);
                    AssetDatabase.CreateAsset(dagoena, bidea2);
                }
                else
                {
                    dagoena.Ezarri(d.Izena, elementuak);
                    EditorUtility.SetDirty(dagoena);
                }

                AssetDatabase.SaveAssets();
                lerroak.Add($"Gordeta: {bidea2} ({elementuak.Count} pieza)");
            }
            else
            {
                lerroak.Add($"Aplikatuko litzateke: {bidea2} ({elementuak.Count} pieza)");
            }

            lerroak.Add(eszena.isDirty ? "OHARRA: eszena zikin dago memorian; EZ da gordetzen" : "Eszena garbi");
            return bideOndo;
        }

        private static bool Egokia(AtrezzoPiezak.PiezaDef def, Vector3 p, Quaternion q, Vector3 norabidea, Vector3 tangentea, float erradioa,
            List<Puntua> puntuak, Bounds[] irteerak, List<(Vector3 P, float Erradioa)> hautatuak, int geruzak, out string arrazoia)
        {
            arrazoia = null;
            float hormaAltuera = def.Horma && !def.Lurra ? 1.8f : 1.0f;
            float erdiZabalera = def.Oina.x * 0.5f - 0.05f;

            // Atzean horma jarraitua (ate-hutsunerik ez).
            foreach (float s in new[] { -erdiZabalera, 0f, erdiZabalera })
            {
                Vector3 o = p + tangentea * s + Vector3.up * hormaAltuera;
                if (!Physics.Raycast(o, norabidea, def.Oina.z * 0.5f + (def.Lurra ? 0.7f : 0.4f), geruzak, QueryTriggerInteraction.Ignore))
                {
                    arrazoia = "hutsunea";
                    return false;
                }
            }

            // Sabaia eta oztopoak.
            if (Physics.Raycast(p + Vector3.up * 0.1f, Vector3.up, def.Oina.y + 0.1f, geruzak, QueryTriggerInteraction.Ignore))
            {
                arrazoia = "sabaia";
                return false;
            }

            float behea = def.Lurra ? 0.02f : def.Horma ? 0.3f : 0.06f;
            Vector3 erdiTamaina = new Vector3(def.Oina.x * 0.5f + 0.05f, Mathf.Max(0.02f, (def.Oina.y - behea) * 0.5f), def.Oina.z * 0.5f + 0.02f);
            if (!def.Lurra && Physics.CheckBox(p + Vector3.up * (behea + erdiTamaina.y), erdiTamaina, q, geruzak, QueryTriggerInteraction.Ignore))
            {
                arrazoia = "solapamendua";
                return false;
            }

            Vector2 p2 = new Vector2(p.x, p.z);
            foreach (Puntua pt in puntuak)
            {
                if (pt.LurrekoakBakarrik && (def.Horma || def.Lurra))
                {
                    continue;
                }

                float muga = def.Horma ? Mathf.Min(pt.Muga, 1.2f) : pt.Muga;
                if (Mathf.Abs(pt.P.y - p.y) < 2.5f && Vector2.Distance(p2, new Vector2(pt.P.x, pt.P.z)) < muga + erradioa)
                {
                    arrazoia = pt.Izena;
                    return false;
                }
            }

            if (!def.Horma && irteerak.Any(b => b.Contains(p + Vector3.up * 0.5f)))
            {
                arrazoia = "irteera";
                return false;
            }

            if (hautatuak.Any(h => Vector2.Distance(p2, new Vector2(h.P.x, h.P.z)) < h.Erradioa + erradioa + 0.2f && Mathf.Abs(h.P.y - p.y) < 2f))
            {
                arrazoia = "beste pieza";
                return false;
            }

            return true;
        }

        private static Dictionary<string, (NavMeshPathStatus, float)> Bideak(NavMeshData datuak, Vector3 spawn, List<(string name, Vector3 position)> helburuak)
        {
            var emaitza = new Dictionary<string, (NavMeshPathStatus, float)>();
            NavMeshDataInstance ins = NavMesh.AddNavMeshData(datuak);
            try
            {
                bool hasiera = NavMesh.SamplePosition(spawn, out NavMeshHit a, 1f, NavMesh.AllAreas);
                foreach ((string izena, Vector3 h) in helburuak)
                {
                    var bidea = new NavMeshPath();
                    if (hasiera && NavMesh.SamplePosition(h, out NavMeshHit z, 1.5f, NavMesh.AllAreas) && NavMesh.CalculatePath(a.position, z.position, NavMesh.AllAreas, bidea))
                    {
                        float luzera = 0f;
                        for (int i = 1; i < bidea.corners.Length; i++)
                        {
                            luzera += Vector3.Distance(bidea.corners[i - 1], bidea.corners[i]);
                        }

                        emaitza[izena] = (bidea.status, luzera);
                    }
                    else
                    {
                        emaitza[izena] = (NavMeshPathStatus.PathInvalid, 0f);
                    }
                }
            }
            finally
            {
                NavMesh.RemoveNavMeshData(ins);
            }

            return emaitza;
        }

        private static float BidetikDistantzia(Vector3[] izkinak, Vector3 p)
        {
            float gutxienez = float.MaxValue;
            for (int i = 1; i < izkinak.Length; i++)
            {
                Vector3 a = izkinak[i - 1];
                Vector3 b = izkinak[i];
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                gutxienez = Mathf.Min(gutxienez, Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3((a + ab * t).x, 0f, (a + ab * t).z)));
            }

            return gutxienez;
        }

        /// <summary>Erakusketa neutroa: sektore bakoitzeko piezak horma baten kontra, Claude-k eta Ion-ek ikusteko (Asset Store-ko aktiborik gabe).</summary>
        private static string Erakusketa(List<AtrezzoPiezak.PiezaDef> defs, Dictionary<string, GameObject> piezak)
        {
            if (File.Exists(ErakusketaEszena))
            {
                EditorSceneManager.OpenScene(ErakusketaEszena, OpenSceneMode.Single);
                return ErakusketaKapturak("lehendik zegoen");
            }

            Scene eszena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material zorua = JokalariBisualTresna.ProbaMateriala("M_Proba_Zorua", Color.white, JokalariBisualTresna.Laukiak(), 20f);
            Material horma = JokalariBisualTresna.ProbaMateriala("M_Proba_Horma", new Color(0.55f, 0.57f, 0.6f));
            var erroa = new GameObject("AtrezzoErakusketa").transform;
            JokalariBisualTresna.ArgiNeutroak(erroa);
            JokalariBisualTresna.Kubo("Zorua", erroa, new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), zorua);
            float z = -12f;
            foreach (string sektorea in new[] { "M1", "M2", "M3" })
            {
                JokalariBisualTresna.Kubo($"Horma_{sektorea}", erroa, new Vector3(0f, 1.5f, z - 0.25f), new Vector3(36f, 3f, 0.5f), horma);
                float x = -16f;
                foreach (AtrezzoPiezak.PiezaDef def in defs.Where(d => d.Sektorea == sektorea))
                {
                    float zabalera = Mathf.Max(def.Oina.x, 1f);
                    var o = (GameObject)PrefabUtility.InstantiatePrefab(piezak[def.Izena], eszena);
                    float sakonera = def.Lurra ? def.Oina.z + 0.6f : def.Oina.z;
                    o.transform.SetPositionAndRotation(new Vector3(x + zabalera * 0.5f, 0f, z + sakonera * 0.5f + 0.05f), Quaternion.identity);
                    o.transform.SetParent(erroa, true);
                    x += zabalera + 0.6f;
                }

                z += 12f;
            }

            var kamera = new GameObject("Main Camera") { tag = "MainCamera" };
            kamera.transform.SetParent(erroa, false);
            kamera.transform.SetPositionAndRotation(new Vector3(0f, 6f, -22f), Quaternion.Euler(25f, 0f, 0f));
            Camera c = kamera.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
            EditorSceneManager.SaveScene(eszena, ErakusketaEszena);
            return ErakusketaKapturak("berria");
        }

        /// <summary>Erakusketaren kapturak: sektore bakoitza, ezkerra eta eskuina (neutroa, Claude-k berrikusten du).</summary>
        private static string ErakusketaKapturak(string egoera)
        {
            Camera k = EbidentziaKapturak.ProbaKamera();
            float z;
            string karpeta = QaKapturak.Azpikarpeta("Atrezzo");
            z = -12f;
            foreach (string sektorea in new[] { "M1", "M2", "M3" })
            {
                foreach ((string izena, float x0, float x1) in new[] { ("ezkerra", -16f, -2f), ("eskuina", -2f, 16f) })
                {
                    float erdia = (x0 + x1) * 0.5f;
                    k.transform.position = new Vector3(erdia, 2.4f, z + 7.5f);
                    k.transform.LookAt(new Vector3(erdia, 1.1f, z + 0.5f));
                    Texture2D t = EbidentziaKapturak.ProbaArgazkia(k, 1600, 900);
                    QaKapturak.Gorde(t.EncodeToPNG(), karpeta, $"neutro_{sektorea}_erakusketa_{izena}.png");
                    UnityEngine.Object.DestroyImmediate(t);
                }

                z += 12f;
            }

            UnityEngine.Object.DestroyImmediate(k.gameObject);
            return $"Erakusketa-eszena ({egoera}): {ErakusketaEszena}; kapturak {karpeta}";
        }
    }
}
