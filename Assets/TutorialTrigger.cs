using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public GameObject tutorial;
    public GameObject cam;
    public bool startOff = true;
    void Start()
    {
        tutorial.SetActive(!startOff);
        cam.SetActive(!startOff);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            tutorial.SetActive(true);
            cam.SetActive(true);
        }
    }
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            tutorial.SetActive(true);
            cam.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            tutorial.SetActive(false);
            cam.SetActive(false);
        }
    }
}
