using UnityEngine;

public class EnemyController : MonoBehaviour {
    [System.Serializable]
    public struct Sprites {
        public Sprite[] sprites;
    }

    public Sprites[] sprites;
    public Sprite[] ripSprites;
    public AudioClip yell;
    public AudioClip die;

    private float framerate = 0.25f;
    private float framerateTimer;
    private int frameIndex = 0;

    private SpriteRaycastAttributes spriteRaycastAttributes;

    private Rigidbody2D rb;
    private Vector2 targetLocation;
    private GameObject player;

    private float moveSpeed = 6f;
    private const float lineOfSight = 10f;

    private float lastYellTimer = 0f;
    private float lastWanderTimer = 0f;

    private bool isDead = false;

    // Start is called before the first frame update
    void Start() {
        this.spriteRaycastAttributes = GetComponent<SpriteRaycastAttributes>();
        this.rb = GetComponent<Rigidbody2D>();
        this.player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update() {
        if (isDead || GlobalGameSettings.isPaused) {
            return;
        }

        if (lastYellTimer > 0f) {
            lastYellTimer -= Time.deltaTime;
        }

        if (lastWanderTimer > 0f) {
            lastWanderTimer -= Time.deltaTime;
        }

        framerateTimer += Time.deltaTime;
        if (framerateTimer > framerate) {
            framerateTimer = 0f;
            frameIndex = (frameIndex + 1) % 4;

            spriteRaycastAttributes.quadAngleSprites = this.sprites[frameIndex].sprites;
        }

        RaycastHit2D canSeePlayer =
            Physics2D.Raycast(transform.position, player.transform.position - transform.position, lineOfSight, LayerMask.GetMask(new string[] { "World", "Player" }));

        if (canSeePlayer.collider != null && canSeePlayer.collider.gameObject.layer == LayerMask.NameToLayer("Player")) {
            targetLocation = canSeePlayer.transform.position;
            transform.up = (targetLocation - (Vector2)transform.position).normalized;

            if (lastYellTimer <= 0f) {
                player.GetComponent<PlayerController>().PlayAudio(yell);

                lastYellTimer = Random.Range(3f, 5f);
            }
        } else {
            if (lastWanderTimer <= 0f) {
                Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

                RaycastHit2D worldHit = Physics2D.Raycast(transform.position,
                    randomDir, lineOfSight * 10f, LayerMask.GetMask(new string[] { "World" }));

                if (worldHit.collider != null) {
                    targetLocation = worldHit.point;
                    transform.up = (targetLocation - (Vector2)transform.position).normalized;
                }

                lastWanderTimer = Random.Range(3f, 5f);
            }
        }
    }

    private void FixedUpdate() {
        if (isDead || GlobalGameSettings.isPaused) {
            return;
        }

        rb.AddForce((targetLocation - (Vector2)transform.position).normalized * moveSpeed);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        if (isDead || GlobalGameSettings.isPaused) {
            return;
        }

        if (collision.collider.CompareTag("Player")) {
            collision.collider.GetComponent<PlayerController>().TakeDamage();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (isDead || GlobalGameSettings.isPaused) {
            return;
        }

        if (collision.collider.CompareTag("PlayerBullet")) {
            player.GetComponent<PlayerController>().PlayAudio(die);
            spriteRaycastAttributes.quadAngleSprites = ripSprites;
            spriteRaycastAttributes.zOffset = -10;
            spriteRaycastAttributes.scale = 0.5f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            isDead = true;
        }
    }
}
