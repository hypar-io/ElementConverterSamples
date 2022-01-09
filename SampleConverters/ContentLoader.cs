using System.Linq;
using DB = Autodesk.Revit.DB;
using Elements.Conversion.Revit.Extensions;
using Elements.Geometry;
using System;

namespace Elements.Conversion.Revit
{
    public class ContentLoader : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => false;

        public string Name => "Content Loader";

        public DB.FilteredElementCollector AddElementFilters(DB.FilteredElementCollector collector)
        {
            throw new System.NotImplementedException();
        }

        public Elements.Element[] FromRevit(DB.Element revitElement, DB.Document document)
        {
            throw new System.NotImplementedException();
        }

        public Elements.Element[] OnlyLoadableElements(Elements.Element[] allElements)
        {
            return allElements
            .OfType<ElementInstance>()
            .Where(el => el.BaseDefinition is ContentElement)
            .ToArray();
        }

        public DB.ElementId[] ToRevit(Elements.Element hyparElement, LoadContext context)
        {
            if (hyparElement is ElementInstance instance && instance.BaseDefinition is ContentElement content)
            {
                if (content.TryGetLoadedRevitFamily(context, out var familySymbol))
                {
                    var insertionPoint = instance.Transform.Origin.ToXYZ(true);
                    var closestLevel = GetLevelClosestToZ(insertionPoint.Z, context.Document, out double offsetFromLevel);
                    var placedContent = context.Document.Create.NewFamilyInstance(insertionPoint, familySymbol, closestLevel, DB.Structure.StructuralType.NonStructural);
                    placedContent.LookupParameter("Elevation from Level")?.Set(offsetFromLevel);
                    try
                    {
                        SetRotationOfInstance(context.Document, instance, content, insertionPoint, placedContent);
                    }
                    catch (Exception e)
                    {
                        context.Logger.Error("Unable to set rotation of instance. Some kinds of elements do not support 3d rotation.");
                        context.Logger.Error(e.Message);
                        context.Logger.Error(e.StackTrace);
                    }

                    placedContent.SetInstanceParametersFromContent(content, context);

                    return new DB.ElementId[] { placedContent.Id };
                }
                else
                {
                    context.Logger.Error($"Unable to find family {content.Name}, so its instances were not placed.");
                    return new DB.ElementId[] { };
                }
            }
            else
            {
                throw new System.Exception($"That element was not a content element.");
            }
        }

        private void SetRotationOfInstance(DB.Document document, ElementInstance instance, ContentElement content, DB.XYZ insertionPoint, DB.FamilyInstance placedContent)
        {
            var epsilon = 1E-5;
            // convert transform to Euler rotation
            var compoundTransform = instance.BaseDefinition.Transform.Concatenated(instance.Transform);
            EulerZYZ(compoundTransform, out var alpha, out var beta, out double gamma);
            // Some categories of elements support full 3D rotation, these are treated differently than others which basically just get a rotation applied.

            if (Math.Abs(gamma) > epsilon)
            {
                DB.ElementTransformUtils.RotateElement(document, placedContent.Id, DB.Line.CreateUnbound(insertionPoint, DB.XYZ.BasisZ), gamma);// * Math.PI / 180);
            }
            if (Math.Abs(beta) > epsilon)
            {
                DB.ElementTransformUtils.RotateElement(document, placedContent.Id, DB.Line.CreateUnbound(insertionPoint, DB.XYZ.BasisY), beta);// * Math.PI / 180);
            }
            if (Math.Abs(alpha) > epsilon)
            {
                DB.ElementTransformUtils.RotateElement(document, placedContent.Id, DB.Line.CreateUnbound(insertionPoint, DB.XYZ.BasisZ), alpha);// * Math.PI / 180);
            }

        }
        private void EulerZYZ(Transform t, out double alpha, out double beta, out double gamma)
        {
            var m = t.Matrix;
            if ((Math.Abs(m.m33) > (1.0 - Vector3.EPSILON)) ||
            (m.m32.ApproximatelyEquals(0.0) && m.m31.ApproximatelyEquals(0.0)) ||
                (m.m23.ApproximatelyEquals(0.0) && m.m13.ApproximatelyEquals(0.0)))
            {
                beta = (m.m33 > 0) ? 0.0 : Math.PI;
                alpha = Math.Atan2(-(m.m21), m.m22);
                gamma = 0.0;
            }
            else
            {
                beta = Math.Acos(m.m33);
                alpha = Math.Atan2(m.m32, m.m31);
                gamma = Math.Atan2(m.m23, -(m.m13));
            }
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
    }
}