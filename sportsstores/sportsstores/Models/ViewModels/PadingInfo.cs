using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sportsstores.Models.ViewModels
{
    public class PadingInfo
    {
        public int TotalItems { set; get; }
        public int ItemPerPage { set; get; }
        public int CurentPage { get; set; }
        public int TotalPages =>
            (int)Math.Ceiling((decimal)TotalItems / ItemPerPage);
    }
}
