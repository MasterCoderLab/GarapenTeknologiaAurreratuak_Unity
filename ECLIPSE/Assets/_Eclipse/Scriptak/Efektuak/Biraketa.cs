using UnityEngine;

namespace Eclipse.Efektuak
{
    /// <summary>Elementu mugikorrak (erreaktorearen eraztunak, haizagailuak): ardatz baten inguruan biratzen dira.</summary>
    public sealed class Biraketa : MonoBehaviour
    {
        [SerializeField] private Vector3 _abiadura = new Vector3(0f, 30f, 0f);

        private void Update()
        {
            transform.Rotate(_abiadura * Time.deltaTime, Space.Self);
        }

#if UNITY_EDITOR
        public bool Konfiguratu(Vector3 abiadura)
        {
            if (_abiadura == abiadura)
            {
                return false;
            }

            _abiadura = abiadura;
            return true;
        }
#endif
    }
}
