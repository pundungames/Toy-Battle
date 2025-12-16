using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteImageAnimation : MonoBehaviour
{
    [SerializeField] internal Image image;
    [SerializeField] internal List<Sprite> sprites;
    [SerializeField] float delay = .1f;

    private RectTransform rectTransform;

    [Header("Maksimum Boyutlar")]
    [SerializeField] private float maxWidth = 310f;
    [SerializeField] private float maxHeight = 256f;

    private Coroutine animCoroutine;

    [SerializeField] private bool autoStartOnEnable = true;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // GameObject aktif olunca otomatik baþlat
        if (autoStartOnEnable && sprites != null && sprites.Count > 0)
        {
            StartAnim();
        }
    }

    public void StartAnim()
    {
        // GameObject aktif deðilse baþlatma
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot start animation - GameObject is not active!");
            return;
        }

        // Eðer zaten çalýþan bir animasyon varsa önce durdur
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }

        animCoroutine = StartCoroutine(Anim());
    }

    public void StopAnim()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }

    private void OnDisable()
    {
        // GameObject deaktif olduðunda coroutine'i temizle
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }

    IEnumerator Anim()
    {
        while (true)
        {
            foreach (var item in sprites)
            {
                image.sprite = item;
                SetSizeWithinLimits();
                yield return new WaitForSecondsRealtime(delay);
            }
        }
    }

    private void SetSizeWithinLimits()
    {
        // Önce native size'a ayarla
        image.SetNativeSize();

        // Mevcut boyutlarý al
        float currentWidth = rectTransform.sizeDelta.x;
        float currentHeight = rectTransform.sizeDelta.y;

        // Maksimum sýnýrlarý aþýyor mu kontrol et
        float widthScale = maxWidth / currentWidth;
        float heightScale = maxHeight / currentHeight;

        // Her iki scale faktöründen küçük olaný seç (aspect ratio korunsun)
        float scale = Mathf.Min(widthScale, heightScale);

        // Eðer scale 1'den küçükse (yani sýnýrlarý aþýyorsa), boyutlarý küçült
        if (scale < 1f)
        {
            rectTransform.sizeDelta = new Vector2(
                currentWidth * scale,
                currentHeight * scale
            );
        }
    }
}