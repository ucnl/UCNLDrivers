
namespace UCNLDrivers
{
    public static class Helper
    {
        public static bool IsValidLatDeg(this double value)
        {
            return (value >= -90) && (value <= 90);
        }

        public static bool IsValidLonDeg(this double value)
        {
            return (value >= -180) && (value <= 180);
        }
    }
}
