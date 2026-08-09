using FS.SDK.Scene.Objects;
using System.Xml.Linq;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeKinematicFrame : FeeAbstractObject
    {
        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeKinematicFrame()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(KinematicFrame);
            Visible = true;
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);
        }


    }
}
