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
            data = File.ReadAllBytes(path);
        }
    }
}
