using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OthelloIA_G3
{
    /// <summary>
    /// Représente une case du plateau avec ses coordonnées
    /// </summary>
    public class Cell
    {
        public int C { get; set; }
        public int L { get; set; }

        public Cell(int c, int l)
        {
            C = c;
            L = l;
        }

        public Tuple<int, int> ToTuple()
        {
            return Tuple.Create(C, L);
        }
    }
}
