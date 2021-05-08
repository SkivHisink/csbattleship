using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace csbattleship
{
    public class NetworkClass
    {
        const int port = 8888;
        static TcpListener listener;
        public TcpClient client;
        NetworkStream stream = null;

        public NetworkClass(TcpClient tcpClient)
        {
            client = tcpClient;
        }

        public void Worker()
        {
            try
            {
                stream = client.GetStream();
                byte[] data = new byte[64];
                while (true)
                {
                    string message = "";
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        message += Encoding.UTF8.GetString(data, 0, bytes);
                    }
                    while (stream.DataAvailable);

                    if (message.Length > 0)
                    {
                        data = Encoding.UTF8.GetBytes("[получено] " + message);
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        public static void Server()
        {
            try
            {
                listener = new TcpListener(IPAddress.Parse(Program.f.host), Program.f.port);
                listener.Start();

                TcpClient client = listener.AcceptTcpClient();
                NetworkClass clientObject = new(client);
                Thread clientThread = new(new ThreadStart(clientObject.Worker));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }
        }

        public static void Client()
        {
            try
            {
                TcpClient client = new(Program.f.host, Program.f.port);
                NetworkClass clientObject = new(client);
                Thread clientThread = new(new ThreadStart(clientObject.Worker));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }


    public partial class MainForm : Form
    {
        // Настройки цвета
        readonly Color border_color = Color.FromArgb(14, 40, 120);
        readonly Color sea_color = Color.FromArgb(15, 45, 135);
        readonly Color ship_color = Color.FromArgb(19, 19, 19);
        readonly Color miss_color = Color.FromArgb(40, 50, 80);
        readonly Color hit_color = Color.FromArgb(40, 50, 80);

        public string host = "127.0.0.1";
        public int port = 7070;

        int gameStatus = 0;

        // Ячейки кораблей
        int currentParts = 0;
        readonly int totalParts = 20;

        bool vertical;
        bool horizontal;
        string last_set_coords = "";

        string transportData;

        bool IsClient = true;

        public Dictionary<string, Button> leftCellField = new();
        public Dictionary<string, Button> rigthCellField = new();

        public MainForm()
        {
            Program.f = this;
            InitializeComponent();
        }

        void Form1_Load(object sender, EventArgs e)
        {
            tableLayoutPanelLeft.BackColor = this.border_color;
            tableLayoutPanelRigth.BackColor = this.border_color;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Button leftCell = new();
                    Button rigthCell = new();

                    leftCell.Click += new EventHandler(LeftCellField_Click);
                    rigthCell.Click += new EventHandler(RigthCellField_Click);

                    leftCell.FlatAppearance.BorderSize = 0;
                    rigthCell.FlatAppearance.BorderSize = 0;

                    Font font = new("Perpetua", 1);
                    leftCell.Font = font;
                    rigthCell.Font = font;

                    leftCell.Name = $"L{i}{j}";
                    rigthCell.Name = $"R{i}{j}";

                    leftCell.Dock = DockStyle.Fill;
                    rigthCell.Dock = DockStyle.Fill;

                    leftCell.FlatStyle = FlatStyle.Flat;
                    rigthCell.FlatStyle = FlatStyle.Flat;

                    SetCellStatus(leftCell, 0);
                    SetCellStatus(rigthCell, 0);

                    leftCellField.Add($"{i}{j}", leftCell);
                    rigthCellField.Add($"{i}{j}", rigthCell);

                    tableLayoutPanelLeft.Controls.Add(leftCell, i, j);
                    tableLayoutPanelRigth.Controls.Add(rigthCell, i, j);
                }
            }
        }

        void StartGame(object sender, EventArgs e)
        {
            if (Validvalidation())
            {
                this.gameStatus = 1;
                buttonStart.Enabled = buttonClear.Enabled = textBoxHost.Enabled = textBoxPort.Enabled = false;
                radioButtonClient.Enabled = radioButtonServer.Enabled = false;
                
                if (this.IsClient)
                {
                    NetworkClass.Client();
                }
                else
                {
                    NetworkClass.Server();
                }
            }
        }

        bool Validvalidation()
        {
            if (textBoxHost.Text == "")
                textBoxHost.Text = textBoxHost.PlaceholderText;
            if (textBoxPort.Text == "")
                textBoxPort.Text = textBoxPort.PlaceholderText;

            IPAddress ip;
            IPAddress.TryParse(textBoxHost.Text, out ip);

            if (ip is null)
            {
                SendMessage("Не верно указан IP адресс.");
                return false;
            }
            else if (int.TryParse(textBoxPort.Text, out _) == false)
            {
                SendMessage("Не верно указан порт.");
                return false;
            }
            else if (Convert.ToInt32(textBoxPort.Text) < 1000 || Convert.ToInt32(textBoxPort.Text) > Math.Pow(2, 16))
            {
                SendMessage("Не верно указан порт.");
                return false;
            }
            else if (this.currentParts != this.totalParts)
            {
                SendMessage("Не все корабли расставлены.");
                return false;
            }

            this.host = textBoxHost.Text;
            this.port = Convert.ToInt32(textBoxPort.Text);
            return true;
        }

        void LeftCellField_Click(object sender, EventArgs e)
        {
            if (this.gameStatus == 0)
            {
                Button cell = (Button)sender;

                if (cell.Text == "1")
                {
                    SetCellStatus(cell, 0);
                    this.currentParts -= 1;
                }
                else if (cell.Text == "0" && this.currentParts < this.totalParts)
                {
                    if (CanSetShip(cell))
                    {
                        SetCellStatus(cell, 1);
                        this.currentParts += 1;
                    }
                }
            }
        }

        void RigthCellField_Click(object sender, EventArgs e)
        {
            if (this.gameStatus == 2)
            {
                this.transportData = $"{((Button)sender).Name}";
            }
        }

        bool CanSetShip(Button cell)
        {
            if (IsCellClear(cell, -1, -1) && IsCellClear(cell, -1, +1) && IsCellClear(cell, +1, -1) && IsCellClear(cell, +1, +1))
            {
                if ((this.currentParts == 5 || this.currentParts == 6 || this.currentParts == 8 || this.currentParts == 9
                     || this.currentParts == 11 || this.currentParts == 13 || this.currentParts == 15) && IsNearLastCoords(cell) == false)
                {
                    return false;
                }

                if ((this.currentParts == 0 || this.currentParts == 4 || this.currentParts == 7 || this.currentParts == 10 ||
                    this.currentParts == 12 || this.currentParts == 14 || this.currentParts >= 16) &&
                    (IsCellClear(cell, -1, 0) && IsCellClear(cell, 0, -1) && IsCellClear(cell, 0, +1) && IsCellClear(cell, +1, 0)))
                {
                    this.horizontal = false;
                    this.vertical = false;

                    UpdateLastCoords(cell);
                    return true;
                }

                else if (this.currentParts == 1 || this.currentParts == 5 || this.currentParts == 8)
                {
                    if (IsCellClear(cell, -1, 0) == false || IsCellClear(cell, +1, 0) == false)
                    {
                        this.horizontal = true;
                        this.vertical = false;

                        UpdateLastCoords(cell);
                        return true;
                    }
                    else if (IsCellClear(cell, 0, -1) == false || IsCellClear(cell, 0, +1) == false)
                    {
                        this.vertical = true;
                        this.horizontal = false;

                        UpdateLastCoords(cell);
                        return true;
                    }
                }

                else if ((this.currentParts == 2 || this.currentParts == 3 || this.currentParts == 6 || this.currentParts == 9) &&
                    ((this.horizontal && (IsCellClear(cell, -1, 0) == false || IsCellClear(cell, +1, 0) == false)) ||
                        (this.vertical && (IsCellClear(cell, 0, +1) == false || IsCellClear(cell, 0, -1) == false))))
                {
                    UpdateLastCoords(cell);
                    return true;
                }

                else if ((this.currentParts == 11 || this.currentParts == 13 || this.currentParts == 15) &&
                    (IsCellClear(cell, -1, 0) == false || IsCellClear(cell, +1, 0) == false || IsCellClear(cell, 0, -1) == false || IsCellClear(cell, 0, +1) == false))
                {
                    UpdateLastCoords(cell);
                    return true;
                }
            }

            SendMessage("недоступно");
            return false;
        }

        void UpdateLastCoords(Button cell)
        {
            this.last_set_coords = "";
            this.last_set_coords += Convert.ToInt32(cell.Name[1].ToString());
            this.last_set_coords += Convert.ToInt32(cell.Name[2].ToString());
        }

        bool IsNearLastCoords(Button cell)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    try
                    {
                        string iter_coords = $"{Convert.ToInt32(cell.Name[1].ToString()) + i}{Convert.ToInt32(cell.Name[2].ToString()) + j}";

                        if (leftCellField[iter_coords].Text == "1" && $"{this.last_set_coords[0]}{this.last_set_coords[1]}" == iter_coords)
                        {
                            return true;
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }
                }
            }

            return false;
        }

        void SetCellStatus(Button cell, int status)
        {
            // Левое поле
            if (status == 0)    // пусто
            {
                cell.BackColor = this.sea_color;
                cell.Text = "0";
            }
            else if (status == 1)   // целый
            {
                cell.BackColor = this.ship_color;
                cell.Text = "1";
            }
            // Правое поле
            else if (status == 2)   // промах
            {
                cell.BackColor = this.miss_color;
                cell.Text = "2";
            }
            else if (status == 3)   // попадание
            {
                cell.BackColor = this.hit_color;
                cell.Text = "3";
            }
        }

        bool IsCellClear(Button cell, int dx, int dy)
        {
            try
            {
                if (leftCellField[$"{Convert.ToInt32(cell.Name[1].ToString()) + dx}{Convert.ToInt32(cell.Name[2].ToString()) + dy}"].Text == "0")
                {
                    return true;
                }
                return false;
            }
            catch (KeyNotFoundException)
            {
                return true;
            }
        }

        void ButtonSend_Click(object sender, EventArgs e)
        {
            string text = textBoxInput.Text;
            if (text != "")
            {
                textBoxInput.Clear();
                SendMessage(text, "player");
            }

        }

        void SendMessage(string text, string from = "sys")
        {
            text = text.Trim();
            if (text != "")
            {
                listBoxChat.Items.Add($"{from}: {text}");
                listBoxChat.TopIndex = listBoxChat.Items.Count - 1;
            }
        }

        void ButtonClear_Click(object sender, EventArgs e)
        {
            if (this.gameStatus == 0)
            {
                foreach (Button cell in tableLayoutPanelLeft.Controls)
                {
                    SetCellStatus(cell, 0);
                }

                this.currentParts = 0;
            }
        }

        private void radioButtonClient_CheckedChanged(object sender, EventArgs e)
        {
            this.IsClient = !this.IsClient;
            if (this.IsClient)
            {
                SendMessage("выбран Client");
            }
            else if (!this.IsClient)
            {
                SendMessage("выбран Server");
            }    
        }
    }
}
