using UnityEngine;
using TMPro;

public class NameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    private static Camera localCamera;

    public static void SetLocalCamera(Camera cam)
    {
        localCamera = cam;
    }

    public void SetName(string playerName)
    {
        nameText.text = playerName;
    }

    void LateUpdate()
    {
        if (!localCamera) return;

        transform.rotation =
            Quaternion.LookRotation(transform.position - localCamera.transform.position);
    }
}
