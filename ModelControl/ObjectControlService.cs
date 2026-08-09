using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FS.SDK.Scene.Objects;
using VIBN_Tools.GlobalClasses.FeeObjects;

namespace VIBN_Tools.ModelControl
{
    public class ObjectControlService
    {
    }










    public class ObjectControlTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ButtonControlTemplate { get; set; }
        public DataTemplate ConveyorControlTemplate { get; set; }
        public DataTemplate JointControlTemplate { get; set; }
        public DataTemplate PickAndPlaceControlTemplate { get; set; }
        public DataTemplate ReparentControlTemplate { get; set; }
        public DataTemplate SensorControlTemplate { get; set; }
        public DataTemplate StopperControlTemplate { get; set; }



        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FeeButton) return ButtonControlTemplate;
            if (item is FeeJoint) return JointControlTemplate;
            if (item is FeePickAndPlace) return PickAndPlaceControlTemplate;
            if (item is FeeReparenter) return ReparentControlTemplate;
            if (item is FeeFloor) return StopperControlTemplate;
            if (item is FeeSensor) return SensorControlTemplate;
            if (item is FeeSurface) return ConveyorControlTemplate;


            //if(item is FeeAbstractObject fee)
            //{
            //    return fee.TypeName switch
            //    {
            //        nameof(FeeButton) => ButtonControlTemplate,

            //        _ => null,
            //    };
            //}


            return base.SelectTemplate(item, container);
        }
    }
}
