using UnityEngine;
using UnityEngine.UI;

public class TempKeyVisual : MonoBehaviour
{
    public KeyCode key;
    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
    }

    void Update()
    {
        image.color = Input.GetKey(key) ? Color.red : Color.white;
    }
}
