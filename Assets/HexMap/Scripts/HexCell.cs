using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HexMap.Scripts
{
    // 代表六边形网格的每一个 cell
    public class HexCell : MonoBehaviour
    {
        #region 寻路相关数据
        
        public HexUnit Unit { get; set; }
        
        // 代表此格子和被选中的源格子之间的距离
        private int distance;

        public int Distance
        {
            get { return distance; }
            set
            {
                distance = value;
                // UpdateDistanceLabel();
            }
        }
        
        // 记录 A star 的启发值 - 代表此格子到目标格子的距离
        public int SearchHeuristic { get; set; }

        public int SearchPriority
        {
            get => (distance + SearchHeuristic);
        }
        
        // 记录搜索路径上的上一个格子
        public HexCell PathFrom { get; set; }

        // private void UpdateDistanceLabel()
        // {
        //     TextMeshProUGUI label = uiRect.GetComponent<TextMeshProUGUI>();
        //     label.text = distance == int.MaxValue ? "" : distance.ToString();
        // }

        public void SetLabel(string text)
        {
            var label = uiRect.GetComponent<TextMeshProUGUI>();
            label.text = text;
        }
        
        // 下一个相同优先级的格子, 用此构成优先队列的链表
        public HexCell NextWithSamePriority { get; set; }
        
        // 搜索的阶段, 和 searchFrontierPhase 比较，确定状态
        public int SearchPhase { get; set; }
        
        #endregion


        public RectTransform uiRect;
        public HexCoordinates coordinates;
        public HexGridChunk chunk;

        private int specialIndex;

        public int SpecialIndex
        {
            get => specialIndex;
            set
            {
                if (specialIndex != value && !HasRiver)
                {
                    specialIndex = value;
                    RemoveRoads();
                    RefreshSelfOnly();
                }
            }
        }

        public bool IsSpecial => specialIndex > 0;

        private int terrainTypeIndex;

        public int TerrainTypeIndex
        {
            get => terrainTypeIndex;
            set
            {
                if (terrainTypeIndex != value)
                {
                    terrainTypeIndex = value;
                    Refresh();
                }
            }
        }

        public Vector3 Position => transform.localPosition;


        #region Edge

        public HexEdgeType GetEdgeType(HexDirection direction)
        {
            return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
        }

        public HexEdgeType GetEdgeType(HexCell otherCell)
        {
            return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
        }

        #endregion

        #region Neighbor

        // NE, E, SE, SW, W, NW
        [SerializeField] private HexCell[] neighbors;

        public HexCell GetNeighbor(HexDirection direction)
        {
            return neighbors[(int)direction];
        }

        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }

        #endregion

        #region Elevation

        // 此 cell 的海拔
        private int elevation = int.MinValue;

        public int Elevation
        {
            get => elevation;
            set
            {
                if (elevation == value)
                {
                    return;
                }

                elevation = value;
                RefreshPosition();
                if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation)
                {
                    RemoveOutgoingRiver();
                }

                if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
                {
                    RemoveIncomingRiver();
                }

                for (int i = 0; i < roads.Length; i++)
                {
                    if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                    {
                        SetRoad(i, false);
                    }
                }

                Refresh();
            }
        }

        private void RefreshPosition()
        {
            Vector3 position = transform.localPosition;
            position.y = elevation * HexMetrics.elevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y - 1f;
            uiRect.localPosition = uiPosition;
        }


        // 获取同 在此方向的邻居 之间的海拔差
        public int GetElevationDifference(HexDirection direction)
        {
            int difference = elevation - GetNeighbor(direction).elevation;
            return difference >= 0 ? difference : -difference;
        }

        #endregion

        #region River

        // 河床的 y 坐标
        public float StreamBedY => (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;

        // 河面的 y 坐标
        public float RiverSurfaceY => (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;

        private bool hasIncomingRiver, hasOutgoingRiver;
        private HexDirection incomingRiver, outgoingRiver;
        public bool HasRiver => hasIncomingRiver || hasOutgoingRiver;

        // 是否有河流的起点或终点 
        public bool HasRiverBeginOrEnd => hasIncomingRiver != hasOutgoingRiver;

        public HexDirection RiverBeginOrEndDirection => hasIncomingRiver ? incomingRiver : outgoingRiver;

        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return hasIncomingRiver && incomingRiver == direction || hasOutgoingRiver && outgoingRiver == direction;
        }

        public bool HasIncomingRiver
        {
            get { return hasIncomingRiver; }
        }

        public bool HasOutgoingRiver
        {
            get { return hasOutgoingRiver; }
        }

        public HexDirection IncomingRiver
        {
            get { return incomingRiver; }
        }

        public HexDirection OutgoingRiver
        {
            get { return outgoingRiver; }
        }

        public void RemoveOutgoingRiver()
        {
            if (!hasOutgoingRiver)
            {
                return;
            }

            hasOutgoingRiver = false;
            RefreshSelfOnly();

            HexCell neighbor = GetNeighbor(outgoingRiver);
            neighbor.hasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveIncomingRiver()
        {
            if (!hasIncomingRiver)
            {
                return;
            }

            hasIncomingRiver = false;
            RefreshSelfOnly();

            HexCell neighbor = GetNeighbor(incomingRiver);
            neighbor.hasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveRiver()
        {
            RemoveOutgoingRiver();
            RemoveIncomingRiver();
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (hasOutgoingRiver && outgoingRiver == direction)
            {
                return;
            }

            HexCell neighbor = GetNeighbor(direction);
            if (!neighbor || elevation < neighbor.elevation)
            {
                return;
            }

            RemoveOutgoingRiver();
            if (hasIncomingRiver && incomingRiver == direction)
            {
                RemoveIncomingRiver();
            }

            hasOutgoingRiver = true;
            outgoingRiver = direction;
            specialIndex = 0;
            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            neighbor.specialIndex = 0;
            SetRoad((int)direction, false);
        }

        #endregion

        #region Water

        private int waterLevel;

        public int WaterLevel
        {
            get => waterLevel;
            set
            {
                if (waterLevel == value)
                {
                    return;
                }

                waterLevel = value;
                Refresh();
            }
        }

        public bool IsUnderwater
        {
            get { return waterLevel > elevation; }
        }

        public float WaterSurfaceY
        {
            get
            {
                return
                    (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
            }
        }

        #endregion

        #region Road

        // 道路信息
        // 记录某个方向上是否有道路
        [SerializeField] private bool[] roads;

        public bool HasRoadThroughEdge(HexDirection direction)
        {
            return roads[(int)direction];
        }

        public bool HasRoads
        {
            get
            {
                for (int i = 0; i < roads.Length; i++)
                {
                    if (roads[i])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void AddRoad(HexDirection direction)
        {
            if (!roads[(int)direction] &&
                !HasRiverThroughEdge(direction) &&
                !IsSpecial &&
                !GetNeighbor(direction).IsSpecial &&
                GetElevationDifference(direction) <= 1)
            {
                SetRoad((int)direction, true);
            }
        }

        public void RemoveRoads()
        {
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (roads[i])
                {
                    SetRoad(i, false);
                }
            }
        }

        private void SetRoad(int index, bool state)
        {
            roads[index] = state;
            neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
            neighbors[index].RefreshSelfOnly();
            RefreshSelfOnly();
        }

        #endregion

        #region Features

        private int urbanLevel, farmLevel, plantLevel;

        public int UrbanLevel
        {
            get => urbanLevel;
            set
            {
                if (urbanLevel != value)
                {
                    urbanLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int FarmLevel
        {
            get { return farmLevel; }
            set
            {
                if (farmLevel != value)
                {
                    farmLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int PlantLevel
        {
            get { return plantLevel; }
            set
            {
                if (plantLevel != value)
                {
                    plantLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        private bool walled;

        public bool Walled
        {
            get { return walled; }
            set
            {
                if (walled != value)
                {
                    walled = value;
                    Refresh();
                }
            }
        }

        #endregion

        #region Highlight

        public void DisableHighlight()
        {
            Image highlight = uiRect.GetChild(0).GetComponent<Image>();
            highlight.enabled = false;
        }

        public void EnableHighlight(Color color)
        {
            Image highlight = uiRect.GetChild(0).GetComponent<Image>();
            highlight.color = color;
            highlight.enabled = true;
        }

        #endregion

        private void RefreshSelfOnly()
        {
            chunk.Refresh();
            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }

        private void Refresh()
        {
            if (chunk)
            {
                chunk.Refresh();
                for (int i = 0; i < neighbors.Length; i++)
                {
                    HexCell neighbor = neighbors[i];
                    if (neighbor != null && neighbor.chunk != chunk)
                    {
                        neighbor.chunk.Refresh();
                    }
                }

                if (Unit)
                {
                    Unit.ValidateLocation();
                }
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((byte)terrainTypeIndex);
            writer.Write((byte)elevation);
            writer.Write((byte)waterLevel);
            writer.Write((byte)urbanLevel);
            writer.Write((byte)farmLevel);
            writer.Write((byte)plantLevel);
            writer.Write((byte)specialIndex);
            writer.Write(walled);
            if (hasIncomingRiver)
            {
                writer.Write((byte)(incomingRiver + 128));
            }
            else
            {
                writer.Write((byte)0);
            }


            if (hasOutgoingRiver)
            {
                writer.Write((byte)(outgoingRiver + 128));
            }
            else
            {
                writer.Write((byte)0);
            }

            int roadFlags = 0;
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    roadFlags |= 1 << i;
                }
            }

            writer.Write((byte)roadFlags);
        }

        public void Load(BinaryReader reader)
        {
            terrainTypeIndex = reader.ReadByte();
            elevation = reader.ReadByte();
            RefreshPosition();
            waterLevel = reader.ReadByte();
            urbanLevel = reader.ReadByte();
            farmLevel = reader.ReadByte();
            plantLevel = reader.ReadByte();
            specialIndex = reader.ReadByte();
            walled = reader.ReadBoolean();
            byte riverData = reader.ReadByte();
            if (riverData >= 128)
            {
                hasIncomingRiver = true;
                incomingRiver = (HexDirection)(riverData - 128);
            }
            else
            {
                hasIncomingRiver = false;
            }

            riverData = reader.ReadByte();
            if (riverData >= 128)
            {
                hasOutgoingRiver = true;
                outgoingRiver = (HexDirection)(riverData - 128);
            }
            else
            {
                hasOutgoingRiver = false;
            }

            int roadFlags = reader.ReadByte();
            for (int i = 0; i < roads.Length; i++)
            {
                roads[i] = (roadFlags & (1 << i)) != 0;
            }
        }
    }
}