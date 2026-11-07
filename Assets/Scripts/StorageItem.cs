using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OndeVai.Gameplay
{
 /// <summary>
/// A sortable object. A single prefab is reused for every item; <see cref="Initialize"/> applies the
/// sprite, collider, scale and physics of an <see cref="ItemTypeDefinition"/> variant.
/// Owns its presentation scale so spawn and absorb animations do not fight the depth scaling.
/// </summary>
public class Item : MonoBehaviour
{
    private const int CircleSegments = 20;

    [Tooltip("Fallback group used only when the item is not initialized from data.")]
    [SerializeField] private ItemGroup _itemType;
    [SerializeField] private bool _shouldScaleWithYPosition = true;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [Tooltip("Rebuilt from the variant sprite on every spawn.")]
    [SerializeField] private PolygonCollider2D _collider;

    private static readonly List<Vector2> PathBuffer = new(64);

    private DragAndDrop _dragAndDrop;
    private Rigidbody2D _rigidbody;
    private Vector3 _authoredScale;
    private float _contentScale = 1f;
    private Action<Item> _releaseHandler;

    public ItemGroup ItemType => _itemType;

    /// <summary>Type this item was created from, or null for legacy items.</summary>
    public ItemTypeDefinition Type { get; private set; }

    /// <summary>Variant this item displays, or null for legacy items.</summary>
    public ItemVariant Variant { get; private set; }

    /// <summary>Multiplier applied on top of the base scale. Animated by spawn and absorb effects.</summary>
    public float PresentationScale { get; set; } = 1f;

    /// <summary>Persistent size multiplier set by the game mode (e.g. larger items in question mode).</summary>
    public float BaseScale { get; set; } = 1f;

    /// <summary><see cref="Time.time"/> when the item became available to the child.</summary>
    public float AvailableSince { get; private set; }

    /// <summary>True while the item is being absorbed or animated and must ignore input.</summary>
    public bool IsResolving { get; private set; }

    public Rigidbody2D Body => _rigidbody;

    private void Awake()
    {
        _authoredScale = transform.localScale;
        TryGetComponent(out _dragAndDrop);
        TryGetComponent(out _rigidbody);
        if (_spriteRenderer == null)
            TryGetComponent(out _spriteRenderer);

        if (_collider == null)
            TryGetComponent(out _collider);

        AvailableSince = Time.time;
    }

    private void Update()
    {
        float scale = _contentScale * BaseScale * PresentationScale;
        if (_shouldScaleWithYPosition)
        {
            float normalizedY = Mathf.InverseLerp(-5f, 5f, transform.position.y);
            transform.localScale = Vector3.one * (Mathf.Lerp(1f, 0.6f, normalizedY) * scale);
        }
        else
        {
            transform.localScale = _authoredScale * scale;
        }
    }

    /// <summary>
    /// Applies a variant of a type and resets the runtime state so pooled items behave like new ones.
    /// </summary>
    public void Initialize(ItemTypeDefinition type, ItemVariant variant)
    {
        if (type == null || variant == null)
        {
            Debug.LogError("Item.Initialize needs a type and a variant.", this);
            return;
        }

        Type = type;
        Variant = variant;
        _itemType = type.Group;
        _shouldScaleWithYPosition = type.ScaleWithDepth;
        _contentScale = type.Scale * variant.Scale;
        name = string.IsNullOrEmpty(variant.Id) ? type.name : variant.Id;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = variant.Sprite;
            _spriteRenderer.color = variant.Tint;
        }
        else
        {
            Debug.LogError("Item prefab has no SpriteRenderer assigned.", this);
        }

        ApplyCollider(variant.Sprite, type.GetColliderShape(variant));
        ResetState(type);
    }

    /// <summary>Called by the spawner that owns this item. Despawned items go back to it instead of being destroyed.</summary>
    public void SetReleaseHandler(Action<Item> releaseHandler)
    {
        _releaseHandler = releaseHandler;
    }

    /// <summary>Removes the item from play: returns it to its pool, or destroys it when it has none.</summary>
    public void Despawn()
    {
        DOTween.Kill(this);
        transform.DOKill();
        if (_releaseHandler != null)
            _releaseHandler(this);
        else
            Destroy(gameObject);
    }

    /// <summary>Enables or disables dragging for this item.</summary>
    public void SetDraggable(bool draggable)
    {
        if (_dragAndDrop != null)
            _dragAndDrop.enabled = draggable;
    }

    /// <summary>Freezes physics so the item stays where gameplay places it.</summary>
    public void SetKinematic(bool kinematic)
    {
        if (_rigidbody == null)
            return;

        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.bodyType = kinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
    }

    /// <summary>Marks the item as resolved so it no longer accepts input.</summary>
    public void BeginResolve()
    {
        IsResolving = true;
        SetDraggable(false);
    }

    /// <summary>Resets the response timer, e.g. after a wrong answer.</summary>
    public void RestartResponseTimer()
    {
        AvailableSince = Time.time;
    }

    private void ResetState(ItemTypeDefinition type)
    {
        IsResolving = false;
        PresentationScale = 1f;
        BaseScale = 1f;
        AvailableSince = Time.time;
        transform.rotation = Quaternion.identity;
        SetDraggable(true);

        if (_rigidbody == null)
            return;

        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _rigidbody.gravityScale = type.GravityScale;
        _rigidbody.mass = type.Mass;
    }

    private void ApplyCollider(Sprite sprite, ItemColliderShape shape)
    {
        if (_collider == null)
        {
            Debug.LogError("Item prefab has no PolygonCollider2D assigned. Items will not collide or be draggable.", this);
            return;
        }

        if (sprite == null)
            return;

        if (shape == ItemColliderShape.PhysicsShape && TryApplyPhysicsShape(sprite))
            return;

        Bounds bounds = sprite.bounds;
        PathBuffer.Clear();
        if (shape == ItemColliderShape.Circle)
        {
            float radius = (bounds.extents.x + bounds.extents.y) * 0.5f;
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                PathBuffer.Add((Vector2)bounds.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
        else
        {
            PathBuffer.Add(new Vector2(bounds.min.x, bounds.min.y));
            PathBuffer.Add(new Vector2(bounds.max.x, bounds.min.y));
            PathBuffer.Add(new Vector2(bounds.max.x, bounds.max.y));
            PathBuffer.Add(new Vector2(bounds.min.x, bounds.max.y));
        }

        _collider.pathCount = 1;
        _collider.SetPath(0, PathBuffer);
    }

    private bool TryApplyPhysicsShape(Sprite sprite)
    {
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount == 0)
            return false;

        _collider.pathCount = shapeCount;
        for (int i = 0; i < shapeCount; i++)
        {
            sprite.GetPhysicsShape(i, PathBuffer);
            _collider.SetPath(i, PathBuffer);
        }

        return true;
    }
}
   
}
