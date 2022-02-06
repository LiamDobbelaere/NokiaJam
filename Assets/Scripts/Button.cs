using UnityEngine;

public class Button : MonoBehaviour {
    public Sprite normalSprite;
    public Sprite pressedSprite;

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
                this.spriteRenderer.sprite = pressedSprite;
            } else {
                this.spriteRenderer.sprite = normalSprite;
            }
        }
    }
}
