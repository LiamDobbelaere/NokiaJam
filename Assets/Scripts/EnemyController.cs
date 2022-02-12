using UnityEngine;

public class EnemyController : MonoBehaviour {
    [System.Serializable]
    public struct Sprites {
        public Sprite[] sprites;
    }

    public Sprites[] sprites;
    public AudioClip yell;

    private float framerate = 0.25f;
    private float framerateTimer;
    private int frameIndex = 0;

    private SpriteRaycastAttributes spriteRaycastAttributes;

    private Rigidbody2D rb;
    private Vector2 targetLocation;
    private GameObject player;

    private float moveSpeed = 4f;
    private const float lineOfSight = 10f;

    private float lastYellTimer = 0f;

    // Start is called before the first frame update
    void Start() {
        this.spriteRaycastAttributes = GetComponent<SpriteRaycastAttributes>();
        this.rb = GetComponent<Rigidbody2D>();
        this.player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update() {
        if (lastYellTimer > 0f) {
            lastYellTimer -= Time.deltaTime;
        }

        framerateTimer += Time.deltaTime;
        if (framerateTimer > framerate) {
            framerateTimer = 0f;
            frameIndex = (frameIndex + 1) % 4;

            spriteRaycastAttributes.quadAngleSprites = this.sprites[frameIndex].sprites;
        }

        RaycastHit2D canSeePlayer =
            Physics2D.Raycast(transform.position, player.transform.position - transform.position, lineOfSight, LayerMask.GetMask(new string[] { "World", "Player" }));

        if (canSeePlayer.collider.gameObject.layer == LayerMask.NameToLayer("Player")) {
            targetLocation = canSeePlayer.transform.position;
            transform.up = (targetLocation - (Vector2)transform.position).normalized;

            if (lastYellTimer <= 0f) {
                player.GetComponent<PlayerController>().PlayAudio(yell);

                lastYellTimer = Random.Range(3f, 5f);
            }
        }
    }

    private void FixedUpdate() {
        // TODO: add wander AI
        rb.AddForce((targetLocation - (Vector2)transform.position).normalized * moveSpeed);
    }
}
