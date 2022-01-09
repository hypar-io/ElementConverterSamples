using DB = Autodesk.Revit.DB;
using System;

namespace Elements.Conversion.Revit
{
    public class AreaConverter : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => false;

        public bool CanConvertFromRevit => true;

        public string Name => "Area Converter";

        public DB.FilteredElementCollector AddElementFilters(DB.FilteredElementCollector collector)
        {
            return collector.OfCategory(DB.BuiltInCategory.OST_Areas);
        }

        public Element[] FromRevit(DB.Element revitElement, DB.Document document)
        {
            return Create.SpaceBoundaryFromRevitArea(revitElement as DB.Area, document);
        }

        public Element[] OnlyLoadableElements(Element[] allElements)
        {
            throw new NotImplementedException();
        }

        public DB.ElementId[] ToRevit(Element hyparElement, LoadContext context)
        {
            throw new NotImplementedException();
        }
    }
}