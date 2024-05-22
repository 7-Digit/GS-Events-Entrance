using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Forms;
using Microsoft.Win32;
using PCSC;
using PCSC.Exceptions;
using PCSC.Iso7816;
using PCSC.Monitoring;
using PCSC.Utils;
using AutoUpdaterDotNET;
using Newtonsoft.Json;
using IWshRuntimeLibrary;

namespace GE_Entrance
{
    public partial class Form1 : Form
    {
        private ISCardContext _context;
        private ISCardMonitor _monitor;
        private string _currentReaderName;
        private bool _readerConnected = false;

        private System.ComponentModel.IContainer components = null;
        private int enteredValidCards = 0;
        private int labelCount = 0;
        private string placeId = "";
        private string physicalDeviceId = "";
        private string placeName = null;
        private static string SERVER_URL = "https://cedd-84-43-197-84.ngrok-free.app";
        private string API_URL = $"{SERVER_URL}/api";

        private ScannedCardData scannedCardData;
        private List<ScannedCardData> allEntries = new List<ScannedCardData>();



        public Form1()
        {
            InitializeComponent();
            // CheckForUpdates();
            AddToStartup();
            ConnectPlace();
            LaunchFullscreen();
            UpdateLabelWidths();
            InitializeClock();
            this.Resize += new EventHandler(Form1_Resize);
        }

