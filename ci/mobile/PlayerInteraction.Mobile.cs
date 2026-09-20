using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 2.0f;
    public LayerMask interactLayer;

    [Header("UI")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;
    public CanvasGroup canvasGroup;

    private IInteractable _currentInteractable;
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (_playerController != null && _playerController.currentState == PlayerState.Interact)
        {
            ShowPrompt(false, "");
            _currentInteractable = null;
            return;
        }

        CheckForInteractable();
    }

    public bool TryInteract()
    {
        if (_currentInteractable == null) return false;

        ShowPrompt(false, "");
        _currentInteractable.Interact(gameObject);
        return true;
    }

    private void CheckForInteractable()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 1.0f;

        bool isHit = Physics.SphereCast(
            origin,
            0.5f,
            transform.forward,
            out hit,
            interactRange,
            interactLayer);

        if (isHit)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                _currentInteractable = interactable;
                ShowPrompt(true, interactable.GetInteractPrompt());
                return;
            }
        }

        _currentInteractable = null;
        ShowPrompt(false, "");
    }

    private void ShowPrompt(bool isActive, string message)
    {
        if (isActive && promptText != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;
            promptText.text = HighflyMobileText.Localize(message);
        }
        else
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(origin + transform.forward * interactRange, 0.5f);
    }
}
