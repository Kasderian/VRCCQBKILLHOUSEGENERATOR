using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HouseGenerator : UdonSharpBehaviour
{
    // ============================================================
    // HOUSE SETTINGS
    // ============================================================

    [Header("House Size")]

    public int width = 12;
    public int length = 10;

    [Header("Seed")]

    public int seed = 12345;


    // ============================================================
    // NETWORKED SETTINGS
    // ============================================================

    [UdonSynced]
    private int syncedWidth = 12;

    [UdonSynced]
    private int syncedLength = 10;

    [UdonSynced]
    private int syncedSeed = 12345;

    // Changes whenever a new house generation is requested.
    [UdonSynced]
    private int generationVersion = 0;

    // The last generation version this client actually generated.
    private int lastGeneratedVersion = -1;


    // ============================================================
    // PENDING GENERATION REQUEST
    // ============================================================

    // These are LOCAL variables.
    //
    // They are used when a player presses Generate while another
    // player currently owns the HouseGenerator.
    private bool hasPendingRequest;

    private int pendingWidth;
    private int pendingLength;
    private int pendingSeed;


    // ============================================================
    // BSP GENERATOR
    // ============================================================

    [Header("Generator Reference")]

    public BSPGenerator bspGenerator;


    // ============================================================
    // OTHER SCRIPT REFERENCES
    // ============================================================

    [Header("UI Reference")]
    public GeneratorUI generatorUI;

    [Header("Builder Reference")]

    public HouseBuilder houseBuilder;

    [Header("Room Connector")]

    public RoomConnector roomConnector;

    [Header("Exterior Door Generator")]

    public ExteriorDoorGenerator exteriorDoorGenerator;

    [Header("Exterior Window Generator")]

    public ExteriorWindowGenerator exteriorWindowGenerator;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    public bool generateOnStart = true;


    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        if (!generateOnStart)
        {
            return;
        }

        // Only the current owner establishes the initial state.
        //
        // We deliberately do not make every client generate here.
        if (Networking.IsOwner(gameObject))
        {
            syncedWidth = ClampWidth(width);
            syncedLength = ClampLength(length);
            syncedSeed = seed;

            width = syncedWidth;
            length = syncedLength;
            seed = syncedSeed;

            generationVersion++;

            RequestSerialization();

            ApplySyncedSettingsAndGenerate();
        }
    }


    // ============================================================
    // OWNERSHIP TRANSFER
    // ============================================================

    public override void OnOwnershipTransferred(
        VRCPlayerApi player)
    {
        // Only the new local owner needs to do anything here.
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }

        // Did this player request a generation while another
        // player owned the HouseGenerator?
        if (!hasPendingRequest)
        {
            return;
        }

        // Consume the pending request.
        hasPendingRequest = false;

        // Now we actually own the object, so it is safe to update
        // the synchronized state.
        CommitGeneration(
            pendingWidth,
            pendingLength,
            pendingSeed
        );
    }


    // ============================================================
    // NETWORK DESERIALIZATION
    // ============================================================

    public override void OnDeserialization()
    {
        // We received synchronized settings from the owner.

        if (generationVersion == lastGeneratedVersion)
        {
            return;
        }

        ApplySyncedSettingsAndGenerate();
    }


    // ============================================================
    // APPLY SYNCHRONIZED SETTINGS
    // ============================================================

    private void ApplySyncedSettingsAndGenerate()
    {
        width = syncedWidth;
        length = syncedLength;
        seed = syncedSeed;

        lastGeneratedVersion = generationVersion;

        GenerateHouse();

        if (generatorUI != null)
        {
            generatorUI.RefreshCurrentSettingsDisplay();
        }
    }


    // ============================================================
    // COMMIT A GENERATION REQUEST
    // ============================================================

    private void CommitGeneration(
        int newWidth,
        int newLength,
        int newSeed)
    {
        // This method must only be called by the owner.

        if (!Networking.IsOwner(gameObject))
        {
            Debug.LogWarning(
                "HouseGenerator: Cannot commit generation because " +
                "this player is not the owner."
            );

            return;
        }

        // Apply safety limits before synchronizing.
        newWidth = ClampWidth(newWidth);
        newLength = ClampLength(newLength);

        // Update local settings.
        width = newWidth;
        length = newLength;
        seed = newSeed;

        // Update networked settings.
        syncedWidth = newWidth;
        syncedLength = newLength;
        syncedSeed = newSeed;

        // Tell everyone that a new generation is available.
        generationVersion++;

        RequestSerialization();

        // Generate immediately for the owner.
        ApplySyncedSettingsAndGenerate();
    }


    // ============================================================
    // GENERATE HOUSE
    // ============================================================

    public void GenerateHouse()
    {
        // Make sure the BSP generator exists.
        if (bspGenerator == null)
        {
            Debug.LogError(
                "HouseGenerator: BSPGenerator is not assigned."
            );

            return;
        }


        // ============================================================
        // BASIC SAFETY LIMITS
        // ============================================================

        width = ClampWidth(width);
        length = ClampLength(length);


        // ============================================================
        // 1. GENERATE ROOM LAYOUT
        // ============================================================

        bspGenerator.Generate(
            width,
            length,
            seed
        );


        // ============================================================
        // 2. GENERATE INTERIOR ROOM CONNECTIONS / DOORS
        // ============================================================

        if (roomConnector != null)
        {
            roomConnector.GenerateConnections(seed);
        }
        else
        {
            Debug.LogWarning(
                "HouseGenerator: RoomConnector is not assigned."
            );
        }


        // ============================================================
        // 3. GENERATE EXTERIOR DOORS
        // ============================================================

        if (exteriorDoorGenerator != null)
        {
            exteriorDoorGenerator.GenerateExteriorDoors(seed);
        }
        else
        {
            Debug.LogWarning(
                "HouseGenerator: ExteriorDoorGenerator is not assigned."
            );
        }


        // ============================================================
        // 4. GENERATE EXTERIOR WINDOWS
        // ============================================================

        if (exteriorWindowGenerator != null)
        {
            exteriorWindowGenerator.GenerateExteriorWindows(
                width,
                length,
                seed
            );
        }
        else
        {
            Debug.LogWarning(
                "HouseGenerator: ExteriorWindowGenerator is not assigned."
            );
        }


        // ============================================================
        // 5. BUILD THE UNITY HOUSE
        // ============================================================

        if (houseBuilder != null)
        {
            houseBuilder.BuildHouse();
        }
        else
        {
            Debug.LogWarning(
                "HouseGenerator: HouseBuilder is not assigned."
            );
        }


        // ============================================================
        // DEBUG OUTPUT
        // ============================================================

        Debug.Log(
            "House generated. " +
            "Width=" + width +
            " Length=" + length +
            " Seed=" + seed +
            " Rooms=" + bspGenerator.GetRoomCount()
        );

        for (
            int i = 0;
            i < bspGenerator.GetRoomCount();
            i++)
        {
            /*
            Debug.Log(
                "Room " + i +
                ": X=" + bspGenerator.GetRoomX(i) +
                " Z=" + bspGenerator.GetRoomZ(i) +
                " Width=" + bspGenerator.GetRoomWidth(i) +
                " Length=" + bspGenerator.GetRoomLength(i)
            );
            */
        }
    }


    // ============================================================
    // CHANGE SEED AND GENERATE
    // ============================================================

    public void SetSeedAndGenerate(int newSeed)
    {
        RequestGeneration(
            width,
            length,
            newSeed
        );
    }


    // ============================================================
    // REGENERATE
    // ============================================================

    public void Regenerate()
    {
        RequestGeneration(
            width,
            length,
            seed
        );
    }


    // ============================================================
    // SETTINGS FROM CANVAS UI
    // ============================================================

    public void SetSettingsAndGenerate(
        int newWidth,
        int newLength,
        int newSeed)
    {
        RequestGeneration(
            newWidth,
            newLength,
            newSeed
        );
    }


    // ============================================================
    // REQUEST GENERATION
    // ============================================================

    private void RequestGeneration(
        int newWidth,
        int newLength,
        int newSeed)
    {
        // --------------------------------------------------------
        // If we already own the HouseGenerator, we can generate
        // immediately.
        // --------------------------------------------------------

        if (Networking.IsOwner(gameObject))
        {
            CommitGeneration(
                newWidth,
                newLength,
                newSeed
            );

            return;
        }


        // --------------------------------------------------------
        // We don't own it yet.
        //
        // Store the request locally.
        // --------------------------------------------------------

        pendingWidth = newWidth;
        pendingLength = newLength;
        pendingSeed = newSeed;

        hasPendingRequest = true;


        // --------------------------------------------------------
        // Request ownership.
        //
        // CommitGeneration() will happen from
        // OnOwnershipTransferred() after ownership actually
        // changes.
        // --------------------------------------------------------

        Networking.SetOwner(
            Networking.LocalPlayer,
            gameObject
        );

        Debug.Log(
            "HouseGenerator: Requested ownership " +
            "for a new generation."
        );
    }


    // ============================================================
    // WIDTH CLAMP
    // ============================================================

    private int ClampWidth(int value)
    {
        if (value < 1)
        {
            return 1;
        }

        if (value > 64)
        {
            return 64;
        }

        return value;
    }


    // ============================================================
    // LENGTH CLAMP
    // ============================================================

    private int ClampLength(int value)
    {
        if (value < 1)
        {
            return 1;
        }

        if (value > 64)
        {
            return 64;
        }

        return value;
    }
}

