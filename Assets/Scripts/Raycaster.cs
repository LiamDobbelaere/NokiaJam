using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ViewMode {
    WORLD,
    MAP,
    OPTIONS
};

public class Option {
    public string label;
    public Func<string> sublabel;
    public Action execute;
}

public class Raycaster : MonoBehaviour {
    public Texture2D font;
    public Texture2D slingshotIdle;
    public Texture2D slingshotFire;

    private Dictionary<char, Color[]> fontMap;

    private Image image;

    private Texture2D surface;
    private float fov = 80f;
    private int surfaceWidth = 84;
    private int surfaceHeight = 48;
    private const int fontCharacterWidth = 5;
    private const int fontCharacterHeight = 6;
    private bool forceGrayscale = false;
    private const bool lensCorrection = true;
    private float fovIncrement;
    private const float maxDistanceFactor = 12f;
    private const float maxSpriteDistanceFactor = 16f;

    private Color[] barColorBuffer;
    private float[] barZBuffer;
    private GameObject player;
    private PlayerController playerController;

    private Color clearColor;
    private Color nokiaBackColor = new Color(199 / 256f, 240 / 256f, 216 / 256f);
    private Color nokiaFrontColor = new Color(67 / 256f, 82 / 256f, 61 / 256f);
    private Color nokiaBack = new Color(199 / 256f, 240 / 256f, 216 / 256f);
    private Color nokiaFront = new Color(67 / 256f, 82 / 256f, 61 / 256f);

    private float artificialFramerateValue = 1 / 15f;
    private float artificialFramerate;
    private int framerateMode = 0;
    private bool useArtificialFramerate = !Application.isEditor;

    private ViewMode currentViewMode = ViewMode.WORLD;

    private Option[] options;
    private int currentOptionIndex = 0;

    // Start is called before the first frame update
    void Start() {
        options = new Option[] {
            new Option {
                label = "colors",
                sublabel = () => forceGrayscale ? "grayscl. test" : "nokia colors",
                execute = () => {
                    forceGrayscale = !forceGrayscale;
                    if (forceGrayscale) {
                        nokiaBack = Color.black;
                        nokiaFront = Color.white;
                        clearColor = nokiaBack;
                    } else {
                        nokiaBack = nokiaBackColor;
                        nokiaFront = nokiaFrontColor;
                        clearColor = nokiaBack;
                    }
                }
            },
            new Option {
                label = "fov",
                sublabel = () => fov == 80f ? "default" : fov.ToString(),
                execute = () => {
                    fov += 10;
                    if (fov > 120) {
                        fov = 30;
                    }
                    fovIncrement = fov / surfaceWidth;
                }
            },
            new Option {
                label = "hires test",
                sublabel = () => surfaceWidth == 84 ? "no" : "yes",
                execute = () => {
                    if (surfaceWidth == 84) {
                        surfaceWidth = 640;
                        surfaceHeight = 480;
                    } else {
                        surfaceWidth = 84;
                        surfaceHeight = 48;
                    }

                    Start();
                }
            },
            new Option {
                label = "frames/s",
                sublabel = () => useArtificialFramerate ? Mathf.RoundToInt(1 / artificialFramerateValue).ToString() : "uncapped",
                execute = () => {
                    framerateMode++;
                    if (framerateMode > 2) {
                        framerateMode = 0;
                    }

                    switch (framerateMode) {
                        case 0:
                            useArtificialFramerate = true;
                            artificialFramerate = 0f;
                            artificialFramerateValue = 1/15f;
                            break;
                        case 1:
                            useArtificialFramerate = true;
                            artificialFramerate = 0f;
                            artificialFramerateValue = 1/60f;
                            break;
                        case 2:
                            useArtificialFramerate = false;
                            break;
                    }
                }
            }
        };
        BuildFontMap();

        SetupSurface();
        SetupSprite();

        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerController>();

        clearColor = nokiaBack;

        barZBuffer = new float[surfaceWidth];

        barColorBuffer = new Color[surfaceWidth * surfaceHeight];
        barColorBuffer.Fill(clearColor);

        fovIncrement = fov / surfaceWidth;
        artificialFramerate = artificialFramerateValue;
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
        ProcessInput();

        if (useArtificialFramerate) {
            artificialFramerate -= Time.deltaTime;
            if (artificialFramerate < 0f) {
                artificialFramerate = artificialFramerateValue;
            } else {
                return;
            }
        }

        surface.Clear(clearColor);

        switch (currentViewMode) {
            case ViewMode.WORLD:
                RenderWorld();
                break;
            case ViewMode.OPTIONS:
                RenderOptions();
                break;
        }

        surface.Apply();
    }

