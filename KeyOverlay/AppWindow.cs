using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace KeyOverlay
{
    public class AppWindow
    {
        readonly RenderWindow _window;
        readonly List<Key> _keyList = [];
        readonly List<RectangleShape> _squareList;
        readonly float _barSpeed;
        readonly float _ratioY;
        readonly int _outlineThickness;
        readonly Color _backgroundColor;
        readonly Color _keyBackgroundColor;
        readonly Color _barColor;
        readonly Color _fontColor;
        readonly Color _pressFontColor;
        readonly Sprite _background;
        readonly bool _fading;
        readonly bool _counter;
        readonly List<Drawable> _staticDrawables = [];
        readonly List<Text> _keyText = [];
        readonly uint _maxFps;
        readonly Clock _clock = new();
        readonly bool _upScroll;
        
        public AppWindow(string configFileName)
        {
            var config = ReadConfig(configFileName);
            var windowWidth = config["windowWidth"];
            var windowHeight = config["windowHeight"];
            _window = new(new(uint.Parse(windowWidth!), uint.Parse(windowHeight!)),
                "KeyOverlay", Styles.Titlebar | Styles.Close);

            // Calculate screen ratio relative to original program size for easy resizing
            //var ratioX = float.Parse(windowWidth) / 480f;
            _ratioY = float.Parse(windowHeight) / 960f;

            _barSpeed = float.Parse(config["barSpeed"], CultureInfo.InvariantCulture);
            _outlineThickness = int.Parse(config["outlineThickness"]);
            _backgroundColor = CreateItems.CreateColor(config["backgroundColor"]);
            _keyBackgroundColor = CreateItems.CreateColor(config["keyColor"]);
            _barColor = CreateItems.CreateColor(config["barColor"]);
            _maxFps = uint.Parse(config["maxFPS"]);
            _upScroll = config["upScroll"].ToLowerInvariant().Contains("y");

            // Get background image if in config
            if (config["backgroundImage"] != "")
                _background = new(new Texture(
                    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Resources",
                        config["backgroundImage"]))));

            // Create keys which will be used to create the squares and text
            var keyAmount = int.Parse(config["keyAmount"]);
            for (var i = 1; i <= keyAmount; i++)
                try
                {
                    var key = new Key(config[$"key" + i]);
                    if (config.ContainsKey($"displayKey" + i))
                        if (config[$"displayKey" + i].Length > 0)
                            key.KeyLetter = config[$"displayKey" + i];
                    _keyList.Add(key);
                }
                catch (InvalidOperationException e)
                {
                    // Invalid key
                    Console.WriteLine(e.Message);
                    using var sw = new StreamWriter("keyErrorMessage.txt");
                    sw.WriteLine(e.Message);
                }

            // Create squares and add them to _staticDrawables list
            var outlineColor = CreateItems.CreateColor(config["borderColor"]);
            var keySize = int.Parse(config["keySize"]);
            var margin = int.Parse(config["margin"]);
            _squareList = CreateItems.CreateKeys(keyAmount, _outlineThickness, keySize, _ratioY, margin,
                _window, _keyBackgroundColor, outlineColor);
            foreach (var square in _squareList)
            {
                if (_upScroll)
                    square.Position = square.Position with { Y = 100f };
                _staticDrawables.Add(square);
            }

            // Create text and add it ti _staticDrawables list
            _fontColor = CreateItems.CreateColor(config["fontColor"]);
            _pressFontColor = CreateItems.CreateColor(config["pressFontColor"]);
            for (var i = 0; i < keyAmount; i++)
            {
                var text = CreateItems.CreateText(_keyList[i].KeyLetter, _squareList[i], _fontColor, false);
                _keyText.Add(text);
                _staticDrawables.Add(text);
            }
            
            _fading = config["fading"].ToLowerInvariant().Contains("y");
            _counter = config["keyCounter"].ToLowerInvariant().Contains("y");
        }

        private Dictionary<string, string> ReadConfig(string configFileName)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var objectDict = new Dictionary<string, string>();
            var file = configFileName == null ? 
                File.ReadLines(Path.Combine(assemblyPath ?? "", "config.txt")).ToArray() :
                File.ReadLines(Path.Combine(assemblyPath ?? "", configFileName)).ToArray();
            foreach (var s in file.Select(x => x.Split('=')))
                objectDict.Add(s[0], s[1]);
            return objectDict;
        }

        private void OnClose(object sender, EventArgs e)
        {
            _window.Close();
        }

        public void Run()
        {
            _window.Closed += OnClose;
            _window.SetFramerateLimit(_maxFps);

            // Creating a sprite for the fading effect
            var fadingTexture = new RenderTexture(_window.Size.X, (uint)(255f * _ratioY) * 2);
            fadingTexture.Clear(Color.Transparent);
            if (_fading)
            {
                var sprites = Fading.GetBackgroundColorFadingTexture(_backgroundColor, _window.Size.X, _ratioY, _upScroll);
                foreach (var sprite in sprites)
                    fadingTexture.Draw(sprite);
                foreach (var sprite in sprites)
                    sprite.Dispose();
            }
            fadingTexture.Display();
            var fadingSprite = new Sprite(fadingTexture.Texture);
            if (_upScroll)
                fadingSprite.Position = new(0, _window.Size.Y - fadingTexture.Size.Y);
            //fadingTexture.Dispose();

            while (_window.IsOpen)
            {
                _window.Clear(_backgroundColor);
                _window.DispatchEvents();
                // If no keys are being held fill the square with bg color
                foreach (var square in _squareList)
                    square.FillColor = _keyBackgroundColor;
                // If a key is being held, change the key bg and increment hold variable of key
                foreach (var key in _keyList)
                {
                    var index = _keyList.IndexOf(key);
                    if (key.IsKey && Keyboard.IsKeyPressed(key.KeyboardKey) ||
                        !key.IsKey && Mouse.IsButtonPressed(key.MouseButton))
                    {
                        key.Hold++;
                        if(_keyText[index].FillColor != _pressFontColor)
                            _keyText[index].FillColor = _pressFontColor;
                        _squareList[index].FillColor = _barColor;
                    }
                    else
                    {
                        if (_keyText[index].FillColor != _fontColor)
                            _keyText[index].FillColor = _fontColor;
                        key.Hold = 0;
                    }
                }

                MoveBars(_keyList, _squareList);

                if (_background != null)
                    _window.Draw(_background);
                foreach (var staticDrawable in _staticDrawables)
                    _window.Draw(staticDrawable);

                foreach (var key in _keyList)
                {
                    if (_counter)
                    {
                        var sqr = _squareList[_keyList.IndexOf(key)];
                        key.CounterText.FillColor = _fontColor;
                        key.CounterText.CharacterSize = (uint)(50 * sqr.Size.X / 140);
                        key.CounterText.Origin = new(key.CounterText.GetLocalBounds().Width / 2f, sqr.Size.X / 140f);
                        var sqrBounds = sqr.GetGlobalBounds();
                        var x = sqrBounds.Left + sqr.OutlineThickness + sqr.Size.X / 2f;
                        key.CounterText.Position = new(x, _upScroll ? sqrBounds.Top - sqr.OutlineThickness - 24 : sqrBounds.Top + sqr.OutlineThickness + sqr.Size.Y + 6); 
                        
                        _window.Draw(key.CounterText);
                    }

                    foreach (var bar in key.BarList)
                        _window.Draw(bar);
                }

                _window.Draw(fadingSprite);
                _window.Display();
            }
        }

        /// <summary>
        /// If a key is a new input create a new bar, if it is being held stretch it and move all bars up
        /// </summary>
        private void MoveBars(List<Key> keyList, List<RectangleShape> squareList)
        {
            var moveDist = _clock.Restart().AsSeconds() * _barSpeed;
            foreach (var key in keyList)
            {
                switch (key.Hold)
                {
                    case 1:
                        {
                            var sqr = squareList[keyList.IndexOf(key)];
                            var rect = CreateItems.CreateBar(sqr, _outlineThickness, moveDist);
                            key.BarList.Add(rect);
                            key.Counter++;
                            key.CounterText.DisplayedString = key.Counter.ToString();
                            if (_upScroll)
                                rect.Position += new Vector2f(0, sqr.Size.Y + _outlineThickness + 2);
                        }
                        break;
                    case > 1:
                        {
                            var rect = key.BarList.Last();
                            rect.Size += new Vector2f(0, moveDist);
                            if (_upScroll)
                                rect.Position -= new Vector2f(0, moveDist);
                        }
                        break;
                }

                foreach (var rect in key.BarList)
                    rect.Position += new Vector2f(0, _upScroll ? moveDist : -moveDist);
                if (key.BarList.Count <= 0)
                    continue;
                var k = _upScroll ? key.BarList.Last() : key.BarList.First();
                if (!(_upScroll ? k.Position.Y > _window.Size.Y : k.Position.Y + k.Size.Y < 0))
                    continue;
                key.BarList.Remove(k);
                k.Dispose();
            }
        }
    }
}