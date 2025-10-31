using System.Collections.Generic;
using UnityEngine;

public class Building
{

    public int width;
    public int height;

    public int[,] myBuildingGrid;

    public GameObject prefab;
    public bool isL;
    public Building(int width, int height, GameObject prefab, bool isL = false)
    {
        this.width = width;
        this.height = height;
        this.prefab = prefab;
        this.isL = isL;
        if (isL)
        {
            SetLShape();
            return;
        }
        SetFullGrid(width, height);
    }

    public void SetLShape()
    {
        myBuildingGrid = new int[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                myBuildingGrid[x, y] = 0;
            }
        }

        for (int x = 0; x < width; x++)
        {
            myBuildingGrid[x, 0] = TestGrid.one;
        }

        if (height > 1)
        {
            myBuildingGrid[0, 1] = TestGrid.one;
        }
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
