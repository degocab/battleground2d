using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSToolkit
{
    public class JourneysUI : MonoBehaviour
    {
        public static JourneysUI active;

        public GameObject positionCellPrefab;
        Transform prefabParent;

        List<JourneysPosCellUI> cells = new List<JourneysPosCellUI>();

        public GameObject closeMenu;
        public Journeys.Journey openJourney;

        public Text distanceLabel;
        public Text timeLabel;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            prefabParent = positionCellPrefab.transform.parent;
        }

        public void OpenJourneyMenu(Journeys.Journey jrn)
        {
            openJourney = jrn;
            CleanPositions();
            LoadExistingJourneyPositions();
            closeMenu.SetActive(true);
        }

        public void LoadExistingJourneyPositions()
        {
            for (int i = 0; i < openJourney.positions.Count; i++)
            {
                AddPosition();
                JourneysPosCellUI journeysPosCellUI = cells[cells.Count - 1];
                journeysPosCellUI.northPos.text = (openJourney.positions[i].x).ToString();
                journeysPosCellUI.eastPos.text = (-openJourney.positions[i].z).ToString();
            }

            UpdateDistanceAndTimeLabels();
        }

        public void AddPosition()
        {
            GameObject go = (GameObject)Instantiate(positionCellPrefab, prefabParent);
            go.SetActive(true);
            cells.Add(go.GetComponent<JourneysPosCellUI>());
            UpdateDistanceAndTimeLabels();
        }

        public void RemovePosition(JourneysPosCellUI journeysPosCellUI)
        {
            cells.Remove(journeysPosCellUI);
            Destroy(journeysPosCellUI.gameObject);
            UpdateDistanceAndTimeLabels();
        }

        public void CleanPositions()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Destroy(cells[i].gameObject);
            }

            cells.Clear();
            UpdateDistanceAndTimeLabels();
        }

        public void StartJourney()
        {
            if (cells.Count > 0)
            {
                closeMenu.SetActive(false);
                openJourney.positions.Clear();

                for (int i = 0; i < cells.Count; i++)
                {
                    bool parsed = true;
                    float x;
                    float z;

                    if (!float.TryParse(cells[i].northPos.text, out x))
                    {
                        parsed = false;
                    }

                    if (!float.TryParse(cells[i].eastPos.text, out z))
                    {
                        parsed = false;
                    }

                    if (parsed)
                    {
                        openJourney.positions.Add(new Vector3(x, 0f, -z));
                    }
                }

                openJourney.StartJourney();
            }
        }

        public void UpdateDistanceAndTimeLabels()
        {
            Vector3 meanPos = openJourney.GetMeanPosition();
            float dist = 0f;
            List<Vector3> pos = new List<Vector3>();

            for (int i = 0; i < cells.Count; i++)
            {
                bool parsed = true;
                float x;
                float z;

                if (!float.TryParse(cells[i].northPos.text, out x))
                {
                    parsed = false;
                }

                if (!float.TryParse(cells[i].eastPos.text, out z))
                {
                    parsed = false;
                }

                if (parsed)
                {
                    pos.Add(new Vector3(x, 0f, -z));
                }
            }

            for (int i = 0; i < pos.Count; i++)
            {
                if (i == 0)
                {
                    dist = dist + (pos[i] - meanPos).magnitude;
                }
                else
                {
                    dist = dist + (pos[i] - pos[i - 1]).magnitude;
                }
            }

            if (dist < 1000)
            {
                distanceLabel.text = ((int)(dist)).ToString() + "m";
            }
            else
            {
                distanceLabel.text = (dist / 1000f).ToString("#.0") + "km";
            }

            float speed = openJourney.MinimumSpeed();
            timeLabel.text = TimeOfDay.DaysHoursMinutes(dist / speed);
        }
    }
}
