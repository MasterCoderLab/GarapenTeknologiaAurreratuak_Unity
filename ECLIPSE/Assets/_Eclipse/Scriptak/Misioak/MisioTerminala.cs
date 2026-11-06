using Eclipse.Jokoa;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Misioak
{
    /// <summary>Terminala edo kontrol-puntua: jokalaria trigger-aren barruan dagoela, E teklak helburua betetzen du.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MisioTerminala : MonoBehaviour
    {
        private static readonly Color Itxaroten = new Color(1f, 0.25f, 0.15f);
        private static readonly Color Aktibo = new Color(1f, 0.75f, 0.15f);
        private static readonly Color Eginda = new Color(0.2f, 1f, 0.4f);

        [SerializeField] private string _helburua;
        [SerializeField] private string _izena;
        [SerializeField] private LevelManager _kudeatzailea;
        [SerializeField] private Light _adierazlea;

        public string Helburua => _helburua;

        public bool JokalariaBarruan { get; private set; }

        private void OnEnable()
        {
            GameEvents.HelburuaBeteta += HelburuaAldatuta;
        }

        private void OnDisable()
        {
            GameEvents.HelburuaBeteta -= HelburuaAldatuta;
        }

        private void Start()
        {
            AdierazleaEguneratu();
        }

        private void Update()
        {
            Keyboard teklatua = Keyboard.current;
            if (JokalariaBarruan && teklatua != null && teklatua.eKey.wasPressedThisFrame)
            {
                Erabili();
            }
        }

        public BeteEmaitza Erabili()
        {
            return _kudeatzailea != null ? _kudeatzailea.HelburuaBete(_helburua) : BeteEmaitza.Ezezaguna;
        }

        private void OnTriggerEnter(Collider beste)
        {
            if (beste is CharacterController)
            {
                JokalariaBarruan = true;
            }
        }

        private void OnTriggerExit(Collider beste)
        {
            if (beste is CharacterController)
            {
                JokalariaBarruan = false;
            }
        }

        private void HelburuaAldatuta(string id)
        {
            AdierazleaEguneratu();
        }

        private void AdierazleaEguneratu()
        {
            if (_adierazlea == null || _kudeatzailea == null || _kudeatzailea.Logika == null)
            {
                return;
            }

            MisioLogika logika = _kudeatzailea.Logika;
            _adierazlea.color = logika.BetetaDago(_helburua) ? Eginda : logika.Aktibo(_helburua) ? Aktibo : Itxaroten;
        }

        private void OnGUI()
        {
            if (!JokalariaBarruan || _kudeatzailea == null || _kudeatzailea.Logika == null || _kudeatzailea.Logika.BetetaDago(_helburua))
            {
                return;
            }

            GUI.Label(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.65f, 400f, 24f), $"[E] {_izena}");
        }

#if UNITY_EDITOR
        public bool Konfiguratu(string helburua, string izena, LevelManager kudeatzailea, Light adierazlea)
        {
            if (_helburua == helburua && _izena == izena && _kudeatzailea == kudeatzailea && _adierazlea == adierazlea)
            {
                return false;
            }

            _helburua = helburua;
            _izena = izena;
            _kudeatzailea = kudeatzailea;
            _adierazlea = adierazlea;
            return true;
        }
#endif
    }
}
