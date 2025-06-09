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
        public class UtilityManager {
            private string sectionKey = "UtilMan";
            private readonly ExcavOSContext _context;
            public string Status;
            private readonly BlockFinder<IMyGyro> _gyros;
            private readonly BlockFinder<IMyShipController> _controllers;
            private readonly BlockFinder<IMyConveyorSorter> _sorters;
            private readonly BlockFinder<IMyBatteryBlock> _batteries;
            private readonly BlockFinder<IMyGasTank> _hydrogenTanks;
            private readonly BlockFinder<IMyReactor> _reactors;
            private readonly List<MyInventoryItemFilter> sorterList = new List<MyInventoryItemFilter>();

            public bool GravityAlign = false;
            public bool GravityArtificial = false;
            public bool CruiseEnabled = false;
            private bool _GravityAlignActive = false;
            public float GravityAlignPitch = 0;
            public float CruiseTarget = 0;
            public string BatteryCharge = "";
            public string HydrogenCharge = "";
            public double BatteryLevel = 0;
            public double HydrogenLevel = 0;
            public double UraniumLevel = 0;

            public UtilityManager(ExcavOSContext context) {
                _context = context;
                _gyros = new BlockFinder<IMyGyro>(_context);
                _sorters = new BlockFinder<IMyConveyorSorter>(_context);
                _controllers = new BlockFinder<IMyShipController>(_context);
                _batteries = new BlockFinder<IMyBatteryBlock>(_context);
                _hydrogenTanks = new BlockFinder<IMyGasTank>(_context);
                _reactors = new BlockFinder<IMyReactor>(_context);

                Initialize();
            }

            public void Save() {
                _context.storage.Set(sectionKey, "GAP", GravityAlignPitch);
                _context.storage.Set(sectionKey, "CruiseTarget", CruiseTarget);
            }

            protected void Initialize() {
                GravityAlignPitch = (float)_context.storage.Get(sectionKey, "GAP").ToDouble(0);
                CruiseTarget = (float)_context.storage.Get(sectionKey, "CruiseTarget").ToDouble(0);
            }

            public void Update() {
                _gyros.FindBlocks(true, null, _context.config.AlignGyrosGroupName);
                _sorters.FindBlocks(true, null, _context.config.DumpSortersGroupName);
                _controllers.FindBlocks(true, null);
                _batteries.FindBlocks(true, null);
                _reactors.FindBlocks(true, null);
                _hydrogenTanks.FindBlocks(true, tank => {
                    return BlockHelper.IsHydrogenTank(tank);
                });
                CalculateCharge();
                CalculateHydrogen();
                CalculateUranium();

                IMyShipController controller = null;
                IMyShipController firstWorking = null;
                foreach (IMyShipController _controller in _controllers.blocks) {
                    if (!_controller.IsWorking) continue;
                    if (firstWorking == null) firstWorking = _controller;
                    if (controller == null && _controller.IsUnderControl && _controller.CanControlShip) controller = _controller;
                    if (_controller.IsMainCockpit) controller = _controller;
                }
                if (controller == null) controller = firstWorking;

                if (controller == null) {
                    Status = "Missing controller";
                    return;
                }

                if (_gyros.Count() == 0) {
                    Status = "Missing gyros";
                    return;
                }

                Status = "";

                if (GravityAlign) {
                    _GravityAlignActive = true;
                    DoGravityAlign(controller, _gyros.blocks, GravityAlignPitch);
                }
                if (!GravityAlign && _GravityAlignActive) {
                    _GravityAlignActive = false;
                    ReleaseGyros(_gyros.blocks);
                }

                if (CruiseEnabled) {
                    double currentSpeed = controller.GetShipSpeed();

                    var error = CruiseTarget - currentSpeed;
                    float mass = _context.systemManager.ActiveController.CalculateShipMass().PhysicalMass;
                    float thrust = (float)_context.thrusterManager.forward.maxThrust;
                    float maxAccel = thrust / mass;

                    _context.thrusterManager.forward.thrusters.ForEach(thruster => {
                        if (error > 0.1) {
                            thruster.Enabled = true;
                            thruster.ThrustOverridePercentage = Math.Min(100, (float)error / CruiseTarget * 100 / maxAccel) * 0.1f;
                        }
                        else {
                            thruster.Enabled = false;
                            thruster.ThrustOverridePercentage = 0f;
                        }
                    });
                    _context.thrusterManager.backward.thrusters.ForEach(thruster => {
                        if (error < -0.1) {
                            thruster.Enabled = true;
                            thruster.ThrustOverridePercentage = Math.Min(100, (float)error / CruiseTarget * 100 / maxAccel) * 0.1f;
                        }
                        else {
                            thruster.Enabled = false;
                            thruster.ThrustOverridePercentage = 0f;
                        }
                    });
                }
            }

            public void SetSortersFilter(string item_id) {
                try {
                    List<MyInventoryItemFilter> currentList = new List<MyInventoryItemFilter>();
                    MyInventoryItemFilter item = new MyInventoryItemFilter("MyObjectBuilder_Ore/" + item_id);
                    _sorters.ForEach(sorter => {
                        sorter.GetFilterList(currentList);
                        if (currentList.Contains(item)) {
                            currentList.Remove(item);
                        }
                        else {
                            currentList.Add(item);
                        }
                        sorter.SetFilter(MyConveyorSorterMode.Whitelist, currentList);
                    });
                }
                catch (Exception) {
                }
            }

            public string GetSortersFilter() {
                if (_sorters.Count() == 0) {
                    return "No sorters";
                }

                string firstItem = "";
                foreach (var sorter in _sorters.blocks) {
                    sorter.GetFilterList(sorterList);
                    if (sorterList.Count == 0) {
                        continue;
                    }
                    if (sorterList.Count > 1) {
                        return "Multiple items";
                    }
                    else {
                        if (firstItem == "") {
                            firstItem = sorterList[0].ItemId.ToString();
                        }
                        if (firstItem != sorterList[0].ItemId.ToString()) {
                            return "Multiple items";
                        }
                    }
                }
                if (firstItem == "") {
                    return "No filters";
                }
                return firstItem.Replace("MyObjectBuilder_Ore/", "");
            }

            private void CalculateHydrogen() {
                if (_hydrogenTanks.Count() == 0) {
                    HydrogenCharge = "N/A";
                    HydrogenLevel = 0;
                    return;
                }
                double total = 0;
                double capacity = 0;
                _hydrogenTanks.ForEach(tank => {
                    total += tank.FilledRatio * tank.Capacity;
                    capacity += tank.Capacity;
                });
                HydrogenLevel = total / capacity;
                HydrogenCharge = string.Format("{0:0.0}%", (total / capacity) * 100);
            }

            private void CalculateCharge() {
                float stored = 0;
                float max = 0;
                float delta = 0;
                _batteries.ForEach(battery => {
                    if (!battery.IsWorking) {
                        return;
                    }

                    delta += battery.CurrentInput - battery.CurrentOutput;
                    stored += battery.CurrentStoredPower;
                    max += battery.MaxStoredPower;
                });

                if (max > 0) {
                    BatteryCharge = string.Format("{0:0.0}%", (stored / max) * 100);
                    BatteryLevel = stored / max;
                }
                else {
                    BatteryCharge = "N/A";
                    BatteryLevel = 0;
                }
            }

            private void CalculateUranium() {
                if (_reactors.Count() == 0) {
                    //ReactorsCharge = "N/A";
                    UraniumLevel = 0;
                    return;
                }
                double total = 0;
                _reactors.ForEach(reactor => {
                    total += BlockHelper.GetReactorFuelLevel(reactor);
                });
                UraniumLevel = total / _reactors.Count();
            }
            private double DoGravityAlign(IMyShipController controller, List<IMyGyro> gyrosToUse, float pitch = 0f, bool onlyCalculate = false) {
                // Thanks to https://forum.keenswh.com/threads/aligning-ship-to-planet-gravity.7373513/#post-1286885461 

                double coefficient = 0.9;
                Matrix orientation;
                controller.Orientation.GetMatrix(out orientation);

                Vector3D down = orientation.Down;
                if (pitch < 0) {
                    down = Vector3D.Lerp(orientation.Down, orientation.Forward, -pitch / 90);
                }
                else if (pitch > 0) {
                    down = Vector3D.Lerp(orientation.Down, -orientation.Forward, pitch / 90);
                }

                Vector3D gravity = controller.GetNaturalGravity();
                if (gravity.Length() == 0) gravity = controller.GetArtificialGravity();
                if (gravity.Length() != 0) gravity.Normalize();

                double offLevel = 0.0;

                foreach (var gyro in gyrosToUse) {

                    if (Math.Abs(controller.RotationIndicator.Length()) > 0.1f || Math.Abs(controller.RollIndicator) > 0.1f || gravity.Length() == 0) {
                        gyro.GyroOverride = false;
                        continue;
                    }

                    gyro.Orientation.GetMatrix(out orientation);
                    var localDown = Vector3D.Transform(down, MatrixD.Transpose(orientation));
                    var localGrav = Vector3D.Transform(gravity, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                    var rotation = Vector3D.Cross(localDown, localGrav);
                    double ang = rotation.Length();
                    ang = Math.Atan2(ang, Math.Sqrt(Math.Max(0.0, 1.0 - ang * ang)));
                    offLevel += ang * 180.0 / 3.14;

                    if (!onlyCalculate) {
                        double controlVelocity = gyro.GetMaximum<float>("Yaw") * (ang / Math.PI) * coefficient;
                        controlVelocity = Math.Min(gyro.GetMaximum<float>("Yaw"), controlVelocity);
                        controlVelocity = Math.Max(0.01, controlVelocity); //Gyros don't work well at very low speeds
                        rotation.Normalize();
                        rotation *= controlVelocity;
                        gyro.SetValueFloat("Pitch", (float)rotation.GetDim(0));
                        gyro.SetValueFloat("Yaw", -((float)rotation.GetDim(1)));
                        gyro.SetValueFloat("Roll", -(float)rotation.GetDim(2));
                        //gyro.SetValueFloat("Power", 1.0f);
                        gyro.GyroOverride = true;
                    }
                }
                if (gyrosToUse.Count() == 0) {
                    return -1000;
                }
                return offLevel / gyrosToUse.Count();
            }
            private void ReleaseGyros(List<IMyGyro> gyros) {
                foreach (IMyGyro gyro in gyros) {
                    gyro.SetValueFloat("Pitch", 0f);
                    gyro.SetValueFloat("Yaw", 0f);
                    gyro.SetValueFloat("Roll", 0f);
                    gyro.SetValueFloat("Power", 1.0f);
                    gyro.GyroOverride = false;
                }
            }
        }
    }
}
