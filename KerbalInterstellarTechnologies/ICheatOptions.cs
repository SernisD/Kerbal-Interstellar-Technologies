using System;
using System.Collections.Generic;
using System.Text;

namespace KerbalInterstellarTechnologies
{
    public interface ICheatOptions
    {
        bool PauseOnVesselUnpack { get; }
        bool UnbreakableJoints { get; }
        bool NoCrashDamage { get; }
        bool IgnoreMaxTemperature { get; }
        bool InfinitePropellant { get; }
        bool InfiniteElectricity { get; }
        bool BiomesVisible { get; }
        bool AllowPartClipping { get; }
        bool NonStrictAttachmentOrientation { get; }
        bool IgnoreAgencyMindsetOnContracts { get; }
    }

    public class RealCheatOptions : ICheatOptions
    {
        public bool PauseOnVesselUnpack => CheatOptions.PauseOnVesselUnpack;
        public bool UnbreakableJoints => CheatOptions.UnbreakableJoints;
        public bool NoCrashDamage => CheatOptions.NoCrashDamage;
        public bool IgnoreMaxTemperature => CheatOptions.IgnoreMaxTemperature;
        public bool InfinitePropellant => CheatOptions.InfinitePropellant;
        public bool InfiniteElectricity => CheatOptions.InfiniteElectricity;
        public bool BiomesVisible => CheatOptions.BiomesVisible;
        public bool AllowPartClipping => CheatOptions.AllowPartClipping;
        public bool NonStrictAttachmentOrientation => CheatOptions.NonStrictAttachmentOrientation;
        public bool IgnoreAgencyMindsetOnContracts => CheatOptions.IgnoreAgencyMindsetOnContracts;

        private static RealCheatOptions instance;
        public static RealCheatOptions Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RealCheatOptions();
                }

                return instance;
            }
        }
    }

    public class ConfigurableCheatOptions : ICheatOptions
    {
        public bool PauseOnVesselUnpack { get; set; }
        public bool UnbreakableJoints { get; set; }
        public bool NoCrashDamage { get; set; }
        public bool IgnoreMaxTemperature { get; set; }
        public bool InfinitePropellant { get; set; }
        public bool InfiniteElectricity { get; set; }
        public bool BiomesVisible { get; set; }
        public bool AllowPartClipping { get; set; }
        public bool NonStrictAttachmentOrientation { get; set; }
        public bool IgnoreAgencyMindsetOnContracts { get; set; }
    }
}
