using System.Threading;
using UnityEngine;
using NaughtyAttributes;

public class SingletonBase<T> : MonoBehaviour where T : Component
{
    [SerializeField, BoxGroup("Singleton")] private bool _dontDestroyOnLoad = true;

    private static T _instance;
    private static bool _applicationIsQuitting;

    private CancellationTokenSource _sourceToken;

    public static T Instance => GetInstance();

    protected CancellationToken CancellationToken { get; private set; }

    public static T GetInstance()
    {
        if (_applicationIsQuitting)
            return null;

        return _instance;
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        _sourceToken = new CancellationTokenSource();
        CancellationToken = _sourceToken.Token;
    }

    protected virtual void OnDestroy()
    {
        if (_instance != this)
            return;

        _sourceToken?.Cancel();
        _sourceToken?.Dispose();
        _sourceToken = null;

        _instance = null;
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}