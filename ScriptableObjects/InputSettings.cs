using UnityEngine;

[CreateAssetMenu(fileName = "InputSettings", menuName = "Scriptable Objects/InputSettings")]
public class InputSettings : ScriptableObject
{
    [Header("Directional Input And Rotation")]
    public float blockDirectionInputDuration = 0.2f;
    public float deadZoneRadius = 10;
    public float sphereCastRadius = 0.2f;

    [Header("Belt Selection")] 
    public float minTimeBetweenClicks;

    [Header("Camera Movement")] public float moveSpeed = 1.2f;
    public float acceleration = 1f;
    public float dampening; 

    [Header("Zooming")] public float minZoomIn = 5;
    public float maxZoomOut = 30; 
    [Tooltip("How much orthographic size is changed")] public float zoomIncrements = 2f;
    [Tooltip("How long it takes to change the orthographic size")] public float zoomTime = 0.5f; 
    public float minTimeBetweenInputs = 0.01f;

    [Header("Rotation")] public float rotationTime = 0.6f; 
}
