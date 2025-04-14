using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class DistanceBasedFootstep : MonoBehaviour
{
    [Header("Footstep Settings")]
    public float stepDistance = 1.6f; // Distance required to trigger a step
    public float raycastDistance = 1.5f;
    public LayerMask surfaceLayer;

    [Header("References")]
    public Transform footOrigin; // Usually the player or camera's transform
    private AudioSource audioSource;

    private Vector3 lastStepPosition;

    void Start()
    {
        if (footOrigin == null)
            footOrigin = transform;

        audioSource = GetComponent<AudioSource>();
        lastStepPosition = footOrigin.position;
    }

    void Update()
    {
        float movedDistance = Vector3.Distance(footOrigin.position, lastStepPosition);

        if (movedDistance >= stepDistance)
        {
            
            TryPlayFootstep();
            lastStepPosition = footOrigin.position;
        }

        Debug.DrawRay(footOrigin.position, Vector3.down * raycastDistance, Color.red);

    }

    void TryPlayFootstep()
    {
        Vector3 rayOrigin = footOrigin.position + Vector3.up * 0f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, surfaceLayer))
        {
            //Debug.LogWarning("Footstep triggered");
            SurfaceAudioProfile surface = hit.collider.GetComponent<SurfaceAudioProfile>();

            if (surface != null && surface.RandomContainer != null)
            {
                audioSource.resource = surface.RandomContainer;
                audioSource.Play();
                //audioSource.Play(surface.RandomContainer);
            }
        }
    }
}
