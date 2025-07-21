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
        private CPU cpu;
        byte[] patternTable;
        public byte[] Framebuffer;

        byte[] nameTable = new byte[4096]; //Should be 4kb
        byte[] oam = new byte[256];
        //PALETTA:
        byte[] paletteTable = new byte[32];
        // NES 64-color palette (approximate RGB values)
        static readonly byte[,] nesPalette = new byte[64, 3]
        {
            {124,124,124},{0,0,252},{0,0,188},{68,40,188},
            {148,0,132},{168,0,32},{168,16,0},{136,20,0},
            {80,48,0},{0,120,0},{0,104,0},{0,88,0},
            {0,64,88},{0,0,0},{0,0,0},{0,0,0},

            {188,188,188},{0,120,248},{0,88,248},{104,68,252},
            {216,0,204},{228,0,88},{248,56,0},{228,92,16},
            {172,124,0},{0,184,0},{0,168,0},{0,168,68},
            {0,136,136},{0,0,0},{0,0,0},{0,0,0},

            {248,248,248},{60,188,252},{104,136,252},{152,120,248},
            {248,120,248},{248,88,152},{248,120,88},{252,160,68},
            {248,184,0},{184,248,24},{88,216,84},{88,248,152},
            {0,232,216},{120,120,120},{0,0,0},{0,0,0},

            {252,252,252},{164,228,252},{184,184,248},{216,184,248},
            {248,184,248},{248,164,192},{240,208,176},{252,224,168},
            {248,216,120},{216,248,120},{184,248,184},{184,248,216},
            {0,252,252},{248,216,248},{0,0,0},{0,0,0}
        };


        int ppu_tick = 0;
        const int TileSize = 8;
        const int TilesPerRow = 16;

        //PPU REGISTERS: $2000 - $2007
        byte register_PPUCTRL; //$2000
        byte register_PPUMASK;
        byte register_PPUSTATUS;
        byte register_OAMADDR;
        byte register_PPUSCROLL_X;
        byte register_PPUSCROLL_Y;
        ushort register_PPUADDR;
        bool latch_PPUADDR;
        bool latch_PPUSCROLL;
        byte register_PPUDATA;
        byte register_OAMDMA;

        public PPU(byte[] chr_rom) 
        { 
            this.patternTable = chr_rom;
            //Framebuffer = new byte[(TileSize*TilesPerRow)*(TileSize * TilesPerRow) *4]; //128x128 (16x16 tile 8x8 pixel each) * 4 (RGBA) = 65.536
            Framebuffer = new byte[256*240 * 4];

            //PopulateNameTable();
        }
        public void Tick()
        {
            ppu_tick++;
            const int totalCyclesPerFrame = 341 * 262;
            

            if (ppu_tick % totalCyclesPerFrame == (241 * 341 + 1))
            {
                register_PPUSTATUS |= 0x80; // VBlank on
                

            }

            if (ppu_tick % totalCyclesPerFrame == (261 * 341 + 1))
            {
                register_PPUSTATUS &= 0x7F; // VBlank off
            }

            if (ppu_tick % 50 == 0 && (register_PPUMASK & 0x08) != 0)
            {
                
            }
            if(ppu_tick % 50 == 0)
            {
                DrawNameTable();
            }
            
        }
        public void SetCPU(CPU cpu)
        {
            this.cpu = cpu;
        }
        public void DrawPattern8x8Tile(int position, int px, int py)
        {
            //NON PALETTE:
            int patternBase = (register_PPUCTRL & 0x10) != 0 ? 0x1000 : 0x0000;
            int tileAddress = patternBase + position * 16;

            //PALETTE:
            int tileX = px / 8;
            int tileY = py / 8;

            int paletteIndex = GetBackgroundPaletteIndex(tileX, tileY);
            int paletteBase = paletteIndex * 4;

            for (int row = 0; row < 8; row++)
            {
                byte first = patternTable[tileAddress + row];
                byte second = patternTable[tileAddress + row + 8];

                for (int col = 0; col < 8; col++)
                {
                    int screenX = px + col;
                    int screenY = py + row;
                    int offset = (screenY * 256 + screenX) * 4;

                    int bit = 7 - col;
                    int bit0 = (first >> bit) & 1;
                    int bit1 = (second >> bit) & 1;
                    int color = (bit1 << 1) | bit0;

                    //PALETTE:

                      //byte intensity = (byte)(color * 85); //Grayscale
                    byte nesColorIndex = (color == 0) ? paletteTable[0] : paletteTable[paletteBase + color];

                    byte r = nesPalette[nesColorIndex, 0];
                    byte g = nesPalette[nesColorIndex, 1];
                    byte b = nesPalette[nesColorIndex, 2];

                    Framebuffer[offset + 0] = b; // B
                    Framebuffer[offset + 1] = g; // G
                    Framebuffer[offset + 2] = r; // R
                    Framebuffer[offset + 3] = 255; // A
                }
            }
        }
        private int GetBackgroundPaletteIndex(int tileX, int tileY)
        {
            // 16x16 pixel regions tileX/4, tileY/4
            int attrTableBase = 0x03C0; // nameTable offseten belül
            int attrX = tileX / 4;
            int attrY = tileY / 4;
            int attrIndex = attrY * 8 + attrX;
            byte attrByte = nameTable[attrTableBase + attrIndex];

            bool top = (tileY % 4) < 2;
            bool left = (tileX % 4) < 2;
            int shift = (top ? 0 : 4) + (left ? 0 : 2);

            return (attrByte >> shift) & 0b11; // 2 bites palette index (0–3)
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
                    int address = nameTable[(yy*32) + xx];
                    DrawPattern8x8Tile(address,xx*8,yy*8);
                }
            }
        }
        private void PopulateNameTable()
        {
            for (int i = 0; i < 960; i++)
            {
                nameTable[i] = (byte)(i % 256);
            }
        }
        //CPU REGISTER METHODS:
        public void SETPPUCTRL(byte value) //$2000
        {
            register_PPUCTRL = value;
        }
        public void SETMASK(byte value) //$2001
        {
            register_PPUMASK = value;
        }
        public byte PEEKPPUSTATUS()
        {
            return register_PPUSTATUS;
        }
        public byte READPPUSTATUS()
        {
            Console.WriteLine("READPPUSTATUS: Clearing latch_PPUADDR");
            byte r = register_PPUSTATUS;
            register_PPUSTATUS &= 0x7F; // Bit 7 clear
            latch_PPUADDR = false;
            return r;
        }
        public void WritePPUSCROLL(byte value)
        {
            if (!latch_PPUSCROLL)
            {
                register_PPUSCROLL_X = value;
                latch_PPUSCROLL = true;
            }
            else
            {
                register_PPUSCROLL_Y = value;
                latch_PPUSCROLL = false;
            }
        }
        public void SetOAMADDR(byte value)
        {
            register_OAMADDR = value;
        }
        public void SETPPUADDR(byte value)
        {
            Console.WriteLine($"WriteToMemory: $2006 ← {value:X2} (Latch: {latch_PPUADDR})");
            if (!latch_PPUADDR)
            {
                register_PPUADDR = (ushort)(value << 8); // high byte
                Console.WriteLine($"PPUADDR high set: {value:X2}");
                latch_PPUADDR = true;
            }
            else
            {
                register_PPUADDR |= value; // low byte
                Console.WriteLine($"PPUADDR full: {register_PPUADDR:X4}");
                latch_PPUADDR = false;
            }
        }

        public void WritePPUDATA(byte value)
        {
            Console.WriteLine($"WritePPUDATA: addr=0x{register_PPUADDR:X4} → value=0x{value:X2}");
            if ((register_PPUSTATUS & 0x80) == 0)
            {
                // block writes outside VBlank
                Console.WriteLine($" VRAM write outside VBlank: addr={register_PPUADDR:X4}");
            }
            if (register_PPUADDR < 0x2000)
            {
                // CHR-RAM write
                patternTable[register_PPUADDR] = value;
            }
            else if (register_PPUADDR < 0x3000)
            {
                int vramAddress = (register_PPUADDR - 0x2000) & 0x07FF;
                nameTable[vramAddress] = value;
                Console.WriteLine($"nameTable[{vramAddress}] = {value}");
            }
            else if (register_PPUADDR >= 0x3F00 && register_PPUADDR <= 0x3F1F)
            {
                int paletteIndex = (register_PPUADDR - 0x3F00) % 32;
                paletteTable[paletteIndex] = value;
                Console.WriteLine($"paletteTable[{paletteIndex}] = {value:X2}");
            }
            //PPUADDR increment logic:
            if ((register_PPUCTRL & 0x04) == 0)
            {
                register_PPUADDR++;
            }
            else
            {
                register_PPUADDR += 32;
            }
        }
    }
}
