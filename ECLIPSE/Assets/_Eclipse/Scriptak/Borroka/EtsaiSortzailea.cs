using System.Collections.Generic;
using Eclipse.Misioak;
using UnityEngine;
using UnityEngine.AI;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Etsaiak runtime-n sortzen ditu bere erroaren azpiko MailaMarka (Etsaia) markatzaileetan. Eszenetan ez da etsairik gordetzen.
    /// Misioetan (Ion-ek onartu ondoren) sektore bakoitzeko erroan jarriko da: sektoreak autonomoak izaten jarraitzen dute.
    /// </summary>
    public sealed class EtsaiSortzailea : MonoBehaviour
    {
        private const string Marka = "ECL_BORROKA";

        [SerializeField] private string _aurrefabrikatua = "Borroka/PF_Etsaia_Proba";
        [SerializeField] private bool _hasieranSortu = true;

        private readonly List<Etsaia> _sortuak = new List<Etsaia>();

        public IReadOnlyList<Etsaia> Sortuak => _sortuak;

        public void Konfiguratu(string aurrefabrikatua, bool hasieranSortu)
        {
            _aurrefabrikatua = aurrefabrikatua;
            _hasieranSortu = hasieranSortu;
        }

        private void Start()
        {
            if (_hasieranSortu)
            {
                SortuGuztiak();
            }
        }

        /// <summary>Erregresio-probetarako: -eclipseEtsairikGabe argumentuarekin ez da etsairik sortzen (robota, HUD-a…).</summary>
        public static bool Desgaituta => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-eclipseEtsairikGabe") >= 0;

        public int SortuGuztiak()
        {
            var markak = new List<MailaMarka>();
            foreach (MailaMarka marka in GetComponentsInChildren<MailaMarka>())
            {
                if (marka.Mota == MarkaMota.Etsaia)
                {
                    markak.Add(marka);
                }
            }

            return Sortu(markak);
        }

        /// <summary>Markatzaile jakin batzuetan sortu (misio-demoa: sektorearen markatzaileak, misioaren arabera).</summary>
        public int Sortu(IReadOnlyList<MailaMarka> markak)
        {
            if (Desgaituta)
            {
                Debug.Log($"{Marka}_ETSAIAK 0 (-eclipseEtsairikGabe)");
                return 0;
            }

            GameObject aurrefab = Resources.Load<GameObject>(_aurrefabrikatua);
            if (aurrefab == null)
            {
                Debug.LogError($"{Marka}_ERROR Ez dago Resources/{_aurrefabrikatua}");
                return 0;
            }

            int zenbakia = _sortuak.Count;
            foreach (MailaMarka marka in markak)
            {
                Vector3 lekua = marka.transform.position;
                if (NavMesh.SamplePosition(lekua, out NavMeshHit jo, 1.5f, NavMesh.AllAreas))
                {
                    lekua = jo.position;
                }
                else
                {
                    Debug.LogWarning($"{Marka}_OHARRA {marka.name}: ez dago NavMesh-ik inguruan; etsaia ez da mugituko.");
                }

                GameObject etsaia = Instantiate(aurrefab, lekua, marka.transform.rotation);
                etsaia.name = $"{aurrefab.name}_{++zenbakia:00}";
                _sortuak.Add(etsaia.GetComponent<Etsaia>());
            }

            Debug.Log($"{Marka}_ETSAIAK {_sortuak.Count} sortuta ({_aurrefabrikatua})");
            return _sortuak.Count;
        }
    }
}
