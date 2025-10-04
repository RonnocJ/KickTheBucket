using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSceneManager : Singleton<GameSceneManager>
{
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

        PInputManager.root.actions[PlayerActionType.Reload].bAction += () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        };

        StartCoroutine(ApplyLevelNextFrame());
    }

    private IEnumerator ApplyLevelNextFrame()
    {
        yield return null;

        LevelManager.root.LoadedLevelIndex = levelIndex;
        LevelManager.root.LoadCurrentLevel();
    }

    void Start()
    {
        Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnTextInput(char c)
    {
        if (!Application.isPlaying) return;

        string ch = c.ToString();

        if (int.TryParse(ch, out int i))
        {
            levelIndex = i;

            if (i <= LevelManager.root.Levels.Count - 1)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
