using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSetupScreen : MonoBehaviour
{
    [SerializeField] private TMP_InputField _itemsToWinInput;
    [SerializeField] private TMP_InputField _matchTimeInput;
    [SerializeField] private TMP_InputField _delayBetweenSpawnsInput;
    [SerializeField] private TMP_InputField _errorLimitInput;
    [SerializeField] private TMP_InputField _maxItemsInput;

    [SerializeField] private Button _startButton;

    public void StartGame()
    {
        int itemsToWin = ReadNumber(_itemsToWinInput, 10);
        int maxItems = ReadNumber(_maxItemsInput, 5);
        int matchTime = ReadNumber(_matchTimeInput, 30);
        int errorLimit = ReadNumber(_errorLimitInput, 3);
        int delayBetweenSpawns = ReadNumber(_delayBetweenSpawnsInput, 3);

        GameConfiguration selectedConfig = new(matchTime, errorLimit, maxItems, itemsToWin, delayBetweenSpawns, ItemGroup.Toy, ItemGroup.Tool);

        GameController.Instance.BeginGame(selectedConfig);

        gameObject.SetActive(false);
    }

    private void SetFields(int itemsToWin, int maxItems, int matchTime, int errorLimit, int delayBetweenSpawns)
    {
        _itemsToWinInput.text = itemsToWin.ToString();
        _maxItemsInput.text = maxItems.ToString();
        _matchTimeInput.text = matchTime.ToString();
        _errorLimitInput.text = errorLimit.ToString();
        _delayBetweenSpawnsInput.text = delayBetweenSpawns.ToString();
    }



    private static int ReadNumber(TMP_InputField input, int fallback)
    {
        return int.TryParse(input.text, out int value) ? Mathf.Max(1, value) : fallback;
    }
}