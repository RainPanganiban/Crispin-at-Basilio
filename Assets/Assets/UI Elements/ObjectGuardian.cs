using UnityEngine;

public class ObjectGuardian : MonoBehaviour
{
    public GameObject targetObject;

    void LateUpdate()
    {
        // Kung pinatay ng Mirror, buhayin agad sa dulo ng frame
        if (targetObject != null && !targetObject.activeSelf)
        {
            targetObject.SetActive(true);
        }
    }
}