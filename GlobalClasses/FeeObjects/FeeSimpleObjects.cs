using FS.Components.SimulationSceneObjects.SimpleLogicObjects.Implementations.BitOperations.Bool;
using FS.Components.SimulationSceneObjects.SimpleLogicObjects.Implementations.Mover;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    #region Bool Not
    public class FeeSimpleNot : FeeAbstractObject
    {

        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSimpleNot()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(BoolNot);
            Visible = false;
        }

        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

    }
    #endregion


    #region Bool AND
    public class FeeSimpleAnd : FeeAbstractObject
    {

        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSimpleAnd()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(BoolAnd);
            Visible = false;
        }

        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

    }
    #endregion


    #region Bool OR
    public class FeeSimpleOr : FeeAbstractObject
    {

        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSimpleOr()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(BoolOr);
            Visible = false;
        }

        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

    }
    #endregion




    #region Move
    public class FeeSimpleMove : FeeAbstractObject
    {

        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeSimpleMove()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(MoveBit);
            Visible = false;
        }

        public FeeSimpleMove(string moveType)
        {
            Guid = Guid.NewGuid();
            FeeType = moveType;
            Visible = false;
        }

        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================


    }
    #endregion

}
