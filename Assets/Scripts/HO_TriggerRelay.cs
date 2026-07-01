using UnityEngine;

/// <summary>
/// Relays trigger enter events to the exhibition flow controller with the assigned prompt or ending output data.
/// </summary>
public sealed class HO_TriggerRelay : MonoBehaviour
{
    [SerializeField] private HO_ExhibitionFlowController flowController;
    [SerializeField] private HO_TriggerData triggerData;
    [SerializeField] private string requiredTag = "Player";

    /// <summary>
    /// Sends the configured trigger data to the flow controller when the player enters the trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (flowController == null || triggerData == null || other == null)
        {
            return;
        }

        if (!other.CompareTag(requiredTag))
        {
            return;
        }

        flowController.TryHandleTriggerEntered(triggerData);
    }

    /// <summary>
    /// Normalizes inspector text so tag comparisons stay stable.
    /// </summary>
    private void OnValidate()
    {
        requiredTag = string.IsNullOrWhiteSpace(requiredTag) ? "Player" : requiredTag.Trim();
    }
}
