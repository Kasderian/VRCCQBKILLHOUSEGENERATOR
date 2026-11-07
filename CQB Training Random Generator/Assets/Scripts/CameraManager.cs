
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CameraManager : UdonSharpBehaviour
{
    [Header("Reference")]
    public Camera topDownCamera;
    public GameObject cameraStatusDisplay;
    public HouseGenerator houseGenerator;

    private bool cameraActive = true;

    public void Start()
    {
        topDownCamera.enabled = cameraActive;
        cameraStatusDisplay.SetActive(cameraActive);
    }
    public override void Interact()
    {
        cameraActive = !cameraActive;
        topDownCamera.enabled = cameraActive;
        cameraStatusDisplay.SetActive(cameraActive);
        if (cameraActive) { SetCameraUp(); }
    }

    public void SetCameraUp()
    {
        if (!cameraActive)
        {
            //For when other scripts call this function but camera is not active.
            return;
        }
        if (topDownCamera == null)
        {
            return;
        }
        if (houseGenerator == null)
        {
            return;
        }
        if (houseGenerator.width < 1 || houseGenerator.length < 1)
        {
            return;
        }

        topDownCamera.orthographic = true;
        float cameraPositionX = houseGenerator.width / 2f;
        float cameraPositionZ = houseGenerator.length / 2f;

        topDownCamera.orthographicSize = Mathf.Max(cameraPositionX, cameraPositionZ) + 2f;
        topDownCamera.transform.position = new Vector3(cameraPositionX, 100f, cameraPositionZ);
        return;
    }
}

