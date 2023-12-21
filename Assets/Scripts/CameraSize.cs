using UnityEngine;

public class CameraSize : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameController controller;

    private void Awake()
    {
        cam.transform.position = new Vector3((controller.numberColumns - 1) / 2.0f, -((controller.numberRows - 1) / 2.0f), cam.transform.position.z);
    }

    private void LateUpdate()
    {
        float maxY = controller.numberRows + 2;
        cam.orthographicSize = maxY / 2f;
    }
}
