using UnityEngine;

/// <summary>Jokalariaren kolpeak kudeatzaile nagusira bideratzen ditu.</summary>
public sealed class HorrorPlayerHealth : MonoBehaviour
{
    [SerializeField] private HorrorGameManager manager;

    public void Configure(HorrorGameManager gameManager) { manager = gameManager; }

    public void TakeHit()
    {
        if (manager != null) manager.RegisterHit();
    }
}
