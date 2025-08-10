using System.Collections.Generic;
using SFML.Graphics;
using System;

namespace KeyOverlay {
    public static class Fading
    {
        public static List<Sprite> GetBackgroundColorFadingTexture(Color backgroundColor, uint windowWidth, float ratioY, bool upScroll)
        {
            if (ratioY < 0.5f)
                return [];
            var sprites = new List<Sprite>();
            var color = backgroundColor;
            for (var i = 0; i < 255; i++)
            {
                var img = new Image(windowWidth, (uint)Math.Max(1, ratioY), color);
                var sprite = new Sprite(new Texture(img));
                sprite.Position = new(0, img.Size.Y * (upScroll ? (uint)(255f * ratioY) * 2 - i : i));
                sprites.Add(sprite);
                color.A = (byte)(255 - i);
            }

            return sprites;
        }
    }
}
