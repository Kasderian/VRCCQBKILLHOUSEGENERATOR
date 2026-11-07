
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;

public class HouseBuilder : UdonSharpBehaviour
{
    [Header("References")]
    public BSPGenerator bspGenerator;
    public GameObject generatedHouse;
    public RoomConnector roomConnector;
    public ExteriorDoorGenerator exteriorDoorGenerator;
    public ExteriorWindowGenerator exteriorWindowGenerator;

    [Header("Prefabs")]
    public GameObject simpleWallPrefab;
    public GameObject windowPrefab;

    [Header("Door Pool")]
    // Empty object (e.g. "Doors") whose direct children are the
    // pre-placed door prefab instances, already inactive in the scene.
    // These must exist before runtime so every client shares the same
    // network identity for each door - do NOT instantiate doors.
    public Transform doorPoolParent;

    [Header("Wall Settings")]
    public float wallCenterY = 0f;

    [Header("Limits")]
    public int maxWallSegments = 8192;

    // Stored wall positions.
    // Used to prevent duplicate walls where two rooms share a wall.
    private int[] wallX;
    private int[] wallZ;
    private int[] wallDirection;
    private int wallCount;

    // Stored door positions.
    private int[] doorX;
    private int[] doorZ;
    private int[] doorDirection;
    private int[] doorRotation;
    private int doorCount;
    // Stored exterior door positions.
    private int[] exteriorDoorX;
    private int[] exteriorDoorZ;
    private int[] exteriorDoorDirection;
    private int[] exteriorDoorRotation;
    private int exteriorDoorCount;

    // Stored exterior window positions.
    private int[] windowX;
    private int[] windowZ;
    private int[] windowDirection;
    private int windowCount;

    // Door pool state.
    // doorPool holds every pre-placed door object under doorPoolParent.
    // doorPoolNextIndex tracks how many of them are in use for the
    // generation currently being built; it resets to 0 in
    // ClearGeneratedHouse. Since BSPGenerator/RoomConnector/
    // ExteriorDoorGenerator are all deterministic and synced, every
    // client walks rooms and walls in the same order, so they all
    // assign the same pool index to the same logical door without
    // needing to agree on anything over the network.
    private GameObject[] doorPool;
    private int doorPoolCount;
    private int doorPoolNextIndex;
    private int lastDoorPoolIndex;

    // The Handle's authored closed-position local transform (relative
    // to DoorHinge), captured once at startup before anything has ever
    // been opened. Swinging a hinge door changes the Handle's local
    // POSITION as well as its rotation (it arcs around the hinge
    // anchor, which usually isn't at Handle's own origin) - so we need
    // to restore both, not just rotation, when a pooled door is reused.
    private Vector3[] doorRestLocalPosition;
    private Quaternion[] doorRestLocalRotation;

    private void Start()
    {
        InitializeDoorPool();
    }

    private void InitializeDoorPool()
    {
        if (doorPoolParent == null)
        {
            Debug.LogError("HouseBuilder: Door Pool Parent is not assigned.");
            doorPool = new GameObject[0];
            doorPoolCount = 0;
            return;
        }

        doorPoolCount = doorPoolParent.childCount;
        doorPool = new GameObject[doorPoolCount];
        doorRestLocalPosition = new Vector3[doorPoolCount];
        doorRestLocalRotation = new Quaternion[doorPoolCount];

        for (int i = 0; i < doorPoolCount; i++)
        {
            GameObject door = doorPoolParent.GetChild(i).gameObject;
            doorPool[i] = door;

            Rigidbody rb = door.GetComponentInChildren<Rigidbody>();

            if (rb != null)
            {
                doorRestLocalPosition[i] = rb.transform.localPosition;
                doorRestLocalRotation[i] = rb.transform.localRotation;
            }
            else
            {
                Debug.LogWarning(
                    "HouseBuilder: Door pool entry " +
                    i +
                    " has no Rigidbody in its children; " +
                    "its rest pose could not be captured."
                );
            }
        }

        if (doorPoolCount == 0)
        {
            Debug.LogWarning(
                "HouseBuilder: Door Pool Parent has no pre-placed doors."
            );
        }
    }

