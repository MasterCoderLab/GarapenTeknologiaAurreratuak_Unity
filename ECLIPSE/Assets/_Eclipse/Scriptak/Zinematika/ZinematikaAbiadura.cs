using UnityEngine;

namespace Eclipse.Zinematika
{
    /// <summary>
    /// Zinematikan (Timeline-k gurasoa mugitzen du): pertsonaiaren Animator-eko "Abiadura" mundu-abiadura horizontaletik kalkulatu,
    /// oinez/korrika animazioa ibilbidearekin bat etor dadin. Root motion-ik gabe; Timeline-k ez du humanoidea zuzenean animatzen.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class ZinematikaAbiadura : MonoBehaviour
    {
        private static readonly int AbiaduraId = Animator.StringToHash("Abiadura");

        private Animator _animator;
        private Vector3 _aurrekoa;

        /// <summary>Azken fotogramako abiadura horizontala (m/s), probetarako.</summary>
        public float Abiadura { get; private set; }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            _aurrekoa = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 aldea = transform.position - _aurrekoa;
            aldea.y = 0f;
            _aurrekoa = transform.position;
            float berria = Time.deltaTime > 0f ? aldea.magnitude / Time.deltaTime : 0f;
            Abiadura = Mathf.Lerp(Abiadura, berria, 0.25f);
            _animator.SetFloat(AbiaduraId, Abiadura, 0.1f, Time.deltaTime);
        }
    }
}
