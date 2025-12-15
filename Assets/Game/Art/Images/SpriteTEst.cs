using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SpriteTEst : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] List<Sprite> sprites;
    [SerializeField] float delay = .1f;
    private void Start()
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
