using System.Collections.Generic;

namespace KerbalInterstellarTechnologies
{
    /// <summary>
    /// This interface is passed to the part modules in IKITMod.KITFixedUpdate. It allows the 
    /// production and consumption of resources, and access to some wrapper variables to avoid global
    /// variable access.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Consumes resources from the resource pool. It automatically converts your request to per-seconds,
        /// so you do not need to account for that yourself.
        /// </summary>
        /// <param name="name">Resource Name</param>
        /// <param name="wanted">Requested amount of resource to consume per second</param>
        /// <returns>Amount of resource that there is to consume (per second)</returns>
        double ConsumeResource(string name, double wanted);

        /// <summary>
        /// Adds resources to the resource pool. It automatically converts your request to per-seconds,
        /// so you do not need to account for that yourself.
        /// </summary>
        /// <param name="name">Resource Name</param>
        /// <param name="amount">Amount of resource to produce per second</param>
        void ProduceResource(string name, double amount);

        /// <summary>Provides access to the (equivilient) of TimeWarp.fixedDeltaTime.</summary>
        /// <remarks>
        /// The resource interface automatically converts everything to per-second values for you. You only need
        /// access to this in special cases.
        /// </remarks>
        double FixedDeltaTime();

        /// <summary>
        /// Access to the cheat options that the code should use.  Use this instead of the global variable CheatOptions
        /// </summary>
        /// <returns>The ICheatOptions associated with this resource manager.</returns>
        ICheatOptions CheatOptions();
    }

    /// <summary>
    /// IKITMod defines an interface used by Kerbal Interstellar Technologies PartModules. Through this interface,
    /// various functions are called on the PartModule from the KIT Resource Manager Vessel Module.
    /// </summary>
    public interface IKITMod
    {
        /// <summary>
        /// This is the priority that the module should run at, from 1 to 5. 1 will be ran first, and 5 will be ran last.
        /// </summary>
        /// <returns>Part Priority</returns>
        int ResourceProcessPriority();
        /// <summary>
        /// KITFixedUpdate replaces the FixedUpdate function for Kerbal Interstellar Technologies PartModules. 
        /// </summary>
        /// <param name="resMan">Interface to the resource manager, and options that should be used when the code is running</param>
        void KITFixedUpdate(IResourceManager resMan);
        /// <summary>
        /// 
        /// </summary>
        /// <returns>String to identify the part</returns>
        string KITPartName();
    }

    public interface IKITVariableSupplier
    {
        /// <summary>
        /// What resources can this module provide on demand?
        /// </summary>
        /// <returns>Returns a list of strings for the resources it can provide</returns>
        string[] ResourcesProvided();

        double ProvideResource(string name, double requestedAmount);
    }

    

}