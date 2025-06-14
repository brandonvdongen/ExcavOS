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
        abstract public class ScriptHandler
        {
            protected Program _program;
            private int _tick;
            private int _tick10;
            private readonly string _spinner = "|/-\\|/-\\";
            protected readonly string _scriptName;
            private readonly string _scriptVersion;
            protected readonly MyIni _ini = new MyIni();
            protected readonly MyIni _storage;
            protected Config _config;
            protected TimeSpan _timeAccumulator = new TimeSpan();

            public ScriptHandler(Program program, MyIni storage , string scriptName, string scriptVersion)
            {
                _program = program;
                _storage = storage;
                _scriptName = scriptName;
                _scriptVersion = scriptVersion;
                _config = new Config(_ini, _scriptName);
             }

            public void Update(string argument, UpdateType updateSource, TimeSpan time)
            {
                _timeAccumulator += time;
                if ((updateSource & UpdateType.Update100) != 0)
                {
                    _tick++;

                    _program.Echo($"{_scriptName} (ver. {_scriptVersion}) is running {_spinner.Substring(_tick % _spinner.Length, 1)}\nLast run time: {_program.Runtime.LastRunTimeMs}ms");

                    if (_tick % 5 == 0)
                    {
                        Initialize();
                    }

                }
                if ((updateSource & UpdateType.Update10) != 0)
                {
                    _tick10++;
                    if (_tick10 % 3 == 0) {
                        Update10();
                    }
                    
                } 
                if (argument != "")
                {
                    HandleCommand(argument);
                }
            }

            private void CreateConfig()
            {
                if (MyIni.HasSection(_program.Me.CustomData, _scriptName))
                {
                    return;
                }

                _ini.Clear();
                _config.SetupDefaults();
                _program.Me.CustomData = _ini.ToString();

            }

            protected void Initialize()
            {
                CreateConfig();
                _config.LoadConfig(_program.Me.CustomData);
                FetchBlocks();
            }

            abstract public void FetchBlocks();
            protected virtual void Update10()
            {

            }

            protected virtual void HandleCommand(string argument)
            {

            }
        }
    }
}
