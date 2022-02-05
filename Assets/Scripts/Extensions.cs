using UnityEngine;

public static class Extensions {
    public static void Clear(this Texture2D texture, Color color) {
        Color[] colors = new Color[texture.width * texture.height];
        for (int i = 0; i < colors.Length; i++) {
            colors[i] = color;
        }

        texture.SetPixels(0, 0, texture.width, texture.height, colors);
    }

    public static Vector2 Rotate(this Vector2 vector, float degrees) {
        return Quaternion.Euler(0, 0, degrees) * vector;
    }
}
