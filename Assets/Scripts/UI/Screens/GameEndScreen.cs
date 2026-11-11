using System;
using DG.Tweening;
using OndeVai.Data;
using OndeVai.Effects;
using OndeVai.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End of match screen. Shows the result, score, errors and up to three stars based on accuracy.
/// </summary>
public class GameEndScreen : UIScreen
{
    private const float CountUpDuration = 0.6f;
    private const float StarDelay = 0.35f;
    private const float StarStagger = 0.22f;

    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _errorText;

    [Tooltip("Optional star images, filled from left to right according to accuracy. A row of stars is generated under the title when empty.")]
    [SerializeField] private Image[] _stars;

    private Tween _scoreTween;
    private Tween _errorTween;

    /// <summary>Raised when the player asks to go back to the title screen.</summary>
    public event Action HomeRequested;

    public void PlayAgain()
    {
        Hide();
        GameController.Instance.ResetGame();
    }

    /// <summary>Hides the screen and asks the flow to return to the title screen.</summary>
    public void GoHome()
    {
        Hide();
        HomeRequested?.Invoke();
    }

    /// <summary>Displays the finished match, using live totals if its history record could not be saved.</summary>
    public void ShowResult(GameSessionRecord record, int score, int errors)
    {
        if (record == null)
            Debug.LogError("GameEndScreen is showing live results because the match history record could not be saved.", this);

        Show();
        _scoreTween?.Kill();
        _errorTween?.Kill();
        int displayedScore = record != null ? record.Score : score;
        int displayedErrors = record != null ? record.Errors : errors;
        _scoreTween = CountUp(_scoreText, displayedScore);
        _errorTween = CountUp(_errorText, displayedErrors);

        ShowStars(CalculateStars(displayedScore, displayedErrors));
    }

    /// <summary>Three stars for no mistakes, fewer as the error ratio grows. At least one star when anything was sorted.</summary>
    public static int CalculateStars(int score, int errors)
    {
        return MatchScoring.CalculateStars(score, errors);
    }

    private static Tween CountUp(TMP_Text label, int target)
    {
        if (label == null)
            return null;

        label.text = "0";
        if (target <= 0)
            return null;

        int shown = 0;
        return DOTween.To(() => shown, value =>
            {
                if (value == shown)
                    return;

                shown = value;
                label.text = value.ToString();
            }, target, CountUpDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(label.gameObject);
    }

    private void ShowStars(int count)
    {
        if (_stars == null)
            return;

        for (int i = 0; i < _stars.Length; i++)
        {
            Image star = _stars[i];
            if (star == null)
                continue;

            Transform starTransform = star.transform;
            starTransform.DOKill();
            star.DOKill();
            starTransform.localScale = Vector3.one;
            starTransform.localRotation = Quaternion.identity;
            if (i >= count)
                continue;

            float delay = StarDelay + i * StarStagger;
            starTransform.DOPunchScale(Vector3.one * 0.6f, 0.45f, 6)
                .SetDelay(delay)
                .SetUpdate(true)
                .SetLink(star.gameObject);
            starTransform.DOPunchRotation(new Vector3(0f, 0f, 25f), 0.45f, 6)
                .SetDelay(delay)
                .SetUpdate(true)
                .SetLink(star.gameObject);
        }
    }
}
