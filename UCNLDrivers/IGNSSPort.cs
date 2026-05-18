using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCNLDrivers
{
    public interface IGNSSPort
    {
        double Latitude { get; }
        double Longitude { get; }
        double GroundSpeed { get; }
        double CourseOverGround { get; }
        double Heading { get; }
        bool MagneticOnly { get; set; }
        DateTime GNSSTime { get; }


        event EventHandler LocationUpdated;
        event EventHandler HeadingUpdated;
    }    
}
