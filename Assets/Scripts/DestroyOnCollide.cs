using UnityEngine;

public class DestroyOnCollide : MonoBehaviour {
    public AudioClip hitClip;

    private SpriteRaycastAttributes spriteRca;
    private float aliveTime = 5f;

    // Start is called before the first frame update
    void Start() {
        this.spriteRca = GetComponent<SpriteRaycastAttributes>();
    }

    // Update is called once per frame
    void Update() {
        aliveTime -= Time.deltaTime;

        if (aliveTime <= 0f) {
            Destroy(gameObject);
        }

        this.spriteRca.zOffset -= 75f * Time.deltaTime;

        if (this.spriteRca.zOffset < -50) {
            GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().PlayAudio(hitClip);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (!collision.collider.CompareTag("Enemy")) {
            GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().PlayAudio(hitClip);
        }
        Destroy(gameObject);
    }
}
