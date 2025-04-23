using UnityEngine;

public class ActivateCollider : MonoBehaviour
{
    public GameObject targetObject; // The GameObject to activate/deactivate
    public string playerTag = "Player"; // Tag to identify the player

    // Start is called before the first frame update
    void Start()
    {
        // Make sure the target object exists
        if (targetObject == null)
        {
            Debug.LogWarning("Target GameObject not assigned to ActivateCollider script!");
        }
        else
        {
            // Optionally start with the object deactivated
            targetObject.SetActive(false);
        }
    }

    // Called when another collider enters this object's trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Only activate when the player enters
        if (other.CompareTag(playerTag) && targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    // Called when another collider exits this object's trigger collider
    private void OnTriggerExit(Collider other)
    {
        // Only deactivate when the player exits
        if (other.CompareTag(playerTag) && targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }
}