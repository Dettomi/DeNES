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
        byte[] headerData;

        
        public byte[] Data { get => data; set => data = value; }
        public ROM()
        {
            header = new Header(new byte[16]);
            headerData = new byte[16];
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

                //GET HEADER DATA
                Array.Copy(data, headerData, 16);
                header.Data = headerData;
                header.getHeader();
                header.printHeader();

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
