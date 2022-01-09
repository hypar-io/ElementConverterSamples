using System;
using System.Linq;
using Elements.Conversion.Revit.Extensions;
using ADSK = Autodesk.Revit.DB;

namespace Elements.Conversion.Revit
{
    public class WallConverter : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => true;

        public string Name => "Wall Converter";

        public ADSK.FilteredElementCollector AddElementFilters(ADSK.FilteredElementCollector collector)
        {
            return collector.OfCategory(ADSK.BuiltInCategory.OST_Walls);
        }

        public Elements.Element[] FromRevit(ADSK.Element revitElement, ADSK.Document document)
        {
            // TODO stop producing WallByProfile when converting from Revit.  StandardWall is the wall that can be converted on the loading path.
            // We should be able to use other types and remove WallByProfile entirely.
            return Create.WallsFromRevitWall(revitElement as ADSK.Wall, document);
        }

        public Element[] OnlyLoadableElements(Element[] allElements)
        {

            var standard = allElements.OfType<Elements.StandardWall>().Cast<Wall>();
            var byProfile = allElements.OfType<Elements.WallByProfile>().Cast<Wall>();
            return standard.Concat(byProfile).ToArray();
        }

        public ADSK.ElementId[] ToRevit(Elements.Element hyparElement, LoadContext context)
        {
            var stdWall = hyparElement as Elements.StandardWall;
            if (stdWall == null)
            {
                return Array.Empty<ADSK.ElementId>();
            }
            var cl = stdWall.CenterLine.ToRevitCurve(true);
            var offsetFromLevel = 0.0;
            var level = context.Level ?? FromHyparExtensions.GetLevelClosestToZ(Units.MetersToFeet(stdWall.Transform.Origin.Z), context.Document, out offsetFromLevel);
            var rvtWall = ADSK.Wall.Create(context.Document, cl, level.Id, false);
            rvtWall.LookupParameter("Unconnected Height").Set(Units.MetersToFeet(stdWall.Height));
            rvtWall.LookupParameter("Base Offset").Set(Units.MetersToFeet(offsetFromLevel));

            // Letting the walls join as they are loaded causes many errors and walls that are missing.
            // For now we disallow joining.
            ADSK.WallUtils.DisallowWallJoinAtEnd(rvtWall, 0);
            ADSK.WallUtils.DisallowWallJoinAtEnd(rvtWall, 1);
            return new ADSK.ElementId[] { rvtWall.Id };
        }
    }
}