using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Platformer {
    public static class CoordinateHelper {
        // Because Farseer uses 1 unit = 1 meter we need to convert
        // between pixel coordinates and physics coordinates.
        // I've chosen to use the rule that 100 pixels is one meter.
        // We have to take care to convert between these two 
        // coordinate-sets wherever we mix them!

        public const float unitToPixel = 100.0f;
        public const float pixelToUnit = 1 / unitToPixel;

        public static Vector2 ToScreen(Vector2 worldCoordinates) {
            return worldCoordinates * unitToPixel;
        }

        public static Vector2 ToWorld(Vector2 screenCoordinates) {
            return screenCoordinates * pixelToUnit;
        }

    }
}
