using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace PicSim
{
    public partial class Form1 : Form
    {

        /// <summary>
        /// beinhaltet eine Picumgebung
        /// </summary>
        Pic picsimu;

        /// <summary>
        /// Gibt an, ob die Portansteuerung über die GUI oder über Extern läuft
        /// </summary>
        bool UseExternPorts = false;

        /// <summary>
        /// Erstellt eine Com-Verbindung zu den Externen Ports
        /// </summary>
        public SerialPort port;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            picsimu = new Pic();
            createRamBoxes();
            refreshObjects();
            initComPort();
        }


        private void btnLoadCode_Click(object sender, EventArgs e)
        {
            LoadSourcecode();
            picsimu = new Pic();
            LoadSourcecodeToPic();
            SelectLineOfCurrentPC();
            refreshObjects();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            tmrAutorun.Enabled = !tmrAutorun.Enabled;

            Button Startbutton = (Button)sender;
            SwapStartButtonText(Startbutton);

        }

        private void tmrAutorun_Tick(object sender, EventArgs e)
        {

            if (breakPointCheck() == false) doPicProgramStep();
            else
            {
                tmrAutorun.Enabled = false;
                SwapStartButtonText((Button)GetControlByName("btnStart"));
            }
        }

        private void btnStepByStep_Click(object sender, EventArgs e)
        {
            doPicProgramStep();
        }

        private void doPicProgramStep()
        {
            if (cbWdtEnabled.Checked == true) picsimu.incWatchdog();
            picsimu.doNextProgramCode();
            refreshObjects();
            SelectLineOfCurrentPC();

        }

        private void btnPortSource_Click(object sender, EventArgs e)
        {
            if (UseExternPorts)
            {   // Ext Ports an -> ausschalten
                UseExternPorts = false;
                btnPortSource.Text = "GUI";
                cbExtSource.Visible = false;
                tmrExtPorts.Enabled = false;
            }
            else
            {                  // Ext Ports aus -> anschalten
                UseExternPorts = true;
                btnPortSource.Text = "EXT";
                cbExtSource.Visible = true;
                tmrExtPorts.Enabled = true;
            }
            getPortDirection();
        }

        /// <summary>
        /// Ruft alle verfügbaren Com-Ports ab und setzt den ersten als aktiven.
        /// </summary>
        private void initComPort()
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length != 0)
            {
                for (int i = 0; i < ports.Length; i++)
                    cbExtSource.Items.Add(ports[i]);
                cbExtSource.SelectedIndex = 0;
                port = new SerialPort(ports[0], 4800, Parity.None, 8, StopBits.One);
                port.Open();
                port.ReceivedBytesThreshold = 5;
                port.DataReceived += new SerialDataReceivedEventHandler(OnSerialDataRecieved);
            }
            else
            {
                port = null;
                cbExtSource.Items.Clear();
            }
        }

        /// <summary>
        /// Wird ausgelöst wenn eine andere ComQuelle markiert wird
        /// Initialisiert die neue Com-Quelle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbExtSource_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cbExtSource.SelectedItem.ToString() != "")
            {
                tmrExtPorts.Enabled = false;
                port.Close();
                port.Dispose();
                port = new SerialPort(cbExtSource.SelectedItem.ToString(), 4800, Parity.None, 8, StopBits.One);
                port.Open();
                port.ReceivedBytesThreshold = 5;
                port.DataReceived += new SerialDataReceivedEventHandler(OnSerialDataRecieved);
                SendSerialData();
                tmrExtPorts.Enabled = true;
            }
        }

        /// <summary>
        /// Timer der zyklisch die externen Ports aktualisiert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrExtPorts_Tick(object sender, EventArgs e)
        {
            if (UseExternPorts) SendSerialData();
        }

        /// <summary>
        /// Holt die Daten zu den Ports aus dem Pic und schickt sie an die exterene Platine
        /// </summary>
        private void SendSerialData()
        {
            if (port != null)       // Nur wenn ein Serieller Port gesetzt wurde
            {
                byte[] send = new byte[9];
                // Zusammensetzen des SendBytes
                send[0] = (byte)(0x30 + (picsimu.ram.read("TRISA") / 16));
                send[1] = (byte)(0x30 + (picsimu.ram.read("TRISA") % 16));
                send[2] = (byte)(0x30 + (picsimu.ram.read("PORTA") / 16));
                send[3] = (byte)(0x30 + (picsimu.ram.read("PORTA") % 16));
                send[4] = (byte)(0x30 + (picsimu.ram.read("TRISB") / 16));
                send[5] = (byte)(0x30 + (picsimu.ram.read("TRISB") % 16));
                send[6] = (byte)(0x30 + (picsimu.ram.read("PORTB") / 16));
                send[7] = (byte)(0x30 + (picsimu.ram.read("PORTB") % 16));
                send[8] = 0x0D;
                port.Write(send, 0, 9);
            }
        }
        /// <summary>
        /// Wird bei erfolgreichem Empfang von 5 Bytes am ComPort ausgeführt.
        /// Aktualisiert die Portstellung im Pic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSerialDataRecieved(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            byte[] received = new byte[5];
            port.Read(received, 0, 5);

            if ((UseExternPorts) && (received[4] == 0x0D))
            {
                // Auf Interrupts testen
                int extIntRa4 = 0, extIntExt = 0, extIntPortChange = 0;
                if (picsimu.ram.readFlag("RA4")) extIntRa4 = 1;
                if (picsimu.ram.readFlag("RB0")) extIntExt = 1;
                extIntPortChange = (picsimu.ram.read("PORTB") / 16);

                // Ports aktualisieren
                picsimu.ram.write("PORTA", (byte)(((received[0] % 2) * 16) + (received[1] % 16)));
                picsimu.ram.write("PORTB", (byte)(((received[2] % 16) * 16) + (received[3] % 16)));

                // Wenn Interrupts erkannt wurden, diese ausführen
                // (Findet erst nach der Aktualisierung der Werte statt, damit die neuen gleich beim Interrupt verwendet werden
                if ((received[0] % 2) != extIntRa4) picsimu.checkTimerInterruptRA4();
                if ((received[3] % 2) != extIntExt) picsimu.checkExternalInterrupt();
                if (extIntPortChange != received[2]) picsimu.checkPortChangeInterrupt();

                // Auf Externen Reset testen
                if (((received[0] / 2) % 2) == 0)
                {
                    // tmrAutorun.Enabled = false;   // auskommentiert (Stop den Autorun bei einem externen reset
                    // SwapStartButtonText(btnStart);
                    picsimu.doReset();
                    refreshObjects();
                    SelectLineOfCurrentPC();
                }
            }
            // Achtung: Löst im Debugmodus Threadfehler aus
            refreshPorts();
            refreshObjects();
        }


        /// <summary>
        /// Testbutton
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Reset_Click(object sender, EventArgs e)
        {
            picsimu.doReset();
            refreshObjects();
            SelectLineOfCurrentPC();
        }

        /// <summary>
        /// Checkt ob die aktuelle Selektion im ListView ein Breakpoint ist
        /// </summary>
        /// <returns></returns>
        private bool breakPointCheck()
        {
            for (int linie = 0; linie < listProgramcode.Items.Count - 1; linie++)
            {
                if (listProgramcode.Items[linie].Selected == true)
                {
                    if (listProgramcode.Items[linie].BackColor == Color.Red) return true;
                }
            }
            return false;
        }

        /// <summary>
        ///Den Text auf dem Startbutton ändern 
        /// </summary>
        /// <param name="startbutton"></param>
        private void SwapStartButtonText(Button startbutton)
        {
            if (string.Compare(startbutton.Text, "Start") == 0)
            {
                startbutton.Text = "Stop";
            }
            else if (string.Compare(startbutton.Text, "Stop") == 0)
            {
                startbutton.Text = "Start";
            }

        }

        /// <summary>
        /// Läd den SourceCode in die Listbox
        /// </summary>
        private void LoadSourcecode()
        {
            using (OpenFileDialog Dialog = new OpenFileDialog())
            {
                DialogResult Result;
                Dialog.CheckFileExists = true;
                Dialog.Title = "Datei Laden";
                Dialog.Filter = "Lst-Dateien|*.lst";
                Dialog.RestoreDirectory = true;

                Result = Dialog.ShowDialog();

                if (Result == DialogResult.OK)
                { // Wenn die Datei ausgewählt wurde, wird sie eingelesen
                    listProgramcode.Columns[0].Text = Dialog.FileName;
                    using (TextReader Reader = new StreamReader(Dialog.FileName, Encoding.Default))
                    {
                        string Line = String.Empty;

                        while ((Line = Reader.ReadLine()) != null) // Zeile für Zeile auslesen
                            listProgramcode.Items.Add(Line);
                    }
                }
            }
        }
        /// <summary>
        /// Läd den aktuellen Source Code in der Listbox in den Pic
        /// </summary>       
        private void LoadSourcecodeToPic()
        {
            string linientext;
            int code, codenr;
            for (int linie = 0; linie < listProgramcode.Items.Count - 1; linie++)
            {
                linientext = (string)(listProgramcode.Items[linie].Text);
                if (linientext.Substring(0, 4) != "    ")
                {
                    codenr = Int32.Parse(linientext.Substring(0, 4), System.Globalization.NumberStyles.HexNumber);
                    code = Int32.Parse(linientext.Substring(5, 4), System.Globalization.NumberStyles.HexNumber);
                    picsimu.rom.write(codenr, code);

                }
            }
        }
        /// <summary>
        /// Markiert den momentanen Befehl des Pics in der Listbox
        /// </summary>
        private void SelectLineOfCurrentPC()
        {
            for (int linie = 0; linie < listProgramcode.Items.Count - 1; linie++)
            {
                string linientext = (string)(listProgramcode.Items[linie].Text);
                if (linientext.Substring(0, 4) != "    ")
                {
                    int nummer = Int32.Parse(linientext.Substring(0, 4), System.Globalization.NumberStyles.HexNumber);
                    if (nummer == picsimu.getPc())
                    {
                        //listProgramcode.Select();
                        listProgramcode.Items[linie].Selected = true;
                        listProgramcode.Items[linie].EnsureVisible();
                    }
                }
            }

        }

        /// <summary>
        /// Switched zwischen 1 und 0 bei klick auf ein Status- oder Portfeld
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortValueSwap(object sender, EventArgs e)
        {

            Button btnsender = (Button)sender;

            if (string.Compare(btnsender.Text, "0") == 0)
            {
                btnsender.Text = "1";
            }
            else if (string.Compare(btnsender.Text, "1") == 0)
            {
                btnsender.Text = "0";
            }

            SetPortButtons();
            SetStatusIntOpt();

            if (btnsender.BackColor == Color.FromArgb(192, 255, 192))
            {
                switch (btnsender.Name)
                {
                    case ("btnPortRb0"): picsimu.checkExternalInterrupt(); break;
                    case ("btnPortRb4"): picsimu.checkPortChangeInterrupt(); break;
                    case ("btnPortRb5"): picsimu.checkPortChangeInterrupt(); break;
                    case ("btnPortRb6"): picsimu.checkPortChangeInterrupt(); break;
                    case ("btnPortRb7"): picsimu.checkPortChangeInterrupt(); break;
                    case ("btnPortRa4"): picsimu.checkTimerInterruptRA4(); break;
                    default: break;
                }
                refreshObjects();
                SelectLineOfCurrentPC();
            }

        }
        /// <summary>
        /// Auffrischen der Werte für die Ports
        /// </summary>
        private void refreshPorts()
        {
            for (byte portnumber = 0; portnumber < 8; portnumber++)
            {
                if (portnumber < 5)
                {
                    Button buttonportra = (Button)GetControlByName("btnPortRa" + portnumber.ToString());
                    if (picsimu.ram.readFlag("RA" + portnumber.ToString()) == true)
                    {
                        buttonportra.Text = "1";
                    }
                    else
                    {
                        buttonportra.Text = "0";
                    }

                }

                Button buttonportrb = (Button)GetControlByName("btnPortRb" + portnumber.ToString());
                if (picsimu.ram.readFlag("RB" + portnumber.ToString()) == true)
                {
                    buttonportrb.Text = "1";
                }
                else
                {
                    buttonportrb.Text = "0";
                }

            }
        }

        /// <summary>
        /// Methode zum setzen der port direction im interface 0 = input und 1 = output
        /// </summary>        
        private void getPortDirection()
        {
            for (byte portnumber = 0; portnumber < 8; portnumber++)
            {
                if (portnumber < 5)
                {
                    Button buttonportra = (Button)GetControlByName("btnPortRa" + portnumber.ToString());
                    Label labelportra = (Label)GetControlByName("lblPortRa" + portnumber.ToString());
                    if (picsimu.ram.readFlag(133, portnumber) == false)
                    {
                        buttonportra.BackColor = Color.FromArgb(255, 192, 192);
                        buttonportra.Enabled = false;
                        labelportra.Text = "out";
                    }
                    else
                    {
                        buttonportra.BackColor = Color.White;
                        if (!UseExternPorts) buttonportra.Enabled = true;
                        else buttonportra.Enabled = false;
                        labelportra.Text = "in";
                    }

                }
                Button buttonportrb = (Button)GetControlByName("btnPortRb" + portnumber.ToString());
                Label labelportrb = (Label)GetControlByName("lblPortRb" + portnumber.ToString());

                if (picsimu.ram.readFlag(134, portnumber) == false)
                {
                    buttonportrb.BackColor = Color.FromArgb(255, 192, 192);
                    buttonportrb.Enabled = false;
                    labelportrb.Text = "out";
                }
                else
                {
                    buttonportrb.BackColor = Color.White;
                    if (!UseExternPorts) buttonportrb.Enabled = true;
                    else buttonportrb.Enabled = false;
                    labelportrb.Text = "in";
                }
            }
        }

        /// <summary>
        /// Aktualisieren der Textfelder für den Ram
        /// </summary>
        private void refreshRamTextBoxes()
        {
            string rambox;
            for (int x = 0; x <= 127; x++)
            {
                for (int y = 1; y <= 2; y++)
                {
                    byte ram = picsimu.ram.readBank(x, (byte)(y - 1));

                    if (y == 1)
                    {
                        if (x < 16) rambox = "tbRam" + "0" + Convert.ToString(x, 16);
                        else rambox = "tbRam" + Convert.ToString(x, 16);
                    }
                    else
                    {
                        rambox = "tbRam" + Convert.ToString(x + 128, 16);
                    }

                    //TextBox RamTextBox = (TextBox)GetControlByName(rambox);

                    foreach (Control c in this.gbRam.Controls)
                    {
                        if (c.Name == rambox)
                        {
                            string Value = Convert.ToString(ram, 16);
                            if (Value.Length == 1) Value = "0" + Value;
                            c.Text = Value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Aktualisieren der Status Buttons
        /// </summary>
        private void refreshStatus()
        {
            byte reg = picsimu.getRegStatus();
            string Status = Convert.ToString(reg, 2);
            int st = Status.Length;

            for (int z = 0; z <= 7; z++)
            {
                string StatusButtonName = "btnStatus" + z.ToString();
                Button StatusButton = (Button)GetControlByName(StatusButtonName);

                if (Status.Length > z)
                {
                    string statusfragment = Status.Substring(st - 1, 1);
                    StatusButton.Text = statusfragment;
                }
                else StatusButton.Text = "0";
                st--;
            }
        }


        /// <summary>
        /// Aktualisieren der Option Buttons
        /// </summary>
        private void refreshOptions()
        {
            for (byte port = 0; port < 8; port++)
            {
                string StatusButtonName = "btnOpt" + port.ToString();
                Button StatusButton = (Button)GetControlByName(StatusButtonName);
                if (picsimu.ram.readFlag(129, port) == true)
                {
                    StatusButton.Text = "1";
                }
                else
                {
                    StatusButton.Text = "0";
                }
            }
        }

        /// <summary>
        /// Aktualisieren der Interrupt Buttons
        /// </summary>
        private void refreshInterrupts()
        {
            for (byte port = 0; port < 8; port++)
            {
                string InterruptButtonName = "btnInt" + port.ToString();
                Button InterruptButton = (Button)GetControlByName(InterruptButtonName);
                if (picsimu.ram.readFlag(11, port) == true)
                {
                    InterruptButton.Text = "1";
                }
                else
                {
                    InterruptButton.Text = "0";
                }
            }
        }

        /// <summary>
        /// Aktualisieren der Stack labels
        /// </summary>
        private void refreshStack()
        {
            for (int stack = 0; stack < 8; stack++)
            {
                string stackName = "lblStack" + stack.ToString();
                string stackValue = Convert.ToString(picsimu.stack.getStack(stack), 16);
                while (stackValue.Length < 3)
                {
                    stackValue = "0" + stackValue;
                }
                GetControlByName(stackName).Text = stackValue;
            }
        }


        /// <summary>
        /// Methode zum auffinden eines Controls durch dessen Namen
        /// </summary>
        /// <param name="Name">Name des Objekts</param>
        /// <returns></returns>
        private Control GetControlByName(string Name)
        {
            System.Reflection.FieldInfo info = this.GetType().GetField(Name,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.IgnoreCase);

            if (info == null) return null;
            Object o = info.GetValue(this);
            return (Control)o;

        }

        /// <summary>
        /// Die Felder zur Anzeige des Ramspeichers werden aktualisiert
        /// </summary>
        public void refreshObjects()
        {

            //Aktualisieren der RamTextBoxen
            refreshRamTextBoxes();

            //Aktualisieren der Buttons für PortRa/Rb
            getPortDirection();
            refreshPorts();

            //Aktualisieren der Status Buttons
            refreshStatus();

            //Aktualisieren der Interupt Buttons
            refreshInterrupts();

            //Aktualisieren der Option Buttons
            refreshOptions();

            //Aktualisieren der Stack Labels  
            refreshStack();


            //Aktualisieren der restlichen Textboxen
            tbBank.Text = picsimu.ram.getPage().ToString();
            tbPC.Text = Convert.ToString(picsimu.getPc(), 16);
            tbW.Text = Convert.ToString(picsimu.getRegW(), 16);
            tbFSR.Text = Convert.ToString(picsimu.ram.read("FSR"), 16);
            tbTmr.Text = Convert.ToString(picsimu.ram.read(1), 16);
            tbWatchDog.Text = picsimu.getWatchDog().ToString();
            tbRuntime.Text = picsimu.getRuntime().ToString();
        }

        /// <summary>
        /// Die Gui Werte für die Ram Textboxen übernehmen
        /// </summary>
        private void SetRamBoxes()
        {
            for (int ram = 0; ram < 139; ram++)
            {
                string rambox = "tbRam" + Convert.ToString(ram, 16);

                foreach (Control c in this.gbRam.Controls)
                {
                    if (c.Name == rambox)
                    {
                        picsimu.ram.write(ram, Convert.ToByte((string)c.Text, 16));
                    }

                }
            }
        }

        /// <summary>
        /// Werte der Statusregister/Int/Opt Buttons übernehmen
        /// </summary>
        /// <returns></returns>
        private void SetStatusIntOpt()
        {
            for (byte statusflag = 0; statusflag < 8; statusflag++)
            {
                Button statusbutton = (Button)GetControlByName("btnStatus" + statusflag.ToString());
                Button Interruptbutton = (Button)GetControlByName("btnInt" + statusflag.ToString());
                Button Optionbutton = (Button)GetControlByName("btnOpt" + statusflag.ToString());
                picsimu.ram.writeFlag(3, statusflag, Convert.ToBoolean(Convert.ToByte(statusbutton.Text)));
                picsimu.ram.writeFlag(11, statusflag, Convert.ToBoolean(Convert.ToByte(Interruptbutton.Text)));
                picsimu.ram.writeFlag(129, statusflag, Convert.ToBoolean(Convert.ToByte(Optionbutton.Text)));
            }
        }

        /// <summary>
        /// Werte für PortRa/Rb übernehmen
        /// </summary>
        private void SetPortButtons()
        {
            for (byte portnumber = 0; portnumber < 8; portnumber++)
            {
                if (portnumber < 5)
                {
                    Button buttonportra = (Button)GetControlByName("btnPortRa" + portnumber.ToString());
                    picsimu.ram.writeFlag("RA" + portnumber.ToString(), Convert.ToBoolean(Convert.ToByte(buttonportra.Text)));
                }
                Button buttonportrb = (Button)GetControlByName("btnPortRb" + portnumber.ToString());
                picsimu.ram.writeFlag("RB" + portnumber.ToString(), Convert.ToBoolean(Convert.ToByte(buttonportrb.Text)));
            }
        }

        /// <summary>
        ///  //Setzen der restlichen Felder
        /// </summary>
        private void SetPcFsrWTextBoxes()
        {
            if (string.Compare(tbPC.Text, "") == 0) picsimu.setPc(Convert.ToByte(tbPC.Text, 16));
            if (string.Compare(tbW.Text, "") == 0) picsimu.setRegW(Convert.ToByte(tbW.Text, 16));
            if (string.Compare(tbFSR.Text, "") == 0) picsimu.ram.write("FSR", Convert.ToByte(tbFSR.Text, 16));
            if (string.Compare(tbTmr.Text, "") == 0) picsimu.ram.write(1, Convert.ToByte(tbTmr.Text, 16));

            // Hier fehlt die aktivierte Bank picsimu.ram.set(Convert.ToByte(tbW.Text, 16));
        }

        /// <summary>
        /// Die Werte die auf der Gui eingestellt sind, werden in den Speicher übernommen
        /// </summary>
        private void setGuiValues()
        {
            //Werte der Ram Textboxen übernehmen

            SetRamBoxes();

            //Werte der Statusregister/Int/Opt Buttons übernehmen
            SetStatusIntOpt();

            //Werte für PortRa/Rb übernehmen
            SetPortButtons();

            //Setzen der restlichen Felder
            SetPcFsrWTextBoxes();

        }



        /// <summary>
        /// Die Textboxen für den Ram dynamisch erstellen
        /// </summary>
        private void createRamBoxes()
        {

            string Test = "tbRam";
            int heigth = 27;
            int name = -1;

            for (int y = 0; y < 20; y++)
            {
                if (y % 2 == 0)
                {
                    name += 1;
                }
                int width = 30;
                for (int x = 0; x < 8; x++)
                {
                    if (y == 19 && x >= 8)
                    {
                        break;
                    }
                    TextBox aNewBox = new System.Windows.Forms.TextBox();

                    aNewBox.Size = new System.Drawing.Size(20, 20);
                    aNewBox.Location = new System.Drawing.Point(width, heigth);
                    aNewBox.MaxLength = 2;
                    aNewBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
                    aNewBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                    aNewBox.Leave += new System.EventHandler(this.RamTextBoxChanged);

                    if (y % 2 == 1)
                    {
                        aNewBox.Name = Test + name.ToString() + Convert.ToString(x + 8, 16);
                    }
                    else aNewBox.Name = Test + name.ToString() + x.ToString();

                    this.gbRam.Controls.Add(aNewBox);

                    width += 20;
                }
                heigth += 20;


            }


        }


        /// <summary>
        /// Sperrt alle Textboxen auf der Oberfläche -- nicht benutzt --
        /// </summary>
        private void LockTextBoxes()
        {
            //tbBank.Enabled = false;
            //tbPC.Enabled = false;

            foreach (Control c in Controls)
            {
                if (c.GetType().ToString().Substring(c.GetType().ToString().LastIndexOf(".") + 1) == "TextBox")
                {
                    TextBox tb = (TextBox)c;
                    tb.Enabled = false;
                }

            }
            foreach (Control c in gbRam.Controls)
            {
                if (c.GetType().ToString().Substring(c.GetType().ToString().LastIndexOf(".") + 1) == "TextBox")
                {
                    TextBox tb = (TextBox)c;
                    tb.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Event für das Ändern der PC FSR und W Textboxen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxChange(object sender, EventArgs e)
        {
            SetPcFsrWTextBoxes();
        }

        /// <summary>
        /// Event für das Ändern der Ramtextboxen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RamTextBoxChanged(object sender, EventArgs e)
        {
            TextBox rambox = (TextBox)sender;
            string textBoxNumber = rambox.Name.Substring(5, 2);
            if (rambox.Text != "")
            {
                picsimu.ram.write((int)Convert.ToInt16(textBoxNumber, 16), (byte)Convert.ToByte((string)rambox.Text, 16));
            }
        }

        /// <summary>
        /// Event, das die Haltepunkte im ListView setzt
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listProgramcode_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            ListViewItem item = listProgramcode.Items[e.Index];
            if (string.Compare(item.Text.Substring(0, 4), "    ") == 1)
            {
                if (item.Checked == false) item.BackColor = Color.Red;
                else item.BackColor = Color.FromArgb(224, 224, 224);
            }
        }

        /// <summary>
        /// Event, das das Checken der Felder ohne Befehl verhindert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listProgramcode_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (string.Compare(e.Item.Text.Substring(0, 4), "    ") == 0)
            {
                e.Item.Checked = false;
            }
        }

        /// <summary>
        /// Diese Event aktiviert/deaktiviert den Watchdog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        private void cbWdtEnabled_CheckStateChanged(object sender, EventArgs e)
        {
            if (cbWdtEnabled.Checked == true)
            {
                tbWatchDog.Enabled = true;
            }
            else tbWatchDog.Enabled = false;

        }

        /// <summary>
        /// Hilfedokumentation aufrufen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void btnHilfe_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        System.Diagnostics.Process.Start("./Dokumentation_PICSim.pdf");
        //    }
        //    catch (Exception ex) { MessageBox.Show("Hilfedatei \"Dokumention_PICSim.pdf\" nicht vorhanden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        //}

    }
}
