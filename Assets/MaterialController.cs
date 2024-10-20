using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MaterialController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Material material;
    
    public string name;
    public float value;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = spriteRenderer.material;
    }

    void Update()
    {
        material.SetFloat(name, value);
    }
}
