using UnityEngine;

public class Interaction : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "E";
    

    public void Interact(GameObject interactor)
    {
        Debug.Log("Interacted with " + gameObject.name);
    }
    public string InteractionText {get { return interactText; } }
}

