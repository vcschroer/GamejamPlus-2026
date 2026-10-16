using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    public void PlayGame(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void OpenOptions()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}