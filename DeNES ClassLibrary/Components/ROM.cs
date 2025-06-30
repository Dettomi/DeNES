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

        Header header;
        int prg_rom_size;
        int chr_rom_size;

        public byte[] Data { get => data; set => data = value; }
        public Header Header { get => header; }
        public ROM()
        {
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
                //GET ALL DATA
                data = File.ReadAllBytes(path);

                //HEADER
                byte[] headerData = new byte[16];
                Array.Copy(data, headerData, 16);
                header = new Header(headerData);
                header.printHeader();

                //PRG-ROM:

                //CHR-ROM


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
        public byte[] GetPrgRom()
        {
            prg_rom_size = header.prgRomBanksx16 * 16 * 1024;
            byte[] prg_rom = new byte[prg_rom_size];
            Array.Copy(data,16,prg_rom,0, prg_rom_size);
            return prg_rom;
        }
        public byte[] GetChrRom()
        {
            chr_rom_size = header.chrRomBanksx8 * 8 * 1024;

            if(chr_rom_size == 0) { 
                return Array.Empty<byte>(); }
            
            byte[] chr_rom = new byte[chr_rom_size];
            
            int offset = 16 + prg_rom_size;
            Array.Copy(data, offset, chr_rom, 0, chr_rom_size);
            
            return chr_rom;HR
        }
    }
}
