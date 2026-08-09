using System.Numerics;
using VIBN_Tools.GlobalClasses.FeeObjects;

namespace VIBN_Tools.ContainerToFee
{
    public class CabinetContainerManager
    {
        private readonly Dictionary<string, FeeAbstractObject> _cabinets = new Dictionary<string, FeeAbstractObject>();
        private readonly Dictionary<string, Vector2> _elementPositions = new Dictionary<string, Vector2>();


        public async Task<FeeAbstractObject> GetOrCreateCabinetAsync(string cabinetType, FeeAbstractObject parentObject)
        {
            if (!_cabinets.TryGetValue(cabinetType, out var cabinet))
            {
                // Create Cabinet if not existing
                cabinet = new FeeCabinet
                {
                    Name = cabinetType,
                    Parent = parentObject,
                };
                await cabinet.CreateAsync();
                await cabinet.SendAndWaitAsync();
                _cabinets[cabinetType] = cabinet;

                // Set Startingposition for this cabinet
                _elementPositions[cabinetType] = new Vector2(-20f, 100f);
            }

            return cabinet;
        }



        public Vector2 GetNextPosition(string cabinetType)
        {
            Vector2 currentPosition = _elementPositions[cabinetType];
            Vector2 nextPosition;

            if (currentPosition.X >= 1420)
            {
                nextPosition = new Vector2(100, currentPosition.Y + 200);
            }
            else
            {
                nextPosition = new Vector2(currentPosition.X + 120, currentPosition.Y);
            }

            _elementPositions[cabinetType] = nextPosition;
            return nextPosition;
        }
    }
}
