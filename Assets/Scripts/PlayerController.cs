using UnityEngine;

public class PlayerController : MonoBehaviour {
    public AudioClip[] footstepSounds;

    private Rigidbody2D rb;
    private AudioSource audioSource;

    private float horizontal;
    private float vertical;

    private const float moveSpeed = 24f;
    private const float rotateSpeed = 150f;
    private const bool oneButtonAtATime = true;
    private Vector2 lastFootstepPosition;

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

        if (Vector2.Distance(lastFootstepPosition, transform.position) > 1f) {
            audioSource.Stop();
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
}
