using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour
{
    private static readonly float PanSpeed = 20f;
    private static readonly float ZoomSpeedTouch = 0.1f;
    private static readonly float ZoomSpeedMouse = 0.5f;

    private static readonly float[] BoundsX = new float[] { -10f, 5f };
    private static readonly float[] BoundsZ = new float[] { -18f, -4f };
    private static readonly float[] ZoomBounds = new float[] { 10f, 85f };

    private Vector3 lastPanPosition;
    private int panFingerId; // Touch mode only

    private bool wasZoomingLastFrame; // Touch mode only
    private Vector2[] lastZoomPositions; // Touch mode only

    /// <summary>
    /// old
    /// </summary>
    enum Touch2FingerState
    {
        None,
        Operation,
        Scale,
        Slide2Finger,
    }

    enum SlideDirection
    {
        Up,
        down,
        left,
        right,
    }

    private Transform cameraTransform;      // targetcamera
    private Vector3 currentTarget;          // current targeted point (after pan)
    private Camera mainCamera;

    public float distance = 5.0f;
    private float distanceInit;

    public float xSpeed = 2f;
    public float ySpeed = 2f;
    public float zoomSpeed = 0.1f;
    public float panSpeed = 0.05f;

    public int xMinLimit = -360;
    public int xMaxLimit = 360;

    public int yMinLimit = -360;
    public int yMaxLimit = 360;

    public float distanceMinLimit = 0.5f;
    public float distanceMaxLimit = 30.0f;

    private float xPan = 0.0f;
    private float yPan = 0.0f;

    public float xRotation = 0.0f;
    public float yRotation = 0.0f;

    private float xRotationInit = 0.0f;
    private float yRotationInit = 0.0f;

    bool IsActive = false;

    float touchDistance = 0;

    Touch2FingerState fingerState = Touch2FingerState.None;
    SlideDirection slide2Direction = SlideDirection.Up;
    Vector2 finger0Start;
    Vector2 finger1Start;
    float originDistance;

    // Use this for initialization
    void Start () {
        cameraTransform = Camera.main.transform;
        mainCamera = Camera.main;

        distanceInit = distance;
        currentTarget = Vector3.zero;

        Vector3 angles = cameraTransform.transform.eulerAngles;
        xRotation = angles.y;
        yRotation = angles.x;

        xRotationInit = xRotation;
        yRotationInit = yRotation;
    }    

	void Update ()
    {
        if (Input.touchSupported == true)
        {
            HandleTouch();
        }
        else
        {
            HandleMouse();
        }

        Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
        Vector3 d = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * d + currentTarget;
        cameraTransform.transform.rotation = rotation;
        cameraTransform.transform.position = position;
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                xRotation += touch.deltaPosition.x * 0.1f;
                yRotation -= touch.deltaPosition.y * 0.1f;

                xRotation = ClampAngle(xRotation, xMinLimit, xMaxLimit);
                yRotation = ClampAngle(yRotation, yMinLimit, yMaxLimit);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            float tmp = (touch0.position - touch1.position).magnitude;

            if (touch1.phase == TouchPhase.Began || fingerState == Touch2FingerState.None)
            {
                touchDistance = tmp;
                finger0Start = touch0.position;
                finger1Start = touch1.position;
                fingerState = Touch2FingerState.Operation;
            }
            else if (touch1.phase == TouchPhase.Moved || touch0.phase == TouchPhase.Moved && fingerState == Touch2FingerState.Operation)
            {
                //DebugTxt.text = $"Distance : {tmp} - touchDistance {touchDistance} = {tmp - touchDistance}";
                double dist = tmp - touchDistance;
                fingerState = Touch2FingerState.Scale;
                originDistance = distance;
                touchDistance = tmp;
            }
            else if (touch1.phase == TouchPhase.Ended || touch0.phase == TouchPhase.Ended)
            {
                fingerState = Touch2FingerState.None;
            }

            if (fingerState == Touch2FingerState.Scale)
            {
                distance = originDistance + ((finger0Start - finger1Start).magnitude - (touch0.position - touch1.position).magnitude) * 0.005f;
                distance = Mathf.Clamp(distance, distanceMinLimit, distanceMaxLimit);
            }
        }      
    }

    void HandleMouse()
    {
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");

        if (Input.GetMouseButton(0))
        {
            if (Input.GetKey(KeyCode.LeftShift) == true)
            {
                // panning test
                Debug.Log("shift + click");
                xPan = x * panSpeed * distance;
                yPan = y * panSpeed * distance;
                currentTarget += cameraTransform.transform.right * -xPan + cameraTransform.transform.up * -yPan;
            }
            else
            {
                xRotation += x * xSpeed;
                yRotation -= y * ySpeed;

                xRotation = ClampAngle(xRotation, xMinLimit, xMaxLimit);
                yRotation = ClampAngle(yRotation, yMinLimit, yMaxLimit);
            }
        }
        if (Input.GetMouseButton(1))
        {
            xRotation -= x * xSpeed;
            yRotation += y * ySpeed;

            xRotation = ClampAngle(xRotation, xMinLimit, xMaxLimit);
            yRotation = ClampAngle(yRotation, yMinLimit, yMaxLimit);
            Quaternion rot2 = Quaternion.Euler(yRotation, xRotation, 0);
            Vector3 d2 = new Vector3(0.0f, 0.0f, distance);

            currentTarget = rot2 * d2 + cameraTransform.transform.position;
        }
        if (Input.GetMouseButton(2))
        {
            xPan = x * panSpeed * distance;
            yPan = y * panSpeed * distance;
            currentTarget += cameraTransform.transform.right * -xPan + cameraTransform.transform.up * -yPan;
        }
        distance -= Input.mouseScrollDelta.y * zoomSpeed;
        distance = Mathf.Clamp(distance, distanceMinLimit, distanceMaxLimit);
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    private void ZoomCamera(float offset, float speed)
    {
        if (offset == 0)
            return;

        mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView - (offset * speed), ZoomBounds[0], ZoomBounds[1]);
    }
}
