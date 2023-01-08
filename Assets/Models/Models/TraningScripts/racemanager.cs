using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AirCraft {
    public class racemanager : MonoBehaviour
    {
        // Start is called before the first frame update
        public int numLaps = 2;


        public float checkpointbonustime = 15f;

        [Serializable]

        public struct difficultymodel
        {
            public GameDifficulty difficulty;
            public NNModel model;
        }


        public List<difficultymodel> difficultymodels;


        

        public AircraftAgent FollowAgent { get; private set; }

        public Camera ActiveCamera { get; private set;}

        private CinemachineVirtualCamera virtualCamera;
        private countdownuicontroller countdownUI;
        private pausemenucontroller pauseMenu;
        private HUDcontroller hud;
        private GameOver gameoverUI;
        private AircraftArea aircraftArea;
        private AircraftPlayer aircraftPlayer;
        private List<AircraftAgent> sortedaircraftAgents;


        private float lastResumeTime = 0f;
        private float previouslyElapsedTime = 0f;

        private float lastplaceupdate=0f;

        private Dictionary<AircraftAgent, AircraftStatus> statuses;

        private class AircraftStatus
        {


            public int checkpointindex = 0;
            public int lap = 0;
            public int place = 0;
            public float timeremaining = 0f;

        }


        public float RaceTime
        {
            get
            {
                if (GameManager.Instance.GameState == GameState.Playing)
                {
                    return previouslyElapsedTime + Time.time -lastResumeTime;
                }
                else if (GameManager.Instance.GameState == GameState.Paused)
                {
                    return previouslyElapsedTime;
                }
                else
                {
                    return 0f;
                }
            }
        }



        private void Awake()
        {
            hud = FindObjectOfType<HUDcontroller>();
            countdownUI = FindObjectOfType<countdownuicontroller>();
            pauseMenu = FindObjectOfType<pausemenucontroller>();
            gameoverUI = FindObjectOfType<GameOver>();
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            aircraftArea = FindObjectOfType<AircraftArea>();
            ActiveCamera = FindObjectOfType<Camera>();
        }



        private void Start()
        {
            GameManager.Instance.OnStateChange += OnStateChange;

            // Choose a default agent for the camera to follow (in case we can't find a player)
            FollowAgent = aircraftArea.AircraftAgents[0];
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                agent.FreezeAgent();
                if (agent.GetType() == typeof(AircraftPlayer))
                {
                    // Found the player, follow it
                    FollowAgent = agent;
                    aircraftPlayer = (AircraftPlayer)agent;
                    aircraftPlayer.pauseInput.performed += PauseInputPerformed;
                }
                else
                {
                    // Set the difficulty
                    agent.SetModel(GameManager.Instance.GameDifficulty.ToString(),
                        difficultymodels.Find(x => x.difficulty == GameManager.Instance.GameDifficulty).model);
                }
            }

            // Tell the camera and HUD what to follow
            Debug.Assert(virtualCamera != null, "Virtual Camera was not specified");
            virtualCamera.Follow = FollowAgent.transform;
            virtualCamera.LookAt = FollowAgent.transform;
            hud.FollowAgent = FollowAgent;

            // Hide UI
            hud.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive(false);
            countdownUI.gameObject.SetActive(false);
            gameoverUI.gameObject.SetActive(false);

            // Start the race
            StartCoroutine(StartRace());
        }


        private IEnumerator StartRace()
        {
            // Show countdown
            countdownUI.gameObject.SetActive(true);
            yield return countdownUI.StartCountdown();

            // Initialize agent status tracking
            statuses = new Dictionary<AircraftAgent, AircraftStatus>();
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                AircraftStatus status = new AircraftStatus();
                status.lap = 1;
                status.timeremaining = checkpointbonustime;

                statuses.Add(agent, status);

               
            }

            // Begin playing
            GameManager.Instance.GameState = GameState.Playing;
        }


        private void OnStateChange()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                // Start/resume game time, show the HUD, thaw the agents
                lastResumeTime = Time.time;
                hud.gameObject.SetActive(true);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.ThawAgent();
            }
            else if (GameManager.Instance.GameState == GameState.Paused)
            {
                // Pause the game time, freeze the agents
                previouslyElapsedTime += Time.time - lastResumeTime;
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.FreezeAgent();
            }
            else if (GameManager.Instance.GameState == GameState.Gameover)
            {
                // Pause game time, hide the HUD, freeze the agents
                previouslyElapsedTime += Time.time - lastResumeTime;
                hud.gameObject.SetActive(false);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.FreezeAgent();

                // Show game over screen
                gameoverUI.gameObject.SetActive(true);
            }
            else
            {
                // Reset time
                lastResumeTime = 0f;
                previouslyElapsedTime = 0f;
            }
        }


        private void PauseInputPerformed(InputAction.CallbackContext obj)
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                GameManager.Instance.GameState = GameState.Paused;
                pauseMenu.gameObject.SetActive(true);
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                // Update the place list every half second
                if (lastplaceupdate + .5f < Time.fixedTime)
                {
                    lastplaceupdate = Time.fixedTime;

                    if (sortedaircraftAgents == null)
                    {
                        // Get a copy of the list of agents for sorting
                        sortedaircraftAgents = new List<AircraftAgent>(aircraftArea.AircraftAgents);
                    }

                    // Recalculate race places
                   sortedaircraftAgents.Sort((a, b) => PlaceComparer(a, b));
                    for (int i = 0; i < sortedaircraftAgents.Count; i++)
                    {
                        statuses[sortedaircraftAgents[i]].place = i + 1;
                    }
                }

                // Update agent statuses
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
                {
                    AircraftStatus status = statuses[agent];

                    // Update agent lap
                    if (status.checkpointindex != agent.NextCheckpointIndex)
                    {
                        status.checkpointindex = agent.NextCheckpointIndex;
                        status.timeremaining = checkpointbonustime;

                        if (status.checkpointindex == 0)
                        {
                            status.lap++;
                            if (agent == FollowAgent && status.lap > numLaps)
                            {
                                GameManager.Instance.GameState = GameState.Gameover;
                            }
                        }
                    }

                    // Update agent time remaining
                    status.timeremaining = Mathf.Max(0f, status.timeremaining - Time.fixedDeltaTime);
                    if (status.timeremaining == 0f)
                    {
                        aircraftArea.ResetAgentPosition(agent);
                        status.timeremaining = checkpointbonustime;
                    }
                }
            }
        }

        private int PlaceComparer(AircraftAgent a, AircraftAgent b)
        {
            AircraftStatus statusA = statuses[a];
            AircraftStatus statusB = statuses[b];
            int checkpointA = statusA.checkpointindex + (statusA.lap - 1) * aircraftArea.checkPoints.Count;
            int checkpointB = statusB.checkpointindex + (statusB.lap - 1) * aircraftArea.checkPoints.Count;
            if (checkpointA == checkpointB)
            {
                // Compare distances to the next checkpoint
                Vector3 nextCheckpointPosition = GetAgentNextCheckpoint(a).position;
                int compare = Vector3.Distance(a.transform.position, nextCheckpointPosition)
                    .CompareTo(Vector3.Distance(b.transform.position, nextCheckpointPosition));
                return compare;
            }
            else
            {
                // Compare number of checkpoints hit. The agent with more checkpoints is
                // ahead (lower place), so we flip the compare
                int compare = -1 * checkpointA.CompareTo(checkpointB);
                return compare;
            }
        }

        public Transform GetAgentNextCheckpoint(AircraftAgent agent)
        {
            return aircraftArea.checkPoints[statuses[agent].checkpointindex].transform;
        }

        public int GetAgentLap(AircraftAgent agent)
        {
            return statuses[agent].lap;
        }

        public string GetAgentPlace(AircraftAgent agent)
        {
            int place = statuses[agent].place;
            if (place <= 0)
            {
                return string.Empty;
            }

            if (place >= 11 && place <= 13) return place.ToString() + "th";

            switch (place % 10)
            {
                case 1:
                    return place.ToString() + "st";
                case 2:
                    return place.ToString() + "nd";
                case 3:
                    return place.ToString() + "rd";
                default:
                    return place.ToString() + "th";
            }
        }

        public float GetAgentTime(AircraftAgent agent)
        {
            return statuses[agent].timeremaining;
        }


        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChange -= OnStateChange;
            if (aircraftPlayer != null) aircraftPlayer.pauseInput.performed -= PauseInputPerformed;
        }

    }
}
