using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EliceLogoAnimation : MonoBehaviour
{
    [Header("Logo Settings")]
    [Tooltip("Assign the logo RectTransform (recommended, for UI logo).")]
    public RectTransform logoRectTransform;

    [Tooltip("Optional: Assign if you use UI Image for fading.")]
    public Image logoImage;

    [Header("Slide Settings")]
    [Tooltip("How far from the right side the logo starts (in screen width ratio).")]
    [Range(0.5f, 3f)]
    public float startOffsetScreenWidth = 1.8f;

    [Tooltip("Total duration of the fast slide-in. Smaller = much faster.")]
    public float slideDuration = 0.35f;

    [Tooltip("How close to the target we consider the 'braking point' (0~1, closer to 1 = brake at the end).")]
    [Range(0.7f, 0.99f)]
    public float brakePointRatio = 0.9f;

    [Tooltip("Delay before starting the logo animation.")]
    public float startDelay = 0.05f;

    [Header("Brake Feeling")]
    [Tooltip("Extra small overshoot after braking (for a tiny bounce). Set 0 for no bounce.")]
    public float brakeOvershoot = 10f;

    [Tooltip("Duration of tiny settle after braking.")]
    public float settleDuration = 0.18f;

    [Header("Shake Effect On Brake")]
    [Tooltip("Enable/disable shake when braking.")]
    public bool useBrakeShake = true;

    [Tooltip("Shake duration at the moment of braking.")]
    public float brakeShakeDuration = 0.2f;

    [Tooltip("Shake strength (position).")]
    public float brakeShakeStrength = 20f;

    [Tooltip("Shake vibrato (number of oscillations).")]
    public int brakeShakeVibrato = 18;

    [Header("Outro (Fade Out)")]
    [Tooltip("How long the logo stays fully visible before fading out.")]
    public float holdDuration = 0.6f;

    [Tooltip("Duration of the fade-out at the end.")]
    public float fadeOutDuration = 1.0f;

    private Vector2 _targetAnchoredPos;

    void Awake()
    {
        if (logoRectTransform == null)
        {
            logoRectTransform = GetComponent<RectTransform>();
        }

        if (logoRectTransform == null)
        {
            Debug.LogError("[EliceLogoAnimation] RectTransform is not assigned or found.");
            enabled = false;
            return;
        }

        _targetAnchoredPos = logoRectTransform.anchoredPosition;

        ResetLogoVisualState();
    }

    void Start()
    {
        PlayLogoTrackIntro();
    }

    /// <summary>
    /// Plays the very fast track-like sliding intro with a strong brake at the end.
    /// </summary>
    void PlayLogoTrackIntro()
    {
        // Calculate start position (far right, off-screen)
        float screenWidth = Screen.width;
        Vector2 startPos = _targetAnchoredPos;
        startPos.x += screenWidth * startOffsetScreenWidth;

        // Position at the brake point
        Vector2 brakePointPos = Vector2.Lerp(startPos, _targetAnchoredPos, brakePointRatio);

        // Apply starting anchored position
        logoRectTransform.anchoredPosition = startPos;

        // Pre-calc durations
        float runDuration = slideDuration * brakePointRatio;
        float brakeDuration = Mathf.Max(slideDuration - runDuration, 0.01f); // avoid 0 duration

        // Create DOTween sequence
        Sequence seq = DOTween.Sequence();

        // Optional start delay
        if (startDelay > 0f)
        {
            seq.AppendInterval(startDelay);
        }

        // 1) High-speed run: start → brakePoint (almost constant speed)
        seq.Append(
            logoRectTransform.DOAnchorPos(brakePointPos, runDuration)
                .SetEase(Ease.Linear)
        );

        // Fade in while sliding
        seq.Join(
                logoImage.DOFade(1f, slideDuration * 0.5f)
                    .SetEase(Ease.Linear)
            );

        // 2) Hard brake: brakePoint → (tiny overshoot) → final target
        Vector2 brakeEndPos = _targetAnchoredPos;
        bool useOvershoot = brakeOvershoot > 0f;

        if (useOvershoot)
        {
            brakeEndPos.x = _targetAnchoredPos.x - brakeOvershoot;
        }

        // 2-1) Strong deceleration to brakeEndPos
        seq.Append(
            logoRectTransform.DOAnchorPos(brakeEndPos, brakeDuration)
                .SetEase(Ease.OutExpo)
        );

        // 2-2) Small settle back to exact final position
        if (useOvershoot && settleDuration > 0f)
        {
            seq.Append(
                logoRectTransform.DOAnchorPos(_targetAnchoredPos, settleDuration)
                    .SetEase(Ease.OutQuad)
            );
        }

        // 3) Shake at brake moment (car body shake)
        if (useBrakeShake && brakeShakeDuration > 0f && brakeShakeStrength > 0f)
        {
            seq.Join(
                logoRectTransform.DOShakeAnchorPos(
                    brakeShakeDuration,
                    strength: new Vector2(brakeShakeStrength, 0f),
                    vibrato: brakeShakeVibrato,
                    randomness: 90f,
                    snapping: false,
                    fadeOut: true
                )
            );
        }

        // 4) Stay fully visible for a moment
        if (holdDuration > 0f)
        {
            seq.AppendInterval(holdDuration);
        }

        // 5) Smooth fade-out outro
        if (logoImage != null && fadeOutDuration > 0f)
        {
            seq.Append(
                logoImage.DOFade(0f, fadeOutDuration)
                    .SetEase(Ease.InOutQuad)
            );
        }

        // 6) Final callback
        seq.OnComplete(OnLogoTrackIntroComplete);
    }

    /// <summary>
    /// Resets logo visual state before the animation (alpha only).
    /// </summary>
    void ResetLogoVisualState()
    {
        if (logoImage == null) return;

        Color c = logoImage.color;
        c.a = 0f;
        logoImage.color = c;
    }

    /// <summary>
    /// Called when the full intro (slide + brake + fade-out) is finished.
    /// Loads the lobby scene.
    /// </summary>
    void OnLogoTrackIntroComplete()
    {
        Debug.Log("[EliceLogoAnimation] Intro finished. Loading LobbyScene_LSH.");
        SceneManager.LoadScene("LobbyScene_LSH");
    }
}