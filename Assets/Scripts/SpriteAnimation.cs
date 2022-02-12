using UnityEngine;

public class SpriteAnimation : MonoBehaviour {
    public Sprite[] sprites;
    public float animationSpeed = 0.1f;
    public int currentSpriteIndex = 0;

    private SpriteRenderer spriteRenderer;
    private float animationTimer = 0f;

    // Start is called before the first frame update
    void Start() {
        this.spriteRenderer = this.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update() {
        animationTimer += Time.deltaTime;
        if (animationTimer > animationSpeed) {
            animationTimer = 0f;

            if (++currentSpriteIndex >= sprites.Length) {
                currentSpriteIndex = 0;
            }

            this.spriteRenderer.sprite = sprites[currentSpriteIndex];
        }
    }
}
