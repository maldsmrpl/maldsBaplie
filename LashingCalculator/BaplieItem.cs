using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LashingCalculator
{
    public class BaplieItem
    {
        public string Position { get; set; }
        public string ContainerNumber { get; set; }
        public int Size { get; set; }
        public string IsoCode { get; set; }
        public Location Location { get; set; }
        public bool IsOOG { get; set; }
        public int LeftOverDim { get; set; }
        public int RightOverDim { get; set; }
        public int TopOverDim { get; set; }

    }
}
