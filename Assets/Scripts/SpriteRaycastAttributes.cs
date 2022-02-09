using UnityEngine;

public class SpriteRaycastAttributes : MonoBehaviour {
    public float zOffset = 0f;
    public float scale = 1f;
    public bool noZTest = false;

    void Start() {
        enabled = false;
    }
}
