using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Conversion.Revit.Extensions;
using ADSK = Autodesk.Revit.DB;

namespace Elements.Conversion.Revit
{
    public class FloorConverter : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => true;

        public string Name => "Floor Converter";

        public ADSK.FilteredElementCollector AddElementFilters(ADSK.FilteredElementCollector collector)
        {
            return collector.OfCategory(ADSK.BuiltInCategory.OST_Floors);
        }

        public Elements.Element[] FromRevit(ADSK.Element revitElement, ADSK.Document document)
        {
            return Create.FloorsFromRevitFloor(revitElement as ADSK.Floor, document);
        }

        public Element[] OnlyLoadableElements(Element[] allElements)
        {
            return allElements.OfType<Elements.Floor>().ToArray();
        }

        public ADSK.ElementId[] ToRevit(Elements.Element hyparElement, LoadContext context)
        {
            var createdElements = new List<ADSK.ElementId>();
            var floor = hyparElement as Elements.Floor;
            var curves = floor.Profile.Perimeter.ToRevitCurveArray(true);
            var floorType = new ADSK.FilteredElementCollector(context.Document)
                .OfClass(typeof(ADSK.FloorType))
                .OfType<ADSK.FloorType>()
                .First(e => e.Name.Contains("Generic"));

            double offsetFromLevel = 0;
            var level = context.Level ?? FromHyparExtensions.GetLevelClosestToZ(Units.MetersToFeet(floor.Elevation), context.Document, out offsetFromLevel);
            var rvtFloor = context.Document.Create.NewFloor(curves, floorType, level, false, ADSK.XYZ.BasisZ);

            context.Document.Regenerate(); // we must regenerate the document before adding openings
            var allOpenings = floor.Openings.Select(o => o.Perimeter).Union(floor.Profile.Voids);
            foreach (var opening in allOpenings)
            {
                var openingProfile = opening.ToRevitCurveArray(true);
                var rvtOpen = context.Document.Create.NewOpening(rvtFloor, openingProfile, true);
                createdElements.Add(rvtOpen.Id);
            }

            offsetFromLevel += Units.MetersToFeet(floor.Thickness);
            rvtFloor.LookupParameter("Height Offset From Level")?.Set(Units.MetersToFeet(offsetFromLevel));
            createdElements.Add(rvtFloor.Id);
            return createdElements.ToArray();
        }
    }
}
