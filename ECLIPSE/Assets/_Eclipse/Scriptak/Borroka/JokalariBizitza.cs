using System;
using Eclipse.Proba;
using UnityEngine;

namespace Eclipse.Borroka
{
    /// <summary>
    /// Jokalariaren bizitza (BizitzaLogika-ren egokitzailea): kaltea, distira gorria, kolpe-animazioa eta heriotza.
    /// Probetan (BerpiztuAutomatikoki) hasierara itzultzen da; misioetan geroago LevelManager-ek berrabiaraziko du.
    /// </summary>
    public sealed class JokalariBizitza : MonoBehaviour, IKalteHartzailea
    {
        private const string Marka = "ECL_BORROKA";
        private static readonly int KalteaId = Animator.StringToHash("Kaltea");

        private EsploratzaileKontrola _kontrola;
        private BorrokaHud _hud;
        private Vector3 _hasiera;
        private Quaternion _hasierakoBiraketa;
        private float _berpizteUnea = -1f;

        public BizitzaLogika Logika { get; } = new BizitzaLogika(100f, ukiezinDenbora: 0.35f);

        public bool Bizirik => Logika.Bizirik;

        /// <summary>Hil ondoren 2 s-ra hasierara itzuli (proba-eszena). Misioetan false izango da.</summary>
        public bool BerpiztuAutomatikoki { get; set; }

        public int Heriotzak { get; private set; }

        public event Action<KalteInfo> KalteaJasoDu;

        public event Action Hildakoa;

        public void Konfiguratu(EsploratzaileKontrola kontrola, BorrokaHud hud)
        {
            _kontrola = kontrola;
            _hud = hud;
            _hasiera = transform.position;
            _hasierakoBiraketa = transform.rotation;
        }

        public void KalteaJaso(in KalteInfo kaltea)
        {
            KalteEmaitza emaitza = Logika.KalteaHartu(kaltea.Kopurua * GorputzKalteLogika.Biderkatzailea(kaltea.Zona), Time.time);
            if (emaitza == KalteEmaitza.Ezikusia)
            {
                return;
            }

            KalteaJasoDu?.Invoke(kaltea);
            if (_hud != null)
            {
                _hud.Distira(emaitza == KalteEmaitza.Hilda ? 1f : 0.6f);
            }

            Animator an = _kontrola != null && _kontrola.Bisuala != null ? _kontrola.Bisuala.Animatzailea : null;
            if (an != null)
            {
                an.SetTrigger(KalteaId);
            }

            if (emaitza == KalteEmaitza.Hilda)
            {
                Heriotzak++;
                Debug.Log($"{Marka}_JOKALARIA_HIL {transform.position}");
                Hildakoa?.Invoke();
                if (BerpiztuAutomatikoki)
                {
                    _berpizteUnea = Time.time + 2f;
                }
            }
        }

        private void Update()
        {
            if (_berpizteUnea > 0f && Time.time >= _berpizteUnea)
            {
                _berpizteUnea = -1f;
                Berpiztu();
            }
        }

        public void Berpiztu()
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            transform.SetPositionAndRotation(_hasiera, _hasierakoBiraketa);
            if (cc != null)
            {
                cc.enabled = true;
            }

            Logika.Berrezarri();
            Debug.Log($"{Marka}_JOKALARIA_BERPIZTUA {_hasiera}");
        }
    }
}
