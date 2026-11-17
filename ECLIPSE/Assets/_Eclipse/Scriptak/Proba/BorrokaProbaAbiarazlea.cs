using Eclipse.Borroka;
using UnityEngine;

namespace Eclipse.Proba
{
    /// <summary>
    /// 04_BorrokaProba eszenarako: misioetako jokalari bera (EsploratzaileKontrola + HypJo) sortu eta EMP pistola eta bizitza gehitu.
    /// Etsaiak EtsaiSortzailea-k sortzen ditu markatzaileetan. Misioetan ez da erabiltzen.
    /// </summary>
    public sealed class BorrokaProbaAbiarazlea : MonoBehaviour
    {
        private const string Marka = "ECL_BORROKAPROBA";

        [SerializeField] private Transform _spawn;

        private void Start()
        {
            if (FindAnyObjectByType<CharacterController>() == null)
            {
                Physics.SyncTransforms();
                if (_spawn == null || !EsploratzaileKontrola.PosizioaLibre(_spawn.position, out Vector3 posizioa))
                {
                    Debug.LogError($"{Marka}_ERROR Spawn puntua falta da edo ez dago libre.");
                    return;
                }

                EsploratzaileKontrola.Sortu(posizioa, _spawn.rotation);
            }

            EsploratzaileKontrola kontrola = FindAnyObjectByType<EsploratzaileKontrola>();
            if (kontrola.GetComponent<JokalariArma>() == null)
            {
                JokalariArma.Gehitu(kontrola, berpiztuAutomatikoki: true);
            }

            Debug.Log($"{Marka}_OK Jokalaria armarekin: {kontrola.transform.position}");
        }
    }
}
