using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee
{
    public class LogicSimObjectContainerFactory : IContainerFactory
    {
        public async Task CreateContainerAsync(ContainerBaseClass container, FeeInterface targetInterface, FeeAbstractObject parentObject)
        {
            if (container is not ILogicSimObjectOwner fullContainer)
            {
                throw new InvalidOperationException("Container does not implement Logic and SimObject");
            }

            // Always same process
            await fullContainer.CreateLogicAsync(parentObject);
            await fullContainer.AssignSignalsAsync(targetInterface);
            await fullContainer.CreateSimObjectsAsync();
            await fullContainer.AssignSimObjectsAsync();
        }
    }



    public class LogicContainerFactory : IContainerFactory
    {
        public async Task CreateContainerAsync(ContainerBaseClass container, FeeInterface targetInterface, FeeAbstractObject parentObject)
        {
            if (container is not ILogicOwner logicContainer)
            {
                throw new InvalidOperationException("Container does not implement Logic");
            }

            // Always same process
            await logicContainer.CreateLogicAsync(parentObject);
            await logicContainer.AssignSignalsAsync(targetInterface);
        }
    }



    public class SimObjectContainerFactory : IContainerFactory
    {
        public async Task CreateContainerAsync(ContainerBaseClass container, FeeInterface targetInterface, FeeAbstractObject parentObject)
        {
            if (container is not ISimObjectOwner soContainer)
            {
                throw new InvalidOperationException("Container does not implement SimObject");
            }

            // Always same process
            await soContainer.CreateSimObjectsAsync(parentObject);
            await soContainer.AssignSignalsAsync(targetInterface);
        }
    }

    public class CabinetElementContainerFactory : IContainerFactory
    {
        private readonly CabinetContainerManager _cabinetContainerManager;

        public CabinetElementContainerFactory(CabinetContainerManager cabinetContainerManager)
        {
            _cabinetContainerManager = cabinetContainerManager;
        }

        public async Task CreateContainerAsync(ContainerBaseClass container, FeeInterface targetInterface, FeeAbstractObject parentObject)
        {
            if (container is not ICabinetElementOwner cabinetElement)
            {
                throw new InvalidOperationException("Container does not implement CabinetElement");
            }

            // Get or Create Cabinet for this type of CabinetElements, e.g. "Cabinet Switches"
            var cabinet = await _cabinetContainerManager.GetOrCreateCabinetAsync(cabinetElement.CabinetName, parentObject);

            // Set Property for current Position to place the CabinetElement
            cabinetElement.ElementPosition = _cabinetContainerManager.GetNextPosition(cabinetElement.CabinetName);

            // Create CabinetElement
            await cabinetElement.CreateSimObjectsAsync(cabinet);

            // Assign Signals to CabinetElement
            await cabinetElement.AssignSignalsAsync(targetInterface);
        }
    }
}
