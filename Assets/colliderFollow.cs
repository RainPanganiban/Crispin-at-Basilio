using UnityEngine;

public class FollowBoneCollider : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform targetBone;    // The bone or object you want the collider to follow
    public Vector3 localOffset;     // Offset relative to the target bone
    public bool followRotation = true; // Should the collider rotate with the bone?

    void LateUpdate()
    {
        if (targetBone == null) return;

        // Follow position
        transform.position = targetBone.position + targetBone.TransformDirection(localOffset);

        // Optionally follow rotation
        if (followRotation)
        {
            transform.rotation = targetBone.rotation;
        }
    }
}