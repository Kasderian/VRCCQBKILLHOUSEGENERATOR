
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InstructorLaserPen : UdonSharpBehaviour
{
    [Header("References")]
    public GameObject Laser;
    public GameObject Floor;

    [UdonSynced]
    public bool laserActive = false;
    [UdonSynced]
    private bool isHolding = false;
    [UdonSynced]
    public bool floorActive = false;

    void Start()
    {
        UpdateLaser();
        UpdateFloor();
    }

    public override void OnPickup()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        isHolding = true;
        laserActive = true;
        RequestSerialization();
        UpdateLaser();
    }

    public override void OnDrop()
    {
        isHolding = false;
        laserActive = false;
        RequestSerialization();
        UpdateLaser();
    }

    public override void InputUse(bool value, VRC.Udon.Common.UdonInputEventArgs args)
    {
        if (!isHolding)
        {
            return;
        }
        if (!value)
        {
            return;
        }
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        floorActive = !floorActive;
        RequestSerialization();
        UpdateFloor();
    }

    public override void OnDeserialization()
    {
        UpdateLaser();
        UpdateFloor();
    }

    private void UpdateLaser()
    {
        Laser.SetActive(laserActive);
    }

    private void UpdateFloor()
    {
        Floor.SetActive(floorActive);
    }
}
