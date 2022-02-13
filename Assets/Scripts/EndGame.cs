using UnityEngine;

public class EndGame : MonoBehaviour {
    public AudioClip endGameAudio;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.collider.CompareTag("Player")) {
            GlobalGameSettings.beatGame = true;
            collision.collider.GetComponent<PlayerController>().PlayAudio(endGameAudio);
            Destroy(gameObject);
        }
    }
}
