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
        public class ExcavOS : ScriptHandler {

            protected ExcavOSContext _context;
            private readonly BlockFinder<IMyTerminalBlock> _surfaceProviders;
            private Dictionary<long, RegisteredProvider> _registeredProviders = new Dictionary<long, RegisteredProvider>();

            public ExcavOS(Program program, MyIni storage) : base(program, storage, "ExcavOS", "0.2") {
                _context = new ExcavOSContext(program, _config, _storage);
                _surfaceProviders = new BlockFinder<IMyTerminalBlock>(_context);

                Initialize();
            }

            public void Save() {
                _context.Save();
            }

            public override void FetchBlocks() {
                FetchSurfaces();
            }

            private void FetchSurfaces() {
                _surfaceProviders.FindBlocks(true, block => {

                    if (!(block is IMyTextSurfaceProvider)) {
                        return false;
                    }

                    if (!MyIni.HasSection(block.CustomData, _scriptName)) {
                        return false;
                    }

                    RegisteredProvider registeredProvider;
                    if (!_registeredProviders.ContainsKey(block.EntityId)) {
                        registeredProvider = new RegisteredProvider(_context, block, _ini, _scriptName);
                        _registeredProviders.Add(block.EntityId, registeredProvider);
                    }
                    else {
                        registeredProvider = _registeredProviders[block.EntityId];
                    }

                    if (block == _program.Me) {
                        registeredProvider.SetScreenHandlerForSurface(ExcavOSScreen.SCREEN_NAME, 0);
                    }
                    else {
                        registeredProvider.LoadConfig(block.CustomData);
                    }

                    if (!registeredProvider.HasSurfaces()) {
                        _registeredProviders.Remove(block.EntityId);
                    }

                    return true;

                });

            }

            protected override void Update10() {
                _context.Update(_timeAccumulator);
                foreach (var registeredProvider in _registeredProviders.Values) {
                    registeredProvider.Update();
                }
            }

            protected override void HandleCommand(string argument) {
                _context.HandleCommand(argument);
            }

        }

    }
}
