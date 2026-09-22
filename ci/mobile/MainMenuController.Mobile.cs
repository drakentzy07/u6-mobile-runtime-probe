using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.IO;
using Highfly.SkillLab;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button exitButton;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 1.0f;

    [Header("Background")]
    public GameObject backgroundImage;

    [Header("BGM")]
    public AudioClip menuBGM;
    public float bgmFadeDuration = 2.0f;

    [Header("Intro Text")]
    public TextMeshProUGUI introText;
    [TextArea(2, 5)]
    public string[] introLines;
    public float typewriterSpeed = 0.04f;

    private bool _inputReceived = false;

    private static readonly string[] HighflySpanishIntro =
    {
        "Cuando el Rey cayó dormido,\nel sol cerró los ojos con él.",
        "Desde aquel día, el reino de Somnia\nquedó atrapado en una pesadilla eterna.",
        "Sus habitantes vagan como almas perdidas,\no son consumidos y transformados en monstruos.",
        "Sólo uno permaneció consciente.\nUno que comprendió que esto es un sueño.",
        "Reúne los fragmentos de memoria.\nConserva tu Ego.\nTermina con esta pesadilla."
    };

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // LAB is a development surface, not a story session:
        // never touch the normal save/new-game flow and skip the prologue entirely.
        if (HighflySkillLabMode.IsActive)
        {
            Time.timeScale = 1f;
            if (introText != null)
                introText.gameObject.SetActive(false);
            if (backgroundImage != null)
                backgroundImage.SetActive(false);

            StartCoroutine(DirectLabBoot());
            return;
        }

        LocalizeHighflyMenu();

        string savePath = Path.Combine(Application.persistentDataPath, "save.json");
        if (continueButton != null)
            continueButton.interactable = File.Exists(savePath);

        if (menuBGM != null)
            SoundManager.Instance?.PlayBGM(menuBGM, bgmFadeDuration);

        if (introText != null)
            introText.gameObject.SetActive(false);

        StartCoroutine(FadeIn());
    }

    private IEnumerator DirectLabBoot()
    {
        // One frame lets the bootstrap root survive the scene transition cleanly.
        yield return null;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Somnia");
    }

    private void LocalizeHighflyMenu()
    {
        introLines = HighflySpanishIntro;

        SetButtonLabel(newGameButton, "NUEVA PARTIDA");
        SetButtonLabel(continueButton, "CONTINUAR");
        SetButtonLabel(exitButton, "SALIR");
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;

        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        var legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null)
            legacy.text = value;
    }

    public void OnNewGame()
    {
        StartCoroutine(NewGameWithIntro());
    }

    public void OnContinue()
    {
        if (GameManager.Instance != null)
            StartCoroutine(FadeAndLoad(() => GameManager.Instance.ContinueGame()));
        else
            StartCoroutine(FadeAndLoad(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Somnia")));
    }

    public void OnExit()
    {
        Application.Quit();
    }

    private IEnumerator NewGameWithIntro()
    {
        yield return StartCoroutine(FadeOut());

        if (backgroundImage != null)
            backgroundImage.SetActive(false);

        if (introText != null && introLines != null && introLines.Length > 0)
        {
            SoundManager.Instance?.StopBGM(1.0f);
            yield return StartCoroutine(PlayIntroSequence());
        }

        if (GameManager.Instance != null)
            GameManager.Instance.StartNewGame();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Somnia");
    }

    private IEnumerator PlayIntroSequence()
    {
        introText.transform.SetAsLastSibling();
        introText.gameObject.SetActive(true);

        for (int i = 0; i < introLines.Length; i++)
        {
            introText.text = "";
            introText.color = new Color(1, 1, 1, 1);

            foreach (char c in introLines[i])
            {
                introText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            bool isLast = (i == introLines.Length - 1);
            string hintLabel = isLast ? "[ TOCA LA PANTALLA PARA COMENZAR ]" : "[ TOCA LA PANTALLA ]";
            string hint = $"\n\n<color=#FFFFFF80><size=50%>{hintLabel}</size></color>";

            introText.text = introLines[i] + hint;

            _inputReceived = false;
            yield return new WaitUntil(() => _inputReceived);

            yield return StartCoroutine(FadeText(introText, 1f, 0f, 0.4f));
        }

        introText.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.3f);
    }

    private void Update()
    {
        if (introText == null || !introText.gameObject.activeSelf)
            return;

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            _inputReceived = true;
            return;
        }

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            _inputReceived = true;
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            _inputReceived = true;
    }

    private IEnumerator FadeText(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, a);
            yield return null;
        }
        tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, to);
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        fadeImage.raycastTarget = true;
        float t = 0f;
        fadeImage.color = new Color(0, 0, 0, 1);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - t / fadeDuration);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        fadeImage.raycastTarget = true;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, t / fadeDuration);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 1);
    }

    private IEnumerator FadeAndLoad(System.Action loadAction)
    {
        yield return StartCoroutine(FadeOut());
        loadAction?.Invoke();
    }
}
