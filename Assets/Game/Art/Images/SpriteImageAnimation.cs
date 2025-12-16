using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SpriteImageAnimation : MonoBehaviour
{
    [SerializeField] internal Image image;
    [SerializeField] internal List<Sprite> sprites;
    [SerializeField] float delay = .1f;
    public void StartAnim()
    {
        StartCoroutine(Anim());
    }
    IEnumerator Anim()
    {
        while (true)
        {
            foreach (var item in sprites)
            {
                image.sprite = item;
                yield return new WaitForSecondsRealtime(delay);
            }
        }

    }
}
