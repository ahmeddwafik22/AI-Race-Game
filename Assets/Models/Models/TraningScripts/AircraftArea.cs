using Cinemachine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AircraftArea : MonoBehaviour
{
    // Start is called before the first frame update
    public CinemachineSmoothPath path;

    public GameObject checkPointPrefab;

 
    public List<AircraftAgent> AircraftAgents { get; private set; }
    public List<GameObject> checkPoints { get; private set; }

    public bool traningMode;

    int count = 0;
    public void Awake()
    {
        findAirCraftAgents();

    }

    private void findAirCraftAgents()
    {
        AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();
    }

    public void Start()
    {
        if(checkPoints == null ) createCheckPoints();
    }

    private void createCheckPoints()
    {

        


        checkPoints = new List<GameObject>();
        int numCheckPoints = (int)path.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);

        for (int i = 0; i < numCheckPoints; i++)
        {
            GameObject checkPoint;


            checkPoint = Instantiate<GameObject>(checkPointPrefab);

            //Make checkpoint take the position of the point in path and it's rotation.
            
            checkPoint.transform.parent = path.transform;
           checkPoint.transform.localPosition = path.m_Waypoints[i].position;

            checkPoint.transform.rotation = path.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);
            checkPoints.Add(checkPoint);


        }
    }

    public void ResetAgentPosition(AircraftAgent agent ,bool randomized  = false)
    {

        
            if (AircraftAgents == null) findAirCraftAgents();
            if (checkPoints == null) createCheckPoints();


        if (randomized)
        {
            agent.NextCheckpointIndex = Random.Range(0, checkPoints.Count);


        }
        int previousCheckPointIndex;

        if (agent.NextCheckpointIndex == 0)
            previousCheckPointIndex = agent.NextCheckpointIndex;

        else
            previousCheckPointIndex =  agent.NextCheckpointIndex - 1;




        Debug.Log(agent.NextCheckpointIndex);
        if (previousCheckPointIndex == -1) previousCheckPointIndex = checkPoints.Count - 1;
        float startPosition = path.FromPathNativeUnits(previousCheckPointIndex, CinemachinePathBase.PositionUnits.PathUnits);
        Vector3 basePosition = path.EvaluatePosition(startPosition);
        Quaternion orientation = path.EvaluateOrientation(startPosition);

        Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f) * Random.Range(9f, 10f);

        agent.transform.position = basePosition + orientation * positionOffset;
        agent.transform.rotation = orientation;


    }
}
