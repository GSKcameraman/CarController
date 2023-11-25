using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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
        Drive();
        Steering();
        ApplyBrake();
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
        wheelColliders.RLWheel.motorTorque = gas * motorpow;
        wheelColliders.RRWheel.motorTorque = gas * motorpow;
    }
    void FrontTorque(){
        wheelColliders.FLWheel.motorTorque = gas * motorpow;
        wheelColliders.FRWheel.motorTorque = gas * motorpow;
    }

    void AllTorque(){

        wheelColliders.RLWheel.motorTorque = gas * motorpow * splitRatio;
        wheelColliders.RRWheel.motorTorque = gas * motorpow * splitRatio;
        wheelColliders.FLWheel.motorTorque = gas * motorpow * (1.0f - splitRatio);
        wheelColliders.FRWheel.motorTorque = gas * motorpow * (1.0f - splitRatio);

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
