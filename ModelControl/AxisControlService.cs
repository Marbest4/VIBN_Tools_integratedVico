using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIBN_Tools.ModelControl
{
    public class AxisControlService
    {

        public static List<AxisCompositionData> ParseAxisCompositionData(string fileName)
        {
            var lines = File.ReadAllLines(fileName);

            var compositions = new Dictionary<string, AxisCompositionData>();

            foreach (var line in lines.Skip(1)) // Skip Header
            {
                if(string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                var compName = parts[0].Trim();
                var posName = parts[1].Trim();

                if(string.IsNullOrEmpty(compName) ) continue;

                var axisValues = parts.Skip(2)
                                      .Where(p => !string.IsNullOrWhiteSpace(p))
                                      .Select(double.Parse)
                                      .ToArray();

                if (!compositions.TryGetValue(compName, out var comp))
                {
                    comp = new AxisCompositionData
                    {
                        CompositionName = compName,
                        PositionsData = new List<AxisCompositionPositionsData>()
                    };
                    compositions.Add(compName, comp);
                }

                comp.PositionsData.Add(new AxisCompositionPositionsData
                {
                    PositionName = posName,
                    AxisValues = axisValues
                });
            }

            return compositions.Values.ToList();

        }

    }






    public class AxisCompositionData
    {
        public string CompositionName { get; set; }
        public List<AxisCompositionPositionsData> PositionsData { get; set; }
    }


    public class AxisCompositionPositionsData
    {
        public string PositionName { get; set; }
        public double[] AxisValues { get; set; }
    }


}
