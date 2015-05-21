using System;
using System.Collections.Generic;
using System.Text;

namespace PicSim
{
    class PicRom
    {
        /// <summary>
        /// Byte-Array zur Simulierung des Roms
        /// </summary>
        private int[] rom;

        /// <summary>
        /// Initialisiert die Größe des Roms
        /// </summary>
        public PicRom()
        {
            rom = new int[1024];
        }


        /// <summary>
        /// Schreibt einen Wert an die Adresse ins Rom
        /// </summary>
        /// <param name="adr">Adresse</param>
        /// <param name="wert">Wert</param>
        public void write(int adr, int wert)
        {
            rom[adr] = wert;
        }

        /// <summary>
        /// Liest einen Wert an der Adresse im Rom aus
        /// </summary>
        /// <param name="adr"></param>
        /// <returns></returns>
        public int read(int adr)
        {
            return rom[adr];
        }

        /// <summary>
        /// Resettet das Rom auf 0
        /// </summary>
        public void reset()
        {
            for (int i = 0; i < rom.Length; i++) rom[i] = 0;
        }

    }
}
