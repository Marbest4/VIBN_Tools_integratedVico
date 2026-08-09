namespace VIBN_Tools.CAD_Wizard
{

    //=========================================================================================================
    // J O I N T   C O D I N G S
    //=========================================================================================================
    public static class JointCodings
    {
        // PLC Axes
        public const string PlcLinearXpos = "PLINX+";
        public const string PlcLinearYpos = "PLINY+";
        public const string PlcLinearZpos = "PLINZ+";

        public const string PlcLinearXneg = "PLINX-";
        public const string PlcLinearYneg = "PLINY-";
        public const string PlcLinearZneg = "PLINZ-";

        public const string PlcRotaryXpos = "PROTX+";
        public const string PlcRotaryYpos = "PROTY+";
        public const string PlcRotaryZpos = "PROTZ+";

        public const string PlcRotaryXneg = "PROTX-";
        public const string PlcRotaryYneg = "PROTY-";
        public const string PlcRotaryZneg = "PROTZ-";

        // Cylinders
        public const string CylLinearXpos = "ZLINX+";
        public const string CylLinearYpos = "ZLINY+";
        public const string CylLinearZpos = "ZLINZ+";

        public const string CylLinearXneg = "ZLINX-";
        public const string CylLinearYneg = "ZLINY-";
        public const string CylLinearZneg = "ZLINZ-";

        public const string CylRotaryXpos = "ZROTX+";
        public const string CylRotaryYpos = "ZROTY+";
        public const string CylRotaryZpos = "ZROTZ+";

        public const string CylRotaryXneg = "ZROTX-";
        public const string CylRotaryYneg = "ZROTY-";
        public const string CylRotaryZneg = "ZROTZ-";

        // Virtual Axes (CAM Logic)
        public const string VirtLinearXpos = "VLINX+";
        public const string VirtLinearYpos = "VLINY+";
        public const string VirtLinearZpos = "VLINZ+";

        public const string VirtLinearXneg = "VLINX-";
        public const string VirtLinearYneg = "VLINY-";
        public const string VirtLinearZneg = "VLINZ-";

        public const string VirtRotaryXpos = "VROTX+";
        public const string VirtRotaryYpos = "VROTY+";
        public const string VirtRotaryZpos = "VROTZ+";

        public const string VirtRotaryXneg = "VROTX-";
        public const string VirtRotaryYneg = "VROTY-";
        public const string VirtRotaryZneg = "VROTZ-";


        public static readonly string[] AllCodings =
        {
            PlcLinearXpos, PlcLinearYpos, PlcLinearZpos,
            PlcLinearXneg, PlcLinearYneg, PlcLinearZneg,
            PlcRotaryXpos, PlcRotaryYpos, PlcRotaryZpos,
            PlcRotaryXneg, PlcRotaryYneg, PlcRotaryZneg,
            CylLinearXpos, CylLinearYpos, CylLinearZpos,
            CylLinearXneg, CylLinearYneg, CylLinearZneg,
            CylRotaryXpos, CylRotaryYpos, CylRotaryZpos,
            CylRotaryXneg, CylRotaryYneg, CylRotaryZneg,
            VirtLinearXpos, VirtLinearYpos, VirtLinearZpos,
            VirtLinearXneg, VirtLinearYneg, VirtLinearZneg,
            VirtRotaryXpos, VirtRotaryYpos, VirtRotaryZpos,
            VirtRotaryXneg, VirtRotaryYneg, VirtRotaryZneg
        };

    }





    //=========================================================================================================
    // C O N V E Y O R   C O D I N G S
    //=========================================================================================================
    public static class ConveyorCodings
    {
        public const string CodingConveyor = "CONV";

        public static readonly string[] AllCodings =
        {
           CodingConveyor,
        };

    }





    //=========================================================================================================
    // S E N S O R   C O D I N G S
    //=========================================================================================================
    public static class SensorCodings
    {
        public const string CodingSensor = "SENS";
        public const string CodingLightBeam = "BEAM";

        public static readonly string[] AllCodings =
        {
           CodingSensor, CodingLightBeam
        };

    }





    //=========================================================================================================
    // T E M P L A T E   /   D E C O   C O D I N G S
    //=========================================================================================================
    public static class TemplateDecoCodings
    {
        public const string CodingTemplate = "PART";
        public const string CodingDeco = "DECO";

        public static readonly string[] AllCodings =
        {
           CodingTemplate, CodingDeco
        };

    }








}
