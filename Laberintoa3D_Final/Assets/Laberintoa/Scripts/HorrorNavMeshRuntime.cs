using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gordetako nabigazio-sarea jokoan kargatu eta askatzen du.
/// </summary>
public sealed class HorrorNavMeshRuntime : MonoBehaviour
{
    public NavMeshData navMeshData;
    private NavMeshDataInstance instance;

    private void OnEnable()
    {
        if (Application.isPlaying && navMeshData != null)
            instance = NavMesh.AddNavMeshData(navMeshData);
    }

    private void OnDisable()
    {
        if (instance.valid)
            instance.Remove();
    }
}
