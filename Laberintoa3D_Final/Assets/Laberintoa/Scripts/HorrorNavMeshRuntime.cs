using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh-a etsaien agenteak aktibatu BAINO LEHEN kargatzen du.
/// Eszenan hiru NavMeshAgent osagaiak desgaituta gordetzen dira.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class HorrorNavMeshRuntime : MonoBehaviour
{
    public NavMeshData navMeshData;
    [SerializeField] public NavMeshAgent[] delayedAgents;
    private NavMeshDataInstance instance;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (navMeshData == null)
        {
            Debug.LogError("HORROR_NAV_ERROR: NavMeshData falta da.", this);
            return;
        }
        instance = NavMesh.AddNavMeshData(navMeshData);
        if (!instance.valid)
        {
            Debug.LogError("HORROR_NAV_ERROR: NavMeshData ezin izan da kargatu.", this);
            return;
        }
        int ready = 0;
        if (delayedAgents != null)
        {
            foreach (NavMeshAgent agent in delayedAgents)
            {
                if (agent == null)
                {
                    Debug.LogError("HORROR_NAV_ERROR: agenteen erreferentzia falta da.", this);
                    continue;
                }
                if (agent.enabled)
                {
                    Debug.LogError("HORROR_NAV_ERROR: agentea aurretik aktibatuta: " + agent.name, agent);
                    continue;
                }
                // NavMesh-a erregistratuta dago; orain bakarrik sortu agentea.
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                {
                    Debug.LogError("HORROR_NAV_ERROR: agentea NavMesh-etik kanpo: " + agent.name, agent);
                    agent.enabled = false;
                    continue;
                }
                ready++;
            }
        }
        if (ready == 3)
            Debug.Log("HORROR_NAV_READY: 3/3 agents on NavMesh; loaded before enabling agents.");
        else
            Debug.LogError("HORROR_NAV_ERROR: agenteak " + ready + "/3 NavMesh-ean.", this);
    }

    private void OnDisable()
    {
        if (Application.isPlaying && delayedAgents != null)
        {
            foreach (NavMeshAgent agent in delayedAgents)
                if (agent != null && agent.enabled) agent.enabled = false;
        }
        if (instance.valid) instance.Remove();
    }
}
