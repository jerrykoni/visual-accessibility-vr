using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class FrustumAudioTrigger : MonoBehaviour
{
    [Header("Frustum Configuration")]
    [SerializeField, Min(0f)]
    private float minDistance = 0f;

    [SerializeField, Min(0f)]
    private float maxDistance = 5f;

    [SerializeField, Min(0f)]
    private float startRadius = 0.03f;

    [SerializeField, Range(0f, 90f)]
    private float apertureDegrees = 20f;

    [Header("Inner Frustum Configuration")]
    [SerializeField, Min(0f)]
    private float innerFrustumRadiusMultiplier = 0.5f;

    [Header("Trigger Settings")]
    [SerializeField]
    private LayerMask interactableLayers = -1;

    [SerializeField]
    private float checkInterval = 0.1f;

    [Header("Audio Settings")]
    [SerializeField, Range(1f, 10f)]
    private float logarithmicFalloff = 2.0f;

    [SerializeField, Range(0.01f, 0.5f)]
    private float minVolumePercent = 0.1f;

    [Header("Debug Visualization")]
    [SerializeField]
    private bool drawDebugFrustum = true;

    [SerializeField]
    private Color frustumColor = new Color(0f, 0.75f, 1f, 0.5f);

    [SerializeField]
    private Color innerFrustumColor = new Color(1f, 0.5f, 0f, 0.5f);

    // Internal variables
    private AudioSource audioSource;
    private float checkTimer;
    private HashSet<GameObject> objectsInFrustum = new HashSet<GameObject>();
    private HashSet<GameObject> objectsInInnerFrustum = new HashSet<GameObject>();
    private Dictionary<GameObject, float> objectDistancesFromAxis = new Dictionary<GameObject, float>();
    private float baseVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = audioSource.volume;
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckForInteractables();
            UpdateAudioVolume();
        }
    }

    private void CheckForInteractables()
    {
        // Store previously detected objects to compare later
        HashSet<GameObject> previouslyDetectedObjects = new HashSet<GameObject>(objectsInFrustum);
        HashSet<GameObject> previouslyInInnerFrustum = new HashSet<GameObject>(objectsInInnerFrustum);

        objectsInFrustum.Clear();
        objectsInInnerFrustum.Clear();
        objectDistancesFromAxis.Clear();

        // Find all possible interactable objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, maxDistance, interactableLayers);

        foreach (Collider collider in colliders)
        {
            GameObject obj = collider.gameObject;
            if (!obj.CompareTag("Interactable"))
                continue;

            Vector3 point = collider.bounds.center;

            if (IsPointInFrustum(point, out float distanceFromAxis, out float radiusAtDistance))
            {
                objectsInFrustum.Add(obj);
                objectDistancesFromAxis[obj] = distanceFromAxis;

                // Check if object is in inner frustum
                float innerRadius = radiusAtDistance * innerFrustumRadiusMultiplier;
                if (distanceFromAxis <= innerRadius)
                {
                    objectsInInnerFrustum.Add(obj);
                }

                // If this is a newly detected object, we might want to trigger something
                if (!previouslyDetectedObjects.Contains(obj))
                {
                    OnObjectEnterFrustum(obj);
                }
            }
        }

        // Check for objects that left the frustum
        foreach (GameObject obj in previouslyDetectedObjects)
        {
            if (!objectsInFrustum.Contains(obj))
            {
                OnObjectExitFrustum(obj);
            }
        }
    }

    private bool IsPointInFrustum(Vector3 point, out float distanceFromAxis, out float radiusAtDistance)
    {
        Vector3 localPoint = transform.InverseTransformPoint(point);
        distanceFromAxis = new Vector2(localPoint.x, localPoint.y).magnitude;

        // Check if the point is in front of the frustum
        if (localPoint.z < minDistance || localPoint.z > maxDistance)
        {
            radiusAtDistance = 0f;
            return false;
        }

        // Calculate the radius at this distance
        radiusAtDistance = GetRadiusAtDistance(localPoint.z);

        // Check if the point is within the cone at this distance
        return distanceFromAxis <= radiusAtDistance;
    }

    private bool IsPointInInnerFrustum(Vector3 point)
    {
        Vector3 localPoint = transform.InverseTransformPoint(point);
        float distanceFromAxis = new Vector2(localPoint.x, localPoint.y).magnitude;

        // Check if the point is in front of the frustum
        if (localPoint.z < minDistance || localPoint.z > maxDistance)
            return false;

        // Calculate the inner radius at this distance
        float innerRadiusAtDistance = GetRadiusAtDistance(localPoint.z) * innerFrustumRadiusMultiplier;

        // Check if the point is within the inner cone at this distance
        return distanceFromAxis <= innerRadiusAtDistance;
    }

    private float GetRadiusAtDistance(float distance)
    {
        // Calculate radius based on the aperture angle and distance
        float endRadius = maxDistance * Mathf.Tan(apertureDegrees * Mathf.Deg2Rad);
        float distanceRatio = Mathf.InverseLerp(minDistance, maxDistance, distance);
        return Mathf.Lerp(startRadius, endRadius, distanceRatio);
    }

    private void UpdateAudioVolume()
    {
        if (objectsInFrustum.Count == 0)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            return;
        }

        // If any object is in the inner frustum, set volume to maximum
        if (objectsInInnerFrustum.Count > 0)
        {
            audioSource.volume = baseVolume;
        }
        else
        {
            // Find the object closest to the central axis
            float closestDistance = float.MaxValue;
            GameObject closestObject = null;

            foreach (var kvp in objectDistancesFromAxis)
            {
                if (kvp.Value < closestDistance)
                {
                    closestDistance = kvp.Value;
                    closestObject = kvp.Key;
                }
            }

            if (closestObject != null)
            {
                // Get the radius at this object's position
                Vector3 localPoint = transform.InverseTransformPoint(closestObject.transform.position);
                float radiusAtDistance = GetRadiusAtDistance(localPoint.z);
                float innerRadius = radiusAtDistance * innerFrustumRadiusMultiplier;

                // Calculate normalized distance from inner frustum boundary (0 = at inner boundary, 1 = at outer boundary)
                float distanceRange = radiusAtDistance - innerRadius;
                float distanceFromInnerBoundary = closestDistance - innerRadius;
                float normalizedDistance = Mathf.Clamp01(distanceFromInnerBoundary / distanceRange);

                // Apply logarithmic falloff:
                // 1. Invert the normalized distance (1 = at inner boundary, 0 = at outer boundary)
                float invertedDistance = 1f - normalizedDistance;

                // 2. Calculate logarithmic volume multiplier
                // This creates a curve that falls off quickly near the outer boundary
                // and approaches max volume more gradually near the inner boundary
                float volumePercent = Mathf.Pow(invertedDistance, logarithmicFalloff);

                // 3. Scale between minVolumePercent and 1
                float volumeMultiplier = Mathf.Lerp(minVolumePercent, 1f, volumePercent);

                // Apply the volume
                audioSource.volume = baseVolume * volumeMultiplier;
            }
        }

        // Make sure audio is playing if objects are in frustum
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void OnObjectEnterFrustum(GameObject obj)
    {
        // Play audio when an object enters the frustum (volume will be set in UpdateAudioVolume)
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        // Additional effects or logic can be added here
        Debug.Log($"Object entered frustum: {obj.name}");
    }

    private void OnObjectExitFrustum(GameObject obj)
    {
        // Stop audio when all objects have left the frustum
        if (objectsInFrustum.Count == 0 && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Additional effects or logic can be added here
        Debug.Log($"Object exited frustum: {obj.name}");
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugFrustum)
            return;

        // Draw frustum in the editor for visualization
        Gizmos.color = frustumColor;

        Vector3 forward = transform.forward;
        Vector3 startPoint = transform.position + forward * minDistance;
        Vector3 endPoint = transform.position + forward * maxDistance;

        // Draw the center line
        Gizmos.DrawLine(startPoint, endPoint);

        // Draw outer frustum circles and connecting lines
        DrawFrustumWireframe(1.0f, frustumColor);

        // Draw inner frustum circles and connecting lines
        DrawFrustumWireframe(innerFrustumRadiusMultiplier, innerFrustumColor);
    }

    private void DrawFrustumWireframe(float radiusMultiplier, Color color)
    {
        Gizmos.color = color;
        Vector3 forward = transform.forward;
        Vector3 startPoint = transform.position + forward * minDistance;
        Vector3 endPoint = transform.position + forward * maxDistance;

        float startRadiusValue = GetRadiusAtDistance(minDistance) * radiusMultiplier;
        float endRadiusValue = GetRadiusAtDistance(maxDistance) * radiusMultiplier;

        // Number of segments to draw the circular wireframe
        int segments = 16;
        float angleStep = 360f / segments;

        Vector3[] startCirclePoints = new Vector3[segments];
        Vector3[] endCirclePoints = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            Quaternion rotation = Quaternion.AngleAxis(angle, forward);

            Vector3 startDirection = rotation * transform.up;
            Vector3 endDirection = rotation * transform.up;

            startCirclePoints[i] = startPoint + startDirection * startRadiusValue;
            endCirclePoints[i] = endPoint + endDirection * endRadiusValue;

            // Draw lines connecting the two circles
            Gizmos.DrawLine(startCirclePoints[i], endCirclePoints[i]);
        }

        // Draw the circles
        for (int i = 0; i < segments; i++)
        {
            int nextI = (i + 1) % segments;

            // Draw start circle segment
            Gizmos.DrawLine(startCirclePoints[i], startCirclePoints[nextI]);

            // Draw end circle segment
            Gizmos.DrawLine(endCirclePoints[i], endCirclePoints[nextI]);
        }
    }
}