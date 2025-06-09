using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class BlockFinder<T> where T : class, IMyTerminalBlock {
            private const double CACHE_TIME = 10.0f;
            private readonly ExcavOSContext _context;
            private DateTime lastFetch;
            public readonly List<T> blocks = new List<T>();

            public BlockFinder(ExcavOSContext context) {
                this._context = context;
                lastFetch = DateTime.Now;
                lastFetch.AddSeconds(-CACHE_TIME);
            }

            public void FindBlocks(bool sameConstruct = true, Func<T, bool> filter = null, string groupName = null) {
                if (blocks.Count > 0 && lastFetch.AddSeconds(CACHE_TIME).CompareTo(DateTime.Now) >= 0) {
                    return;
                }

                Func<T, bool> filterFunc = block => {
                    bool constructCheck = true;
                    if (block is IMyTerminalBlock) {
                        if (sameConstruct) {
                            constructCheck = (block as IMyTerminalBlock).IsSameConstructAs(_context.program.Me);
                        }
                        else {
                            constructCheck = !(block as IMyTerminalBlock).IsSameConstructAs(_context.program.Me);
                        }
                    }
                    return constructCheck && ((filter != null) ? filter(block) : true);
                };

                lastFetch = new DateTime();
                blocks.Clear();

                if (groupName != null && groupName != "") {
                    IMyBlockGroup group = _context.program.GridTerminalSystem.GetBlockGroupWithName(groupName);
                    if (group != null) {
                        group.GetBlocksOfType(blocks, filterFunc);
                    }
                }
                else {
                    _context.program.GridTerminalSystem.GetBlocksOfType(blocks, filterFunc);
                }
            }

            public T GetFirstWorking() {
                if (!HasBlocks()) return null;
                foreach (T block in blocks) {
                    if (!block.IsWorking) continue;
                    return block;
                }
                return null;
            }
            public void ForEach(Action<T> callback) {
                blocks.ForEach(callback);
            }

            public bool HasBlocks() {
                return blocks.Count > 0;
            }

            public int Count() {
                return blocks.Count;
            }
        }
    }
}
