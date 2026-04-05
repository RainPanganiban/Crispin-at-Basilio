using UnityEngine;

public class Billboarding : MonoBehaviour
{
    private Camera localCamera;

    void LateUpdate()
    {
        // 1. Hanapin ang Main Camera kung wala pa ito sa memory
        if (localCamera == null)
        {
            localCamera = Camera.main;
        }

        // 2. Kung may nahanap na camera at buhay ito, lumingon doon
        if (localCamera != null && localCamera.enabled)
        {
            // Pinapaharap ang object sa forward direction ng camera
            transform.LookAt(transform.position + localCamera.transform.forward);

            // OPTIONAL: I-uncomment ang line sa ibaba kung ayaw mong 
            // tumitingala/yumuyuko ang text (Y-axis lock lang)
            // transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        }
        else
        {
            // 3. Fallback: Kung biglang nawala ang Camera.main, 
            // mag-search uli ng anumang active camera
            localCamera = GameObject.FindObjectOfType<Camera>();
        }
    }
}