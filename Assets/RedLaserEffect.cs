using UnityEngine;

public class RedLaserEffect : MonoBehaviour
{
    [Header("Settings")]
    public Transform firePoint;      // Where the laser starts
    public float maxBeamDistance = 50f;
    public Color laserColor = Color.red;
    public float beamWidth = 0.1f;

    private LineRenderer laserLine;

    void Start()
    {
        // Setup the Line Renderer component
        laserLine = gameObject.AddComponent<LineRenderer>();

        // Apply visual settings
        laserLine.startWidth = beamWidth;
        laserLine.endWidth = beamWidth;
        laserLine.material = new Material(Shader.Find("Sprites/Default")); // Basic glowy shader
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;

        // Hide the laser initially
        laserLine.enabled = false;
    }

    public void ActivateLaser(bool isActive)
    {
        laserLine.enabled = isActive;
    }

    void Update()
    {
        if (laserLine.enabled)
        {
            UpdateLaserPositions();
        }
    }

    void UpdateLaserPositions()
    {
        // Start the laser at the fire point
        laserLine.SetPosition(0, firePoint.position);

        // Calculate the end point (Check if we hit something)
        RaycastHit hit;
        Vector3 endPosition = firePoint.position + (firePoint.forward * maxBeamDistance);

        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, maxBeamDistance))
        {
            // If the laser hits a wall/enemy, stop it there
            endPosition = hit.point;
        }

        laserLine.SetPosition(1, endPosition);
    }
}