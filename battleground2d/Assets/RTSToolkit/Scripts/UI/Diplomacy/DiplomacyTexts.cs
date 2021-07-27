using System.Collections.Generic;
using UnityEngine;

namespace RTSToolkit
{
    public class DiplomacyTexts : MonoBehaviour
    {
        public static DiplomacyTexts active;
        [HideInInspector] public Dictionary<string, RandomDiplomacyTexts> diplomacyTextsByKey = new Dictionary<string, RandomDiplomacyTexts>();

        void Awake()
        {
            active = this;
        }

        void Start()
        {

        }

        public string GetText(string key, string nationName)
        {
            RandomDiplomacyTexts randomDiplomacyTexts;

            if (diplomacyTextsByKey.TryGetValue(key, out randomDiplomacyTexts))
            {
                DiplomacyText diplomacyText = randomDiplomacyTexts.GetRandomText();
                string fullText = string.Empty;

                if (diplomacyText.nationNameInFront)
                {
                    fullText = nationName + diplomacyText.text;
                }
                else
                {
                    fullText = diplomacyText.text + nationName;
                }

                return fullText;
            }

            return string.Empty;
        }
    }

    [System.Serializable]
    public class RandomDiplomacyTexts
    {
        public string key;
        public List<DiplomacyText> randomTexts = new List<DiplomacyText>();

        public DiplomacyText GetRandomText()
        {
            int randomIndex = Random.Range(0, randomTexts.Count);
            return randomTexts[randomIndex];
        }
    }

    [System.Serializable]
    public class DiplomacyText
    {
        public string text = string.Empty;
        public bool nationNameInFront = false;
    }
}
