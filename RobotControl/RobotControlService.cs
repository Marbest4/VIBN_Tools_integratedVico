using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;

namespace VIBN_Tools.RobotControl
{
    public class RobotControlService
    {



        public static async Task<List<RobotControlData>> ParseRobotCsvAsync(string filePath)
        {

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length == 0)
                return new List<RobotControlData>();

            // Find Header
            int headerIndex = Array.FindIndex(lines, l => l.Contains("Operation_Name") || l.Contains("Location_Name"));

            if (headerIndex < 0)
                throw new FormatException("CSV header with required columns not found.");

            var header = lines[headerIndex].Split(new[] { '\t', ';', ',' }, StringSplitOptions.None);


            // Find column indices
            int opIdx = Array.IndexOf(header, "Operation_Name");
            int locIdx = Array.IndexOf(header, "Location_Name");
            int robotIdx = opIdx - 1;

            if (opIdx < 0 || locIdx < 0)
                throw new FormatException("Required columns 'Operation_Name' or 'Location_Name' missing in header.");
            if (robotIdx < 0)
                throw new FormatException("Cannot determine robot column index based on header layout.");


            // Find joint columns
            var jointIndices = header
                .Select((col, index) => new { col, index })
                .Where(x => x.col.StartsWith("J") && int.TryParse(x.col.Substring(1), out _))
                .ToDictionary(
                    x => int.Parse(x.col.Substring(1)),
                    x => x.index);


            var robots = new Dictionary<string, RobotControlData>();


            // Parse data lines




            // Start parsing rows after the header line
            for (int l = headerIndex + 1; l < lines.Length; l++)
            {
                var fields = lines[l].Split(new[] { '\t', ';', ',' }, StringSplitOptions.None);
                if (fields.Length < locIdx) continue;

                string robotName = fields[robotIdx].Trim();
                string pathName = fields[opIdx].Trim();
                string posName = fields[locIdx].Trim();

                // Check if robot already exists 
                if (!robots.TryGetValue(robotName, out var robot))
                {
                    robot = new RobotControlData
                    {
                        RobotName = robotName,
                        Paths = new ObservableCollection<RobotControlPath>()
                    };
                    robots[robotName] = robot;
                }

                // Check if path already exists
                var path = robot.Paths.FirstOrDefault(p => p.PathName == pathName);
                if (path == null)
                {
                    path = new RobotControlPath
                    {
                        PathName = pathName,
                        Positions = new ObservableCollection<RobotControlPosition>()
                    };
                    robot.Paths.Add(path);
                }

                // Create position
                var position = new RobotControlPosition { Name = posName };

                foreach (var kv in jointIndices)
                {
                    if (kv.Value < fields.Length && float.TryParse(fields[kv.Value].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    {
                        switch (kv.Key)
                        {
                            case 1: position.J1 = value; break;
                            case 2: position.J2 = value; break;
                            case 3: position.J3 = value; break;
                            case 4: position.J4 = value; break;
                            case 5: position.J5 = value; break;
                            case 6: position.J6 = value; break;
                            case 7: position.J7 = value; break;
                        }
                    }


                }

                path.Positions.Add(position); // Preserve CSV order
            }

            return robots.Values.ToList();

        }



        public static List<SimRobotDefinition> GetSimRobots()
        {
            var resultList = new List<SimRobotDefinition>();

            var frames = Services.FeeObjects.AllFeeObjects.OfType<FeeKinematicFrame>();

            foreach (var frame in frames)
            {
                var descendants = GetObjectChildrenRecursive(frame.Guid);
                var joints = ExtractJoints(descendants);

                if (joints.Count == 0)
                    continue;

                resultList.Add(new SimRobotDefinition
                {
                    Frame = frame,
                    Joints = joints,
                });
            }

            return resultList;
        }








        /// <summary>
        /// Function gets all descendant objects starting with the guid of the root object
        /// </summary>
        /// <param name="rootGuid"></param>
        /// <returns></returns>
        private static List<FeeAbstractObject> GetObjectChildrenRecursive(Guid rootGuid)
        {
            var result = new List<FeeAbstractObject>();
            var visited = new HashSet<Guid>();

            void Traverse(Guid guid)
            {
                if (!visited.Add(guid))
                    return;

                var obj = Services.FeeObjects.AllFeeObjects.FirstOrDefault(x => x.Guid == guid);
                if (obj == null)
                    return;

                result.Add(obj);

                foreach (var childGuid in obj.ChildrenGuids)
                {
                    Traverse(childGuid);
                }
            }

            Traverse(rootGuid);
            return result;
        }


        private static List<FeeJoint> ExtractJoints(IEnumerable<FeeAbstractObject> objects)
        {
            return objects
                .OfType<FeeJoint>()
                .Where(j => j.Name.StartsWith("joint_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(j => ExtractJointIndex(j))
                .ToList();
        }

        private static int ExtractJointIndex(FeeJoint joint)
        {
            if (joint.Name.StartsWith("joint_", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(joint.Name.Substring(6), out int idx))
            {
                return idx;
            }
            return 999;
        }




















    }



    public enum PositionState { Pending, Active, Done }



    public class RobotControlData
    {
        public string RobotName { get; set; }
        public ObservableCollection<RobotControlPath> Paths { get; set; }
    }

    public class RobotControlPath
    {
        public string PathName { get; set; }
        public ObservableCollection<RobotControlPosition> Positions { get; set; }
    }

    public class RobotControlPosition : NotifyBase
    {
        public string Name { get; set; }
        public float J1 { get; set; }
        public float J2 { get; set; }
        public float J3 { get; set; }
        public float J4 { get; set; }
        public float J5 { get; set; }
        public float J6 { get; set; }
        public float J7 { get; set; }

        private PositionState _state = PositionState.Pending;
        public PositionState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

    }

    public class SimRobotDefinition
    {
        public FeeKinematicFrame Frame { get; set; }
        public List<FeeJoint> Joints { get; set; }
        //public FeeJoint Joint2 { get; set; }
        //public FeeJoint Joint3 { get; set; }
        //public FeeJoint Joint4 { get; set; }
        //public FeeJoint Joint5 { get; set; }
        //public FeeJoint Joint6 { get; set; }
        //public FeeJoint Joint7 { get; set; }

    }
}
