using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Header("Win/Lose Objects")]
    [SerializeField] private TextMeshProUGUI winningText;
    [SerializeField] private RectTransform content;

    [SerializeField] private string playerWonText = "You Won!";
    [SerializeField] private string playerLoseText = "You Lose!";
    [SerializeField] private string drawText = "Draw!";

    private void OnEnable()
    {
        GameController.OnGameOver += OnGameOver;
        content.gameObject.SetActive(false);

    }
    private void OnDisable()
    {
        GameController.OnGameOver -= OnGameOver;
    }

    private void OnGameOver(GameOverState _state)
    {
        content.gameObject.SetActive(true);
        winningText.text = _state switch
        {
            GameOverState.draw => drawText,
            GameOverState.lose => playerLoseText,
            GameOverState.win => playerWonText,
            _ => throw new NotImplementedException(),
        };
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
