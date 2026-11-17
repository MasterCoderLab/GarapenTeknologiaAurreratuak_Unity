using System.Collections.Generic;
using Eclipse.Atrezzo;
using Eclipse.Borroka;
using Eclipse.Jokoa;
using Eclipse.Npc;
using Eclipse.Proba;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Eclipse.Misioak
{
    /// <summary>
    /// Misioaren edukia runtime-n aplikatzen du (A4), Editorean zein build-etan, Resources/Misioak/ME_&lt;eszena&gt; badago:
    /// atrezzoa, borroka (arma, NavMesh, etsaiak), NPC-ak eta, misioa amaitzean hurrengorik ez badago, amaierako eszena.
    /// Dena "MisioEdukia" erro baten azpian, eszenan ezer gorde gabe. Editoreko demo-banderak (ECLIPSE/Borroka eta Atrezzo/Demoa)
    /// eduki hau gabe ere lanean jarraitzen dute; biak batera, honek lehentasuna du eta demoek ez dute ezer bikoizten.
    /// -eclipseEdukirikGabe argumentuarekin ez da ezer aplikatzen (erregresioak eduki gabe).
    /// </summary>
    public sealed class MisioEdukiKargatzailea : MonoBehaviour
    {
        private const string Marka = "ECL_MISIOEDUKIA";
        public const string ErroIzena = "MisioEdukia";

        private MisioEdukia _edukia;
        private bool _aplikatuta;
        private bool _amaieraAbiatuta;

        public MisioEdukia Edukia => _edukia;

        public bool Aplikatuta => _aplikatuta;

        public int Etsaiak { get; private set; }

        public int Npcak { get; private set; }

        public static bool Desgaituta => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-eclipseEdukirikGabe") >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Erregistratu()
        {
            SceneManager.sceneLoaded -= Kargatuta;
            SceneManager.sceneLoaded += Kargatuta;
            Kargatuta(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Kargatuta(Scene eszena, LoadSceneMode modua)
        {
            if (Desgaituta || !eszena.name.StartsWith("Mision0", System.StringComparison.Ordinal))
            {
                return;
            }

            foreach (GameObject erroa in eszena.GetRootGameObjects())
            {
                if (erroa.name == ErroIzena)
                {
                    return;
                }
            }

            MisioEdukia edukia = MisioEdukia.Kargatu(eszena.name);
            if (edukia == null)
            {
                return;
            }

            var o = new GameObject(ErroIzena);
            SceneManager.MoveGameObjectToScene(o, eszena);
            o.AddComponent<MisioEdukiKargatzailea>()._edukia = edukia;
        }

        private void OnEnable()
        {
            GameEvents.SektoreaAmaitu += SektoreaAmaituta;
        }

        private void OnDisable()
        {
            GameEvents.SektoreaAmaitu -= SektoreaAmaituta;
        }

        /// <summary>Jokalaria sortu arte itxaron (LevelManager-ek Start-en sortzen du); gero dena behin aplikatu.</summary>
        private void Update()
        {
            if (_aplikatuta)
            {
                return;
            }

            EsploratzaileKontrola kontrola = FindAnyObjectByType<EsploratzaileKontrola>();
            if (kontrola == null)
            {
                return;
            }

            _aplikatuta = true;
            Scene eszena = gameObject.scene;
            bool navMesh = false;
            if (_edukia.BorrokaAktibo || _edukia.Npcak.Count > 0)
            {
                navMesh = BorrokaDemoa.NavMeshKargatu(eszena.name);
            }

            if (_edukia.AtrezzoAktibo)
            {
                AtrezzoKargatzailea.Jarri(eszena);
            }

            if (_edukia.BorrokaAktibo)
            {
                Etsaiak = BorrokaDemoa.Aplikatu(gameObject, kontrola, _edukia.EtsaiKopurua < 0 ? (int?)null : _edukia.EtsaiKopurua, _edukia.EtsaiAurrefab);
            }

            Npcak = NpcakSortu();
            Debug.Log($"{Marka}_OK {eszena.name}: atrezzoa {_edukia.AtrezzoAktibo}, borroka {_edukia.BorrokaAktibo} ({Etsaiak} etsai), {Npcak} NPC, NavMesh {navMesh}, amaiera '{_edukia.AmaieraEszena}'");
        }

        private int NpcakSortu()
        {
            int sortuak = 0;
            var aurrefabak = new Dictionary<NpcRola, GameObject>();
            foreach (MisioEdukia.NpcKokapena k in _edukia.Npcak)
            {
                if (!aurrefabak.TryGetValue(k.Rola, out GameObject aurrefab))
                {
                    aurrefab = Resources.Load<GameObject>($"Npc/PF_Npc_{k.Rola}");
                    aurrefabak[k.Rola] = aurrefab;
                }

                if (aurrefab == null)
                {
                    Debug.LogWarning($"{Marka}_ERROR Npc/PF_Npc_{k.Rola} falta da (NpcTresna aplikatu).");
                    continue;
                }

                Vector3 lekua = k.Lekua;
                if (NavMesh.SamplePosition(lekua, out NavMeshHit jo, 1.5f, NavMesh.AllAreas))
                {
                    lekua = jo.position;
                }

                var puntuak = new GameObject($"Puntuak_{k.Rola}_{sortuak + 1:00}").transform;
                puntuak.SetParent(transform, false);
                Transform lanLekua = null;
                if (k.LanLekuaDu)
                {
                    lanLekua = Puntua(puntuak, "LanLekua", k.LanLekua, k.LanYaw);
                }

                var patruila = new Transform[k.Patruila.Length];
                for (int i = 0; i < patruila.Length; i++)
                {
                    patruila[i] = Puntua(puntuak, $"Patruila_{i + 1}", k.Patruila[i], 0f);
                }

                GameObject npc = Instantiate(aurrefab, lekua, Quaternion.Euler(0f, k.Yaw, 0f), transform);
                npc.name = $"Npc_{k.Rola}_{++sortuak:00}";
                npc.GetComponent<NpcKontrola>().KokapenaEzarri(lanLekua, patruila);
            }

            return sortuak;
        }

        private static Transform Puntua(Transform gurasoa, string izena, Vector3 lekua, float yaw)
        {
            var t = new GameObject(izena).transform;
            t.SetParent(gurasoa, false);
            t.SetPositionAndRotation(lekua, Quaternion.Euler(0f, yaw, 0f));
            return t;
        }

        /// <summary>Misioa amaituta eta hurrengo misiorik gabe (M3): amaierako zinematika kargatu.</summary>
        private void SektoreaAmaituta(string sektorea)
        {
            if (_amaieraAbiatuta || string.IsNullOrEmpty(_edukia.AmaieraEszena))
            {
                return;
            }

            LevelManager km = FindAnyObjectByType<LevelManager>();
            if (km != null && !string.IsNullOrEmpty(km.HurrengoEszena))
            {
                return;
            }

            _amaieraAbiatuta = true;
            Debug.Log($"{Marka}_AMAIERA {_edukia.AmaieraEszena}");
            Invoke(nameof(AmaieraKargatu), 1.5f);
        }

        private void AmaieraKargatu()
        {
            string eszena = _edukia.AmaieraEszena;
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneAsyncInPlayMode($"Assets/_Eclipse/Eszenak/Proba/{eszena}.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            if (Application.CanStreamedLevelBeLoaded(eszena))
            {
                SceneManager.LoadScene(eszena);
            }
            else
            {
                Debug.LogError($"{Marka}_ERROR {eszena} ez dago Build Profile-an: amaierako zinematika ezin da kargatu.");
            }
#endif
        }
    }
}
