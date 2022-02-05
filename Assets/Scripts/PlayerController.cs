using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Rigidbody2D rb;
    private float horizontal;
    private float vertical;

    private const float moveSpeed = 24f;
    private const float rotateSpeed = 150f;

    // Start is called before the first frame update
    void Start() {
        this.rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        transform.Rotate(0f, 0f, -horizontal * rotateSpeed * Time.deltaTime);
    }

    // Update is called once per frame
    void FixedUpdate() {
        // TODO: add strafing
        rb.AddRelativeForce(new Vector2(0f, vertical) * moveSpeed);
        //rb.AddTorque(-horizontal * rotateSpeed);
    }
}
