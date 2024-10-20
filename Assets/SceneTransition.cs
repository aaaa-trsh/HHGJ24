using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    private int sceneToGoTo = 0;
    public void ExitTo(int scene)
    {
        GetComponent<Animator>().Play("DoExit");
        sceneToGoTo = scene;
        Invoke("DoLoadScene", 1.3f);
    }

    void DoLoadScene()
    {
        SceneManager.LoadScene(sceneToGoTo);
    }
}
