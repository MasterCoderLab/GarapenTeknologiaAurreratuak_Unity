using System;
using System.Collections.Generic;

namespace Eclipse.Inbentarioak
{
    /// <summary>Partidaren egoera, Unity-tik kanpo: misioen artean gordetzen dena. Eszenako egoerarik ez.</summary>
    public sealed class GameSession
    {
        private readonly HashSet<string> _bildutakoak = new HashSet<string>(StringComparer.Ordinal);

        public Inbentarioa Inbentarioa { get; } = new Inbentarioa();

        public int DatuNukleoak => _bildutakoak.Count;

        /// <summary>Datu-nukleo bat erregistratu. Id bera birritan ez da birritan zenbatzen.</summary>
        public bool BildumaErregistratu(string id)
        {
            return !string.IsNullOrEmpty(id) && _bildutakoak.Add(id);
        }
    }
}
