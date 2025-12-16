using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class VfxDestroyer : MonoBehaviour
{
    [Inject] PoolingSystem poolingSystem;

    public void DestroyObject(float time)
    {
        poolingSystem.DestroyAPS(gameObject, time);
    }
}
