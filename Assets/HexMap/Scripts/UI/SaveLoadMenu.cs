using System;
using System.IO;
using TMPro;
using UnityEngine;

namespace HexMap.Scripts
{
    public class SaveLoadMenu : MonoBehaviour
    {
        public HexGrid hexGrid;

        public TextMeshProUGUI menuLabel, actionButtonLabel;
        public TMP_InputField nameInput;

        public RectTransform listContent;
        public SaveLoadItem itemPrefab;

        private bool saveMode;

        public void Open(bool saveMode)
        {
            this.saveMode = saveMode;
            if (saveMode)
            {
                menuLabel.text = "Save Map";
                actionButtonLabel.text = "Save";
            }
            else
            {
                menuLabel.text = "Load Map";
                actionButtonLabel.text = "Load";
            }

            FillList();
            gameObject.SetActive(true);
            HexMapCamera.Locked = true;
        }

        public void Action()
        {
            string path = GetSelectedPath();
            if (path == null)
            {
                return;
            }

            if (saveMode)
            {
                Save(path);
            }
            else
            {
                Load(path);
            }

            Close();
        }

        public void Delete()
        {
            string path = GetSelectedPath();
            if (path == null)
            {
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            nameInput.text = "";
            FillList();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            HexMapCamera.Locked = false;
        }

        private string GetSelectedPath()
        {
            string mapName = nameInput.text;
            if (mapName.Length == 0)
            {
                return null;
            }

            return Path.Combine(Application.persistentDataPath, mapName + ".map");
        }


        private void Save(string path)
        {
            Debug.Log(Application.persistentDataPath);
            // 创建新文件，或者替换已有文件
            Stream fileSteam = File.Open(path, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(fileSteam);
            writer.Write(2);
            hexGrid.Save(writer);
        }

        private void Load(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("File does not exist " + path);
                return;
            }

            using BinaryReader reader = new BinaryReader(File.OpenRead(path));
            int header = reader.ReadInt32();
            if (header <= 2)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }

        public void SelectItem(string name)
        {
            nameInput.text = name;
        }

        private void FillList()
        {
            for (int i = 0; i < listContent.childCount; i++)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }

            string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
            Array.Sort(paths);
            for (int i = 0; i < paths.Length; i++)
            {
                SaveLoadItem item = Instantiate(itemPrefab);
                item.menu = this;
                item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
                item.transform.SetParent(listContent, false);
            }
        }
    }
}