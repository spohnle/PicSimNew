using System;
using System.Collections.Generic;
using System.Text;

namespace PicSim
{
    /// <summary>
    /// Klasse "Pic" - Simuliert die PIC Umgebung
    /// </summary>
    class Pic
    {

        /// <summary>
        /// Ram: 8-Bit Array - enhält die Registerwerte
        /// </summary>
        public PicRam ram;

        /// <summary>
        /// Rom: Array - enthält die Befehlsfolge
        /// </summary>
        public PicRom rom;

        /// <summary>
        /// Stack des ProgramCounters
        /// </summary>
        public PicStack stack;

        /// <summary>
        /// W-Register: 8-Bit
        /// </summary>
        private byte regw;

        /// <summary>
        /// Interner Prescaler: 8-Bit
        /// </summary>
        private byte prescaler;

        /// <summary>
        /// Speichert wieviele Taktcyclen der Pic schon läuft
        /// </summary>
        private int runtime;

        /// <summary>
        /// Watchdog zähler
        /// </summary>
        private int watchdog;

        /// <summary>
        /// Programm-Counter
        /// </summary>
        private int pc;

        /// <summary>
        /// Initialisiert den PIC
        /// </summary>
        public Pic()
        {
            // Erstellen der Speicherbereiche
            ram = new PicRam();
            rom = new PicRom();
            stack = new PicStack();

            // Initialisieren der Standardwerte
            doPowerOnReset();

        }

        /// <summary>
        /// Führt einen Reset durch
        /// Dabei werden alle Register auf diese Standardwerte gesetzt.
        /// </summary>
        public void doReset()
        {
            stack.reset();                  // Stack resetten
            prescaler = 0;                  // Prescaler resetten
            runtime = 0;                    // Runtime resetten
            resetWatchDog();                // watchdog resetten

            // Standardwerte ins ram setzen
            setPc(0);                       // ProgrammCounter resetten
            ram.writeFlag("IRP", false);    // Statusregisterwerte
            ram.writeFlag("RP0", false);
            ram.writeFlag("RP1", false);
            ram.writeFlag("!TO", true);
            ram.writeFlag("!PD", true);
            ram.write("OPTION_REG", 0xFF);      // Option Register
            ram.write("TRISA", 0x1F);           // Tris A
            ram.write("TRISB", 0xFF);           // Tris B
            ram.write("INTCON", 0x00);          // IntCon
        }

        /// <summary>
        /// Führt einen PowerOnReset durch.
        /// Das Komplette Ram wird geleert und mit Standardwerten belegt.
        /// </summary>
        public void doPowerOnReset()
        {
            ram.reset();                    // Ram resetten
            setRegW(0);                     // W-Register resetten
            doReset();
        }

        /// <summary>
        /// Gibt das Low-Byte des Programm-Counter zurück
        /// </summary>
        /// <returns>Programm-Counter</returns>
        public byte getPc()
        {
            return (byte)(pc % 256);
        }
        /// <summary>
        /// Gibt das High-Byte des PC zurück
        /// </summary>
        /// <returns></returns>
        public byte getPcLath()
        {
            return (byte)((pc / 256) % 64);
        }
        /// <summary>
        /// Setzt den Programm-Counter auf einen neuen Wert
        /// </summary>
        /// <param name="pc_new">Neuer Programm-Counter</param>
        public void setPc(byte pc_new)
        {
            //ram.write(0x02,pc_new);     
            pc = pc_new + ((pc / 256) * 256);
        }
        /// <summary>
        /// Setzt das High-Byte des Programm-Counter auf einen neuen Wert
        /// </summary>
        /// <param name="pclath_new"></param>
        public void setPcLath(byte pclath_new)
        {
            pc = (pc % 256) + pclath_new;
            ram.write(0x0A, (byte)(pc / 256));
        }
        /// <summary>
        /// Erhöht den Programm-Counter um eins
        /// </summary>
        public void incPc()
        {
            pc++;
            ram.write(0x02, (byte)(pc % 256));
            ram.write(0x0A, (byte)(pc / 256));
        }

        /// <summary>
        /// Gibt den akuellen Wert des Watchdogs zurück
        /// </summary>
        /// <returns></returns>
        public int getWatchDog()
        {
            return watchdog;
        }

