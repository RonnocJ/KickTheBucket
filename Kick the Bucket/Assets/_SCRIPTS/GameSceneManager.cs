using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : Singleton<GameSceneManager>
{
    void Start()
    {
        PInputManager.root.actions[PlayerActionType.Reload].bAction += () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}