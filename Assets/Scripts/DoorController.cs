using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Tooltip("Animator that plays the open/close animation. Auto-found if left empty.")]
    [SerializeField] private Animator animator;

    [Tooltip("Name of the bool parameter in the Animator that opens the door.")]
    [SerializeField] private string openParameter = "IsOpen";

    [Tooltip("If locked, the door won't open until Unlock() is called.")]
    [SerializeField] private bool isLocked = false;

    private bool isOpen = false;
    private int openHash;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        openHash = Animator.StringToHash(openParameter);
    }

    public string GetDisplayName()
    {
        if (isLocked) return "Door (locked)";
        return isOpen ? "Close door" : "Open door";
    }

    public void Interact(GameObject interactor)
    {
        if (isLocked)
        {
            Debug.Log($"{name} is locked.");
            return;
        }

        isOpen = !isOpen;
        if (animator != null) animator.SetBool(openHash, isOpen);
    }

    public void Unlock()
    {
        isLocked = false;
    }
}
