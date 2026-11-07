#if ECLIPSE_AUTOPROBA
using System;
using System.Collections;
using System.Linq;
using Eclipse.Misioak;
using UnityEngine;

namespace Eclipse.Proba
{
    /// <summary>
    /// QA build-eko proba automatikoa: misioak zuzenean osatzen ditu, oinez ibili gabe, jokalaria eta eszena-aldaketak egiaztatzeko.
    /// Fitxategi osoa ECLIPSE_AUTOPROBA ikurraren barruan dago: ikur hori BatchReleaseProba-ren QA build-ak bakarrik ematen du,
    /// eta ez da ez Editorean ez azken Release-an konpilatzen. QA build-ean ere -eclipseAutoProba argumentuarekin bakarrik aktibatzen da.
    /// </summary>
    public sealed class AutoProbaQA : MonoBehaviour
    {
        private const string Marka = "ECL_AUTOPROBA";
        private const string Argumentua = "-eclipseAutoProba";
        private const string KontrolArgumentua = "-eclipseAutoProbaKontrola";
        private const string AzkenEszena = "Mision03_Erreaktorea";
        private const float EszenaDenbora = 60f;
        private const int TxandaGehienez = 50;

        private static AutoProbaQA _instantzia;

        private bool _kontrola;

        /// <summary>Domain Reload desaktibatuta dago: egoera estatikoa Play bakoitzean garbitu.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Garbitu()
        {
            _instantzia = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Abiarazi()
        {
            string[] argumentuak = Environment.GetCommandLineArgs();
            if (_instantzia != null || !argumentuak.Contains(Argumentua))
            {
                return;
            }

            var objektua = new GameObject("ECLIPSE_AutoProbaQA");
            DontDestroyOnLoad(objektua);
            _instantzia = objektua.AddComponent<AutoProbaQA>();
            _instantzia._kontrola = argumentuak.Contains(KontrolArgumentua);
            _instantzia.StartCoroutine(_instantzia.Exekutatu());
        }

        private IEnumerator Exekutatu()
        {
            Debug.Log($"{Marka}_HASI debug={Debug.isDebugBuild} kontrola={_kontrola}");
            LevelManager aurrekoa = null;
            while (true)
            {
                // Laginketaz: ez da suposatzen Start edo eszenaren karga noiz gertatzen den.
                LevelManager kudeatzailea = null;
                float muga = Time.realtimeSinceStartup + EszenaDenbora;
                while (kudeatzailea == null)
                {
                    LevelManager aurkitua = FindAnyObjectByType<LevelManager>();
                    if (aurkitua != null && aurkitua != aurrekoa && aurkitua.Logika != null && aurkitua.HudAktibo)
                    {
                        kudeatzailea = aurkitua;
                    }
                    else if (Time.realtimeSinceStartup > muga)
                    {
                        Hutsa($"{EszenaDenbora} s igaro dira LevelManager prest egon gabe.");
                        yield break;
                    }
                    else
                    {
                        yield return null;
                    }
                }

                string eszena = kudeatzailea.gameObject.scene.name;
                Debug.Log($"{Marka}_ESZENA {eszena}");

                // Kontrol negatiboa: jokalaria kenduta, egiaztapenak huts egin behar du.
                if (_kontrola)
                {
                    CharacterController kentzekoa = FindAnyObjectByType<CharacterController>();
                    if (kentzekoa != null)
                    {
                        Destroy(kentzekoa.gameObject);
                    }

                    yield return null;
                }

                if (!JokalariaDago(eszena))
                {
                    Hutsa($"{eszena}: ez dago jokalaririk.");
                    yield break;
                }

                int txandak = 0;
                while (!kudeatzailea.Logika.Amaituta)
                {
                    if (++txandak > TxandaGehienez)
                    {
                        Hutsa($"{eszena}: {TxandaGehienez} txandatan ez da misioa amaitu.");
                        yield break;
                    }

                    foreach (MisioTerminala terminala in FindObjectsByType<MisioTerminala>(FindObjectsInactive.Exclude))
                    {
                        if (kudeatzailea.Logika.Aktibo(terminala.Helburua))
                        {
                            kudeatzailea.HelburuaBete(terminala.Helburua);
                        }
                    }

                    yield return null;
                }

                kudeatzailea.IrteeraLortu();
                if (!kudeatzailea.MisioaAmaituta)
                {
                    Hutsa($"{eszena}: irteerak ez du misioa amaitu.");
                    yield break;
                }

                if (eszena == AzkenEszena)
                {
                    Debug.Log($"{Marka}_AMAITUTA {eszena}");
                    Application.Quit(0);
                    yield break;
                }

                aurrekoa = kudeatzailea;
                yield return null;
            }
        }

        private static bool JokalariaDago(string eszena)
        {
            CharacterController jokalaria = FindAnyObjectByType<CharacterController>();
            bool kontrolagailua = jokalaria != null && jokalaria.enabled && jokalaria.gameObject.activeInHierarchy;
            bool esploratzailea = kontrolagailua && jokalaria.GetComponent<EsploratzaileKontrola>() != null;
            Debug.Log($"{Marka}_JOKALARIA {eszena} cc={kontrolagailua} kontrola={esploratzailea}");
            return kontrolagailua && esploratzailea;
        }

        private static void Hutsa(string arrazoia)
        {
            Debug.LogError($"{Marka}_ERROR {arrazoia}");
            Application.Quit(1);
        }
    }
}
#endif
