using System.Linq;
using ADSK = Autodesk.Revit.DB;

namespace Elements.Conversion.Revit
{
    public class CeilingConverter : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => false;

        public bool CanConvertFromRevit => true;

        public string Name => "Ceiling Converter";

        public ADSK.FilteredElementCollector AddElementFilters(ADSK.FilteredElementCollector collector)
        {
            return collector.OfClass(typeof(ADSK.Ceiling));
        }

        public Elements.Element[] FromRevit(Autodesk.Revit.DB.Element revitElement, ADSK.Document document)
        {
            return Create.CeilingFromRevitCeiling(revitElement as ADSK.Ceiling, document);
        }

        public Element[] OnlyLoadableElements(Element[] allElements)
        {
            return allElements.OfType<Elements.Ceiling>().ToArray();
        }

        public ADSK.ElementId[] ToRevit(Element hyparElement, LoadContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}