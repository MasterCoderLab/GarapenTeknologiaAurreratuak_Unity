using UnityEngine;

/// <summary>Etsai batek bi tiro jasaten ditu; hiltzean desaktibatzen da.</summary>
public sealed class HorrorEnemyHealth : MonoBehaviour
{
    [SerializeField] private HorrorGameManager manager;
    [SerializeField] private int hitPoints = 2;
    private bool dead;

    public void Configure(HorrorGameManager gameManager) { manager = gameManager; }

    public void TakeShot()
    {
        if (dead || manager == null || manager.Ended) return;
        hitPoints--;
        Debug.Log("HORROR_ENEMY_HIT: " + name + " HP=" + hitPoints);
        if (hitPoints > 0) return;
        dead = true;
        Debug.Log("HORROR_ENEMY_DEAD: " + name);
        manager.EnemyDefeated();
        gameObject.SetActive(false);
    }
}
