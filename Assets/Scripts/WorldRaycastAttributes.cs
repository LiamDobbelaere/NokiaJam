using UnityEngine;

public class WorldRaycastAttributes : MonoBehaviour {
    public bool isTranslucent = false;
    public string renderStyle = "h";
    public Sprite texture;
    void Start() {
        enabled = false;
    }
}
