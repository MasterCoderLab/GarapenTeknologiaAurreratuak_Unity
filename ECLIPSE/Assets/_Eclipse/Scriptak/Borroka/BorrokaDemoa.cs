using System.Collections.Generic;
using System.Linq;
using Eclipse.Misioak;
using Eclipse.Proba;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Misioetako borroka-demoa (A3): EDITOREAN BAKARRIK eta Ion-ek aktibatzen duenean (ECLIPSE/Borroka/Misio-demoa) edo
    /// -eclipseBorrokaDemoa argumentuarekin (probak). Eszenak ez dira aldatzen: dena runtime-n gehitzen da.
    /// - Jokalariari EMP pistola eta bizitza.
    /// - NavMesh-a Resources/Nabigazioa/NM_&lt;eszena&gt;-tik (NabigazioTresna-k sortua).
    /// - Etsaiak narratiboaren arabera: M1 bat ere ez (zientzialariak), M2 lehen biak (lehen munstroa), M3 markatzaile guztiak.
    /// Build-etan ez da ezer egiten.
    /// </summary>
    public sealed class BorrokaDemoa : MonoBehaviour
    {
        public const string EditorGakoa = "ECLIPSE_BorrokaDemoa";
        private const string Marka = "ECL_BORROKADEMOA";

        private string _eszena;
        private bool _eginda;

        public static bool Aktibo
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(EditorGakoa, false)
                       || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-eclipseBorrokaDemoa") >= 0;
#else
                return false;
#endif
            }
        }

        /// <summary>Misio bakoitzeko etsai-kopurua (null = markatzaile guztiak).</summary>
        public static int? EtsaiKopurua(string eszena) => eszena switch
        {
            "Mision01_Laborategia" => 0,
            "Mision02_Energia" => 2,
            _ => null,
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Erregistratu()
        {
            SceneManager.sceneLoaded -= Kargatuta;
            SceneManager.sceneLoaded += Kargatuta;
            Kargatuta(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Kargatuta(Scene eszena, LoadSceneMode modua)
        {
            if (!Aktibo || !eszena.name.StartsWith("Mision0", System.StringComparison.Ordinal) || FindAnyObjectByType<BorrokaDemoa>() != null)
            {
                return;
            }

            // Misioak eduki propioa badu (Resources/Misioak/ME_*), hark agintzen du: demoak ez du ezer bikoizten.
            if (!MisioEdukiKargatzailea.Desgaituta && MisioEdukia.Kargatu(eszena.name) != null)
            {
                Debug.Log($"{Marka}_OK {eszena.name}: MisioEdukia dago; demoa ez da behar.");
                return;
            }

            var o = new GameObject("BorrokaDemoa");
            SceneManager.MoveGameObjectToScene(o, eszena);
            o.AddComponent<BorrokaDemoa>()._eszena = eszena.name;
        }

        private void Update()
        {
            if (_eginda)
            {
                return;
            }

            EsploratzaileKontrola kontrola = FindAnyObjectByType<EsploratzaileKontrola>();
            if (kontrola == null)
            {
                return;
            }

            _eginda = true;
            if (FindAnyObjectByType<EtsaiSortzailea>() != null)
            {
                Debug.Log($"{Marka}_OK {_eszena}: MisioEdukia-k aplikatuta; demoak ez du ezer bikoizten.");
                return;
            }

            bool navMesh = NavMeshKargatu(_eszena);
            int sortuak = Aplikatu(gameObject, kontrola, EtsaiKopurua(_eszena), "Borroka/PF_Etsaia_Proba");
            Debug.Log($"{Marka}_OK {_eszena}: arma, NavMesh {(navMesh ? "bai" : "ez")}, {sortuak} etsai");
        }

        /// <summary>Misioaren NavMesh-a Resources/Nabigazioa/NM_&lt;eszena&gt;-tik gehitu (behin). False: ez dago.</summary>
        public static bool NavMeshKargatu(string eszena)
        {
            var datuak = Resources.Load<NavMeshData>($"Nabigazioa/NM_{eszena}");
            if (datuak == null)
            {
                Debug.LogWarning($"{Marka}_ERROR Ez dago Resources/Nabigazioa/NM_{eszena}: etsaiak eta NPC-ak ez dira mugituko (exekutatu NabigazioTresna).");
                return false;
            }

            NavMesh.AddNavMeshData(datuak);
            return true;
        }

        /// <summary>Jokalariari arma eta bizitza, eta etsaiak markatzaileetan (jokalaritik urrunenak lehenbizi). Etsai-kopurua itzultzen du.</summary>
        public static int Aplikatu(GameObject jabea, EsploratzaileKontrola kontrola, int? kopurua, string etsaiAurrefab)
        {
            if (kontrola.GetComponent<JokalariArma>() == null)
            {
                JokalariArma.Gehitu(kontrola, berpiztuAutomatikoki: true);
            }

            List<MailaMarka> markak = FindObjectsByType<MailaMarka>(FindObjectsInactive.Exclude)
                .Where(m => m.Mota == MarkaMota.Etsaia)
                .OrderBy(m => Vector3.Distance(m.transform.position, kontrola.transform.position))
                .ToList();
            if (kopurua.HasValue)
            {
                markak = markak.Skip(markak.Count - System.Math.Min(kopurua.Value, markak.Count)).ToList();
            }

            var sortzailea = jabea.AddComponent<EtsaiSortzailea>();
            sortzailea.Konfiguratu(etsaiAurrefab, false);
            return sortzailea.Sortu(markak);
        }
    }
}
