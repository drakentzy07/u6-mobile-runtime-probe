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
    private bool _fallbackPromptVisible;
    private string _fallbackPromptMessage = "";
    private GUIStyle _fallbackPromptStyle;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
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
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

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
        string localized = HighflyMobileText.Localize(message);
        bool hasCanvasPrompt = promptText != null && canvasGroup != null;

        if (hasCanvasPrompt)
        {
            canvasGroup.alpha = isActive ? 1f : 0f;
            canvasGroup.interactable = isActive;
            canvasGroup.blocksRaycasts = false;
            if (isActive) promptText.text = localized;
        }

        _fallbackPromptVisible = isActive && !hasCanvasPrompt;
        _fallbackPromptMessage = _fallbackPromptVisible ? localized : "";
    }

    private void OnGUI()
    {
        if (!_fallbackPromptVisible || string.IsNullOrEmpty(_fallbackPromptMessage)) return;

        _fallbackPromptStyle ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        float width = Mathf.Min(Screen.width * 0.56f, 700f);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.76f;
        GUI.Box(new Rect(x, y, width, 52f), _fallbackPromptMessage, _fallbackPromptStyle);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(origin + transform.forward * interactRange, 0.5f);
    }
}
