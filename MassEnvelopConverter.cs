using System;
using System.Linq;
using ADSK = Autodesk.Revit.DB;
using Elements;
using Elements.Conversion.Revit.Extensions;
using Elements.Conversion.Revit;
using System.Collections.Generic;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace HyparRevitConverterSample
{
    public class MassEnvelopeConverter : IRevitConverter
    {
        public bool CanConvertToRevit => true;

        public bool CanConvertFromRevit => true;

        public ADSK.FilteredElementCollector AddElementFilters(ADSK.FilteredElementCollector collector)
        {
            return collector.OfCategory(ADSK.BuiltInCategory.OST_Mass);
        }

        public Elements.Element[] FromRevit(Autodesk.Revit.DB.Element revitElement, ADSK.Document document)
        {
            return MakeEnvelopesFromMass(revitElement);
        }

        public Elements.Element[] OnlyLoadableElements(Elements.Element[] allElements)
        {
            var tyeps = allElements.Select(e => e.GetType());
            var elemType = typeof(Elements.Envelope);
            return allElements.Where(e => e.GetType().FullName == typeof(Elements.Envelope).FullName).ToArray();
        }

        public ADSK.ElementId[] ToRevit(Elements.Element hyparElement, LoadContext context)
        {
            return DSFromEnvelope(hyparElement, context);
        }

        private static ADSK.ElementId[] DSFromEnvelope(Element hyparElement, LoadContext context)
        {
            var envelope = hyparElement as Envelope;

            var curves = envelope.Profile.Perimeter.Segments().Select(s => s.ToRevitCurve(true)); //.Vertices.Select(v => v.ToXYZ(true));
            var profileLoop = ADSK.CurveLoop.Create(curves.ToList());
            var profileLoops = new List<ADSK.CurveLoop> { profileLoop };

            var solid = ADSK.GeometryCreationUtilities.CreateExtrusionGeometry(profileLoops, ADSK.XYZ.BasisZ, envelope.Height);

            var elemId = new ADSK.ElementId(ADSK.BuiltInCategory.OST_Mass);
            var ds = ADSK.DirectShape.CreateElement(context.Document, elemId);
            ds.SetShape(new[] { solid }, ADSK.DirectShapeTargetViewType.Default);

            return new[] { elemId };
        }

        private static Element[] MakeEnvelopesFromMass(ADSK.Element revitElement)
        {
            var mass = revitElement as Autodesk.Revit.DB.FamilyInstance;
            var geom = mass.get_Geometry(new ADSK.Options());
            var solids = geom.Where(g => g is ADSK.Solid).Cast<ADSK.Solid>().Where(s => s.Volume > 0).ToList();
            var bottomFaces = solids.SelectMany(g => g.GetMostLikelyHorizontalFaces(downwardFacing: true));

            var envelopes = new List<Envelope>();
            var height = mass.get_BoundingBox(null).Max.Z - mass.get_BoundingBox(null).Min.Z;
            foreach (var face in bottomFaces)
            {
                var profiles = face.GetProfiles();

                foreach (var profile in profiles)
                {
                    var extrude = new Elements.Geometry.Solids.Extrude(profile, height, Vector3.ZAxis, false);
                    var rep = new Representation(new List<SolidOperation> { extrude });
                    var newEnvelope = new Envelope(profile,
                                                   0,
                                                   height,
                                                   Vector3.ZAxis,
                                                   0,
                                                   new Transform(),
                                                   BuiltInMaterials.Steel,
                                                   rep,
                                                   false,
                                                   Guid.NewGuid(),
                                                   "");
                    envelopes.Add(newEnvelope);
                }
            }

            return envelopes.ToArray();
        }
    }
}
