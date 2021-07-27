using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class SaveLoadCellUI : MonoBehaviour
    {
        public Text rowNumberText;
        public GameObject loadButtonGameObject;
        public GameObject deleteButtonGameObject;

        void Start()
        {

        }

        public void Save()
        {
            SaveLoad.active.Save(rowNumberText.text + ".sav");
            SaveLoadUI.active.Refresh();
        }

        public void Load()
        {
            SaveLoad.active.Load(rowNumberText.text + ".sav");
        }

        public void Delete()
        {
            SaveLoad.active.DeleteSavedGame(rowNumberText.text + ".sav");
            SaveLoadUI.active.Refresh();
        }
    }
}
