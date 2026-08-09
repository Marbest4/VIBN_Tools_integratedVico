using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.FeeObjects.FeeLogic;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ModelValidation
{

    public static class LogicValidation
    {
        public static readonly Dictionary<Guid, ILogicValidator> Map = new Dictionary<Guid, ILogicValidator>()
        {
            {LogicsStandard.Grob_AxisBeckhoff.PersistedGuid, new AxisBeckhoffValidator() },
            {LogicsStandard.Grob_AxisSiemens.PersistedGuid, new AxisSiemensValidator() },
            {LogicsStandard.Grob_BeltControl.PersistedGuid, new BeltControlValidator() },
            {LogicsStandard.Grob_Clamping.PersistedGuid, new ClampingValidator() },
            {LogicsStandard.Grob_Conveyor.PersistedGuid, new ConveyorValidator() },
            {LogicsStandard.Grob_Cylinder.PersistedGuid, new CylinderValidator() },
            {LogicsStandard.Grob_GripperBasic.PersistedGuid, new GripperBasicValidator() },
            {LogicsStandard.Grob_LiftUnit.PersistedGuid, new LiftUnitValidator() },
            {LogicsStandard.Grob_SafetyDoor.PersistedGuid, new SafetyDoortValidator() },
            {LogicsStandard.Grob_Stop.PersistedGuid, new StopValidator() },

        };

    }






    public class AxisBeckhoffValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var plcValueConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_AxisBeckhoff.Slots.AxisValue, out guid) && guid != Guid.Empty;
            var simValueConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_AxisBeckhoff.Slots.SimValue, out guid) && guid != Guid.Empty;

            if (!plcValueConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_AxisBeckhoff.Slots.AxisValue}' nicht verbunden", Severity.Error));

            if (!simValueConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_AxisBeckhoff.Slots.SimValue}' nicht verbunden", Severity.Error));

            return issues;
        }
    }

    public class AxisSiemensValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var plcValueConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_AxisSiemens.Slots.AxisValue, out guid) && guid != Guid.Empty;
            var simValueConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_AxisSiemens.Slots.SimValue, out guid) && guid != Guid.Empty;

            if (!plcValueConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_AxisSiemens.Slots.AxisValue}' nicht verbunden", Severity.Error));

            if (!simValueConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_AxisSiemens.Slots.SimValue}' nicht verbunden", Severity.Error));

            return issues;
        }
    }

    public class BeltControlValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject    )
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var plcValueConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_BeltControl.Slots.AxisValue, out guid) && guid != Guid.Empty;
            var stateConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_BeltControl.Slots.BeltControlState, out guid) && guid != Guid.Empty;

            if (!plcValueConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_BeltControl.Slots.AxisValue}' nicht verbunden", Severity.Error));

            if (stateConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_BeltControl.Slots.BeltControlState}' nicht verbunden", Severity.Error));

            return issues;
        }
    }

    public class ClampingValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var plcOutConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Clamping.Slots.ReleaseClamping, out guid) && guid != Guid.Empty;
            var plcInConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Clamping.Slots.ClampingReleased, out guid) && guid != Guid.Empty;

            if (!plcOutConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_Clamping.Slots.ReleaseClamping}' nicht verbunden", Severity.Error));

            if (!plcInConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_Clamping.Slots.ClampingReleased}' nicht verbunden", Severity.Error));

            return issues;
        }
    }

    public class ConveyorValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var simVelocityConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Conveyor.Slots.VelocityOut, out guid) && guid != Guid.Empty;
            var parVelocityConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Conveyor.Slots.VelocityIn, out guid) && guid != Guid.Empty;
            var clockwiseConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Conveyor.Slots.Clockwise, out guid) && guid != Guid.Empty;
            var counterclockwiseConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Conveyor.Slots.CounterClockwise, out guid) && guid != Guid.Empty;
            var controlWordConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Conveyor.Slots.ControlWord, out guid) && guid != Guid.Empty;
            var statusWordConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Conveyor.Slots.StatusWord, out guid) && guid != Guid.Empty;

            if (!simVelocityConnected || !parVelocityConnected)
                issues.Add(new PlausibilityIssue($"Velocity Slot nicht verknüpft oder parametriert", Severity.Error));

            if (!((clockwiseConnected && counterclockwiseConnected) || (controlWordConnected && statusWordConnected)))
                issues.Add(new PlausibilityIssue($"Control Slots nicht verbunden ({LogicsStandard.Grob_Conveyor.Slots.Clockwise} & {LogicsStandard.Grob_Conveyor.Slots.CounterClockwise}) oder " +
                    $"({LogicsStandard.Grob_Conveyor.Slots.ControlWord} & {LogicsStandard.Grob_Conveyor.Slots.StatusWord})", Severity.Error));

            return issues;
        }
    }

    public class CylinderValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var toHomeConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Cylinder.Slots.ToHomePos, out guid) && guid != Guid.Empty;
            var toWorkConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Cylinder.Slots.ToWorkPos, out guid) && guid != Guid.Empty;
            var inHomeConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Cylinder.Slots.InHomePos, out guid) && guid != Guid.Empty;
            var inWorkConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Cylinder.Slots.InWorkPos, out guid) && guid != Guid.Empty;
            var jointConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Cylinder.Slots.ActualPosition, out guid) && guid != Guid.Empty;

            var operationTimeSet = await SlotValidationHelper.CheckSlotValueNotZero(logicObject.Guid, LogicsStandard.Grob_Cylinder.Slots.OperationTime);
            var positionsPlausible = await SlotValidationHelper.CompareSlotValues(logicObject.Guid, LogicsStandard.Grob_Cylinder.Slots.HomePos, LogicsStandard.Grob_Cylinder.Slots.WorkPos, SlotValidationHelper.SlotValueCompare.Different);

            if (!toHomeConnected && !toWorkConnected)
                issues.Add(new PlausibilityIssue($"Steuer Slots nicht verbunden ({LogicsStandard.Grob_Cylinder.Slots.ToHomePos} und/oder {LogicsStandard.Grob_Cylinder.Slots.ToWorkPos})", Severity.Error));

            if (!inHomeConnected && !inWorkConnected)
                issues.Add(new PlausibilityIssue($"Status Slots nicht verbunden ({LogicsStandard.Grob_Cylinder.Slots.InHomePos} und/oder {LogicsStandard.Grob_Cylinder.Slots.InWorkPos})", Severity.Error));

            if (!operationTimeSet)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_Cylinder.Slots.OperationTime}' hat keinen Wert parametriert", Severity.Error));

            if (!positionsPlausible && jointConnected)
                issues.Add(new PlausibilityIssue($"Slots '{LogicsStandard.Grob_Cylinder.Slots.HomePos}' und '{LogicsStandard.Grob_Cylinder.Slots.WorkPos}' haben gleichen Wert", Severity.Error));

            if(!jointConnected)
                issues.Add(new PlausibilityIssue($"Kein MotionJoint verknüpft", Severity.Warning));

            return issues;

        }
    }

    public class GripperBasicValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var clampConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_GripperBasic.Slots.Clamp, out guid) && guid != Guid.Empty;
            var unclampConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_GripperBasic.Slots.Unclamp, out guid) && guid != Guid.Empty;
            var clampedConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_GripperBasic.Slots.Clamped, out guid) && guid != Guid.Empty;
            var unclampedConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_GripperBasic.Slots.Unclamped, out guid) && guid != Guid.Empty;

            var operationTimeSet = await SlotValidationHelper.CheckSlotValueNotZero(logicObject.Guid, LogicsStandard.Grob_GripperBasic.Slots.OperationTime);
            var positionsPlausible = await SlotValidationHelper.CompareSlotValues(logicObject.Guid, LogicsStandard.Grob_GripperBasic.Slots.ClampedPos, LogicsStandard.Grob_GripperBasic.Slots.UnclampedPos, SlotValidationHelper.SlotValueCompare.Different);

            if (!clampConnected && !unclampConnected)
                issues.Add(new PlausibilityIssue($"Control Slots nicht verbunden", Severity.Error));
            else if(!clampConnected || !unclampConnected)
                issues.Add(new PlausibilityIssue($"Control Slots nicht verbunden", Severity.Warning));

            if (!clampedConnected && !unclampedConnected)
                issues.Add(new PlausibilityIssue($"Status Slots nicht verbunden", Severity.Error));
            else if (!clampedConnected ^ !unclampedConnected)
                issues.Add(new PlausibilityIssue($"Ein Status Slot nicht verbunden", Severity.Warning));

            if (!operationTimeSet)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_GripperBasic.Slots.OperationTime}' nicht verbunden", Severity.Error));

            if (!positionsPlausible)
                issues.Add(new PlausibilityIssue($"Slots '{LogicsStandard.Grob_GripperBasic.Slots.ClampedPos}' und '{LogicsStandard.Grob_GripperBasic.Slots.UnclampedPos}' haben gleichen Wert", Severity.Error));

            return issues;

        }
    }

    public class LiftUnitValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var toHomeConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_LiftUnit.Slots.ToHomePos, out guid) && guid != Guid.Empty;
            var toWorkConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_LiftUnit.Slots.ToWorkPos, out guid) && guid != Guid.Empty;
            var inHomeConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_LiftUnit.Slots.InHomePos, out guid) && guid != Guid.Empty;
            var inWorkConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_LiftUnit.Slots.InWorkPos, out guid) && guid != Guid.Empty;
            var middlePosConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_LiftUnit.Slots.InMiddlePos, out guid) && guid != Guid.Empty;
            var middlePosNotZero = await SlotValidationHelper.CheckSlotValueNotFalse(logicObject.Guid, LogicsStandard.Grob_LiftUnit.Slots.EnableMiddlePos);

            var operationTimeSet = await SlotValidationHelper.CheckSlotValueNotZero(logicObject.Guid, LogicsStandard.Grob_LiftUnit.Slots.OperationTime);
            var positionsPlausible = await SlotValidationHelper.CompareSlotValues(logicObject.Guid, LogicsStandard.Grob_LiftUnit.Slots.HomePos, LogicsStandard.Grob_Cylinder.Slots.WorkPos, SlotValidationHelper.SlotValueCompare.Different);

            if (!toHomeConnected || !toWorkConnected)
                issues.Add(new PlausibilityIssue($"Control Slots nicht verbunden ({LogicsStandard.Grob_LiftUnit.Slots.ToHomePos} und/oder {LogicsStandard.Grob_LiftUnit.Slots.ToWorkPos})", Severity.Error));

            if (!inHomeConnected || !inWorkConnected)
                issues.Add(new PlausibilityIssue($"Status Slots nicht verbunden ({LogicsStandard.Grob_LiftUnit.Slots.InHomePos} und/oder {LogicsStandard.Grob_LiftUnit.Slots.InWorkPos})", Severity.Error));

            if (!operationTimeSet)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_LiftUnit.Slots.OperationTime}' nicht verbunden", Severity.Error));

            if (!positionsPlausible)
                issues.Add(new PlausibilityIssue($"Slots '{LogicsStandard.Grob_LiftUnit.Slots.HomePos}' und '{LogicsStandard.Grob_LiftUnit.Slots.WorkPos}' haben gleichen Wert", Severity.Error));

            if (middlePosNotZero && !middlePosConnected)
                issues.Add(new PlausibilityIssue($"MiddlePos aktiviert aber Slot '{LogicsStandard.Grob_LiftUnit.Slots.InMiddlePos}' nicht verbunden", Severity.Error));

            return issues;

        }
    }

    public class SafetyDoortValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var unlockConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.Unlock, out guid) && guid != Guid.Empty;
            var unlockedConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.Unlocked, out guid) && guid != Guid.Empty;
            var closed1Connected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.Closed_Ch1, out guid) && guid != Guid.Empty;
            var closed2Connected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.Closed_Ch2, out guid) && guid != Guid.Empty;
            var closedlocked1Connected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked, out guid) && guid != Guid.Empty;
            var closedlocked2Connected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked_Ch1, out guid) && guid != Guid.Empty;
            var closedlocked3Connected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_SafetyDoor.Slots.ClosedAndLocked_Ch2, out guid) && guid != Guid.Empty;

            if (!unlockConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_SafetyDoor.Slots.Unlock}' nicht verbunden", Severity.Error));

            if (!unlockedConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_SafetyDoor.Slots.Unlocked}' nicht verbunden", Severity.Error));

            if (!closed1Connected && !closed2Connected)
                issues.Add(new PlausibilityIssue($"Slot 'Closed' nicht verbunden", Severity.Error));

            if (!closedlocked1Connected && !closedlocked2Connected && !closedlocked3Connected)
                issues.Add(new PlausibilityIssue($"Slot 'ClosedAndLocked' nicht verbunden", Severity.Error));

            return issues;

        }
    }

    public class StopValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();

            Guid guid;
            var openConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Stop.Slots.Open, out guid) && guid != Guid.Empty;
            var closeConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Stop.Slots.Close, out guid) && guid != Guid.Empty;
            var openedConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Stop.Slots.Open, out guid) && guid != Guid.Empty;
            var closedConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Stop.Slots.Close, out guid) && guid != Guid.Empty;
            var collisionConnected = logicObject.Slots.TryGetValue(LogicsStandard.Grob_Stop.Slots.Collision, out guid) && guid != Guid.Empty;

            if (!openConnected && !closeConnected)
                issues.Add(new PlausibilityIssue($"Control Slots nicht verbunden ({LogicsStandard.Grob_Stop.Slots.Open} und/oder ({LogicsStandard.Grob_Stop.Slots.Close})", Severity.Error));

            if (!openedConnected && !closedConnected)
                issues.Add(new PlausibilityIssue($"Status Slots nicht verbunden ({LogicsStandard.Grob_Stop.Slots.Opened} und/oder ({LogicsStandard.Grob_Stop.Slots.Closed})", Severity.Error));

            if (!collisionConnected)
                issues.Add(new PlausibilityIssue($"Slot '{LogicsStandard.Grob_Stop.Slots.Collision}' nicht verbunden", Severity.Error));

            return issues;

        }
    }


    public class GenericLogicValidator : ILogicValidator
    {
        public async Task<IEnumerable<PlausibilityIssue>> ValidateAsync(FeeLogic logicObject)
        {
            var issues = new List<PlausibilityIssue>();
            if (logicObject.LogicDefinitionGuid == Guid.Empty)
                issues.Add(new PlausibilityIssue($"Keine Logik-Definition verbunden", Severity.Error));

            return issues;
        }
    }





}
