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
        public byte[] Data { get => data; set => data = value; }
        public ROM()
        {

        }
        public void Load(string path)
        {
            if(path == "" || path == null)
            {
                data = new byte[0];
                Console.WriteLine("Please provide a rom file!");
            }
            else
            {
                try
                {
                    data = File.ReadAllBytes(path);
                    Console.WriteLine("Rom loaded succesfully! ");
                }
                catch (FileNotFoundException)
                {
                    data = new byte[0];
                    Console.WriteLine("Please provide a valid path!");
                }
            }
            
            
        }
    }
}
