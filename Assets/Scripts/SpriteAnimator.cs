using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteAnim[] _animations;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _animationCoroutine;

    private SpriteAnim _currentAnimation;

    /// <summary>
    /// Returns true if a sprite animation is currently playing.
    /// </summary>
    public bool IsPlaying => _animationCoroutine != null;

    /// <summary>
    /// Returns the name of the currently playing animation. 
    /// </summary>
    public string CurrentAnimation => _currentAnimation.Name;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// Plays an animation by name and invokes a callback when a non-looping animation finishes. The callback is not invoked if the animation is manually stopped or replaced.
    /// </summary>
    public bool PlayAnimation(string animationName, Action onComplete, bool loop = false, bool restartIfPlaying = true)
    {
        if (!TryGetAnimation(animationName, out SpriteAnim animation))
            return false;

        if (!restartIfPlaying && IsPlaying && _currentAnimation.Name == animation.Name)
            return false;

        StopAnimation();
        _currentAnimation = animation;
        _spriteRenderer.sprite = animation.Frames[0];
        _animationCoroutine = StartCoroutine(PlayAnimationRoutine(animation, loop, onComplete));
        return true;
    }

    /// <summary>
    /// Plays an animation by name.
    /// </summary>
    public bool PlayAnimation(string animationName, bool loop = false, bool restartIfPlaying = true)
    {
        return PlayAnimation(animationName, null, loop, restartIfPlaying);
    }

    /// <summary>
    /// Stops the currently playing animation. The completion callback will not be invoked.
    /// </summary>
    public void StopAnimation()
    {
        if (_animationCoroutine == null)
            return;

        StopCoroutine(_animationCoroutine);
        _animationCoroutine = null;
    }

    /// <summary>
    /// Restarts the currently playing animation from its first frame.
    /// </summary>
    public bool RestartAnimation()
    {
        if (string.IsNullOrEmpty(_currentAnimation.Name))
            return false;

        return PlayAnimation(_currentAnimation.Name, true);
    }

    private IEnumerator PlayAnimationRoutine(SpriteAnim animation, bool loop, Action onComplete)
    {
        int currentFrame = 0;
        float timer = 0f;

        if (animation.Frames.Length <= 1)
        {
            if (!loop)
                onComplete?.Invoke();

            _animationCoroutine = null;
            yield break;
        }

        float frameDuration = animation.FrameDuration;

        while (true)
        {
            timer += Time.deltaTime;

            if (timer >= frameDuration)
            {
                timer -= frameDuration;
                currentFrame++;

                if (currentFrame >= animation.Frames.Length)
                {
                    if (!loop)
                    {
                        currentFrame = animation.Frames.Length - 1;
                        _spriteRenderer.sprite = animation.Frames[currentFrame];
                        _animationCoroutine = null;
                        onComplete?.Invoke();
                        yield break;
                    }
                    currentFrame = 0;
                }

                _spriteRenderer.sprite = animation.Frames[currentFrame];
            }

            yield return null;
        }
    }

    private bool TryGetAnimation(string animationName, out SpriteAnim animation)
    {
        animation = default;

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"{nameof(SpriteAnimator)} on {name}: Animation name is null or empty.");
            return false;
        }

        if (_animations == null || _animations.Length == 0)
        {
            Debug.LogWarning($"{nameof(SpriteAnimator)} on {name}: No animations have been configured.");
            return false;
        }

        for (int i = 0; i < _animations.Length; i++)
        {
            SpriteAnim candidate = _animations[i];

            if (candidate.Name != animationName)
                continue;

            if (candidate.Frames == null || candidate.Frames.Length == 0)
            {
                Debug.LogWarning($"{nameof(SpriteAnimator)} on {name}: Animation {animationName} has no frames.");
                return false;
            }

            animation = candidate;
            return true;
        }

        Debug.LogWarning($"{nameof(SpriteAnimator)} on {name}: Animation '{animationName}' was not found.");
        return false;
    }
}

[Serializable]
public struct SpriteAnim
{
    public string Name;
    public Sprite[] Frames;
    [Min(0.01f)] public float FPS;

    public float FrameDuration => 1f / Mathf.Max(FPS, 0.01f);

    public SpriteAnim(string name, Sprite[] frames, float fps)
    {
        Name = name;
        Frames = frames;
        FPS = fps;
    }
}