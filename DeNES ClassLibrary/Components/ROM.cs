using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class ROM
    {
        byte[] data;

        byte[] header;
        string nesHeader;
        byte prgRomBanksx16;
        byte chrRomBanksx8;
        byte fistControlByte;
        byte secondControlByte;
        byte sizeOfPrgRamx8;
        byte tvsystem;
        byte extensions;
        public byte[] Data { get => data; set => data = value; }
        public ROM()
        {
            header = new byte[16];
        }
        public void Load(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                data = new byte[0];
                Console.WriteLine("Please provide a ROM file!");
                return;
            }
            try
            {
                data = File.ReadAllBytes(path);
                getHeader();
                printHeader();
                Console.WriteLine("ROM loaded succesfully! ");
            }
            catch (FileNotFoundException)
            {
                data = new byte[0];
                Console.WriteLine("Please provide a valid path for the ROM file!");
            }
            catch (Exception ex) { 
                data = new byte[0]; 
                Console.WriteLine("An error occurred: " + ex.Message); 
            }
            
        }
        public void getHeader()
        {
            if(data.Length< 16)
            {
                Console.WriteLine("Invalid ROM file: Header is too short.");
                return;
            }
            Array.Copy(data, header, 16);
            nesHeader = Encoding.ASCII.GetString(header, 0, 4);
            prgRomBanksx16 = header[4];
            chrRomBanksx8 = header[5];
            fistControlByte = header[6];
            secondControlByte = header[7];
            sizeOfPrgRamx8 = header[8];
            tvsystem = header[9];
            extensions = header[10];
            //foreach (byte b in header) { Console.WriteLine(((char)b+"("+b+")"));}
        }
        public void printHeader()
        {
            Console.WriteLine("\n---ROM HEADER---");
            foreach (byte b in header) { Console.Write(((char)b + "(" + b + ")")); }
            Console.WriteLine();
            if (nesHeader == "NES\x1A")
            {
                Console.WriteLine(nesHeader + " <- Valid Rom");
                Console.WriteLine("PRG-ROM: "+(prgRomBanksx16*16)+" KB");
                Console.WriteLine("CHR-ROM: " + (chrRomBanksx8 * 8) + " KB");

                if (sizeOfPrgRamx8 == 0) { Console.WriteLine("PRG-RAM: none"); }
                else { Console.WriteLine("PRG-RAM: "+(sizeOfPrgRamx8*8)+" KB"); }

                if (tvsystem == 0) { Console.WriteLine("TV system: NTSC"); }
                else if(tvsystem == 1) { Console.WriteLine("TV system: PAL"); }
                else { Console.WriteLine("TV system: INVALID"); }
            }
            else { Console.WriteLine(nesHeader + " <- Invalid Rom"); }
        }
    }
}
