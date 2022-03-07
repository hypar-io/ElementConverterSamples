using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Elements;
using Elements.Conversion.Revit;
using Elements.Conversion.Revit.Extensions;
using Elements.Geometry;

namespace SpaceExtractor
{
    public class SpaceExtractor : IRevitConverter
    {
        public bool CanConvertToRevit => false;

        public bool CanConvertFromRevit => true;

        public FilteredElementCollector AddElementFilters(FilteredElementCollector collector)
        {
            return collector.OfCategory(BuiltInCategory.OST_MEPSpaces);
        }

        public Elements.Element[] FromRevit(Autodesk.Revit.DB.Element revitElement, Document document)
        {
            var geomCalculator = new SpatialElementGeometryCalculator(document);
            var space = revitElement as Autodesk.Revit.DB.Mechanical.Space;
            var results = geomCalculator.CalculateSpatialElementGeometry(space);
            var geometry = results.GetGeometry();

            var spaceHeight = Elements.Units.FeetToMeters(space.get_BoundingBox(null).Max.Z - space.get_BoundingBox(null).Min.Z);

            var downfaces = geometry.GetMostLikelyHorizontalFaces(downwardFacing: true);

            var allRooms = new List<Room>();
            foreach (var face in downfaces)
            {
                foreach (var profile in face.GetProfiles(true))
                {
                    var room = new Room(profile.Perimeter, Vector3.ZAxis, "", "", "", "", 0, 0, 0, 0, spaceHeight, 0, null, Room.RoomMaterial, null, false, Guid.NewGuid(), "");
                    allRooms.Add(room);
                }
            }

            return allRooms.ToArray();
        }

        public Elements.Element[] OnlyLoadableElements(Elements.Element[] allElements)
        {
            throw new NotImplementedException();
        }

        public ElementId[] ToRevit(Elements.Element hyparElement, LoadContext context)
        {
            throw new NotImplementedException();
        }
    }
}