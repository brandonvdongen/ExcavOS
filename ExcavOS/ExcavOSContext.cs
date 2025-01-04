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
        public class ExcavOSContext {
            public ExcavOSContext _context;
            public Program program;
            public Config config;
            public MyIni storage;

            public TimeSpan TimeAccumulator;
            public Random Randomizer = new Random();
            public CargoManager cargoManager;
            public WeightAnalizer weightAnalizer;
            public UtilityManager utilitymanager;
            public SystemManager systemManager;
            public ThrusterManager thrusterManager;
            public int tick;

            public ExcavOSContext(Program program, Config config, MyIni storage) {
                _context = this;
                this.program = program;
                this.config = config;
                this.storage = storage;
                cargoManager = new CargoManager(_context);
                systemManager = new SystemManager(_context);
                thrusterManager = new ThrusterManager(_context);
                weightAnalizer = new WeightAnalizer(_context);
                utilitymanager = new UtilityManager(_context);
            }

            public void Save() {
                utilitymanager.Save();
            }

            public void Update(TimeSpan time) {
                TimeAccumulator = time;
                cargoManager.QueryData();
                weightAnalizer.QueryData(time);
                utilitymanager.Update();
                systemManager.Update();
                tick++;
            }

            public void HandleCommand(string argument) {
                string[] args = argument.Split(' ');
                switch (args[0].ToLower()) {
                    case "toggle_gaa":
                        if (args[1].ToLower() == "on") utilitymanager.GravityAlign = true;
                        if (args[1].ToLower() == "off") utilitymanager.GravityAlign = false;
                        else utilitymanager.GravityAlign = !utilitymanager.GravityAlign;
                        break;
                    case "set_gaa_pitch":
                        char modifier = args[1][0];
                        float pitch = float.Parse(args[1]);
                        if (modifier.ToString() == "+" || modifier.ToString() == "-") {
                            utilitymanager.GravityAlignPitch += pitch;
                        }
                        else if (!float.IsNaN(pitch)) {
                            utilitymanager.GravityAlignPitch = pitch;
                        }
                        utilitymanager.GravityAlignPitch = MathHelper.Clamp(utilitymanager.GravityAlignPitch, -90, 90);
                        break;
                    case "toggle_cruise":
                        if (args[1].ToLower() == "on") utilitymanager.CruiseEnabled = true;
                        if (args[1].ToLower() == "off") utilitymanager.CruiseEnabled = false;
                        else utilitymanager.CruiseEnabled = !utilitymanager.CruiseEnabled;
                        if (!utilitymanager.CruiseEnabled) {
                            thrusterManager.forward.thrusters.ForEach(thruster => { thruster.ThrustOverridePercentage = 0.0f; thruster.Enabled = true; });
                            thrusterManager.backward.thrusters.ForEach(thruster => { thruster.ThrustOverridePercentage = 0.0f; thruster.Enabled = true; });
                        }
                        break;
                    case "set_cruise":
                        modifier = args[1][0];
                        float speed = float.Parse(args[1]);
                        if (modifier.ToString() == "+" || modifier.ToString() == "-") {
                            utilitymanager.CruiseTarget += speed;
                        }
                        else if (!float.IsNaN(speed)) {
                            utilitymanager.CruiseTarget = speed;
                        }
                        break;
                    case "dump":
                        utilitymanager.SetSortersFilter(args[1]);
                        break;
                }
            }
        }
    }
}