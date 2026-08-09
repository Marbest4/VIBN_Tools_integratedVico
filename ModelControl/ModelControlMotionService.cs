using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FS.SDK.Scene.Objects;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.Application.VM.ModelControlPageVM;

namespace VIBN_Tools.ModelControl
{
    public class ModelControlMotionService
    {

        //===========================================================================================================================
        // G E N E R A L   P R O P E R T I E S   &   M E T  H O D S
        //===========================================================================================================================


        public event Action<string, Severity> StatusChanged;
        public event Action<double[]> JointValuesUpdated;
        public event Action<bool> MovingStateChanged;

        private CancellationTokenSource _movementCts;


        private bool _isMoving;
        public bool IsMoving
        {
            get => _isMoving;
            private set
            {
                _isMoving = value;
                MovingStateChanged?.Invoke(value);
            }
        }






        private async Task MoveInterpolatedAsync(List<FeeJoint> joints, double[] targetAxesPositions, double durationMs, CancellationToken token)
        {
            var currentJointValues = await ReadCurrentJointValuesAsync(joints);

            var validJoints = joints.Where(x => x != null).ToList();

            int axisCount = Math.Min(validJoints.Count, targetAxesPositions.Length);            

            // Check if already at tragetPos
            bool alreadyAtTarget = currentJointValues
                    .Take(axisCount)
                    .Select(v => Math.Round(v, 3))
                    .SequenceEqual(targetAxesPositions.Take(axisCount).Select(v => Math.Round(v, 3)));

            if (alreadyAtTarget)
                return;


            // configure interpolation
            int steps = 100;
            int stepDelay = Math.Max(10, (int)(durationMs / steps));

            for (int step = 1; step <= steps; step++)
            {
                token.ThrowIfCancellationRequested();

                double t = (double)step / steps;
                double factor = 0.5 - 0.5 * Math.Cos(Math.PI * t);

                var interpolatedValues = new double[axisCount];

                for (int i = 0; i < axisCount; i++)
                {
                    interpolatedValues[i] = currentJointValues[i] + (targetAxesPositions[i] - currentJointValues[i]) * factor;
                }

                // Write values to simulation
                await Services.ApiInstance.Object.SetSlotValuesAsync(
                    joints.Take(axisCount).Select(j => j.Guid).ToArray(),
                    Enumerable.Repeat("InValue", axisCount).ToArray(),
                    interpolatedValues.Take(axisCount).Select(v => (object)v).ToArray()
                    );


                // Update view
                JointValuesUpdated?.Invoke(interpolatedValues.ToArray());
                await Task.Delay(stepDelay, token);

            }

        }



        public static async Task<double[]> ReadCurrentJointValuesAsync(List<FeeJoint> joints)
        {
            var validJoints = joints.Where(x => x != null).ToList();

            var values = new double[validJoints.Count];

            for (int i = 0; i < validJoints.Count; i++)
            {
                var guid = validJoints[i].Guid;

                var slotValue = await Services.ApiInstance.Object.GetSlotValueAsync(guid, nameof(MotionJoint.InValue));

                values[i] = Services.ApiInstance.XmlHelper.ConvertToFloat(slotValue);
            }

            return values;
        }


        public void Cancel()
        {
            _movementCts.Cancel();
        }





        //===========================================================================================================================
        // R O B O T - C O N T R O L   S P E C I F I C
        //===========================================================================================================================


        public async Task<bool> MoveRobotToPositionAsync(RobotControlData robot, RobotControlPath path, RobotControlPosition targetPos, SimRobotDefinition simRobot, int velocityPercent, bool driveSinglePosition)
        {
            StatusChanged?.Invoke("Moving to target...", Severity.Info);
            IsMoving = true;

            // Reset states
            foreach (var pos in path.Positions)
                pos.State = PositionState.Pending;

            int targetIndex = path.Positions.IndexOf(targetPos);
            if (targetIndex < 0)
                return false;

            int speedPercent = Math.Clamp(velocityPercent, 1, 100);
            double durationMs = 1000 * (100.0 / speedPercent);

            _movementCts?.Cancel();
            _movementCts = new CancellationTokenSource();
            var token = _movementCts.Token;

            try
            {
                if (!driveSinglePosition)
                {
                    for (int i = 0; i <= targetIndex; i++)
                    {
                        var axesPositions = ExtractAxesPositions(path.Positions[i]);
                        path.Positions[i].State = PositionState.Active;

                        await MoveInterpolatedAsync(simRobot.Joints, axesPositions, durationMs, token);

                        var currentSimRobotValues = await ReadCurrentJointValuesAsync(simRobot.Joints);
                        JointValuesUpdated?.Invoke(currentSimRobotValues);

                        path.Positions[i].State = PositionState.Done;
                    }
                }
                else
                {
                    var axes = ExtractAxesPositions(targetPos);
                    targetPos.State = PositionState.Active;

                    await MoveInterpolatedAsync(simRobot.Joints, axes, durationMs, token);

                    var currentSimRobotValues = await ReadCurrentJointValuesAsync(simRobot.Joints);
                    JointValuesUpdated?.Invoke(currentSimRobotValues);

                    targetPos.State = PositionState.Done;
                }

                StatusChanged?.Invoke($"Target reached: {targetPos.Name}", Severity.Info);
                return true;
            }
            catch (OperationCanceledException)
            {
                StatusChanged?.Invoke("Movement cancelled", Severity.Info);
                return false;
            }
            finally
            {
                _movementCts?.Dispose();
                _movementCts = null;
                IsMoving = false;
            }
        }



        private double[] ExtractAxesPositions(RobotControlPosition p)
        {
            return new[]
            { p.J1, p.J2, p.J3, p.J4, p.J5, p.J6, p.J7
            }.Select(v => (double)v).ToArray();
        }





        //===========================================================================================================================
        // A X I S - C O M P O S I T I O N   S P E C I F I C
        //===========================================================================================================================

        public async Task<bool> MoveAxisCompositionToPositionAsync(AxisCompositionPositionsData targetPos, List<FeeJoint> joints, int velocityPercent)
        {
            StatusChanged?.Invoke("Moving to target...", Severity.Info);
            IsMoving = true;

            int speedPercent = Math.Clamp(velocityPercent, 1, 100);
            double durationMs = 1000 * (100.0 / speedPercent);

            _movementCts?.Cancel();
            _movementCts = new CancellationTokenSource();
            var token = _movementCts.Token;

            try
            {
                await MoveInterpolatedAsync(joints, targetPos.AxisValues, durationMs, token);

                var currentJointValues = await ReadCurrentJointValuesAsync(joints);
                JointValuesUpdated?.Invoke(currentJointValues);

                StatusChanged?.Invoke($"Target reached: {targetPos.PositionName}", Severity.Info);
                return true;
            }
            catch (OperationCanceledException)
            {
                StatusChanged?.Invoke("Movement cancelled", Severity.Info);
                return false;
            }
            finally
            {
                _movementCts?.Dispose();
                _movementCts = null;
                IsMoving = false;
            }
        }





    }
}
