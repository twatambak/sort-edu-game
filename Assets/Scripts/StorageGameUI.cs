using UnityEngine;
using UnityEngine.UI;

public class StorageGameUI : MonoBehaviour
{
    [SerializeField] private int _matchTime = 30;
    [SerializeField] private Slider _slider;

    private float _remainingTime;

    private void Start()
    {
        _remainingTime = _matchTime;

        _slider.minValue = 0f;
        _slider.maxValue = _matchTime;
        _slider.value = _remainingTime;
    }

    private void Update()
    {
        if (_remainingTime <= 0f)
            return;

        _remainingTime -= Time.deltaTime;
        _remainingTime = Mathf.Max(_remainingTime, 0f);

        _slider.value = _remainingTime;
    }
}