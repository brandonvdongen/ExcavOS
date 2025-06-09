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
using System.Runtime.InteropServices;

namespace IngameScript
{
    partial class Program
    {
        public class CargoScreen : ScreenHandler<ExcavOSContext>
        {
            public new const string SCREEN_NAME = "CargoOre";
            private readonly StringBuilder sb = new StringBuilder();

            public CargoScreen(ExcavOSContext context) : base(context)
            {
            }

            private string ExtractName(string itemType)
            {
                return itemType.Split('/').Last();
            }

            private string FormatWithSuffix(double amount)
            {
                if (amount >= 1000000)
                {
                    return string.Format("{0,10:0.00}Mt", amount / 1000000);
                } else if (amount >= 1000)
                {
                    return string.Format("{0,10:0.00}t", amount / 1000);
                }
                return string.Format("{0,10:0.00}kg", amount);
            }

            public override void Draw(IMyTextSurface surface)
            {
                using (var frame = surface.DrawFrame())
                {
                    Painter.SetCurrentSurfaceAndFrame(surface, frame);
                    float margin = Painter.Width >= 512.0f ? 25.0f : 5.0f;
                    float gap = Painter.Width >= 512.0f ? 10.0f : 2.0f;
                    float fontSize = Painter.Width >= 512.0f ? 1.0f : 0.8f;
                    Vector2 position = new Vector2(margin, margin);
                    Vector2 barSize = new Vector2(Painter.Width - margin * 2, Painter.Width >= 512.0f ? 2.0f : 1.0f);

                    if (!_screenContext.cargoManager.hasAnyOre)
                    {
                        Painter.SpriteCentered(Painter.Center, new Vector2(128f, 128f), "MyObjectBuilder_Ore/Stone", Painter.SecondaryColor);
                        Painter.Text(Painter.Center, "No ores");
                        return;
                    }

                    _screenContext.cargoManager.IterateCargoDescending((name, entry) => {
                        if (entry.typeid != "MyObjectBuilder_Ore")
                        {
                            return;
                        }
                        sb.Clear();
                        sb.Append(ExtractName(name));
                        Vector2 textSize = surface.MeasureStringInPixels(sb, surface.Font, fontSize);
                        Painter.Sprite(position, new Vector2(textSize.Y, textSize.Y), name);
                        Painter.Text(position + new Vector2(textSize.Y + gap, 0), sb.ToString(), fontSize, TextAlignment.LEFT);
                        Painter.Text(new Vector2(Painter.Width - margin, position.Y), FormatWithSuffix(entry.amount), fontSize, TextAlignment.RIGHT);
                        position.Y += textSize.Y + gap;
                        Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                        position.Y += gap;
                    });
                }
            }
        }
    }
}