        /// <summary>
        /// Resettet den watchdog
        /// </summary>
        public void resetWatchDog()
        {
            watchdog = 0;
            prescaler = 0;
        }
        /// <summary>
        /// Gibt den Inhalt des W-Registers zurück
        /// </summary>
        public byte getRegW()
        {
            return regw;
        }
        /// <summary>
        /// Setzt das W-Register auf einen neuen Wert
        /// </summary>
        /// <param name="regw_new">Neuer Wert des W-Registers</param>
        public void setRegW(byte regw_new)
        {
            regw = regw_new;
        }

        /// <summary>
        /// Gibt den Wert des Statusregisters zurück
        /// </summary>
        public byte getRegStatus()
        {
            return ram.read(0x03);
        }
        /// <summary>
        /// Setzt das Statusregister
        /// </summary>
        /// <param name="status_new">Neuer Wert des Statusregisters</param>
        public void setRegStatus(byte status_new)
        {
            ram.write(0x03, status_new);
        }

        /// <summary>
        /// Gibt den Wert des Z Flags zurück
        /// </summary>
        /// <returns>Z Flag</returns>
        public bool getFlagZ()
        {
            return ram.readFlag(0x03, 2);
        }
        /// <summary>
        /// Setzt den Wert des Z Flags
        /// </summary>
        /// <param name="flagz_new">Neuer Wert des Z Flags</param>
        public void setFlagZ(bool flagz_new)
        {
            ram.writeFlag(0x03, 2, flagz_new);
        }

        /// <summary>
        /// Gibt den Wert des C Flags zurück
        /// </summary>
        /// <returns>Wert des C Flags</returns>
        public bool getFlagC()
        {
            return ram.readFlag(0x03, 0);
        }
        /// <summary>
        /// Setzt das C Flag
        /// </summary>
        /// <param name="flagc_new">Neuer Wert des C Flags</param>
        public void setFlagC(bool flagc_new)
        {
            ram.writeFlag(0x03, 0, flagc_new);
        }
        /// <summary>
        /// Gibt den Wert des DC Flags zurück
        /// </summary>
        /// <returns></returns>
        public bool getFlagDC()
        {
            return ram.readFlag(0x03, 1);
        }
        /// <summary>
        /// Setzt den Wert des DC Flags
        /// </summary>
        /// <param name="flagdc_new">Neuer Wert des DC Flags</param>
        public void setFlagDC(bool flagdc_new)
        {
            ram.writeFlag(0x03, 1, flagdc_new);
        }

        /// <summary>
        /// Erhöht die Runtime Zeit um die übergebene Anzahl
        /// </summary>
        /// <param name="anzahl"></param>
        public void incRuntime(int anzahl)
        {
            runtime += anzahl;
        }
        /// <summary>
        /// Gibt die Laufzeit zurück
        /// </summary>
        /// <returns></returns>
        public int getRuntime()
        {
            return runtime;
        }


        /// <summary>
        /// Führt einen Interrupt aus
        /// </summary>
        public void interrupt()
        {
            ram.writeFlag("GIE", false);    // clear GIE
            stack.push((byte)(getPc() - 1), getPcLath());  // Push PC
            setPc(0x04);                    // Set PC to Adr 0x04
            incRuntime(1);
        }

        /// <summary>
        /// Führt die Timerrelevanten Befehle bei einer Umschaltung von RA4 aus
        /// Wird bei Umschaltung von RA4 aufgerufen
        /// </summary>
        public void checkTimerInterruptRA4()
        {
            if (ram.readFlag("T0CS"))   // T0CS gesetzt -> TMR0 Counter Mode
                if ((ram.readFlag("T0SE")) != (ram.readFlag("RA4")))  // Check auf Flanke
                    if (!ram.readFlag("PSA")) // Ist Prescaler TMR0 zugewiesen?
                        incPrescaler();         // Prescaler zugewiesen -> Prescaler erhöhen
                    else
                        incTmr0();              // Prescaler nicht zugewiesen -> TMR0 erhöhen
        }

        /// <summary>
        /// Führ die Timerrelevanten Befehle nach einer OpCode ausführung aus
        /// Wird nach jeder Befehlsausführung aufgerufen
        /// </summary>
        public void checkTimerInterruptInstruction()
        {
            if (!ram.readFlag("T0CS"))   // T0CS gecleart -> TMR0 Timer Mode
                incTmr0();
        }

