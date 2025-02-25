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
    
    private float NumCats = 1;

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
            //SpawnAtPosition(Vector3.zero, Quaternion.identity);
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
    
    //Positions the scoreboard on the back edge of the table from the perspective of the player, facing the center of the table.
    private void PositionScoreboard(MRUKAnchor table)
    {
        // Get the center of the table
        Vector3 center = table.GetAnchorCenter();
        float tableHeightFromCenter = room.GenerateRandomPositionOnSurface(targetSurface, 0, new LabelFilter(surfaceLabels), out Vector3 pos, out Vector3 normal) ? pos.y : 0;
        // Get the player's position
        Vector3 playerPosition = playerTransform.position;
        // Calculate the vector from the player to the center of the table
        Vector3 playerToCenter = center - playerPosition;
        // Calculate the position of the scoreboard
        Vector3 scoreboardPositionInitial = new Vector3(center.x, center.y + ((tableHeightFromCenter - center.y) * 2), center.z) - playerToCenter.normalized * 0.5f;
        // Sets the max distance from the scoreboard to the player in the x, y, and z directions
        Vector3 maxPlayerXYZDistance = new Vector3(Mathf.Abs(playerPosition.x - scoreboardPositionInitial.x), Mathf.Abs(playerPosition.y - scoreboardPositionInitial.y), Mathf.Abs(playerPosition.z - scoreboardPositionInitial.z));
        // Sets the scoreboard to be 0.5 units away from the player compared to its initial position on the axis with the largest distance from the player
        //Can position on X, -X, Z, -Z, not Y, -Y
        char largestDistanceIndex = maxPlayerXYZDistance.x > maxPlayerXYZDistance.y ? (maxPlayerXYZDistance.x > maxPlayerXYZDistance.z ? 'X' : 'Z') : (maxPlayerXYZDistance.y > maxPlayerXYZDistance.z ? 'Y' : 'Z');
        // If largest distance X, move away from the player 1f on the X axis, if Y, move away from the player 1f on the Y axis, if Z, move away from the player 1f on the Z axis, only one axis will be moved away from the player.
        Vector3 scoreboardPosition = new Vector3(scoreboardPositionInitial.x + (largestDistanceIndex == 'X' ? (playerPosition.x > scoreboardPositionInitial.x ? -1f : 1f) : 0), scoreboardPositionInitial.y, scoreboardPositionInitial.z + (largestDistanceIndex == 'Z' ? (playerPosition.z > scoreboardPositionInitial.z ? -0.5f : 0.5f) : 0));
        //Vector3 scoreboardPosition = new Vector3(scoreboardPositionInitial.x + (maxPlayerXYZDistance.x > maxPlayerXYZDistance.y ? (playerPosition.x > scoreboardPositionInitial.x ? 0.5f : -0.5f) : 0), scoreboardPositionInitial.y + (maxPlayerXYZDistance.y > maxPlayerXYZDistance.x ? (playerPosition.y > scoreboardPositionInitial.y ? 0.5f : -0.5f) : 0), scoreboardPositionInitial.z + (maxPlayerXYZDistance.z > maxPlayerXYZDistance.y ? (playerPosition.z > scoreboardPositionInitial.z ? 0.5f : -0.5f) : 0));
        
        
        // Set the position of the scoreboard
        scoreboardCanvas.transform.position = scoreboardPosition;
        // Rotate the scoreboard to face the center of the table
        scoreboardCanvas.transform.LookAt(scoreboardPositionInitial);
        scoreboardCanvas.transform.Rotate(0, 180, 0);
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
            // Update the scoreboard text
            if (ScoreboardTextComponent.GetComponent<TMP_Text>() != null)
            {
                ScoreboardTextComponent.GetComponent<TMP_Text>().SetText($"Score: (Cats Spawned): {NumCats}");
            }
            // Move the cat
            Vector3 newCatPosition = CatMovement.MoveCat(cat);
            // Destroy the cat if it falls below the table, Set Game Over Logic.
            if (newCatPosition.y < -10)
            {
                foreach (GameObject c in cats)
                {
                    Destroy(c);
                }
                cats.Remove(cat);
                cats.Clear();
                ScoreboardTextComponent.GetComponent<TMP_Text>().SetText("Game Over! You managed " + NumCats + " cats.");
                LastSpawnTime = Time.time;
                NumCats = 0;
            }
        }
        
    }
}
