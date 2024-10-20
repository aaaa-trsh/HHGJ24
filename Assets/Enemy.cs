using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Enemy : MonoBehaviour
{
    public int maxHp = 3;
    public int hp = 3;

    public void TakeDamage(int damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}
