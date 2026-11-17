using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Atrezzo;
using Eclipse.Borroka;
using Eclipse.Misioak;
using Eclipse.Npc;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A4: misioaren eduki dinamikoaren proba Play-n (-eclipseMaila 1|2|3), Editoreko demo-banderarik GABE: Resources/Misioak/ME_* bakarrik.
    /// Atrezzoa, arma, etsai-kopurua narratiboaren arabera, NPC-ak NavMesh-ean eta lanean/patruilan, jokalaria kaltetu gabe NPC-ek,
    /// eta M3-n misioa amaitzean 07_AmaieraZinematika kargatzen dela. Eszenarik ez da gordetzen.
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string MpMarka = "ECL_MISIOEDUKIPROBA";
        private static MailaDef _mpMaila;
        private static double _mpHasiera;
        private static readonly List<string> MpErroreak = new List<string>();

        public static void BatchMisioEdukiProba()
        {
            try
            {
                _mpMaila = BatchMailaLortu();
                EditorSceneManager.OpenScene(_mpMaila.Bidea, OpenSceneMode.Single);
                MpErroreak.Clear();
                Application.logMessageReceived += MpLog;
                _mpHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += MpEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{MpMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void MpLog(string mezua, string pila, LogType mota)
        {
            if ((mota == LogType.Error || mota == LogType.Exception) && !mezua.StartsWith(MpMarka, StringComparison.Ordinal) && MpErroreak.Count < 20)
            {
                MpErroreak.Add($"{mota}: {mezua.Split('\n')[0]}");
            }
        }

        private static void MpEguneratu()
        {
            MisioEdukiProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<MisioEdukiProbaKontrola>();
            if (EditorApplication.timeSinceStartup - _mpHasiera > 240.0)
            {
                var l = kontrola != null ? kontrola.Lerroak : new List<string>();
                l.Add("HUTS: denbora agortuta");
                MpAmaitu(l);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (kontrola == null)
            {
                if (UnityEngine.Object.FindAnyObjectByType<EsploratzaileKontrola>() != null)
                {
                    kontrola = new GameObject("MisioEdukiProbaKontrola").AddComponent<MisioEdukiProbaKontrola>();
                    kontrola.Hasi(_mpMaila);
                }

                return;
            }

            if (kontrola.Amaituta)
            {
                MpAmaitu(kontrola.Lerroak);
            }
        }

        private static void MpAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= MpEguneratu;
            Application.logMessageReceived -= MpLog;
            lerroak.Add(MpErroreak.Count == 0 ? "ONDO: Play-n 0 errore/salbuespen log-ean" : $"HUTS: Play-n {MpErroreak.Count} errore: {string.Join(" | ", MpErroreak)}");
            var informea = new Informea("MisioEdukiProba", MpMarka, $"{_mpMaila.Izena}: eduki dinamikoa (Play, demo-banderarik gabe)", _mpMaila.Bidea, _mpMaila.Erroa);
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

        private sealed class MisioEdukiProbaKontrola : MonoBehaviour
        {
            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;

            private void Oharra(string izena, bool ondo, string x = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena}{(string.IsNullOrEmpty(x) ? string.Empty : $" ({x})")}");

            public void Hasi(MailaDef maila)
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Exekutatu(maila));
            }

            private static float Horiz(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

            private IEnumerator Exekutatu(MailaDef maila)
            {
                float muga = Time.time + 10f;
                MisioEdukiKargatzailea kargatzailea = null;
                while (Time.time < muga && (kargatzailea == null || !kargatzailea.Aplikatuta))
                {
                    kargatzailea = FindAnyObjectByType<MisioEdukiKargatzailea>();
                    yield return null;
                }

                Oharra("MisioEdukia kargatuta eta aplikatuta (Resources/Misioak)", kargatzailea != null && kargatzailea.Aplikatuta);
                if (kargatzailea == null)
                {
                    Amaituta = true;
                    yield break;
                }

                yield return new WaitForSeconds(1f);
                MisioEdukia edukia = kargatzailea.Edukia;
                EsploratzaileKontrola jokalaria = FindAnyObjectByType<EsploratzaileKontrola>();
                LevelManager km = FindAnyObjectByType<LevelManager>();
                Scene eszena = kargatzailea.gameObject.scene;
                Oharra("Demo-banderak ez dira behar (BorrokaDemoa-rik ez)", FindAnyObjectByType<BorrokaDemoa>() == null);
                Oharra("Atrezzoa misioan", eszena.GetRootGameObjects().Any(o => o.name == AtrezzoKargatzailea.ErroIzena) == edukia.AtrezzoAktibo, $"aktibo {edukia.AtrezzoAktibo}");
                bool armaDu = jokalaria.GetComponent<JokalariArma>() != null;
                Oharra($"EMP pistola {(edukia.BorrokaAktibo ? "bai" : "ez")} (narratiba)", armaDu == edukia.BorrokaAktibo);
                Etsaia[] etsaiak = FindObjectsByType<Etsaia>(FindObjectsInactive.Exclude);
                int markak = FindObjectsByType<MailaMarka>(FindObjectsInactive.Exclude).Count(m => m.Mota == MarkaMota.Etsaia);
                int espero = !edukia.BorrokaAktibo ? 0 : edukia.EtsaiKopurua < 0 ? markak : Math.Min(edukia.EtsaiKopurua, markak);
                Oharra($"Etsai-kopurua {espero}", etsaiak.Length == espero, $"{etsaiak.Length} etsai, {markak} markatzaile");
                Oharra("Etsaiak NavMesh-ean", etsaiak.All(e => e.GetComponent<NavMeshAgent>().isOnNavMesh));
                NpcKontrola[] npcak = FindObjectsByType<NpcKontrola>(FindObjectsInactive.Exclude);
                Oharra($"NPC-kopurua {edukia.Npcak.Count}", npcak.Length == edukia.Npcak.Count, $"{npcak.Length}");
                Oharra("NPC guztiak NavMesh-ean", npcak.All(n => n.GetComponent<NavMeshAgent>().isOnNavMesh));
                Oharra("NPC-ek ez dute kolisio solidorik (korridoreak libre)", npcak.All(n => n.GetComponent<Collider>().isTrigger));

                if (npcak.Length > 0)
                {
                    JokalariBizitza bizitza = jokalaria.GetComponent<JokalariBizitza>();
                    float bizitza0 = bizitza != null ? bizitza.Logika.Unekoa : -1f;
                    Vector3[] aurrekoak = npcak.Select(n => n.transform.position).ToArray();
                    float[] ibilia = new float[npcak.Length];
                    for (float amaiera = Time.time + 10f; Time.time < amaiera;)
                    {
                        yield return new WaitForSeconds(0.25f);
                        for (int i = 0; i < npcak.Length; i++)
                        {
                            ibilia[i] += Horiz(npcak[i].transform.position, aurrekoak[i]);
                            aurrekoak[i] = npcak[i].transform.position;
                        }
                    }

                    NpcKontrola[] zientzialariak = npcak.Where(n => n.Rola == NpcRola.Zientzialaria).ToArray();
                    NpcKontrola[] soldaduak = npcak.Where(n => n.Rola == NpcRola.Soldadua).ToArray();
                    if (zientzialariak.Length > 0)
                    {
                        Oharra("Zientzialariak lan-lekuan lanean", zientzialariak.All(z => z.Egoera == NpcEgoera.Lanean && z.LanLekuan),
                            string.Join(", ", zientzialariak.Select(z => $"{z.Egoera}{(z.LanLekuan ? "" : "?")}")));
                    }

                    if (soldaduak.Length > 0)
                    {
                        float[] s = soldaduak.Select(x => ibilia[Array.IndexOf(npcak, x)]).ToArray();
                        Oharra("Soldaduak patruilan (≥ 3 m ibilita 10 s-tan)", soldaduak.All(x => x.Egoera == NpcEgoera.Patruila || x.Egoera == NpcEgoera.Begiratu) && s.All(m => m >= 3f),
                            string.Join(", ", s.Select(m => $"{m:0.0} m")));
                    }

                    Oharra("NPC-ek ez diote jokalariari kalterik egin", bizitza == null || Mathf.Approximately(bizitza.Logika.Unekoa, bizitza0));
                }

                // Misioaren amaiera: hurrengo misioa edo (azkenean) amaierako zinematika.
                bool azkena = string.IsNullOrEmpty(km.HurrengoEszena);
                Oharra(azkena ? "Azken misioa: amaierako eszena konfiguratuta" : "Hurrengo misioa konfiguratuta, amaierako eszenarik gabe",
                    azkena ? edukia.AmaieraEszena == AmaieraEszenaIzena : string.IsNullOrEmpty(edukia.AmaieraEszena), $"'{km.HurrengoEszena}' / '{edukia.AmaieraEszena}'");
                if (azkena)
                {
                    foreach (string id in maila.Misioa.Faseak.SelectMany(f => f.Helburuak))
                    {
                        km.HelburuaBete(id);
                    }

                    Oharra("Helburu guztiak beteta (programatikoki)", km.Logika.Amaituta);
                    km.IrteeraLortu();
                    float itxaron = Time.time + 8f;
                    while (Time.time < itxaron && SceneManager.GetActiveScene().name != AmaieraEszenaIzena)
                    {
                        yield return null;
                    }

                    yield return new WaitForSeconds(1f);
                    PlayableDirector zuzendaria = FindAnyObjectByType<PlayableDirector>();
                    Oharra($"Misioa amaitzean {AmaieraEszenaIzena} kargatzen da eta zinematika martxan", SceneManager.GetActiveScene().name == AmaieraEszenaIzena
                        && zuzendaria != null && zuzendaria.state == PlayState.Playing, SceneManager.GetActiveScene().name);
                }

                Amaituta = true;
            }
        }
    }
}
