using UnityEngine;
using UnityEngine.Events;

public class MovingAudioObject : MonoBehaviour
{
    // Reference to the moving audio GameObject (if left unassigned, this script's GameObject is used).
    [SerializeField] private Transform movingObject;

    // Defines the direction from point A to point B.
    [SerializeField] private Vector3 moveDirection = Vector3.right;

    // Sets the distance between point A and point B.
    [SerializeField] private float distance = 5f;

    // Controls the movement speed.
    [SerializeField] private float velocity = 2f;

    // Unity Events to control audio actions on disappearance and reappearance.
    [SerializeField] private UnityEvent onAudioDisappear;
    [SerializeField] private UnityEvent onAudioReappear;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Start()
    {
        // Use this GameObject if none is assigned.
        if (movingObject == null)
        {
            movingObject = transform;
        }

        // Store the start point (point A) and calculate point B.
        startPosition = movingObject.position;
        endPosition = startPosition + moveDirection.normalized * distance;
    }

    private void Update()
    {
        // Move the object toward point B.
        movingObject.position = Vector3.MoveTowards(
            movingObject.position,
            endPosition,
            velocity * Time.deltaTime
        );

        // When close to point B, trigger the configured events and reset position.
        if (Vector3.Distance(movingObject.position, endPosition) < 0.001f)
        {
            // Invoke the event to handle audio when the object is about to disappear.
            onAudioDisappear?.Invoke();

            // Reset the position to point A.
            movingObject.position = startPosition;

            // Invoke the event to handle audio when the object reappears.
            onAudioReappear?.Invoke();
        }
    }

    // Optional: Visualize the movement path for debugging.
    private void OnDrawGizmos()
    {
        Color gizmoColor = Color.green;
        gizmoColor.a = 0.5f;
        Gizmos.color = gizmoColor;

        Vector3 startPoint;
        if (Application.isPlaying && movingObject != null)
        {
            startPoint = startPosition;
        }
        else if (movingObject != null)
        {
            startPoint = movingObject.position;
        }
        else
        {
            startPoint = transform.position;
        }

        Vector3 targetPoint = startPoint + moveDirection.normalized * distance;
        Gizmos.DrawLine(startPoint, targetPoint);
        Gizmos.DrawSphere(startPoint, 0.1f);
        Gizmos.DrawSphere(targetPoint, 0.1f);
    }
}
