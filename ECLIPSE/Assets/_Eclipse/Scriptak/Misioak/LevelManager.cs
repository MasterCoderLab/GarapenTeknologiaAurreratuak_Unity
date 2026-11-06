using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Jokoa;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Eclipse.Misioak
{
    /// <summary>
    /// Sektore bakoitzeko bat: misioaren hasiera, helburuak, blokeoak eta irteera. Jokalaria Spawn_Jokalaria puntuan sortzen du.
    /// Oraingoz esploratzailea erabiltzen du eta HUD behin-behinekoa marrazten du.
    /// </summary>
    public sealed class LevelManager : MonoBehaviour
    {
        [Serializable]
        public sealed class FaseDatuak
        {
            public string Deskribapena;
            public string[] Helburuak;
        }

        [SerializeField] private string _misioIzena;
        [SerializeField] private string _sektorea;
        [SerializeField] private string _hurrengoEszena;
        [SerializeField] private Transform _spawn;
        [SerializeField] private List<FaseDatuak> _faseak = new List<FaseDatuak>();

        private MisioLogika _logika;
        private int _bildumak;
        private int _bildumakGuztira;
        private string _oharra;
        private float _oharraAmaiera;
        private bool _amaierakoa;

        public string MisioIzena => _misioIzena;

        public MisioLogika Logika => _logika;

        public bool MisioaAmaituta => _amaierakoa;

        private void Awake()
        {
            _logika = new MisioLogika(_faseak.Select(fasea => new MisioFasea(fasea.Deskribapena, fasea.Helburuak)));
            _bildumakGuztira = FindObjectsByType<MisioBilduma>(FindObjectsInactive.Exclude).Length;
        }

        private void Start()
        {
            JokalariaSortu();
            GameEvents.JakinaraziSektoreaHasi(_sektorea);
            Debug.Log($"ECL_MISIOA_HASI {_misioIzena}");
        }

        /// <summary>Terminal batek bere helburua betetzea eskatzen du.</summary>
        public BeteEmaitza HelburuaBete(string id)
        {
            BeteEmaitza emaitza = _logika.Bete(id);
            if (emaitza == BeteEmaitza.Onartua)
            {
                Debug.Log($"ECL_MISIOA_HELBURUA {id}");
                GameEvents.JakinaraziHelburuaBeteta(id);
                OharraErakutsi(_logika.Amaituta ? "Misioa beteta: joan irteerara" : _logika.UnekoDeskribapena);
            }
            else if (emaitza == BeteEmaitza.EzDagokio)
            {
                OharraErakutsi($"Oraindik ez. {_logika.UnekoDeskribapena}");
            }

            return emaitza;
        }

        public void BildumaLortu()
        {
            _bildumak++;
            OharraErakutsi($"Datu-nukleoa: {_bildumak} / {_bildumakGuztira}");
        }

        /// <summary>Jokalaria irteerara iritsi da. Misioa amaitu gabe badago, ez da ezer gertatzen.</summary>
        public void IrteeraLortu()
        {
            if (_amaierakoa)
            {
                return;
            }

            if (!_logika.Amaituta)
            {
                OharraErakutsi(_logika.UnekoDeskribapena);
                return;
            }

            _amaierakoa = true;
            Debug.Log($"ECL_MISIOA_OK {_misioIzena}");
            GameEvents.JakinaraziSektoreaAmaitu(_sektorea);
            HurrengoaKargatu();
        }

        private void JokalariaSortu()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (FindAnyObjectByType<CharacterController>() != null || _spawn == null)
            {
                return;
            }

            Physics.SyncTransforms();
            if (!Proba.EsploratzaileKontrola.PosizioaLibre(_spawn.position, out Vector3 posizioa))
            {
                Debug.LogError($"ECL_MISIOA_ERROR {_spawn.position} ez dago libre.");
                return;
            }

            Proba.EsploratzaileKontrola.Sortu(posizioa, _spawn.rotation);
#endif
        }

        private void HurrengoaKargatu()
        {
            if (string.IsNullOrEmpty(_hurrengoEszena))
            {
                OharraErakutsi("ECLIPSE: instalaziotik ihes egin duzu");
                return;
            }

#if UNITY_EDITOR
            // Editorean Build Settings-en egon gabe karga daiteke.
            EditorSceneManager.LoadSceneAsyncInPlayMode($"Assets/_Eclipse/Eszenak/{_hurrengoEszena}.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            if (Application.CanStreamedLevelBeLoaded(_hurrengoEszena))
            {
                SceneManager.LoadScene(_hurrengoEszena);
            }
            else
            {
                Debug.LogError($"ECL_MISIOA_ERROR {_hurrengoEszena} ez dago Build Profile-an.");
            }
#endif
        }

        private void OharraErakutsi(string testua)
        {
            _oharra = testua;
            _oharraAmaiera = Time.time + 4f;
        }

        private void OnGUI()
        {
            if (_logika == null)
            {
                return;
            }

            string helburua = _logika.Amaituta
                ? "Joan irteerara"
                : _logika.FasekoGuztiak > 1
                    ? $"{_logika.UnekoDeskribapena} ({_logika.FasekoBeteak}/{_logika.FasekoGuztiak})"
                    : _logika.UnekoDeskribapena;
            GUI.Label(new Rect(10f, 34f, 900f, 24f), $"{_misioIzena}  |  {helburua}");
            if (_bildumakGuztira > 0)
            {
                GUI.Label(new Rect(10f, 56f, 400f, 24f), $"Datu-nukleoak: {_bildumak} / {_bildumakGuztira}");
            }

            if (Time.time < _oharraAmaiera)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.75f, 600f, 24f), _oharra);
            }
        }

#if UNITY_EDITOR
        /// <summary>Editoreko tresnarako: datuak bateratu eta zerbait aldatu den esan.</summary>
        public bool Konfiguratu(string misioIzena, string sektorea, string hurrengoa, Transform spawn, IReadOnlyList<FaseDatuak> faseak)
        {
            // Unity-k null kate hutsa bezala serializatzen du: bestela aldaketa bat agertuko litzateke beti.
            hurrengoa ??= string.Empty;
            bool berdina = _misioIzena == misioIzena && _sektorea == sektorea && _hurrengoEszena == hurrengoa && _spawn == spawn
                           && _faseak.Count == faseak.Count
                           && _faseak.Zip(faseak, (a, b) => a.Deskribapena == b.Deskribapena && a.Helburuak.SequenceEqual(b.Helburuak)).All(x => x);
            if (berdina)
            {
                return false;
            }

            _misioIzena = misioIzena;
            _sektorea = sektorea;
            _hurrengoEszena = hurrengoa;
            _spawn = spawn;
            _faseak = faseak.ToList();
            return true;
        }
#endif
    }
}
