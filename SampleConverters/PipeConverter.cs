using DB = Autodesk.Revit.DB;
using System;
using System.Linq;
using Autodesk.Revit.DB;
using Elements.Piping;
using Elements.Conversion.Revit.Extensions;
using System.Collections.Generic;

namespace Elements.Conversion.Revit
{
    public class PipeConverter : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => false;

        public string Name => "Pipe Converter";

        public Element[] OnlyLoadableElements(Element[] allElements)
        {
            var ells = allElements.Where(e => e.GetType().FullName == "Elements.Piping.PipeSegment").ToArray();
            return allElements.Where(e => e.GetType().FullName == "Elements.Piping.PipeSegment").ToArray();
        }

        public ElementId[] ToRevit(Element hyparElement, LoadContext context)
        {
            var pipe = hyparElement as PipeSegment;
            var pipeType = GetAndCachePipeType(context.Document, pipe.Name);
            var systemType = GetAndCacheSystemType(context.Document, pipe.Name);
            var start = pipe.Start.Position.ToXYZ(true);
            var end = pipe.End.Position.ToXYZ(true);
            var levelId = GetLevelClosestToZ(start.Z, context.Document, out var offsetFromLevel).Id;
            var newPipe = DB.Plumbing.Pipe.Create(context.Document, systemType, pipeType, levelId, start, end);

            // 0.99 factor to be sure that we are "below" the desired pipe size
            // which will be the next largest allowed size from whatever is set.
            newPipe.LookupParameter("Diameter").Set(Elements.Units.MetersToFeet(0.99 * pipe.Diameter));

            return new[] { newPipe.Id };
        }

        ElementId _systemTypeId = null;
        private ElementId GetAndCacheSystemType(DB.Document document, string name = null)
        {
            if (_systemTypeId == null)
            {
                var allSystemTypes = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_PipingSystem).WhereElementIsElementType();
                if (name == null)
                {
                    _systemTypeId = allSystemTypes.First().Id;
                }
                else
                {
                    var foundSystem = allSystemTypes.FirstOrDefault(s => s.Name.ToLower().Contains(name.ToLower()));
                    if (foundSystem == null)
                    {
                        _systemTypeId = allSystemTypes.First().Id;
                    }
                    else
                    {
                        _systemTypeId = foundSystem.Id;
                    }
                }
            }
            return _systemTypeId;
        }

        ElementId _pipeTypeId = null;
        private ElementId GetAndCachePipeType(DB.Document document, string name = null)
        {
            if (_pipeTypeId == null)
            {
                var allPipeTypes = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_PipeCurves).WhereElementIsElementType();
                if (name == null)
                {
                    _pipeTypeId = allPipeTypes.First().Id;
                }
                else
                {
                    var foundType = allPipeTypes.FirstOrDefault(t => t.Name.ToLower().Contains(name.ToLower()));
                    if (foundType == null)
                    {
                        _pipeTypeId = allPipeTypes.First().Id;
                    }
                    else
                    {
                        _pipeTypeId = foundType.Id;
                    }
                }
            }
            return _pipeTypeId;
        }

        private static DB.Level GetLevelClosestToZ(double z, DB.Document document, out double offsetFromLevel)
        {
            var levels = new DB.FilteredElementCollector(document).OfClass(typeof(DB.Level))
                                                                  .Cast<DB.Level>()
                                                                  .OrderBy(x => Math.Abs(z - x.ProjectElevation));
            var level = levels.FirstOrDefault();
            offsetFromLevel = z - level.ProjectElevation;
            return level;
        }

        public FilteredElementCollector AddElementFilters(FilteredElementCollector collector)
        {
            throw new NotImplementedException();
        }

        public Element[] FromRevit(DB.Element revitElement, Document document)
        {
            throw new NotImplementedException();
        }
    }
}