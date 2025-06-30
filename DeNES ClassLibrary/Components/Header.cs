using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class Header
    {
        byte[] data;

        string nesHeader;
        byte prgRomBanksx16;
        byte chrRomBanksx8;
        byte fistControlByte;
        byte secondControlByte;
        byte sizeOfPrgRamx8;
        byte tvsystem;
        byte extensions;

        public byte[] Data { get => data; set => data = value; }
        public Header(byte[] data)
        {
            this.data = data;
            if (data.Length < 16)
            {
                Console.WriteLine("Invalid ROM file: Header is too short.");
                return;
            }
            nesHeader = Encoding.ASCII.GetString(data, 0, 4);
            prgRomBanksx16 = data[4];
            chrRomBanksx8 = data[5];
            fistControlByte = data[6];
            secondControlByte = data[7];
            sizeOfPrgRamx8 = data[8];
            tvsystem = data[9];
            extensions = data[10];
            //foreach (byte b in data) { Console.WriteLine(((char)b+"("+b+")"));}
        }
        public void printHeader()
        {
            Console.WriteLine("\n---ROM HEADER---");
            foreach (byte b in data) { Console.Write(((char)b + "(" + b + ")")); }
            Console.WriteLine();
            if (nesHeader == "NES\x1A")
            {
                Console.WriteLine(nesHeader + " <- Valid Rom");
                Console.WriteLine("PRG-ROM: " + (prgRomBanksx16 * 16) + " KB");
                Console.WriteLine("CHR-ROM: " + (chrRomBanksx8 * 8) + " KB");

                if (sizeOfPrgRamx8 == 0) { Console.WriteLine("PRG-RAM: none"); }
                else { Console.WriteLine("PRG-RAM: " + (sizeOfPrgRamx8 * 8) + " KB"); }

                if (tvsystem == 0) { Console.WriteLine("TV system: NTSC"); }
                else if (tvsystem == 1) { Console.WriteLine("TV system: PAL"); }
                else { Console.WriteLine("TV system: INVALID"); }
            }
            else { Console.WriteLine(nesHeader + " <- Invalid Rom"); }
        }
    }
}
