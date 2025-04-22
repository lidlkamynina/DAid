using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum BoxPosition
{
    Vidus,
    Prieksa,
    Aizmugure,
    DiagonaliLabi,
    DiagonaliKreisi,
    Labi,
    Kreisi
}

public class BoxUIManager : MonoBehaviour
{
    [Header("Assign UI Boxes in correct order")]
    public Image[] boxImages = new Image[9]; // left-to-right, top-to-bottom

    [Header("Colors")]
    public Color highlightColor = new Color(0.196f, 0.803f, 0.196f, 0.7f); // #32CD32
    public float fadeOutDuration = 0.1f;
    public float stayDuration = 1.0f;

    private Coroutine[] activeCoroutines;

    void Awake()
    {
        activeCoroutines = new Coroutine[boxImages.Length];

        // Start with all boxes fully transparent
        foreach (var img in boxImages)
        {
            SetAlpha(img, 0f);
        }
    }

    public void Highlight(BoxPosition pos)
    {
        int index = GetIndexForPosition(pos);
        if (index >= 0 && index < boxImages.Length)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);

            activeCoroutines[index] = StartCoroutine(FlashHighlight(boxImages[index], index));
        }
    }

    private IEnumerator FlashHighlight(Image image, int index)
    {
        // Set to highlight color instantly
        image.color = highlightColor;

        // Wait for visible duration
        yield return new WaitForSeconds(stayDuration);

        // Fade out over time
        float time = 0f;
        Color startColor = image.color;

        while (time < fadeOutDuration)
        {
            float t = time / fadeOutDuration;
            image.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
            time += Time.deltaTime;
            yield return null;
        }

        SetAlpha(image, 0f);
        activeCoroutines[index] = null;
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    private int GetIndexForPosition(BoxPosition pos)
    {
        switch (pos)
        {
            case BoxPosition.Prieksa: return 1;         // Top middle
            case BoxPosition.DiagonaliKreisi: return 0; // Top left
            case BoxPosition.DiagonaliLabi: return 2;   // Top right
            case BoxPosition.Kreisi: return 3;          // Middle left
            case BoxPosition.Vidus: return 4;           // Center
            case BoxPosition.Labi: return 5;            // Middle right
            case BoxPosition.Aizmugure: return 7;       // Bottom middle
            default: return -1;
        }
    }
}
