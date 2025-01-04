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

namespace IngameScript {
    partial class Program {

        public class UtilityScreen : ScreenHandler<ExcavOSContext> {
            public new const string SCREEN_NAME = "Utility";

            private readonly StringBuilder sb = new StringBuilder();

            public UtilityScreen(ExcavOSContext context) : base(context) {
            }

            public override void Draw(IMyTextSurface surface) {
                using (var frame = surface.DrawFrame()) {
                    Painter.SetCurrentSurfaceAndFrame(surface, frame);
                    float margin = Painter.Width >= 512.0f ? 25.0f : 5.0f;
                    float gap = Painter.Width >= 512.0f ? 10.0f : 2.0f;
                    float fontSize = Painter.Width >= 512.0f ? 1.0f : 0.8f;
                    sb.Clear();
                    sb.Append("Xy");
                    Vector2 textSize = surface.MeasureStringInPixels(sb, surface.Font, fontSize);
                    Vector2 position = new Vector2(margin, margin);
                    Vector2 barSize = new Vector2(Painter.Width - margin * 2, Painter.Width >= 512.0f ? 2.0f : 1.0f);

                    Painter.Text(position, (_screenContext.systemManager.ActiveController.GetNaturalGravity().Length() == 0 ? "A-Grav Align" : "P-Grav Align"), fontSize, TextAlignment.LEFT);
                    if (_screenContext.utilitymanager.Status == "") {
                        float pitch = _screenContext.utilitymanager.GravityAlignPitch;
                        string status;
                        if (pitch == 0) status = "[Level]";
                        else if (pitch == 90) status = "[Up]";
                        else if (pitch == -90) status = "[Down]";
                        else status = string.Format("({0}°)", pitch);
                        Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), (_screenContext.utilitymanager.GravityAlign ? Painter.PrimaryColor : Painter.SecondaryColor), string.Format("{0} {1}", (_screenContext.utilitymanager.GravityAlign ? "On" : "Off"), status), fontSize, TextAlignment.RIGHT);
                    }
                    else {
                        Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, _screenContext.utilitymanager.Status, fontSize, TextAlignment.RIGHT);
                    }

                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;

                    Painter.Text(position, "Cruise Control", fontSize, TextAlignment.LEFT);
                    if (_screenContext.utilitymanager.CruiseEnabled) {
                        Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.PrimaryColor, string.Format("On [{0} m/s]", _screenContext.utilitymanager.CruiseTarget), fontSize, TextAlignment.RIGHT);
                    }
                    else {
                        Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, string.Format("Off [{0} m/s]", _screenContext.utilitymanager.CruiseTarget), fontSize, TextAlignment.RIGHT);
                    }


                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;

                    Painter.Text(position, "Stop", fontSize, TextAlignment.LEFT);
                    if (_screenContext.weightAnalizer.Status == "") {
                        if (_screenContext.weightAnalizer.StoppingDistance > 0) {
                            string w = _screenContext.weightAnalizer.StopThrustersWarning ? " (!)" : "";
                            string s = string.Format("{0:0.00}m @ {1:0.00}s{2}", _screenContext.weightAnalizer.StoppingDistance, _screenContext.weightAnalizer.StoppingTime, w);
                            Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, s, fontSize, TextAlignment.RIGHT);
                        }
                        else {
                            Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, "-", fontSize, TextAlignment.RIGHT);
                        }
                    }
                    else {
                        Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, _screenContext.weightAnalizer.Status, fontSize, TextAlignment.RIGHT);
                    }

                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;

                    Painter.Text(position, "Jettison", fontSize, TextAlignment.LEFT);
                    Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, _screenContext.utilitymanager.GetSortersFilter(), fontSize, TextAlignment.RIGHT);

                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;


                    float maxWidth = (barSize.X - (4 * gap)) / 3;
                    //float maxHeight = Painter.Height - 2 * margin - position.Y;
                    float maxHeight = textSize.Y + gap;
                    position.X += gap;
                    Painter.ProgressBarWithIconAndText(position, new Vector2(maxWidth, maxHeight), (float)_screenContext.utilitymanager.BatteryLevel, 1.0f, "IconEnergy", _screenContext.utilitymanager.BatteryCharge);
                    position.X += maxWidth + gap;

                    Painter.ProgressBarWithIconAndText(position, new Vector2(maxWidth, maxHeight), (float)_screenContext.utilitymanager.HydrogenLevel, 1.0f, "IconHydrogen", _screenContext.utilitymanager.HydrogenCharge);
                    position.X += maxWidth + gap;

                    Painter.ProgressBar(position, new Vector2(maxWidth, maxHeight), (float)_screenContext.utilitymanager.UraniumLevel * 1000, 1.0f, "MyObjectBuilder_Ingot/Uranium");

                    /*
                    Painter.Text(position, "Batteries", fontSize, TextAlignment.LEFT);
                    Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, _context._utilitymanager.BatteryCharge, fontSize, TextAlignment.RIGHT);

                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;

                    Painter.Text(position, "Hydrogen", fontSize, TextAlignment.LEFT);
                    Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, _context._utilitymanager.HydrogenLevel, fontSize, TextAlignment.RIGHT);

                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;

                    Painter.Text(position, "Uranium", fontSize, TextAlignment.LEFT);
                    Painter.TextEx(new Vector2(Painter.Width - margin, position.Y), Painter.SecondaryColor, _context._utilitymanager.HydrogenLevel, fontSize, TextAlignment.RIGHT);

                    position.Y += textSize.Y + gap;
                    Painter.FilledRectangleEx(position, barSize, Painter.SecondaryColor);
                    position.Y += gap;
                    */
                }
            }
        }
    }
}
