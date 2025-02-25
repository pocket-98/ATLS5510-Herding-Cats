using System;
using UnityEngine;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction; // MRUK namespace – ensure you have the MRUK package installed
using TMPro;

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
    public float baseOffset = 0.1f;

    [Tooltip("Additional offset along the surface normal to adjust to the 'center' of the surface.")]
    public float centerOffset = 0f;

    [Tooltip("Clearance distance to ensure nothing obstructs the spawn area.")]
    public float surfaceClearanceDistance = 0.1f;
    
    private float LastSpawnTime = 0;
    
    private float NumCats = 0;

    private MRUKRoom room = null;
    
    private List<GameObject> cats = new List<GameObject>();
    
    public GameObject ScoreboardTextComponent;
    
    public GameObject scoreboardCanvas;
    
    public Transform playerTransform; // Reference to the player's transform


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
            ScoreboardTextComponent.GetComponent<TMP_Text>().SetText("No Table Found, Please Perform a Space Setup and add exactly 1 table with a sizable surface area.");
            SpawnAtPosition(Vector3.zero, Quaternion.identity);
        }
    }

    // Called when the scene (room data) is loaded
    private void OnSceneLoaded()
    {
        // Find the player GameObject by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            ScoreboardTextComponent.GetComponent<TMP_Text>().SetText("Score: (Cats Spawned): 0");
        }
        else
        {
            Debug.LogError("Player GameObject not found.");
        }
        
        // Get the current room data
        room = MRUK.Instance.GetCurrentRoom();
        
        // If room data is available, spawn the prefab on the largest surface
        if (room != null)
        {
            SpawnOnSurface(room);
            // Generates surface plane Rigidbody2D on the top surface for the cats to move on
            MRUKAnchor table = room.FindLargestSurface(surfaceLabels);
            if (table != null)
            {
                PositionScoreboard(table);
                Vector3 position = table.GetAnchorCenter();
                // Ensure the table has a Rigidbody2D
                Rigidbody rb = table.gameObject.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = table.gameObject.AddComponent<Rigidbody>();
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    rb.mass = 10;
                }
                // Get the center of the table
                
                // Ensure the table has a BoxCollider2D and then assign a zero-friction material to it
                BoxCollider col = table.gameObject.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = table.gameObject.AddComponent<BoxCollider>();
                    col.center = position;
                }
                
                PositionScoreboard(table);
            }

        }
        else
        {
            ScoreboardTextComponent.GetComponent<TMP_Text>().SetText("Could not find room data.");
            Debug.LogWarning("No room data available from MRUK. Spawning at default position.");
            SpawnAtPosition(Vector3.zero, Quaternion.identity);
        }
        // Ensure ScoreboardText is 
        /*
        if (ScoreboardText != null)
        {
            scoreboardTextComponent = ScoreboardText.GetComponent<TextMeshPro>();
            if (scoreboardTextComponent == null)
            {
                Debug.LogError("TextMeshPro component not found on ScoreboardText.");
            }
        }
        else
        {
            Debug.LogError("ScoreboardText GameObject is not assigned.");
        }
        */
    }
    
    
    private void PositionScoreboard(MRUKAnchor table)
    {
        // Retrieve the VolumeBounds of the table
        Bounds? nullableBounds = table.VolumeBounds;
        
        if (nullableBounds.HasValue)
        {
            Bounds tableBounds = nullableBounds.Value;
            // Calculate the corners of the table based on the bounds
            Vector3 tableCenter = tableBounds.center;
            Vector3 tableExtents = tableBounds.size;
            
            float minRadius = 0.0f; // For surface placement, can be set to zero
            LabelFilter filter = new LabelFilter(surfaceLabels);
            if (room.GenerateRandomPositionOnSurface(targetSurface, minRadius, filter, out Vector3 pos,
                    out Vector3 normal))
            {
                Vector3 playerPosition = playerTransform.position;
                Vector3 extents = tableBounds.extents;

// Define the corners of the table's top face
                Vector3[] topCorners = new Vector3[4];
                topCorners[0] = tableCenter + new Vector3(extents.x, extents.y, extents.z);  // Front-Right
                topCorners[1] = tableCenter + new Vector3(-extents.x, extents.y, extents.z); // Front-Left
                topCorners[2] = tableCenter + new Vector3(extents.x, extents.y, -extents.z); // Back-Right
                topCorners[3] = tableCenter + new Vector3(-extents.x, extents.y, -extents.z);// Back-Left

// Determine the furthest edge from the player
                Vector3 furthestCorner1 = topCorners[0];
                Vector3 furthestCorner2 = topCorners[1];
                float maxDistance = 0f;

                for (int i = 0; i < topCorners.Length; i++)
                {
                    for (int j = i + 1; j < topCorners.Length; j++)
                    {
                        float distance = (topCorners[i] - playerPosition).sqrMagnitude + (topCorners[j] - playerPosition).sqrMagnitude;
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            furthestCorner1 = topCorners[i];
                            furthestCorner2 = topCorners[j];
                        }
                    }
                }

                Vector3 furthestEdgeCenter = (furthestCorner1 + furthestCorner2) / 2;

                // Offset the scoreboard above the furthest corner
                Vector3 scoreboardPosition = furthestEdgeCenter + new Vector3(0, 0.2f, 0); // Adjust the Y offset as needed

                // Instantiate the ScoreboardText at the calculated position

                scoreboardCanvas.transform.position = scoreboardPosition;

                // Optionally, adjust the rotation to face the player
                scoreboardCanvas.transform.LookAt(new Vector3(playerPosition.x, scoreboardCanvas.transform.position.y,
                    playerPosition.z));
                scoreboardCanvas.transform.Rotate(0, 180, 0); // Rotate to face the player
            }
        }
        else
        {
            Debug.LogWarning("Table VolumeBounds is null.");
            // Handle the null case appropriately
        }
        

        
    }


    // Uses the room’s GenerateRandomPositionOnSurface method to get a position on a surface
    private void SpawnOnSurface(MRUKRoom room)
    {
        Vector3 spawnPosition = Vector3.zero;
        Vector3 spawnNormal = Vector3.up;
        
        Quaternion randomRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
        
        float minRadius = 0.0f; // For surface placement, can be set to zero
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
                SpawnAtPosition(tableSurfaceCenter, randomRotation);
            }
        }
        else
        {
            Debug.LogWarning("No surface found in the room data. Spawning at default position.");
            SpawnAtPosition(Vector3.zero, randomRotation);
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
        cats.Add(Instantiate(spawnPrefab, position, rotation) as GameObject);
        Debug.Log("Spawned prefab at: " + position);
    }
    
    //Spawn cats every 10 seconds that move in a consistent random direction at a set speed (catSpeed)
    private void Update()
    {
        // Spawns a cat that will always try to move in a random direction until moved up by the player
        // Logic for handling cat movement after being picked up by the player + Game-Over logic included in CatMovement.cs
        if (Time.time - LastSpawnTime > 15)
        {
            LastSpawnTime = Time.time;
            NumCats++;
            SpawnOnSurface(room);
        }
        
        
        //Moves the cats
        foreach (GameObject cat in cats)
        {
            CatMovement.MoveCat(cat);
        }
        // Update the scoreboard text
        if (ScoreboardTextComponent.GetComponent<TMP_Text>() != null)
        {
            ScoreboardTextComponent.GetComponent<TMP_Text>().SetText($"Score: (Cats Spawned): {NumCats}");
        }
        
    }
}
