using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCoreApplication.Models
{
    public class Geometry
    {
        public int Altitude { get; set; }
        public int Height { get; set; }
        public double GetArea()
        {
            return Altitude * Height / 2;
        }
    }
}
