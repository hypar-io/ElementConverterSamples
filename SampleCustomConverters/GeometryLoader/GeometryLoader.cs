
using DB = Autodesk.Revit.DB;
using System;
using System.Linq;
using Elements.Conversion.Revit.Extensions;
using System.Collections.Generic;
using Elements;
using System.IO;
using Newtonsoft.Json;
using Elements.Geometry;

namespace Elements.Conversion.Revit
{
    public class GeometryLoader : IRevitConverter, INamed
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => false;

        public string Name => "Direct Shape Geometry Loader";

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
            var c = typeNameNumber;
            var all = allElements.Where(e => e is Elements.GeometricElement
                                          || e.GetType().IsAssignableFrom(typeof(ElementInstance)))
                                  .ToArray();
            return all;
        }

        private Dictionary<string, int> typeNameNumber = new Dictionary<string, int>();

        public DB.ElementId[] ToRevit(Element hyparElement, LoadContext context)
        {
            if (!hyparElement.AdditionalProperties.TryGetValue("discriminator", out var nameVal) || nameVal == null)
            {
                nameVal = "";
            }

            var name = nameVal as string;
            var categoryId = new DB.ElementId(GeometryLoaderConfig.GetBuiltInCategory(hyparElement));
            if (hyparElement is ElementInstance instance)
            {
                return DirectShapeInstance(context, categoryId, instance);
            }
            else
            {
                return DirectShapeElement(hyparElement, context, name, categoryId);
            }
        }

        private static DB.ElementId[] DirectShapeElement(Element hyparElement, LoadContext context, string name, DB.ElementId categoryId)
        {
            if (!GeometryLoaderConfig.ShouldDirectShapeObject(hyparElement)
                || (hyparElement is GeometricElement geometricElement && geometricElement.IsElementDefinition))
            {
                return Array.Empty<DB.ElementId>();
            }

            var ds = DB.DirectShape.CreateElement(context.Document, categoryId);
            var shapeBuilder = hyparElement.GetShapeBuilder(context.Document);

            if (shapeBuilder == null)
            {
                return Array.Empty<DB.ElementId>();
            }

            ds.SetShape(shapeBuilder);
            ds.LookupParameter("Comments")?.Set(name);
            ds.LookupParameter("Mark")?.Set(hyparElement.Id.ToString());
            return new[] { ds.Id };
        }

        private DB.ElementId[] DirectShapeInstance(LoadContext context, DB.ElementId categoryId, ElementInstance instance)
        {
            var discriminator = "ElementInstance";
            if (instance.BaseDefinition.AdditionalProperties.TryGetValue("discriminator", out var discriminatorValue) && discriminatorValue != null)
            {
                discriminator += $" of {discriminatorValue as string}";
            }
            if (!GeometryLoaderConfig.ShouldDirectShapeObject(instance.BaseDefinition))
            {
                return Array.Empty<DB.ElementId>();
            }
            else
            {
                var defTypeId = DB.DirectShapeLibrary.GetDirectShapeLibrary(context.Document).FindDefinitionType(instance.BaseDefinition.Id.ToString());
                // If a user uses "Undo" after we've created some direct shape objects the DirectShapeLibrary
                // does not refresh and know that this element has been removed.  This will look like it's going
                // to work all the way until we go to regenerate the document, when a DocumentCorruption error
                // will be posted resulting in the entire Load operation (all converters) to be rolled back.
                var foundType = context.Document.GetElement(defTypeId);
                if (defTypeId == DB.ElementId.InvalidElementId || foundType == null)
                {
                    string typeName = TypeNameForDirectShapeType(instance);
                    var defType = DB.DirectShapeType.Create(context.Document, typeName, categoryId);
                    var shapeBuilder = instance.BaseDefinition.GetShapeBuilder(context.Document);
                    if (shapeBuilder != null)
                    {
                        defType.AppendShape(shapeBuilder);
                    }
                    else
                    {
                        return Array.Empty<DB.ElementId>();
                    }
                    defTypeId = defType.Id;
                    DB.DirectShapeLibrary.GetDirectShapeLibrary(context.Document).AddDefinitionType(instance.BaseDefinition.Id.ToString(), defTypeId);
                }

                var completeTransform = new Transform();
                if (instance.BaseDefinition.Transform != null)
                {
                    completeTransform.Concatenate(instance.BaseDefinition.Transform);
                    completeTransform.Invert();
                }
                if (instance.Transform != null)
                {
                    completeTransform.Concatenate(instance.Transform);
                }

                var directShape = DB.DirectShape.CreateElementInstance(context.Document,
                                                                       defTypeId,
                                                                       categoryId,
                                                                       instance.BaseDefinition.Id.ToString(),
                                                                       completeTransform.ToRevitTransform(true));
                if (directShape == null)
                {
                    return Array.Empty<DB.ElementId>();
                }

                directShape.LookupParameter("Comments")?.Set(discriminator);
                directShape.LookupParameter("Mark")?.Set(instance.Id.ToString());
                return new[] { directShape.Id };
            }
        }

        private string TypeNameForDirectShapeType(ElementInstance instance)
        {
            var nameParts = new List<string>();
            if (instance.BaseDefinition.AdditionalProperties.TryGetValue("discriminator", out var discriminator))
            {
                nameParts.Add(discriminator as string);
            }
            if (!string.IsNullOrWhiteSpace(instance.BaseDefinition.Name))
            {
                nameParts.Add(instance.BaseDefinition.Name);
            }
            var typeName = String.Join(" - ", nameParts);
            if (typeNameNumber.TryGetValue(typeName, out var number))
            {
                typeNameNumber[typeName] = number + 1;
                return typeName + $" ({number++})"; //increment the number to be applied if a type name already exists with this name.
            }
            else
            {
                typeNameNumber[typeName] = 0;
                return typeName;
            }

        }
    }
}