using UnityEngine;

namespace Eclipse.Misioak
{
    /// <summary>Hurrengo mailarako irteera. Misioa amaitu arte ez du ezer egiten.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MisioIrteera : MonoBehaviour
    {
        [SerializeField] private LevelManager _kudeatzailea;

        private void OnTriggerStay(Collider beste)
        {
            if (beste is CharacterController && _kudeatzailea != null)
            {
                _kudeatzailea.IrteeraLortu();
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
