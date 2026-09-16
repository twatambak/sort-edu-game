using DG.Tweening;
using UnityEngine;

public static class Tweenimation
{
    #region Constants

    private const float DefaultRotationDuration = 0.1f;

    #endregion


    #region Basic Animations

    /// <summary>
    /// Stretches the object vertically, holds the stretched state, then returns to its original scale.
    /// </summary>
    public static Sequence StretchY(
        GameObject gameObject,
        float stretch = 1.25f,
        float duration = 0.15f,
        float holdDuration = 0.25f)
    {
        Transform target = BeginAnimation(gameObject);
        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(originalScale.y * stretch, duration)
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(holdDuration);

        sequence.Append(
            target.DOScaleY(originalScale.y, duration)
                .SetEase(Ease.InOutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Shrinks the object vertically, holds the compressed state, then returns to its original scale.
    /// </summary>
    public static Sequence ShrinkY(
        GameObject gameObject,
        float shrink = 0.8f,
        float duration = 0.15f,
        float holdDuration = 0.25f)
    {
        Transform target = BeginAnimation(gameObject);
        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(originalScale.y * shrink, duration)
                .SetEase(Ease.OutQuad)
        );

        sequence.AppendInterval(holdDuration);

        sequence.Append(
            target.DOScaleY(originalScale.y, duration)
                .SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Stretches the object vertically, holds it, shrinks it vertically,
    /// holds the compressed state, then returns to its original scale.
    /// </summary>
    public static Sequence StretchShrink(
        GameObject gameObject,
        float stretch = 1.25f,
        float shrink = 0.8f,
        float duration = 0.12f,
        float holdDuration = 0.2f)
    {
        Transform target = BeginAnimation(gameObject);
        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(originalScale.y * stretch, duration)
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(holdDuration);

        sequence.Append(
            target.DOScaleY(originalScale.y * shrink, duration)
                .SetEase(Ease.InOutQuad)
        );

        sequence.AppendInterval(holdDuration);

        sequence.Append(
            target.DOScaleY(originalScale.y, duration)
                .SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Pulses the object's entire scale outward, then smoothly returns to its original scale.
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
            target.DOScale(originalScale * amount, duration)
                .SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOScale(originalScale, duration)
                .SetEase(Ease.InOutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Compresses and stretches the object in opposite directions to create a cartoon-like squash and stretch effect.
    /// </summary>
    public static Sequence SquashAndStretch(
        GameObject gameObject,
        float squash = 0.85f,
        float stretch = 1.12f,
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
                    originalScale.z
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                new Vector3(
                    originalScale.x * squash,
                    originalScale.y * stretch,
                    originalScale.z
                ),
                duration
            ).SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOScale(originalScale, duration * 1.2f)
                .SetEase(Ease.OutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Alternates between horizontal and vertical deformation to create a soft jelly-like effect.
    /// </summary>
    public static Sequence Jelly(
        GameObject gameObject,
        float amount = 0.08f,
        float duration = 0.12f)
    {
        Transform target = BeginAnimation(gameObject);
        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                new Vector3(
                    originalScale.x * (1f + amount),
                    originalScale.y * (1f - amount),
                    originalScale.z
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                new Vector3(
                    originalScale.x * (1f - amount),
                    originalScale.y * (1f + amount),
                    originalScale.z
                ),
                duration * 1.2f
            ).SetEase(Ease.InOutQuad)
        );

        sequence.Append(
            target.DOScale(originalScale, duration * 1.5f)
                .SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Reaction Animations

    /// <summary>
    /// Stretches the object vertically, shakes its rotation, then restores its original transform.
    /// </summary>
    public static Sequence StretchAndVibrate(
        GameObject gameObject,
        float stretch = 1.2f,
        float stretchDuration = 0.12f,
        float vibrationDuration = 0.35f,
        float vibrationStrength = 8f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;
        Quaternion originalRotation = target.localRotation;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(originalScale.y * stretch, stretchDuration)
                .SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOShakeRotation(
                vibrationDuration,
                new Vector3(0f, 0f, vibrationStrength),
                12,
                90f,
                false
            )
        );

        sequence.Append(
            target.DOLocalRotateQuaternion(
                originalRotation,
                DefaultRotationDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            target.DOScaleY(
                originalScale.y,
                DefaultRotationDuration
            ).SetEase(Ease.InOutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Stretches the object vertically, makes it wobble from side to side,
    /// then restores its original rotation and scale.
    /// </summary>
    public static Sequence StretchAndWobble(
        GameObject gameObject,
        float stretch = 1.2f,
        float stretchDuration = 0.12f,
        float angle = 7f,
        int wobbles = 3)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;
        Quaternion originalRotation = target.localRotation;

        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(
                originalScale.y * stretch,
                stretchDuration
            ).SetEase(Ease.OutBack)
        );

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

        sequence.Join(
            target.DOScaleY(
                originalScale.y,
                DefaultRotationDuration
            ).SetEase(Ease.InOutQuad)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Compresses the object and slightly rotates it to simulate a physical impact,
    /// then restores its original transform.
    /// </summary>
    public static Sequence Impact(
        GameObject gameObject,
        float squash = 0.82f,
        float angle = 4f,
        float duration = 0.1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;
        Quaternion originalRotation = target.localRotation;

        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(
                originalScale.y * squash,
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScaleY(
                originalScale.y,
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
    /// Quickly shakes the object's position to simulate a strong physical reaction.
    /// </summary>
    public static Sequence Shake(
        GameObject gameObject,
        float strength = 4f,
        float duration = 0.3f)
    {
        Transform target = BeginAnimation(gameObject);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOShakePosition(
                duration,
                new Vector3(strength, strength * 0.5f, 0f),
                15,
                90f,
                false,
                true
            )
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Quickly shakes the object with a small amount of random positional movement.
    /// Useful for nervous, frightened, or unstable objects.
    /// </summary>
    public static Sequence Shiver(
        GameObject gameObject,
        float strength = 2f,
        float duration = 0.25f)
    {
        Transform target = BeginAnimation(gameObject);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOShakePosition(
                duration,
                new Vector3(strength, strength * 0.5f, 0f),
                15,
                90f,
                false,
                true
            )
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Compresses the object, stretches it beyond its original size,
    /// then springs back to its original scale.
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


    /// <summary>
    /// Tilts the object from one side to the other before returning to its original rotation.
    /// </summary>
    public static Sequence Lean(
        GameObject gameObject,
        float angle = 8f,
        float duration = 0.15f)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;
        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ - angle
                ),
                duration * 2f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalRotateQuaternion(
                originalRotation,
                duration
            ).SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Performs a small rotation movement resembling a subtle nod.
    /// </summary>
    public static Sequence Nod(
        GameObject gameObject,
        float angle = 5f,
        float duration = 0.12f)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;
        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ - angle
                ),
                duration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle * 0.5f
                ),
                duration
            ).SetEase(Ease.InOutQuad)
        );

        sequence.Append(
            target.DOLocalRotateQuaternion(
                originalRotation,
                duration
            ).SetEase(Ease.OutBack)
        );

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Rotates the object by a random small amount and then returns it to its original rotation.
    /// </summary>
    public static Sequence LookAround(
        GameObject gameObject,
        float maxAngle = 8f,
        float duration = 0.15f)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;
        float angle = Random.Range(-maxAngle, maxAngle);
        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.AppendInterval(Random.Range(0.1f, 0.3f));

        sequence.Append(
            target.DOLocalRotateQuaternion(
                originalRotation,
                duration
            ).SetEase(Ease.InOutSine)
        );

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Idle Animations

    /// <summary>
    /// Continuously expands and contracts the object vertically to create a subtle breathing effect.
    /// </summary>
    public static Sequence Breathing(
        GameObject gameObject,
        float amount = 1.04f,
        float duration = 0.8f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(
                originalScale.y * amount,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOScaleY(
                originalScale.y,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Continuously tilts the object from side to side to create a subtle idle wobble.
    /// </summary>
    public static Sequence IdleWiggle(
        GameObject gameObject,
        float angle = 3f,
        float duration = 0.5f)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;
        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ - angle
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
    /// Continuously moves the object vertically up and down to create a floating effect.
    /// </summary>
    public static Sequence Float(
        GameObject gameObject,
        float distance = 0.05f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + distance,
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
    /// Continuously floats the object vertically while gently rotating it from side to side.
    /// </summary>
    public static Sequence FloatAndWiggle(
        GameObject gameObject,
        float distance = 0.05f,
        float angle = 3f,
        float duration = 0.8f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalPosition = target.localPosition;
        Quaternion originalRotation = target.localRotation;
        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalMoveY(
                originalPosition.y + distance,
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle
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
                    originalRotation.eulerAngles.y,
                    originalZ - angle
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Continuously sways the object from one side to the other, creating a gentle pendulum-like motion.
    /// </summary>
    public static Sequence Sway(
        GameObject gameObject,
        float angle = 4f,
        float duration = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Quaternion originalRotation = target.localRotation;
        float originalZ = originalRotation.eulerAngles.z;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ + angle
                ),
                duration
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalZ - angle
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
    /// Performs two quick scale pulses followed by a pause, creating a heartbeat-like idle animation.
    /// </summary>
    public static Sequence Heartbeat(
        GameObject gameObject,
        float amount = 1.08f,
        float pulseDuration = 0.1f,
        float pauseDuration = 0.8f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                originalScale * amount,
                pulseDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                pulseDuration
            ).SetEase(Ease.InOutQuad)
        );

        sequence.Append(
            target.DOScale(
                originalScale * (amount * 0.96f),
                pulseDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                pulseDuration
            ).SetEase(Ease.InOutQuad)
        );

        sequence.AppendInterval(pauseDuration);

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }


    /// <summary>
    /// Combines subtle scaling, vertical movement, and rotation into a generic looping idle animation.
    /// </summary>
    public static Sequence IdleLife(
        GameObject gameObject,
        float intensity = 1f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;
        Vector3 originalPosition = target.localPosition;
        Quaternion originalRotation = target.localRotation;

        float scaleAmount = 1f + (0.025f * intensity);
        float angle = 2.5f * intensity;
        float movement = 0.025f * intensity;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScaleY(
                originalScale.y * scaleAmount,
                0.6f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalMoveY(
                originalPosition.y + movement,
                0.6f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
            target.DOLocalRotate(
                new Vector3(
                    originalRotation.eulerAngles.x,
                    originalRotation.eulerAngles.y,
                    originalRotation.eulerAngles.z + angle
                ),
                0.6f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                0.8f
            ).SetEase(Ease.InOutSine)
        );

        sequence.Join(
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

        sequence.AppendInterval(Random.Range(0.5f, 2f));

        sequence.SetLoops(-1, LoopType.Restart);

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Spawn Animations

    /// <summary>
    /// Starts the object at a smaller scale, overshoots its original size, then settles into its normal scale.
    /// </summary>
    public static Sequence Pop(
        GameObject gameObject,
        float initialScale = 0.7f,
        float overshoot = 1.15f,
        float duration = 0.15f)
    {
        Transform target = BeginAnimation(gameObject);

        Vector3 originalScale = target.localScale;

        target.localScale = originalScale * initialScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            target.DOScale(
                originalScale * overshoot,
                duration
            ).SetEase(Ease.OutBack)
        );

        sequence.Append(
            target.DOScale(
                originalScale,
                duration
            ).SetEase(Ease.OutBounce)
        );

        return FinishAnimation(sequence, gameObject);
    }

    #endregion


    #region Utility

    /// <summary>
    /// Stops the current Tweenimation2D animation running on the GameObject.
    /// </summary>
    public static void Kill(GameObject gameObject)
    {
        if (gameObject == null)
            return;

        DOTween.Kill(gameObject);
    }


    /// <summary>
    /// Stops the current Tweenimation2D animation and restores the supplied transform values.
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
    /// Stops the previous Tweenimation2D animation and prepares the GameObject for a new animation.
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

