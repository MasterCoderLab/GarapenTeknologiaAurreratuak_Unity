#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.Proba
{
    /// <summary>Esploratzailea 02_PrototipoDiseinua eszenan, Hasiera salaren erdian eta ekialdeko atera begira.</summary>
    public static class PrototipoAbiarazlea
    {
        private const string Marka = "ECL_ESPLORATZAILEA";
        private const string Eszena = "02_PrototipoDiseinua";
        private const string Hasiera = "Prototipoa/Gela_P_Hasiera";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Abiarazi()
        {
            if (SceneManager.GetActiveScene().name != Eszena)
            {
                return;
            }

            GameObject gela = GameObject.Find(Hasiera);
            if (gela == null)
            {
                Debug.LogError($"{Marka}_ERROR Ez da aurkitu {Hasiera}.");
                return;
            }

            Physics.SyncTransforms();
            if (!EsploratzaileKontrola.PosizioaLibre(gela.transform.position, out Vector3 posizioa))
            {
                Debug.LogError($"{Marka}_ERROR {posizioa} ez dago libre.");
                return;
            }

            EsploratzaileKontrola.Sortu(posizioa, Quaternion.LookRotation(Vector3.right));
            Debug.Log($"{Marka}_OK Esploratzailea prototipoan: {posizioa}.");
        }
    }
}
#endif
