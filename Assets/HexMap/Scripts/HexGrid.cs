using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace HexMap.Scripts
{
    public class HexGrid : MonoBehaviour
    {
        public HexUnit unitPrefab;
        private List<HexUnit> units = new List<HexUnit>();
        
        // Cell 的数量
        public int cellCountX = 20;
        public int cellCountZ = 15;

        // Chunk 的数量
        private int chunkCountX;
        private int chunkCountZ;

        public HexGridChunk chunkPrefab;
        public HexCell cellPrefab;
        public TextMeshProUGUI cellLabelPrefab;

        public Texture2D noiseSource;

        private HexCell[] cells;
        private HexGridChunk[] chunks;
        public int seed;

        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
            CreateMap(cellCountX, cellCountZ);
        }

        private void OnEnable()
        {
            if (!HexMetrics.noiseSource)
            {
                HexMetrics.noiseSource = noiseSource;
                HexMetrics.InitializeHashGrid(seed);
                HexUnit.unitPrefab = unitPrefab;
            }
        }

        public bool CreateMap(int x, int z)
        {
            if (x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
                z <= 0 || z % HexMetrics.chunkSizeZ != 0)
            {
                Debug.LogError("Unsupported map size.");
                return false;
            }
            cellCountX = x;
            cellCountZ = z;
            ClearPath();
            ClearUnits();
            if (chunks != null)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    Destroy(chunks[i].gameObject);
                }
            }
            chunkCountX = cellCountX / HexMetrics.chunkSizeX;
            chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
            CreateChunks();
            CreateCells();
            return true;
        }

        private void CreateChunks()
        {
            chunks = new HexGridChunk[chunkCountX * chunkCountZ];
            int i = 0;
            for (int z = 0; z < chunkCountZ; z++)
            {
                for (int x = 0; x < chunkCountX; x++)
                {
                    HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                    chunk.transform.SetParent(transform);
                }
            }
        }

        private void CreateCells()
        {
            cells = new HexCell[cellCountZ * cellCountX];
            int i = 0;
            // 创建整张地图
            for (int z = 0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
        }

        // 创建一个 cell
        private void CreateCell(int x, int z, int i)
        {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f);

            HexCell cell = cells[i] = Instantiate(cellPrefab);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            // 把 cell 连起来
            if (x > 0)
            {
                cell.SetNeighbor(HexDirection.W, cells[i - 1]);
            }

            if (z > 0)
            {
                // 处理偶数行
                if ((z & 1) == 0)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                    if (x > 0)
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                    }
                }
                // 处理奇数行
                else
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                    if (x < cellCountX - 1)
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                    }
                }
            }

            TextMeshProUGUI label = Instantiate(cellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            // label.text = cell.coordinates.ToStringOnSeparateLines();
            cell.uiRect = label.rectTransform;
            cell.Elevation = 0;
            AddCellToChunk(x, z, cell);
        }

        private void AddCellToChunk(int x, int z, HexCell cell)
        {
            // 计算 cell 所在 chunk 的 X 和 Z 坐标
            int chunkX = x / HexMetrics.chunkSizeX;
            int chunkZ = z / HexMetrics.chunkSizeZ;
            HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];
            // 计算 cell 在 chunk 中的 X 和 Z 坐标
            int localX = x - chunkX * HexMetrics.chunkSizeX;
            int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
            // 把 cell 添加到 chunk 中
            chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
        }

        public HexCell GetCell(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            var coordinates = HexCoordinates.FromPosition(localPos);
            // 计算 cell 在数组中的索引，必须加上 offset
            int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
            return cells[index];
        }

        public HexCell GetCell(HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            if (z < 0 || z >= cellCountZ)
            {
                return null;
            }

            int x = coordinates.X + z / 2;
            if (x < 0 || x >= cellCountX)
            {
                return null;
            }

            return cells[x + z * cellCountX];
        }

        public void ShowUI(bool visible)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].ShowUI(visible);
            }
        }


        

        public void Save(BinaryWriter writer)
        {
            writer.Write(cellCountX);
            writer.Write(cellCountZ);
            foreach (var cell in cells)
            {
                cell.Save(writer);
            }
            writer.Write(units.Count);
            foreach (var unit in units)
            {
                unit.Save(writer);
            }
        }

        public void Load(BinaryReader reader, int header)
        {
            StopAllCoroutines();
            ClearPath();
            ClearUnits();
            int x = 20, z = 15;
            if (header >= 1)
            {
                x = reader.ReadInt32();
                z = reader.ReadInt32();
            }

            if (x != cellCountX || z != cellCountZ)
            {
                if (!CreateMap(x, z))
                {
                    return;
                }
            }
            foreach (var cell in cells)
            {
                cell.Load(reader);
            }

            foreach (var chunk in chunks)
            {
                chunk.Refresh();
            }

            if (header >= 2)
            {
                int unitCount = reader.ReadInt32();
                for(int i = 0; i < unitCount; i++)
                {
                    HexUnit.Load(reader, this);
                }
            }
        }
        
        /// <summary>
        /// 计算所有其他格子到 cell 的距离
        /// </summary>
        /// <param name="cell">计算到此 cell 的距离</param>
        public void FindAllDistanceTo(HexCell cell)
        {
            StopAllCoroutines();
            StartCoroutine(Search(cell));
        }

        private IEnumerator Search(HexCell cell)
        {
            foreach (var c in cells)
            {
                c.Distance = int.MaxValue;
            }
            
            WaitForSeconds delay = new WaitForSeconds(1 / 60f);
            
            //求当前格子和其他所有格子之间的最短距离
            // for (int i = 0; i < cells.Length; i++)
            // {
            //     yield return delay;
            //     cells[i].Distance = cell.coordinates.DistanceTo(cells[i].coordinates);
            // }
            
            // BFS
            // BFS 仅适合无权重的图
            // Dijkstra 算法适合有权重的图
            // open set
            List<HexCell> frontier = new List<HexCell>();
            cell.Distance = 0;
            frontier.Add(cell);
            while (frontier.Count > 0)
            {
                yield return delay;
                HexCell current = frontier[0];
                frontier.RemoveAt(0);
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);
                    if (neighbor == null) continue;
                    if (neighbor.IsUnderwater) continue;
                    HexEdgeType edgeType = current.GetEdgeType(neighbor);
                    if (edgeType == HexEdgeType.Cliff) continue;
                    
                    int distance = current.Distance;
                    if (current.HasRoadThroughEdge(d))
                    {
                        distance += 1;
                    }
                    else if (current.Walled != neighbor.Walled)
                    {
                        continue;
                    }
                    else
                    {
                        distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                        distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                    }

                    if (neighbor.Distance == int.MaxValue)
                    {
                        neighbor.Distance = distance;
                        frontier.Add(neighbor);
                    } 
                    else if (distance < neighbor.Distance)
                    {
                        neighbor.Distance = distance;
                    }
                    frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
                }
            }
        }
        
        
        
        // open set
        private HexCellPriorityQueue searchFrontier;
        // 每次搜索时增加此值，可以避免重新初始化每个格子的 SearchPhase
        private int searchFrontierPhase;
        private HexCell currentPathFrom;
        private HexCell currentPathTo;
        private bool currentPathExists;

        public bool HasPath => currentPathExists;
        
        /// <summary>
        /// 找到从 fromCell 到 toCell 的最短路径
        /// </summary>
        /// <param name="fromCell">源 cell</param>
        /// <param name="toCell">目标 cell</param>
        /// <param name="speed">速度, 移动预算, movement budget for one turn, 因为其代表一回合移动的距离，所以叫做速度</param>
        public void FindPath(HexCell fromCell, HexCell toCell, int speed)
        {
            // StopAllCoroutines();
            // StartCoroutine(Search(fromCell, toCell, speed));
            
            // System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            // sw.Start();
            ClearPath();
            currentPathFrom = fromCell;
            currentPathTo = toCell;
            currentPathExists = Search(fromCell, toCell, speed);
            ShowPath(speed);
            // sw.Stop();
            // Debug.Log("搜索耗时：" + sw.ElapsedMilliseconds);
        }
        

        
        private bool Search(HexCell fromCell, HexCell toCell, int speed)
        {
            // 每次搜索时增加此值，可以避免重新初始化每个格子的 SearchPhase
            searchFrontierPhase += 2;
            searchFrontier ??= new HexCellPriorityQueue();
            searchFrontier.Clear();
            
            foreach (var c in cells)
            {
                // 每一个 Cell 都设置一遍，很耗时
                // c.SetLabel(null);
                // c.DisableHighlight();
            }
            // fromCell.EnableHighlight(Color.cyan);
            
            // WaitForSeconds delay = new WaitForSeconds(1 / 60f);
            // 初始化
            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            searchFrontier.Enqueue(fromCell);
            while (searchFrontier.Count > 0)
            {
                // yield return delay;
                HexCell current = searchFrontier.Dequeue();
                // 表示正在搜索此格子 
                current.SearchPhase += 1;
                // 终止条件
                if (current == toCell)
                {
                    // while (current != fromCell)
                    // {
                    //     int turn = current.Distance / speed;
                    //     current.SetLabel(turn.ToString());
                    //     current.EnableHighlight(Color.white);
                    //     current = current.PathFrom;
                    // }
                    // toCell.EnableHighlight(Color.red);
                    // break;
                    return true;
                }
                
                // current cell 的回合
                int currentTurn = (current.Distance - 1) / speed;
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);
                    if (neighbor == null) continue;
                    if (neighbor.SearchPhase > searchFrontierPhase) continue;
                    if (neighbor.IsUnderwater || neighbor.Unit) continue;
                    HexEdgeType edgeType = current.GetEdgeType(neighbor);
                    if (edgeType == HexEdgeType.Cliff) continue;

                    // 移动到当前邻居需要的代价
                    int moveCost;
                    
                    // 如果有路，代价为1
                    if (current.HasRoadThroughEdge(d))
                    {
                        moveCost = 1;
                    }
                    // 如果有墙没有路，不能通行
                    else if (current.Walled != neighbor.Walled)
                    {
                        continue;
                    }
                    // 否则，根据环境计算代价
                    else
                    {
                        moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                        moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                        moveCost += neighbor.HasRiver ? 15 : 0;
                    }
                    int distance = current.Distance + moveCost;
                    // 当前邻居的回合
                    // 如果邻居的回合大于当前回合，那么我们已经越过了一个回合边界
                    int turn = (distance-1) / speed;
                    if (turn > currentTurn)
                    {
                        distance = turn * speed + moveCost;
                    }
                    // 表示 neighbor 处于上一次的状态，还未被搜索到
                    if (neighbor.SearchPhase < searchFrontierPhase)
                    {
                        // 初始化
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        // neighbor.SetLabel(turn.ToString());
                        neighbor.PathFrom = current;
                        neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                        searchFrontier.Enqueue(neighbor);
                        
                    } 
                    else if (distance < neighbor.Distance)
                    {
                        int oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        // neighbor.SetLabel(turn.ToString());
                        neighbor.PathFrom = current;
                        searchFrontier.Change(neighbor, oldPriority);
                    }
                }
            }
            return false;
        }

        private void ShowPath(int speed)
        {
            if (currentPathExists)
            {
                HexCell current = currentPathTo;
                while (current != currentPathFrom)
                {
                    int turn = (current.Distance - 1) / speed;
                    current.SetLabel(turn.ToString());
                    current.EnableHighlight(Color.white);
                    current = current.PathFrom;
                }
            }
            currentPathFrom.EnableHighlight(Color.cyan);
            currentPathTo.EnableHighlight(Color.red);
        }

        public void ClearPath()
        {
            if (currentPathExists)
            {
                HexCell current = currentPathTo;
                while (current != currentPathFrom)
                {
                    current.SetLabel(null);
                    current.DisableHighlight();
                    current = current.PathFrom;
                }
                current.DisableHighlight();
                currentPathExists = false;
            }
            else if (currentPathFrom)
            {
                currentPathFrom.DisableHighlight();
                currentPathTo.DisableHighlight();
            }
            currentPathFrom = currentPathTo = null;
        }

        public List<HexCell> GetPath()
        {
            if (!currentPathExists)
            {
                return null;
            }
            List<HexCell> path = ListPool<HexCell>.Get();
            for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
            {
                path.Add(c);
            }
            path.Add(currentPathFrom);
            path.Reverse();
            return path;
        }
        

        public void AddUnit(HexUnit unit, HexCell location, float orientation)
        {
            units.Add(unit);
            unit.transform.SetParent(transform, false);
            unit.Location = location;
            unit.Orientation = orientation;
        }

        public void RemoveUnit(HexUnit unit)
        {
            units.Remove(unit);
            unit.Die();
        }

        private void ClearUnits()
        {
            foreach (var unit in units)
            {
                unit.Die();
            }
            units.Clear();
        }

        public HexCell GetCell(Ray ray)
        {
            if (Physics.Raycast(ray, out var hit))
            {
                return GetCell(hit.point);
            }
            return null;
        }
        
    }
}