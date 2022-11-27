using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AverageTerrariaSurvival
{


    public class Utilities
    {
        static Color[] colors = new Color[] { Color.Black, Color.DarkGreen, Color.LightGreen, Color.Green, Color.Cyan, Color.Blue, Color.DarkBlue, Color.Purple, Color.Magenta, Color.Red, Color.IndianRed, Color.Orange, Color.Yellow, Color.White, Color.Transparent};


        public static Microsoft.Xna.Framework.Color IntToColor(int i)
        {
            Console.WriteLine(i);
            float scaled = (float)(-i - 400) / 400 * 13 + 26;
            Console.WriteLine(scaled);
            Color color0 = colors[(int)scaled];
            Color color1 = colors[(int)scaled + 1];
            float fraction = scaled - (int)scaled;
            var r = (byte)((1 - fraction) * (float)color0.R + fraction * (float)color1.R);
            var g = (byte)((1 - fraction) * (float)color0.G + fraction * (float)color1.G);
            var b = (byte)((1 - fraction) * (float)color0.B + fraction * (float)color1.B);
            var a = 255;
            Microsoft.Xna.Framework.Color result = new Microsoft.Xna.Framework.Color(r,g,b);

            return result;
        }

    }
}
