using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyIcons : MonoBehaviour
{
    public TextMeshProUGUI key_text;
    private Coroutine colour_change_coroutine;
    [SerializeField] Image key_image;
    public void ChangeColour(Color colour)
    {
        if (colour_change_coroutine != null)
        {
            StopCoroutine(colour_change_coroutine);
        }
        colour_change_coroutine = StartCoroutine(ColourChangeCoroutine(colour));
    }

    private IEnumerator ColourChangeCoroutine(Color colour)
    {
        if (!key_image)
        {
            yield break;
        }

        while (key_image.color != colour)
        {
            key_image.color = Color.Lerp(key_image.color, colour, 2*Time.deltaTime);
            yield return null;
        }
    }
}
