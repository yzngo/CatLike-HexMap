using TMPro;
using UnityEngine;

namespace HexMap.Scripts
{
    public class SaveLoadItem : MonoBehaviour
    {
        public SaveLoadMenu menu;

        public string MapName
        {
            get { return mapName; }
            set
            {
                mapName = value;
                transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = value;
            }
        }

        string mapName;

        public void Select()
        {
            menu.SelectItem(mapName);
        }
    }
}