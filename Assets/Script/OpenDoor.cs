using System.Collections.Generic;
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public TestGrid testGrid;
    public Building building;

    public direction _directions;
    public int roomId;

    public List<GameObject> UpDoor = new List<GameObject>();
    public List<GameObject> DownDoor = new List<GameObject>();
    public List<GameObject> LeftDoor = new List<GameObject>();
    public List<GameObject> RightDoor = new List<GameObject>();

    private const int tileSize = 2;

    private readonly HashSet<int> openedNeighborIds = new HashSet<int>();

    public void OpenDoors()
    {
        switch (_directions)
        {
            case direction.Up:
                ForUpDir();
                break;
            case direction.Down:
                ForDownDir();
                break;
            case direction.Left:
                ForLeftDir();
                break;
            case direction.Right:
                ForRightDir();
                break;
        }
    }

    public void ResetOpenedNeighbors()
    {
        openedNeighborIds.Clear();
    }

    private Vector2Int GetGridOrigin()
    {
        int originX = Mathf.RoundToInt(transform.position.x / tileSize);
        int originY = Mathf.RoundToInt(-transform.position.z / tileSize);
        return new Vector2Int(originX, originY);
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < testGrid.width && y < testGrid.height;
    }

    private void ForRightDir()
    {
        var origin = GetGridOrigin();
        int xCheck = origin.x + building.width;

        for (int i = 0; i < building.height; i++)
        {
            int y = origin.y + i;
            if (!InBounds(xCheck, y)) continue;
            int neighborId = testGrid.myGrid[xCheck, y];

            if (neighborId != 0 && neighborId != roomId && openedNeighborIds.Add(neighborId))
            {
                Debug.Log($"{name} Open Door Right -> room {neighborId} at [{xCheck},{y}]");
                if (i < RightDoor.Count && RightDoor[i] != null) RightDoor[i].SetActive(false);
            }
        }
    }

    private void ForLeftDir()
    {
        var origin = GetGridOrigin();
        int xCheck = origin.x - 1;

        for (int i = 0; i < building.height; i++)
        {
            int y = origin.y + i;
            if (!InBounds(xCheck, y)) continue;
            int neighborId = testGrid.myGrid[xCheck, y];

            if (neighborId != 0 && neighborId != roomId && openedNeighborIds.Add(neighborId))
            {
                Debug.Log($"{name} Open Door Left -> room {neighborId} at [{xCheck},{y}]");
                if (i < LeftDoor.Count && LeftDoor[i] != null) LeftDoor[i].SetActive(false);
            }
        }
    }

    private void ForDownDir()
    {
        var origin = GetGridOrigin();
        int yCheck = origin.y + building.height;

        for (int i = 0; i < building.width; i++)
        {
            int x = origin.x + i;
            if (!InBounds(x, yCheck)) continue;
            int neighborId = testGrid.myGrid[x, yCheck];

            if (neighborId != 0 && neighborId != roomId && openedNeighborIds.Add(neighborId))
            {
                Debug.Log($"{name} Open Door Down -> room {neighborId} at [{x},{yCheck}]");
                if (i < DownDoor.Count && DownDoor[i] != null) DownDoor[i].SetActive(false);
            }
        }
    }

    private void ForUpDir()
    {
        var origin = GetGridOrigin();
        int yCheck = origin.y - 1;

        for (int i = 0; i < building.width; i++)
        {
            int x = origin.x + i;
            if (!InBounds(x, yCheck)) continue;
            int neighborId = testGrid.myGrid[x, yCheck];

            if (neighborId != 0 && neighborId != roomId && openedNeighborIds.Add(neighborId))
            {
                Debug.Log($"{name} Open Door Up -> room {neighborId} at [{x},{yCheck}]");
                if (i < UpDoor.Count && UpDoor[i] != null) UpDoor[i].SetActive(false);
            }
        }
    }
}