    public void BuildHouse()
    {
        if (bspGenerator == null)
        {
            Debug.LogError("HouseBuilder: BSPGenerator is not assigned.");
            return;
        }

        if (simpleWallPrefab == null)
        {
            Debug.LogError("HouseBuilder: Simple Wall Prefab is not assigned.");
            return;
        }

        if (generatedHouse == null)
        {
            Debug.LogError("HouseBuilder: Generated House is not assigned.");
            return;
        }

        if (doorPool == null)
        {
            InitializeDoorPool();
        }

        ClearGeneratedHouse();

        wallX = new int[maxWallSegments];
        wallZ = new int[maxWallSegments];
        wallDirection = new int[maxWallSegments];
        wallCount = 0;

        // -----------------------------------
        // INTERIOR DOORS
        // -----------------------------------

        doorCount = 0;

        if (roomConnector != null)
        {
            int connectionCount =
                roomConnector.GetConnectionCount();

            doorX = new int[connectionCount];
            doorZ = new int[connectionCount];
            doorDirection = new int[connectionCount];
            doorRotation = new int[connectionCount];

            for (int i = 0; i < connectionCount; i++)
            {
                doorX[i] =
                    roomConnector.GetConnectionX(i);

                doorZ[i] =
                    roomConnector.GetConnectionZ(i);

                doorDirection[i] =
                    roomConnector.GetConnectionDirection(i);

                doorRotation[i] =
                    roomConnector.GetConnectionRotation(i);

                doorCount++;
            }
        }

        // -----------------------------------
        // EXTERIOR DOORS
        // -----------------------------------

        exteriorDoorCount = 0;

        if (exteriorDoorGenerator != null)
        {
            int count =
                exteriorDoorGenerator.GetDoorCount();

            exteriorDoorX = new int[count];
            exteriorDoorZ = new int[count];
            exteriorDoorDirection = new int[count];
            exteriorDoorRotation = new int[count];

            for (int i = 0; i < count; i++)
            {
                exteriorDoorX[i] =
                    exteriorDoorGenerator.GetDoorX(i);

                exteriorDoorZ[i] =
                    exteriorDoorGenerator.GetDoorZ(i);

                exteriorDoorDirection[i] =
                    exteriorDoorGenerator.GetDoorDirection(i);

                exteriorDoorRotation[i] = exteriorDoorGenerator.GetDoorRotation(i);

                exteriorDoorCount++;
            }
        }
        windowCount = 0;

        if (exteriorWindowGenerator != null)
        {
            int count = exteriorWindowGenerator.GetWindowCount();

            windowX = new int[count];
            windowZ = new int[count];
            windowDirection = new int[count];

            for (int i = 0; i < count; i++)
            {
                windowX[i] =
                    exteriorWindowGenerator.GetWindowX(i);

                windowZ[i] =
                    exteriorWindowGenerator.GetWindowZ(i);

                windowDirection[i] =
                    exteriorWindowGenerator.GetWindowDirection(i);

                windowCount++;
            }
        }
        // -----------------------------------
        // BUILD ROOMS
        // -----------------------------------

        int roomCount =
            bspGenerator.GetRoomCount();

        for (int i = 0; i < roomCount; i++)
        {
            int x =
                bspGenerator.GetRoomX(i);

            int z =
                bspGenerator.GetRoomZ(i);

            int width =
                bspGenerator.GetRoomWidth(i);

            int length =
                bspGenerator.GetRoomLength(i);

            BuildRoomWalls(
                x,
                z,
                width,
                length
            );
        }

        Debug.Log(
            "HouseBuilder: Built " +
            wallCount +
            " wall segments, used " +
            doorPoolNextIndex +
            "/" +
            doorPoolCount +
            " doors."
        );
    }

