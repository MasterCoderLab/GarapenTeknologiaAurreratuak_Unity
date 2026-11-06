using UnityEngine;

namespace Eclipse.Misioak
{
    /// <summary>Bigarren mailako helburua: bildu beharreko datu-nukleoa. Behin bakarrik biltzen da.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MisioBilduma : MonoBehaviour
    {
        [SerializeField] private LevelManager _kudeatzailea;

        private void Update()
        {
            transform.Rotate(0f, 60f * Time.deltaTime, 0f);
        }

        private void OnTriggerEnter(Collider beste)
        {
            if (beste is CharacterController && _kudeatzailea != null)
            {
                _kudeatzailea.BildumaLortu();
                gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public bool Konfiguratu(LevelManager kudeatzailea)
        {
            if (_kudeatzailea == kudeatzailea)
            {
                return false;
            }

            _kudeatzailea = kudeatzailea;
            return true;
        }
#endif
    }
}
