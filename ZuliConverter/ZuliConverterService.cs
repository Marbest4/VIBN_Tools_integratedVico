using NPOI.SS.UserModel;
using VIBN_Tools.GlobalClasses;
using static VIBN_Tools.GlobalClasses.Interfaces;



namespace VIBN_Tools.ZuliConverter
{
    public class ZuliConverterService
    {

        private static readonly List<IZuliTypeDefinitionBase> Definitions = new List<IZuliTypeDefinitionBase>()
        {
            new BeckhoffZuliDefinition(),
            new SiemensZuliDefinition(),
            new TiaPlcTagsDefinition(),
        };

        public static IZuliTypeDefinitionBase? DetectZuliType(IWorkbook workbook)
        {
            return Definitions.FirstOrDefault(def => def.Matches(workbook));
        }


        public static List<IZuliToInterface> Parse(IWorkbook workbook, IZuliTypeDefinitionBase def)
        {
            var sheet = workbook.GetSheetAt(def.SheetIndex);
            var result = new List<IZuliToInterface>();

            var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
            var formatter = new DataFormatter();

            for (int i = def.FirstDataRow; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                var line = def.ParseRowGeneric(row, formatter, evaluator);

                if (line != null && line.TextLanguage1 != string.Empty)
                {
                    result.Add(line);
                }
            }

            return result;
        }

    }


}
