using UnityEngine;

public class TestGrid : MonoBehaviour
{

    public static readonly int zero = 0;
    public static int one = 1;
    public static readonly int two = 2;
    public int width;
    public int height;

    public int[,] myGrid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myGrid = new int[width,height];
        InitGrid();
        PrintBool();
        BuildManager building = new BuildManager(this);
        PrintBool();
    }


    void PrintBool()
    {
        string s = ("");
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                s += myGrid[j, i];
            }
            s += "\n";
        }
        print(s);
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
