using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OthelloIA_G3
{
    /// <summary>
    /// Représente un pion avec ses coordonnées dans le plateau et sa couleur.
    /// </summary>
    public class Pawn : Cell
    {
        public int Color { get; set; }

        public Pawn(int c, int l, int color) : base(c, l)
        {
            Color = color;
        }
    }
}
