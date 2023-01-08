using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
//using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AircraftAgent : Agent
{
   
    
        [Header("Movement Parameters")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Explosion Stuff")]
        [Tooltip("The aircraft mesh that will disappear on explosion")]
        public GameObject meshObject;

        [Tooltip("The game object of the explosion particle effect")]
        public GameObject explosionEffect;

        [Header("Training")]
        [Tooltip("Number of steps to time out after in training")]
        public int stepTimeout = 300;

        public int NextCheckpointIndex { get; set; }

        // Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;

        // When the next step timeout will be during training
        private float nextStepTimeout;

        // Whether the aircraft is frozen (intentionally not flying)
        private bool frozen = false;

        // Controls
        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;

        /// <summary>
        /// Called when the agent is first initialized
        /// </summary>
        public override void Initialize()
        {
            area = GetComponentInParent<AircraftArea>();

            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();

            // Override the max step set in the inspector
            // Max 5000 steps if training, infinite steps if racing
            MaxStep = area.traningMode ? 5000 : 0;
        }

        /// <summary>
        /// Called when a new episode begins
        /// </summary>
        public override void OnEpisodeBegin()
        {
        // Reset the velocity, position, and orientation
      
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this, randomized: area.traningMode);

            // Update the step timeout if training
            if (area.traningMode) nextStepTimeout = StepCount + stepTimeout;
        }

    /// <summary>
    /// Read action inputs from vectorAction
    /// </summary>
    /// <param name="vectorAction">The chosen actions</param>

    public override void OnActionReceived(float[] actions)
    {

       
        if (frozen) return;


        pitchChange = actions[0];

        if (pitchChange == 2) pitchChange = -1;


        yawChange = actions[1];
        if (yawChange == 2) yawChange = -1;


        boost = actions[2] == 1;

        if (boost && !trail.emitting) trail.Clear();

        trail.emitting = boost;



        ProcessMovement();

        //Small negative reward everyTime
        if (area.traningMode)
        {
            AddReward(-1f / MaxStep);

            //To limit time of traning 
            if (StepCount > nextStepTimeout)
            {
                AddReward(-0.5f);
                EndEpisode();
            }
        }

        



        Vector3 localCheckPointDir = VectorToNextCheckpoint();

        if (localCheckPointDir.magnitude < Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0))
        {
            GotCheckpoint();
        }

    }

    /// <summary>
    /// Collects observations used by agent to make decisions
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
        {
            // Observe aircraft velocity (1 Vector3 = 3 values)
            sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity));

            // Where is the next checkpoint? (1 Vector3 = 3 values)
            sensor.AddObservation(VectorToNextCheckpoint());

            // Orientation of the next checkpoint (1 Vector3 = 3 values)
            Vector3 nextCheckpointForward = area.checkPoints[NextCheckpointIndex].transform.forward;
            sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));

            // Total Observations = 3 + 3 + 3 = 9
        }

    /// <summary>
    /// In this project, we only expect Heuristic to be used on AircraftPlayer
    /// </summary>
    /// <param name="actionsOut">Empty array</param>
    /// 

    
    public override void Heuristic(float[] actionsOut)
        {


            Debug.LogError("Heuristic() was called on " + gameObject.name +
                " Make sure only the AircraftPlayer is set to Behavior Type: Heuristic Only.");
        }

        /// <summary>
        /// Prevent the agent from moving and taking actions
        /// </summary>
        public void FreezeAgent()
        {
       // Debug.Log("disabling player");
            Debug.Assert(area.traningMode == false, "Freeze/Thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        /// <summary>
        /// Resume agent movement and actions
        /// </summary>
        public void ThawAgent()
        {
            Debug.Assert(area.traningMode == false, "Freeze/Thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
        }

        /// <summary>
        /// Gets a vector to the next checkpoint the agent needs to fly through
        /// </summary>
        /// <returns>A local-space vector</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.checkPoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        /// <summary>
        /// Called when the agent flies through the correct checkpoint
        /// </summary>
        private void GotCheckpoint()
        {
            // Next checkpoint reached, update
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.checkPoints.Count;

            if (area.traningMode)
            {
                AddReward(.5f);
                nextStepTimeout = StepCount + stepTimeout;
            }
        }

        /// <summary>
        /// Calculate and apply movement
        /// </summary>
        private void ProcessMovement()
        {
            // Calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;

       
            // Apply forward thrust
            rigidbody.AddForce(- transform.forward * thrust * boostModifier, ForceMode.Force);

            // Get the current rotation
            Vector3 curRot = transform.rotation.eulerAngles;

            // Calculate the roll angle (between -180 and 180)
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if (yawChange == 0f)
            {
                // Not turning; smoothly roll toward center
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                // Turning; roll in opposite direction of turn
                rollChange = -yawChange;
            }

            // Calculate smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            // Calculate new pitch, yaw, and roll. Clamp pitch and roll.
            float pitch = curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            float roll = curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;
            if (roll > 180f) roll -= 360f;
            roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

            // Set the new rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        /// <summary>
        /// React to entering a trigger
        /// </summary>
        /// <param name="other">The collider entered</param>
        private void OnTriggerEnter(Collider other)
        {

     //   Debug.Log("Triggered with " +other.name);


        if (other.transform.CompareTag("checkpoint") &&
                other.gameObject == area.checkPoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }


        }

        /// <summary>
        /// React to collisions
        /// </summary>
        /// <param name="collision">Collision info</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                 Debug.Log("collided with " + collision.collider.name);
                // We hit something that wasn't another agent
                if (area.traningMode)
                {
                    AddReward(-1f);
                    EndEpisode();
                }
                else
                {
                Debug.Log("Collided");
                    StartCoroutine(ExplosionReset());
                }
            }
        }

        /// <summary>
        /// Resets the aircraft to the most recent complete checkpoint
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator ExplosionReset()
        {
            //FreezeAgent();

            // Disable aircraft mesh object, enable explosion
            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            // Disable explosion, re-enable aircraft mesh
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);

            // Reset position
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }
    
}
