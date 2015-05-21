using System;
using System.Collections.Generic;
using System.Text;

namespace PicSim
{
    class PicStack
    {
        /// <summary>
        /// Das Stack Array
        /// </summary>
        private int[] stack;

        /// <summary>
        /// Point auf den aktuellen Stackwert
        /// </summary>
        private byte stackpointer;

        /// <summary>
        /// Initialisiert den Stack
        /// </summary>
        public PicStack()
        {
            stackpointer = 0;
            stack = new int[8];
        }

        /// <summary>
        /// Speichert den übergebenen Wert in den Stack
        /// </summary>
        /// <param name="topush"></param>
        public void push(byte pcl, byte pclath)
        {
            stack[stackpointer] = (int)((pcl + 1) + (pclath * 256));
            stackpointer++;
            if (stackpointer == 8) stackpointer = 0;
        }

        /// <summary>
        /// Gibt den nächsten gespeicherten PC Wert zurück
        /// </summary>
        public int pop()
        {
            if (stackpointer == 0) stackpointer = 7;
            else stackpointer--;
            return stack[stackpointer];
        }

        /// <summary>
        /// Setzt den Stack auf Anfang zurück
        /// </summary>
        public void reset()
        {
            stackpointer = 0;
            stack = new int[8];
        }

        /// <summary>
        /// Gibt ein Item des Stacks zurück
        /// </summary>
        public int getStack(int nummer)
        {
            if ((nummer >= 0) && (nummer < 8))
                return stack[nummer];
            else
                return 0;
        }

    }
}
