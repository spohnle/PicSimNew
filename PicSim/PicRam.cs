using System;
using System.Collections.Generic;
using System.Text;

namespace PicSim
{
    class PicRam
    {

        /// <summary>
        /// Byte-Array zur Simulierung des Rams
        /// </summary>
        private byte[] ram;

        /// <summary>
        /// Konstruktur:
        /// Initialisiert die Größe des Rams
        /// </summary>
        public PicRam()
        {
            ram = new byte[256];

        }


        /// <summary>
        /// Schreibt einen Wert an eine Adresse im Ram.
        /// Die Aktivierung der Bänke (0 bzw. 1) wird berücksichtigt
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="wert">neuer Wert</param>
        public void write(int adr, byte wert)
        {
            adr = adjustAdr(adr);

            writeDirect(adr, wert);
        }
        /// <summary>
        /// Schreib einen Wert an eine Adresse im Ram unabhängig der aktivierten Bank
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="wert">neuer Wert</param>
        public void writeDirect(int adr, byte wert)
        {
            if (adr == 0) adr = ram[0x04];      // Indirekte Adressierung

            ram[adr] = wert;
        }

        /// <summary>
        /// Schreibt ein Flag (Bit) an eine Adresse im Ram
        /// Die Aktivierung der Bänke (0 bzw. 1) wird berücksichtigt
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="bit">Bit</param>
        /// <param name="wert">Wert</param>
        public void writeFlag(int adr, byte bit, bool wert)
        {
            adr = adjustAdr(adr);

            writeFlagDirect(adr, bit, wert);
        }
        /// <summary>
        /// Schreib ein Flag an eine Speicheradresse unabhängig der aktivierten Bank
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="bit">Bit</param>
        /// <param name="wert">Wert</param>
        public void writeFlagDirect(int adr, byte bit, bool wert)
        {
            if (adr == 0) adr = ram[0x04];      // Indirekte Adressierung

            if (((ram[adr] / Math.Pow(2, bit)) % 2) >= 1)
                ram[adr] = (byte)(ram[adr] - Math.Pow(2, bit));
            if (wert) ram[adr] = (byte)(ram[adr] + Math.Pow(2, bit));
        }

        /// <summary>
        /// Gibt den Wert einer Adresse im Ram zurück
        /// Schaltet automatisch auf andere Bank
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <returns>Wert im Ram</returns>
        public byte read(int adr)
        {
            adr = adjustAdr(adr);

            return readDirect(adr);
        }
        /// <summary>
        /// Gibt den Wert einer Adresse im Ram unabhängig der aktivierten Bank zurück
        /// </summary>
        /// <param name="adr"></param>
        /// <returns></returns>
        public byte readDirect(int adr)
        {
            if (adr == 0) adr = ram[0x04];          // Indirekte Adressierung

            return ram[adr];
        }

        /// <summary>
        /// Gibt den wert eines Flags (Bits) an der Adresse im Ram zurück
        /// Schaltet automatisch auf Bank 1
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="bit">Bit</param>
        /// <returns>Wert</returns>
        public bool readFlag(int adr, byte bit)
        {
            adr = adjustAdr(adr);

            return readFlagDirect(adr, bit);
        }
        /// <summary>
        /// Liest ein Flag aus einer Ram Adresse, unabhängig von der Bankaktivität
        /// </summary>
        /// <param name="adr"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public bool readFlagDirect(int adr, byte bit)
        {
            if (adr == 0) adr = ram[0x04];          // Indirekte Adressierung

            int ret = (int)((ram[adr] / Math.Pow(2, bit)) % 2);
            if (ret >= 1) return true; else return false;
        }

        /// <summary>
        /// Ermöglicht das Schreiben an eine Adresse über den Name des Registers
        /// </summary>
        /// <param name="name">Name des Registers</param>
        /// <param name="wert">neuer Wert</param>
        public void write(string name, byte wert)
        {
            writeDirect(getAdrByName(name), wert);
        }
        /// <summary>
        /// Ermöglicht das Schreiben eines Flags über den Flagnamen
        /// </summary>
        /// <param name="name">Flagname</param>
        /// <param name="wert">neuer Wert</param>
        public void writeFlag(string name, bool wert)
        {
            writeFlagDirect(getBitByName(name, true), getBitByName(name, false), wert);
        }
        /// <summary>
        /// Ermöglicht das Abfragen eines Registers über den Namen
        /// </summary>
        /// <param name="name">Name des Registers</param>
        /// <returns></returns>
        public byte read(string name)
        {
            return readDirect(getAdrByName(name));
        }
        /// <summary>
        /// Ermöglicht das Abfragen eines Bits über dessen Namen
        /// </summary>
        /// <param name="name">Name des Bits</param>
        /// <returns></returns>
        public bool readFlag(string name)
        {
            return readFlagDirect(getBitByName(name, true), getBitByName(name, false));
        }


        /// <summary>
        /// Gibt den Inhalt an der Adresse in der angebenen Bank zurück
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="bank">Bank</param>
        /// <returns></returns>
        public byte readBank(int adr, byte bank)
        {
            if ((bank == 1) && (checkAdr(adr))) adr = adr + 128;

            return ram[adr];
        }

