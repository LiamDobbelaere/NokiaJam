using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour {
    public Sprite normalSprite;
    public Sprite pressedSprite;
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    private SpriteRenderer spriteRenderer;
    private float boxTestTimer = 0f;

    void Start() {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update() {
        boxTestTimer += Time.deltaTime;
        if (boxTestTimer > 0.3f) {
            boxTestTimer = 0f;
            Collider2D col = Physics2D.OverlapCircle(transform.position, 0.1f, LayerMask.GetMask(new string[] { "Sprites" }));

            if (col != null) {
                if (this.spriteRenderer.sprite != pressedSprite && onPressed != null) {
                    onPressed.Invoke();
                }

                this.spriteRenderer.sprite = pressedSprite;
            } else {
                if (this.spriteRenderer.sprite != normalSprite && onReleased != null) {
                    onReleased.Invoke();
                }

                this.spriteRenderer.sprite = normalSprite;
            }
        }
    }
}
