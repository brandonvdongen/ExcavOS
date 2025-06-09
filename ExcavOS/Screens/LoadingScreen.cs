using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class LoadingScreen : ScreenHandler<ExcavOSContext>
        {
            public new const string SCREEN_NAME = "LoadingScreen";
            private readonly double loadingStart;
            private readonly double loadingTime;
            private readonly int quotesPerLoading;
            private int currentQuote = 0;
            private string quote;

            public LoadingScreen(ExcavOSContext context) : base(context)
            {
                loadingStart = _screenContext.TimeAccumulator.TotalSeconds;
                loadingTime = 1.0 + _screenContext.Randomizer.Next(1000, 2000) / 1000.0;
                quotesPerLoading = _screenContext.Randomizer.Next(2, 8);
            }

            private string GetInitializationSimsLikeText(double progress)
            {
                int quoteNumber = (int)(progress / (1.0 / quotesPerLoading));
                if (quoteNumber == 0)
                {
                    return "Booting";
                }

                string[] quotes = {
                    "Praying to Clang",
                    "Couting subgrids",
                    "Something something",
                    "Calming Clang Wrath",
                    "Counting stones in cargo",
                    "Doing important stuff",
                    "Formatting drive",
                    "Generating phantom forces",
                    "Connecting dots"
                };
                if (currentQuote != quoteNumber)
                {
                    int quoteIndex = _screenContext.Randomizer.Next(0, quotes.Length);
                    quote = quotes[quoteIndex];
                }

                currentQuote = quoteNumber;
                return quote;
            }

            public override void Draw(IMyTextSurface surface)
            {
                using (var frame = surface.DrawFrame())
                {
                    Painter.SetCurrentSurfaceAndFrame(surface, frame);
                    float value = (float)((_screenContext.TimeAccumulator.TotalSeconds - loadingStart) / loadingTime);
                    float margin = 20.0f;
                    float bottomYPos = Painter.AvailableSize.Y - margin;
                    
                    Painter.Text(new Vector2(Painter.Center.X, margin), "ExcavOS");
                    Painter.SpriteCentered(Painter.Center, new Vector2(80, 80), "Textures\\FactionLogo\\Miners\\MinerIcon_3.dds");
                    Painter.Text(new Vector2(margin, bottomYPos - margin), GetInitializationSimsLikeText(value) + "...", 0.5f, TextAlignment.LEFT);
                    Painter.ProgressBar(new Vector2(margin, bottomYPos), new Vector2(Painter.AvailableSize.X - margin * 2, 10), value, 2.0f);
                }
            }

            public override bool ShouldDispose()
            {
                return _screenContext.TimeAccumulator.TotalSeconds - loadingStart > loadingTime;
            }
        }
    }
}
