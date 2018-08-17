using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public struct FLOOR_CELL
{
    public int cellInfo; //high 28bit for indexHeight; low 4bit for groundType

    public int indexHeight { get { return cellInfo >> 4; } set { cellInfo |= (value << 4); } }
    public int groundType { get { return cellInfo & 0xf; } set { cellInfo |= value; } }
}

public struct SCENE_FLOOR_CELL_CONFIG
{
    public float fCellSize;
    public float fMinX;
    public float fMinZ;
    public float fBoundMin;
    public float fBoundMax;
    public int nWidth;
    public int nHeight;

    public SCENE_FLOOR_CELL_CONFIG(float cellSize, float minX, float minZ, float boundMin, float boundMax, int width, int height)
    {
        fCellSize = cellSize;
        fMinX = minX;
        fMinZ = minZ;
        fBoundMin = boundMin;
        fBoundMax = boundMax;
        nWidth = width;
        nHeight = height;
    }
}

public struct Vector3
{
    public float x;
    public float y;
    public float z;
}

public class FloorCells
{
    private bool useHashTable = false;
    public bool UseHashTable
    {
        get { return useHashTable; }
        set { useHashTable = value; }
    }

    private SCENE_FLOOR_CELL_CONFIG config;
    public SCENE_FLOOR_CELL_CONFIG Config
    {
        get { return config; }
        set { config = value; }
    }

    private Dictionary<int, FLOOR_CELL> cellsDictionary = null; //index + uv
    private Hashtable cellsHashTable = null; //for performance test

    private float[] heightsArray = null; //stripped height, runtime

    public FloorCells() { }

    public bool GetFloor(Vector3 pos, ref float height, ref int groundType)
    {
        if (useHashTable)
        {
            if (cellsHashTable == null)
                return false;
        }
        else
        {
            if (cellsDictionary == null)
                return false;
        }

        if (pos.x < Config.fMinX || pos.z < Config.fMinZ)
            return false;

        var w = (int)((pos.x - Config.fMinX) / Config.fCellSize);
        var h = (int)((pos.z - Config.fMinZ) / Config.fCellSize);
        if (w >= Config.nWidth || h >= Config.nHeight)
            return false;

        FLOOR_CELL cell;
        var key = w * Config.nHeight + h;

        if (useHashTable)
        {
            if (!cellsHashTable.ContainsKey(key))
                return false;

            cell = (FLOOR_CELL)cellsHashTable[key];
        }
        else
        {
            if (!cellsDictionary.ContainsKey(key))
                return false;

            cell = cellsDictionary[key];
        }

        height = heightsArray[cell.indexHeight];
        groundType = cell.groundType;
        return true;
    }

    static BinaryFormatter DataFormatter = new BinaryFormatter();
    static readonly string ms_fileExtension = ".cell";
    static readonly string ms_byteExtension = ".bytes";

    public static ushort ToStrippedHeight(float fHeight, float fBoundMin, float fBoundMax)
    {
        return (ushort)((fHeight - fBoundMin) * 65535 / (fBoundMax - fBoundMin));
    }

    public static float ToFloatHeight(ushort height, float fBoundMin, float fBoundMax)
    {
        return height * (fBoundMax - fBoundMin) / 65535 + fBoundMin;
    }

    public static FloorCells LoadCellFile(string name)
    {
        FloorCells floorCells = new FloorCells();

        var path = Path.Combine(name + ms_fileExtension + ms_byteExtension);
        using (BinaryReader br = new BinaryReader(File.OpenRead(path)))
        {
            floorCells.config.fCellSize = br.ReadSingle();
            floorCells.config.fMinX = br.ReadSingle();
            floorCells.config.fMinZ = br.ReadSingle();
            floorCells.config.fBoundMin = br.ReadSingle();
            floorCells.config.fBoundMax = br.ReadSingle();
            floorCells.config.nWidth = br.ReadInt32();
            floorCells.config.nHeight = br.ReadInt32();

            FLOOR_CELL cell = new global::FLOOR_CELL();
            floorCells.cellsDictionary = new Dictionary<int, FLOOR_CELL>();
            floorCells.cellsHashTable = new Hashtable();

            var cellCount = br.ReadInt32();
            for (int i = 0; i < cellCount; ++i)
            {
                var key = br.ReadInt32();
                cell.cellInfo = br.ReadInt32();

                floorCells.cellsDictionary.Add(key, cell);
                floorCells.cellsHashTable.Add(key, cell);
            }

            var heightCount = br.ReadInt32();
            floorCells.heightsArray = new float[heightCount];
            for (int i = 0; i < heightCount; ++i)
            {
                floorCells.heightsArray[i] = ToFloatHeight(br.ReadUInt16(), floorCells.Config.fBoundMin, floorCells.Config.fBoundMax);
            }
        }

        return floorCells;
    }

    public static void Main()
    {
        float[] elapsed = { .0f, 0.0f };

        FloorCells cells = LoadCellFile("DXC_HomeTown");
        const int N = 9000000;

        Random rand = new Random();
        float height = .0f;
        int groundType = 0;
        Vector3 pos = new Vector3();
        Stopwatch sw = Stopwatch.StartNew();

        sw.Start();
        cells.useHashTable = true;
        for (int i = 0; i < N; ++i)
        {
            pos.x = (float)rand.NextDouble() * cells.Config.nWidth * cells.Config.fCellSize + cells.Config.fMinX;
            pos.z = (float)rand.NextDouble() * cells.Config.nHeight * cells.Config.fCellSize + cells.Config.fMinZ;
            cells.GetFloor(pos, ref height, ref groundType);
        }
        sw.Stop();
        elapsed[0] = sw.ElapsedMilliseconds;

        sw.Reset();
        sw.Start();
        cells.useHashTable = false;
        for (int i = 0; i < N; ++i)
        {
            pos.x = (float)rand.NextDouble() * cells.Config.nWidth * cells.Config.fCellSize + cells.Config.fMinX;
            pos.z = (float)rand.NextDouble() * cells.Config.nHeight * cells.Config.fCellSize + cells.Config.fMinZ;
            cells.GetFloor(pos, ref height, ref groundType);
        }
        sw.Stop();
        elapsed[1] = sw.ElapsedMilliseconds;

        Console.WriteLine("Hash:{0}, Dict:{1}", elapsed[0], elapsed[1]);
    }
}
