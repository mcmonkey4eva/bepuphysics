﻿using System;
using BEPUphysics.Constraints;
using BEPUphysics.Entities;
 
using BEPUphysics.Materials;
using BEPUutilities;

namespace BEPUphysics.Vehicle
{
    /// <summary>
    /// Attempts to resist rolling motion of a vehicle.
    /// </summary>
    public class WheelBrake : ISolverSettings
    {
        #region Static Stuff

        /// <summary>
        /// Default blender used by WheelRollingFriction constraints.
        /// </summary>
        public static WheelFrictionBlender DefaultRollingFrictionBlender;

        static WheelBrake()
        {
            DefaultRollingFrictionBlender = BlendFriction;
        }

        /// <summary>
        /// Function which takes the friction values from a wheel and a supporting material and computes the blended friction.
        /// </summary>
        /// <param name="wheelFriction">Friction coefficient associated with the wheel.</param>
        /// <param name="materialFriction">Friction coefficient associated with the support material.</param>
        /// <param name="usingKineticFriction">True if the friction coefficients passed into the blender are kinetic coefficients, false otherwise.</param>
        /// <param name="wheel">Wheel being blended.</param>
        /// <returns>Blended friction coefficient.</returns>
        public static double BlendFriction(double wheelFriction, double materialFriction, bool usingKineticFriction, Wheel wheel)
        {
            return wheelFriction * materialFriction;
        }

        #endregion

        internal double accumulatedImpulse;

        //double linearBX, linearBY, linearBZ;
        private double angularAX, angularAY, angularAZ;
        private double angularBX, angularBY, angularBZ;
        internal bool isActive = true;
        private double linearAX, linearAY, linearAZ;
        private double blendedCoefficient;
        private double kineticBrakingFrictionCoefficient;
        private WheelFrictionBlender frictionBlender = DefaultRollingFrictionBlender;
        private bool isBraking;
        private double rollingFrictionCoefficient;
        internal SolverSettings solverSettings = new SolverSettings();
        private double staticBrakingFrictionCoefficient;
        private double staticFrictionVelocityThreshold = 5f;
        private Wheel wheel;
        internal int numIterationsAtZeroImpulse;
        private Entity vehicleEntity, supportEntity;

        //Inverse effective mass matrix
        private double velocityToImpulse;
        private bool supportIsDynamic;


        /// <summary>
        /// Constructs a new rolling friction object for a wheel.
        /// </summary>
        /// <param name="dynamicBrakingFrictionCoefficient">Coefficient of dynamic friction of the wheel for friction when the brake is active.</param>
        /// <param name="staticBrakingFrictionCoefficient">Coefficient of static friction of the wheel for friction when the brake is active.</param>
        /// <param name="rollingFrictionCoefficient">Coefficient of friction of the wheel for rolling friction when the brake isn't active.</param>
        public WheelBrake(double dynamicBrakingFrictionCoefficient, double staticBrakingFrictionCoefficient, double rollingFrictionCoefficient)
        {
            KineticBrakingFrictionCoefficient = dynamicBrakingFrictionCoefficient;
            StaticBrakingFrictionCoefficient = staticBrakingFrictionCoefficient;
            RollingFrictionCoefficient = rollingFrictionCoefficient;
        }

        internal WheelBrake(Wheel wheel)
        {
            Wheel = wheel;
        }

        /// <summary>
        /// Gets the coefficient of rolling friction between the wheel and support.
        /// This coefficient is the blended result of the supporting entity's friction and the wheel's friction.
        /// </summary>
        public double BlendedCoefficient
        {
            get { return blendedCoefficient; }
        }

