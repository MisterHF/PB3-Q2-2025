using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OpenDoor : MonoBehaviour
{

    public TestGrid testGrid;
    public Building building;

    public direction _directions;
    public int roomId;

    // int = ID du batiment / Building = type du batiment
    Dictionary<int, Building> buildings = new Dictionary<int, Building>();
    List<Tuple<int, int>> openDoors = new List<Tuple<int, int>>();
    public List<GameObject> UpDoor = new List<GameObject>();
    public List<GameObject> DownDoor = new List<GameObject>();
    public List<GameObject> LeftDoor = new List<GameObject>();
    public List<GameObject> RightDoor = new List<GameObject>();

    public void OpenDoors(Building _PreviousBuilding)
    {
        //for (int i = 0; i < testGrid.width; i++)
        //{
        //    for (int j = 0; j < testGrid.height; j++)
        //    {
        //        Vector2 pos = new Vector2(i, j);

        //        for (int k = -1; k <= 1; k++)
        //        {
        //            for (int l = -1; l <= 1; l++)
        //            {
        //                Vector2 neighbor = new Vector2(pos.x + k, pos.y + l);
        //                if ((Mathf.Abs(k) != 1) ^ (Mathf.Abs(l) != 1))
        //                {

        //                }
        //            }
        //        }
        //    }
        //}

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

    private direction GetOppositeEnum(direction _Current)
    {
        switch (_Current)
        {
            case direction.Up:
                return direction.Down;
            case direction.Down:
                return direction.Up;
            case direction.Left:
                return direction.Right;
            case direction.Right:
                return direction.Left;
            default: return _Current;
        }
    }

    private void ForRightDir()
    {
        for (int i = 0; i < building.height; i++)
        {
            int xIndex = Mathf.FloorToInt((transform.position.x / 2) + building.width);
            int yIndex = Mathf.Abs(Mathf.FloorToInt((transform.position.z / 2) + i));

            int f = testGrid.myGrid[xIndex, yIndex];

            if (f == roomId)
            {
                Debug.Log($"{name} Open Door Right");
                RightDoor[i].SetActive(false);
                break;
            }
        }
    }



    private void ForLeftDir()
    {
        for (int i = 0; i < building.height; i++)
        {
            int xIndex = Mathf.FloorToInt((transform.position.x / 2) - 1);
            int yIndex = Mathf.Abs(Mathf.FloorToInt((transform.position.z / 2) + i));

            int f = testGrid.myGrid[xIndex, yIndex];

            if (f == roomId)
            {
                Debug.Log($"{name} Open Door Left");
                LeftDoor[i].SetActive(false);
                break;
            }
        }
    }
    private void ForDownDir()
    {

        //DownDoor = DownDoor.OrderBy(x => UnityEngine.Random.value).ToList();
        for (int i = 0; i < building.width; i++)
        {
            int xIndex = (int)((transform.position.x / 2 + i));
            int yIndex = Mathf.Abs(Mathf.CeilToInt((transform.position.z / 2 - building.height)));

            int f = testGrid.myGrid[xIndex, yIndex];

            Debug.Log($"{name} Checking Down at [{xIndex},{yIndex}] → {f} (RoomId: {roomId})");
            Debug.Log(yIndex);

            if (f == roomId)
            {
                Debug.Log($"{name} Open Door Down");
                DownDoor[i].SetActive(false);
                break;
            }
        }
    }


    private void ForUpDir()
    {
        //UpDoor = UpDoor.OrderBy(x => UnityEngine.Random.value).ToList();
        for (int i = 0; i < building.width; i++)
        {
            var f = new Vector2(transform.position.x + i, -building.height - 1);
            if (testGrid.myGrid[(int)(transform.position.x + i) / 2, Mathf.Abs((int)(transform.position.z + building.height - 1) / 2)] == roomId)
            {
                Debug.Log($"{name} Open Door Up");
                UpDoor[i].SetActive(false);
                break;
            }
        }
    }
}