using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum DriveType{
    FWD,
    RWD,
    AWD
}


public class CarController : MonoBehaviour
{
    public WheelColliders wheelColliders;
    public WheelMeshs wheelMeshs;
    
    public float gas;
    public float brake;
    public float steering;

    public DriveType driveType;
    [Range(0,1)]
    public float splitRatio = 0.6f; 

    [Range(0,1)]
    public float brakeRatio = 0.7f;

    public float motorpow = 1000;

    public float brakePow = 5000;
    private float slipAngle;
    public bool fourWheelSteer = false;
    [Range(0,1)]
    public float rearSteerDamping = 0.2f;
    private Rigidbody rb;
    private float speed;

    public AnimationCurve steeringCurve;
    public AnimationCurve engineCurve;

    

    public float[] gears;
    int currentGear = 1;
    public float maxRPM = 8000f;
    public float RPMLower = 1000f;
    public float RPMUpper = 3000f;
    float lastRPM = 0;
    //UI
    public TMP_Text speedText;
    public Slider RPMslider;

    public TMP_Text gearText;

    public AudioSource engineSound;
    // Start is called before the first frame update
    void Start()
    {

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        speed = rb.velocity.magnitude;
        //if (speed > 1.0f) print(speed);
        getInput();
    }

    void Steering(){
        float steeringAngle = steering * steeringCurve.Evaluate(speed);
        wheelColliders.FLWheel.steerAngle = steeringAngle;
        wheelColliders.FRWheel.steerAngle = steeringAngle;
        if (fourWheelSteer){
            wheelColliders.RLWheel.steerAngle = -steeringAngle * rearSteerDamping;
            wheelColliders.RRWheel.steerAngle = -steeringAngle * rearSteerDamping;

        }
    }
    void FixedUpdate() {
        UpdateWheels();
        AutoShifting();
        Drive();
        Steering();
        ApplyBrake();
        speedText.text = (rb.velocity.magnitude * 3.6).ToString("0");
        float rpmRatio = getRPM()/maxRPM;
        RPMslider.value = rpmRatio;
        engineSound.pitch = 1 + rpmRatio * 2;

    }

    void getInput(){
        gas = Input.GetAxis("Vertical");
        steering = Input.GetAxis("Horizontal");
        slipAngle = Vector3.Angle(transform.forward,rb.velocity - transform.forward);
        if (slipAngle < 120f){
            if (gas < 0){
                brake =  - gas;
                gas = 0;
            }
            else{
                brake = 0;
            }

        }
        else if (rb.velocity.magnitude > 1.0f){
            if (gas > 0){
                brake =  gas;
                gas = 0;
            }
            else{
                brake = 0;
            }

        }
        else{
            brake = 0;
        }
        //print(slipAngle);
    }

    void Drive(){
        if (driveType == DriveType.FWD){
            FrontTorque();
        }
        else if (driveType == DriveType.RWD){
            RearTorque();
        }
        else{
            AllTorque();
        }
    }
    void RearTorque(){
        float realpow = motorpow * engineCurve.Evaluate(getRPM()/maxRPM);
        wheelColliders.RLWheel.motorTorque = gas * realpow * 0.5f;
        wheelColliders.RRWheel.motorTorque = gas * realpow * 0.5f;
    }
    void FrontTorque(){
        float realpow = motorpow * engineCurve.Evaluate(getRPM()/maxRPM);
        wheelColliders.FLWheel.motorTorque = gas * realpow * 0.5f;
        wheelColliders.FRWheel.motorTorque = gas * realpow * 0.5f;
    }

    void AllTorque(){
        float realpow = motorpow * engineCurve.Evaluate(getRPM()/maxRPM);
        wheelColliders.RLWheel.motorTorque = gas * realpow * splitRatio * 0.5f;
        wheelColliders.RRWheel.motorTorque = gas * realpow * splitRatio * 0.5f;
        wheelColliders.FLWheel.motorTorque = gas * realpow * (1.0f - splitRatio) * 0.5f;
        wheelColliders.FRWheel.motorTorque = gas * realpow * (1.0f - splitRatio) * 0.5f;

    }

    void ApplyBrake(){
        wheelColliders.RLWheel.brakeTorque = brake * brakePow * brakeRatio;
        wheelColliders.RRWheel.brakeTorque = brake * brakePow * brakeRatio;
        wheelColliders.FLWheel.brakeTorque = brake * brakePow * (1.0f - brakeRatio);
        wheelColliders.FRWheel.brakeTorque = brake * brakePow * (1.0f - brakeRatio);

    }

    void UpdateWheels(){
        UpdateWheel(wheelColliders.FLWheel,wheelMeshs.FLWheel);
        UpdateWheel(wheelColliders.FRWheel,wheelMeshs.FRWheel);
        UpdateWheel(wheelColliders.RLWheel,wheelMeshs.RLWheel);
        UpdateWheel(wheelColliders.RRWheel,wheelMeshs.RRWheel);

    }
    void UpdateWheel(WheelCollider collider, MeshRenderer mesh){
        Quaternion quat;
        Vector3 pos;
        collider.GetWorldPose(out pos, out quat);
        mesh.transform.position = pos;
        mesh.transform.rotation = quat;
    }

    float getRPM(){
        WheelCollider wheelL;
        WheelCollider wheelR;
        if (driveType == DriveType.FWD){
            wheelL = wheelColliders.FLWheel;
            wheelR = wheelColliders.FRWheel;
        }
        else{
            wheelL = wheelColliders.RLWheel;
            wheelR = wheelColliders.RRWheel;
        }
        float rpm = Math.Min(Math.Abs(wheelL.rpm) , Math.Abs(wheelR.rpm)) * gears[currentGear + 1] * 3;
        float rpmLerp = Mathf.Lerp(lastRPM,rpm,Time.deltaTime * 1.0f);
        if (rpmLerp < RPMLower * 0.8f){
            rpmLerp = RPMLower * 0.8f;
        }
        else if (rpmLerp > maxRPM * 1.1f){
            rpmLerp = maxRPM * 0.9f;
        }
        
        lastRPM = rpmLerp;
        return rpmLerp;
        
        
    }

    void AutoShifting(){
        float RPM = getRPM();
        if (gas < 0){
            currentGear = -1;
            gearText.text = "R";
        }
        else if (gas > 0 && currentGear <= 0){
            currentGear = 1;
            gearText.text = currentGear.ToString();
        }
        if (RPM < RPMLower && currentGear > 1){
            currentGear--;
            gearText.text = currentGear.ToString();
        }
        else if (RPM > RPMUpper && currentGear < gears.Length - 2 && gas > 0){
            currentGear ++;
            gearText.text = currentGear.ToString();

        }
    }
}

[System.Serializable]
public class WheelColliders{
    public WheelCollider FLWheel;
    public WheelCollider FRWheel;
    public WheelCollider RLWheel;
    public WheelCollider RRWheel;

}

[System.Serializable]
public class WheelMeshs{
    public MeshRenderer FLWheel;
    public MeshRenderer FRWheel;
    public MeshRenderer RLWheel;
    public MeshRenderer RRWheel;

}
