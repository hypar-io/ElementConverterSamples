
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
    public class LevelLoader : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => false;

        public string Name => "Level Loader";

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
            var all = allElements.Where(e => e is Elements.Level)
                                  .ToArray();
            return all;
        }

        public DB.ElementId[] ToRevit(Element hyparElement, LoadContext context)
        {
            var lvl = hyparElement as Elements.Level;
            if (lvl == null)
            {
                return Array.Empty<DB.ElementId>();
            }
            var revitLevel = DB.Level.Create(context.Document, Units.MetersToFeet(lvl.Elevation));
            var allNames = new HashSet<string>(new DB.FilteredElementCollector(context.Document).OfCategory(DB.BuiltInCategory.OST_Levels)
                                                                            .WhereElementIsNotElementType()
                                                                            .ToElements()
                                                                            .Select(l => l.Name));
            var name = lvl.Name;
            var i = 0;
            while (allNames.Contains(name))
            {
                i++;
                name = name + $"-(Copy {i})";
            }

            revitLevel.LookupParameter("Name").Set(name);
            return new[] { revitLevel.Id };
        }
    }
}