    private void BuildRoomWalls(
        int x,
        int z,
        int width,
        int length)
    {
        // SOUTH WALL
        for (int i = 0; i < width; i++)
        {
            AddWall(
                x + i,
                z,
                0
            );
        }

        // NORTH WALL
        for (int i = 0; i < width; i++)
        {
            AddWall(
                x + i,
                z + length,
                0
            );
        }

        // WEST WALL
        for (int i = 0; i < length; i++)
        {
            AddWall(
                x,
                z + i,
                1
            );
        }

        // EAST WALL
        for (int i = 0; i < length; i++)
        {
            AddWall(
                x + width,
                z + i,
                1
            );
        }
    }

    private void AddWall(
        int x,
        int z,
        int direction)
    {
        // Don't create the same wall segment twice.
        if (WallAlreadyExists(x, z, direction))
        {
            return;
        }

        if (wallCount >= maxWallSegments)
        {
            Debug.LogWarning(
                "HouseBuilder: Maximum wall segment limit reached."
            );

            return;
        }

        wallX[wallCount] = x;
        wallZ[wallCount] = z;
        wallDirection[wallCount] = direction;

        if (DoorAlreadyExists(x, z, direction))
        {
            CreateDoor(
                x,
                z,
                direction,
                GetDoorRotation(x, z, direction)
            );
        }
        else if (ExteriorDoorAlreadyExists(x, z, direction))
        {
            CreateDoor(
                x,
                z,
                direction,
                GetExteriorDoorRotation(x, z, direction)
            );
        }
        else if (WindowAlreadyExists(x, z, direction))
        {
            CreateWindow(x, z, direction);
        }
        else
        {
            CreateWall(x, z, direction);
        }

        wallCount++;
    }

    private bool WallAlreadyExists(
        int x,
        int z,
        int direction)
    {
        for (int i = 0; i < wallCount; i++)
        {
            if (wallX[i] == x &&
                wallZ[i] == z &&
                wallDirection[i] == direction)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateWall(
        int x,
        int z,
        int direction)
    {
        GameObject wall = Instantiate(simpleWallPrefab);

        if (wall == null)
        {
            Debug.LogError(
                "HouseBuilder: Failed to instantiate wall."
            );
            return;
        }
        wall.transform.SetParent(generatedHouse.transform);

        // Horizontal wall.
        // The prefab's 1m length runs along X.
        if (direction == 0)
        {
            wall.transform.localPosition = new Vector3(
                x + 0.5f,
                wallCenterY,
                z
            );

            wall.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                0f
            );
        }
        // Vertical wall.
        // Rotate the prefab 90 degrees so its
        // 1m length runs along Z.
        else
        {
            wall.transform.localPosition = new Vector3(
                x,
                wallCenterY,
                z + 0.5f
            );

            wall.transform.localRotation = Quaternion.Euler(
                0f,
                90f,
                0f
            );
        }

        // Parent first, then use LOCAL position/rotation.
        // This makes GeneratedHouse the coordinate origin.
        wall.transform.SetParent(
            generatedHouse.transform
        );
    }

    private void ClearGeneratedHouse()
    {
        int childCount =
            generatedHouse.transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            GameObject child =
                generatedHouse.transform
                    .GetChild(i)
                    .gameObject;

            child.SetActive(false);
        }

        ReturnAllDoorsToPool();
    }

    // Deactivates every pooled door and resets the "in use" counter so
    // the next BuildHouse() call starts handing doors out from index 0
    // again. Doors themselves stay parented under doorPoolParent the
    // whole time - they are never reparented, since Object Sync tracks
    // network identity on the object itself, not on where it happens
    // to sit at any given moment.
    private void ReturnAllDoorsToPool()
    {
        if (doorPool == null)
        {
            InitializeDoorPool();
        }

        for (int i = 0; i < doorPoolCount; i++)
        {
            if (doorPool[i] != null)
            {
                doorPool[i].SetActive(false);
            }
        }

        doorPoolNextIndex = 0;
    }

    private bool DoorAlreadyExists(
    int x,
    int z,
    int direction)
    {
        for (int i = 0; i < doorCount; i++)
        {
            if (doorX[i] == x &&
                doorZ[i] == z &&
                doorDirection[i] == direction)
            {
                return true;
            }
        }

        return false;
    }

