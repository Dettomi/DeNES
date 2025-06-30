using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class PPU
    {
        byte[] patternTable;
        public byte[] Framebuffer;

        const int TileSize = 8;
        const int TilesPerRow = 16;
        
        public PPU(byte[] chr_rom) 
        { 
            this.patternTable = chr_rom;
            Framebuffer = new byte[(TileSize*TilesPerRow)*(TileSize * TilesPerRow) *4];
        }
        public void Tick()
        {
            DrawPatternTable();
        }
        public void DrawPattern8x8Tile(int position, int px, int py)
        {
            int tileAddress = position * 16;
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
                    int offset = (screenY * 128 + screenX) * 4;

                    Framebuffer[offset + 0] = intensity; // B
                    Framebuffer[offset + 1] = intensity; // G
                    Framebuffer[offset + 2] = intensity; // R
                    Framebuffer[offset + 3] = 255;       // A
                }
            }
        }
        public void DrawPatternTable()
        {
            for (int tileIndex = 0; tileIndex < 256; tileIndex++) // 16x16 tiles
            {
                int tileX = (tileIndex % TilesPerRow) * TileSize;
                int tileY = (tileIndex / TilesPerRow) * TileSize;
                DrawPattern8x8Tile(tileIndex, tileX, tileY);
            }
        }
    }
}
