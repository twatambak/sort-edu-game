using DG.Tweening;
using UnityEngine;

public static class Tweenimation3D
{
    #region Constants

    private const float DefaultRotationDuration = 0.1f;

    #endregion


    #region Basic Animations

    /// <summary>
    /// Moves the object upward, overshoots slightly, then settles back to its original position.
    /// </summary>
    public static Sequence Bounce(
        GameObject gameObject,
        float height = 0.5f,
        float duration = 0.25f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + height,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y,
                duration
            ).SetEase(Ease.OutBounce)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Scales the object outward and then smoothly returns it to its original scale.
    /// </summary>
    public static Sequence Pulse(
        GameObject gameObject,
        float amount = 1.1f,
        float duration = 0.15f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                originalScale * amount,
                duration
            ).SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                duration
            ).SetEase(Ease.InOutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Squashes the object vertically while expanding it horizontally, then restores its original scale.
    /// </summary>
    public static Sequence SquashAndStretch(
        GameObject gameObject,
        float squash = 0.8f,
        float stretch = 1.15f,
        float duration = 0.12f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                new Vector3(
                    originalScale.x * stretch,
                    originalScale.y * squash,
                    originalScale.z * stretch
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                new Vector3(
                    originalScale.x * squash,
                    originalScale.y * stretch,
                    originalScale.z * squash
                ),
                duration
            ).SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                duration * 1.2f
            ).SetEase(Ease.OutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Compresses the object before quickly returning it to its original scale, creating a spring-like effect.
    /// </summary>
    public static Sequence Spring(
        GameObject gameObject,
        float squash = 0.8f,
        float stretch = 1.2f,
        float duration = 0.1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(
                originalScale.y * squash,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScaleY(
                originalScale.y * stretch,
                duration
            ).SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOScaleY(
                originalScale.y,
                duration * 1.5f
            ).SetEase(Ease.OutElastic)
        );

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Movement Animations

    /// <summary>
    /// Moves the object vertically upward and downward continuously, creating a floating effect.
    /// </summary>
    public static Sequence Float(
        GameObject gameObject,
        float height = 0.25f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + height,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Moves the object vertically while gently rotating it around the Y axis.
    /// </summary>
    public static Sequence Hover(
        GameObject gameObject,
        float height = 0.2f,
        float rotation = 5f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;
        Quaternion originalRotation = target.localRotation;

        float originalY = originalRotation.eulerAngles.y;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + height,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalY + rotation,
                    originalRotation.eulerAngles.z
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalY - rotation,
                    originalRotation.eulerAngles.z
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Moves the object forward and backward continuously, creating a subtle hovering motion in depth.
    /// </summary>
    public static Sequence HoverDepth(
        GameObject gameObject,
        float distance = 0.15f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveZ(
                originalPosition.z + distance,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalMoveZ(
                originalPosition.z,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Quickly moves the object upward and then drops it back down with a bounce.
    /// </summary>
    public static Sequence Jump(
        GameObject gameObject,
        float height = 1f,
        float duration = 0.25f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + height,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y,
                duration
            ).SetEase(Ease.InQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Makes the object hop repeatedly with a short pause between jumps.
    /// </summary>
    public static Sequence Hop(
        GameObject gameObject,
        float height = 0.5f,
        float jumpDuration = 0.25f,
        float pauseDuration = 0.5f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + height,
                jumpDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y,
                jumpDuration
            ).SetEase(Ease.OutBounce)
        );

        sequence.AppendInterval(pauseDuration);

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Rotation Animations

    /// <summary>
    /// Rotates the object around the Y axis from side to side, creating a gentle idle sway.
    /// </summary>
    public static Sequence Sway(
        GameObject gameObject,
        float angle = 5f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;

        float originalY = originalRotation.eulerAngles.y;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalY + angle,
                    originalRotation.eulerAngles.z
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalY - angle,
                    originalRotation.eulerAngles.z
                ),
                duration * 2f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalRotateQuaternion(
                originalRotation,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Continuously rotates the object around its vertical axis.
    /// </summary>
    public static Sequence Spin(
        GameObject gameObject,
        float rotations = 1f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    target.localEulerAngles.x,
                    target.localEulerAngles.y + (360f * rotations),
                    target.localEulerAngles.z
                ),
                duration,
                RotateMode.FastBeyond360
            ).SetEase(Ease.Linear)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Rotates the object rapidly in a random direction before returning it to its original rotation.
    /// </summary>
    public static Sequence Wobble(
        GameObject gameObject,
        float angle = 10f,
        int wobbles = 3)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;

        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < wobbles; i++)
        {
            float direction = i % 2 == 0 ? 1f : -1f;

            sequence.Append(
                target.DOLocalRotate(
                    new Vector3(
                        originalRotation.eulerAngles.x,
                        originalRotation.eulerAngles.y,
                        originalZ + angle * direction
                    ),
                    0.08f
                ).SetEase(Ease.InOutSine)
            );
        }

        sequence.Append(
            target.DOLocalRotateQuaternion(
                originalRotation,
                DefaultRotationDuration
            ).SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Impact Animations

    /// <summary>
    /// Quickly shakes the object's position to simulate a strong physical impact.
    /// </summary>
    public static Sequence Shake(
        GameObject gameObject,
        float strength = 0.15f,
        float duration = 0.3f)
    {
        Transform target = BeginAnimation(gameObject);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOShakePosition(
                duration,
                strength,
                15,
                90f,
                false,
                true
            )
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Shakes the object's rotation to simulate vibration, damage, or mechanical instability.
    /// </summary>
    public static Sequence Vibrate(
        GameObject gameObject,
        float strength = 8f,
        float duration = 0.3f)
    {
        Transform target = BeginAnimation(gameObject);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOShakeRotation(
                duration,
                new Vector3(
                    strength,
                    strength,
                    strength
                ),
                15,
                90f,
                false
            )
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Compresses the object and rotates it slightly to simulate a physical collision or landing.
    /// </summary>
    public static Sequence Impact(
        GameObject gameObject,
        float squash = 0.85f,
        float rotation = 5f,
        float duration = 0.1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;
        Quaternion originalRotation = target.localRotation;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                new Vector3(
                    originalScale.x * 1.05f,
                    originalScale.y * squash,
                    originalScale.z * 1.05f
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalRotation.eulerAngles.z + rotation
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                duration * 1.5f
            ).SetEase(Ease.OutBack)
        );

        sequence.Join(
            target.DOLocalRotateQuaternion(
                originalRotation,
                duration * 1.5f
            ).SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Quickly scales the object down and back up to simulate a sharp hit or recoil.
    /// </summary>
    public static Sequence Hit(
        GameObject gameObject,
        float amount = 0.9f,
        float duration = 0.08f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                originalScale * amount,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                duration
            ).SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Produces a quick recoil motion in a specified local direction before returning to the original position.
    /// </summary>
    public static Sequence Recoil(
        GameObject gameObject,
        Vector3 direction,
        float distance = 0.25f,
        float duration = 0.1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Vector3 recoilPosition =
            originalPosition + direction.normalized * distance;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMove(
                recoilPosition,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOLocalMove(
                originalPosition,
                duration * 1.5f
            ).SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Idle Animations

    /// <summary>
    /// Gently scales the object in and out continuously to create a subtle breathing effect.
    /// </summary>
    public static Sequence Breathing(
        GameObject gameObject,
        float amount = 1.03f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                originalScale * amount,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Combines subtle floating, rotation, and scaling to give a static object a sense of life.
    /// </summary>
    public static Sequence IdleLife(
        GameObject gameObject,
        float intensity = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;
        Vector3 originalScale = target.localScale;
        Quaternion originalRotation = target.localRotation;

        float movement = 0.05f * intensity;
        float angle = 3f * intensity;
        float scale = 1f + (0.02f * intensity);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + movement,
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y + angle,
                    originalRotation.eulerAngles.z
                ),
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOScale(
                originalScale * scale,
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalMove(
                originalPosition,
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalRotateQuaternion(
                originalRotation,
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOScale(
                originalScale,
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.AppendInterval(
            Random.Range(0.5f, 2f)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Utility

    /// <summary>
    /// Stops the current Tweenimation3D animation running on the GameObject.
    /// </summary>
    public static void Kill(GameObject gameObject)
    {
        if (gameObject == null)
            return;

        DOTween.Kill(gameObject);
    }


    /// <summary>
    /// Stops the current Tweenimation3D animation and restores the supplied transform values.
    /// </summary>
    public static void Reset(
        GameObject gameObject,
        Vector3 originalPosition,
        Quaternion originalRotation,
        Vector3 originalScale)
    {
        if (gameObject == null)
            return;

        DOTween.Kill(gameObject);

        Transform target = gameObject.transform;

        target.localPosition = originalPosition;
        target.localRotation = originalRotation;
        target.localScale = originalScale;
    }


    /// <summary>
    /// Stops the previous Tweenimation3D animation and prepares the GameObject for a new animation.
    /// </summary>
    private static Transform BeginAnimation(GameObject gameObject)
    {
        if (gameObject == null)
            return null;

        DOTween.Kill(gameObject);

        return gameObject.transform;
    }


    /// <summary>
    /// Assigns the GameObject as the Tween ID and links the sequence to its lifetime.
    /// </summary>
    private static Sequence FinishAnimation(
        Sequence sequence,
        GameObject gameObject)
    {
        sequence
            .SetId(gameObject)
            .SetLink(gameObject);

        return sequence;
    }

    #endregion
}
