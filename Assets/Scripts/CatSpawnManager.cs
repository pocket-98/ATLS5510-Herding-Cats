using UnityEngine;
using Meta.XR.MRUtilityKit; // MRUK namespace – ensure you have the MRUK package installed

/// <summary>
/// Spawns a prefab at the “center” of a table (or any upward-facing surface that meets a given label filter)
/// without requiring a direct reference to the table object.
/// </summary>
public class CenterSpawnManager : MonoBehaviour
{
    [Tooltip("Prefab to spawn on the surface.")]
    public GameObject spawnPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Which surface type to target. For tables, choose upward-facing surfaces.")]
    private MRUK.SurfaceType targetSurface = MRUK.SurfaceType.FACING_UP;

    [Tooltip("Labels to filter the surfaces (e.g. use the Table label if available).")]
    // Replace 'Table' with the actual bit corresponding to your table label.
    public MRUKAnchor.SceneLabels surfaceLabels = MRUKAnchor.SceneLabels.TABLE;

    [Tooltip("Offset along the surface normal to adjust spawn position (e.g. to account for prefab bottom).")]
    public float baseOffset = 0f;

    [Tooltip("Additional offset along the surface normal to adjust to the 'center' of the surface.")]
    public float centerOffset = 0f;

    [Tooltip("Clearance distance to ensure nothing obstructs the spawn area.")]
    public float surfaceClearanceDistance = 0.1f;

    // Called on Start – if MRUK data is available, wait for the scene to be loaded.
    private void Start()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }
        else
        {
            // Fallback: if MRUK isn't available, you could optionally define a default position.
            Debug.LogWarning("MRUK Instance not found – cannot auto-find a surface. Spawning at Vector3.zero.");
            SpawnAtPosition(Vector3.zero, Quaternion.identity);
        }
    }

    // Called when the scene (room data) is loaded
    private void OnSceneLoaded()
    {
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();
        if (room != null)
        {
            SpawnOnSurface(room);
        }
        else
        {
            Debug.LogWarning("No room data available from MRUK. Spawning at default position.");
            SpawnAtPosition(Vector3.zero, Quaternion.identity);
        }
    }

    // Uses the room’s GenerateRandomPositionOnSurface method to get a position on a surface
    private void SpawnOnSurface(MRUKRoom room)
    {
        Vector3 spawnPosition = Vector3.zero;
        Vector3 spawnNormal = Vector3.up;
        float minRadius = 0.0f; // For surface placement, you can set this to zero
        // Create a label filter using the desired surface labels (for example, Table)
        LabelFilter filter = new LabelFilter(surfaceLabels);
        MRUKAnchor table = room.FindLargestSurface(surfaceLabels);
        if(room.GenerateRandomPositionOnSurface(targetSurface, minRadius, filter, out Vector3 pos, out Vector3 normal))
        {
            
            if (table != null)
            {
                //Find center of surface
                Vector3 center = table.GetAnchorCenter();
                Vector3 tableSurfaceCenter = new Vector3(center.x, pos.y + baseOffset, center.z);
                SpawnAtPosition(tableSurfaceCenter, Quaternion.identity);
            }
        }
        else
        {
            Debug.LogWarning("No surface found in the room data. Spawning at default position.");
            SpawnAtPosition(Vector3.zero, Quaternion.identity);
        }
    }

    // Instantiates the prefab at the given position and rotation.
    private void SpawnAtPosition(Vector3 position, Quaternion rotation)
    {
        if (spawnPrefab == null)
        {
            Debug.LogError("Spawn prefab is not assigned!");
            return;
        }
        Instantiate(spawnPrefab, position, rotation);
        Debug.Log("Spawned prefab at: " + position);
    }
}
