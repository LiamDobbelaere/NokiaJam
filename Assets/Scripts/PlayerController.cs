using UnityEngine;

public class PlayerController : MonoBehaviour {
    public AudioClip[] footstepSounds;
    public AudioClip music;

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
        if (oneButtonAtATime) {
            horizontal = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(horizontal) < 0.1f) {
                horizontal = 0f;
                vertical = Input.GetAxisRaw("Vertical");
            } else {
                vertical = 0f;
            }

        } else {

        }

        transform.Rotate(0f, 0f, -horizontal * rotateSpeed * Time.deltaTime);

        if (!audioSource.isPlaying) {
            ContinueMusic();
        }

        if (Vector2.Distance(lastFootstepPosition, transform.position) > 1f) {
            HaltMusic();
            audioSource.time = 0f;
            audioSource.volume = 1f;
            audioSource.clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.Play();

            lastFootstepPosition = transform.position;
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        // TODO: add strafing
        rb.AddRelativeForce(new Vector2(0f, vertical) * moveSpeed);
        //rb.AddTorque(-horizontal * rotateSpeed);
    }

    private void HaltMusic() {
        musicTime = audioSource.time;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.Stop();
    }

    private void ContinueMusic() {
        audioSource.loop = true;
        audioSource.clip = music;
        audioSource.volume = 0f;

        audioSource.Play();
        audioSource.time = musicTime;
    }
}
