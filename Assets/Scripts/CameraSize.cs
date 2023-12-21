using UnityEngine;

public class CameraSize : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameController controller;
    [SerializeField] private UpdateUI UI;

    private void Awake()
    {
        cam = this.GetComponent<Camera>();
        Camera.main.transform.position = new Vector3((controller.numberColumns - 1) / 2.0f, -((controller.numberRows - 1) / 2.0f), Camera.main.transform.position.z);
    }

    private void LateUpdate()
    {
        float maxY = controller.numberRows + 2;
        cam.orthographicSize = maxY / 2f;
    }
}
