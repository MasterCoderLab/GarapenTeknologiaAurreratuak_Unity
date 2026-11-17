using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eclipse.Borroka;
using Eclipse.Npc;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3 (E blokea): NPC-en eta desmenbratzearen proba Play moduan, 06_NpcProba eszenan (batch). Eszenarik ez da gordetzen.
    /// Zientzialariak lanean, soldaduak patruilan, begiratzea, munstroa agertzean ihesa eta tiroak, gorpu aldi baterakoa, lanera
    /// itzultzea, eta jomuga geldian hanka/beso zatiak, herrena, narrastaka eta garbiketa. Kapturak neutro_* (eszena propioa eta CC0).
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string NpMarka = "ECL_NPCPROBA";
        private const double NpDenbora = 400.0;
        private static double _npHasiera;
        private static readonly List<string> NpErroreak = new List<string>();

        public static void BatchNpcProba()
        {
            try
            {
                EditorSceneManager.OpenScene(NpcTresna.ProbaEszena, OpenSceneMode.Single);
                NpErroreak.Clear();
                Application.logMessageReceived += NpLog;
                _npHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += NpEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{NpMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void NpLog(string mezua, string pila, LogType mota)
        {
            if ((mota == LogType.Error || mota == LogType.Exception) && !mezua.StartsWith(NpMarka, StringComparison.Ordinal) && NpErroreak.Count < 20)
            {
                NpErroreak.Add($"{mota}: {mezua.Split('\n')[0]}");
            }
        }

        private static void NpEguneratu()
        {
            NpcProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<NpcProbaKontrola>();
            if (EditorApplication.timeSinceStartup - _npHasiera > NpDenbora)
            {
                var l = kontrola != null ? kontrola.Lerroak : new List<string>();
                l.Add("HUTS: denbora agortuta");
                NpAmaitu(l);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (kontrola == null)
            {
                EtsaiSortzailea sortzailea = UnityEngine.Object.FindAnyObjectByType<EtsaiSortzailea>();
                if (UnityEngine.Object.FindAnyObjectByType<JokalariArma>() != null && sortzailea != null && sortzailea.Sortuak.Count > 0)
                {
                    kontrola = new GameObject("NpcProbaKontrola").AddComponent<NpcProbaKontrola>();
                    kontrola.Hasi();
                }

                return;
            }

            if (kontrola.Amaituta)
            {
                NpAmaitu(kontrola.Lerroak);
            }
        }

        private static void NpAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= NpEguneratu;
            Application.logMessageReceived -= NpLog;
            lerroak.Add(NpErroreak.Count == 0 ? "ONDO: Play-n 0 errore/salbuespen log-ean" : $"HUTS: Play-n {NpErroreak.Count} errore: {string.Join(" | ", NpErroreak)}");
            var informea = new Informea("NpcProba", NpMarka, "A3 E: NPC-ak eta desmenbratzea Play-n (06_NpcProba)", NpcTresna.ProbaEszena, "Npc");
            informea.Atala("Emaitzak", lerroak);
            int hutsak = lerroak.Count(l => l.StartsWith("HUTS", StringComparison.Ordinal));
            if (hutsak == 0)
            {
                informea.AmaituOndo($"{lerroak.Count(l => l.StartsWith("ONDO", StringComparison.Ordinal))} egiaztapen, 0 huts");
            }
            else
            {
                informea.AmaituErrorearekin($"{hutsak} huts");
            }

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(hutsak == 0 ? 0 : 1);
        }

        private sealed class NpcProbaKontrola : MonoBehaviour
        {
            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;
            private CharacterController _cc;
            private Camera _kamera;
            private string _kapturak;

            private void Oharra(string izena, bool ondo, string x = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena}{(string.IsNullOrEmpty(x) ? string.Empty : $" ({x})")}");

            public void Hasi()
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Exekutatu());
            }

            private static IEnumerator Itxaron(float s)
            {
                float amaiera = Time.time + s;
                while (Time.time < amaiera)
                {
                    yield return null;
                }
            }

            /// <summary>Baldintza bete arte (edo denbora-muga): betetzeko behar izan den denbora (s), edo -1.</summary>
            private IEnumerator Arte(Func<bool> baldintza, float muga, Action<float> emaitza)
            {
                float hasiera = Time.time;
                while (!baldintza())
                {
                    if (Time.time - hasiera > muga)
                    {
                        emaitza(-1f);
                        yield break;
                    }

                    yield return null;
                }

                emaitza(Time.time - hasiera);
            }

            private IEnumerator Kokatu(Vector3 oinak, float yaw)
            {
                _cc.enabled = false;
                _cc.transform.SetPositionAndRotation(oinak, Quaternion.Euler(0f, yaw, 0f));
                _cc.enabled = true;
                Physics.SyncTransforms();
                yield return Itxaron(0.2f);
            }

            private IEnumerator Argazkia(string izena, Vector3 lekua, Vector3 begira)
            {
                yield return null;
                _kamera.transform.position = lekua;
                _kamera.transform.LookAt(begira);
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _kamera.targetTexture = rt;
                _kamera.Render();
                RenderTexture.active = rt;
                var irudia = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                irudia.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                irudia.Apply();
                RenderTexture.active = null;
                _kamera.targetTexture = null;
                QaKapturak.Gorde(irudia.EncodeToPNG(), _kapturak, $"neutro_{izena}.png");
                Destroy(irudia);
                rt.Release();
                Destroy(rt);
            }

            private static Etsaia Hurbilena(IEnumerable<Etsaia> etsaiak, string marka)
            {
                Vector3 p = GameObject.Find(marka).transform.position;
                return etsaiak.OrderBy(e => Vector3.Distance(e.transform.position, p)).First();
            }

            private static float Horiz(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

            private IEnumerator Exekutatu()
            {
                yield return Itxaron(1.5f);
                EsploratzaileKontrola jokalaria = FindAnyObjectByType<EsploratzaileKontrola>();
                _cc = jokalaria.GetComponent<CharacterController>();
                JokalariBizitza bizitza = jokalaria.GetComponent<JokalariBizitza>();
                jokalaria.enabled = false;
                _kapturak = QaKapturak.Azpikarpeta("A3_Npc");
                _kamera = new GameObject("NpcProba_Kamera").AddComponent<Camera>();
                _kamera.enabled = false;
                _kamera.fieldOfView = 50f;
                _kamera.nearClipPlane = 0.05f;

                NpcKontrola[] npcak = FindObjectsByType<NpcKontrola>(FindObjectsInactive.Exclude).OrderBy(n => n.name, StringComparer.Ordinal).ToArray();
                NpcKontrola[] zientzialariak = npcak.Where(n => n.Rola == NpcRola.Zientzialaria).ToArray();
                NpcKontrola[] soldaduak = npcak.Where(n => n.Rola == NpcRola.Soldadua).ToArray();
                Etsaia[] etsaiak = FindAnyObjectByType<EtsaiSortzailea>().Sortuak.ToArray();
                Etsaia munstroa = Hurbilena(etsaiak, "Din_Marka_Etsaia_Biltegia");
                Etsaia jomuga = Hurbilena(etsaiak, "Din_Marka_Etsaia_Jomuga");

                // ---------- Konfigurazioa ----------
                Oharra("Jokalari bakarra, armarekin", FindObjectsByType<CharacterController>(FindObjectsInactive.Include).Length == 1 && jokalaria.GetComponent<JokalariArma>() != null);
                Oharra("3 zientzialari eta 2 soldadu", zientzialariak.Length == 3 && soldaduak.Length == 2, string.Join(", ", npcak.Select(n => n.name)));
                Oharra("NPC-ak ez dira etsaiak (rola)", npcak.All(n => !NpcEgoeraMakina.Etsaia(n.Rola)));
                Oharra("NPC guztiak NavMesh-ean", npcak.All(n => n.GetComponent<NavMeshAgent>().isOnNavMesh));
                Oharra("NPC-ek ez dute kalte-zonarik (jokalariaren tiroek ez diete eragiten)", npcak.All(n => n.GetComponentInChildren<KalteZonaKolisioa>() == null && n.GetComponent<IKalteHartzailea>() == null));
                Oharra("2 etsai desmenbragarri (runtime, markatzaileetan)", etsaiak.Length == 2 && munstroa != jomuga && etsaiak.All(e => e.GetComponent<Desmenbratzea>() != null),
                    string.Join(", ", etsaiak.Select(e => e.name)));
                Oharra($"Etsai desmenbragarriaren bizitza {NpcTresna.DesmenBizitza}", etsaiak.All(e => Mathf.Approximately(e.Bizitza.Gehienez, NpcTresna.DesmenBizitza)));
                Oharra("Jomuga geldia (IA geldituta)", jomuga.AdimenaGelditu && !munstroa.AdimenaGelditu);
                float bizitza0 = bizitza.Logika.Unekoa;

                // ---------- Lanean eta patruilan ----------
                // Patruila joan-etorria da: ibilitako distantzia metatua neurtu (ez hasiera-amaiera).
                Vector3[] aurrekoak = soldaduak.Select(s => s.transform.position).ToArray();
                float[] mugituak = new float[soldaduak.Length];
                for (float amaiera = Time.time + 8f; Time.time < amaiera;)
                {
                    yield return Itxaron(0.25f);
                    for (int i = 0; i < soldaduak.Length; i++)
                    {
                        mugituak[i] += Horiz(soldaduak[i].transform.position, aurrekoak[i]);
                        aurrekoak[i] = soldaduak[i].transform.position;
                    }
                }

                Oharra("Zientzialariak lanean, lan-lekuan", zientzialariak.All(z => z.Egoera == NpcEgoera.Lanean && z.LanLekuan),
                    string.Join(", ", zientzialariak.Select(z => $"{z.name} {z.Egoera} {Horiz(z.transform.position, z.LanLekua.position):0.00} m")));
                Oharra("Soldaduak patruilan (≥ 3 m ibilita 8 s-tan)", soldaduak.All(s => s.Egoera == NpcEgoera.Patruila) && mugituak.All(m => m >= 3f),
                    string.Join(", ", mugituak.Select(m => $"{m:0.0} m")));
                Oharra("Munstroa biltegian zain (jokalaria ikusi gabe)", munstroa.Egoera == EtsaiEgoera.Zain, munstroa.Egoera.ToString());
                yield return Argazkia("01_ikuspegia", new Vector3(-16f, 9f, -12f), new Vector3(0f, 0f, -3f));
                NpcKontrola z1 = zientzialariak[0];
                yield return Argazkia("02_zientzialaria_lanean", z1.LanLekua.position + new Vector3(2.2f, 1.6f, 1.6f), z1.transform.position + Vector3.up * 1.1f);

                // ---------- Begiratu ----------
                yield return Kokatu(z1.LanLekua.position + new Vector3(0f, 0f, 2.3f), 180f);
                yield return Itxaron(2f);
                Vector3 nora = jokalaria.transform.position - z1.transform.position;
                nora.y = 0f;
                float angelua = Vector3.Angle(z1.transform.forward, nora);
                Oharra("Jokalaria hurbil: zientzialariak begiratu egiten du", z1.Egoera == NpcEgoera.Begiratu && angelua < 35f, $"{z1.Egoera}, {angelua:0} gradu");
                Oharra("Urruneko zientzialaria lanean jarraitzen du", zientzialariak[2].Egoera == NpcEgoera.Lanean);
                yield return Argazkia("03_begiratu", z1.transform.position + new Vector3(2.6f, 1.7f, 1.2f), z1.transform.position + new Vector3(0f, 1.3f, 1f));
                // Munstroa geldi lehenengo (laborategira eramango da), eta jokalaria biltegiko ekialdera: NPC-en ihes- eta patruila-bideetatik urrun (hurbil balego, begiratzen geratuko lirateke).
                munstroa.AdimenaGelditu = true;
                yield return Kokatu(new Vector3(14f, 0f, 10f), 180f);
                float laneraBueltan = -1f;
                yield return Arte(() => z1.Egoera == NpcEgoera.Lanean, 4f, t => laneraBueltan = t);
                Oharra("Jokalaria urrundu ondoren, lanera itzultzen da", laneraBueltan >= 0f, $"{laneraBueltan:0.0} s");

                // ---------- Mehatxua: munstroa laborategian ----------
                var desmen = munstroa.GetComponent<Desmenbratzea>();
                desmen.HilotzIraupena = 3f;
                desmen.ZatiIraupena = 3f;
                munstroa.AdimenaGelditu = true;
                munstroa.GetComponent<NavMeshAgent>().Warp(new Vector3(-2f, 0f, -7f));
                float[] d0 = zientzialariak.Select(z => Horiz(z.transform.position, munstroa.transform.position)).ToArray();
                float hp0 = munstroa.Bizitza.Unekoa;
                yield return Itxaron(1.2f);
                yield return Argazkia("04_mehatxua_ihes", new Vector3(-12f, 5.5f, -1.5f), new Vector3(-3f, 0.5f, -9f));
                yield return Itxaron(2.5f);
                float[] d1 = zientzialariak.Select(z => munstroa != null ? Horiz(z.transform.position, munstroa.transform.position) : 99f).ToArray();
                bool ihes = zientzialariak.All(z => z.Egoera == NpcEgoera.Ihes);
                Oharra("Munstroa ikustean zientzialari guztiek ihes egiten dute", ihes, string.Join(", ", zientzialariak.Select(z => z.Egoera.ToString())));
                Oharra("Ihesean munstrotik urruntzen dira (≥ 2 m)", d1.Select((d, i) => d - d0[i]).All(d => d >= 2f),
                    string.Join(", ", d0.Select((d, i) => $"{d:0.0}→{d1[i]:0.0} m")));
                Oharra("Soldaduek munstroari aurre egiten diote (Zaindu) eta tiro egiten dute", soldaduak.All(s => s.Egoera == NpcEgoera.Zaindu && s.Tiroak > 0),
                    string.Join(", ", soldaduak.Select(s => $"{s.name} {s.Egoera} {s.Tiroak} tiro/{s.Jotakoak} jo")));
                Oharra("Soldaduen tiroek munstroa zauritzen dute", munstroa != null && munstroa.Bizitza.Unekoa < hp0, munstroa != null ? $"{hp0:0} → {munstroa.Bizitza.Unekoa:0}" : "");
                NpcKontrola s1 = soldaduak[0];
                yield return Argazkia("05_soldadua_tiro", s1.transform.position + s1.transform.right * 2.2f + new Vector3(0f, 1.6f, 0f) - s1.transform.forward * 1.5f,
                    s1.transform.position + s1.transform.forward * 3f + Vector3.up * 1.2f);
                float hilArte = -1f;
                yield return Arte(() => munstroa == null || !munstroa.Bizirik, 25f, t => hilArte = t);
                Oharra("Soldaduek munstroa hiltzen dute", hilArte >= 0f, $"{hilArte:0.0} s gehiago");
                float ezabatuArte = -1f;
                yield return Arte(() => munstroa == null, 8f, t => ezabatuArte = t);
                Oharra("Gorpua aldi baterakoa: hondoratu eta ezabatzen da", ezabatuArte >= 0f, $"{ezabatuArte:0.0} s (HilotzIraupena 3 s + hondoratzea)");
                float bueltan = -1f;
                yield return Arte(() => zientzialariak.All(z => z.Egoera == NpcEgoera.Lanean && z.LanLekuan), 30f, t => bueltan = t);
                Oharra("Arriskua igarota, zientzialariak lan-lekura itzultzen dira", bueltan >= 0f,
                    $"{bueltan:0.0} s; {string.Join(", ", zientzialariak.Select(z => $"{z.Egoera} {Horiz(z.transform.position, z.LanLekua.position):0.0} m"))}");
                Oharra("Soldaduak patruilara itzultzen dira", soldaduak.All(s => s.Egoera == NpcEgoera.Patruila), string.Join(", ", soldaduak.Select(s => s.Egoera.ToString())));
                Oharra("Jokalariaren bizitza ez da aldatu (NPC-ek ez diote inoiz erasotzen)", Mathf.Approximately(bizitza.Logika.Unekoa, bizitza0), $"{bizitza.Logika.Unekoa:0}");

                // ---------- Desmenbratzea: jomuga geldia ----------
                var d2 = jomuga.GetComponent<Desmenbratzea>();
                d2.ZatiIraupena = 5f;
                d2.HilotzIraupena = 3f;
                Animator an = jomuga.Animatzailea;
                Vector3 Hezurra(HumanBodyBones h) => an.GetBoneTransform(h).position;
                Vector3 tiroNorabidea = (jomuga.transform.position - new Vector3(-10f, 0f, 3f)).normalized;
                Vector3 kamera = jomuga.transform.position + new Vector3(3.2f, 1.6f, -2.4f);
                Vector3 begira = jomuga.transform.position + Vector3.up * 0.8f;

                jomuga.KalteaJaso(new KalteInfo(KalteZona.HankaEzkerra, 54f, Hezurra(HumanBodyBones.LeftLowerLeg), tiroNorabidea));
                yield return null;
                yield return null;
                GameObject hanka = d2.Zatiak.Count > 0 ? d2.Zatiak[0] : null;
                SkinnedMeshRenderer[] hankaR = hanka != null ? hanka.GetComponentsInChildren<SkinnedMeshRenderer>() : new SkinnedMeshRenderer[0];
                Oharra("Hanka ezkerra: zatia bereizita (Rigidbody + malla)", hanka != null && hanka.GetComponent<Rigidbody>() != null && hankaR.Length > 0);
                float zatiTamaina = hankaR.Length > 0 ? hankaR.Max(r => r.bounds.size.magnitude) : 99f;
                Oharra("Zatiak hanka bakarrik erakusten du (gorputza ebakiduran bilduta)", zatiTamaina < 1.1f, $"muga {zatiTamaina:0.00} m");
                Oharra("Etsaian hanka ezkutatuta (hezurra eskala ~0)", an.GetBoneTransform(HumanBodyBones.LeftLowerLeg).lossyScale.x < 0.01f);
                Oharra("Herren: abiadura x0,5", Mathf.Approximately(jomuga.AbiaduraFaktorea, 0.5f), $"x{jomuga.AbiaduraFaktorea:0.##}");
                Oharra("Odol-zurrusta ebakiduran", jomuga.GetComponentsInChildren<ParticleSystem>().Any(p => p.name == "Zurrusta" && p.isPlaying));
                Oharra("Odol-orbana lurrean", d2.Orbanak.Count(o => o != null) >= 1);
                yield return Itxaron(1.5f);
                Rigidbody rb = hanka != null ? hanka.GetComponent<Rigidbody>() : null;
                Oharra("Zatia lurrera erori da eta ez du zorua zeharkatzen", rb != null && rb.position.y > -0.05f && rb.position.y < 0.6f,
                    rb != null ? $"y {rb.position.y:0.00}, abiadura {rb.linearVelocity.magnitude:0.00}" : "");
                yield return Argazkia("06_desmenbratzea_hanka", kamera, begira);

                jomuga.KalteaJaso(new KalteInfo(KalteZona.BesoEskuina, 67f, Hezurra(HumanBodyBones.RightLowerArm), tiroNorabidea));
                yield return null;
                Oharra("Beso eskuina: bigarren zatia eta erasoa x0,6", d2.Zatiak.Count == 2 && Mathf.Approximately(jomuga.ErasoFaktorea, 0.6f), $"{d2.Zatiak.Count} zati, x{jomuga.ErasoFaktorea:0.##}");
                jomuga.KalteaJaso(new KalteInfo(KalteZona.HankaEskuina, 54f, Hezurra(HumanBodyBones.RightLowerLeg), tiroNorabidea));
                yield return null;
                Oharra("Bi hankak galduta: narrastaka (Animator) eta abiadura x0,3", an.GetBool("Narrastaka") && Mathf.Approximately(jomuga.AbiaduraFaktorea, 0.3f), $"x{jomuga.AbiaduraFaktorea:0.##}");
                Oharra("Hiru zati galduta bizirik jarraitzen du", jomuga.Bizirik, $"{jomuga.Bizitza.Unekoa:0}/{jomuga.Bizitza.Gehienez:0}");
                yield return Itxaron(1.2f);
                yield return Argazkia("07_desmenbratzea_narrastaka", kamera, begira);

                jomuga.KalteaJaso(new KalteInfo(KalteZona.Burua, 25f, Hezurra(HumanBodyBones.Head), tiroNorabidea));
                Oharra("Buruan tiroa: hilda", !jomuga.Bizirik);
                yield return Itxaron(1.2f);
                yield return Argazkia("08_hilda", kamera, begira);
                List<GameObject> zatiak = d2.Zatiak.ToList();
                List<GameObject> orbanak = d2.Orbanak.ToList();
                float garbiArte = -1f;
                yield return Arte(() => jomuga == null && zatiak.All(z => z == null) && orbanak.All(o => o == null), 9f, t => garbiArte = t);
                Oharra("Gorpua, zatiak eta orbanak ezabatuta (aldi baterakoak)", garbiArte >= 0f, $"{garbiArte:0.0} s");
                Amaituta = true;
            }
        }
    }
}
