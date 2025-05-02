using UnityEngine;
using UnityEngine.Events;

public class MovingAudioObject : MonoBehaviour
{
    // The game object to move
    [SerializeField] private Transform movingObject;

    // Define point A and point B via transform references
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    // Offset for Point A along the AB axis
    [SerializeField] private float pointAOffset = 0f;

    // Controls the movement speed
    [SerializeField] private float velocity = 2f;

    // Unity Events for disappearance and reappearance
    [SerializeField] private UnityEvent onAudioDisappear;
    [SerializeField] private UnityEvent onAudioReappear;

    private void Start()
    {
        // Use this GameObject as the movingObject if none is assigned
        if (movingObject == null)
        {
            movingObject = transform;
        }
    }

    private void Update()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogError("Point A or Point B is not assigned. Please assign both transforms in the Inspector.");
            return;
        }

        // Compute the adjusted Point A position based on the offset
        Vector3 adjustedPointA = pointA.position + (pointB.position - pointA.position).normalized * pointAOffset;

        // Move the object toward point B
        movingObject.position = Vector3.MoveTowards(
            movingObject.position,
            pointB.position,
            velocity * Time.deltaTime
        );

        // When the object reaches point B, trigger the events and reset position
        if (Vector3.Distance(movingObject.position, pointB.position) < 0.001f)
        {
            onAudioDisappear?.Invoke();  // Disappearance event
            movingObject.position = adjustedPointA;  // Reset to adjusted Point A
            onAudioReappear?.Invoke();  // Reappearance event
        }
    }

    // Debug visualization of the path between Adjusted Point A and Point B
    private void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Color gizmoColor = Color.green;
            gizmoColor.a = 0.5f;  // Semi-transparent
            Gizmos.color = gizmoColor;

            // Compute the adjusted Point A position
            Vector3 adjustedPointA = pointA.position + (pointB.position - pointA.position).normalized * pointAOffset;

            // Draw line from adjusted Point A to Point B
            Gizmos.DrawLine(adjustedPointA, pointB.position);

            // Draw spheres at each point
            Gizmos.DrawSphere(adjustedPointA, 0.1f);
            Gizmos.DrawSphere(pointB.position, 0.1f);
        }
    }
}
