using System.Collections.Generic;
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
    private const float maxDistanceFactor = 12f;
    private const float maxSpriteDistanceFactor = 16f;

    private Color[] barColorBuffer;
    private GameObject player;

    private Color clearColor;
    private Color nokiaBack = new Color(199 / 256f, 240 / 256f, 216 / 256f);
    private Color nokiaFront = new Color(67 / 256f, 82 / 256f, 61 / 256f);

    private const float artificialFramerateValue = 0.064f;
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
            return;
        }

        surface.Clear(clearColor);

        DrawCeiling();

        float startAngle = fov * 0.5f;
        float endAngle = -startAngle;
        int x = 0;
        List<Collider2D> spriteColliders = new List<Collider2D>();
        for (float angle = startAngle; angle > endAngle; angle -= fovIncrement) {
            if (x >= surfaceWidth) {
                continue;
            }

            Vector2 rayVector = ((Vector2)player.transform.up).Rotate(angle).normalized;
            RaycastHit2D hit = Physics2D.Raycast(player.transform.position, rayVector, maxDistanceFactor, LayerMask.GetMask(new string[] { "World" }));
            if (hit.collider != null) {
                DrawWallColumn(x, angle, hit);
                Debug.DrawLine(
                   (Vector2)player.transform.position,
                   hit.point,
                   Color.blue,
                   artificialFramerateValue
               );

                RaycastHit2D[] spritesHit = Physics2D.RaycastAll(
                    player.transform.position, rayVector, maxSpriteDistanceFactor, LayerMask.GetMask(new string[] { "SpritesNoCollision" })
                );
                //Debug.DrawLine(player.transform.position, (Vector2)player.transform.position + rayVector * maxSpriteDistanceFactor, Color.green);

                foreach (RaycastHit2D spriteHit in spritesHit) {
                    if (spriteHit.collider != null && spriteHit.distance < hit.distance && !spriteColliders.Contains(spriteHit.collider)) {
                        spriteColliders.Add(spriteHit.collider);
                    }
                }
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

        /*GameObject[] spriteObjects = GameObject.FindGameObjectsWithTag("SpriteNoCollision");
        foreach (GameObject spriteObject in spriteObjects) {
            spriteColliders.Add(spriteObject.GetComponent<Collider2D>());
        }*/

        foreach (Collider2D spriteCollider in spriteColliders) {
            Vector2 vectorToSprite = spriteCollider.transform.position - player.transform.position;
            float angleToSprite = Vector2.SignedAngle(vectorToSprite, player.transform.up);
            if (angleToSprite < fov * -0.5f || angleToSprite > fov * 0.5f) {
                continue;
            }

            SpriteRenderer spriteRenderer = spriteCollider.gameObject.GetComponent<SpriteRenderer>();

            float baseDistance = Vector2.Distance(spriteCollider.transform.position, player.transform.position);
            float distance = baseDistance;
            if (lensCorrection) {
                distance = Mathf.Cos(angleToSprite * Mathf.Deg2Rad) * baseDistance;
            }

            float closenessFactor = 1f / distance; //2f - Mathf.Max(Mathf.Min((distance / 3f), 3f), 0f);

            int targetWidth = Mathf.RoundToInt(spriteRenderer.sprite.texture.width * closenessFactor);
            int targetHeight = Mathf.RoundToInt(spriteRenderer.sprite.texture.height * closenessFactor);
            if (targetWidth < 1 || targetHeight < 1) {
                continue;
            }

            Texture2D spriteTexture = spriteRenderer.sprite.texture.ResizeNN(targetWidth, targetHeight);

            float screenXPosition = (angleToSprite / (fov * 0.5f)) * (surface.width * 0.5f);

            int drawX = Mathf.RoundToInt(surface.width * 0.5f + screenXPosition);
            int drawY = Mathf.RoundToInt(surfaceHeight * 0.5f - spriteTexture.height * 0.5f);

            int spritePixelX = -1;
            int spritePixelY = -1;
            for (int surfY = drawY; surfY < drawY + spriteTexture.height; surfY++) {
                spritePixelY++;

                for (int surfX = drawX; surfX < drawX + spriteTexture.width; surfX++) {
                    spritePixelX++;

                    Color spritePixel = spriteTexture.GetPixel(spritePixelX, spriteTexture.height - 1 - spritePixelY);

                    if (spritePixel.a < 0.5f) {
                        continue;
                    }

                    if (surfX < 0 || surfX >= surface.width || surfY < 0 || surfY >= surface.height) {
                        continue;
                    }

                    surface.SetPixel(surfX, surfY, spritePixel.r > 0.5f ? nokiaBack : nokiaFront);
                }
            }
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

    private void DrawWallColumn(int x, float angle, RaycastHit2D hit) {
        float baseDistance = hit.distance;
        float distance = baseDistance;
        if (lensCorrection) {
            distance = Mathf.Cos(angle * Mathf.Deg2Rad) * baseDistance;
        }

        //float closenessFactor = 1f - Mathf.Max(Mathf.Min((distance / maxDistanceFactor), maxDistanceFactor), 0f);
        float closenessFactor = Mathf.Min(1f / distance, 1f);
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
                if (closenessFactor > 0.9f) {
                    barColorBuffer[i] = nokiaFront;
                } else {
                    barColorBuffer[i] = i % skip == 0 ? nokiaBack : nokiaFront;
                }
            }
        }

        surface.SetPixels(x, y, 1, barSize, barColorBuffer);
    }
}
