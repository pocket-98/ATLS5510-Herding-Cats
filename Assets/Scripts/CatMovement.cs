using UnityEngine;

public class CatMovement : MonoBehaviour
{
    static float moveSpeed = 0.3f;          // Forward movement speed
    static float uprightSpeed = 15f;         // Speed at which the cat corrects its rotation
    static float downwardThreshold = -0.5f;  // Threshold to decide if the cat is moving downward
    static float uprightAngleThreshold = 10f; // Maximum allowed deviation (in degrees) from upright
    
    public static Vector3 MoveCat(GameObject cat)
    {
        Rigidbody rb = cat.GetComponent<Rigidbody>();
        rb.WakeUp();

        // If the cat is kinematic, do nothing.
        if (rb.isKinematic)
        {
            return;
        }

        // Only move forward if the cat is nearly upright and not moving significantly downward.
        if (Vector3.Angle(cat.transform.up, Vector3.up) < uprightAngleThreshold && rb.velocity.y > downwardThreshold)
        {
            MoveForward(cat, rb);
        }

        // Keep the cat upright regardless of movement direction.
        KeepUpright(cat, rb);
        return cat.transform.position;
    }

    static void MoveForward(GameObject cat, Rigidbody rb)
    {
        // Preserve the vertical component of velocity.
        float verticalVelocity = rb.velocity.y;

        // Determine current forward speed along the object's facing direction.
        float forwardSpeed = Vector3.Dot(rb.velocity, cat.transform.forward);

        // Only update the horizontal velocity if it's slower than moveSpeed.
        if (forwardSpeed < moveSpeed)
        {
            // Set horizontal velocity along the object's forward direction.
            Vector3 horizontalVelocity = cat.transform.forward * moveSpeed;

            // Combine with vertical velocity.
            rb.velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        }
    }


    static void KeepUpright(GameObject cat, Rigidbody rb)
    {
        // Calculate the target rotation that aligns the cat's up vector with world up.
        Quaternion uprightRotation = Quaternion.FromToRotation(cat.transform.up, Vector3.up) * cat.transform.rotation;
        rb.MoveRotation(Quaternion.Slerp(cat.transform.rotation, uprightRotation, Time.fixedDeltaTime * uprightSpeed));
    }
}