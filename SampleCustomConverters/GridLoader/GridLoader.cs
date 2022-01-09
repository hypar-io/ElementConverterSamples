
using DB = Autodesk.Revit.DB;
using System;
using System.Linq;
using Elements.Conversion.Revit.Extensions;
using System.Collections.Generic;
using Elements;
using System.IO;
using Newtonsoft.Json;

namespace Elements.Conversion.Revit
{
    public class GridLoader : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => false;

        public string Name => "Grid Loader";

        public DB.FilteredElementCollector AddElementFilters(DB.FilteredElementCollector collector)
        {
            throw new NotImplementedException();
        }

        public Elements.Element[] FromRevit(DB.Element revitElement, DB.Document document)
        {
            throw new NotImplementedException();
        }

        public Elements.Element[] OnlyLoadableElements(Elements.Element[] allElements)
        {
            var all = allElements.Where(e => e is Elements.GridLine)
                                  .ToArray();
            return all;
        }


        public DB.ElementId[] ToRevit(Element hyparElement, LoadContext context)
        {
            var grid = hyparElement as Elements.GridLine;
            if (grid == null)
            {
                return Array.Empty<DB.ElementId>();
            }
            var line = DB.Line.CreateBound(grid.Geometry.Start.ToXYZ(true), grid.Geometry.End.ToXYZ(true));
            var revitGrid = DB.Grid.Create(context.Document, line);
            revitGrid.LookupParameter("Name").Set(grid.Name);
            return new[] { revitGrid.Id };
        }
    }
}