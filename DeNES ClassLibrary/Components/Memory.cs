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
            Console.WriteLine($"WriteToMemory called: addr=0x{address:X4} data=0x{value:X2}");
            if (address >= 0x2000 && address <= 0x3FFF)
            {
                // $2000–$2007 tükrözve 8 byte-onként
                ushort reg = (ushort)(address & 0x2007);
                switch (reg)
                {
                    case 0x2000:
                        ppu.SETPPUCTRL(value);
                        break;
                    case 0x2001:
                        ppu.SETMASK(value);
                        break;
                    case 0x2005:
                        ppu.WritePPUSCROLL(value);
                        break;
                    case 0x2006:
                        ppu.SETPPUADDR(value);
                        break;
                    case 0x2007:
                        ppu.WritePPUDATA(value);
                        break;
                    default:
                        Console.WriteLine($"PPU Unsupported register: {reg:X4} at address: {address:X4})");
                        break;
                }
                return;
            }
            memory[address] = value;
        }
    }
}
