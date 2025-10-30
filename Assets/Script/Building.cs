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
        SetFullGrid(width, height);
    }


    public void SetFullGrid(int w, int h)
    {
        myBuildingGrid = new int[w, h];
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                    myBuildingGrid[j, i] = TestGrid.one;
            }
        }
    }
}
