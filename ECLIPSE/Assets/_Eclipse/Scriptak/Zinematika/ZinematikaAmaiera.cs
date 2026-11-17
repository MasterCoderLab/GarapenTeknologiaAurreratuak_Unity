using UnityEngine;
using UnityEngine.Playables;

namespace Eclipse.Zinematika
{
    /// <summary>
    /// Amaierako zinematika (prototipoa, 07_AmaieraZinematika): PlayableDirector-a amaitzean marka bat idazten du eta Amaituta jartzen du.
    /// Etorkizunean (Ion-ek onartuta): menura itzuli edo kredituak. M3-ko garaipenarekin lotzea: docs/estado.md-n dokumentatuta.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class ZinematikaAmaiera : MonoBehaviour
    {
        private PlayableDirector _zuzendaria;

        public bool Amaituta { get; private set; }

        private void Awake()
        {
            _zuzendaria = GetComponent<PlayableDirector>();
        }

        private void Update()
        {
            if (Amaituta || _zuzendaria.playableAsset == null)
            {
                return;
            }

            if (_zuzendaria.time >= _zuzendaria.duration - 0.05)
            {
                Amaituta = true;
                Debug.Log($"ECL_ZINEMATIKA_AMAITUTA {_zuzendaria.duration:0.0} s");
            }
        }
    }
}