    private int GetDoorRotation(
    int x,
    int z,
    int direction)
    {
        for (int i = 0; i < doorCount; i++)
        {
            if (
                doorX[i] == x &&
                doorZ[i] == z &&
                doorDirection[i] == direction)
            {
                return doorRotation[i];
            }
        }

        return 0;
    }

    private bool ExteriorDoorAlreadyExists(
    int x,
    int z,
    int direction)
    {
        for (int i = 0; i < exteriorDoorCount; i++)
        {
            if (exteriorDoorX[i] == x &&
                exteriorDoorZ[i] == z &&
                exteriorDoorDirection[i] == direction)
            {
                return true;
            }
        }

        return false;
    }
    private int GetExteriorDoorRotation(
        int x,
        int z,
        int direction)
    {
        for (int i = 0; i < exteriorDoorCount; i++)
        {
            if (
                exteriorDoorX[i] == x &&
                exteriorDoorZ[i] == z &&
                exteriorDoorDirection[i] == direction)
            {
                return exteriorDoorRotation[i];
            }
        }

        return 0;
    }
    private bool WindowAlreadyExists(
    int x,
    int z,
    int direction)
    {
        for (int i = 0; i < windowCount; i++)
        {
            if (
                windowX[i] == x &&
                windowZ[i] == z &&
                windowDirection[i] == direction)
            {
                return true;
            }
        }

        return false;
    }

    // Grabs the next unused door from the pool. Because every client
    // reaches this method for the same logical door at the same point
    // in the same deterministic build order, they all pull the same
    // pool index - no network message needed to agree on "which door".
    private GameObject GetNextPooledDoor()
    {
        if (doorPool == null)
        {
            InitializeDoorPool();
        }

        if (doorPoolNextIndex >= doorPoolCount)
        {
            Debug.LogWarning(
                "HouseBuilder: Door pool exhausted (" +
                doorPoolCount +
                " doors available). Add more doors under " +
                "the Door Pool Parent."
            );

            return null;
        }

        lastDoorPoolIndex = doorPoolNextIndex;

        GameObject door = doorPool[doorPoolNextIndex];
        doorPoolNextIndex++;

        return door;
    }

    private void CreateDoor(
     int x,
     int z,
     int direction,
     int rotation)
    {
        GameObject door = GetNextPooledDoor();

        if (door == null)
        {
            return;
        }

        int poolIndex = lastDoorPoolIndex;

        Vector3 localPosition;

        if (direction == 0)
        {
            localPosition = new Vector3(
                x + 0.5f,
                wallCenterY,
                z
            );
        }
        else
        {
            localPosition = new Vector3(
                x,
                wallCenterY,
                z + 0.5f
            );
        }

        Quaternion localRotation = Quaternion.Euler(
            0f,
            rotation,
            0f
        );

        PlacePooledDoor(door, poolIndex, localPosition, localRotation);
    }

