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

        public class WeightScreen : ScreenHandler<ExcavOSContext> {
            public new const string SCREEN_NAME = "Weight";
            private readonly ExcavOSContext _context;
            public WeightScreen(ExcavOSContext context) : base(context) {
                _context = context;
            }

            public override void Draw(IMyTextSurface surface) {

                using (var frame = surface.DrawFrame()) {
                    Painter.SetCurrentSurfaceAndFrame(surface, frame);

                    bool roverMode = _context.thrusterManager.down.thrusters.Count == 0;
                    float liftUsage = _context.weightAnalizer.LiftThrustUsage;
                    float cargoUsage = (float)(_context.cargoManager.CurrentCapacity / _context.cargoManager.TotalCapacity);
                    float margin = 20.0f;
                    float max = Math.Min(Painter.AvailableSize.X, Painter.AvailableSize.Y);
                    bool shortMode = max < Painter.AvailableSize.X;

                    if (roverMode) {
                        Vector2 position = new Vector2((Painter.AvailableSize.X - max) / 2.0f + margin, margin);
                        Vector2 size = new Vector2(max - margin * 2, max / 2.0f - margin);
                        string subText = shortMode ? "Cargo" : "Cargo capacity";
                        if (_context.weightAnalizer.CapacityDelta > 0.0001) {
                            float timeLeft = (float)((1.0f - cargoUsage) / _context.weightAnalizer.CapacityDelta);
                            subText = string.Format("+{0:0.00}% @ {1:0.0}s", _context.weightAnalizer.CapacityDelta * 100, timeLeft);
                        }
                        Painter.FullRadial(position, size, cargoUsage, subText, 60);

                    }
                    else {
                        Vector2 position = new Vector2((Painter.AvailableSize.X - max) / 2.0f + margin, margin / 2.0f);
                        Vector2 size = new Vector2(max - margin * 2, max / 2.0f - margin);
                        Painter.Radial(position, size, liftUsage, shortMode ? "Lift thrust" : "Lift thrust usage", 30);
                        position.Y += Painter.AvailableSize.Y / 2.0f;
                        Painter.FilledRectangleEx(new Vector2(position.X, position.Y - 1.0f - margin / 2.0f), new Vector2(max - 2 * margin, 2.0f), Painter.SecondaryColor);
                        string subText = shortMode ? "Cargo" : "Cargo capacity";
                        if (_context.weightAnalizer.CapacityDelta > 0.0001) {
                            float timeLeft = (float)((1.0f - cargoUsage) / _context.weightAnalizer.CapacityDelta);
                            subText = string.Format("+{0:0.00}% @ {1:0.0}s", _context.weightAnalizer.CapacityDelta * 100, timeLeft);
                        }
                        Painter.Radial(position, size, cargoUsage, subText, 30, true);
                        if (liftUsage > _context.weightAnalizer.GetLiftThresholdWarning()) {
                            position.Y -= Painter.AvailableSize.Y / 2.0f;
                            Vector2 spriteSize = new Vector2(64, 64);
                            Vector2 spritePos = new Vector2((Painter.AvailableSize.X - spriteSize.X) / 2.0f, margin / 2.0f + size.Y - spriteSize.Y);
                            if (_context.tick % 2 == 0) {
                                Painter.Sprite(spritePos, spriteSize, "Danger");
                            }
                        }
                    }
                }
            }
        }
    }
}
