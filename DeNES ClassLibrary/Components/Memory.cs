using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class Memory
    {
        int[] memo;
        public int[] memory { get => memo; set => memo = value; }
        public Memory()
        {
            memo = new int[65536];
        }
    }
}
