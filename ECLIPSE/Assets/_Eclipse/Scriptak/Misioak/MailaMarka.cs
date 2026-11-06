using UnityEngine;

namespace Eclipse.Misioak
{
    public enum MarkaMota
    {
        Etsaia,
        Kobertura,
        Arriskua,
    }

    /// <summary>Etorkizuneko mekanikentzako markatzailea: etsaien sorrera-puntuak, koberturak eta arriskuak. Editorean bakarrik ikusten da.</summary>
    public sealed class MailaMarka : MonoBehaviour
    {
        [SerializeField] private MarkaMota _mota;

        public MarkaMota Mota => _mota;

        private void OnDrawGizmos()
        {
            Gizmos.color = _mota == MarkaMota.Etsaia ? Color.red : _mota == MarkaMota.Kobertura ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
        }

#if UNITY_EDITOR
        public bool Konfiguratu(MarkaMota mota)
        {
            if (_mota == mota)
            {
                return false;
            }

            _mota = mota;
            return true;
        }
#endif
    }
}
