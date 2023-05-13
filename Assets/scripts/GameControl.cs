using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameControl : MonoBehaviour
{

    public  GameControl Instance { get; private set; }

    public int[,] map = new int[20, 20];  // 存放每个位置信息，0表示正常路径，-1表示障碍物，2表示宝箱
    public int playerPosX = 0;  // 玩家当前位置横坐标
    public int playerPosY = 0;  // 玩家当前位置纵坐标
    public Vector2Int endPos;  // 地图终点坐标
    private PlayerControl playerControl;
    // 障碍物对象
    public GameObject barrierPrefab;
    //终点
    public GameObject EndPrefab;
    //
    public GameObject textPrefab;
    // 地图大小
    private const int MapWidth = 15;
    private const int MapHeight = 9;
    // 终点位置
    public int endPosX = MapWidth - 1;
    public int endPosY = MapHeight - 1;
    public int startPosX = 0;  // 玩家当前位置横坐标
    public int startPosY = 0;
    //可视化路径
    public List<Vector2Int> resultpath;
    // 提示路径
    public GameObject tipPrefab;

    private bool Search_DsjIsOn;
    private bool Search_BFSIsOn;
    private bool Search_AstarIsOn;
    private GameObject[,] BlockPrefab;//放置文字块

    [Header("A_Star")]
    public Dictionary<Vector2Int, int> f;
    public Dictionary<Vector2Int, int> g;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // 初始化地图和玩家等
        GameInit();
        // ...

        
        
    }

    private void ShowPath(List<Vector2Int> resultpath)
    {
        // 循环遍历路径上的每一个坐标
        for (int i = 0; i < resultpath.Count; i++)
        {
            // 实例化一个预设体生成提示对象
            GameObject tip = Instantiate(tipPrefab, transform.position, Quaternion.identity);
            // 设置提示对象的位置
            tip.transform.position = new Vector3(resultpath[i].x, resultpath[i].y, -2);
        }
    }

    private void GameInit()
    {
        BlockPrefab = new GameObject[20, 20];
        // 初始化地图数据
        for (int i = 0; i < MapWidth; i++)
        {
            for (int j = 0; j < MapHeight; j++)
            {
                
                //设置地图方块及方块内容,随机1-9
                GameObject Prefab = Instantiate(textPrefab, new Vector3(i, j, -2), Quaternion.identity);
                BlockPrefab[i,j]= Prefab;//将文字块对象放入对应位置，以便通过下标访问
                int x = UnityEngine.Random.Range(1, 9);
                Prefab.transform.SetParent(transform);
                Text textComponent = Prefab.transform.Find("Canvas").Find("Text").GetComponent<Text>(); ;
                if (textComponent != null)
                {
                    textComponent.text = x.ToString();
                }
                map[i, j] = x;
            }
        }

        // 生成起点和终点
        startPosX = UnityEngine.Random.Range(0, 3);
        startPosY = UnityEngine.Random.Range(0, 3);
        endPosX = UnityEngine.Random.Range(7, MapWidth);
        endPosY = UnityEngine.Random.Range(7, MapHeight);
        //设置终点对象坐标
        GameObject endObj = Instantiate(EndPrefab, new Vector3(endPosX, endPosY, -2), Quaternion.identity);
        endObj.transform.SetParent(transform);

        // 生成障碍物
        int numObstacles = 20;
        int curObstacle = 0;
        while (curObstacle < numObstacles)
        {
            int posX = UnityEngine.Random.Range(0, MapWidth);
            int posY = UnityEngine.Random.Range(0, MapHeight);
            // 判断是否为起点或终点
            if ((posX == startPosX && posY == startPosY) || (posX == endPosX && posY == endPosY))
            {
                continue;
            }
            // 判断是否与起点或终点在同一直线上
            if (posX == startPosX || posY == startPosY || posX - startPosX == posY - startPosY || posX - startPosX == startPosY - posY)
            {
                continue;
            }
            // 判断是否与障碍物重叠
            if (map[posX, posY] == 1)
            {
                continue;
            }
            // 生成障碍物
            map[posX, posY] = -1;
            GameObject barrierObj = Instantiate(barrierPrefab, new Vector3(posX, posY, -2), Quaternion.identity);
            barrierObj.transform.SetParent(transform);

            curObstacle++;
        }

        // 初始化玩家位置
        playerPosX = startPosX;
        playerPosY = startPosY;
    }

    public List<Vector2Int> Search_Dijkstra(int startPosX, int startPosY, int endPosX, int endPosY)
    {
        Vector2Int start = new Vector2Int(startPosX, startPosY);
        Vector2Int end = new Vector2Int(endPosX, endPosY);
        // 存储起点到各个点的最短距离
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
        // 存储每个点的前驱节点，即到该点的最短路径上，该点的前一个节点
        Dictionary<Vector2Int, Vector2Int> predecessors = new Dictionary<Vector2Int, Vector2Int>();
        // 存储已经被访问的节点
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        // 存储还未被访问的节点
        List<Vector2Int> unvisited = new List<Vector2Int>();

        Vector2Int[] mapPositions = new Vector2Int[map.GetLength(0) * map.GetLength(1)];
        int index = 0;
        //将map转换为Vector2Int[]类型mapPositions
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                mapPositions[index] = new Vector2Int(i, j);
                index++;
            }
        }
        // 初始化distances和unvisited
        foreach (Vector2Int pos in mapPositions)
        {
            if (pos == start)
                distances[pos] = 0;
            else
                distances[pos] = int.MaxValue;

            unvisited.Add(pos);
        }

        while (unvisited.Count > 0)
        {
            // 从未被访问的节点中选出当前距离起点最近的节点
            Vector2Int current = unvisited.OrderBy(pos => distances[pos]).First();

            if (current == end)
                break;

            unvisited.Remove(current);
            visited.Add(current);
            //找到当前距离起点最近的节点，并且对它的邻居节点进行松弛操作，更新到达邻居节点的最短距离和前置节点
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor))
                    continue;

                int tentativeDistance = distances[current] + GetDistance(current, neighbor);

                if (tentativeDistance < distances[neighbor])
                {
                    distances[neighbor] = tentativeDistance;
                    predecessors[neighbor] = current;
                }
            }
        }

        // 生成路径
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int currentPos = end;
        int sum = 0;
        while (currentPos != start)
        {
            path.Insert(0, currentPos);

            sum += map[currentPos.x, currentPos.y];//把所需步骤加起来
            Text textComponent = GameObject.FindGameObjectWithTag("NeedStep").GetComponent<Text>(); ;
            if (textComponent != null)
            {
                textComponent.text = sum.ToString();
            }

            currentPos = predecessors[currentPos];
        }
        path.Insert(0, start);

        return path;
    }

    // 获取相邻的位置
    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] offsets = {
    new Vector2Int(1, 0),  // 右侧
    new Vector2Int(-1, 0), // 左侧
    new Vector2Int(0, 1),  // 上方
    new Vector2Int(0, -1)  // 下方
};
        foreach (Vector2Int offset in offsets)
        {
            Vector2Int neighbor = pos + offset;

            if (IsInMap(neighbor) && !IsObstacle(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    // 获取两个位置之间的距离
    int GetDistance(Vector2Int pos1, Vector2Int pos2)
    {
        //return 1; // 相邻两个位置距离为1
        return map[pos2.x,pos2.y]; // 更改路径消耗
    }

    // 判断位置是否在地图范围内
    bool IsInMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < MapWidth && pos.y >= 0 && pos.y < MapHeight;
    }

    // 判断位置是否为障碍物
    bool IsObstacle(Vector2Int pos)
    {
        return map[pos.x, pos.y] == -1;
    }

    List<Vector2Int> Search_BFS(int startPosX, int startPosY, int endPosX, int endPosY)
    {
        Vector2Int start = new Vector2Int(startPosX, startPosY);
        Vector2Int end = new Vector2Int(endPosX, endPosY);
        //使用了一个队列 queue 和一个字典 parents
        //queue队列中保存的是当前处理的节点，parents字典中保存的是每个节点的父节点
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
        List<Vector2Int> path = new List<Vector2Int>();
        //将起点 start 加入队列并设置其父节点为自己。
        queue.Enqueue(start);
        parents[start] = start;

        while (queue.Count > 0)
        {
            //对于当前节点 current，遍历其相邻的节点 neighbor，
            //如果 neighbor 没有被访问过，则将其加入队列，并将 current 设为其父节点
            Vector2Int current = queue.Dequeue();

            if (current == end)
                break;

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (!parents.ContainsKey(neighbor))
                {
                    parents[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (parents.ContainsKey(end))
        {
            Vector2Int current = end;
            int sum = 0;
            while (current != start)
            {
                path.Add(current);
                

                sum += map[current.x, current.y];//把所需步骤加起来
                Text textComponent = GameObject.FindGameObjectWithTag("NeedStep").GetComponent<Text>(); ;
                if (textComponent != null)
                {
                    textComponent.text = sum.ToString();
                }

                current = parents[current];
            }

            path.Add(start);
            path.Reverse();
        }

        return path;
    }

    List<Vector2Int> Search_Astar(int startPosX, int startPosY, int endPosX, int endPosY)
    {
        // 构建路径
        List<Vector2Int> path = new List<Vector2Int>();

        Vector2Int start = new Vector2Int(startPosX, startPosY);
        Vector2Int end = new Vector2Int(endPosX, endPosY);
        // 初始化 open 和 closed 列表，用于存储节点
        List<Vector2Int> open = new List<Vector2Int>() { start };
        List<Vector2Int> closed = new List<Vector2Int>();
        // 存储每个点的前驱节点，即到该点的最短路径上，该点的前一个节点
        Dictionary<Vector2Int, Vector2Int> Apredecessors = new Dictionary<Vector2Int, Vector2Int>();
        // 初始化 f 和 g 字典，用于存储节点的启发函数值和实际代价
        f = new Dictionary<Vector2Int, int>();
        g = new Dictionary<Vector2Int, int>();

        // 设置起点的代价和启发函数值
        g[start] = 0;
        f[start] = g[start] + GetDistanceAstar(start, end);

        // 重复执行直到 open 列表为空
        while (open.Count > 0)
        {
            // 找到 open 列表中 f 值最小的节点，作为当前节点
            Vector2Int current = open.OrderBy(pos => f[pos]).First();

            if (current == end)
            {
                // Found the goal, reconstruct the path
                path.Add(current);

                while (Apredecessors.ContainsKey(current))
                {
                    current = Apredecessors[current];
                    path.Insert(0, current);
                }

                break;
            }

            // 将当前节点从 open 列表移除，加入 closed 列表
            open.Remove(current);
            closed.Add(current);

            // 遍历当前节点的相邻节点
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                // 如果相邻节点已经在 closed 列表中，跳过
                if (closed.Contains(neighbor))
                    continue;
                print("come in");
                // 计算从起点到相邻节点的代价
                int tentativeG = g[current] + GetDistance(current, neighbor);

                // 如果相邻节点不在 open 列表中，或者当前代价更小
                if (!open.Contains(neighbor) || tentativeG < g[neighbor])
                {
                    // 更新相邻节点的代价和启发函数值
                    g[neighbor] = tentativeG;
                    f[neighbor] = g[neighbor] + GetDistanceAstar(neighbor, end);

                    // 如果相邻节点不在 open 列表中，加入 open 列表
                    if (!open.Contains(neighbor))
                    {
                        open.Add(neighbor);
                        Apredecessors[neighbor] = current;
                    }

                }
            }
        }
        //Vector2Int currentPos = end;

        //while (currentPos != start)
        //{
        //    path.Add(currentPos);
        //    currentPos = Apredecessors[currentPos];
        //}

        path.Reverse();
        return path;
    }

    private int GetDistanceAstar(Vector2Int start, Vector2Int end)
    {
            return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
        
    }


    //OpenSearch_Dijkstra用来给按钮调用，实现寻路
    public void OpenSearch_Dijkstra()

    {
        
        Search_DsjIsOn = !Search_DsjIsOn;
        if (Search_DsjIsOn)
        {
            //可视化路径
            resultpath = Search_Dijkstra(startPosX, startPosY, endPosX, endPosY);
            ShowPath(resultpath);
        }
        else
        {
            // 获取所有 tag 为 "tip" 的游戏对象
            GameObject[] tips = GameObject.FindGameObjectsWithTag("Tips");

            // 将所有 tip 游戏对象设置为未激活状态
            foreach (GameObject tip in tips)
            {
                tip.SetActive(false);
            }
        }

    }

    //OpenSearch_BFS用来给按钮调用，实现寻路
    public void OpenSearch_BFS()

    {

        Search_BFSIsOn = !Search_BFSIsOn;
        if (Search_BFSIsOn)
        {
            //可视化路径
            resultpath = Search_BFS(startPosX, startPosY, endPosX, endPosY);
            ShowPath(resultpath);
        }
        else
        {
            // 获取所有 tag 为 "tip" 的游戏对象
            GameObject[] tips = GameObject.FindGameObjectsWithTag("Tips");

            // 将所有 tip 游戏对象设置为未激活状态
            foreach (GameObject tip in tips)
            {
                tip.SetActive(false);
            }
        }

    }
    public void OpenSearch_Astar()

    {

        Search_AstarIsOn = !Search_AstarIsOn;
        if (Search_AstarIsOn)
        {
            //可视化路径
            print("scuees");
            resultpath = Search_Astar(startPosX, startPosY, endPosX, endPosY);
            ShowPath(resultpath);
        }
        else
        {
            // 获取所有 tag 为 "tip" 的游戏对象
            GameObject[] tips = GameObject.FindGameObjectsWithTag("Tips");

            // 将所有 tip 游戏对象设置为未激活状态
            foreach (GameObject tip in tips)
            {
                tip.SetActive(false);
            }
        }

    }
    public void ChangeMap()//将BFS得到的路径全部权值加大
    {
        resultpath = Search_BFS(startPosX, startPosY, endPosX, endPosY);

        // 循环遍历路径上的每一个坐标
        for (int i = 0; i < resultpath.Count; i++)
        {
            //局部变量 prabs
            //将BFS得到的路径全部权值加大
            GameObject prabs = BlockPrefab[resultpath[i].x, resultpath[i].y];
            int x = UnityEngine.Random.Range(10, 20);
            Text textComponent = prabs.transform.Find("Canvas").Find("Text").GetComponent<Text>(); ;
            if (textComponent != null)
            {
                textComponent.text = x.ToString();
            }
            map[resultpath[i].x, resultpath[i].y] = x;
        }


    }




    public void MoveToLeft()
    {
        if (playerPosX > 0 && map[playerPosX - 1, playerPosY] != -1)
        {
            playerPosX--;
        }
    }

    public void MoveToRight()
    {
        if (playerPosX < MapWidth && map[playerPosX + 1, playerPosY] != -1)
        {
            playerPosX++;
        }
    }

    public void MoveToUp()
    {
        if (playerPosY < MapHeight && map[playerPosX, playerPosY + 1] != -1)
        {
            playerPosY++;
        }
    }

    public void MoveToDown()
    {
        if (playerPosY > 0 && map[playerPosX, playerPosY - 1] != -1)
        {
            playerPosY--;
        }
    }

}
