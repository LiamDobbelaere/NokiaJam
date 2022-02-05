using UnityEngine;
using UnityEngine.UI;

public class Raycaster : MonoBehaviour {
    private Image image;

    private Texture2D surface;
    private const float fov = 80f;
    private const int surfaceWidth = 84;
    private const int surfaceHeight = 48;
    private float fovIncrement;
    public float maxDistanceFactor = 12f;

    private Color[] barColorBuffer = new Color[surfaceWidth * surfaceHeight];
    private GameObject player;

    private Color nokiaBack = new Color(199 / 256f, 240 / 256f, 216 / 256f);
    private Color nokiaFront = new Color(67 / 256f, 82 / 256f, 61 / 256f);

    private const float artificialFramerateValue = 0.05f;
    private float artificialFramerate = artificialFramerateValue;

    // Start is called before the first frame update
    void Start() {
        image = GetComponent<Image>();

        surface = new Texture2D(surfaceWidth, surfaceHeight);
        surface.filterMode = FilterMode.Point;

        image.sprite = Sprite.Create(surface, new Rect(0, 0, surface.width, surface.height), new Vector2(0f, 0f));
        image.enabled = true;

        player = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < barColorBuffer.Length; i++) {
            barColorBuffer[i] = Color.white;
        }

        fovIncrement = fov / surfaceWidth;
    }

    // Update is called once per frame
    void Update() {
        artificialFramerate -= Time.deltaTime;
        if (artificialFramerate < 0f) {
            artificialFramerate = artificialFramerateValue;
        } else {
            //return;
        }

        Debug.DrawRay(player.transform.position, player.transform.up, Color.yellow);
        surface.Clear(nokiaBack);

        float startAngle = fov * 0.5f;
        float endAngle = -startAngle;
        int x = 0;
        for (float angle = startAngle; angle > endAngle; angle -= fovIncrement) {
            Vector2 rayVector = ((Vector2)player.transform.up).Rotate(angle);

            RaycastHit2D hit = Physics2D.Raycast(player.transform.position, rayVector, 10f, LayerMask.GetMask(new string[] { "World" }));
            if (hit.collider != null) {
                float distance = hit.distance;
                float closenessFactor = 1f - Mathf.Max(Mathf.Min((distance / maxDistanceFactor), maxDistanceFactor), 0f);
                int barSize = Mathf.RoundToInt(closenessFactor * (surfaceHeight + 2));


                int y = Mathf.RoundToInt(surfaceHeight * 0.5f - barSize * 0.5f);
                int skip = Mathf.RoundToInt(closenessFactor * closenessFactor * 10) + 1;
                for (int i = 0; i < barSize; i++) {
                    //float distanceFromMiddle = 1f - (Mathf.Abs(((barSize * 0.5f) - i)) / barSize * 0.5f);
                    //barColorBuffer[i] = new Color(closenessFactor * distanceFromMiddle, closenessFactor * distanceFromMiddle, closenessFactor * distanceFromMiddle);

                    if (closenessFactor > 0.6f) {
                        barColorBuffer[i] = nokiaFront;
                    } else {
                        barColorBuffer[i] = i % skip == 0 ? nokiaBack : nokiaFront;
                    }
                }

                surface.SetPixels(x, y, 1, barSize, barColorBuffer);

                Debug.DrawLine(
                    (Vector2)player.transform.position,
                    hit.point,
                    Color.blue,
                    artificialFramerateValue
                );
            } else {
                Debug.DrawLine(
                    (Vector2)player.transform.position,
                    (Vector2)player.transform.position + (((Vector2)player.transform.up).Rotate(angle)) * 50f,
                    Color.cyan,
                    artificialFramerateValue
                );
            }

            x++;
        }

        surface.Apply();
    }
}
