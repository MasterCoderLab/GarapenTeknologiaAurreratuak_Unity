using UnityEngine;

namespace Eclipse.Efektuak
{
    /// <summary>Larrialdi-argia: intentsitatea aldian-aldian igotzen eta jaisten du. Itzalik gabea.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class ArgiPultsua : MonoBehaviour
    {
        [SerializeField] private float _maiztasuna = 0.7f;
        [SerializeField] private float _gutxienekoa = 0.25f;

        private Light _argia;
        private float _oinarria;

        private void Awake()
        {
            _argia = GetComponent<Light>();
            _oinarria = _argia.intensity;
        }

        private void Update()
        {
            float pultsua = Mathf.PingPong(Time.time * _maiztasuna * 2f, 1f);
            _argia.intensity = _oinarria * Mathf.Lerp(_gutxienekoa, 1f, pultsua * pultsua);
        }
    }
}
