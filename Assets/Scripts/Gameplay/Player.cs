using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable, IInputProvider
{
    
    public Vector2 Move { get; }

    public bool Fireheld => throw new NotImplementedException();

    public Device lastDevice => throw new NotImplementedException();

    public event Action OnBombPressed;
    public event Action OnCrouched;

    public void TakeDamage(float damage)
    {
        throw new System.NotImplementedException();
    }

    void Start()
    {
    }
}
