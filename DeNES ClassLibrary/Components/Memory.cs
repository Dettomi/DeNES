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
        PPU ppu;
        public Memory()
        {
            memory = new byte[65536]; //64k memory
        }

        public PPU Ppu { get => ppu; set => ppu = value; }

        public byte Read(ushort address)
        {
            if(address == 0x2002)
            {
                return ppu.READPPUSTATUS();
            }
            return memory[address];
        }
        public void Write(ushort address, byte value)
        {
            memory[address] = value;
        }
    }
}
