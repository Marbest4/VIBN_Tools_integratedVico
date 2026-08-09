using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeCabinet : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeCabinet()
        {
            Guid = Guid.NewGuid();
            FeeType = "Cabinet";
            Visible = false;
        }







        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            throw new NotImplementedException();
        }




    }
}