        /// <summary>
        /// Passt die Zugriffsadresse bei Bedarf an.
        /// -> Wenn ein Zugriff auf Bank 1 auf bestimmte Register erfolgt, welche nicht dieselben Register wie in Bank 0 sind
        /// </summary>
        /// <param name="adr">anzupassende Adresse</param>
        /// <returns></returns>
        public byte adjustAdr(int adr)
        {
            if ((getPage() == 1) && (checkAdr(adr))) return (byte)(adr + 128);
            return (byte)adr;
        }

        /// <summary>
        /// Gibt true zurück, wenn die Adresse in Bank 1 nicht dasselbe Register ist wie in Bank 0
        /// </summary>
        /// <param name="adr"></param>
        /// <returns></returns>
        public bool checkAdr(int adr)
        {
            short[] bank1regs = { 1, 5, 6, 8, 9 };
            for (int i = 0; i < bank1regs.Length; i++)
                if (adr == bank1regs[i]) return true;
            return false;
        }

        /// <summary>
        /// Resettet das Ram (alle Register = 0)
        /// </summary>
        public void reset()
        {
            for (int i = 0; i < ram.Length; i++) write(i, 0);  // Alles auf 0
        }

        /// <summary>
        /// Gibt die momentan aktive Bank zurück
        /// </summary>
        public byte getPage()
        {
            return (byte)((ram[0x03] / 32) % 2);
        }

        /// <summary>
        /// Gibt die Adresse des Registers zurück
        /// </summary>
        /// <param name="name">Name des Registers</param>
        /// <returns></returns>
        public int getAdrByName(string name)
        {
            switch (name)
            {
                case ("TMR0"): return 0x01; break;
                case ("STATUS"): return 0x03; break;
                case ("FSR"): return 0x04; break;
                case ("PORTA"): return 0x05; break;
                case ("PORTB"): return 0x06; break;
                case ("EEDATA"): return 0x08; break;
                case ("EEADR"): return 0x09; break;
                case ("INTCON"): return 0x0B; break;
                case ("OPTION_REG"): return 0x81; break;
                case ("TRISA"): return 0x85; break;
                case ("TRISB"): return 0x86; break;
                case ("EECON1"): return 0x88; break;
                case ("EECON2"): return 0x89; break;
                default:
                    {
                        System.Console.WriteLine("Fehler: Adresse NameCalling nicht vorhanden: " + name);
                        return 0x00;
                    }
            }
        }
        /// <summary>
        /// Gibt die Bitnummer eines Bits im Register zurück
        /// </summary>
        /// <param name="name">Name des Bits</param>
        /// <param name="adr">True -> Adresse, False -> Bitnummer</param>
        /// <returns></returns>
        public byte getBitByName(string name, bool adr)
        {
            switch (name)
            {
                case ("C"): if (adr) return 0x03; else return 0; break;     // Status
                case ("DC"): if (adr) return 0x03; else return 1; break;
                case ("Z"): if (adr) return 0x03; else return 2; break;
                case ("!PD"): if (adr) return 0x03; else return 3; break;
                case ("!TO"): if (adr) return 0x03; else return 4; break;
                case ("RP0"): if (adr) return 0x03; else return 5; break;
                case ("RP1"): if (adr) return 0x03; else return 6; break;
                case ("IRP"): if (adr) return 0x03; else return 7; break;
                case ("RBIF"): if (adr) return 0x0B; else return 0; break;  // INTCON
                case ("INTF"): if (adr) return 0x0B; else return 1; break;
                case ("T0IF"): if (adr) return 0x0B; else return 2; break;
                case ("RBIE"): if (adr) return 0x0B; else return 3; break;
                case ("INTE"): if (adr) return 0x0B; else return 4; break;
                case ("T0IE"): if (adr) return 0x0B; else return 5; break;
                case ("EEIE"): if (adr) return 0x0B; else return 6; break;
                case ("GIE"): if (adr) return 0x0B; else return 7; break;
                case ("PS0"): if (adr) return 0x81; else return 0; break;   // OPTION
                case ("PS1"): if (adr) return 0x81; else return 1; break;
                case ("PS2"): if (adr) return 0x81; else return 2; break;
                case ("PSA"): if (adr) return 0x81; else return 3; break;
                case ("T0SE"): if (adr) return 0x81; else return 4; break;
                case ("T0CS"): if (adr) return 0x81; else return 5; break;
                case ("INTEDG"): if (adr) return 0x81; else return 6; break;
                case ("!RBPU"): if (adr) return 0x81; else return 7; break;
                case ("RA0"): if (adr) return 0x05; else return 0; break;   // PortA
                case ("RA1"): if (adr) return 0x05; else return 1; break;
                case ("RA2"): if (adr) return 0x05; else return 2; break;
                case ("RA3"): if (adr) return 0x05; else return 3; break;
                case ("RA4"): if (adr) return 0x05; else return 4; break;
                case ("RB0"): if (adr) return 0x06; else return 0; break;   // PortB
                case ("RB1"): if (adr) return 0x06; else return 1; break;
                case ("RB2"): if (adr) return 0x06; else return 2; break;
                case ("RB3"): if (adr) return 0x06; else return 3; break;
                case ("RB4"): if (adr) return 0x06; else return 4; break;
                case ("RB5"): if (adr) return 0x06; else return 5; break;
                case ("RB6"): if (adr) return 0x06; else return 6; break;
                case ("RB7"): if (adr) return 0x06; else return 7; break;
                default:
                    {
                        System.Console.WriteLine("Fehler: Bit NameCalling nicht vorhanden: " + name);
                        return 0;
                    }
            }
        }
    }
}
