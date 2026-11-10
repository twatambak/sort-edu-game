using System;
using DG.Tweening;
using OndeVai.Gameplay;
using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>
    /// Reports positions along the path of the item the child is holding, one every
    /// <see cref="StepDistance"/> world units, so a trail can be drawn. Only ticks while an item is held.
    /// </summary>
    public sealed class HeldItemTrail
    {
        private const float SampleInterval = 0.04f;
        private const float StepDistance = 0.3f;

        private readonly Action<Item, Vector3> _onStep;
        private Item _item;
        private Vector3 _lastPosition;
        private Tween _sampler;

        public HeldItemTrail(Action<Item, Vector3> onStep)
        {
            _onStep = onStep ?? throw new ArgumentNullException(nameof(onStep));
        }

        public void Begin(Item item)
        {
            End();
            if (item == null)
                return;

            _item = item;
            _lastPosition = item.transform.position;
            _sampler = DOTween.Sequence()
                .AppendInterval(SampleInterval)
                .AppendCallback(Sample)
                .SetLoops(-1)
                .SetLink(item.gameObject);
        }

        public void End()
        {
            _sampler?.Kill();
            _sampler = null;
            _item = null;
        }

        private void Sample()
        {
            if (_item == null)
            {
                End();
                return;
            }

            Vector3 position = _item.transform.position;
            if ((position - _lastPosition).sqrMagnitude < StepDistance * StepDistance)
                return;

            _lastPosition = position;
            _onStep(_item, position);
        }
    }
}
