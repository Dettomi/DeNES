﻿using System;
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

        byte[] nameTable = new byte[4096]; //Should be 4kb
        byte[] oam = new byte[256];
        byte[] paletteTable = new byte[32];

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

            if (ppu_tick % totalCyclesPerFrame == 0)
            {
                DrawNameTable();
            }
            
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
        public byte READPPUSTATUS()
        {
            byte r = register_PPUSTATUS;
            register_PPUSTATUS &= 0x7F; // Bit 7 clear
            latch_PPUADDR = false;
            return r;
        }
        public void SETPPUADDR(byte value) //$2006 SETS VRAM ADDRESS 2 WRITE (2x8 bit)
        {
            if (!latch_PPUADDR) // High byte
            {
                register_PPUADDR = (ushort)((value & 0x3F) << 8); //Only 14 bit address with nes, first 2 bits not used
                latch_PPUADDR = true;
            }
            else // Low byte
            {
                register_PPUADDR = (ushort)(register_PPUADDR | value);
                latch_PPUADDR = false;
            }
        }
        public void WritePPUDATA(byte value)
        {
            Console.WriteLine($"WritePPUDATA: addr=0x{register_PPUADDR:X4} → value=0x{value:X2}");
            if ((register_PPUSTATUS & 0x80) == 0)
            {
                // block writes outside VBlank
                Console.WriteLine($"⚠ REJECTED VRAM write outside VBlank: addr={register_PPUADDR:X4}");
                return;
            }
            if (register_PPUADDR < 0x2000)
            {
                // CHR-RAM write
                patternTable[register_PPUADDR] = value;
            }
            else if (register_PPUADDR < 0x3000)
            {
                // Nametable write with vertical mirroring (2KB wraparound)
                int vramAddress = (register_PPUADDR - 0x2000) & 0x07FF;
                nameTable[vramAddress] = value;
                Console.WriteLine($"name table[{vramAddress}] = {value}");
            }

            register_PPUADDR++;
        }
    }
}
