// ============================================================================
// UNIT SPRITE ANIMATOR - 3-frame GIF animasyon sistemi
// Sprite array üzerinden frame bazlı animasyon yapar
// ============================================================================

using UnityEngine;

public class UnitSpriteAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] Sprite[] frames = new Sprite[3];
    [SerializeField] float frameRate = 0.15f; // Her frame süresi
    
    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private float timer = 0f;
    private bool isPlaying = true;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    private void Start()
    {
        if (frames.Length > 0 && frames[0] != null)
        {
            spriteRenderer.sprite = frames[0];
        }
    }
    
    public void SetFrames(Sprite[] animationFrames)
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            frames = animationFrames;
            currentFrame = 0;
            
            if (spriteRenderer != null && frames[0] != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }
    }
    
    private void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0) return;
        
        timer += Time.deltaTime;
        
        if (timer >= frameRate)
        {
            timer = 0f;
            NextFrame();
        }
    }
    
    private void NextFrame()
    {
        currentFrame = (currentFrame + 1) % frames.Length;
        
        if (frames[currentFrame] != null)
        {
            spriteRenderer.sprite = frames[currentFrame];
        }
    }
    
    public void Play()
    {
        isPlaying = true;
    }
    
    public void Stop()
    {
        isPlaying = false;
    }
    
    public void SetFrameRate(float newFrameRate)
    {
        frameRate = newFrameRate;
    }
}