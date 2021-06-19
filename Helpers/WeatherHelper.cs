using MafaniaBot.Constants;

namespace MafaniaBot.Helpers
{
    public static class WeatherHelper
    {
        public static Direction ResolveWindDirection(float deg)
        {
            if (deg >= 22 && deg < 68)
                return Direction.NE;
            else if (deg >= 68 && deg < 113)
                return Direction.E;
            else if (deg >= 113 && deg < 158)
                return Direction.SE;
            else if (deg >= 158 && deg < 203)
                return Direction.S;
            else if (deg >= 203 && deg < 248)
                return Direction.SW;
            else if (deg >= 248 && deg < 293)
                return Direction.W;
            else if (deg >= 293 && deg < 338)
                return Direction.NW;
            else
                return Direction.N;
        }
    }
}
