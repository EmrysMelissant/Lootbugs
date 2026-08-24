using UnityEngine;
public interface IInteractable
{
    void Interact(GameObject interactor);

    public string InteractionText { get; }
}