        /// <summary>
        /// Erhöht Register Tmr0 und führt bei Bedarf einen Interrupt aus
        /// </summary>
        public void incTmr0()
        {
            if ((ram.read("TMR0") == 0xFF) && (ram.readFlag("T0IE")) && (ram.readFlag("GIE")))
            {               // bei Überlauf, akt. Timer (T0IE) und akt. Interrupts (GIE) -> Flag setzen und Interrupt                    
                ram.write("TMR0", (byte)(ram.read("TMR0") + 1));
                ram.writeFlag("T0IF", true);   // Timer Overflow Flag setzten (T0IF)
                ram.writeFlag("GIE", false);  // Interrupts deaktivieren
                interrupt();
            }
            else ram.write("TMR0", (byte)(ram.read("TMR0") + 1));
        }

        /// <summary>
        /// erhöht den Wert für den Watchdog
        /// </summary>
        public void incWatchdog()
        {
            if (ram.readFlag("PSA"))
            {
                incPrescaler();
            }
            else watchdog += 1;

            if (watchdog == 128)
            {
                doReset();
                ram.writeFlag("!TO", false);
                ram.writeFlag("!PD", true);
            }

        }
        /// <summary>
        /// Erhöht den Prescalerwert und führt bei Max-Wert die gekoppelten Befehle aus
        /// </summary>
        public void incPrescaler()
        {
            prescaler++;
            if (!ram.readFlag("PSA"))     // Wem ist der Prescaler zugewiesen?
            {                               // Prescaler ist Timer zugewiesen
                if (prescaler >= (Math.Pow(2, (ram.read("OPTION_REG") % 8)) * 2))  // Ist Prescaler auf Max Wert?
                {
                    prescaler = 0;
                    incTmr0();
                }
            }
            else                            // Prescaler ist WDT zugewiesen
            {
                if (prescaler >= (Math.Pow(2, ram.read("OPTION_REG") % 8)))      // Ist Prescaler auf Max Wert?
                {
                    prescaler = 0;
                    watchdog += 1;
                }
            }
        }

