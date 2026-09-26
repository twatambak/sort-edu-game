using TMPro;
using UnityEngine;

public class GameVersion : MonoBehaviour
{
    [SerializeField] private TMP_Text _gameVersionText;
    
    void Start()
    {
        _gameVersionText.text = $"v{Application.version}";
    }

}
