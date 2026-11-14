using UnityEngine;

namespace Eclipse.Proba
{
    /// <summary>
    /// 03_JokalariProba eszenarako: misioetako jokalari bera (EsploratzaileKontrola + HypJo) sortzen du Spawn puntuan.
    /// Misiorik, HUD-ik eta mapa-rik gabe: animazioak, kamera eta oinak probatzeko bakarrik.
    /// </summary>
    public sealed class JokalariProbaAbiarazlea : MonoBehaviour
    {
        private const string Marka = "ECL_JOKALARIPROBA";

        [SerializeField] private Transform _spawn;

        private void Start()
        {
            if (FindAnyObjectByType<CharacterController>() != null)
            {
                Debug.Log($"{Marka}_OK Jokalaria badago: ez da beste bat sortu.");
                return;
            }

            if (_spawn == null)
            {
                Debug.LogError($"{Marka}_ERROR Spawn puntua falta da.");
                return;
            }

            Physics.SyncTransforms();
            if (!EsploratzaileKontrola.PosizioaLibre(_spawn.position, out Vector3 posizioa))
            {
                Debug.LogError($"{Marka}_ERROR {_spawn.position} ez dago libre.");
                return;
            }

            EsploratzaileKontrola.Sortu(posizioa, _spawn.rotation);
            Debug.Log($"{Marka}_OK Jokalaria sortuta: {posizioa}.");
        }
    }
}
