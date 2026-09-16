using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSystem.Models
{
    public class ProductConfig
    {
        public string ModelId { get; set; }
        public bool EnableBarcode { get; set; }
        public bool EnableLabel { get; set; }
        public int PackageLayers { get; set; } // 1 or 2
    }
}
