using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eclipse.Atrezzo
{
    /// <summary>
    /// Misio baten atrezzo-demoa (A3, C blokea): pieza bakoitzaren izena eta kokapena. AtrezzoTresna-k sortzen du (eszena aldatu gabe,
    /// bideak eta helburuak egiaztatuta); AtrezzoKargatzailea-k runtime-n instantziatzen ditu. Eszenetan ez da ezer gordetzen.
    /// </summary>
    public sealed class AtrezzoDatuak : ScriptableObject
    {
        [Serializable]
        public sealed class Elementua
        {
            public string Pieza;
            public Vector3 Lekua;
            public float Yaw;
            public string Gela;
        }

        [SerializeField] private string _eszena;
        [SerializeField] private List<Elementua> _elementuak = new List<Elementua>();

        public string Eszena => _eszena;

        public IReadOnlyList<Elementua> Elementuak => _elementuak;

        public void Ezarri(string eszena, List<Elementua> elementuak)
        {
            _eszena = eszena;
            _elementuak = elementuak;
        }
    }
}
