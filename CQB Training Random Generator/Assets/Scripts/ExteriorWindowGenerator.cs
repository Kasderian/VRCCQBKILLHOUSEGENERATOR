
using UdonSharp;
using UnityEngine;

public class ExteriorWindowGenerator : UdonSharpBehaviour
{
    [Header("References")]
    public BSPGenerator bspGenerator;
    public ExteriorDoorGenerator exteriorDoorGenerator;

    [Header("Window Settings")]
    public int windowLength = 1;

    [Header("Limits")]
    public int maxExteriorWindows = 8;

    [Header("Debug")]
    public bool logExteriorWindows = true;

    private int[] windowX;
    private int[] windowZ;
    private int[] windowDirection;
    private int windowCount;

    private uint randomState;

    public int GetWindowCount()
    {
        return windowCount;
    }

    public int GetWindowX(int index)
    {
        return windowX[index];
    }

    public int GetWindowZ(int index)
    {
        return windowZ[index];
    }

    public int GetWindowDirection(int index)
    {
        return windowDirection[index];
    }

    public void GenerateExteriorWindows(
        int houseWidth,
        int houseLength,
        int seed)
    {
        windowCount = 0;

        windowX = new int[maxExteriorWindows];
        windowZ = new int[maxExteriorWindows];
        windowDirection = new int[maxExteriorWindows];

        if (bspGenerator == null)
        {
            Debug.LogError(
                "ExteriorWindowGenerator: BSPGenerator is not assigned."
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
                "ExteriorWindowGenerator: No rooms available."
            );
            return;
        }

        int possibleMax = roomCount * 256;

        int[] possibleX = new int[possibleMax];
        int[] possibleZ = new int[possibleMax];
        int[] possibleDirection = new int[possibleMax];

        int possibleCount = 0;

        for (int room = 0; room < roomCount; room++)
        {
            int x = bspGenerator.GetRoomX(room);
            int z = bspGenerator.GetRoomZ(room);
            int width = bspGenerator.GetRoomWidth(room);
            int length = bspGenerator.GetRoomLength(room);

            // SOUTH
            if (z == 0 && width >= windowLength)
            {
                for (
                    int i = 0;
                    i <= width - windowLength;
                    i++)
                {
                    if (possibleCount >= possibleMax)
                        break;

                    int windowXPosition = x + i;

                    if (IsExteriorDoor(
                        windowXPosition,
                        z,
                        0))
                    {
                        continue;
                    }

                    possibleX[possibleCount] =
                        windowXPosition;

                    possibleZ[possibleCount] =
                        z;

                    possibleDirection[possibleCount] =
                        0;

                    possibleCount++;
                }
            }

            // NORTH
            if (z + length == houseLength)
            {
                if (width >= windowLength)
                {
                    for (
                        int i = 0;
                        i <= width - windowLength;
                        i++)
                    {
                        if (possibleCount >= possibleMax)
                            break;

                        int windowXPosition = x + i;

                        possibleX[possibleCount] =
                            windowXPosition;

                        possibleZ[possibleCount] =
                            z + length;

                        possibleDirection[possibleCount] =
                            0;

                        possibleCount++;
                    }
                }
            }

            // WEST
            if (x == 0 && length >= windowLength)
            {
                for (
                    int i = 0;
                    i <= length - windowLength;
                    i++)
                {
                    if (possibleCount >= possibleMax)
                        break;

                    int windowZPosition = z + i;

                    if (IsExteriorDoor(
                        x,
                        windowZPosition,
                        1))
                    {
                        continue;
                    }

                    possibleX[possibleCount] =
                        x;

                    possibleZ[possibleCount] =
                        windowZPosition;

                    possibleDirection[possibleCount] =
                        1;

                    possibleCount++;
                }
            }

            // EAST
            if (x + width == houseWidth)
            {
                if (length >= windowLength)
                {
                    for (
                        int i = 0;
                        i <= length - windowLength;
                        i++)
                    {
                        if (possibleCount >= possibleMax)
                            break;

                        int windowZPosition = z + i;

                        possibleX[possibleCount] =
                            x + width;

                        possibleZ[possibleCount] =
                            windowZPosition;

                        possibleDirection[possibleCount] =
                            1;

                        possibleCount++;
                    }
                }
            }
        }

        if (possibleCount <= 0)
        {
            Debug.LogWarning(
                "ExteriorWindowGenerator: " +
                "Could not find any valid exterior window positions."
            );
            return;
        }

        // Randomly select windows.
        while (
            windowCount < maxExteriorWindows &&
            possibleCount > 0)
        {
            int selectedIndex =
                RandomRange(0, possibleCount);

            windowX[windowCount] =
                possibleX[selectedIndex];

            windowZ[windowCount] =
                possibleZ[selectedIndex];

            windowDirection[windowCount] =
                possibleDirection[selectedIndex];

            windowCount++;

            // Remove the selected position so that
            // the exact same position cannot be selected again.
            possibleCount--;

            possibleX[selectedIndex] =
                possibleX[possibleCount];

            possibleZ[selectedIndex] =
                possibleZ[possibleCount];

            possibleDirection[selectedIndex] =
                possibleDirection[possibleCount];
        }

        if (logExteriorWindows)
        {
            //Debug.Log("ExteriorWindowGenerator: Created " + windowCount + " exterior windows.");

            for (int i = 0; i < windowCount; i++)
            {
                /*
                Debug.Log(
                    "Window " + i +
                    ": X=" +
                    windowX[i] +
                    " Z=" +
                    windowZ[i] +
                    " Direction=" +
                    windowDirection[i]
                );
                */
            }
        }
    }

    private bool IsExteriorDoor(
        int x,
        int z,
        int direction)
    {
        if (exteriorDoorGenerator == null)
            return false;

        int doorCount =
            exteriorDoorGenerator.GetDoorCount();

        for (int i = 0; i < doorCount; i++)
        {
            if (
                exteriorDoorGenerator.GetDoorX(i) == x &&
                exteriorDoorGenerator.GetDoorZ(i) == z &&
                exteriorDoorGenerator.GetDoorDirection(i) == direction)
            {
                return true;
            }
        }

        return false;
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

