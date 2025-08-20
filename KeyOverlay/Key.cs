using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Window;

namespace KeyOverlay
{
    public class Key
    {
        public int Hold { get; set; }
        public List<RectangleShape> BarList = new();
        public string KeyLetter;
        public readonly Keyboard.Key KeyboardKey;
        public readonly Mouse.Button MouseButton;
        public int Counter = 0;
        public readonly bool IsKey = true;
        public readonly Text CounterText;

        public Key(string key)
        {
            CounterText = CreateItems.CreateText("0", new(), Color.White, true);
            KeyLetter = key;
            if (!Enum.TryParse(key, true, out KeyboardKey))
            {
                if (KeyLetter[0] == 'm')
                    KeyLetter = KeyLetter.Remove(0, 1);
                if (key != null && Enum.TryParse(key[1..], true, out MouseButton))
                //if(!Enum.TryParse(key, out MouseButton))
                {
                    //KeyLetter = key.Substring(1);
                    IsKey = false;
                }
                else
                    throw new InvalidOperationException($"Invalid key {key}");

            }
        }
    }
}