using UnityEngine;

namespace Eclipse.Inbentarioak
{
    /// <summary>GameSession-en jabea: eszenetatik kanpo bizi da, misioz misio iraun dezan. Ez dago eszenetan gordeta; behar denean sortzen da.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager _instantzia;

        public static GameSession Saioa
        {
            get
            {
                if (_instantzia == null)
                {
                    var objektua = new GameObject("ECLIPSE_GameManager");
                    DontDestroyOnLoad(objektua);
                    _instantzia = objektua.AddComponent<GameManager>();
                }

                return _instantzia.Oraingoa;
            }
        }

        private GameSession Oraingoa { get; } = new GameSession();

        /// <summary>Domain Reload desaktibatuta dago: Play bakoitzak saio garbia du.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Garbitu()
        {
            _instantzia = null;
        }
    }
}
