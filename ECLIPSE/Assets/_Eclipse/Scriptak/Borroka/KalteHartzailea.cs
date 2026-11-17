using UnityEngine;

namespace Eclipse.Borroka
{
    /// <summary>Kolpe edo tiro baten datuak.</summary>
    public readonly struct KalteInfo
    {
        public KalteInfo(KalteZona zona, float kopurua, Vector3 puntua, Vector3 norabidea)
        {
            Zona = zona;
            Kopurua = kopurua;
            Puntua = puntua;
            Norabidea = norabidea;
        }

        public KalteZona Zona { get; }

        /// <summary>Oinarrizko kaltea, zonaren biderkatzailea aplikatu aurretik.</summary>
        public float Kopurua { get; }

        public Vector3 Puntua { get; }

        public Vector3 Norabidea { get; }
    }

    /// <summary>Kaltea jaso dezakeen edozer: jokalaria, etsaiak eta, etorkizunean, NPC-ak edo objektuak.</summary>
    public interface IKalteHartzailea
    {
        bool Bizirik { get; }

        void KalteaJaso(in KalteInfo kaltea);
    }
}
