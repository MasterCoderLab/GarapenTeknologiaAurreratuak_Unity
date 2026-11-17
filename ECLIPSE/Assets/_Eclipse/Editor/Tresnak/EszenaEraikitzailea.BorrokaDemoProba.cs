using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Borroka;
using Eclipse.Hud;
using Eclipse.Misioak;
using Eclipse.Proba;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.EditorTresnak
{
    /// <summary>
    /// A3: misio-demoaren proba (Play, batch, -eclipseMaila 1|2|3 -eclipseBorrokaDemoa). Eszenarik ez da gordetzen.
    /// LevelManager-en fluxua + BorrokaDemoa: arma, NavMesh-a, etsai-kopurua misioaren arabera, eta etsai alertatu bat misioaren
    /// geometrian zehar jokalariarengana doa. Kapturak ion_* (Asset Store-ko kit-a: Ion-ek begiratzen ditu).
    /// </summary>
    public static partial class EszenaEraikitzailea
    {
        private const string BdMarka = "ECL_BORROKADEMOPROBA";
        private static MailaDef _bdMaila;
        private static double _bdHasiera;
        private static readonly List<string> BdErroreak = new List<string>();

        public static void BatchBorrokaDemoProba()
        {
            try
            {
                _bdMaila = BatchMailaLortu();
                EditorSceneManager.OpenScene(_bdMaila.Bidea, OpenSceneMode.Single);
                BdErroreak.Clear();
                Application.logMessageReceived += BdLog;
                _bdHasiera = EditorApplication.timeSinceStartup;
                EditorApplication.update += BdEguneratu;
                EditorApplication.isPlaying = true;
            }
            catch (Exception salbuespena)
            {
                Debug.LogError($"{BdMarka}_ERROR {salbuespena}");
                EditorApplication.Exit(1);
            }
        }

        private static void BdLog(string mezua, string pila, LogType mota)
        {
            if ((mota == LogType.Error || mota == LogType.Exception) && !mezua.StartsWith(BdMarka, StringComparison.Ordinal) && BdErroreak.Count < 20)
            {
                BdErroreak.Add($"{mota}: {mezua.Split('\n')[0]}");
            }
        }

        private static void BdEguneratu()
        {
            DemoProbaKontrola kontrola = UnityEngine.Object.FindAnyObjectByType<DemoProbaKontrola>();
            if (EditorApplication.timeSinceStartup - _bdHasiera > 300.0)
            {
                var l = kontrola != null ? kontrola.Lerroak : new List<string>();
                l.Add("HUTS: denbora agortuta");
                BdAmaitu(l);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (kontrola == null)
            {
                if (UnityEngine.Object.FindAnyObjectByType<JokalariArma>() != null)
                {
                    kontrola = new GameObject("DemoProbaKontrola").AddComponent<DemoProbaKontrola>();
                    kontrola.Hasi(_bdMaila);
                }

                return;
            }

            if (kontrola.Amaituta)
            {
                BdAmaitu(kontrola.Lerroak);
            }
        }

        private static void BdAmaitu(List<string> lerroak)
        {
            EditorApplication.update -= BdEguneratu;
            Application.logMessageReceived -= BdLog;
            lerroak.Add(BdErroreak.Count == 0 ? "ONDO: Play-n 0 errore log-ean" : $"HUTS: {BdErroreak.Count} errore: {string.Join(" | ", BdErroreak)}");
            var informea = new Informea("BorrokaDemoProba", BdMarka, $"{_bdMaila.Izena}: misio-demoa (Play, eszena gorde gabe)", _bdMaila.Bidea, _bdMaila.Erroa);
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

        private sealed class DemoProbaKontrola : MonoBehaviour
        {
            public readonly List<string> Lerroak = new List<string>();
            public bool Amaituta;

            private void Oharra(string izena, bool ondo, string x = "") => Lerroak.Add($"{(ondo ? "ONDO" : "HUTS")}: {izena} {x}");

            public void Hasi(MailaDef maila)
            {
                DontDestroyOnLoad(gameObject);
                StartCoroutine(Exekutatu(maila));
            }

            private IEnumerator Exekutatu(MailaDef maila)
            {
                yield return new WaitForSeconds(1f);
                EsploratzaileKontrola kontrola = FindAnyObjectByType<EsploratzaileKontrola>();
                LevelManager km = FindAnyObjectByType<LevelManager>();
                Etsaia[] etsaiak = FindObjectsByType<Etsaia>(FindObjectsInactive.Exclude);
                int markak = FindObjectsByType<MailaMarka>(FindObjectsInactive.Exclude).Count(m => m.Mota == MarkaMota.Etsaia);
                int? espero = BorrokaDemoa.EtsaiKopurua(maila.Izena);
                int esperoa = espero ?? markak;

                Oharra("LevelManager, HUD eta jokalari bakarra", km != null && km.HudAktibo && FindAnyObjectByType<HudKudeatzailea>() != null
                    && FindObjectsByType<CharacterController>(FindObjectsInactive.Include).Length == 1);
                Oharra("Jokalariak EMP pistola du", kontrola.GetComponent<JokalariArma>() != null && kontrola.GetComponent<JokalariArma>().Pistola != null);
                Oharra("NavMesh-a kargatuta (spawn-ean)", NavMesh.SamplePosition(kontrola.transform.position, out NavMeshHit _, 1f, NavMesh.AllAreas));
                Oharra($"Etsai-kopurua narratiboaren arabera ({esperoa})", etsaiak.Length == esperoa, $"{etsaiak.Length} etsai, {markak} markatzaile");
                Oharra("Etsai guztiak NavMesh-ean", etsaiak.All(e => e.GetComponent<NavMeshAgent>().isOnNavMesh));

                if (etsaiak.Length > 0)
                {
                    // Hurbilena alertatu: misioaren geometrian zehar jokalariarengana doala egiaztatu.
                    Etsaia e = etsaiak.OrderBy(x => Vector3.Distance(x.transform.position, kontrola.transform.position)).First();
                    float hasiera = Vector3.Distance(e.transform.position, kontrola.transform.position);
                    var bidea = new NavMeshPath();
                    bool osoa = e.GetComponent<NavMeshAgent>().CalculatePath(kontrola.transform.position, bidea) && bidea.status == NavMeshPathStatus.PathComplete;
                    Oharra($"{e.name}: jokalarirako bide osoa", osoa, $"{hasiera:0.0} m zuzenean");
                    float amaiera = Time.time + 70f;
                    float hurbilena = hasiera;
                    while (Time.time < amaiera && hurbilena > 3f && e.Bizirik)
                    {
                        e.Entzun(kontrola.transform.position);
                        hurbilena = Mathf.Min(hurbilena, Vector3.Distance(e.transform.position, kontrola.transform.position));
                        yield return null;
                    }

                    // Narratiba: soldaduak badaude (MisioEdukia), munstroa bidean hil dezakete; hori ere emaitza zuzena da.
                    Eclipse.Npc.NpcKontrola[] soldaduak = FindObjectsByType<Eclipse.Npc.NpcKontrola>(FindObjectsInactive.Exclude).Where(n => n.Rola == Eclipse.Npc.NpcRola.Soldadua).ToArray();
                    int soldaduTiroak = soldaduak.Sum(s => s.Jotakoak);
                    bool soldaduekHila = !e.Bizirik && soldaduTiroak > 0;
                    Oharra($"{e.name}: misioan zehar jokalariarengana iristen da (< 3 m) edo soldaduek hiltzen dute", hurbilena <= 3f || soldaduekHila,
                        $"{hasiera:0.0} → {hurbilena:0.0} m; bizirik {e.Bizirik}; soldaduen kolpeak {soldaduTiroak}");
                }

                Amaituta = true;
            }
        }
    }
}
