namespace BEPUphysics
{
    ///<summary>
    /// Contains settings for the instance's time step.
    ///</summary>
    public class TimeStepSettings
    {
        /// <summary>
        /// Maximum number of timesteps to perform during a given frame when Space.Update(double) is used.  The unsimulated time will be accumulated for subsequent calls to Space.Update(double).
        /// Defaults to 3.
        /// </summary>
        public int MaximumTimeStepsPerFrame = 3;

        /// <summary>
        /// Length of each integration step.  Calling a Space's Update() method moves time forward this much.
        /// The other method, Space.Update(double), will try to move time forward by the amount specified in the parameter by taking steps of TimeStepDuration size.
        /// Defaults to 1/60.
        /// </summary>
        public double TimeStepDuration = 1f / 60;

        /// <summary>
        /// Amount of time accumulated by previous calls to Space.Update(double) that has not yet been simulated.
        /// </summary>
        public double AccumulatedTime;
    }
}
