using UnityEngine;
using UnityEngine.UI;

public class Raycaster : MonoBehaviour {
    private Image image;

    private Texture2D surface;
    private const float fov = 80f;
    private const int surfaceWidth = 84;
    private const int surfaceHeight = 48;
    private const bool forceGrayscale = false;
    private const bool lensCorrection = true;
    private float fovIncrement;
    public float maxDistanceFactor = 100f;

    private Color[] barColorBuffer;
    private GameObject player;

    private Color clearColor;
    private Color nokiaBack = new Color(199 / 256f, 240 / 256f, 216 / 256f);
    private Color nokiaFront = new Color(67 / 256f, 82 / 256f, 61 / 256f);

    private const float artificialFramerateValue = 0.05f;
    private float artificialFramerate = artificialFramerateValue;

    // Start is called before the first frame update
    void Start() {
        SetupSurface();
        SetupSprite();

        player = GameObject.FindGameObjectWithTag("Player");

        if (forceGrayscale) {
            clearColor = Color.black;
        } else {
            clearColor = nokiaBack;
        }

        barColorBuffer = new Color[surfaceWidth * surfaceHeight];
        barColorBuffer.Fill(clearColor);

        fovIncrement = fov / surfaceWidth;
    }

    private void SetupSurface() {
        surface = new Texture2D(surfaceWidth, surfaceHeight);
        surface.filterMode = FilterMode.Point;
    }

    private void SetupSprite() {
        image = GetComponent<Image>();

        image.sprite = Sprite.Create(surface, new Rect(0, 0, surface.width, surface.height), new Vector2(0f, 0f));
        image.enabled = true;
    }

    // Update is called once per frame
    void Update() {
        artificialFramerate -= Time.deltaTime;
        if (artificialFramerate < 0f) {
            artificialFramerate = artificialFramerateValue;
        } else {
            //return;
        }

        surface.Clear(clearColor);

        DrawCeiling();

        float startAngle = fov * 0.5f;
        float endAngle = -startAngle;
        int x = 0;
        for (float angle = startAngle; angle > endAngle; angle -= fovIncrement) {
            if (x >= surfaceWidth) {
                continue;
            }

            Vector2 rayVector = ((Vector2)player.transform.up).Rotate(angle);

            RaycastHit2D hit = Physics2D.Raycast(player.transform.position, rayVector, maxDistanceFactor, LayerMask.GetMask(new string[] { "World" }));
            if (hit.collider != null) {
                float baseDistance = hit.distance;
                float distance = baseDistance;
                if (lensCorrection) {
                    distance = Mathf.Cos(angle * Mathf.Deg2Rad) * baseDistance;
                }

                float closenessFactor = 1f - Mathf.Max(Mathf.Min((distance / maxDistanceFactor), maxDistanceFactor), 0f);
                int barSize = Mathf.RoundToInt(closenessFactor * surfaceHeight);


                int y = Mathf.RoundToInt(surfaceHeight * 0.5f - barSize * 0.5f);
                int skip = Mathf.RoundToInt(
                    closenessFactor * closenessFactor * 10
                ) + 1;
                for (int i = 0; i < barSize; i++) {
                    if (forceGrayscale) {
                        //float distanceFromMiddle = 1f - (Mathf.Abs(((barSize * 0.5f) - i)) / barSize * 0.5f);
                        float distanceFromMiddle = 1f;
                        barColorBuffer[i] = new Color(closenessFactor * distanceFromMiddle, closenessFactor * distanceFromMiddle, closenessFactor * distanceFromMiddle);
                    } else {
                        if (closenessFactor > 0.8f) {
                            barColorBuffer[i] = nokiaFront;
                        } else {
                            barColorBuffer[i] = i % skip == 0 ? nokiaBack : nokiaFront;
                        }
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

        Debug.DrawLine(player.transform.position, player.transform.position + player.transform.up * 50f, Color.yellow);

        //ApplyFloydSteinbergDither();

        surface.Apply();
    }

    private void DrawCeiling() {
        Color[] floorColorBuffer = new Color[surface.width * surface.height / 2];
        floorColorBuffer.Fill(nokiaFront);

        surface.SetPixels(0, 0, surface.width, surface.height / 2, floorColorBuffer);
    }

    private void ApplyFloydSteinbergDither() {
        Color32[] pixels = surface.GetPixels32();
        for (int y = 0; y < surface.height; y++) {
            for (int x = 0; x < surface.width; x++) {
                Color32 oldPixel = pixels.GetCoordinate(x, y, surface.width);
                Color32 newPixel = oldPixel.r < 128 ? nokiaBack : nokiaFront;
                pixels.SetCoordinate(x, y, surface.width, newPixel);

                uint oldPixelUInt = oldPixel.ToUInt();
                uint newPixelUInt = newPixel.ToUInt();

                float quantizationError = oldPixelUInt - newPixelUInt;

                if (IsInRange(x + 1, y, surface.width, surface.height)) {
                    float newValue = pixels.GetCoordinate(x + 1, y, surface.width).ToUInt() + quantizationError * (7.0f / 16.0f);
                    pixels.SetCoordinate(
                        x + 1, y, surface.width,
                        ((uint)newValue).ToColor()
                    );
                }

                if (IsInRange(x - 1, y + 1, surface.width, surface.height)) {
                    float newValue = pixels.GetCoordinate(x - 1, y + 1, surface.width).ToUInt() + quantizationError * (3.0f / 16.0f);
                    pixels.SetCoordinate(
                        x - 1, y + 1, surface.width,
                        ((uint)newValue).ToColor()
                    );
                }

                if (IsInRange(x, y + 1, surface.width, surface.height)) {
                    float newValue = pixels.GetCoordinate(x, y + 1, surface.width).ToUInt() + quantizationError * (5.0f / 16.0f);
                    pixels.SetCoordinate(
                        x, y + 1, surface.width,
                        ((uint)newValue).ToColor()
                    );
                }

                if (IsInRange(x + 1, y + 1, surface.width, surface.height)) {
                    float newValue = pixels.GetCoordinate(x + 1, y + 1, surface.width).ToUInt() + quantizationError * (1.0f / 16.0f);
                    pixels.SetCoordinate(
                        x + 1, y + 1, surface.width,
                        ((uint)newValue).ToColor()
                    );
                }
            }
        }

        surface.SetPixels32(pixels);
    }

    private bool IsInRange(int x, int y, int width, int height) {
        return x >= 0 && y >= 0 && x < width && y < height;
    }
}