    private void DrawCeiling() {
        Color[] floorColorBuffer = new Color[surface.width * surface.height / 2];
        floorColorBuffer.Fill(nokiaFront);

        if (forceGrayscale) {
            surface.SetPixels(0, surfaceHeight / 2, surface.width, surface.height / 2, floorColorBuffer);
        } else {
            surface.SetPixels(0, 0, surface.width, surface.height / 2, floorColorBuffer);
        }
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

        barZBuffer[x] = distance;

        WorldRaycastAttributes worldRaycastAttributes = hit.collider.gameObject.GetComponent<WorldRaycastAttributes>();

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
                if (closenessFactor > 0.9f && !(worldRaycastAttributes != null && worldRaycastAttributes.renderStyle == "tex")) {
                    barColorBuffer[i] = nokiaFront;
                } else {
                    bool shouldDraw = i % skip == 0;

                    if (worldRaycastAttributes != null && worldRaycastAttributes.renderStyle != "h") {
                        switch (worldRaycastAttributes.renderStyle) {
                            case "v":
                                shouldDraw = x % skip == 0;
                                break;
                            case "hv":
                                shouldDraw = i % skip == 0 || x % skip == 0;
                                break;
                            case "tex":
                                Texture2D tex = worldRaycastAttributes.texture.texture;
                                Color texPixel = tex.GetPixel(Mathf.RoundToInt(Mathf.Abs(hit.point.magnitude) * 100f % tex.width),
                                    Mathf.RoundToInt((i / (float)barSize) * tex.height * (surfaceWidth / (float)surfaceHeight) % tex.height));

                                shouldDraw = texPixel.r < 0.5f;
                                break;
                        }

                        barColorBuffer[i] = shouldDraw ? nokiaBack : nokiaFront;
                    } else {
                        barColorBuffer[i] = shouldDraw ? nokiaBack : nokiaFront;
                    }
                }
            }
        }

        surface.SetPixels(x, y, 1, barSize, barColorBuffer);
    }

    private void RenderWorld() {
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
            } else {
                barZBuffer[x] = float.MaxValue;
            }

            RaycastHit2D[] spritesHit = Physics2D.RaycastAll(
                player.transform.position, rayVector, maxSpriteDistanceFactor,
                    LayerMask.GetMask(new string[] { "SpritesNoCollision", "Sprites" }
                )
            );

            foreach (RaycastHit2D spriteHit in spritesHit) {
                if (spriteHit.collider != null && !spriteColliders.Contains(spriteHit.collider)) {

                    SpriteRaycastAttributes src = spriteHit.collider.gameObject.GetComponent<SpriteRaycastAttributes>();
                    if (src != null && src.noZTest) {
                        Vector2 rayFromPlayerToSprite = (spriteHit.transform.position - player.transform.position).normalized;
                        RaycastHit2D canPlayerSeeSprite = Physics2D.Raycast(
                            player.transform.position,
                            rayFromPlayerToSprite,
                            maxSpriteDistanceFactor,
                            LayerMask.GetMask(new string[] { "Sprites", "SpritesNoCollision", "World" })
                        );
                        Debug.DrawRay(
                           (Vector2)player.transform.position,
                           rayFromPlayerToSprite,
                           Color.magenta,
                           artificialFramerateValue
                        );

                        if (canPlayerSeeSprite.collider.gameObject.layer == LayerMask.NameToLayer("World")) {
                            continue;
                        }
                    }

                    spriteColliders.Add(spriteHit.collider);
                }
            }

            x++;
        }

        spriteColliders.Reverse();
        foreach (Collider2D spriteCollider in spriteColliders) {
            Vector2 vectorToSprite = spriteCollider.transform.position - player.transform.position;
            float angleToSprite = Vector2.SignedAngle(vectorToSprite, player.transform.up);
            if (angleToSprite < fov * -0.5f || angleToSprite > fov * 0.5f) {
                continue;
            }

            SpriteRenderer spriteRenderer = spriteCollider.gameObject.GetComponent<SpriteRenderer>();
            SpriteRaycastAttributes spriteAttributes = spriteCollider.gameObject.GetComponent<SpriteRaycastAttributes>();
            Texture2D spriteTextureOriginal = spriteRenderer.sprite.texture;

            if (spriteAttributes.quadAngleSprites.Length == 4) {
                float rawIndex = Mathf.Round(Vector2.SignedAngle(player.transform.right, spriteCollider.transform.right) / 90f);
                int actualIndex = ((int)rawIndex + 2) % 4;

                spriteTextureOriginal = spriteAttributes.quadAngleSprites[actualIndex].texture;
            }

            float baseDistance = Vector2.Distance(spriteCollider.transform.position, player.transform.position);
            float distance = baseDistance;
            if (lensCorrection) {
                distance = Mathf.Cos(angleToSprite * Mathf.Deg2Rad) * baseDistance;
            }

            float closenessFactor = 1f / distance; //2f - Mathf.Max(Mathf.Min((distance / 3f), 3f), 0f);

            int targetWidth = Mathf.RoundToInt(spriteTextureOriginal.width * closenessFactor);
            int targetHeight = Mathf.RoundToInt(spriteTextureOriginal.height * closenessFactor);
            if (targetWidth < 1 || targetHeight < 1) {
                continue;
            }

            float fovAlignmentFactor = angleToSprite / (fov * 0.5f);

            Texture2D spriteTexture = spriteTextureOriginal.ResizeNN(
                Mathf.Min(100, Mathf.RoundToInt(targetWidth * spriteAttributes.scale)),
                Mathf.Min(100, Mathf.RoundToInt(targetHeight * spriteAttributes.scale))
            );

            float screenXPosition = fovAlignmentFactor * (surface.width * 0.5f);

            int drawX = Mathf.RoundToInt(surface.width * 0.5f + screenXPosition - spriteTexture.width * 0.5f);
            int drawY = Mathf.RoundToInt(
                surface.height * 0.5f - spriteTexture.height * 0.5f
                - (spriteAttributes.zOffset * closenessFactor));

            int spritePixelX = -1;
            int spritePixelY = -1;
            for (int surfY = drawY; surfY < drawY + spriteTexture.height; surfY++) {
                spritePixelY++;

                for (int surfX = drawX; surfX < drawX + spriteTexture.width; surfX++) {
                    spritePixelX++;

                    if (surfX < 0 || surfX >= surface.width || surfY < 0 || surfY >= surface.height) {
                        continue;
                    }

                    if (distance > barZBuffer[surfX] && !spriteAttributes.noZTest) {
                        continue;
                    }

                    Color spritePixel = spriteTexture.GetPixel(spritePixelX, spriteTexture.height - 1 - spritePixelY);
                    if (spritePixel.a < 0.5f) {
                        continue;
                    }

                    surface.SetPixel(surfX, surfY, spritePixel.r > 0.5f ? nokiaBack : nokiaFront);
                }
            }
        }

        if (playerController.IsFireCooldownActive()) {
            DrawOverlay(slingshotFire);
        } else {
            DrawOverlay(slingshotIdle);
        }

        Debug.DrawLine(player.transform.position, player.transform.position + player.transform.up * 50f, Color.yellow);
    }

    private void BuildFontMap() {
        this.fontMap = new Dictionary<char, Color[]>();
        string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,;:?!-_~#\"'&()[]{}^|`/\\@�+=*%�$��<>��";

        int i = 0;
        foreach (char character in charSet) {
            fontMap[character] = font.GetPixels(fontCharacterWidth * i, 0, fontCharacterWidth, fontCharacterHeight);
            i++;
        }
    }

    private void DrawCharacter(char character, int x, int y) {
        if (!fontMap.ContainsKey(character)) {
            return;
        }

        for (int cy = 0; cy < fontCharacterHeight; cy++) {
            for (int cx = 0; cx < fontCharacterWidth; cx++) {
                Color col = fontMap[character][cy * fontCharacterWidth + cx];
                if (col.a < 0.5f) {
                    continue;
                }

                surface.SetPixel(x + cx, y + fontCharacterHeight - cy, nokiaFront);
            }
        }
    }

    private void DrawText(string text, int x, int y) {
        for (int i = 0; i < text.Length; i++) {
            char character = text[i];

            DrawCharacter(character, x + i * (fontCharacterWidth + 1), y);
        }
    }

    private void RenderOptions() {
        Option currentOption = options[currentOptionIndex];

        DrawText("Options", 2, 2);
        DrawText(currentOption.label + " ->", 2, 2 + fontCharacterHeight * 2);
        DrawText(currentOption.sublabel(), 2, 2 + fontCharacterHeight * 3);
    }

    private void ProcessInput() {
        if (currentViewMode == ViewMode.OPTIONS) {
            if (Input.GetButtonDown("PrimaryAction")) {
                options[currentOptionIndex].execute();
            }
            if (Input.GetButtonDown("Horizontal")) {
                currentOptionIndex++;
                if (currentOptionIndex >= options.Length) {
                    currentOptionIndex = 0;
                }
            }
        }

        if (Input.GetButtonDown("Options")) {
            currentViewMode = currentViewMode == ViewMode.OPTIONS ? ViewMode.WORLD : ViewMode.OPTIONS;
        }
    }

    private void DrawOverlay(Texture2D overlay) {
        for (int cy = 0; cy < overlay.height; cy++) {
            for (int cx = 0; cx < overlay.width; cx++) {
                Color col = overlay.GetPixel(cx, cy);
                if (col.a < 0.5f) {
                    continue;
                }

                surface.SetPixel(cx, surface.height - 1 - cy, col.r < 0.5f ? nokiaFront : nokiaBack);
            }
        }
    }
}
