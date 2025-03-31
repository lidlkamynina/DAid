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

    [Header("Foot UI Groups")]
    public GameObject leftFootGroup;
    public GameObject rightFootGroup;

    private float fadeDuration = 0.25f; // 250ms fade time

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
        Color green = new Color(0, 1, 0, 0.2f);
        Color red = new Color(1, 0, 0, 1f);

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
            // Zone 3: back → arrow points up (90°)
            // Zone 4: front → arrow points down (-90°)
            // Zone 5: left → arrow points left (180°)
            // Zone 6: right → arrow points right (0°)
            switch (zone)
            {
                case 3:
                    rotationAngle = 90f;
                    break;
                case 4:
                    rotationAngle = -90f; // or 270f
                    break;
                case 5:
                    rotationAngle = 180f;
                    break;
                case 6:
                    rotationAngle = 0f;
                    break;
            }
        }
        else
        {
            // For mirrored arrow, swap the left/right rotations:
            // Zone 3: back → arrow points up (90°) remains the same.
            // Zone 4: front → arrow points down (-90°) remains the same.
            // Zone 5: left → arrow should show left. Since the arrow sprite is mirrored,
            //          a rotation of 0 will display left.
            // Zone 6: right → arrow should show right. Thus, rotation of 180° will display right.
            switch (zone)
            {
                case 3:
                    rotationAngle = 90f;
                    break;
                case 4:
                    rotationAngle = -90f; // or 270f
                    break;
                case 5:
                    rotationAngle = 0f;
                    break;
                case 6:
                    rotationAngle = 180f;
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
}
