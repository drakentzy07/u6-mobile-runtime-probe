using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.InputSystem;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public float typewriterSpeed = 0.03f;

    private string[] _lines;
    private bool _inputReceived;
    private float _inputCooldown;
    private Action _onComplete;
    private PlayerController _playerController;

    public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        if (canvasGroup == null || canvasGroup.alpha == 0f) return;

        if (_inputCooldown > 0f)
        {
            _inputCooldown -= Time.unscaledDeltaTime;
            return;
        }

        bool advance = false;

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            advance = true;

        if (!advance && Keyboard.current != null)
        {
            var kb = Keyboard.current;
            advance = kb.anyKey.wasPressedThisFrame && !kb.escapeKey.wasPressedThisFrame;
        }

        if (!advance && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            advance = true;

        if (advance) _inputReceived = true;
    }

    public void Open(string speakerName, string[] lines, PlayerController player, Action onComplete = null)
    {
        _playerController = player;
        _onComplete = onComplete;
        _lines = HighflyMobileText.LocalizeLines(lines);

        speakerNameText.text = HighflyMobileText.Localize(speakerName);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        _inputCooldown = 0.15f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = !HighflyMobileText.IsTouchMode;

        StartCoroutine(PlayDialogue());
    }

    private IEnumerator PlayDialogue()
    {
        for (int i = 0; i < _lines.Length; i++)
        {
            _inputReceived = false;
            dialogueText.text = "";

            foreach (char c in _lines[i])
            {
                if (_inputReceived)
                {
                    dialogueText.text = _lines[i];
                    _inputReceived = false;
                    break;
                }

                dialogueText.text += c;
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }

            bool isLast = i == _lines.Length - 1;
            string hint = isLast
                ? "[ TOCA PARA CERRAR ]"
                : "[ TOCA LA PANTALLA ]";

            dialogueText.text =
                _lines[i] +
                $"\n\n<color=#8FDFFF90><size=60%>{hint}</size></color>";

            _inputReceived = false;
            yield return new WaitUntil(() => _inputReceived);
        }

        Close();
    }

    private void Close()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (HighflyMobileText.IsTouchMode)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        _playerController?.ChangeState(PlayerState.Locomotion);
        _onComplete?.Invoke();
    }
}