        /// <summary>
        /// Überprüft ob eine externer Interrupt an RB0 erfolgt und führt diesen aus
        /// Wird beim Umschalten von RB0 aufgerufen
        /// </summary>
        public void checkExternalInterrupt()
        {
            if ((ram.readFlag("INTEDG")) == (ram.readFlag("RB0")))    // RB0 Flanke erfolgt
            {
                if (ram.readFlag("GIE") && ram.readFlag("INTE"))    // Interrupts aktiviert
                {
                    ram.writeFlag("INTF", true);
                    ram.writeFlag("GIE", false);
                    interrupt();
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn eine Änderung an einem der Pins RB4 bis RB7 erfolgte
        /// </summary>
        public void checkPortChangeInterrupt()
        {
            if (ram.readFlag("GIE") && ram.readFlag("RBIE"))
            {
                ram.writeFlag("RBIF", true);
                ram.writeFlag("GIE", false);
                interrupt();
            }
        }


        /// <summary>
        /// Überprüft ein Ergebnis auf einen 8-Bit Überlauf und setzt die Flags
        /// </summary>
        /// <param name="result">zu Überprüfende Zahl</param>
        public void checkAffectedC(int result)
        {
            if (result >= Math.Pow(2, 8)) setFlagC(true);
            else setFlagC(false);
        }
        /// <summary>
        /// Überprüft ein Ergebnis auf 8-Bit null und setzt die Flags
        /// </summary>
        /// <param name="result">zu überprüfende Zahl</param>
        public void checkAffectedZ(int result)
        {
            if ((byte)(result) == 0) setFlagZ(true);
            else setFlagZ(false);
        }

        /// <summary>
        /// Check ob der aktuelle ProgrammCode mit dem angegebenen Wert anfängt
        /// </summary>
        /// <param name="startwith">Zu überprüfender Anfang"</param>
        /// <returns>Gibt Wahr zurück, wenn der Programcode mit dem Teilstring anfängt</returns>
        public bool opCodeStartWith(string startwith)
        {
            string programcode = Convert.ToString(rom.read(getPc()), 2);
            while (programcode.Length < 14) programcode = "0" + programcode;

            return programcode.StartsWith(startwith);
        }

        /// <summary>
        /// Führt den nächsten ProgrammCode im Rom aus
        /// </summary>
        public void doNextProgramCode()
        {
            bool d;         // Destination Bit
            byte f;         // Register File Address 
            byte b;         // Bit Adress within an 8-Bit file register
            byte k;         // literal field, constant data/label
            short kbig;     // großes literal field (für call und goto)
            int command;    // aktueller Befehl
            int result;     // Ergebnis Zwischenspeicher

            command = rom.read(getPc());     // Kompletten Befehl auslesen

            // -----------------------------------------------------------------------
            // Parameterliste auslesen

            if (((command / 128) % 2) == 1) d = true; else d = false;
            f = (byte)(command % 128);
            b = (byte)((command / 128) % 8);
            k = (byte)(command % 256);
            kbig = (short)(command % 2048);

            // -----------------------------------------------------------------------
            // Befehle abfragen         

            // -----------------------------------------------------------------------
            // Control Operations

            // GOTO         10 1kkk kkkk kkkk

            if (opCodeStartWith("101"))
            {
                setPc(k);
                incRuntime(2);
            }

        // CALL         10 0kkk kkkk kkkk

            else if (opCodeStartWith("100"))
            {
                stack.push(getPc(), getPcLath());
                setPc(k);
                incRuntime(2);
            }

        // RETURN       00 0000 0000 1000

            else if (command == 8)
            {
                int pop = stack.pop();
                setPc((byte)(pop % 256));
                setPcLath((byte)(pop / 256));
                incRuntime(2);
            }

        // RETLW        11 01xx kkkk kkkk

            else if (opCodeStartWith("1101"))
            {
                setRegW(k);
                int pop = stack.pop();
                setPc((byte)(pop % 256));
                setPcLath((byte)(pop / 256));
                incRuntime(2);
            }

        // RETFIE       00 0000 0000 1001

            else if (command == 9)
            {
                int pop = stack.pop();
                setPc((byte)(pop % 256));
                setPcLath((byte)(pop / 256));
                ram.writeFlag(0x0B, 7, true);    // GIE Flag setzen
                incRuntime(2);
            }

        // NOP          00 0000 0xx0 0000

            else if (opCodeStartWith("0000000"))
            {
                // NOP
                incRuntime(1);
            }

        // CLRWDT       00 0000 0110 0100

            else if (command == 0x64)
            {
                ram.writeFlag(0x03, 3, true);    // !TP
                ram.writeFlag(0x03, 4, true);    // !TO
                resetWatchDog();
                incRuntime(1);
            }

        // SLEEP        00 0000 0110 0011

            else if (command == 0x63)
            {
                // SLEEP

                incRuntime(2);
            }

            else
            {

                // -----------------------------------------------------------------------
                // Byte-Oriented File Register Operations
                if (opCodeStartWith("00"))
                {

                    // ADDWF        00 0111 dfff ffff       Affected Bits: C, DC, Z  

                    if (opCodeStartWith("000111"))
                    {
                        result = doAdd(getRegW(), ram.read(f));

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // ANDWF        00 0101 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("000101"))
                    {
                        result = doAnd(getRegW(), ram.read(f));

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // CLRF         00 0001 lfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("0000011"))
                    {
                        setFlagZ(true);
                        ram.write(f, 0);
                        incRuntime(1);
                    }
                    // CLRW         00 0001 0xxx xxxx       Affected Bits: Z

                    else if (opCodeStartWith("0000010"))
                    {
                        setFlagZ(true);
                        setRegW(0);
                        incRuntime(1);
                    }

            // COMF         00 1001 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("001001"))
                    {
                        result = doComplement(ram.read(f));

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // DECF         00 0011 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("000011"))
                    {
                        result = ram.read(f) - 1;
                        checkAffectedZ(result);

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // DECFSZ       00 1011 dfff ffff

                    else if (opCodeStartWith("001011"))
                    {
                        result = ram.read(f) - 1;

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);

                        if (result == 0)        // If ergebnis 0 -> Nächsten Befehl auslassen
                        { incPc(); incRuntime(2); }
                        else incRuntime(1);
                    }

            // INCF         00 1010 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("001010"))
                    {
                        result = ram.read(f) + 1;
                        checkAffectedZ(result);

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // INCFSZ       00 1111 dfff ffff

                    else if (opCodeStartWith("001111"))
                    {
                        result = ram.read(f) + 1;

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);

                        if (result == 0)           // If 0 -> Nächsten Befehl auslassen
                        { incPc(); incRuntime(2); }
                        else incRuntime(1);
                    }

            // IORWF        00 0100 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("000100"))
                    {
                        result = doOr(regw, ram.read(f));

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // MOVF         00 1000 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("001000"))
                    {
                        result = ram.read(f);
                        checkAffectedZ(result);

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // MOVWF        00 0000 1fff ffff

                    else if (opCodeStartWith("0000001"))
                    {
                        ram.write(f, getRegW());
                        incRuntime(1);
                    }

            // RLF          00 1101 dfff ffff       Affected Bits: C

                    else if (opCodeStartWith("001101"))
                    {
                        result = doRotateLeft(ram.read(f));

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // RRF          00 1100 dfff ffff       Affected Bits: C

                    else if (opCodeStartWith("001100"))
                    {
                        result = doRotateRight(ram.read(f));

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // SUBWF        00 0010 dfff ffff       Affected Bits: C, DC, Z

                    else if (opCodeStartWith("000010"))
                    {
                        result = doSubstract(ram.read(f), getRegW());

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // SWAPF            00 1110 dfff ffff

                    else if (opCodeStartWith("001110"))
                    {
                        result = (ram.read(f) / 16) - ((ram.read(f) / 16) % 1);
                        result += ((ram.read(f) % 16) * 16);

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }

            // XORWF        00 0110 dfff ffff       Affected Bits: Z

                    else if (opCodeStartWith("000110"))
                    {
                        result = doXOr(ram.read(f), getRegW());

                        if (d) ram.write(f, (byte)result);
                        else setRegW((byte)result);
                        incRuntime(1);
                    }
                }

            // -----------------------------------------------------------------------
                // Bit Oriented File Register Operations

                else if (opCodeStartWith("01"))
                {

                    // BCF          01 00bb bfff ffff

                    if (opCodeStartWith("0100"))
                    {
                        ram.writeFlag(f, b, false);
                        incRuntime(1);
                    }

            // BSF          01 01bb bfff ffff

                    else if (opCodeStartWith("0101"))
                    {
                        ram.writeFlag(f, b, true);
                        incRuntime(1);
                    }

            // BTFSC        01 10bb bfff ffff

                    else if (opCodeStartWith("0110"))
                    {
                        if (ram.readFlag(f, b) == false)
                        { incPc(); incRuntime(2); }
                        else incRuntime(1);

                    }

            // BTFSS        01 11bb bfff ffff

                    else if (opCodeStartWith("0111"))
                    {
                        if (ram.readFlag(f, b) == true)
                        { incPc(); incRuntime(2); }
                        else incRuntime(1);
                    }

                }

            // -----------------------------------------------------------------------
                // Literal Operations

                else if (opCodeStartWith("11"))
                {

                    // ADDLW        11 111x kkkk kkkk       Affected Bits: C, Z, DC

                    if (opCodeStartWith("11111"))
                    {
                        result = doAdd(getRegW(), k);

                        setRegW((byte)result);
                        incRuntime(1);
                    }

            // ANDLW        11 1001 kkkk kkkk       Affected Bits: Z

                    else if (opCodeStartWith("111001"))
                    {
                        result = doAnd(getRegW(), k);

                        setRegW((byte)result);
                        incRuntime(1);
                    }

            // IORLW        11 1000 kkkk kkkk

                    else if (opCodeStartWith("111000"))
                    {
                        result = doOr(k, getRegW());
                        checkAffectedZ(result);

                        setRegW((byte)result);
                        incRuntime(1);
                    }

            // MOVLW        11 00xx kkkk kkkk

                    else if (opCodeStartWith("1100"))
                    {
                        setRegW(k);
                        incRuntime(1);
                    }

            // SUBLW        11 110x kkkk kkkk       Affected Bits: C, DC, Z

                    else if (opCodeStartWith("11110"))
                    {
                        result = doSubstract(k, getRegW());

                        setRegW((byte)result);
                        incRuntime(1);
                    }

            // XORLW        11 1010 kkkk kkkk       Affected Bits: Z

                    else if (opCodeStartWith("111010"))
                    {
                        result = doXOr(k, getRegW());

                        setRegW((byte)result);
                        incRuntime(1);
                    }

                }
                else
                {
                    Console.WriteLine("Fehler! Kein OpCode erkannt");
                }

                // -----------------------------------------------------------------------
                // Nach jedem Befehl ausführen, der keine Änderung am PC vornimmt:

                incPc();                    // Nach jedem nicht-Sprung-Befehl: PC erhöhen                
                checkTimerInterruptInstruction();   // Timer Befehle ausführen (Bei Bedarf Tmr0 erhöhen)
            }

            // -------------------------------------------------------------------
            // Nach allen Befehlen ausführen:


        }


        // --------------------------------------------------------------
        // Hilfsfunktionen für einige Befehle

        /// <summary>
        /// Führt eine Und-Verknüpfung zweier Zahlen aus
        /// </summary>
        /// <param name="zahla"></param>
        /// <param name="zahlb"></param>
        /// <returns></returns>
        public byte doAnd(byte zahla, byte zahlb)
        {
            int result = 0;
            for (int i = 0; i < 8; i++)
                if ((((zahla / (int)Math.Pow(2, i)) % 2) == 1) && ((zahlb / (int)Math.Pow(2, i)) % 2) == 1)
                    result = (int)(result + Math.Pow(2, i));
            checkAffectedZ(result);
            return (byte)result;
        }
        /// <summary>
        /// Führt eine Oder-Verknüpfung zweier Zahlen aus
        /// </summary>
        /// <param name="zahla"></param>
        /// <param name="zahlb"></param>
        /// <returns></returns>
        public byte doOr(byte zahla, byte zahlb)
        {
            int result = 0;
            for (int i = 0; i < 8; i++)
                if ((((zahla / (int)Math.Pow(2, i)) % 2) == 1) || (((zahlb / (int)Math.Pow(2, i)) % 2) == 1))
                    result = (int)(result + Math.Pow(2, i));
            checkAffectedZ(result);
            return (byte)result;
        }
        /// <summary>
        /// Führt eine Entweder-Oder-Verknüpfung zweier Zahlen aus
        /// </summary>
        /// <param name="zahla"></param>
        /// <param name="zahlb"></param>
        /// <returns></returns>
        public byte doXOr(byte zahla, byte zahlb)
        {
            int result = 0;
            for (int i = 0; i < 8; i++)
                if (((zahla / (int)Math.Pow(2, i)) % 2) != ((zahlb / (int)Math.Pow(2, i)) % 2))
                    result = (int)(result + Math.Pow(2, i));
            checkAffectedZ(result);
            return (byte)result;
        }
        /// <summary>
        /// Addiert zwei Zahlen
        /// </summary>
        /// <param name="zahla"></param>
        /// <param name="zahlb"></param>
        /// <returns></returns>
        public byte doAdd(byte zahla, byte zahlb)
        {
            int result = zahla + zahlb;
            checkAffectedC(result);
            checkAffectedZ(result);
            if (((zahla % 16) + (zahlb % 16)) >= 16) setFlagDC(true); else setFlagDC(false);
            return (byte)result;
        }
        /// <summary>
        /// Subtrahiert zwei Zahlen nach der Einser-Komplement Methode
        /// </summary>
        /// <param name="zahla"></param>
        /// <param name="zahlb"></param>
        /// <returns></returns>
        public byte doSubstract(byte zahla, byte zahlb)
        {
            int result = doAdd(zahla, (byte)(255 - zahlb + 1));
            checkAffectedZ(result);
            return (byte)result;
        }
        /// <summary>
        /// Bilder das Komplement einer Zahl
        /// </summary>
        /// <param name="zahl"></param>
        /// <returns></returns>
        public byte doComplement(byte zahl)
        {
            int result = 255 - zahl;
            checkAffectedZ(result);
            return (byte)result;
        }
        /// <summary>
        /// Rotiert eine Zahl um ein Bit nach links
        /// </summary>
        /// <param name="zahl"></param>
        /// <returns></returns>
        public byte doRotateLeft(byte zahl)
        {
            int result = 0;
            if (getFlagC()) result++;
            for (int i = 1; i < 8; i++)
                if (((zahl / (int)Math.Pow(2, i - 1)) % 2) == 1)
                    result = (int)(result + Math.Pow(2, i));
            if (((zahl / (int)Math.Pow(2, 7)) % 2) >= 1) setFlagC(true);
            else setFlagC(false);
            checkAffectedC(result);
            return (byte)result;
        }
        /// <summary>
        /// Rotiert eine Zahl um ein Bit nach rechts
        /// </summary>
        /// <param name="zahl"></param>
        /// <returns></returns>
        public byte doRotateRight(byte zahl)
        {
            int result = 0;
            if (getFlagC()) result = (int)(result + Math.Pow(2, 8));
            for (int i = 0; i < 7; i++)
                if (((zahl / (int)Math.Pow(2, i + 1)) % 2) == 1)
                    result = (int)(result + Math.Pow(2, i));
            if ((zahl % 2) == 1) setFlagC(true);
            else setFlagC(false);
            checkAffectedC(result);
            return (byte)result;
        }


    }
}
