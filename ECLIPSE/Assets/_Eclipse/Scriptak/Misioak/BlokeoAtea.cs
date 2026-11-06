using Eclipse.Jokoa;
using UnityEngine;

namespace Eclipse.Misioak
{
    /// <summary>Energia-hesia: misioa fase jakin batera iristean irekitzen da. Ez dago beste sektoreekiko erreferentziarik.</summary>
    public sealed class BlokeoAtea : MonoBehaviour
    {
        [SerializeField] private int _fasea;
        [SerializeField] private LevelManager _kudeatzailea;

        public int Fasea => _fasea;

        private void OnEnable()
        {
            GameEvents.HelburuaBeteta += HelburuaAldatuta;
        }

        private void OnDisable()
        {
            GameEvents.HelburuaBeteta -= HelburuaAldatuta;
        }

        private void HelburuaAldatuta(string id)
        {
            if (_kudeatzailea != null && _kudeatzailea.Logika != null && _kudeatzailea.Logika.UnekoFasea >= _fasea)
            {
                Debug.Log($"ECL_MISIOA_ATEA_IREKITA {name}");
                gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public bool Konfiguratu(int fasea, LevelManager kudeatzailea)
        {
            if (_fasea == fasea && _kudeatzailea == kudeatzailea)
            {
                return false;
            }

            _fasea = fasea;
            _kudeatzailea = kudeatzailea;
            return true;
        }
#endif
    }
}
