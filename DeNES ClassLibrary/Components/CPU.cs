using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DeNES_ClassLibrary.Components
{
    public class CPU
    {
        PPU ppu;
        Memory memory;
        int programCounter;
        bool C; //Carry
        bool Z; //Zero
        bool I; //Interrupt Disable
        bool D; //Decimal
        bool V; //Overflow
        bool N; //Negative

        byte A;
        byte X;
        byte Y;
        byte SP; //Stack pointer
        public CPU(Memory memo)
        {
            memory = memo;
            programCounter = (ushort)(memory.Read(0xFFFC) | (memory.Read(0xFFFD) << 8)); //NES starts at this address
            SP = 0xFD; //NES default value
            C = false;
            Z = false;
            I = false;
            D = false;
            V = false;
            N = false;
        }
        public PPU Ppu { set => ppu = value; }

        public int instruction()
        {
            int cycle = 0;
            byte opcode = memory.Read((ushort)(programCounter++));
            Console.WriteLine("Opcode: " + opcode+"(0x"+opcode.ToString("X2")+")");
            Console.WriteLine($"PC: {programCounter:X4}");
            switch (opcode)
            {
                #region ACCESS
                #region LDA
                case 0xA9: //LDA #Immediate (Example.: A9 64 -> A = 64)
                    A = memory.Read((ushort)(programCounter++));
                    SetZN(A);

                    Console.WriteLine($"Executing LDA #Immediate: A = {A}");
                    cycle = 2;
                    break;
                case 0xA5: //LDA Zero Page (Example.: A5 $ZZ -> A = $ZZ)
                    byte ldaZP_address = memory.Read((ushort)(programCounter++));

                    A = memory.Read(ldaZP_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Zero Page: A = {A} [{ldaZP_address:X2}]");
                    cycle = 3;
                    break;
                case 0xB5: //LDA Zero Page,X (Example.: ZP + X)
                    byte ldaZPX_ZpAddress = memory.Read((ushort)(programCounter++));
                    byte ldaZPX_address = (byte)((ldaZPX_ZpAddress + X) & 0xFF); // <= $FF = Zero Page

                    A = memory.Read(ldaZPX_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Zero Page,X: A = {A:X2} [{ldaZPX_address:X2}]");
                    cycle = 4;
                    break;
                case 0xAD: //LDA Absolute (Example.: AD 34 12 -> $1234
                    byte ldaA_low = memory.Read((ushort)(programCounter++));
                    byte ldaA_high = memory.Read((ushort)(programCounter++));
                    ushort ldaA_address = (ushort)((ldaA_high << 8) | ldaA_low); //16 bit

                    A = memory.Read(ldaA_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Absolute: A = {A} [{ldaA_address:X4}]");
                    cycle = 4;
                    break;
                case 0xBD: //LDA Absolute,X ($1234 + X)
                    byte ldaAX_low = memory.Read((ushort)(programCounter++));
                    byte ldaAX_high = memory.Read((ushort)(programCounter++));
                    ushort ldaAX_baseAddress = (ushort)((ldaAX_high << 8) | ldaAX_low);
                    ushort ldaAX_address = (ushort)(ldaAX_baseAddress + X);

                    A = memory.Read(ldaAX_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Absolute,X: A = {A:X2} [{ldaAX_address:X4}]");
                    if (PageCrossed(ldaAX_baseAddress, ldaAX_address))
                    {
                        cycle = 5;
                    }
                    else
                    {
                        cycle = 4;
                    }
                    break;
                case 0xB9: //LDA Absolute,Y
                    byte ldaAY_low = memory.Read((ushort)(programCounter++));
                    byte ldaAY_high = memory.Read((ushort)(programCounter++));
                    ushort ldaAY_baseAddress = (ushort)((ldaAY_high << 8) | ldaAY_low);
                    ushort ldaAY_address = (ushort)(ldaAY_baseAddress + Y);

                    A = memory.Read(ldaAY_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA Absolute,Y: A = {A:X2} [{ldaAY_address:X4}]");
                    if (PageCrossed(ldaAY_baseAddress, ldaAY_address))
                    {
                        cycle = 5;
                    }
                    else
                    {
                        cycle = 4;
                    }
                    break;
                case 0xA1: //LDA (Indirect,X) (Example.: $20 + X -> $24)
                    byte ldaIX_zp = memory.Read((ushort)(programCounter++));
                    byte ldaIX_baseAddress = (byte)(ldaIX_zp + X); //Wrap around 
                    byte ldaIX_low = memory.Read((ldaIX_baseAddress));
                    byte ldaIX_high = memory.Read((byte)(ldaIX_baseAddress + 1)); //Wrap around $FF + 1 would be wrong
                    ushort ldaIX_address = (ushort)((ldaIX_high << 8) | ldaIX_low);
                    

                    A = memory.Read(ldaIX_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA (Indirect,X): A = {A:X2} [{ldaIX_address:X4}]");
                    cycle = 6;
                    break;
                case 0xB1: //LDA (Indirect),Y
                    byte ldaIY_zp = memory.Read((ushort)(programCounter++));
                    byte ldaIY_low = memory.Read(ldaIY_zp);
                    byte ldaIY_high = memory.Read((byte)((ldaIY_zp + 1)));
                    ushort ldaIY_baseAddress = (ushort)((ldaIY_high << 8) | ldaIY_low);
                    ushort ldaIY_address = (ushort)(ldaIY_baseAddress + Y);

                    A = memory.Read(ldaIY_address);
                    SetZN(A);

                    Console.WriteLine($"Executing LDA (Indirect),Y: A = {A:X2} [{ldaIY_address:X4}]");
                    if (PageCrossed(ldaIY_baseAddress, ldaIY_address))
                    {
                        cycle = 6;
                    }
                    else
                    {
                        cycle = 5;
                    }
                    break;
                #endregion
                #region STA
                case 0x8d: //STA Absolute (STORE A)
                    byte low = memory.Read((ushort)(programCounter++));
                    byte high = memory.Read((ushort)(programCounter++));
                    ushort address = (ushort)((high << 8) | low); //16 bit
                    WriteToMemory(address, A);
                    Console.WriteLine("Executing STA Absolute: Store A");
                    cycle = 4;
                    break;
                case 0x9D: // STA Absolute,X
                    byte staax_low = memory.Read((ushort)(programCounter++));
                    byte staax_high = memory.Read((ushort)(programCounter++));
                    ushort staax_addr = (ushort)(((staax_high << 8) | staax_low) + X);
                    WriteToMemory(staax_addr, A);
                    cycle = 5;
                    Console.WriteLine($"Executing STA Absolute,X: A = {A:X2} → [{staax_addr:X4}]");
                    break;
                case 0x81: //STA Indirect X (STORE A) ZP + X => 16 bit address
                    byte stax_zeroPage = memory.Read((ushort)programCounter++);
                    byte stax_low = memory.Read((byte)((stax_zeroPage + X) & 0xFF));
                    byte stax_high = memory.Read((byte)((stax_zeroPage + X + 1) & 0xFF));
                    ushort stax_address = (ushort)((stax_high << 8) | stax_low);
                    WriteToMemory(stax_address, A);
                    Console.WriteLine("Executing STA Indirect X: Store A");
                    cycle = 6;
                    break;
                case 0x91: //STA Indirect Y (STORE A) ZP => 16 bit address + Y
                    byte stay_zeroPage = memory.Read((ushort)programCounter++);
                    byte stay_low = memory.Read(stay_zeroPage);
                    byte stay_high = memory.Read((byte)((stay_zeroPage + 1) & 0xFF));
                    ushort stay_address = (ushort)((stay_high << 8) | stay_low);
                    WriteToMemory((ushort)(stay_address+Y), A);
                    Console.WriteLine($"Executing STA Indirect Y: Store A {stay_address + Y:X4} = {A:X2}");
                    cycle = 6;
                    break;
                case 0x85: // STA Zero Page
                    byte stazp_addr = memory.Read((ushort)(programCounter++));
                    WriteToMemory(stazp_addr, A);
                    cycle = 3;
                    Console.WriteLine($"Executing STA Zero Page: A = {A:X2} → [{stazp_addr:X2}]");
                    break;
                #endregion
                #region LDX
                case 0xA2: //LDX - Load X
                    X = memory.Read((ushort)(programCounter++));
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    Console.WriteLine("Executing LDX Immediate: Load X");
                    cycle = 2;
                    break;
                case 0xA6: // LDX Zero Page
                    byte ldxzp_addr = memory.Read((ushort)(programCounter++));
                    X = memory.Read(ldxzp_addr);
                    Z = (X == 0);
                    N = (X & 0x80) != 0;
                    cycle = 3;
                    Console.WriteLine($"Executing LDX Zero Page: X = {X:X2} from [{ldxzp_addr:X2}]");
                    break;
                #endregion
                #region STX
                case 0x8E: //STX Absolute (STORE X)
                    byte x_low = memory.Read((ushort)(programCounter++));
                    byte x_high = memory.Read((ushort)(programCounter++));
                    ushort x_address = (ushort)((x_high << 8) | x_low); //16 bit
                    WriteToMemory(x_address, X);
                    Console.WriteLine("Executing STX Absolute: Store X");
                    cycle = 4;
                    break;
                case 0x86: // STX Zero Page
                    {
                        byte addr = memory.Read((ushort)(programCounter++));
                        memory.Write(addr, X);
                        cycle = 3;
                        Console.WriteLine($"Executing STX Zero Page: X = {X:X2} → [{addr:X2}]");
                        break;
                    }
                #endregion
                #region LDY
                case 0xA0: //LDY - Load Y
                    Y = memory.Read((ushort)(programCounter++));
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    Console.WriteLine("Executing LDY Immediate: Load Y");
                    cycle = 2;
                    break;
                #endregion
                #region STY
                case 0x8C: //STY Absolute (STORE Y)
                    byte y_low = memory.Read((ushort)(programCounter++));
                    byte y_high = memory.Read((ushort)(programCounter++));
                    ushort y_address = (ushort)((y_high << 8) | y_low); //16 bit
                    WriteToMemory(y_address, Y);
                    Console.WriteLine("Executing STY Absolute: Store Y");
                    cycle = 4;
                    break;
                case 0x84: // STY Zero Page
                    byte styzp_address = memory.Read((ushort)(programCounter++));
                    memory.Write(styzp_address, Y);
                    cycle = 3;
                    Console.WriteLine($"Executing STY Zero Page: Y = {Y:X2} → [{styzp_address:X2}]");
                    break;
                #endregion
                #endregion
                #region TRANSFER
                //TRANSFER:
                case 0xAA: //TAX: Transfer A to X
                    X = A;
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TAX: Transfer A to X");
                    break;
                case 0xA8: //TAY: Transfer A to Y
                    Y = A;
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TAY: Transfer A to Y");
                    break;
                case 0xBA: //TSX: Transfer SP to X
                    X = SP;
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TSX: Transfer SP to X");
                    break;
                case 0x8A: //TXA: Transfer X to A
                    A = X;
                    Z = (A == 0);
                    N = (A & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TXA: Transfer X to A");
                    break;
                case 0x9A: //TXS: Transfer X to SP
                    SP = X;
                    cycle = 2;
                    Console.WriteLine("Executing TXS: Transfer X to SP");
                    break;
                case 0x98: //TYA: Transfer Y to A
                    A = Y;
                    Z = (A == 0);
                    N = (A & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing TYA: Transfer Y to A");
                    break;
                #endregion
                #region ARITHMETIC
                //ARITHMETIC:
                case 0x69: // ADC Immediate
                    byte adc_value = memory.Read((ushort)(programCounter++));
                    int sum = A + adc_value + (C ? 1 : 0);
                    C = sum > 0xFF;
                    byte result = (byte)(sum & 0xFF);

                    V = ((A ^ result) & (adc_value ^ result) & 0x80) != 0;
                    A = result;
                    Z = (A == 0);
                    N = (A & 0x80) != 0;
                    cycle = 2;
                    Console.WriteLine($"Executing ADC Immediate: A + {adc_value:X2} + C → {A:X2}");
                    break;
                case 0xEE: // INC Absolute
                    byte inc_low = memory.Read((ushort)(programCounter++));
                    byte inc_high = memory.Read((ushort)(programCounter++));
                    ushort inc_addr = (ushort)((inc_high << 8) | inc_low);
                    byte inc_value = memory.Read(inc_addr);
                    inc_value++;
                    memory.Write(inc_addr, inc_value);
                    Z = (inc_value == 0);
                    N = (inc_value & 0x80) != 0;
                    cycle = 6;
                    Console.WriteLine($"Executing INC Absolute: {inc_addr:X4} = {inc_value:X2}");
                    break;
                case 0xE8: //INX Increment X
                    X = (byte)(X+1);
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing INX: Increment X");
                    break;
                case 0xC8: //INY Increment Y
                    Y = (byte)(Y + 1);
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing INY: Increment Y");
                    break;
                case 0xC6: //DEC Decrement Memory Zero Page
                    byte zeroPage_Address = memory.Read((ushort)(programCounter++));
                    byte value = memory.Read(zeroPage_Address);
                    value-=1;
                    memory.Write(zeroPage_Address, value);
                    Z = (value == 0);
                    N = (value & 0x80) != 0;
                    cycle = 5;
                    Console.WriteLine($"Executing DEC Zero Page address: {zeroPage_Address:X2}");
                    break;
                case 0xD6: // DEC Zero Page,X
                    byte deczpx_baseAddr = memory.Read((ushort)(programCounter++));
                    byte deczpx_addr = (byte)((deczpx_baseAddr + X) & 0xFF); // Zero-page wraparound
                    byte deczpx_value = memory.Read(deczpx_addr);
                    deczpx_value--;
                    memory.Write(deczpx_addr, deczpx_value);
                    Z = (deczpx_value == 0);
                    N = (deczpx_value & 0x80) != 0;
                    cycle = 6;
                    Console.WriteLine($"Executing DEC Zero Page,X: [{deczpx_addr:X2}] → {deczpx_value:X2}");
                    break;
                case 0xCA: //DEX Decrement X
                    X = (byte)(X - 1);
                    Z = (X == 0);
                    N = (X & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing DEX: Decrement X");
                    break;
                case 0x88: //DEY Decrement Y
                    Y = (byte)(Y - 1);
                    Z = (Y == 0);
                    N = (Y & 0x80) != 0; //7th bit
                    cycle = 2;
                    Console.WriteLine("Executing DEY: Decrement Y");
                    break;
                #endregion
                #region SHIFT
                //SHIFT
                case 0x4A: // LSR A Accumulator
                    {
                        C = (A & 0x01) != 0;
                        A >>= 1;
                        Z = (A == 0);
                        N = false;
                        cycle = 2;
                        Console.WriteLine($"Executing LSR Accumulator: A = {A:X2}, C = {C}");
                        break;
                    }
                #endregion
                #region BITWISE
                //BITWISE:
                case 0x29: // AND Immediate
                    byte and_value = memory.Read((ushort)(programCounter++));
                    A &= and_value;
                    Z = (A == 0);
                    N = (A & 0x80) != 0;
                    cycle = 2;
                    Console.WriteLine($"Executing AND Immediate: A &= {and_value:X2} → {A:X2}");
                    break;
                #endregion
                #region COMPARE
                //COMPARE:
                case 0xC9: // CMP Immediate
                    {
                        byte cmp_value = memory.Read((ushort)(programCounter++));
                        byte cmp_result = (byte)(A - cmp_value);
                        C = A >= cmp_value;
                        Z = (cmp_result == 0);
                        N = (cmp_result & 0x80) != 0;
                        cycle = 2;
                        Console.WriteLine($"Executing CMP Immediate: A = {A:X2}, value = {cmp_value:X2}, C = {C}, Z = {Z}, N = {N}");
                        break;
                    }
                #endregion
                #region BRANCH
                //BRANCH:
                case 0x90: //BCC Branch if Carry Clear
                    sbyte bcc_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if(!C)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bcc_offset;
                        cycle = 3;
                        if((prevPC & 0xFF00) != (programCounter & 0xFF00)){ //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BCC: Branch if Carry Clear");
                    break;
                case 0xB0: //BCS Branch if Carry Set
                    sbyte offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (C)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BCS: Branch if Carry Set");
                    break;
                case 0xF0: //BEQ Branch if Equal (Z true)
                    sbyte beq_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (Z)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += beq_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BEQ: Branch if Equal");
                    break;
                case 0xD0: //BNE Branch if Not Equal (Z false)
                    sbyte bne_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (!Z)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bne_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BNE: Branch if Not Equal");
                    break;
                case 0x10: //BPL Branch if Plus (N false)
                    sbyte bpl_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (!N)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bpl_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BPL: Branch if Plus");
                    break;
                case 0x30: //BMI Branch if Minus (N true)
                    sbyte bmi_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (N)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bmi_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BMI: Branch if Minus");
                    break;
                case 0x50: //BVC Branch if Overflow Clear (V false)
                    sbyte bvc_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (!V)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bvc_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BVC: Branch if Overflow Clear");
                    break;
                case 0x70: //BVS Branch if Overflow Set (V true)
                    sbyte bvs_offset = (sbyte)memory.Read((ushort)programCounter++);
                    if (V)
                    {
                        ushort prevPC = (ushort)programCounter;
                        programCounter += bvs_offset;
                        cycle = 3;
                        if ((prevPC & 0xFF00) != (programCounter & 0xFF00))
                        { //Page crossed
                            cycle++;
                        }
                    }
                    else
                    {
                        cycle = 2;
                    }
                    Console.WriteLine("Executing BVS: Branch if Overflow Set");
                    break;
                #endregion
                #region JUMP
                //JUMP:
                case 0x4C: //JMP Absolute
                    byte jmp_low = memory.Read((ushort)(programCounter++));
                    byte jmp_high = memory.Read((ushort)(programCounter++));
                    ushort jmp_address = (ushort)((jmp_high << 8) | jmp_low); //16 bit
                    programCounter = jmp_address;

                    Console.WriteLine("Executing JMP Absolute");
                    cycle = 3;
                    break;
                case 0x20: //JSR Absolute - Store and set
                    byte jsr_low = memory.Read((ushort)(programCounter++));
                    byte jsr_high = memory.Read((ushort)(programCounter++));

                    ushort return_address = (ushort)(programCounter - 1);
                    PushWord(return_address);

                    programCounter = (ushort)((jsr_high << 8) | jsr_low);
                    Console.WriteLine("Executing JSR Absolute: Jump to subroutine");
                    cycle = 6;
                    break;
                case 0x60: //RTS Implied - Return to Subroutine
                    programCounter = PopWord() + 1;
                    Console.WriteLine("Executing RTS Implied: Return to subroutine");
                    cycle = 6;
                    break;
                #endregion
                #region STACK
                #endregion
                #region FLAGS
                //FLAGS:
                case 0x18: //CLC (Clear Carry)
                    C = false;
                    Console.WriteLine("Executing CLC: Clear Carry");
                    cycle = 2;
                    break;
                case 0x38: //SEC (Set Carry)
                    C = true;
                    Console.WriteLine("Executing SEC: Set Carry");
                    cycle = 2;
                    break;

                case 0x58: //CLI (Clear Interrupt Disable)
                    I = false;
                    Console.WriteLine("Executing CLI: Clear Interrupt Disable");
                    cycle = 2;
                    break;
                case 0x78: //SEI (Set Interrupt Disable)
                    I = true;
                    Console.WriteLine("Executing SEI: Set Interrupt Disable");
                    cycle = 2;
                    break;

                case 0xD8: //CLD (Clear Decimal)
                    D = false;
                    Console.WriteLine("Executing CLD: Clear Decimal");
                    cycle = 2;
                    break;
                case 0xF8: //SED (Set Decimal)
                    D = true;
                    Console.WriteLine("Executing SED: Set Decimal");
                    cycle = 2;
                    break;

                case 0xB8: //CLV (Clear Overflow)
                    V = false;
                    Console.WriteLine("Executing CLV: Clear Overflow");
                    cycle = 2;
                    break;
                #endregion
                #region OTHER/UNOFFICAL
                //Other:
                case 0xEA: //NOP (No Operation)
                    Console.WriteLine("Executing NOP: No Operation");
                    cycle = 2;
                    break;
                case 0x2B: // ANC (Unofficial): AND + set Carry = bit 7
                    byte anc_value = memory.Read((ushort)(programCounter++));
                    A &= anc_value;
                    C = (A & 0x80) != 0;
                    Z = (A == 0);
                    N = (A & 0x80) != 0;
                    cycle = 2;
                    Console.WriteLine($"Executing ANC (unofficial): A &= {anc_value:X2} → {A:X2}, C = {C}");
                    break;
                //PLACE:
                case 0x0A: // ASL A (Accumulator)
                    {
                        C = (A & 0x80) != 0;   // Save bit 7 into Carry
                        A <<= 1;
                        Z = (A == 0);
                        N = (A & 0x80) != 0;
                        cycle = 2;
                        Console.WriteLine($"Executing ASL A: A = {A:X2}, C = {C}");
                        break;
                    }
                case 0x68: // PLA
                    {
                        A = PopByte();
                        Z = (A == 0);
                        N = (A & 0x80) != 0;
                        cycle = 4;
                        Console.WriteLine($"Executing PLA: Pulled A = {A:X2}");
                        break;
                    }
                case 0x14: // Unofficial NOP Zero Page,X
                    {
                        byte baseAddr = memory.Read((ushort)(programCounter++));
                        byte addr = (byte)((baseAddr + X) & 0xFF); // zero-page wraparound
                        _ = memory.Read(addr); // discard value
                        cycle = 4;
                        Console.WriteLine($"Executing NOP (unofficial 0x14) at [{addr:X2}]");
                        break;
                    }
                case 0x48: // PHA
                    {
                        PushByte(A);
                        cycle = 3;
                        Console.WriteLine($"Executing PHA: Pushed A = {A:X2}");
                        break;
                    }
                case 0x65: // ADC Zero Page
                    {
                        byte adczp_addr = memory.Read((ushort)(programCounter++));
                        byte adczp_value = memory.Read(adczp_addr);
                        int adczp_sum = A + adczp_value + (C ? 1 : 0);

                        C = adczp_sum > 0xFF;
                        byte adczp_result = (byte)(adczp_sum & 0xFF);
                        V = ((A ^ adczp_result) & (adczp_value ^ adczp_result) & 0x80) != 0;

                        A = adczp_result;
                        Z = (A == 0);
                        N = (A & 0x80) != 0;

                        cycle = 3;
                        Console.WriteLine($"Executing ADC Zero Page: A + {adczp_value:X2} + C → {A:X2}");
                        break;
                    }
                case 0x12: // Unofficial NOP (Indirect)
                    {
                        byte zpn = memory.Read((ushort)(programCounter++));
                        byte addr_lo = memory.Read((byte)((zpn + X) & 0xFF));
                        byte addr_hi = memory.Read((byte)((zpn + X + 1) & 0xFF));
                        ushort effectiveAddr = (ushort)((addr_hi << 8) | addr_lo);
                        _ = memory.Read(effectiveAddr); // discard result
                        cycle = 5;
                        Console.WriteLine($"Executing unofficial NOP 0x12: (Indirect,X) addr = {effectiveAddr:X4}");
                        break;
                    }
                case 0x24: // BIT Zero Page
                    {
                        byte zpb = memory.Read((ushort)(programCounter++));
                        byte v = memory.Read(zpb);
                        Z = (A & v) == 0;
                        V = (v & 0x40) != 0;
                        N = (v & 0x80) != 0;
                        cycle = 3;
                        Console.WriteLine($"Executing BIT ZeroPage: A&[{zpb:X2}] -> Z={Z}, V={V}, N={N}");
                        break;
                    }
                case 0x2C: // BIT Absolute
                    {
                        byte lo = memory.Read((ushort)(programCounter++));
                        byte hi = memory.Read((ushort)(programCounter++));
                        ushort addr = (ushort)((hi << 8) | lo);
                        byte v = memory.Read(addr);
                        Z = (A & v) == 0;
                        V = (v & 0x40) != 0;
                        N = (v & 0x80) != 0;
                        cycle = 4;
                        Console.WriteLine($"Executing BIT Absolute: A&[{addr:X4}] -> Z={Z}, V={V}, N={N}");
                        break;
                    }
                #endregion

                default:
                    Console.WriteLine("Unknown opcode: " + opcode + "(0x" + opcode.ToString("X2") + ")");
                    break;
                }
            return cycle;
        }
        void SetZN(byte value)
        {
            Z = (value == 0);
            N = (value & 0x80) != 0; //10000000
        }
        bool PageCrossed(ushort baseAddress, ushort address)
        {
            return (baseAddress & 0xFF00) != (address & 0xFF00);
        }
        void WriteToMemory(ushort address, byte value)
        {
            Console.WriteLine($"WriteToMemory called: addr=0x{address:X4} data=0x{value:X2}");
            if (address >= 0x2000 && address <= 0x3FFF)
            {
                // $2000–$2007 tükrözve 8 byte-onként
                ushort reg = (ushort)(address & 0x2007);
                switch (reg)
                {
                    case 0x2000: 
                        ppu.SETPPUCTRL(value); 
                        break;
                    case 0x2001: 
                        ppu.SETMASK(value); 
                        break;
                    case 0x2006: 
                        ppu.SETPPUADDR(value);
                        break;
                    case 0x2007: 
                        ppu.WritePPUDATA(value); 
                        break;
                    default:
                        Console.WriteLine($"PPU Unsupported register: {reg:X4} at address: {address:X4})");
                        break;
                }
                return;
            }

            memory.Write(address, value);
        }
        //STACK METHODS:
        void PushByte(byte value)
        {
            memory.Write((ushort)(0x0100 + SP), value);
            SP--;
        }
        void PushWord(ushort value)
        {
            PushByte((byte)((value >> 8) & 0xFF));
            PushByte((byte)(value & 0xFF));
        }
        byte PopByte()
        {
            SP++;
            return memory.Read((ushort)(0x0100 + SP));
        }
        ushort PopWord()
        {
            byte low = PopByte();
            byte high = PopByte();
            return (ushort)((high << 8) | low);
        }
    }
}
