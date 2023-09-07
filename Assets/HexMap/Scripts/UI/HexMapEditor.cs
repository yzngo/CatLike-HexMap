using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace HexMap.Scripts
{
    public class HexMapEditor : MonoBehaviour
    {
        public HexGrid hexGrid;
        public Material terrainMaterial;

        // private bool editMode = true;

        private int activeTerrainTypeIndex;
        private int activeElevation;
        private int activeWaterLevel;
        private int activeUrbanLevel;
        private int activeFarmLevel;
        private int activePlantLevel;
        private int activeSpecialIndex;

        private bool applyElevation = true;
        private bool applyWaterLevel = true;
        private bool appleUrbanLevel = true;
        private bool applyFarmLevel = true;
        private bool applyPlantLevel = true;
        private bool applySpecialIndex = true;

        private int brushSize;

        private enum OptionalToggle
        {
            Ignore,
            Yes,
            No
        }

        private OptionalToggle riverMode = OptionalToggle.Ignore;
        private OptionalToggle roadMode = OptionalToggle.Ignore;
        private OptionalToggle walledMode = OptionalToggle.Ignore;

        private bool isDrag;
        private HexDirection dragDirection;

        private HexCell previousCell;
        // private HexCell searchFromCell;
        // private HexCell searchToCell;


        private void Awake()
        {
            terrainMaterial.DisableKeyword("_GRID_ON");
            SetEditMode(true);
        }

        private void Update()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButton(0))
                {
                    HandleInput();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.U))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        DestroyUnit();
                    }
                    else
                    {
                        CreateUnit();
                    }
                    return;
                }
            }
            previousCell = null;
        }

        private HexCell GetCellUnderCursor()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            return hexGrid.GetCell(inputRay);
        }
        
        private void HandleInput()
        {
            HexCell currentCell = GetCellUnderCursor();
            if (currentCell)
            {
                if (previousCell && previousCell != currentCell)
                {
                    ValidateDrag(currentCell);
                }
                else
                {
                    isDrag = false;
                }

                // if (editMode)
                // {
                    EditCells(currentCell);
                // }
                // 按住shift选择起点
                // else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell)
                // {
                //     if (searchFromCell != currentCell)
                //     {
                //         if (searchFromCell)
                //         {
                //             searchFromCell.DisableHighlight();
                //         }
                //
                //         searchFromCell = currentCell;
                //         searchFromCell.EnableHighlight(Color.cyan);
                //         if (searchToCell)
                //         {
                //             hexGrid.FindPath(searchFromCell, searchToCell, 24);
                //         }
                //     }
                // }
                // else
                // {
                // hexGrid.FindAllDistanceTo(currentCell);
                // }
                // 选择终点，并寻找路径
                // else if (searchFromCell && searchFromCell != currentCell)
                // {
                //     if (searchToCell != currentCell)
                //     {
                //         searchToCell = currentCell;
                //         hexGrid.FindPath(searchFromCell, searchToCell, 24);
                //     }
                // }

                previousCell = currentCell;
            }
            else
            {
                previousCell = null;
            }
        }

        private void ValidateDrag(HexCell currentCell)
        {
            for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
            {
                if (previousCell.GetNeighbor(dragDirection) == currentCell)
                {
                    isDrag = true;
                    return;
                }
            }

            isDrag = false;
        }

        // 笔刷效果，编辑周围一圈cell
        private void EditCells(HexCell center)
        {
            int centerX = center.coordinates.X;
            int centerZ = center.coordinates.Z;
            for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
            {
                for (int x = centerX - r; x <= centerX + brushSize; x++)
                {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }

            for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
            {
                for (int x = centerX - brushSize; x <= centerX + r; x++)
                {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
        }

        private void EditCell(HexCell cell)
        {
            if (cell == null) return;

            if (activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }

            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }

            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }

            if (applySpecialIndex)
            {
                cell.SpecialIndex = activeSpecialIndex;
            }

            if (appleUrbanLevel)
            {
                cell.UrbanLevel = activeUrbanLevel;
            }

            if (applyFarmLevel)
            {
                cell.FarmLevel = activeFarmLevel;
            }

            if (applyPlantLevel)
            {
                cell.PlantLevel = activePlantLevel;
            }

            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }

            if (roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }

            if (walledMode != OptionalToggle.Ignore)
            {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }

            if (isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }

                    if (roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }

        private void CreateUnit()
        {
            HexCell cell = GetCellUnderCursor();
            if (cell && !cell.Unit)
            {
                hexGrid.AddUnit(Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f));
            }
        }


        private void DestroyUnit()
        {
            HexCell cell = GetCellUnderCursor();
            if (cell && cell.Unit)
            {
                hexGrid.RemoveUnit(cell.Unit);
            }
        }

        public void SetTerrainTypeIndex(int index)
        {
            activeTerrainTypeIndex = index;
        }

        public void SetApplyElevation(bool toggle)
        {
            applyElevation = toggle;
        }

        public void SetElevation(float elevation)
        {
            activeElevation = (int)elevation;
        }

        public void SetBrushSize(float size)
        {
            brushSize = (int)size;
        }

        public void SetRiverMode(int mode)
        {
            riverMode = (OptionalToggle)mode;
        }

        public void SetRoadMode(int mode)
        {
            roadMode = (OptionalToggle)mode;
        }

        public void SetWalledMode(int mode)
        {
            walledMode = (OptionalToggle)mode;
        }

        public void SetApplyWaterLevel(bool toggle)
        {
            applyWaterLevel = toggle;
        }

        public void SetWaterLevel(float level)
        {
            activeWaterLevel = (int)level;
        }

        public void SetApplyUrbanLevel(bool toggle)
        {
            appleUrbanLevel = toggle;
        }

        public void SetUrbanLevel(float level)
        {
            activeUrbanLevel = (int)level;
        }

        public void SetApplyFarmLevel(bool toggle)
        {
            applyFarmLevel = toggle;
        }

        public void SetFarmLevel(float level)
        {
            activeFarmLevel = (int)level;
        }

        public void SetApplyPlantLevel(bool toggle)
        {
            applyPlantLevel = toggle;
        }

        public void SetPlantLevel(float level)
        {
            activePlantLevel = (int)level;
        }

        public void SetApplySpecialIndex(bool toggle)
        {
            applySpecialIndex = toggle;
        }

        public void SetSpecialIndex(float index)
        {
            activeSpecialIndex = (int)index;
        }

        public void ShowGrid(bool visible)
        {
            if (visible)
            {
                terrainMaterial.EnableKeyword("_GRID_ON");
            }
            else
            {
                terrainMaterial.DisableKeyword("_GRID_ON");
            }
        }

        public void SetEditMode(bool toggle)
        {
            Debug.Log("Disable Editor UI");
            // editMode = toggle;
            // hexGrid.ShowUI(!toggle);
            // ShowGrid(!toggle);
            enabled = toggle;
        }


    }
}