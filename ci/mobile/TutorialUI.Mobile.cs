using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;

    private Coroutine _autoCloseCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        canvasGroup.alpha = 0f;
    }

    public void Show(string title, string content)
    {
        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);

        titleText.text = HighflyMobileText.Localize(title);
        contentText.text = HighflyMobileText.Localize(content);
        canvasGroup.alpha = 1f;
    }

    public void Show(string title, string content, float duration)
    {
        Show(title, content);

        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        _autoCloseCoroutine = StartCoroutine(AutoClose(duration));
    }

    public void Close()
    {
        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        canvasGroup.alpha = 0f;
    }

    private IEnumerator AutoClose(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        canvasGroup.alpha = 0f;
    }
}
