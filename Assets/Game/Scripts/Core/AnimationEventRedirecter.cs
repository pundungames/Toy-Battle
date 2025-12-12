using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEventRedirecter : MonoBehaviour
{
    [SerializeField] UnityEvent action;
    public void AnimationEvent() => action?.Invoke();
}
