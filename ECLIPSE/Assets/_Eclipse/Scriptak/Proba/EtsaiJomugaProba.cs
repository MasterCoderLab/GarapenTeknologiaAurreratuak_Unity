using Eclipse.Borroka;
using UnityEngine;

namespace Eclipse.Proba
{
    /// <summary>
    /// 06_NpcProba eszenarako: markatzaile honetan sortzen den etsaia jomuga geldia bihurtzen du (IA geldituta), desmenbratzea
    /// lasai probatzeko. Kaltea, zatiak eta heriotza berdin dabiltza. Misioetan ez da erabiltzen.
    /// </summary>
    public sealed class EtsaiJomugaProba : MonoBehaviour
    {
        private const float Erradioa = 1.5f;

        private void Update()
        {
            foreach (Etsaia e in FindObjectsByType<Etsaia>(FindObjectsInactive.Exclude))
            {
                if (Vector3.Distance(e.transform.position, transform.position) <= Erradioa)
                {
                    e.AdimenaGelditu = true;
                    Debug.Log($"ECL_NPCPROBA_JOMUGA {e.name}: IA geldituta {transform.position}");
                    enabled = false;
                    return;
                }
            }
        }
    }
}
