using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KerbalInterstellarTechnologies.Settings;

namespace KerbalInterstellarTechnologies.Electrical
{
    public class KITRadioisotopeGenerator : PartModule, IKITMod
    {
        [KSPField(isPersistant = true)] public double upgradeMultiplier = 1;
        [KSPField(isPersistant = true)] public double powerMultiplier = (8 / 0.75); // 8kg of Pu238 gives 0.75W output
        [KSPField(isPersistant = true)] public double halfLife = (
            GameSettings.KERBIN_TIME ?
                87.7 * 426 * 6 * 60 * 60 :
                87.7 * 365 * 24 * 60 * 60
        );

        /*
         * Per AntaresMC, The typical RTG has a mass ratio of about 1.1 and a peltier of about 10% efficiency.
         * With 80kg that would give about 8kg of Pu238, heat output of about 5kW and power output of about 1/2kW. Seems about right,
         * just lets say they add a bit more plutonium, pr their peltier is a bit better
         * 
         * I would add 3 tech levels, RTG, using the same calculations as the peltier generator on areactor with a core temp
         * proportional yo how full of trash in respect to fuel is it, or just a flat 15%; alphavoltaics (first upgrade), a flat
         * 50% in heavy fuels (actinides, Pu238, Po210 if it gets added, etc); and ßvoltaics, a flat lets say 40% in light
         * fuels (T and FissionProducts when get added) and maybe an improvement to 60% in ghe heavg ones, idk
         * 
         * Ok, so, actinides would produce about a watt per kg forever, alpha emitter
         * Pu238 (that is extracted from actinide waste) about 500W/kg, halving each 90ish years, alpha emitter
         * FissionProducts (what is left after all fuel has been burned and reprocessed over and over again
         * would give about 250W/kg halving each 30ish years, ß emiter T would give about 36kW/kg, ß emitter,
         * halves at about 12y
         * 
         * I didnt find the notes, had to calculate it again :tired_face:
         * As a bonus, but I dont think its worth the extra complexity, Po210 is an alpha emitter that gives about 140kW/kg
         * and halves in about 5 months
         */

        /// <summary>
        /// DecayResource calculates the amount to decay the given resource by.
        /// </summary>
        /// <param name="halfLife">half life of the material</param>
        /// <param name="resource">resource to deplete</param>
        /// <param name="fixedDeltaTime">how much time has elapsed</param>
        public static void DecayResource(PartResource resource, double halfLife, double fixedDeltaTime)
        {
            // should do this vessel wide.
            double decayAmount = Math.Pow(2, (-fixedDeltaTime) / halfLife);
            // decay into useful products?
            resource.amount *= decayAmount;
        }

        /// <summary>
        /// Generates power given how much of that there is in storage.
        /// </summary>
        /// <param name="resource">Resource definition to use</param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static double GeneratePower(PartResource resource, double powerMultiplier, double upgradeMultiplier)
        {
            double kilograms = resource.amount;

            double power = kilograms * powerMultiplier * upgradeMultiplier;

            return power;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            var power = GeneratePower(part.Resources[0], powerMultiplier, upgradeMultiplier);
            resMan.ProduceResource(ResourceName.ElectricCharge, power);
            // DecayResource(part.Resources[0], halfLife, resMan.FixedDeltaTime());
            // part.Resources[0].part.vessel.FindVesselModuleImplementing
        }

        public string KITPartName() => "Radioisotope Generator";

        public override string GetInfo()
        {
            return "";
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First | ResourcePriorityValue.SupplierOnlyFlag;
    }
}
