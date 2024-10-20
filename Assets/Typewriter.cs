using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoreManager : MonoBehaviour
{
    [System.Serializable]
    public class LoreScreen
    {
        public GameObject screenObj;
        public string screenText;
    }


    public List<LoreScreen> loreScreens = new List<LoreScreen>();
    public float textSpeed = 0.05f;
    public int sceneToGoTo = 0;
    private int currentScreen = 0;
    private TMPro.TextMeshProUGUI text;
    private bool isTyping => text.maxVisibleCharacters < text.text.Length;

    void Start()
    {
        text = GetComponent<TMPro.TextMeshProUGUI>();
        ShowScreen(currentScreen);
    }

    void ShowScreen(int index)
    {
        text.maxVisibleCharacters = 0;
        text.text = loreScreens[index].screenText;
        StartCoroutine(AnimateText());

        foreach (var screen in loreScreens)
        {
            screen.screenObj.SetActive(false);
        }

        loreScreens[index].screenObj.SetActive(true);
    }

    IEnumerator AnimateText()
    {
        while (isTyping)
        {
            text.maxVisibleCharacters++;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                text.maxVisibleCharacters = text.text.Length;
                StopAllCoroutines();
            }
            else
            {
                currentScreen++;
                if (currentScreen >= loreScreens.Count)
                {
                    foreach (var screen in loreScreens)
                    {
                        screen.screenObj.SetActive(false);
                    }
                    text.text = "";
                    Invoke("LoadScene", .2f);
                    return;
                }
                ShowScreen(currentScreen);
            }
        }
    }

    void LoadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToGoTo);
    }
}
