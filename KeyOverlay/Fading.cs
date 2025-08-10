using System.Collections.Generic;
using SFML.Graphics;

namespace KeyOverlay {
    public static class Fading
    {
        public static List<Sprite> GetBackgroundColorFadingTexture(Color backgroundColor, uint windowWidth, float ratioY) {
            var sprites = new List<Sprite>();
            var alpha = 255;
            var color = backgroundColor;
            for (var i = 0; i < 255; i++)
            {
                Image img = ratioY >= 0.5f ? new(windowWidth, (uint)(2 * ratioY), color) : new Image(windowWidth, 1, color);
                var sprite = new Sprite(new Texture(img));
                sprite.Position = new(0, img.Size.Y * i);
                sprites.Add(sprite);
                alpha -= 1;
                color.A = (byte)alpha;
            }

            return sprites;
        }
    }
}
