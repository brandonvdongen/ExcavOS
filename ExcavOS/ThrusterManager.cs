using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program {

        public class ThrusterManager {

            public ThrustGroup up;
            public ThrustGroup down;
            public ThrustGroup left;
            public ThrustGroup right;
            public ThrustGroup forward;
            public ThrustGroup backward;

            public List<ThrustGroup> groups;

            private readonly ExcavOSContext _context;

            public ThrusterManager(ExcavOSContext context) {
                _context = context;

                up = new ThrustGroup(_context);
                down = new ThrustGroup(_context);
                left = new ThrustGroup(_context);
                right = new ThrustGroup(_context);
                forward = new ThrustGroup(_context);
                backward = new ThrustGroup(_context);

                groups = new List<ThrustGroup>();
                groups.Add(up);
                groups.Add(down);
                groups.Add(left);
                groups.Add(right);
                groups.Add(forward);
                groups.Add(backward);
            }

            public void Add(IMyThrust thruster, IMyShipController controller) {
                if (!thruster.IsFunctional) return;
                var dir = thruster.WorldMatrix.Backward;
                var forwardDot = Vector3D.Dot(dir, controller.WorldMatrix.Forward);
                var upDot = Vector3D.Dot(dir, controller.WorldMatrix.Up);
                var leftDot = Vector3D.Dot(dir, controller.WorldMatrix.Left);

                if (forwardDot >= 0.9) forward.thrusters.Add(thruster);
                else if (forwardDot <= -0.9) backward.thrusters.Add(thruster);
                else if (leftDot >= 0.9) left.thrusters.Add(thruster);
                else if (leftDot <= -0.9) right.thrusters.Add(thruster);
                else if (upDot >= 0.9) up.thrusters.Add(thruster);
                else if (upDot <= -0.9) down.thrusters.Add(thruster);
            }
            public void ClearAll() {
                up.thrusters.Clear();
                down.thrusters.Clear();
                left.thrusters.Clear();
                right.thrusters.Clear();
                forward.thrusters.Clear();
                backward.thrusters.Clear();
            }

            public void UpdateAll() {
                up.Update();
                down.Update();
                left.Update();
                right.Update();
                forward.Update();
                backward.Update();
            }

            public double AccelerationInDirection(Vector3D direction, Vector3D gravity, double mass) {
                double totalAccel = 0;
                groups.ForEach(group => {
                    if (Vector3D.Dot(group.direction, direction) < -0.01) {
                        totalAccel += group.AccelerationInDirection(direction, gravity, mass);
                    }
                });
                return totalAccel;
            }
        }

        public class ThrustGroup {

            public List<IMyThrust> thrusters = new List<IMyThrust>();
            public double maxThrust;
            public bool allWorking = false;
            public Vector3D direction;
            private readonly ExcavOSContext _context;

            public ThrustGroup(ExcavOSContext context) {
                _context = context;
            }

            public void Update() {

                maxThrust = 0;
                allWorking = true;



                foreach (IMyThrust thrust in thrusters) {

                    if (_context.systemManager.ShipState == ShipState.isDocked || _context.systemManager.ShipState == ShipState.isStatic) {
                        thrust.Enabled = false;
                    }
                    else if (_context.systemManager.ShipState == ShipState.isControlled || _context.systemManager.ShipState == ShipState.isIdle) {
                        thrust.Enabled = thrust.MaxEffectiveThrust > 0;
                    }

                    if (!thrust.IsWorking) {
                        allWorking = false;
                        continue;
                    }
                    maxThrust += thrust.MaxEffectiveThrust;
                }
                if (thrusters.Count <= 0) return;
                direction = thrusters[0].WorldMatrix.Backward;
            }
            public double AccelerationInDirection(Vector3D direction, Vector3D gravity, double mass) {

                if (!Vector3D.IsUnit(ref direction)) {
                    direction = Vector3D.Normalize(direction);
                }

                double effectiveThrust = 0;
                foreach (IMyThrust thrust in thrusters) {
                    effectiveThrust += Vector3D.Dot(direction, thrust.WorldMatrix.Backward) * thrust.MaxEffectiveThrust;
                }
                double effectiveAcceleration = effectiveThrust / mass;

                // Now add the effects of gravity
                return effectiveAcceleration + Vector3D.Dot(gravity, direction);
            }
        }
    }
}
