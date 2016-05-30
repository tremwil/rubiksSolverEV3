using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubiksSolveEV3
{
    enum LightColors
    {
        Green,
        Orange,
        Red
    }

    static class EV3Lights
    {
        public static void setOff()
        {
            Program.ev3.Mailbox.Send("color", "7");
        }

        public static void setOn(LightColors color, bool flicker = false)
        {
            var value = ((int)color << 1) | (flicker ? 1 : 0);
            Program.ev3.Mailbox.Send("color", value.ToString());
        }
    }
}
