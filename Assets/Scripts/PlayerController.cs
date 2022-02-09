using UnityEngine;

public class PlayerController : MonoBehaviour {
    public AudioClip footstepSound;
    public AudioClip shootSound;
    public GameObject rock;

    private float fireCooldownTimer;
    private Rigidbody2D rb;
    private AudioSource audioSource;

    private float horizontal;
    private float vertical;

    private const float moveSpeed = 24f;
    private const float rotateSpeed = 150f;
    private const bool oneButtonAtATime = true;

    private Vector2 lastFootstepPosition;
    private float musicTime = 0f;

    // Start is called before the first frame update
    void Start() {
        this.rb = GetComponent<Rigidbody2D>();
        this.audioSource = GetComponent<AudioSource>();

        lastFootstepPosition = transform.position;
    }

    private void Update() {
        if (fireCooldownTimer > 0f) {
            fireCooldownTimer -= Time.deltaTime;
        }

        if (oneButtonAtATime) {
            horizontal = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(horizontal) < 0.1f) {
                horizontal = 0f;
                vertical = Input.GetAxisRaw("Vertical");

                if (Mathf.Abs(vertical) < 0.1f) {
                    bool fire = Input.GetButtonDown("PrimaryAction");

                    if (fire && !IsFireCooldownActive()) {
                        fireCooldownTimer = 1f;

                        GameObject bullet = Instantiate(rock, transform.position + transform.up * 0.5f, transform.rotation);
                        bullet.GetComponent<Rigidbody2D>().AddForce(transform.up * 500f);

                        PlayAudio(shootSound);
                    }
                }
            } else {
                vertical = 0f;
            }

        } else {

        }

        transform.Rotate(0f, 0f, -horizontal * rotateSpeed * Time.deltaTime);

        if (Vector2.Distance(lastFootstepPosition, transform.position) > 1f) {
            PlayAudio(footstepSound);
            lastFootstepPosition = transform.position;
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        // TODO: add strafing
        rb.AddRelativeForce(new Vector2(0f, vertical) * moveSpeed);
        //rb.AddTorque(-horizontal * rotateSpeed);
    }

    public bool IsFireCooldownActive() {
        return this.fireCooldownTimer > 0f;
    }

    public void PlayAudio(AudioClip clip) {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
