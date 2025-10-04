using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSceneManager : Singleton<GameSceneManager>
{
    [SerializeField] private GameObject levelSelect;
    [SerializeField] private Animator levelSelectAnim;
    [SerializeField] private TMP_InputField levelText;
    private int levelIndex;
    protected override void Awake()
    {
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PInputManager.root.ClearActions();

        StartCoroutine(ApplyLevelNextFrame());
    }

    private IEnumerator ApplyLevelNextFrame()
    {
        yield return null;

        PInputManager.root.actions[PlayerActionType.Reload].bAction += () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        };

        PInputManager.root.actions[PlayerActionType.Menu].bAction += ToggleMenu;
        PInputManager.root.actions[PlayerActionType.Close].bAction += SetLevel;

        LevelManager.root.LoadedLevelIndex = levelIndex;
        LevelManager.root.LoadCurrentLevel();
    }
    private void ToggleMenu()
    {
        if (!levelSelect.activeInHierarchy)
        {
            levelSelect.SetActive(true);
            levelText.ActivateInputField();
        }
        else
        {
            levelSelect.SetActive(false);
        }
    }
    private void SetLevel()
    {
        if (!levelSelect.activeInHierarchy) return;

        if (int.TryParse(levelText.text, out int j) && j >= 0 && j < LevelManager.root.Levels.Count)
        {
            levelIndex = j;
            levelSelect.SetActive(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            levelSelectAnim.SetTrigger("invalid");
            levelText.ActivateInputField();
        }
    }
}
