using System.Collections.Generic;
using UnityEngine;

public class TestGrid : MonoBehaviour
{

    public static readonly int zero = 0;
    public static int one = 1;
    public static readonly int two = 2;
    public int width;
    public int height;

    public int[,] myGrid;
    public int mapSeed;
    void Start()
    {
        myGrid = new int[width,height];
        InitGrid();
        PrintBool();
        BuildManager building = new BuildManager(this, mapSeed);
        PrintBool();
    }


    void PrintBool()
    {
        string s = "";
        Dictionary<int, string> colorMap = new Dictionary<int, string>();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int value = myGrid[j, i];

                if (!colorMap.ContainsKey(value))
                {
                    colorMap[value] = GetRandomColorHex();
                }

                if (value != 0)
                    s += $"<color={colorMap[value]}>{value}</color>";
                else
                    s += "0";
            }
            s += "\n";
        }

        Debug.Log(s);
    }

    string GetRandomColorHex()
    {
        Color color = new Color(Random.value, Random.value, Random.value);
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }

    void InitGrid()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                myGrid[j, i] = zero;
            }
        }
    }
}
 