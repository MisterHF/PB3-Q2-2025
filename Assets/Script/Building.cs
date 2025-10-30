using System.Collections.Generic;
using UnityEngine;

public class Building
{

    public int width;
    public int height;

    public int[,] myBuildingGrid;

    public GameObject prefab;

    public Building(int width, int height, GameObject prefab) 
    {
        this.width = width;
        this.height = height;
        this.prefab = prefab;
        myBuildingGrid = new int[width, height];
        SetFullGrid();
    }


    public void SetFullGrid()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                myBuildingGrid[j, i] = TestGrid.one ;
            }
        }
    }
}
