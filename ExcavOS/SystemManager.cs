using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
        public class SystemManager {

            //public string Status;
            private readonly ExcavOSContext _context;
            //private Config _config;
            public ShipState ShipState { get { return _shipState; } }

            private ShipState _shipState = ShipState.isIdle;
            private readonly BlockFinder<IMyGyro> _gyros;
            private readonly BlockFinder<IMyShipController> _controllers;
            private readonly BlockFinder<IMyParachute> _parachutes;
            private readonly BlockFinder<IMyGasTank> _gasTanks;
            private readonly BlockFinder<IMyBatteryBlock> _batteries;
            private readonly BlockFinder<IMyThrust> _thrusters;
            private readonly BlockFinder<IMyShipConnector> _connectors;
            private IMyShipController _controller;
            private IMyParachute _parachute;

            public IMyShipController ActiveController { get { return _controller; } }

            public IMyParachute Parachute { get { return _parachute; } }
            public SystemManager(ExcavOSContext context) {
                _context = context;
                _gyros = new BlockFinder<IMyGyro>(_context);
                _controllers = new BlockFinder<IMyShipController>(_context);
                _thrusters = new BlockFinder<IMyThrust>(_context);
                _connectors = new BlockFinder<IMyShipConnector>(_context);
                _parachutes = new BlockFinder<IMyParachute>(_context);
                _gasTanks = new BlockFinder<IMyGasTank>(_context);
                _batteries = new BlockFinder<IMyBatteryBlock>(_context);
            }

            public void Update() {
                UpdateController();
                UpdateThrusterGroups();
                UpdateShipState();
                UpdateParachute();
            }

            public void UpdateShipState() {
                _connectors.FindBlocks(true);
                _gasTanks.FindBlocks(true);
                _batteries.FindBlocks(true);

                if (ActiveController.IsUnderControl) {
                    _shipState = ShipState.isControlled;
                }
                else {
                    if (_context.systemManager.ActiveController.CalculateShipMass().PhysicalMass == 0) {
                        _shipState = ShipState.isStatic;
                    }
                    else {
                        _shipState = ShipState.isIdle;
                        foreach (var block in _connectors.blocks) {
                            if (block.OtherConnector != null && block.Status == MyShipConnectorStatus.Connected) {
                                if (block.OtherConnector.CustomData.Contains(_context.config.DockTag)) {
                                    _shipState = ShipState.isDocked;
                                }
                            }
                        }
                    }
                }
                foreach (var block in _gasTanks.blocks) {
                    if (_shipState == ShipState.isDocked) {
                        block.Stockpile = true;
                    }
                    else {
                        block.Stockpile = false;
                    }
                }
                foreach (var block in _batteries.blocks) {
                    if (_shipState == ShipState.isDocked) {
                        block.ChargeMode = ChargeMode.Recharge;
                    }
                    else {
                        block.ChargeMode = ChargeMode.Auto;
                    }
                }
            }

            private void UpdateController() {
                IMyShipController firstWorking = null;
                _controllers.FindBlocks(true);
                foreach (IMyShipController _controller in _controllers.blocks) {
                    if (!_controller.IsWorking) continue;
                    if (firstWorking == null) firstWorking = _controller;
                    if (this._controller == null && _controller.IsUnderControl && _controller.CanControlShip) this._controller = _controller;
                    if (_controller.IsMainCockpit) this._controller = _controller;
                }
                if (_controller == null) _controller = firstWorking;

                if (_controller == null) {
                    throw new Exception("Missing Controller!");
                }
            }

            private void UpdateParachute() {
                if (_parachute != null && _parachute.IsWorking) return;
                _parachutes.FindBlocks(true);
                _parachute = _parachutes.GetFirstWorking();
            }

            private void UpdateThrusterGroups() {
                if (_controller == null) return;

                _context.thrusterManager.ClearAll();
                _thrusters.FindBlocks(true, thruster => {
                    _context.thrusterManager.Add(thruster, _controller);
                    return true;
                });
                _context.thrusterManager?.UpdateAll();
            }
        }
        public enum ShipState {
            isDocked,
            isControlled,
            isIdle,
            isStatic
        }
    }
}
