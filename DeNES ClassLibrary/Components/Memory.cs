using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class Memory
    {
        byte[] memory;

        public Memory()
        {
            memory = new byte[65536]; //64k memory
        }
        public byte Read(ushort address)
        {
            return memory[address];
        }
        public void Write(ushort address, byte value)
        {
            memory[address] = value;
        }
    }
}
