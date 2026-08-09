using FS.SDK;
using FS.SDK.Mathematics;
using VIBN_Tools.CAD_Wizard;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeDecoration : FeeAbstractObject, ICadWizardCreatable<FeeDecoration>
    {
        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================

        // Needed for CAD Wizard
        public string Coding { get; set; }
        public Guid CadDecoGuid { get; set; }



        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeDecoration()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(Decoration);
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





        //===================================================================================================================
        // M E T H O D S   ( C A D - W I Z A R D )
        //===================================================================================================================


        public static FeeDecoration? CadWizardFactory(string name, Vector3 position, Vector3 rotation, Guid cadDecoGuid)
        {
            foreach (var coding in TemplateDecoCodings.AllCodings)
            {
                if (name.StartsWith(coding))
                {
                    var template = new FeeDecoration()
                    {
                        Name = name.Substring(coding.Length + 1),
                        Coding = coding,
                        Position = position,
                        Rotation = rotation,
                        CadDecoGuid = cadDecoGuid,
                    };

                    return template;
                }
            }
            return null;
        }


        public async Task<bool> CadWizardCreateAndSendAsync()
        {
            // Create Object
            Services.ApiInstance.Object.CreateObject(nameof(Decoration), CadDecoGuid);

            // Set Properties
            await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, nameof(SceneObject.Name), Name);
            await Services.ApiInstance.Object.SetPropertyAsync(CadDecoGuid, "IsComponentActive", true, "Model");

            Services.ApiInstance.Object.Send(CadDecoGuid);
            await Services.ApiInstance.Object.WaitForSceneObjectAsync(CadDecoGuid.ToString());

            // Exchange Guid with the old CadDecoGuid, because object was replaced
            this.Guid = CadDecoGuid;

            return true;
        }

    }
}
