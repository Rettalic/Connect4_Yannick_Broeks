using UnityEngine;
using UnityEngine.SceneManagement;

public class UpdateUI : MonoBehaviour
{
    [Header("Win/Lose Objects")]
    public GameObject winningText;
    public string playerWonText = "You Won!";
    public string playerLoseText = "You Lose!";
    public string drawText = "Draw!";
    public GameObject buttonPlayAgain;

    public void PlayAgain()
    {
        SceneManager.LoadScene(0);
    }
}
