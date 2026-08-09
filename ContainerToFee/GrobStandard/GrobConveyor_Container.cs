using FS.SDK;
using FS.SDK.Mathematics;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee.GrobStandard
{

    public class GrobConveyor_Container : ContainerBaseClass, ISimObjectFindOrSelect, ILogicSimObjectOwner
    {

        public GrobConveyor_Container()
        {
            SlotAssignment = new Dictionary<string, PropertyInfo>()
            {
                {LogicsStandard.Grob_Conveyor.Slots.ControlWord, typeof(GrobConveyor_Container).GetProperty("Signal_ControlWord") },
                {LogicsStandard.Grob_Conveyor.Slots.StatusWord, typeof(GrobConveyor_Container).GetProperty("Signal_StatusWord") },
                {LogicsStandard.Grob_Conveyor.Slots.Clockwise, typeof(GrobConveyor_Container).GetProperty("Signal_Clockwise") },
                {LogicsStandard.Grob_Conveyor.Slots.CounterClockwise, typeof(GrobConveyor_Container).GetProperty("Signal_CounterClockwise") },

                {LogicsStandard.Grob_Conveyor.Slots.Speed, typeof(GrobConveyor_Container).GetProperty("Signal_Speed") },
                {LogicsStandard.Grob_Conveyor.Slots.SlowSpeed, typeof(GrobConveyor_Container).GetProperty("Signal_SlowSpeed") },

                {LogicsStandard.Grob_Conveyor.Slots.PowerSupplyTurnedOn, typeof(GrobConveyor_Container).GetProperty("Signal_PowerSupplyTurnedOn") },
                {LogicsStandard.Grob_Conveyor.Slots.TurnOff, typeof(GrobConveyor_Container).GetProperty("Signal_TurnOff") },
                {LogicsStandard.Grob_Conveyor.Slots.ReadyForOperation, typeof(GrobConveyor_Container).GetProperty("Signal_ReadyForOperation") },
                {LogicsStandard.Grob_Conveyor.Slots.ConveyorActive, typeof(GrobConveyor_Container).GetProperty("Signal_ConveyorActive") },
                {LogicsStandard.Grob_Conveyor.Slots.ConveyorOk, typeof(GrobConveyor_Container).GetProperty("Signal_ConveyorOk") },

                {LogicsStandard.Grob_Conveyor.Slots.AckFault, typeof(GrobConveyor_Container).GetProperty("Signal_AckFault") },
                {LogicsStandard.Grob_Conveyor.Slots.Warning, typeof(GrobConveyor_Container).GetProperty("Signal_Warning") },
                {LogicsStandard.Grob_Conveyor.Slots.Error, typeof(GrobConveyor_Container).GetProperty("Signal_Error") },
            };
        }



        public FeeLogic Logic_Conveyor { get; set; }

        public FeeInterfaceSignal Signal_ControlWord { get; set; }
        public FeeInterfaceSignal Signal_StatusWord { get; set; }
        public FeeInterfaceSignal Signal_Clockwise { get; set; }
        public FeeInterfaceSignal Signal_CounterClockwise { get; set; }

        public FeeInterfaceSignal Signal_Speed { get; set; }
        public FeeInterfaceSignal Signal_SlowSpeed { get; set; }
        public FeeInterfaceSignal Signal_PowerSupplyTurnedOn { get; set; }
        public FeeInterfaceSignal Signal_TurnOff { get; set; }

        public FeeInterfaceSignal Signal_ReadyForOperation { get; set; }
        public FeeInterfaceSignal Signal_ConveyorActive { get; set; }
        public FeeInterfaceSignal Signal_ConveyorOk { get; set; }

        public FeeInterfaceSignal Signal_AckFault { get; set; }
        public FeeInterfaceSignal Signal_Warning { get; set; }
        public FeeInterfaceSignal Signal_Error { get; set; }

        public List<FeeSurface> Surfaces_Conveyor { get; set; } = new List<FeeSurface>();

        public float Parameter_Velocity { get; set; } = -1f;

        public bool IsCreationRequested { get; set; }




        void ISimObjectFindOrSelect.FindSimObjects(ObservableCollection<FeeAbstractObject> mappableSimObjects)
        {
            Surfaces_Conveyor = FindSimObjectsByNameAndType<FeeSurface>(mappableSimObjects);
        }

        IEnumerable<SimObjectTarget> ISimObjectFindOrSelect.GetSimObjectTargets()
        {
            yield return new SimObjectTarget()
            {
                DisplayName = "Surfaces",

                AllowedType = typeof(FeeSurface),

                AllowMultiSelect = true,

                GetObjects = () => Surfaces_Conveyor,

                AssignObjects = objects =>
                {
                    Surfaces_Conveyor = objects.OfType<FeeSurface>().ToList();
                }
            };
        }



        async Task<FeeLogic> ILogicSimObjectOwner.CreateLogicAsync(FeeAbstractObject parentObject)
        {
            Logic_Conveyor = new FeeLogic()
            {
                Name = this.ComponentName,
                LogicDefinitionName = LogicsStandard.Grob_Conveyor.Name,
                LogicDefinitionPath = LogicsStandard.Grob_Conveyor.Path,
                Parent = parentObject,
            };

            (Logic_Conveyor.LogicDefinitionGuid, Logic_Conveyor.LogicDefinitionVersion) = await GetOrImportLogicDefinition(Logic_Conveyor.LogicDefinitionName, Logic_Conveyor.LogicDefinitionPath);
            await Logic_Conveyor.CreateSendAssignAndWaitAsync();

            return Logic_Conveyor;
        }

        async Task ILogicSimObjectOwner.AssignSignalsAsync(FeeInterface targetInterface)
        {
            var mappings = new (FeeInterfaceSignal Signal, string SlotName)[]
            {
                (Signal_ControlWord, LogicsStandard.Grob_Conveyor.Slots.ControlWord),
                (Signal_StatusWord, LogicsStandard.Grob_Conveyor.Slots.StatusWord),
                (Signal_Clockwise, LogicsStandard.Grob_Conveyor.Slots.Clockwise),
                (Signal_CounterClockwise, LogicsStandard.Grob_Conveyor.Slots.CounterClockwise),
                (Signal_Speed, LogicsStandard.Grob_Conveyor.Slots.Speed),
                (Signal_SlowSpeed, LogicsStandard.Grob_Conveyor.Slots.SlowSpeed),
                (Signal_PowerSupplyTurnedOn, LogicsStandard.Grob_Conveyor.Slots.PowerSupplyTurnedOn),
                (Signal_TurnOff, LogicsStandard.Grob_Conveyor.Slots.TurnOff),
                (Signal_ReadyForOperation, LogicsStandard.Grob_Conveyor.Slots.ReadyForOperation),
                (Signal_ConveyorActive, LogicsStandard.Grob_Conveyor.Slots.ConveyorActive),
                (Signal_ConveyorOk, LogicsStandard.Grob_Conveyor.Slots.ConveyorOk),
                (Signal_AckFault, LogicsStandard.Grob_Conveyor.Slots.AckFault),
                (Signal_Warning, LogicsStandard.Grob_Conveyor.Slots.Warning),
                (Signal_Error, LogicsStandard.Grob_Conveyor.Slots.Error),
            };

            foreach (var (signal, slotname) in mappings)
            {
                if (signal != null)
                {
                    await signal.CreateSignalAsync(targetInterface);
                    await Services.ApiInstance.Interface.SendSlotVarAssignmentAsync(Logic_Conveyor.Guid, slotname, signal.Guid, true);
                }
            }
        }

        async Task ILogicSimObjectOwner.CreateSimObjectsAsync()
        {
            if (!Surfaces_Conveyor.Any() && IsCreationRequested)
            {
                var surface = new FeeSurface()
                {
                    Name = this.ComponentName,
                    Parent = Logic_Conveyor,
                    Position = new Vector3(0, 0, 0),
                    Scale = new Vector3(2f, 0.5f, 0.05f),
                };

                await surface.CreateAsync();
                await surface.SendAndWaitAsync();
                Surfaces_Conveyor.Add(surface);
            }
        }

        async Task ILogicSimObjectOwner.AssignSimObjectsAsync()
        {
            if (Surfaces_Conveyor.Any())
            {
                // Lists with slot assignments for later assignment
                var slotsToAssignVelocity = new List<(Guid, string)>() { (Logic_Conveyor.Guid, LogicsStandard.Grob_Conveyor.Slots.VelocityOut) };

                foreach (var surface in Surfaces_Conveyor)
                {
                    slotsToAssignVelocity.Add((surface.Guid, "InVelocityX"));

                    // Add label to surface
                    var positionSurfaceXml = await Services.ApiInstance.Object.GetPropertyAsync(surface.Guid, nameof(SceneObject.Transform.Position), nameof(SceneObject.Transform));
                    var rotationSurfaceXml = await Services.ApiInstance.Object.GetPropertyAsync(surface.Guid, nameof(SceneObject.Transform.Rotation), nameof(SceneObject.Transform));
                    Vector3 positionSurface = Services.ApiInstance.XmlHelper.ConvertToVector3(positionSurfaceXml);
                    Vector3 rotationSurface = Services.ApiInstance.XmlHelper.ConvertToVector3(rotationSurfaceXml);
                    FeeLabel surfaceLabel = new FeeLabel()
                    {
                        Name = this.ComponentName,
                        Parent = surface,
                        Position = new Vector3(positionSurface.X, positionSurface.Y, positionSurface.Z + 0.0255f),    //new Vector3(0, 0, 0.0255f),
                        Rotation = GenerateLabelTextAndRotation(rotationSurface).Item2,
                        Text = GenerateLabelTextAndRotation(rotationSurface).Item1,
                        TextScale = 0.1f,
                        TextPosition = new Vector3(0f, 0f, 0f),
                        TextRotation = Matrix.RotationQuaternion(Quaternion.FromRollPitchYawDegrees(new Vector3(-90f, 0f, 0f))),  // Defines.FeeRotation_X_neg,
                        EnableFaceCamera = false,
                        TextColor = Color.Black,
                        BackgroundColor = Color.White,
                    };

                    await surfaceLabel.CreateAsync();
                    await surfaceLabel.SendAndWaitAsync();
                }

                // Assign all slots parallel
                await Services.ApiInstance.Interface.SendMultipleSlotSlotAssignmentsAsync(slotsToAssignVelocity.Select(x => x.Item1).ToArray(), slotsToAssignVelocity.Select(x => x.Item2).ToArray());

            }
        }


















        /// <summary>
        /// Helper Function to set Label Text and Rotation. Goal is to have only 2 viewing angles to read the label
        /// </summary>
        /// <param name="rotationSurface"></param>
        /// <returns></returns>
        private (string, Vector3) GenerateLabelTextAndRotation(Vector3 rotationSurface)
        {
            string text = string.Empty;
            Vector3 rotation = rotationSurface;

            const float TOL = 0.001f;

            // Normalize Rotation Z value to 180°
            static float NormalizeTo180Deg(float deg) => ((deg + 180f) % 360f + 360f) % 360f - 180f;

            // Check if value is near target value, e.g. Rotation value is 90.0000001 instead of 90
            static bool NearAngle(float currentValue, float targetValue, float eps = TOL) => Math.Abs(NormalizeTo180Deg(currentValue - targetValue)) <= eps;

            // Create flags for Rotations
            bool is90 = NearAngle(rotationSurface.Z, 90f);
            bool is180 = NearAngle(rotationSurface.Z, 180f);
            bool is270 = NearAngle(rotationSurface.Z, 270f);

            // Build Name
            string name = this.ComponentName ?? string.Empty;
            int spaceIndex = name.IndexOf(' ');
            name = (spaceIndex > 0) ? name.Substring(0, spaceIndex) : name;

            // Set arrow direction
            string arrowDirection = (is180 || is270) ? "<<" : ">>";
            float rotZ = (is90 || is270) ? 90f :
                          is180 ? 0f :
                          rotationSurface.Z;

            text = $"{arrowDirection} {name} {arrowDirection}";
            return (text, new Vector3(0f, 0f, rotZ));

        }


    }
}
