using FS.SDK.Scene.Objects;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.RobotControl
{
    public class RobotMotionService
    {


        public event Action<string, Severity> StatusChanged;
        public event Action<double[]> SimRobotValuesUpdated;
        public event Action<bool> MovingStateChanged;

        private CancellationTokenSource _movementCts;


        private bool _robotIsMoving;
        public bool RobotIsMoving
        {
            get => _robotIsMoving;
            private set
            {
                _robotIsMoving = value;
                MovingStateChanged?.Invoke(value);
            }
        }




        public async Task<bool> MoveToPositionAsync(RobotControlData robot, RobotControlPath path, RobotControlPosition targetPos, SimRobotDefinition simRobot, int velocityPercent, bool driveSinglePosition)
        {
            // Validation
            if (robot == null)
            {
                StatusChanged?.Invoke("No physical robot selected.", Severity.Error);
                return false;
            }

            if (path == null)
            {
                StatusChanged?.Invoke("No path selected.", Severity.Error);
                return false;
            }

            if (targetPos == null)
            {
                StatusChanged?.Invoke("No target position selected.", Severity.Warning);
                return false;
            }

            if (simRobot == null)
            {
                StatusChanged?.Invoke("No simulation robot selected.", Severity.Error);
                return false;
            }

            if (velocityPercent <= 0)
            {
                StatusChanged?.Invoke("Velocity is 0%. Set a value > 0.", Severity.Warning);
                return false;
            }

            StatusChanged?.Invoke("Moving to target...", Severity.Info);
            RobotIsMoving = true;

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
                        var axes = ExtractAxes(path.Positions[i]);
                        path.Positions[i].State = PositionState.Active;

                        await MoveInterpolatedAsync(simRobot, axes, durationMs, token);

                        var currentSimRobotValues = await ReadCurrentSimRobotValuesAsync(simRobot);
                        SimRobotValuesUpdated?.Invoke(currentSimRobotValues);

                        path.Positions[i].State = PositionState.Done;
                    }
                }
                else
                {
                    var axes = ExtractAxes(targetPos);
                    targetPos.State = PositionState.Active;

                    await MoveInterpolatedAsync(simRobot, axes, durationMs, token);

                    var currentSimRobotValues = await ReadCurrentSimRobotValuesAsync(simRobot);
                    SimRobotValuesUpdated?.Invoke(currentSimRobotValues);

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
                RobotIsMoving = false;
            }
        }




        private double[] ExtractAxes(RobotControlPosition p)
        {
            return new[]
            { p.J1, p.J2, p.J3, p.J4, p.J5, p.J6, p.J7
            }.Select(v => (double)v).ToArray();
        }



        private async Task MoveInterpolatedAsync(SimRobotDefinition simRobot, double[] targetAxes, double durationMs, CancellationToken token)
        {
            if (simRobot == null)
                throw new ArgumentNullException(nameof(simRobot));


            var joints = simRobot.Joints;
            int axisCount = Math.Min(joints.Count, targetAxes.Length);

            var currentSimRobotValues = await ReadCurrentSimRobotValuesAsync(simRobot);

            // Check if already at tragetPos
            bool alreadyAtTarget = currentSimRobotValues
                    .Take(axisCount)
                    .Select(v => Math.Round(v, 3))
                    .SequenceEqual(targetAxes.Take(axisCount).Select(v => Math.Round(v, 3)));

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
                    interpolatedValues[i] = currentSimRobotValues[i] + (targetAxes[i] - currentSimRobotValues[i]) * factor;
                }

                // Write values to simulation
                await Services.ApiInstance.Object.SetSlotValuesAsync(
                    joints.Take(axisCount).Select(j => j.Guid).ToArray(),
                    Enumerable.Repeat("InValue", axisCount).ToArray(),
                    interpolatedValues.Take(axisCount).Select(v => (object)v).ToArray()
                    );


                // Update view
                SimRobotValuesUpdated?.Invoke(interpolatedValues.ToArray());
                await Task.Delay(stepDelay, token);

            }

        }



        public static async Task<double[]> ReadCurrentSimRobotValuesAsync(SimRobotDefinition simRobot)
        {
            var joints = simRobot.Joints;
            var values = new double[joints.Count];

            for (int i = 0; i < joints.Count; i++)
            {
                var guid = joints[i].Guid;

                var slotValue = await Services.ApiInstance.Object.GetSlotValueAsync(guid, nameof(MotionJoint.InValue));

                values[i] = Services.ApiInstance.XmlHelper.ConvertToFloat(slotValue);
            }

            return values;

        }


        public void Cancel()
        {
            _movementCts.Cancel();
        }


    }
}
