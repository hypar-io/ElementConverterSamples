using System;
using ADSK = Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Elements.Conversion.Revit
{
    public class ColumnConverter : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => false;

        public bool CanConvertFromRevit => true;

        public string Name => "Column Converter";

        public ADSK.FilteredElementCollector AddElementFilters(ADSK.FilteredElementCollector collector)
        {
            ADSK.ElementMulticategoryFilter filter = new ADSK.ElementMulticategoryFilter(new List<ADSK.BuiltInCategory> { ADSK.BuiltInCategory.OST_Columns, ADSK.BuiltInCategory.OST_StructuralColumns }, false);
            return collector.WherePasses(filter);
        }

        public Elements.Element[] FromRevit(ADSK.Element revitElement, ADSK.Document document)
        {
            return new[] { Create.ColumnFromRevitColumn(revitElement as ADSK.FamilyInstance, document) };
        }

        public Element[] OnlyLoadableElements(Element[] allElements)
        {
            throw new NotImplementedException();
        }

        public ADSK.ElementId[] ToRevit(Elements.Element hyparElement, LoadContext context)
        {
            throw new NotImplementedException();
        }
    }
}