    // Moves a pooled door into place and (re)activates it.
    //
    // IMPORTANT ORDER OF OPERATIONS:
    // The door's Handle has a non-kinematic Rigidbody + HingeJoint.
    // While a Rigidbody is inactive, physics doesn't control it, so its
    // transform still follows its parent normally. Once active, physics
    // takes over and it stops following parent transform changes. So we
    // reset the hinge and reposition the whole door object WHILE IT IS
    // STILL INACTIVE, and only call SetActive(true) at the very end.
    // Doing this in the other order can leave the door leaf visually
    // detached from the frame after a reused door is moved.
    private void PlacePooledDoor(
        GameObject door,
        int poolIndex,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (door == null)
        {
            return;
        }

        if (generatedHouse == null)
        {
            Debug.LogError("HouseBuilder: Generated House is not assigned.");
            return;
        }

        // Make sure it's inactive before we touch its transform/physics
        // state, regardless of whether it was previously in use.
        door.SetActive(false);

        ResetDoorHinge(door, poolIndex);

        // Convert from GeneratedHouse-local space (what the rest of the
        // build logic works in) to world space, since the door pool is
        // not parented under GeneratedHouse and is never reparented.
        Vector3 worldPosition =
            generatedHouse.transform.TransformPoint(localPosition);

        Quaternion worldRotation =
            generatedHouse.transform.rotation * localRotation;

        door.transform.SetPositionAndRotation(worldPosition, worldRotation);

        // Recompute the hinge anchor now that the door is at its final
        // position, same as the original instantiate-based logic did.
        Rigidbody rb = door.GetComponentInChildren<Rigidbody>();
        HingeJoint hinge = door.GetComponentInChildren<HingeJoint>();

        if (rb != null && hinge != null)
        {
            Vector3 worldHingePosition =
                rb.transform.TransformPoint(hinge.anchor);

            hinge.autoConfigureConnectedAnchor = false;
            hinge.connectedAnchor = worldHingePosition;

            Physics.SyncTransforms();
        }
        else
        {
            if (rb == null)
            {
                Debug.LogWarning(
                    "HouseBuilder: Door Rigidbody not found."
                );
            }

            if (hinge == null)
            {
                Debug.LogWarning(
                    "HouseBuilder: Door Hinge Joint not found."
                );
            }
        }

        door.SetActive(true);

        // Whoever locally builds this generation becomes the door's
        // owner, since our deterministic reset/placement is what should
        // be authoritative right now.
        if (!Networking.IsOwner(Networking.LocalPlayer, door))
        {
            Networking.SetOwner(Networking.LocalPlayer, door);
        }

        // Tell Object Sync this was an intentional teleport, not drift,
        // so remote clients snap to the new spot instead of
        // interpolating a slide from the door's previous location.
        VRCObjectSync objectSync = door.GetComponentInChildren<VRCObjectSync>();

        if (objectSync != null)
        {
            objectSync.FlagDiscontinuity();
        }
    }

    // Resets the door leaf back to its ORIGINAL closed rest pose
    // (position AND rotation, both relative to DoorHinge) and clears
    // any physics motion left over from a previous generation, before
    // the door gets repositioned/reactivated.
    //
    // Swinging a hinge door changes the Handle's local position, not
    // just its rotation, because it physically arcs around the hinge
    // anchor rather than spinning in place. Resetting rotation alone
    // leaves a door that LOOKS closed (0 degrees) but sits wherever it
    // last drifted to mid-swing - which is the bug this fixes.
    private void ResetDoorHinge(GameObject door, int poolIndex)
    {
        Rigidbody rb = door.GetComponentInChildren<Rigidbody>();

        if (rb == null)
        {
            return;
        }

        // NOTE: if your VRChat/Unity version has migrated to
        // Rigidbody.linearVelocity, swap these two lines accordingly.
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (doorRestLocalPosition != null &&
            poolIndex >= 0 &&
            poolIndex < doorRestLocalPosition.Length)
        {
            rb.transform.localPosition = doorRestLocalPosition[poolIndex];
            rb.transform.localRotation = doorRestLocalRotation[poolIndex];
        }
        else
        {
            // Fallback if the rest pose wasn't captured for some
            // reason - only resets rotation, so this case can still
            // reproduce the position bug. Should not normally happen.
            rb.transform.localRotation = Quaternion.identity;
        }
    }

    private void CreateWindow(
        int x,
        int z,
        int direction)
    {
        if (windowPrefab == null)
        {
            Debug.LogError(
                "HouseBuilder: Window Prefab is not assigned."
            );
            return;
        }

        if (generatedHouse == null)
        {
            Debug.LogError(
                "HouseBuilder: Generated House is not assigned."
            );
            return;
        }

        GameObject window =
            Instantiate(windowPrefab);

        if (window == null)
        {
            Debug.LogError(
                "HouseBuilder: Failed to instantiate window."
            );
            return;
        }

        Vector3 localPosition;
        Quaternion localRotation;

        if (direction == 0)
        {
            localPosition = new Vector3(
                x + 0.5f,
                wallCenterY,
                z
            );

            localRotation =
                Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            localPosition = new Vector3(
                x,
                wallCenterY,
                z + 0.5f
            );

            localRotation =
                Quaternion.Euler(0f, 90f, 0f);
        }

        window.transform.SetParent(
            generatedHouse.transform,
            false
        );

        window.transform.localPosition =
            localPosition;

        window.transform.localRotation =
            localRotation;
    }
}







