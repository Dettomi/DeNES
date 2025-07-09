using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class PPU
    {
        byte[] patternTable;
        public byte[] Framebuffer;

        byte[] nameTable0 = new byte[960]; //Should be 4kb
        byte[] paletteTable = new byte[32];

        const int TileSize = 8;
        const int TilesPerRow = 16;

        //PPU REGISTERS: $2000 - $2007
        byte register_PPUCTRL;
        byte register_PPUMASK;
        byte register_PPUSTATUS;
        byte register_OAMADDR;
        byte register_PPUSCROLL_X;
        byte register_PPUSCROLL_Y;
        ushort register_PPUADDR;
        byte register_PPUDATA;
        byte register_OAMDMA;

        public PPU(byte[] chr_rom) 
        { 
            this.patternTable = chr_rom;
            //Framebuffer = new byte[(TileSize*TilesPerRow)*(TileSize * TilesPerRow) *4]; //128x128 (16x16 tile 8x8 pixel each) * 4 (RGBA) = 65.536
            Framebuffer = new byte[256*240 * 4];

            PopulateNameTable();
        }
        public void Tick()
        {
            DrawNameTable();
            //DrawPatternTable();
        }
        public void DrawPattern8x8Tile(int position, int px, int py)
        {
            int tileAddress = position * 16; //1 tile = 16 byte
            for (int row = 0; row < 8; row++)
            {
                byte first = patternTable[tileAddress + row];
                byte second = patternTable[tileAddress + row + 8];

                for (int col = 0; col < 8; col++)
                {
                    int bit = 7 - col;
                    int bit0 = (first >> bit) & 1;
                    int bit1 = (second >> bit) & 1;
                    int color = (bit1 << 1) | bit0;

                    byte intensity = (byte)(color * 85);

                    int screenX = px + col;
                    int screenY = py + row;
                    int offset = (screenY * 256 + screenX) * 4;

                    Framebuffer[offset + 0] = intensity; // B
                    Framebuffer[offset + 1] = intensity; // G
                    Framebuffer[offset + 2] = intensity; // R
                    Framebuffer[offset + 3] = 255;       // A
                }
            }
        }
        public void DrawPatternTable()
        {
            //How many to draw
            int bankCount = patternTable.Length / 4096;
            int maxTiles;
            if(bankCount <= 3){
                maxTiles = bankCount * 256;
            }else { maxTiles = 960; }
            
            for (int tileIndex = 0; tileIndex < maxTiles; tileIndex++) // 16x16 tiles
            {
                int tileX = (tileIndex % 32) * TileSize;
                int tileY = (tileIndex / 32) * TileSize;
                DrawPattern8x8Tile(tileIndex, tileX, tileY);
            }
        }
        public void DrawNameTable()
        {
            for (int yy = 0; yy < 30; yy++)
            {
                for (int xx = 0; xx < 32; xx++)
                {
                    int address = nameTable0[(yy*32) + xx];
                    DrawPattern8x8Tile(address,xx*8,yy*8);
                }
            }
        }
        private void PopulateNameTable()
        {
            for (int i = 0; i < 960; i++)
            {
                nameTable0[i] = (byte)(i % 256);
            }
        }
    }
}
