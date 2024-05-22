using System.Drawing;
using System.Windows.Forms;

namespace GE_Entrance
{
    partial class Form1
    {
        private System.Windows.Forms.Label lblReaderStatus;
        private System.Windows.Forms.Label lblCompanyName;
        private System.Windows.Forms.Label lblCurrentCardInfo;
        private System.Windows.Forms.Label totalEnteredCards;
        private System.Windows.Forms.Label scannedCardLabel;
        private System.Windows.Forms.FlowLayoutPanel pnlScannedCards;
        private System.Windows.Forms.Panel pnlCurrentCard;
        private System.Windows.Forms.Panel pnlCardInfo;
        private int panelsTopLocation;
        private int thirdWidth;
        private BorderlessListBox lastEntries;

        // Clock
        private System.Windows.Forms.Timer timerClock;
        private System.Windows.Forms.Label lblClock;

        private void InitializeComponent()
        {
            this.lblReaderStatus = new System.Windows.Forms.Label();
            this.scannedCardLabel = new System.Windows.Forms.Label();
            this.totalEnteredCards = new System.Windows.Forms.Label();
            this.lblCompanyName = new System.Windows.Forms.Label();
            this.lblCurrentCardInfo = new System.Windows.Forms.Label();
            this.pnlScannedCards = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlCurrentCard = new System.Windows.Forms.Panel();
            this.pnlCardInfo = new System.Windows.Forms.Panel();
            this.lastEntries = new BorderlessListBox();
            this.panelsTopLocation = 180;

            this.lblClock = new System.Windows.Forms.Label();
            this.timerClock = new System.Windows.Forms.Timer();

            this.timerClock.Tick += new System.EventHandler(this.timerClock_Tick);


            this.SuspendLayout();

            //
            // lblClock
            //
            this.lblClock.AutoSize = true;
            this.lblClock.Location = new System.Drawing.Point(((800 - 85) / 2), 0);
            this.lblClock.Name = "lblClock";
            this.lblClock.Size = new System.Drawing.Size(85, 20);
            this.lblClock.TabIndex = 2;
            this.lblClock.ForeColor = System.Drawing.Color.Black;
            this.lblClock.Font = new System.Drawing.Font("Segoe UI", 22, System.Drawing.FontStyle.Bold);


            // 
            // lblReaderStatus
            // 
            this.lblReaderStatus.AutoSize = true;
            this.lblReaderStatus.Location = new System.Drawing.Point(((800 - 85) / 2), 60);
            this.lblReaderStatus.Name = "lblReaderStatus";
            this.lblReaderStatus.Size = new System.Drawing.Size(85, 13);
            this.lblReaderStatus.TabIndex = 0;
            this.lblReaderStatus.Text = "Статус на четецът";
            this.lblReaderStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblReaderStatus.Font = new System.Drawing.Font("Segoe UI", 24, System.Drawing.FontStyle.Bold);

            //
            // totalEnteredCards
            //
            this.totalEnteredCards.AutoSize = true;
            this.totalEnteredCards.Location = new System.Drawing.Point(((800 - 85) / 2), 180);
            this.totalEnteredCards.Name = "totalEnteredCards";
            this.totalEnteredCards.Size = new System.Drawing.Size(85, 13);
            this.totalEnteredCards.TabIndex = 2;
            this.totalEnteredCards.ForeColor = System.Drawing.Color.Black;
            this.totalEnteredCards.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.totalEnteredCards.Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);

            // 
            // pnlScannedCards
            // 
            this.pnlScannedCards.Location = new System.Drawing.Point(12, this.panelsTopLocation);
            this.pnlScannedCards.Name = "pnlScannedCards";
            this.pnlScannedCards.Size = new System.Drawing.Size(250, 300);
            this.pnlScannedCards.BackColor = System.Drawing.Color.Gray;
            this.pnlScannedCards.AutoScroll = false;
            this.pnlScannedCards.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlScannedCards.WrapContents = false;

            // 
            // pnlCurrentCard
            // 
            this.pnlCurrentCard.Location = new System.Drawing.Point(270, this.panelsTopLocation);
            this.pnlCurrentCard.Name = "pnlCurrentCard";
            this.pnlCurrentCard.Size = new System.Drawing.Size(250, 300);
            this.pnlCurrentCard.BackColor = System.Drawing.Color.Gray;
            this.pnlCurrentCard.Controls.Add(this.lblCurrentCardInfo);


            //
            // lblCurrentCardInfo
            //
            this.lblCurrentCardInfo.AutoSize = false;
            this.lblCurrentCardInfo.Location = new System.Drawing.Point(10, 30);
            this.lblCurrentCardInfo.Name = "lblCurrentCardInfo";
            this.lblCurrentCardInfo.Size = new System.Drawing.Size(100, 20);
            this.lblCurrentCardInfo.TabIndex = 2;
            this.lblCurrentCardInfo.Font = new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold);
            this.lblCurrentCardInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // pnlCardInfo
            // 
            this.pnlCardInfo.Location = new System.Drawing.Point(530, this.panelsTopLocation);
            this.pnlCardInfo.Name = "pnlCardInfo";
            this.pnlCardInfo.Size = new System.Drawing.Size(250, 800);
            this.pnlCardInfo.BackColor = System.Drawing.Color.Gray;

            // Adding labels to card info panel
            this.pnlCardInfo.Controls.Add(this.lastEntries);

            //
            // lastEntries
            //
            this.lastEntries.AutoSize = true;
            this.lastEntries.Name = "lastEntries";
            this.lastEntries.Location = new System.Drawing.Point(10, 50);
            this.lastEntries.TabIndex = 7;
            this.lastEntries.Size = new System.Drawing.Size(230, 800);
            this.lastEntries.BackColor = System.Drawing.Color.Gray;

            // Add top labels with bottom borders
            AddTopLabel(pnlScannedCards, "Последни влизания");
            AddTopLabel(pnlCurrentCard, "Текущо");
            AddTopLabel(pnlCardInfo, "Информация за карта");

            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(800, 800);
            this.Controls.Add(this.lblClock);
            this.Controls.Add(this.lblReaderStatus);
            this.Controls.Add(this.totalEnteredCards);
            this.Controls.Add(this.pnlScannedCards);
            this.Controls.Add(this.pnlCurrentCard);
            this.Controls.Add(this.pnlCardInfo);
            this.Name = "Form1";

            this.ResumeLayout(false);
            this.PerformLayout();

            ApplyStyles();
        }

        private void AddTopLabel(Control panel, string labelText)
        {
            Label topLabel = new Label
            {
                Text = labelText,
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };

            // Attach the Paint event to draw the bottom border
            topLabel.Paint += (sender, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, topLabel.ClientRectangle,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.Transparent, 0, ButtonBorderStyle.None,
                    Color.White, 1, ButtonBorderStyle.Solid);
            };

            panel.Controls.Add(topLabel);
            panel.Controls.SetChildIndex(topLabel, 0);
        }

        public class BorderlessListBox : ListBox
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.Style &= ~0x00800000; // WS_BORDER
                    cp.ExStyle &= ~0x00000200; // WS_EX_CLIENTEDGE
                    return cp;
                }
            }
        }

        private void LaunchFullscreen()
        {
            this.WindowState = FormWindowState.Maximized; // Launch fullscreen
         //   this.TopMost = true; // Optional: Keep form on top of other windows
        }
    }
}
