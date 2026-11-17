using UnityEngine;

namespace Eclipse.Borroka
{
    /// <summary>
    /// EMP pistolaren alde bisuala eta soinua (aurrefabrikatuan, BorrokaTresna-k sortua): mutura, izpia, distira, txinpartak eta audioa.
    /// Logikarik ez: JokalariArma-k esaten dio zer erakutsi.
    /// </summary>
    public sealed class EmpPistolaBisuala : MonoBehaviour
    {
        [SerializeField] private Transform _mutura;
        [SerializeField] private LineRenderer _izpia;
        [SerializeField] private Light _distira;
        [SerializeField] private ParticleSystem _txinpartak;
        [SerializeField] private AudioSource _audioa;
        [SerializeField] private AudioClip _tiroa;
        [SerializeField] private AudioClip _hutsik;
        [SerializeField] private AudioClip _birkarga;
        [SerializeField] private AudioClip _inpaktua;

        private const float IzpiDenbora = 0.06f;
        private float _izpiAmaiera;

        public Transform Mutura => _mutura != null ? _mutura : transform;

        public void Konfiguratu(Transform mutura, LineRenderer izpia, Light distira, ParticleSystem txinpartak, AudioSource audioa,
            AudioClip tiroa, AudioClip hutsik, AudioClip birkarga, AudioClip inpaktua)
        {
            _mutura = mutura;
            _izpia = izpia;
            _distira = distira;
            _txinpartak = txinpartak;
            _audioa = audioa;
            _tiroa = tiroa;
            _hutsik = hutsik;
            _birkarga = birkarga;
            _inpaktua = inpaktua;
        }

        /// <summary>Tiroa: izpia muturretik inpaktura, distira eta, gainazal gogorrean, txinpartak.</summary>
        public void Tiroa(Vector3 inpaktua, Vector3 normala, bool haragia)
        {
            if (_izpia != null)
            {
                _izpia.SetPosition(0, Mutura.position);
                _izpia.SetPosition(1, inpaktua);
                _izpia.enabled = true;
            }

            if (_distira != null)
            {
                _distira.enabled = true;
            }

            _izpiAmaiera = Time.time + IzpiDenbora;
            if (!haragia && _txinpartak != null)
            {
                _txinpartak.transform.SetPositionAndRotation(inpaktua, Quaternion.LookRotation(normala.sqrMagnitude > 0f ? normala : Vector3.up));
                _txinpartak.Emit(18);
            }

            Jo(_tiroa);
            if (!haragia && _inpaktua != null)
            {
                AudioSource.PlayClipAtPoint(_inpaktua, inpaktua, 0.6f);
            }
        }

        public void Hutsik() => Jo(_hutsik);

        public void Birkarga() => Jo(_birkarga);

        private void Jo(AudioClip klipa)
        {
            if (_audioa != null && klipa != null)
            {
                _audioa.PlayOneShot(klipa);
            }
        }

        private void LateUpdate()
        {
            if (_izpiAmaiera > 0f && Time.time >= _izpiAmaiera)
            {
                _izpiAmaiera = 0f;
                if (_izpia != null)
                {
                    _izpia.enabled = false;
                }

                if (_distira != null)
                {
                    _distira.enabled = false;
                }
            }
        }
    }
}
