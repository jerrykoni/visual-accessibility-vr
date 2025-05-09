using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AudioCycleManager : MonoBehaviour
{
    // The player's transform to measure distances.
    [SerializeField] private Transform playerTransform;

    // The tag that AudioSources must have to be considered.
    [SerializeField] private string targetAudioTag = "Audio";

    // The distance threshold beyond which the cycle resets.
    [SerializeField] private float distanceThreshold = 5f;

    // Internal reference to the collider on this GameObject.
    private Collider myCollider;

    // Tracks the currently playing audio (by list index).
    private int currentAudioIndex = -1;
    private AudioSource currentAudioPlaying = null;

    // Records player position when the cycle started.
    private Vector3 cycleStartPosition;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();

        if (myCollider == null)
        {
            Debug.LogError("AudioCycleManager requires a Collider component on this GameObject.");
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform is not assigned on " + gameObject.name);
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            CycleAudioSource();
        }
    }

    /// <summary>
    /// Called from an external script (e.g., on a button press) to cycle through AudioSources.
    /// Only AudioSources with the specified tag (targetAudioTag) and within the collider's bounds are considered.
    /// 
    /// New functionality: during a cycle, if the player moves beyond the distanceThreshold from where the cycle began,
    /// then on the next call the current audio is immediately disabled (skipping the normal progression) and the cycle resets.
    /// On the following call, the cycle will restart using updated positions.
    /// </summary>
    public void CycleAudioSource()
    {
        if (myCollider == null || playerTransform == null)
        {
            Debug.LogWarning("Missing Collider or Player Transform reference.");
            return;
        }

        // If we're in the middle of a cycle (an audio is actively playing), check if the player moved beyond the threshold.
        if (currentAudioIndex != -1)
        {
            float distanceMoved = Vector3.Distance(playerTransform.position, cycleStartPosition);
            if (distanceMoved > distanceThreshold)
            {
                // Player has moved too far: skip to the "last step" by stopping the current audio.
                if (currentAudioPlaying != null)
                {
                    currentAudioPlaying.Stop();
                }
                // Reset cycle so that on the next call a fresh cycle will begin.
                currentAudioIndex = -1;
                cycleStartPosition = playerTransform.position;
                Debug.Log("Player moved " + distanceMoved + " units beyond threshold (" + distanceThreshold + "). Audio reset.");
                return;
            }
        }
        else
        {
            // Starting a new cycle, record the player's position.
            cycleStartPosition = playerTransform.position;
        }

        // Find all active AudioSources in the scene using the new API.
        AudioSource[] potentialSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<AudioSource> sourcesInCollider = new List<AudioSource>();

        foreach (AudioSource audio in potentialSources)
        {
            // Only consider AudioSources that have the desired tag and are within the collider's bounds.
            if (audio.gameObject.CompareTag(targetAudioTag) && myCollider.bounds.Contains(audio.transform.position))
            {
                sourcesInCollider.Add(audio);
            }
        }

        if (sourcesInCollider.Count == 0)
        {
            Debug.LogWarning("No AudioSources found within collider bounds with tag: " + targetAudioTag);
            return;
        }

        // Sort the audio sources by distance from the player (closest first).
        sourcesInCollider.Sort((a, b) =>
        {
            // Use squared distances for a minor optimization.
            float distA = (playerTransform.position - a.transform.position).sqrMagnitude;
            float distB = (playerTransform.position - b.transform.position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        // Safety: reset if our current index is out of range.
        if (currentAudioIndex >= sourcesInCollider.Count)
        {
            currentAudioIndex = -1;
        }

        // Normal cycle behavior:
        // If no audio is playing, start with the closest source.
        if (currentAudioIndex == -1)
        {
            currentAudioIndex = 0;
            sourcesInCollider[currentAudioIndex].Play();
            currentAudioPlaying = sourcesInCollider[currentAudioIndex];
            Debug.Log("Playing new cycle. Audio index: " + currentAudioIndex);
        }
        else
        {
            // Stop the currently playing source.
            if (currentAudioPlaying != null)
            {
                currentAudioPlaying.Stop();
            }

            // If more sources remain, play the next one.
            if (currentAudioIndex < sourcesInCollider.Count - 1)
            {
                currentAudioIndex++;
                sourcesInCollider[currentAudioIndex].Play();
                currentAudioPlaying = sourcesInCollider[currentAudioIndex];
                Debug.Log("Cycling to audio index: " + currentAudioIndex);
            }
            // If the last AudioSource was just played, reset the cycle.
            else
            {
                currentAudioIndex = -1;
                Debug.Log("Reached end of cycle. Stopping audio.");
            }
        }
    }

    /// <summary>
    /// Stops the currently playing audio (if any) and resets the cycle,
    /// so that the next call to CycleAudioSource() starts fresh using the updated positions.
    /// </summary>
    public void ResetCycle()
    {
        if (currentAudioPlaying != null)
        {
            currentAudioPlaying.Stop();
            currentAudioPlaying = null;
        }
        currentAudioIndex = -1;
        if (playerTransform != null)
        {
            cycleStartPosition = playerTransform.position;
        }
        Debug.Log("Cycle manually reset; next call will start a new cycle with updated positions.");
    }
}
