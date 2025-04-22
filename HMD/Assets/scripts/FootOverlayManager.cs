using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FootOverlayManagerTwoFeet : MonoBehaviour
{
    [Header("Left Foot Overlays")]
    public Image leftFootLeftOverlay;
    public Image leftFootRightOverlay;
    public Image leftFootTopOverlay;
    public Image leftFootBottomOverlay;

    [Header("Right Foot Overlays")]
    public Image rightFootLeftOverlay;
    public Image rightFootRightOverlay;
    public Image rightFootTopOverlay;
    public Image rightFootBottomOverlay;

    [Header("Arrow Overlays")]
    public Image leftFootArrow;
    public Image rightFootArrow;

    [Header("Bottom Cutouts")]
    public GameObject leftFootBottomCut;
    public GameObject rightFootBottomCut;

    [Header("Foot UI Groups")]
    public GameObject leftFootGroup;
    public GameObject rightFootGroup;

    private float fadeDuration = 0.25f; // 250ms fade time

    // Default y position for the foot UI elements.
    private const float defaultY = -0.014f;
    // New y position when moving the foot "down".
    private const float downY = -0.5f;

    public static FootOverlayManagerTwoFeet Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void setDefaultGreen()
    {
        // Set default green with even more transparency (alpha 0.2)
        Color green = new Color(0, 1, 0, 0.2f);
        StartCoroutine(FadeOverlay(leftFootLeftOverlay, green));
        StartCoroutine(FadeOverlay(leftFootRightOverlay, green));
        StartCoroutine(FadeOverlay(leftFootTopOverlay, green));
        StartCoroutine(FadeOverlay(leftFootBottomOverlay, green));
        StartCoroutine(FadeOverlay(rightFootLeftOverlay, green));
        StartCoroutine(FadeOverlay(rightFootRightOverlay, green));
        StartCoroutine(FadeOverlay(rightFootTopOverlay, green));
        StartCoroutine(FadeOverlay(rightFootBottomOverlay, green));

        // Hide arrows by default
        if (leftFootArrow != null)
            leftFootArrow.gameObject.SetActive(false);
        if (rightFootArrow != null)
            rightFootArrow.gameObject.SetActive(false);
    }

    public void UpdateOverlayForZone(int zone, string foot)
    {
        // Use more transparent green (alpha 0.2) and opaque red (alpha 1)
        Color green = new Color(0, 1, 0, 0.099f);
        Color red = new Color(1, 0, 0, 0.99f);

        if (foot.ToLower() == "left")
        {
            // Handle left foot overlays
            switch (zone) // left, right, top, bottom respectively
            {
                case 1:
                    FadeLeftFoot(green, green, green, green);
                    break;
                case 2:
                    FadeLeftFoot(red, red, red, red);
                    break;
                case 3:
                    FadeLeftFoot(green, green, green, red);
                    break;
                case 4:
                    FadeLeftFoot(green, green, red, green);
                    break;
                case 5:
                    FadeLeftFoot(red, green, green, green);
                    break;
                case 6:
                    FadeLeftFoot(green, red, green, green);
                    break;
                case 7:
                    FadeLeftFoot(red, red, red, red);
                    break;
                default:
                    FadeLeftFoot(green, green, green, green);
                    break;
            }
            // For left foot arrow, we assume no mirroring.
            UpdateArrow(leftFootArrow, zone, false);
        }
        else if (foot.ToLower() == "right")
        {
            // Handle right foot overlays
            switch (zone)
            {
                case 1:
                    FadeRightFoot(green, green, green, green);
                    break;
                case 2:
                    FadeRightFoot(red, red, red, red);
                    break;
                case 3:
                    FadeRightFoot(green, green, green, red);
                    break;
                case 4:
                    FadeRightFoot(green, green, red, green);
                    break;
                case 5:
                    FadeRightFoot(red, green, green, green);
                    break;
                case 6:
                    FadeRightFoot(green, red, green, green);
                    break;
                case 7:
                    FadeRightFoot(red, red, red, red);
                    break;
                default:
                    FadeRightFoot(green, green, green, green);
                    break;
            }
            // For right foot arrow, we assume it is mirrored due to its parent settings.
            UpdateArrow(rightFootArrow, zone, true);
        }
        else if (foot.ToLower() == "both")
        {
            UpdateOverlayForZone(zone, "left");
            UpdateOverlayForZone(zone, "right");
        }
    }

    private void FadeLeftFoot(Color left, Color right, Color top, Color bottom)
    {
        StartCoroutine(FadeOverlay(leftFootLeftOverlay, left));
        StartCoroutine(FadeOverlay(leftFootRightOverlay, right));
        StartCoroutine(FadeOverlay(leftFootTopOverlay, top));
        StartCoroutine(FadeOverlay(leftFootBottomOverlay, bottom));
    }

    private void FadeRightFoot(Color left, Color right, Color top, Color bottom)
    {
        StartCoroutine(FadeOverlay(rightFootLeftOverlay, left));
        StartCoroutine(FadeOverlay(rightFootRightOverlay, right));
        StartCoroutine(FadeOverlay(rightFootTopOverlay, top));
        StartCoroutine(FadeOverlay(rightFootBottomOverlay, bottom));
    }

    private IEnumerator FadeOverlay(Image overlay, Color targetColor)
    {
        Color startColor = overlay.color;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            overlay.color = Color.Lerp(startColor, targetColor, elapsedTime / fadeDuration);
            yield return null;
        }

        overlay.color = targetColor; // Ensure final color is set exactly

        // If the target color is opaque red, bring this overlay to the front.
        if (Mathf.Approximately(targetColor.r, 1f) &&
            Mathf.Approximately(targetColor.g, 0f) &&
            Mathf.Approximately(targetColor.b, 0f) &&
            Mathf.Approximately(targetColor.a, 1f))
        {
            overlay.transform.SetAsLastSibling();
        }
    }

    // Update the arrow visibility and rotation based on the zone.
    // The isMirrored flag tells if the arrow is already flipped (e.g., via parent's scale).
    private void UpdateArrow(Image arrow, int zone, bool isMirrored)
    {
        if (arrow == null)
            return;

        // Hide arrow for zones 1, 2, and 7.
        if (zone == 1 || zone == 2 || zone == 7)
        {
            arrow.gameObject.SetActive(false);
            return;
        }

        float rotationAngle = 0f;
        if (!isMirrored)
        {
            // For non-mirrored arrow:
            // Zone 3: back → arrow points up (0°)
            // Zone 4: front → arrow points down (180°)
            // Zone 5: left → arrow points left (90°)
            // Zone 6: right → arrow points right (-90°)
            switch (zone)
            {
                case 3:
                    rotationAngle = 0f;
                    break;
                case 4:
                    rotationAngle = 180f;
                    break;
                case 5:
                    rotationAngle = 90f;
                    break;
                case 6:
                    rotationAngle = -90f;
                    break;
            }
        }
        else
        {
            // For mirrored arrow, swap the left/right rotations.
            switch (zone)
            {
                case 3:
                    rotationAngle = 0f;
                    break;
                case 4:
                    rotationAngle = 180f;
                    break;
                case 5:
                    rotationAngle = -90f;
                    break;
                case 6:
                    rotationAngle = 90f;
                    break;
            }
        }

        arrow.gameObject.SetActive(true);
        arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
    }

    public void SetActiveFoot(string activeFoot)
    {
        activeFoot = activeFoot.ToLower();
        if (activeFoot == "left")
        {
            if (leftFootGroup != null)
                leftFootGroup.SetActive(true);
            if (rightFootGroup != null)
                rightFootGroup.SetActive(false);
            Debug.Log("Active foot set to LEFT");
        }
        else if (activeFoot == "right")
        {
            if (rightFootGroup != null)
                rightFootGroup.SetActive(true);
            if (leftFootGroup != null)
                leftFootGroup.SetActive(false);
            Debug.Log("Active foot set to RIGHT");
        }
        else if (activeFoot == "both")
        {
            if (leftFootGroup != null)
                leftFootGroup.SetActive(true);
            if (rightFootGroup != null)
                rightFootGroup.SetActive(true);
            Debug.Log("Active foot set to BOTH");
        }
        else
        {
            if (leftFootGroup != null)
                leftFootGroup.SetActive(false);
            if (rightFootGroup != null)
                rightFootGroup.SetActive(false);
            Debug.Log("Active foot set to NONE");
        }
    }

    // NEW FUNCTION: Moves the opposite foot's UI group down.
    // If isRight is true (indicating right foot is active), then move left foot UI group down.
    // If isRight is false, then move right foot UI group down.
    public void MoveOppositeFootDown(bool isRight)
    {
        if (isRight)
        {
            if (leftFootGroup != null)
            {
                RectTransform rt = leftFootGroup.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, downY);
            }
            if (leftFootBottomCut != null)
                leftFootBottomCut.SetActive(true);

            if (leftFootArrow != null)
            {
                Vector3 pos = leftFootArrow.rectTransform.localPosition;
                leftFootArrow.rectTransform.localPosition = new Vector3(pos.x, 0.1f, pos.z);
            }

            Debug.Log("Left foot UI moved down and bottom cut enabled.");
        }
        else
        {
            if (rightFootGroup != null)
            {
                RectTransform rt = rightFootGroup.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, downY);
            }
            if (rightFootBottomCut != null)
                rightFootBottomCut.SetActive(true);

            if (rightFootArrow != null)
            {
                Vector3 pos = rightFootArrow.rectTransform.localPosition;
                rightFootArrow.rectTransform.localPosition = new Vector3(pos.x, 0.1f, pos.z);
            }

            Debug.Log("Right foot UI moved down and bottom cut enabled.");
        }
    }


    // NEW FUNCTION: Resets both foot UI groups to the default y position (-0.014)
    public void ResetFootPositions()
    {
        if (leftFootGroup != null)
        {
            RectTransform rtLeft = leftFootGroup.GetComponent<RectTransform>();
            rtLeft.anchoredPosition = new Vector2(rtLeft.anchoredPosition.x, defaultY);
        }

        if (rightFootGroup != null)
        {
            RectTransform rtRight = rightFootGroup.GetComponent<RectTransform>();
            rtRight.anchoredPosition = new Vector2(rtRight.anchoredPosition.x, defaultY);
        }

        if (leftFootArrow != null)
        {
            leftFootArrow.rectTransform.localPosition = new Vector3(0.023f, -0.04f, -0.01999998f);
        }

        if (rightFootArrow != null)
        {
            rightFootArrow.rectTransform.localPosition = new Vector3(0.02f, -0.03699994f, -0.01999998f);
        }

        if (leftFootBottomCut != null)
            leftFootBottomCut.SetActive(false);

        if (rightFootBottomCut != null)
            rightFootBottomCut.SetActive(false);

        Debug.Log("Foot positions reset and bottom cuts hidden.");
    }

}