        private async void ConnectPlace()
        {
            var macAddress = Form1Helpers.GetMacAddress();
            using (var httpClient = new HttpClient())
            {
                var serverResponse = await httpClient.GetAsync($"https://cedd-84-43-197-84.ngrok-free.app/api/v1/admin/place-by-mac-address?macAddress={macAddress}");

                if (serverResponse.IsSuccessStatusCode)
                {
                    var result = await serverResponse.Content.ReadFromJsonAsync<PlaceByMacAddressResponse>();
                    if (result != null)
                    {
                        if (result.name != null)
                        {
                            placeName = $"Обект {result.name}";
                            placeId = result.place_id.ToString();
                            physicalDeviceId = result.physical_device_id.ToString();
                            LoadAndFilterEntries(result.reset_time);
                            ConnectReader();
                        }
                        else
                        {
                            lblReaderStatus.Text = $"Обектът не е регистриран\n{macAddress}";
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Error connecting to place.");
                    lblReaderStatus.Text = $"Обектът не е регистриран\n{macAddress}";
                }
            }

        }

        private async void ConnectReader()
        {
            Console.WriteLine("Connecting to reader...");
            try
            {
                _context = ContextFactory.Instance.Establish(SCardScope.System);
                var readerNames = _context.GetReaders();
                if (readerNames == null || readerNames.Length == 0)
                {
                    throw new InvalidOperationException("No NFC readers found.");
                }

                Console.WriteLine("Available readers:");
                foreach (var reader in readerNames)
                {
                    Console.WriteLine(reader);
                }

                // Use the PICC reader
                var piccReaderName = readerNames.FirstOrDefault(name => name.Contains("PICC"));
                Console.WriteLine($"Reader name PICC: {piccReaderName}");
                if (piccReaderName == null)
                {
                    throw new InvalidOperationException("PICC reader not found.");
                }

                _currentReaderName = piccReaderName;

                _monitor = MonitorFactory.Instance.Create(SCardScope.System);
                AttachToAllEvents(_monitor);

                _monitor.Start(_currentReaderName);

                lblReaderStatus.Text = "ЧЕТЕЦЪТ Е СВЪРЗАН";
                lblReaderStatus.Text += $"\n{placeName}";
                lblReaderStatus.ForeColor = Color.Green;
                _readerConnected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error here: {ex.Message}");
                ReaderDisconnected();
                // MessageBox.Show($"Error connecting to reader: {ex.Message}");
            }

            Form1_Resize(null, null);
        }

        private void AttachToAllEvents(ISCardMonitor monitor)
        {
            // Point the callback function(s) to the anonymous & static defined methods below.
            monitor.CardInserted += (sender, args) => DisplayEvent("CardInserted", args);
            monitor.CardRemoved += (sender, args) => DisplayEvent("CardRemoved", args);
            monitor.Initialized += (sender, args) => DisplayEvent("Initialized", args);
            monitor.StatusChanged += StatusChanged;
            monitor.MonitorException += MonitorException;
        }

        private void DisplayEvent(string eventName, CardStatusEventArgs args)
        {

            if (eventName == "CardInserted")
            {
                HandleCardInserted(args.ReaderName);
            }
        }

        private void StatusChanged(object sender, StatusChangeEventArgs args)
        {
            Console.Write($"New state {args.NewState.ToString()}");
            
        }

        private void MonitorException(object sender, PCSCException ex)
        {
            Console.WriteLine("Monitor exited due to an error:");
            Console.WriteLine($"SC Card Exception {SCardHelper.StringifyError(ex.SCardError)}");
            ReaderDisconnected();
            PollReaderReconnect();

        }

        private void PollReaderReconnect()
        {
            while (_readerConnected == false)
            {
                Console.WriteLine("Trying to reconnect reader...");
                if (InvokeRequired)
                {
                    Invoke(new Action(() => ConnectReader()));
                }
                else
                {
                    ConnectReader();
                }
              
                System.Threading.Thread.Sleep(1000);
            }   
        }

        private void ReaderDisconnected()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    lblReaderStatus.Text = "НЯМА ВРЪЗКА С ЧЕТЕЦ";
                    lblReaderStatus.ForeColor = Color.Red;
                    _readerConnected = false;
                }));
            }
            else
            {
                lblReaderStatus.Text = "НЯМА ВРЪЗКА С ЧЕТЕЦ";
                lblReaderStatus.ForeColor = Color.Red;
                _readerConnected = false;
            }
        }

        private async void HandleCardInserted(string readerName)
        {
            try
            {
                var cardReader = new IsoReader(_context, readerName, SCardShareMode.Shared, SCardProtocol.Any, false);

                // Get UID from the card
                var apduGetData = new CommandApdu(IsoCase.Case2Short, SCardProtocol.T1)
                {
                    CLA = 0xFF, // Class
                    Instruction = (InstructionCode)0xCA, // INS: Get Data
                    P1 = 0x00, // P1
                    P2 = 0x00, // P2
                    Le = 0x00 // Expected length of the data
                };

                var response = cardReader.Transmit(apduGetData);
                var cardUid = BitConverter.ToString(response.GetData());


                // Prepare data to send to the server
                var cardData = new
                {
                    cardId = cardUid,
                    placeId,
                    physicalDeviceId
                };

                using (var httpClient = new HttpClient())
                {
                    var serverResponse = await httpClient.PostAsJsonAsync($"{API_URL}/v1/cards/scan-card", cardData);
                    Console.WriteLine(serverResponse.IsSuccessStatusCode);
                    if (serverResponse.IsSuccessStatusCode)
                    {
                        var result = await serverResponse.Content.ReadFromJsonAsync<ScanCardResponse>();

                        if (result != null)
                        {
                            HandleSendEventToRenderer("card-scanned", result);
                        }
                        else
                        {
                            HandleSendEventToRenderer("card-scanned", new ScanCardResponse { Valid = false });
                        }
                    }
                    else
                    {
                        Console.WriteLine("INVALID");
                        HandleSendEventToRenderer("card-scanned", new ScanCardResponse { Valid = false });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occur3ed {ex.ToString()}");
                if (InvokeRequired)
                {
                    Invoke(new Action(() => lblReaderStatus.Text = $"Error: {ex.Message}"));
                }
                else
                {
                    lblReaderStatus.Text = $"Error: {ex.Message}";
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _monitor?.Dispose();
        }

        private void HandleSendEventToRenderer(string eventName, ScanCardResponse result)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    UpdateUIWithCardResult(result);
                }));
            }
            else
            {
                UpdateUIWithCardResult(result);
            }
        }

        private void UpdateUIWithCardResult(ScanCardResponse result)
        {
            SetScannedCardData(result);
            if (result.Valid)
            {
                lblCurrentCardInfo.ForeColor = Color.White;
                pnlCurrentCard.BackColor = Color.Green;

                if (result.HasEntryInLastPeriod)
                {
                    lblCurrentCardInfo.Text += "ПОВТОРНО ВЛИЗАНЕ\n";
                    pnlCurrentCard.BackColor = Color.Orange;

                }

                if (!String.IsNullOrEmpty(result.OwnerName))
                {
                    lblCurrentCardInfo.Text = $"Притежател - {result.OwnerName}\n";
                }

                lblCurrentCardInfo.Text += $"Номер на карта - #{result.CardData.SeqNumber}\n\n";
                lblCurrentCardInfo.Text += $"Дата на валидност - {result.CardData.ValidTo}\n";
                lblCurrentCardInfo.Text += $"Дата на активация - {result.CardData.FirstUse}\n";
            }
            else
            {
                lblCurrentCardInfo.Text = "Картата е невалидна\n";
                lblCurrentCardInfo.ForeColor = Color.White;
                pnlCurrentCard.BackColor = Color.Red;

                if (result.ConfiscateCard)
                {
                    lblCurrentCardInfo.Text += " - ВЗЕМИ КАРТА\n";
                }
                else if (result.IsExpired)
                {
                    lblCurrentCardInfo.Text += $" - Услугата '{result.ServiceName}' е изтекла\n";
                }
                else if (result.IsInactive)
                {
                    lblCurrentCardInfo.Text += " - Картата е неактивна";
                }
                else if (result.IsSingleUse)
                {
                    lblCurrentCardInfo.Text += " - Single use card";
                }
            }

            var validText = result.Valid ? "Валидно" : "Невалидно";
            var ownerName = String.IsNullOrEmpty(result.OwnerName) ? "" : result.OwnerName;
            var secondaryEntry = result.HasEntryInLastPeriod ? "Повторно влизане -" : "";
            var seqNumber = result.CardData.SeqNumber;
            var date = DateTime.Now.ToString("dd.MM, HH:mm");

            var scannedCardLabel = "";
            if (String.IsNullOrEmpty(seqNumber))
            {
                scannedCardLabel = $"Невалидно - {date}";
            }
            else
            {
                scannedCardLabel = $"#{result.CardData.SeqNumber} - {validText} - {ownerName} - {secondaryEntry} {DateTime.Now.ToString("dd.MM, HH:mm")}ч.";
            }

            AddScannedCard(
                scannedCardLabel,
                result.Valid,
                result.HasEntryInLastPeriod,
                result.ServiceUsages
                );

            UpdateScannedCardData(result);
        }

        private void AddScannedCard(string labelText, bool Valid, bool secondaryEntry, List<ServiceUsage> serviceUsages)
        {

            AddLabelToScannedCardsPanel(
                labelText,
                Valid,
                secondaryEntry,
                secondaryEntry ? System.Drawing.Color.Orange : Valid ? System.Drawing.Color.Green : System.Drawing.Color.Red
            );

            lastEntries.Controls.Clear();

            if (serviceUsages.Count > 0)
            {
                Console.WriteLine("Service usages count: " + serviceUsages.Count);
                Label entriesLabel = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 19),
                    ForeColor = Color.White
                };

                entriesLabel.Text += $"Общо влизания с текущата карта: {serviceUsages.Count}\n\n";
                entriesLabel.Text += "Последни влизания:\n";
                for (int i = 0; i < serviceUsages.Count; i++)
                {
                    // Label entriesLabel = new Label();
                    // entriesLabel.AutoSize = true;
                    var usage = serviceUsages[i];
                    entriesLabel.Text += $"{i+1}. {Form1Helpers.ParseJavaScriptDate(usage.used_at)}\n";
                    entriesLabel.Location = new System.Drawing.Point(10, 15);
                    entriesLabel.Name = $"lastEntries-{i}";
                    lastEntries.Controls.Add(entriesLabel);
                   //if (i == 9) break;

                }
            }
            UpdateLabelWidths();
        }

        private void SetScannedCardData(ScanCardResponse result)
        {
            // Update the scanned card data
            scannedCardData = new ScannedCardData
            {
                Valid = result.Valid,
                IsScanned = true,
                OwnerName = result.OwnerName,
                CardSeqNumber = result.CardData.SeqNumber,
                Entries = result.ServiceUsages,
                TotalEntries = result.TotalUsages + 1,
                IsExpired = result.IsExpired,
                HasEntryInLastPeriod = result.HasEntryInLastPeriod,
                IsSingleUse = result.IsSingleUse,
                ConfiscateCard = result.ConfiscateCard,
                ServiceName = result.ServiceName,
                Timestamp = DateTime.Now,
                IsInactive = result.IsInactive,
                CompanyName = result.CompanyName,
                FirstUse = result.CardData.FirstUse,
                ValidTo = result.CardData.ValidTo
            };

            allEntries.Add(scannedCardData);
            JsonStorage.SaveData(allEntries);
            var validEntries = allEntries.Where(x => x.Valid).ToList();
            var uniqueCards = validEntries.Select(x => x.CardSeqNumber).Distinct().Count();
            Console.WriteLine($"Unique cards: {validEntries.Select(x => x.CardSeqNumber).ToString()}");
            enteredValidCards = uniqueCards;
            totalEnteredCards.Text = $"Влезли карти: {enteredValidCards}";
        }

        private void UpdateScannedCardData(ScanCardResponse result)
        {
            lblCompanyName.Text = $"Фирма: {result.CompanyName}";
        }

        private void ApplyStyles()
        {
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            pnlScannedCards.BorderStyle = BorderStyle.FixedSingle;

          //  pnlCurrentCard.Padding = new Padding(10);
            pnlCurrentCard.BorderStyle = BorderStyle.FixedSingle;

           // pnlCardInfo.Padding = new Padding(10);
            pnlCardInfo.BorderStyle = BorderStyle.FixedSingle;
        }

        private void UpdateLabelWidths()
        {
            int panelWidth = pnlScannedCards.ClientSize.Width;
            foreach (Control control in pnlScannedCards.Controls)
            {
                if (control is Label label)
                {
                    label.AutoSize = true;
                    label.MinimumSize = new Size(panelWidth, 0);
                    // label.MaximumSize.Width = panelWidth;
                    label.Width = panelWidth;
                }
            }
        }


        private void AddLabelToScannedCardsPanel(string labelText, bool Valid, bool secondaryEntry, Color backColor)
        {
            Label newLabel = new Label
            {
                AutoSize = true,
                Name = "label" + labelCount,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(3),
                TabIndex = labelCount,
                BackColor = backColor,
                ForeColor = Color.White,
                Text = labelText,
                MinimumSize = new Size(pnlScannedCards.ClientSize.Width, 0),
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
            };
            newLabel.Paint += (sender, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, newLabel.ClientRectangle,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.White, 1, ButtonBorderStyle.Solid);
            };

            pnlScannedCards.Controls.Add(newLabel);
            pnlScannedCards.Controls.SetChildIndex(newLabel, 1); // Insert at the top
            labelCount++;
        }

        private void AddToStartup()
        {
            try
            {
                string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(runKey, true);

                if (startupKey.GetValue(Application.ProductName) == null)
                {
                    // Add the application to startup
                    startupKey.SetValue(Application.ProductName, $"\"{Application.ExecutablePath}\"");
                    MessageBox.Show("Application added to startup.");
                }
                MessageBox.Show("Application already in startup.");

            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add application to startup: " + ex.Message);
                Console.WriteLine("Failed to add application to startup: " + ex.Message);
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            int thirdWidth = (int)(this.ClientSize.Width / 2.95);
            int eightyHeight = (int)(this.ClientSize.Height - this.lblReaderStatus.Height - this.totalEnteredCards.Height - 50);

            // Update panel widths
            this.pnlScannedCards.Width = thirdWidth;
            this.pnlCurrentCard.Width = thirdWidth;
            this.pnlCardInfo.Width = thirdWidth;
            this.lastEntries.Width = thirdWidth - 20;
            this.lblCurrentCardInfo.Width = thirdWidth - 20;


            // Update panel heights
            this.pnlScannedCards.Height = eightyHeight;
            this.pnlCurrentCard.Height = eightyHeight;
            this.pnlCardInfo.Height = eightyHeight;
            this.lastEntries.Height = eightyHeight - 50;
            this.lblCurrentCardInfo.Height = eightyHeight - 50;


            this.pnlCurrentCard.Left = this.pnlScannedCards.Right;
            this.pnlCardInfo.Left = this.pnlCurrentCard.Right;

            //
            // Center labels
            //
            int x = this.ClientSize.Width / 2;
            this.lblClock.Location = new Point(x - this.lblClock.Width / 2, 0);
            this.lblReaderStatus.Location = new Point(x - this.lblReaderStatus.Width / 2, 30);
            this.totalEnteredCards.Location = new Point(x - this.totalEnteredCards.Width / 2, 120);
            Console.WriteLine($"Client width: {this.ClientSize.Width}");
            Console.WriteLine($"Middle: {x - this.totalEnteredCards.Width / 2}");

            UpdateLabelWidths(); // Update label widths when the form is resized
        }

        private void InitializeClock()
        {
            // Set the timer interval to 1 second (1000 milliseconds)
            timerClock.Interval = 1000;
            // Start the timer
            timerClock.Start();
            // Update the label immediately with the current time
            UpdateClock();
        }

        private void timerClock_Tick(object sender, EventArgs e)
        {
            // Update the label with the current time on each tick
            UpdateClock();
        }

        private void UpdateClock()
        {
            // Set the label text to the current time
            lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void CheckForUpdates()
        {
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.Start($"{SERVER_URL}/update.xml");
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            Console.WriteLine($"Check for update {JsonConvert.SerializeObject(args)}");
            if (args.IsUpdateAvailable)
            {
                if (args.Mandatory.Value)
                {
                    AutoUpdater.DownloadUpdate(args);

                }
            }
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            MessageBox.Show("The application is closing to apply the update.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //
        // Interface classes
        //
        public class CardData
        {
            public bool Valid { get; set; }
            public string OwnerName { get; set; }
            public string CardSeqNumber { get; set; }
            public List<ServiceUsage> Entries { get; set; }
            public int TotalEntries { get; set; }
            public bool IsExpired { get; set; }
            public bool HasEntryInLastPeriod { get; set; }
            public bool IsSingleUse { get; set; }
            public bool ConfiscateCard { get; set; }
            public string ServiceName { get; set; }
            public bool IsInactive { get; set; }
            public string CompanyName { get; set; }
            public string FirstUse { get; set; }
            public string ValidTo { get; set; }
        }
        public class ScannedCardData : CardData
        {
            public bool IsScanned { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class ScanCardResponse
        {
            public bool Valid { get; set; }
            public Guid PlaceId { get; set; }
            public CardDataResponse CardData { get; set; }
            public string OwnerName { get; set; }
            public List<ServiceUsage> ServiceUsages { get; set; } = new List<ServiceUsage>();
            public int TotalUsages { get; set; }
            public bool IsExpired { get; set; }
            public bool HasEntryInLastPeriod { get; set; }
            public bool IsSingleUse { get; set; }
            public bool ConfiscateCard { get; set; }
            public string ServiceName { get; set; }
            public string CompanyName { get; set; }
            public bool IsInactive { get; set; }
        }

        public class ServiceUsage
        {
            public Guid ServiceId { get; set; }
            public string used_at { get; set; }
        }

        public class CardDataResponse
        {
            public string CardId { get; set; }
            public string SeqNumber { get; set; }
            public string ValidTo { get; set; }
            public string FirstUse { get; set; }
        }

        public class PlaceByMacAddressResponse
        {
            public string name { get; set; }
            public Guid place_id { get; set; }
            public Guid physical_device_id { get; set; }

            public string reset_time { get; set; }
        }

        private void LoadAndFilterEntries(string resetTime)
        {
            var entries = JsonStorage.LoadData<List<ScannedCardData>>();
            if (entries == null)
            {
                JsonStorage.SaveData(new List<ScannedCardData>());
            }
            else
            {
                var resetHour = int.Parse(resetTime.Split(':')[0]);
                var resetMinutes = int.Parse(resetTime.Split(':')[1]);
                var resetDate = DateTime.Today.AddHours(resetHour).AddMinutes(resetMinutes);
                var now = DateTime.Now;
                List<ScannedCardData> currentDayEntries;

                if (now > resetDate)
                {
                    currentDayEntries = entries.Where(x => x.Timestamp > resetDate).ToList();
                }
                else
                {
                    var yesterday = resetDate.AddDays(-1);
                    currentDayEntries = entries.Where(x => x.Timestamp > yesterday).ToList();
                }

                // Filter valid entries and count unique cards
                var validEntries = currentDayEntries.Where(x => x.Valid).ToList();
                var uniqueCards = validEntries.Select(x => x.CardSeqNumber).Distinct().Count();
                enteredValidCards = uniqueCards;
                totalEnteredCards.Text = $"Влезли карти: {enteredValidCards}";
                allEntries = currentDayEntries;

                for (int i = 0; i < allEntries.Count; i++)
                {
                    var entry = allEntries[i];
                    var Valid = entry.Valid;
                    var secondaryEntry = entry.HasEntryInLastPeriod;
                    var labelText = $"#{entry.CardSeqNumber} - {Valid} - {entry.OwnerName} - {secondaryEntry} {entry.Timestamp.ToString("dd.MM, HH:mm")}ч.";
                    AddLabelToScannedCardsPanel(
                        labelText,
                        Valid,
                        secondaryEntry,
                        // back color
                        secondaryEntry ? System.Drawing.Color.Orange : Valid ? System.Drawing.Color.Green : System.Drawing.Color.Red
                    );

                }
            }
        }
    }


}