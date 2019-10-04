using System;
using Planetbase;
using PlanetbaseFramework;

namespace Planetbase_FreeBuilding
{
    public class FreeBuildingMod : ModBase
    {
        public override string ModName { get; } = "FreeBuildingMod";

        public const string AssemblyVersion = "2.2.1.0";
        public override Version ModVersion { get; } = new Version(AssemblyVersion);

        public override void Init()
        {
            TypeList<ModuleType, ModuleTypeList>.find<ModuleTypeMine>().mFlags |= ModuleType.FlagAutoRotate;
            InjectPatches();
        }
    }
}