        /// <summary>
        /// Gets or sets the coefficient of braking dynamic friction for this wheel.
        /// This coefficient and the supporting entity's coefficient of friction will be 
        /// taken into account to determine the used coefficient at any given time.
        /// This coefficient is used instead of the rollingFrictionCoefficient when 
        /// isBraking is true.
        /// </summary>
        public double KineticBrakingFrictionCoefficient
        {
            get { return kineticBrakingFrictionCoefficient; }
            set { kineticBrakingFrictionCoefficient = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Gets the axis along which rolling friction is applied.
        /// </summary>
        public Vector3 FrictionAxis
        {
            get { return wheel.drivingMotor.ForceAxis; }
        }

        /// <summary>
        /// Gets or sets the function used to blend the supporting entity's friction and the wheel's friction.
        /// </summary>
        public WheelFrictionBlender FrictionBlender
        {
            get { return frictionBlender; }
            set { frictionBlender = value; }
        }

        /// <summary>
        /// Gets or sets whether or not the wheel is braking.
        /// When set to true, the brakingFrictionCoefficient is used.
        /// When false, the rollingFrictionCoefficient is used.
        /// </summary>
        public bool IsBraking
        {
            get { return isBraking; }
            set { isBraking = value; }
        }

        /// <summary>
        /// Gets or sets the coefficient of rolling friction for this wheel.
        /// This coefficient and the supporting entity's coefficient of friction will be 
        /// taken into account to determine the used coefficient at any given time.
        /// This coefficient is used instead of the brakingFrictionCoefficient when 
        /// isBraking is false.
        /// </summary>
        public double RollingFrictionCoefficient
        {
            get { return rollingFrictionCoefficient; }
            set { rollingFrictionCoefficient = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the coefficient of static dynamic friction for this wheel.
        /// This coefficient and the supporting entity's coefficient of friction will be 
        /// taken into account to determine the used coefficient at any given time.
        /// This coefficient is used instead of the rollingFrictionCoefficient when 
        /// isBraking is true.
        /// </summary>
        public double StaticBrakingFrictionCoefficient
        {
            get { return staticBrakingFrictionCoefficient; }
            set { staticBrakingFrictionCoefficient = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets the velocity under which the coefficient of static friction will be used instead of the dynamic one.
        /// </summary>
        public double StaticFrictionVelocityThreshold
        {
            get { return staticFrictionVelocityThreshold; }
            set { staticFrictionVelocityThreshold = Math.Abs(value); }
        }

        /// <summary>
        /// Gets the force 
        /// </summary>
        public double TotalImpulse
        {
            get { return accumulatedImpulse; }
        }

        /// <summary>
        /// Gets the wheel that this rolling friction applies to.
        /// </summary>
        public Wheel Wheel
        {
            get { return wheel; }
            internal set { wheel = value; }
        }

        #region ISolverSettings Members

        /// <summary>
        /// Gets the solver settings used by this wheel constraint.
        /// </summary>
        public SolverSettings SolverSettings
        {
            get { return solverSettings; }
        }

        #endregion

        ///<summary>
        /// Gets the relative velocity along the braking direction at the wheel contact.
        ///</summary>
        public double RelativeVelocity
        {
            get
            {
                double velocity = vehicleEntity.linearVelocity.X * linearAX + vehicleEntity.linearVelocity.Y * linearAY + vehicleEntity.linearVelocity.Z * linearAZ +
                            vehicleEntity.angularVelocity.X * angularAX + vehicleEntity.angularVelocity.Y * angularAY + vehicleEntity.angularVelocity.Z * angularAZ;
                if (supportEntity != null)
                    velocity += -supportEntity.linearVelocity.X * linearAX - supportEntity.linearVelocity.Y * linearAY - supportEntity.linearVelocity.Z * linearAZ +
                                supportEntity.angularVelocity.X * angularBX + supportEntity.angularVelocity.Y * angularBY + supportEntity.angularVelocity.Z * angularBZ;
                return velocity;
            }
        }

        internal double ApplyImpulse()
        {
            //Compute relative velocity and convert to impulse
            double lambda = RelativeVelocity * velocityToImpulse;


            //Clamp accumulated impulse
            double previousAccumulatedImpulse = accumulatedImpulse;
            double maxForce = -blendedCoefficient * wheel.suspension.accumulatedImpulse;
            accumulatedImpulse = MathHelper.Clamp(accumulatedImpulse + lambda, -maxForce, maxForce);
            lambda = accumulatedImpulse - previousAccumulatedImpulse;

            //Apply the impulse
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            linear.X = lambda * linearAX;
            linear.Y = lambda * linearAY;
            linear.Z = lambda * linearAZ;
            if (vehicleEntity.isDynamic)
            {
                angular.X = lambda * angularAX;
                angular.Y = lambda * angularAY;
                angular.Z = lambda * angularAZ;
                vehicleEntity.ApplyLinearImpulse(ref linear);
                vehicleEntity.ApplyAngularImpulse(ref angular);
            }
            if (supportIsDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = lambda * angularBX;
                angular.Y = lambda * angularBY;
                angular.Z = lambda * angularBZ;
                supportEntity.ApplyLinearImpulse(ref linear);
                supportEntity.ApplyAngularImpulse(ref angular);
            }

            return lambda;
        }

        internal void PreStep(double dt)
        {
            vehicleEntity = wheel.Vehicle.Body;
            supportEntity = wheel.SupportingEntity;
            supportIsDynamic = supportEntity != null && supportEntity.isDynamic;

            //Grab jacobian and mass matrix from the driving motor!
            linearAX = wheel.drivingMotor.linearAX;
            linearAY = wheel.drivingMotor.linearAY;
            linearAZ = wheel.drivingMotor.linearAZ;

            angularAX = wheel.drivingMotor.angularAX;
            angularAY = wheel.drivingMotor.angularAY;
            angularAZ = wheel.drivingMotor.angularAZ;
            angularBX = wheel.drivingMotor.angularBX;
            angularBY = wheel.drivingMotor.angularBY;
            angularBZ = wheel.drivingMotor.angularBZ;

            velocityToImpulse = wheel.drivingMotor.velocityToImpulse;

            //Friction
            //Which coefficient? Check velocity.
            if (isBraking)
                if (Math.Abs(RelativeVelocity) < staticFrictionVelocityThreshold)
                    blendedCoefficient = frictionBlender(staticBrakingFrictionCoefficient, wheel.supportMaterial.staticFriction, false, wheel);
                else
                    blendedCoefficient = frictionBlender(kineticBrakingFrictionCoefficient, wheel.supportMaterial.kineticFriction, true, wheel);
            else
                blendedCoefficient = rollingFrictionCoefficient;


        }

        internal void ExclusiveUpdate()
        {

            //Warm starting
#if !WINDOWS
            Vector3 linear = new Vector3();
            Vector3 angular = new Vector3();
#else
            Vector3 linear, angular;
#endif
            linear.X = accumulatedImpulse * linearAX;
            linear.Y = accumulatedImpulse * linearAY;
            linear.Z = accumulatedImpulse * linearAZ;
            if (vehicleEntity.isDynamic)
            {
                angular.X = accumulatedImpulse * angularAX;
                angular.Y = accumulatedImpulse * angularAY;
                angular.Z = accumulatedImpulse * angularAZ;
                vehicleEntity.ApplyLinearImpulse(ref linear);
                vehicleEntity.ApplyAngularImpulse(ref angular);
            }
            if (supportIsDynamic)
            {
                linear.X = -linear.X;
                linear.Y = -linear.Y;
                linear.Z = -linear.Z;
                angular.X = accumulatedImpulse * angularBX;
                angular.Y = accumulatedImpulse * angularBY;
                angular.Z = accumulatedImpulse * angularBZ;
                supportEntity.ApplyLinearImpulse(ref linear);
                supportEntity.ApplyAngularImpulse(ref angular);
            }
        }
    }
}