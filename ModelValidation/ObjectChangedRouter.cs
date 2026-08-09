using FS.API;
using FS.SDK;
using FS.SDK.Components;
using FS.SDK.Components.Label;
using FS.SDK.Scene.Objects;
using FS.SDK.SceneObjectCommands;
using ReadingUnitPlugin.SO;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ModelValidation
{

    public class ObjectChangedRouter
    {

        private readonly Dictionary<Type, IChangeHandler> _handlers;

        public ObjectChangedRouter(IEnumerable<IChangeHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.TargetType);
        }

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            if (_handlers.TryGetValue(obj.GetType(), out var handler))
            {
                await handler.HandleChangeAsync(obj, propertyName);
            }
        }
    }








    public class FeeButtonChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeButton);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeButton
            var button = (FeeButton)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(button.FeeType, button.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeButton.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(button.Guid, nameof(SceneObject.Name), button.Name);
                    break;
                case nameof(FeeButton.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(button.Guid, nameof(SceneObject.MarkComponent.Mark), button.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeButton.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(button.Guid, nameof(ModelComponent.IsComponentActive), button.Visible, "Model");
                    break;
                case nameof(FeeButton.ButtonType):
                    await Services.ApiInstance.Object.SetPropertyAsync(button.Guid, nameof(Button.ButtonType), button.ButtonType);
                    break;
                case nameof(FeeButton.LatchingTime):
                    await Services.ApiInstance.Object.SetPropertyAsync(button.Guid, nameof(Button.LatchingTime), button.LatchingTime);
                    break;
            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeDetectionFlagChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeDetectionFlag);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeDetectionFlag
            var detectFlag = (FeeDetectionFlag)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(detectFlag.FeeType, detectFlag.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeButton.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(detectFlag.Guid, nameof(SceneObject.Name), detectFlag.Name);
                    break;
                case nameof(FeeButton.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(detectFlag.Guid, nameof(SceneObject.MarkComponent.Mark), detectFlag.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeButton.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(detectFlag.Guid, nameof(ModelComponent.IsComponentActive), detectFlag.Visible, "Model");
                    break;
            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeFloorChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeFloor);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var floor = (FeeFloor)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(floor.FeeType, floor.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeFloor.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(floor.Guid, nameof(SceneObject.Name), floor.Name);
                    break;
                case nameof(FeeFloor.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(floor.Guid, nameof(SceneObject.MarkComponent.Mark), floor.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeFloor.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(floor.Guid, nameof(ModelComponent.IsComponentActive), floor.Visible, "Model");
                    break;
                case nameof(FeeFloor.UseCollisionSlot):
                    await Services.ApiInstance.Object.SetPropertyAsync(floor.Guid, nameof(Floor.CollisionSlot), floor.UseCollisionSlot);
                    break;
            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);
        }
    }


    public class FeeInserterChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeInserter);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var inserter = (FeeInserter)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(inserter.FeeType, inserter.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeInserter.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(SceneObject.Name), inserter.Name);
                    break;
                case nameof(FeeInserter.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(SceneObject.MarkComponent.Mark), inserter.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeInserter.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(ModelComponent.IsComponentActive), inserter.Visible, "Model");
                    break;

                case nameof(FeeInserter.UseIndexInsertSlot):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(SequenceInserter.Inserter.UseIndexInsertSlot), inserter.UseIndexInsertSlot, nameof(SequenceInserter.Inserter));
                    break;
                case nameof(FeeInserter.UseDropAreaBlockedSlot):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(SequenceInserter.Inserter.UseDropAreaBlockedSlot), inserter.UseDropAreaBlockedSlot, nameof(SequenceInserter.Inserter));
                    break;
                case nameof(FeeInserter.IsInsertionEnabled):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(SequenceInserter.Inserter.IsInsertionEnabled), inserter.IsInsertionEnabled, nameof(SequenceInserter.Inserter));
                    break;
                case nameof(FeeInserter.TriggerSource):
                    await Services.ApiInstance.Object.SetPropertyAsync(inserter.Guid, nameof(SequenceInserter.Inserter.Trigger), inserter.TriggerSource, nameof(SequenceInserter.Inserter));
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeInterfaceChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeInterface);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var interfaceFee = (FeeInterface)obj;

            // Update existing interface
            await Services.ApiInstance.Interface.UpdateOrCreateInterfacePluginAsync(interfaceFee.ProviderGuid, interfaceFee.Guid, new Dictionary<string, object>
            {
                { "InterfaceName", interfaceFee.Name},
                { "IpAddress", interfaceFee.IpAddress},
                { "Port", interfaceFee.Port},
            });

        }
    }


    public class FeeInterfaceSignalChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeInterfaceSignal);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var interfaceSignal = (FeeInterfaceSignal)obj;

            // Update existing interface signals
            await Services.ApiInstance.Interface.UpdateOrCreateVariableAsync(new ApiInterfaceVariableDefinition
            {
                InterfacePluginProvider = interfaceSignal.ParentInterface.ProviderGuid,
                InterfaceGuid = interfaceSignal.ParentInterface.Guid,
                VariableGuid = interfaceSignal.Guid,
                InterfaceName = interfaceSignal.ParentInterface.Name,
                Tag = interfaceSignal.Tag,
                Address = interfaceSignal.Address,
                Path = interfaceSignal.Path,
                Comment = interfaceSignal.Comment,
                Usage = interfaceSignal.Usage,
                Type = interfaceSignal.IOType,
            });

        }
    }


    public class FeeJointChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeJoint);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var joint = (FeeJoint)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(joint.FeeType, joint.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeJoint.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(SceneObject.Name), joint.Name);
                    break;
                case nameof(FeeJoint.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(SceneObject.MarkComponent.Mark), joint.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeJoint.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(ModelComponent.IsComponentActive), joint.Visible, "Model");
                    break;

                case nameof(FeeJoint.JointType):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(MotionJoint.JointType), joint.JointType);
                    break;
                case nameof(FeeJoint.ControlType):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(JointControllerComponent.MotionSource), joint.ControlType, "Controller");
                    break;
                case nameof(FeeJoint.UseLimits):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(JointControllerComponent.UseLimits), joint.UseLimits, "Controller");
                    break;
                case nameof(FeeJoint.UseLimitIndication):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(JointControllerComponent.UseLimitIndication), joint.UseLimitIndication, "Controller");
                    break;
                case nameof(FeeJoint.ManualModeActive):
                    await Services.ApiInstance.Object.SetPropertyAsync(joint.Guid, nameof(JointControllerComponent.IsManualModeEnabled), joint.ManualModeActive, "Controller");
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeLabelChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeLabel);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var label = (FeeLabel)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(label.FeeType, label.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeLabel.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(SceneObject.Name), label.Name);
                    break;
                case nameof(FeeLabel.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(SceneObject.MarkComponent.Mark), label.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeLabel.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(ModelComponent.IsComponentActive), label.Visible, "Model");
                    break;

                case nameof(FeeLabel.Text):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(LabelComponent.Text), label.Text, "Label");
                    break;
                case nameof(FeeLabel.TextScale):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(LabelComponent.Scale), label.TextScale, "Label");
                    break;
                case nameof(FeeLabel.EnableSlot):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(LabelObject.EnableSlot), label.EnableSlot);
                    break;
                case nameof(FeeLabel.EnableFaceCamera):
                    await Services.ApiInstance.Object.SetPropertyAsync(label.Guid, nameof(LabelComponent.FaceCamera), label.EnableFaceCamera, "Label");
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeLogicChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeLogic);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var logic = (FeeLogic)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(logic.FeeType, logic.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeLogic.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(logic.Guid, nameof(SceneObject.Name), logic.Name);
                    break;
                case nameof(FeeLogic.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(logic.Guid, nameof(SceneObject.MarkComponent.Mark), logic.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeLogic.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(logic.Guid, nameof(ModelComponent.IsComponentActive), logic.Visible, "Model");
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeePickAndPlaceChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeePickAndPlace);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var pickPlace = (FeePickAndPlace)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(pickPlace.FeeType, pickPlace.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeePickAndPlace.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(SceneObject.Name), pickPlace.Name);
                    break;
                case nameof(FeePickAndPlace.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(SceneObject.MarkComponent.Mark), pickPlace.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeePickAndPlace.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(ModelComponent.IsComponentActive), pickPlace.Visible, "Model");
                    break;
                case nameof(FeePickAndPlace.PickMarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(PickAndPlaceComponent.MarkToPick), pickPlace.PickMarksString, nameof(PickAndPlaceComponent));
                    break;
                case nameof(FeePickAndPlace.DropMarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(PickAndPlaceComponent.MarkOfDropPlaces), pickPlace.DropMarksString, nameof(PickAndPlaceComponent));
                    break;
                case nameof(FeePickAndPlace.PickRange):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(PickAndPlaceComponent.MaxDistanceToPick), pickPlace.PickRange, nameof(PickAndPlaceComponent));
                    break;
                case nameof(FeePickAndPlace.DropRange):
                    await Services.ApiInstance.Object.SetPropertyAsync(pickPlace.Guid, nameof(PickAndPlaceComponent.MaxDistanceToPlace), pickPlace.DropRange, nameof(PickAndPlaceComponent));
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeReadingUnitChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeReadingUnit);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var readingUnit = (FeeReadingUnit)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(readingUnit.FeeType, readingUnit.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeReadingUnit.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(readingUnit.Guid, nameof(SceneObject.Name), readingUnit.Name);
                    break;
                case nameof(FeeReadingUnit.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(readingUnit.Guid, nameof(SceneObject.MarkComponent.Mark), readingUnit.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeReadingUnit.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(readingUnit.Guid, nameof(ModelComponent.IsComponentActive), readingUnit.Visible, "Model");
                    break;
                case nameof(FeeReadingUnit.DetectMark):
                    await Services.ApiInstance.Object.SetPropertyAsync(readingUnit.Guid, nameof(ReadingUnitUdt.DetectByMark), readingUnit.DetectMark);
                    break;
                case nameof(FeeReadingUnit.MarkToDetect):
                    await Services.ApiInstance.Object.SetPropertyAsync(readingUnit.Guid, nameof(ReadingUnitUdt.MarkToDetect), readingUnit.MarkToDetect);
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeRemoverChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeRemover);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var remover = (FeeRemover)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(remover.FeeType, remover.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeRemover.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(SceneObject.Name), remover.Name);
                    break;
                case nameof(FeeRemover.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(SceneObject.MarkComponent.Mark), remover.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeRemover.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(ModelComponent.IsComponentActive), remover.Visible, "Model");
                    break;
                case nameof(FeeRemover.DetectPayload):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.DetectPayload), remover.DetectPayload);
                    break;
                case nameof(FeeRemover.DetectDetectionFlag):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.DetectDetectionFlag), remover.DetectDetectionFlag);
                    break;
                case nameof(FeeRemover.DetectBumper):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.DetectBumper), remover.DetectBumper);
                    break;
                case nameof(FeeRemover.DetectTag):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.DetectTag), remover.DetectTag);
                    break;
                case nameof(FeeRemover.DetectMark):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.UseDetectMark), remover.DetectMark);
                    break;
                case nameof(FeeRemover.MarkToDetect):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.DetectMark), remover.MarkToDetect);
                    break;
                case nameof(FeeRemover.IsActivationSlotEnabled):
                    await Services.ApiInstance.Object.SetPropertyAsync(remover.Guid, nameof(Remover.ActivationSlot), remover.IsActivationSlotEnabled);
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeReparenterChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeReparenter);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var reparenter = (FeeReparenter)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(reparenter.FeeType, reparenter.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeReparenter.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(SceneObject.Name), reparenter.Name);
                    break;
                case nameof(FeeReparenter.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(SceneObject.MarkComponent.Mark), reparenter.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeReparenter.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(ModelComponent.IsComponentActive), reparenter.Visible, "Model");
                    break;
                case nameof(FeeReparenter.ReparentRange):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(ReparenterComponent.Range), reparenter.ReparentRange, nameof(ReparenterComponent));
                    break;
                case nameof(FeeReparenter.ChildrenMark):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(ReparenterComponent.ChildrenMark), reparenter.ChildrenMark, nameof(ReparenterComponent));
                    break;
                case nameof(FeeReparenter.ParentMark):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(ReparenterComponent.ParentMark), reparenter.ParentMark, nameof(ReparenterComponent));
                    break;
                case nameof(FeeReparenter.ReparentMode):
                    await Services.ApiInstance.Object.SetPropertyAsync(reparenter.Guid, nameof(ReparenterComponent.ReparentMode), reparenter.ReparentMode, nameof(ReparenterComponent));
                    break;
            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeSensorChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeSensor);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var sensor = (FeeSensor)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(sensor.FeeType, sensor.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeSensor.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SceneObject.Name), sensor.Name);
                    break;
                case nameof(FeeSensor.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SceneObject.MarkComponent.Mark), sensor.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeSensor.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(ModelComponent.IsComponentActive), sensor.Visible, "Model");
                    break;
                case nameof(FeeSensor.SensorType):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, "SafetySensorType", sensor.SensorType);
                    break;
                case nameof(FeeSensor.DetectPayload):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.DetectPayload), sensor.DetectPayload);
                    break;
                case nameof(FeeSensor.DetectDetectionFlag):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.DetectDetectionFlag), sensor.DetectDetectionFlag);
                    break;
                case nameof(FeeSensor.DetectBumper):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.DetectBumper), sensor.DetectBumper);
                    break;
                case nameof(FeeSensor.DetectTag):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.DetectTag), sensor.DetectTag);
                    break;
                case nameof(FeeSensor.DetectMark):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.UseDetectMark), sensor.DetectMark);
                    break;
                case nameof(FeeSensor.MarkToDetect):
                    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.DetectMark), sensor.MarkToDetect);
                    break;
                //case nameof(FeeSensor.IsDetecting):
                //    await Services.ApiInstance.Object.SetPropertyAsync(sensor.Guid, nameof(SafetySensor.IsDetecting), sensor.IsDetecting);
                //    break;


            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeSurfaceChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeSurface);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var surface = (FeeSurface)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(surface.FeeType, surface.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeSurface.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(surface.Guid, nameof(SceneObject.Name), surface.Name);
                    break;
                case nameof(FeeSurface.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(surface.Guid, nameof(SceneObject.MarkComponent.Mark), surface.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeSurface.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(surface.Guid, nameof(ModelComponent.IsComponentActive), surface.Visible, "Model");
                    break;
                case nameof(FeeSurface.StaticFriction):
                    await Services.ApiInstance.Object.SetPropertyAsync(surface.Guid, nameof(ColliderComponent.StaticFriction), surface.StaticFriction, "Collider");
                    break;
                case nameof(FeeSurface.KineticFriction):
                    await Services.ApiInstance.Object.SetPropertyAsync(surface.Guid, nameof(ColliderComponent.KineticFriction), surface.KineticFriction, "Collider");
                    break;
                case nameof(FeeSurface.ManualModeActive):
                    await Services.ApiInstance.Object.SetPropertyAsync(surface.Guid, nameof(Surface.IsManualModeEnabled), surface.ManualModeActive);
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }


    public class FeeWritingUnitChangeHandler : IChangeHandler
    {
        public Type TargetType => typeof(FeeWritingUnit);

        public async Task HandleChangeAsync(FeeAbstractObject obj, string propertyName)
        {
            // Cast to FeeFloor
            var writingUnit = (FeeWritingUnit)obj;

            // Create Instance of changed object
            Services.ApiInstance.Object.CreateObject(writingUnit.FeeType, writingUnit.Guid);

            // Set properties
            switch (propertyName)
            {
                case nameof(FeeWritingUnit.Name):
                    await Services.ApiInstance.Object.SetPropertyAsync(writingUnit.Guid, nameof(SceneObject.Name), writingUnit.Name);
                    break;
                case nameof(FeeWritingUnit.MarksString):
                    await Services.ApiInstance.Object.SetPropertyAsync(writingUnit.Guid, nameof(SceneObject.MarkComponent.Mark), writingUnit.MarksString, nameof(MarkComponent));
                    break;
                case nameof(FeeWritingUnit.Visible):
                    await Services.ApiInstance.Object.SetPropertyAsync(writingUnit.Guid, nameof(ModelComponent.IsComponentActive), writingUnit.Visible, "Model");
                    break;
                case nameof(FeeWritingUnit.DetectMark):
                    await Services.ApiInstance.Object.SetPropertyAsync(writingUnit.Guid, nameof(ReadingUnit.UseDetectMark), writingUnit.DetectMark);
                    break;
                case nameof(FeeWritingUnit.MarkToDetect):
                    await Services.ApiInstance.Object.SetPropertyAsync(writingUnit.Guid, nameof(ReadingUnit.DetectMark), writingUnit.MarkToDetect);
                    break;

            }

            // Send Property change to core
            Services.ApiInstance.Object.Send(obj.Guid);

        }
    }
}
