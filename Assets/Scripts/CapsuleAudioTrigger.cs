using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CapsuleCollider))]
public class CapsuleAudioTrigger : MonoBehaviour
{
    // Select which layer(s) are allowed to trigger the audio.
    [Tooltip("Select the layer(s) that are allowed to trigger the audio.")]
    [SerializeField]
    private LayerMask triggerLayer;

    // Speed at which the audio fades in/out. Higher values result in faster transitions.
    [Tooltip("Speed at which the audio volume fades in/out. Higher values result in faster transitions.")]
    [SerializeField]
    private float fadeSpeed = 3f;

    // AnimationCurve defining the volume attenuation. 
    // The X axis should be normalized distance (0 = at the reference, 1 = at maxDistance), 
    // and the Y axis is the volume multiplier.
    [Tooltip("Animation curve for volume attenuation. X: normalized distance (0 at reference, 1 at maxDistance), Y: volume multiplier.")]
    [SerializeField]
    private AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // Cached AudioSource and CapsuleCollider components.
    private AudioSource audioSource;
    private CapsuleCollider capsuleCollider;

    // A rough measure of the maximum distance from the capsule's center (or bounds) to its outer edge.
    private float maxDistance;

    // Keep track of valid colliders currently in the trigger.
    private HashSet<Collider> validColliders = new HashSet<Collider>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Warn if no AudioClip is assigned.
        if (audioSource.clip == null)
        {
            Debug.LogWarning("CapsuleAudioTrigger: No audio clip is assigned to the AudioSource. Please assign one in the Inspector.");
        }

        // Ensure the CapsuleCollider is set as a trigger.
        if (!capsuleCollider.isTrigger)
        {
            Debug.LogWarning("CapsuleAudioTrigger: CapsuleCollider is not set as a trigger. Enabling it automatically.");
            capsuleCollider.isTrigger = true;
        }

        // Determine a maximum distance using the collider's bounds.
        // For a capsule, this is an approximation based on the bounding box.
        maxDistance = capsuleCollider.bounds.extents.magnitude;
    }

    void OnTriggerEnter(Collider other)
    {
        // Only add the collider if its layer is selected.
        if (((1 << other.gameObject.layer) & triggerLayer) != 0)
        {
            validColliders.Add(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Remove the collider if it exists in our set.
        if (((1 << other.gameObject.layer) & triggerLayer) != 0)
        {
            validColliders.Remove(other);
        }
    }

    void Update()
    {
        // Clean up the set by removing any null or inactive colliders.
        validColliders.RemoveWhere(col => col == null || !col.gameObject.activeInHierarchy);

        float targetVolume = 0f;

        if (validColliders.Count > 0)
        {
            // Automatically compute the reference point:
            // Start with the capsule's center.
            Vector3 capsuleCenter = transform.TransformPoint(capsuleCollider.center);
            // Then add an offset along transform.forward multiplied by half the capsule's height.
            Vector3 referencePoint = capsuleCenter - transform.forward * (capsuleCollider.height * 0.5f);

            float highestVolume = 0f; // Use the maximum volume from all valid colliders.

            foreach (Collider col in validColliders)
            {
                // Use the collider's closest point for accurate distance measurement.
                Vector3 closestPoint = col.ClosestPoint(referencePoint);
                float distance = Vector3.Distance(closestPoint, referencePoint);

                // Normalize the distance relative to 2 * maxDistance (since the reference is at the capsule's edge).
                float normDistance = Mathf.Clamp01(distance / (2 * maxDistance));

                // Evaluate the volume based on the curve.
                float vol = volumeCurve.Evaluate(normDistance);

                if (vol > highestVolume)
                {
                    highestVolume = vol;
                }
            }
            targetVolume = highestVolume;
        }
        else
        {
            targetVolume = 0f;
        }

        // Smoothly fade the audio volume towards the target volume.
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);

        // If the faded volume is very small, snap it to 0 to avoid minute fluctuations.
        if (audioSource.volume < 0.01f)
        {
            audioSource.volume = 0f;
        }

        // Start playback if valid objects exist and the audio is not already playing.
        if (validColliders.Count > 0 && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        // Stop the audio if no valid objects remain and the volume is essentially 0.
        if (validColliders.Count == 0 && audioSource.isPlaying && audioSource.volume < 0.01f)
        {
            audioSource.Stop();
        }
    }

    // Visualization using Gizmos to show where the reference point is.
    void OnDrawGizmos()
    {
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null) return;
        }
        // Compute capsule center in world space.
        Vector3 capsuleCenter = transform.TransformPoint(capsuleCollider.center);
        // Compute the default reference point (using transform.forward).
        Vector3 referencePoint = capsuleCenter - transform.forward * (capsuleCollider.height * 0.5f);

        // Draw a green sphere at the capsule's center.
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(capsuleCenter, 0.05f);

        // Draw a red sphere at the reference point.
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(referencePoint, 0.05f);

        // Draw a yellow line connecting the center and the reference point.
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(capsuleCenter, referencePoint);

        // Optionally, label the reference point.
#if UNITY_EDITOR
        UnityEditor.Handles.Label(referencePoint, "Reference Point");
#endif
    }
}
