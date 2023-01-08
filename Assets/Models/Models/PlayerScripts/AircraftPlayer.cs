using System.Collections;
using System.Collections.Generic;
//using Unity.MLAgents.Actuators;

using UnityEngine;
using UnityEngine.InputSystem;






public class AircraftPlayer  : AircraftAgent 
{
    // Start is called before the first frame update

        [Header("Input Bindings")]

    public InputAction pitchInput;

    public InputAction yawInput;

    public InputAction boostInput;

    public InputAction pauseInput;



   

    public override void Initialize()

    {

       
    
          base.Initialize();

        pitchInput.Enable();

        yawInput.Enable();

        boostInput.Enable();

        pauseInput.Enable();


    }


    private void onDestroy()
    {

        pitchInput.Disable();

        yawInput.Disable();

        boostInput.Disable();
        pauseInput.Disable();

    }


    public override void Heuristic(float[] actionsOut)
    {
       



// Pitch: 1 == up, e == none, -1 == down

 float pitchValue = Mathf. Round(pitchInput. ReadValue<float>());

    // Yaw: 1 == turn right, 0 = none, -1 == turn left
    float yawValue = Mathf. Round (yawInput. ReadValue<float>());

    // Boost: 1 == boost, 0 == no boost
    float boostValue = Mathf. Round(boostInput. ReadValue<float>());

    // convert -1 (down) to discrete value 2
    if (pitchValue == -1f) pitchValue = 2f;

    // convert -1 (turn left) to discrete value 2
    if (yawValue == -1f) yawValue = 2f;


       
        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = Input.GetAxis("Horizontal");
        //continuousActionsOut[1] = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
        //continuousActionsOut[2] = Input.GetAxis("Vertical");

     


        //var continuousActionsOut = actionsOut.DiscreteActions;
        actionsOut[0] = pitchValue;
       actionsOut[1] = yawValue;
        actionsOut[2] = boostValue;


    }
}
