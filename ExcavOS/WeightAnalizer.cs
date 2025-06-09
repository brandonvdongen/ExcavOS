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
using static IngameScript.Program.ThrusterManager;

namespace IngameScript {
    internal partial class Program {
        public class WeightAnalizer {
            private readonly ExcavOSContext _context;
            public string Status;

            public float LiftThrustUsage;
            public float StoppingDistance;
            public float StoppingTime;
            public float CapacityDelta;
            public bool StopThrustersWarning = false;

            protected struct WeightPoint {
                public double time;
                public double capacity;
            }
            private const int MaxWeightPoints = 20;
            private WeightPoint[] _weightPoints = new WeightPoint[MaxWeightPoints];
            private int addedWeightPoints = 0;

            public WeightAnalizer(ExcavOSContext context) {
                _context = context;
            }

            public void QueryData(TimeSpan time) {
                Calculate();
                CalculateCapacityDelta(time);
            }

            public float GetLiftThresholdWarning() {
                return _context.config.LiftThresholdWarning;
            }

            private void Calculate() {

                if (_context.systemManager.ActiveController == null) {
                    Status = "Missing controller";
                    return;
                }

                if (_context.systemManager.ActiveController.CalculateShipMass().PhysicalMass == 0) {
                    Status = "Grid is static";
                }
                else {
                    Status = "";
                }
                float mass = _context.systemManager.ActiveController.CalculateShipMass().PhysicalMass;
                Vector3D direction = _context.systemManager.ActiveController.GetShipVelocities().LinearVelocity.Normalized();
                Vector3D gravity = _context.systemManager.ActiveController.GetNaturalGravity();
                double speed = _context.systemManager.ActiveController.GetShipSpeed();

                _context.thrusterManager.UpdateAll();
                LiftThrustUsage = (float)CalculateLiftThrustUsage(_context.systemManager.ActiveController, _context.thrusterManager);
                CalculateStopDistance(speed, direction, gravity, mass, _context.thrusterManager);
            }

            private double CalculateLiftThrustUsage(IMyShipController controller, ThrusterManager thrusterGroups) {
                double ThrustUsage = 0;
                float mass = controller.CalculateShipMass().PhysicalMass;
                Vector3D gravity = controller.GetNaturalGravity();
                double gravitationalForce = mass * gravity.Length();
                foreach (ThrustGroup thrustGroup in thrusterGroups.groups) {
                    var GravAccel = Vector3D.Dot(gravity, thrustGroup.direction);
                    if (GravAccel > 0) continue;

                    double maxAcceleration = thrustGroup.maxThrust / mass;
                    double effectiveAcceleration = maxAcceleration + GravAccel;

                    float requiredThrust = 1 - (float)(effectiveAcceleration / maxAcceleration);

                    if (ThrustUsage < requiredThrust) {
                        ThrustUsage = requiredThrust;
                    }
                }
                return ThrustUsage;
            }

            private void CalculateStopDistance(double CurrentSpeed, Vector3D Direction, Vector3D Gravity, Double Mass, ThrusterManager thrustGroups) {

                double effectiveAcceleration = thrustGroups.AccelerationInDirection(Direction, Gravity, Mass);
                StoppingTime = (float)(-CurrentSpeed / effectiveAcceleration);
                StoppingDistance = (float)(CurrentSpeed * StoppingTime + (effectiveAcceleration * StoppingTime * StoppingTime) / 2.0f);
            }

            private void CalculateCapacityDelta(TimeSpan time) {
                WeightPoint wp = new WeightPoint {
                    time = time.TotalSeconds,
                    capacity = _context.cargoManager.CurrentCapacity / _context.cargoManager.TotalCapacity
                };

                if (addedWeightPoints < MaxWeightPoints) {
                    _weightPoints[addedWeightPoints] = wp;
                    addedWeightPoints++;
                    CapacityDelta = 0;
                }
                else {
                    for (int n = 1; n < MaxWeightPoints; n++) {
                        _weightPoints[n - 1] = _weightPoints[n];
                    }
                    _weightPoints[MaxWeightPoints - 1] = wp;

                    float capacityIncrease = (float)(_weightPoints[MaxWeightPoints - 1].capacity - _weightPoints[0].capacity);
                    float timeIncrease = (float)(_weightPoints[MaxWeightPoints - 1].time - _weightPoints[0].time);
                    CapacityDelta = capacityIncrease / timeIncrease;
                }

            }
        }
    }
}
