using UdonSharp;
using UnityEngine;

public class ExteriorDoorGenerator : UdonSharpBehaviour
{
    [Header("References")]
    public BSPGenerator bspGenerator;

    [Header("Door Settings")]
    public int doorLength = 1;

    [Header("Limits")]
    public int maxExteriorDoors = 4;

    [Header("Debug")]
    public bool logExteriorDoor = true;

    private int[] doorX;
    private int[] doorZ;
    private int[] doorDirection;
    private int[] doorRotation;
    private int doorCount;

    private uint randomState;

    public int GetDoorCount()
    {
        return doorCount;
    }

    public int GetDoorX(int index)
    {
        return doorX[index];
    }

    public int GetDoorZ(int index)
    {
        return doorZ[index];
    }

    public int GetDoorDirection(int index)
    {
        return doorDirection[index];
    }
    public int GetDoorRotation(int index)
    {
        return doorRotation[index];
    }

    public void GenerateExteriorDoors(int seed)
    {
        doorCount = 0;

        doorX = new int[maxExteriorDoors];
        doorZ = new int[maxExteriorDoors];
        doorDirection = new int[maxExteriorDoors];
        doorRotation = new int[maxExteriorDoors];

        if (bspGenerator == null)
        {
            Debug.LogError(
                "ExteriorDoorGenerator: BSPGenerator is not assigned."
            );
            return;
        }

        randomState = (uint)seed;

        if (randomState == 0)
            randomState = 1;

        int roomCount = bspGenerator.GetRoomCount();

        if (roomCount <= 0)
        {
            Debug.LogWarning(
                "ExteriorDoorGenerator: No rooms available."
            );
            return;
        }

        // Find every valid 1m position along the SOUTH
        // exterior wall.
        int[] possibleX = new int[roomCount * 64];
        int possibleCount = 0;

        for (int room = 0; room < roomCount; room++)
        {
            int x = bspGenerator.GetRoomX(room);
            int z = bspGenerator.GetRoomZ(room);
            int width = bspGenerator.GetRoomWidth(room);

            // Room must touch the south edge of the house.
            if (z != 0)
                continue;

            if (width < doorLength)
                continue;

            int maxStart = x + width - doorLength;

            for (int doorXPosition = x;
                 doorXPosition <= maxStart;
                 doorXPosition++)
            {
                if (possibleCount >= possibleX.Length)
                    break;

                possibleX[possibleCount] = doorXPosition;
                possibleCount++;
            }
        }

        if (possibleCount <= 0)
        {
            Debug.LogWarning(
                "ExteriorDoorGenerator: " +
                "Could not find a valid south-facing exterior door."
            );
            return;
        }

        // Pick one position using the seed.
        int selectedIndex =
            RandomRange(0, possibleCount);

        int selectedX =
            possibleX[selectedIndex];

        doorX[0] = selectedX;
        doorZ[0] = 0;
        doorDirection[0] = 0;
        // Width-wise exterior door: 0° or 180°
        doorRotation[0] =
            RandomRange(0, 2) * 180;

        doorCount = 1;

        if (logExteriorDoor)
        {
            Debug.Log(
                "ExteriorDoorGenerator: " +
                "Created south entrance at X=" +
                selectedX +
                " Z=0" +
                " Rotation=" +
                doorRotation[0] +
                "°"
            );
        }
    }

    private int RandomRange(
        int minInclusive,
        int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        randomState ^= randomState << 13;
        randomState ^= randomState >> 17;
        randomState ^= randomState << 5;

        int stateAsInt =
            (int)(randomState & 0x7FFFFFFF);

        int range =
            maxExclusive - minInclusive;

        return minInclusive +
            (stateAsInt % range);
    }
}