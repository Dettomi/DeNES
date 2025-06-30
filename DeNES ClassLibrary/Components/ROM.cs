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
         //SHOULD MOVE

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
    }
}
