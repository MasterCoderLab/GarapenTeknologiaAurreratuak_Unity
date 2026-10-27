using UnityEngine;

/// <summary>
/// Horror jokoaren egoera: hiru bizitza, hiru etsai eta garaipena/porrota.
/// </summary>
public sealed class HorrorGameManager : MonoBehaviour
{
    [SerializeField] private HorrorPlayerMovement movement;
    [SerializeField] private HorrorShotgun shotgun;
    [SerializeField] private HorrorEnemyHealth[] enemies;
    [SerializeField] private int lives = 3;
    private int defeated;
    private float lastHit = -100f;
    private bool ended;
    private bool won;
    private GUIStyle bigStyle;
    private GUIStyle smallStyle;

    public bool Ended => ended;
    public int Lives => lives;

    public void Configure(HorrorPlayerMovement playerMovement, HorrorShotgun gun,
        HorrorEnemyHealth[] enemyHealth)
    {
        movement = playerMovement;
        shotgun = gun;
        enemies = enemyHealth;
    }

    public void RegisterHit()
    {
        if (ended || Time.time - lastHit < 1.5f) return;
        lastHit = Time.time;
        lives = Mathf.Max(0, lives - 1);
        Debug.Log("HORROR_LIFE: " + lives + "/3");
        if (lives == 0) Finish(false);
    }

    public void EnemyDefeated()
    {
        if (ended) return;
        defeated++;
        Debug.Log("HORROR_KILL: " + defeated + "/3");
        if (defeated >= 3) Finish(true);
    }

    private void Finish(bool victory)
    {
        ended = true;
        won = victory;
        if (movement != null)
        {
            movement.enabled = false;
            Rigidbody rb = movement.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
        if (enemies != null)
        {
            foreach (HorrorEnemyHealth hp in enemies)
            {
                if (hp == null || !hp.gameObject.activeInHierarchy) continue;
                HorrorEnemyAI ai = hp.GetComponent<HorrorEnemyAI>();
                if (ai != null) ai.enabled = false;
                UnityEngine.AI.NavMeshAgent agent = hp.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = true;
            }
        }
        Debug.Log(victory ? "HORROR_VICTORY: 3/3 enemies defeated" : "HORROR_GAME_OVER: no lives remaining");
    }

    private void EnsureStyles()
    {
        if (bigStyle != null) return;
        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bigStyle.normal.textColor = Color.white;
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
        smallStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.Box(new Rect(14, 12, 360, 96), "");
        GUI.Label(new Rect(26, 22, 340, 26), "VIDAS  " + lives + "/3    MONSTRUOS  " + defeated + "/3", smallStyle);
        GUI.Label(new Rect(26, 48, 345, 26), shotgun != null ? shotgun.StatusText : "Busca la escopeta", smallStyle);
        GUI.Label(new Rect(26, 76, 340, 24), "WASD: mover   F: linterna   E: recoger", GUI.skin.label);
        if (ended)
        {
            Rect centered = new Rect(Screen.width / 2f - 280, Screen.height / 2f - 80, 560, 160);
            GUI.Box(centered, "");
            GUI.Label(new Rect(centered.x + 20, centered.y + 28, 520, 52),
                won ? "¡VICTORIA!" : "GAME OVER", bigStyle);
            GUI.Label(new Rect(centered.x + 20, centered.y + 96, 520, 30),
                "Sal de Play y vuelve a entrar para reiniciar.", GUI.skin.label);
        }
    }
}
