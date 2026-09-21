using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public static class HighflyMobileText
{
    private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "The Last Dreamer", "El Último Soñador" },
        { "Memory Fragment", "Fragmento de Memoria" },
        { "Potion", "Poción" },
        { "Controls", "Controles" },
        { "Altar of Memeory", "Altar de la Memoria" },

        { "I'm still here.\nStill breathing.\nThe nightmare didn't take me.",
          "Sigo aquí.\nSigo respirando.\nLa pesadilla no pudo conmigo." },

        { "My body remembers — even if my mind struggles.\nMove. Dodge. Survive.\nThat's all there is to it.",
          "Mi cuerpo recuerda, incluso si mi mente lucha por hacerlo.\nMuévete. Esquiva. Sobrevive.\nEso es todo." },
        { "My body remembers — even if my mind struggles.", "Mi cuerpo recuerda, incluso si mi mente lucha por hacerlo." },
        { "Move. Dodge. Survive.", "Muévete. Esquiva. Sobrevive." },
        { "That's all there is to it.", "Eso es todo." },

        { "You...\nYou're still lucid.\nI haven't seen eyes like that since the King fell asleep.",
          "Tú...\nSigues lúcido.\nNo veía unos ojos así desde que el Rey cayó dormido." },
        { "This castle used to be full of light.\nNow the nightmare has swallowed everything —\nthe people, the knights... all of them, lost.",
          "Este castillo solía estar lleno de luz.\nAhora la pesadilla lo ha devorado todo:\nla gente, los caballeros... todos se han perdido." },
        { "If you want to fight back, you'll need strength.\nGather the Memory Fragments the lost souls carry.\nBring them to the altar. Let them anchor you.",
          "Si quieres resistir, necesitarás fuerza.\nReúne los Fragmentos de Memoria que llevan las almas perdidas.\nLlévalos al altar. Deja que te anclen a este mundo." },
        { "The Tower at the center — something dark festers at its peak.\nBut the gate won't open for just anyone.\nFind the ones that guard the inner wards.\nProve yourself first.",
          "En la Torre del centro algo oscuro crece en su cima.\nPero la puerta no se abrirá para cualquiera.\nEncuentra a quienes custodian los sellos interiores.\nDemuestra tu fuerza primero." },
        { "You did it.\nThe wards are broken. The Tower gate... it's open.",
          "Lo lograste.\nLos sellos se rompieron. La puerta de la Torre... está abierta." },
        { "At the top, you'll find a Fragment of pure nightmare.\nShatter it.\nWhat waits beyond — end it.",
          "En la cima encontrarás un Fragmento de pesadilla pura.\nDestrúyelo.\nY acaba con lo que te espere más allá." },

        { "The top of the Tower...\nThe air itself feels wrong here.\nLike the nightmare is breathing.",
          "La cima de la Torre...\nEl aire mismo se siente extraño.\nComo si la pesadilla respirara." },
        { "That fragment — a shard of pure darkness.\nIt pulses. Watches.\nThis is what poisoned the castle.",
          "Ese fragmento... una esquirla de oscuridad pura.\nLate. Observa.\nEsto fue lo que envenenó el castillo." },
        { "Shatter it.\nEnd this.",
          "Destrúyelo.\nTermina con esto." },

        { "You've collected a Memory Fragment.\n\nBring them to the Altar.\nPray there to grow stronger —\nand to anchor yourself to this world.",
          "Has conseguido un Fragmento de Memoria.\n\nLlévalos al Altar.\nÚsalos allí para hacerte más fuerte\ny anclarte a este mundo." },
        { "Your Ego is fading.\n\n[ R ]  Use Potion\nRestores a portion of your Ego.\nRefills when you rest at an Altar.",
          "Tu Ego se está desvaneciendo.\n\nPOCIÓN  Usar poción\nRestaura una parte de tu Ego.\nSe recarga al descansar en un Altar." },

        { "A crystallized fragment of memory.\nPray here to anchor your existence to this moment.\n<i>Your Ego will be restored. Your journey, recorded.</i>",
          "Un fragmento cristalizado de memoria.\nÚsalo para anclar tu existencia a este momento.\n<i>Tu Ego será restaurado. Tu viaje quedará registrado.</i>" },
        { "<color=#90EE90>* Progress saved</color>\n<color=#90EE90>* Ego fully restored</color>\n<color=#90EE90>* Potions refilled</color>",
          "<color=#90EE90>* Progreso guardado</color>\n<color=#90EE90>* Ego restaurado</color>\n<color=#90EE90>* Pociones recargadas</color>" }
    };

    public static bool IsTouchMode =>
        Application.isMobilePlatform || Touchscreen.current != null;

    public static string Localize(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        string exact;
        if (Exact.TryGetValue(value, out exact))
            return exact;

        string s = value;

        s = s.Replace("[ Press any key to begin ]", "[ TOCA LA PANTALLA PARA COMENZAR ]");
        s = s.Replace("[ Press any key to close ]", "[ TOCA PARA CERRAR ]");
        s = s.Replace("[ Press any key ]", "[ TOCA LA PANTALLA ]");
        s = s.Replace("Press any key to begin", "Toca la pantalla para comenzar");
        s = s.Replace("Press any key to close", "Toca para cerrar");
        s = s.Replace("Press any key", "Toca la pantalla");

        s = s.Replace("[E] Talk", "[USAR] HABLAR");
        s = s.Replace("[ E ] Talk", "[USAR] HABLAR");
        s = s.Replace("[R] Pray", "[USAR] ALTAR");
        s = s.Replace("[ R ] Pray", "[USAR] ALTAR");
        s = s.Replace("[ E ] Interact", "[USAR] INTERACTUAR");

        s = s.Replace("[ W A S D ]", "JOYSTICK");
        s = s.Replace("[WASD]", "JOYSTICK");
        s = s.Replace("[ Left Click ]", "ATQ");
        s = s.Replace("[LMB]", "ATQ");
        s = s.Replace("[ Right Click ]", "PARRY");
        s = s.Replace("[RMB]", "PARRY");
        s = s.Replace("[ F ]", "ESQUIVAR");
        s = s.Replace("[ Q ]", "S1");
        s = s.Replace("[ TAB ]", "LOCK");
        s = s.Replace("[ Space ]", "SALTAR");
        s = s.Replace("[ R ]", "POCIÓN");

        return s;
    }

    public static string[] LocalizeLines(string[] lines)
    {
        if (lines == null) return Array.Empty<string>();

        var translated = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
            translated[i] = Localize(lines[i]);
        return translated;
    }
}

public sealed class HighflyMobileRuntimeLocalizer : MonoBehaviour
{
    private float _nextScan;

    private void Update()
    {
        if (!HighflyMobileText.IsTouchMode) return;
        if (Time.unscaledTime < _nextScan) return;
        _nextScan = Time.unscaledTime + 0.35f;

        var texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text == null || string.IsNullOrEmpty(text.text)) continue;

            string localized = HighflyMobileText.Localize(text.text);
            if (!string.Equals(localized, text.text, StringComparison.Ordinal))
                text.text = localized;
        }
    }
}
