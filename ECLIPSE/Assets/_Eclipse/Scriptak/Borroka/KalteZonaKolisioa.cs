using UnityEngine;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Kalte-zona bat (hitbox): trigger kolisioa hezur baten gainean. Tiroak zona hau aurkitzen du eta jabeari pasatzen dio.
    /// Desmenbratzeko zatiak zona hauen gainean eraikiko dira.
    /// </summary>
    public sealed class KalteZonaKolisioa : MonoBehaviour
    {
        [SerializeField] private KalteZona _zona;
        [SerializeField] private MonoBehaviour _jabea;

        public KalteZona Zona => _zona;

        public IKalteHartzailea Jabea => _jabea as IKalteHartzailea ?? GetComponentInParent<IKalteHartzailea>();

        public void Konfiguratu(KalteZona zona, MonoBehaviour jabea)
        {
            _zona = zona;
            _jabea = jabea;
        }
    }
}
