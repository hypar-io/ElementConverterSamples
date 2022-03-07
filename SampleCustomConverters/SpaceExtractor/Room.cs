
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Properties;
using Elements.Validators;
using Elements.Serialization.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    public partial class Room
    {
        public static Material RoomMaterial = new Material("Rooms", new Color(0, 0.2, 0.8, 0.5));
        public override void UpdateRepresentations()
        {
            var solids = new List<SolidOperation>{
                new Extrude(this.Perimeter, this.Height, this.Direction, false)
            };
            Representation = new Representation(solids);
        }
    }
}