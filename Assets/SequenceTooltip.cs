using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequenceTooltip : MonoBehaviour
{
    public InputSequence sequence;
    public TMPro.TextMeshProUGUI text;

    [System.Serializable]
    public class InputKey
    {
        public KeyCode key;
        public Animator animator;
    }

    public List<InputKey> inputKeys = new List<InputKey>();
    private Dictionary<KeyCode, Animator> keyToAnimator = new Dictionary<KeyCode, Animator>();
    public float waitTime = 0.1f;
    public float delay = 0f;

    void Awake()
    {
        text.text = sequence.name;
        foreach (var key in inputKeys)
        {
            keyToAnimator[key.key] = key.animator;
        }
    }

    void OnEnable()
    {
        StartCoroutine(Animate());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator Animate()
    {
        foreach (var key in keyToAnimator.Keys)
        {
            if (sequence.sequence.Contains(key))
                continue;

            keyToAnimator[key].Play("Disabled");
        }

        yield return new WaitForSeconds(delay);

        while (true)
        {
            foreach (var key in sequence.sequence)
            {
                keyToAnimator[key].Play("Hit", 0, 0);
                yield return new WaitForSeconds(waitTime);
            }

            yield return new WaitForSeconds(1);
        }
    }

}
