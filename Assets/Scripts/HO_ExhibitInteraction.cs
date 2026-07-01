using UnityEngine;

/// <summary>
/// Stores one exhibit data reference and forwards player interaction requests into the exhibition flow.
/// </summary>
public sealed class HO_ExhibitInteraction : MonoBehaviour
{
    [SerializeField] private HO_ExhibitData exhibitData;
    [SerializeField] private HO_ExhibitionFlowController flowController;

    /// <summary>
    /// Returns the connected exhibit data so scene wiring can be verified from other systems.
    /// </summary>
    public HO_ExhibitData ExhibitData => exhibitData;

    /// <summary>
    /// Starts the connected exhibit presentation when a valid curator presenter and exhibit data are available.
    /// </summary>
    public bool TryInteract(HO_CuratorPresenter curatorPresenter)
    {
        _ = curatorPresenter;

        if (flowController == null)
        {
            Debug.LogWarning("HO_ExhibitInteraction requires a HO_ExhibitionFlowController reference.", this);
            return false;
        }

        if (exhibitData == null)
        {
            Debug.LogWarning("HO_ExhibitInteraction requires a HO_ExhibitData reference.", this);
            return false;
        }

        return flowController.TryStartRepresentativeExhibit(exhibitData);
    }

    /// <summary>
    /// Logs setup warnings early when the component is missing required serialized references.
    /// </summary>
    private void OnValidate()
    {
        if (exhibitData == null)
        {
            Debug.LogWarning("HO_ExhibitInteraction should reference a HO_ExhibitData asset.", this);
        }

        if (flowController == null)
        {
            Debug.LogWarning("HO_ExhibitInteraction should reference a HO_ExhibitionFlowController.", this);
        }
    }
}
