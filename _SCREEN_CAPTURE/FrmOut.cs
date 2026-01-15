using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Threading;
using System.Drawing.Imaging;
using _SCREEN_CAPTURE.Interop;


namespace _SCREEN_CAPTURE
{
    public partial class FrmOut : Form
    {
        public FrmOut(Bitmap bmp)
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None;//导致无法缩放
            this.TopMost = true;
            m_bmp = bmp;

            this.FormClosing += (s, e) => m_bmp.Dispose();

            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        private Bitmap m_bmp;
        public Bitmap Bmp
        {
            get { return m_bmp; }
        }

        private Point m_ptOriginal;
        private bool m_bMouseEnter;
        private bool m_bLoad;
        private bool m_bMinimum;
        private bool m_bMaxmum;
        private Size m_szForm;
        private float m_fScale;

        private Rectangle m_rectSaveO;
        private Rectangle m_rectSaveC;
        private Rectangle m_rectHelp;

        private bool m_bOnSaveO;
        private bool m_bOnSaveC;
        private bool m_bOnClose;
        private bool m_bOnHelp;
        private bool m_bOnMinimize;

        private const int gripSize = 10; // 拖动区域大小

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Width = m_bmp.Width + 2;
            this.Height = m_bmp.Height + 2;
            m_szForm = m_bmp.Size;
            m_fScale = 1;
            this.Twist();
        }

        private Cursor GetCursor(Point cursor)
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            bool left = cursor.X <= gripSize;
            bool right = cursor.X >= w - gripSize;
            bool top = cursor.Y <= gripSize;
            bool bottom = cursor.Y >= h - gripSize;

            // 四角
            if (left && top) return Cursors.SizeNWSE;
            if (right && top) return Cursors.SizeNESW;
            if (left && bottom) return Cursors.SizeNESW;
            if (right && bottom) return Cursors.SizeNWSE;

            // 四边
            if (left || right) return Cursors.SizeWE;
            if (top || bottom) return Cursors.SizeNS;

            // 默认可拖动区域
            return Cursors.Default;
        }

        protected override CreateParams CreateParams//为了启用最小化按钮
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x00020000;
                const int WS_SYSMENU = 0x00080000;

                var cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX; // 关键：加上最小化按钮样式
                cp.Style |= WS_SYSMENU;     // 没有系统菜单也会导致最小化失效
                return cp;
            }
        }


        protected override void WndProc(ref Message m)//为了缩放时也能触发 MouseWheel 事件
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                Point cursor = this.PointToClient(Cursor.Position);
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;

                bool left = cursor.X <= gripSize;
                bool right = cursor.X >= w - gripSize;
                bool top = cursor.Y <= gripSize;
                bool bottom = cursor.Y >= h - gripSize;

                // 只有边缘才返回 HT*，内部保持 HTCLIENT
                if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (left) m.Result = (IntPtr)HTLEFT;
                else if (right) m.Result = (IntPtr)HTRIGHT;
                else if (top) m.Result = (IntPtr)HTTOP;
                else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                else m.Result = (IntPtr)HTCLIENT; // 内部区域仍然触发 MouseWheel
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right) this.Close();//防止右键关闭
            if (e.Button == MouseButtons.Right)
            {
                this.ContextMenuStrip = contextMenuStrip1;
                this.ContextMenuStrip.Show(MousePosition);

            }
            if (e.Button == MouseButtons.Left)
            {
                if (e.X >= this.Width - 50 && e.Y <= 50) this.Close();//关闭按钮
                if (e.X >= this.Width - 50 - 60 && e.Y <= 50) this.WindowState = FormWindowState.Minimized;//最小化按钮

            }
            if (m_bOnSaveC) SaveBmp(false);
            if (m_bOnSaveO) SaveBmp(true);
            if (m_bOnHelp) MessageBox.Show("鼠标滚轮控制缩放", "[截图助手]", MessageBoxButtons.OK, MessageBoxIcon.Information);
            base.OnMouseClick(e);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.Twist();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            m_ptOriginal = e.Location;
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Cursor = GetCursor(e.Location);

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
                this.Location = (Point)((Size)MousePosition - (Size)m_ptOriginal);
            if (m_rectSaveO.Contains(e.Location))
            {
                if (!m_bOnSaveO)
                {
                    m_bOnSaveO = true;
                    this.Invalidate(m_bOnSaveO);
                }
            }
            else
            {
                if (m_bOnSaveO)
                {
                    m_bOnSaveO = false;
                    this.Invalidate(m_bOnSaveO);
                }
            }
            if (m_rectSaveC.Contains(e.Location))
            {
                if (!m_bOnSaveC)
                {
                    m_bOnSaveC = true;
                    this.Invalidate(m_rectSaveC);
                }
            }
            else
            {
                if (m_bOnSaveC)
                {
                    m_bOnSaveC = false;
                    this.Invalidate(m_rectSaveC);
                }
            }
            if (e.X >= this.Width - 50 && e.Y <= 50)
            {
                if (!m_bOnClose)
                {
                    m_bOnClose = true;
                    this.ContextMenuStrip = contextMenuStrip1;
                    this.Invalidate(new Rectangle(this.Width - 50, 1, 49, 49));
                }
            }
            else
            {
                if (m_bOnClose)
                {
                    m_bOnClose = false;
                    this.ContextMenuStrip = null;
                    this.Invalidate(new Rectangle(this.Width - 50, 1, 49, 49));
                }
            }

            if (m_rectHelp.Contains(e.Location))
            {
                if (!m_bOnHelp)
                {
                    m_bOnHelp = true;
                    this.Invalidate(m_rectHelp);
                }
            }
            else
            {
                if (m_bOnHelp)
                {
                    m_bOnHelp = false;
                    this.Invalidate(m_rectHelp);
                }
            }

            if (e.X >= this.Width - 50 - 60 && e.Y <= 50)
            {
                if (!m_bOnMinimize)
                {
                    m_bOnMinimize = true;
                    this.ContextMenuStrip = contextMenuStrip1;
                    this.Invalidate(new Rectangle(this.Width - 50 - 60, 1, 49, 49));
                }
            }
            else
            {
                if (m_bOnMinimize)
                {
                    m_bOnMinimize = false;
                    this.ContextMenuStrip = null;
                    this.Invalidate(new Rectangle(this.Width - 50 - 60, 1, 49, 49));
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            m_bMouseEnter = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            m_bOnSaveC = m_bOnSaveO = m_bMouseEnter = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (m_bMouseEnter)
            {
                float nIncrement = 0;
                if (e.Delta > 0)
                {
                    if (this.Width < Screen.PrimaryScreen.Bounds.Width
                        || this.Height < Screen.PrimaryScreen.Bounds.Height)
                        nIncrement = 0.1F;
                    else return;
                }
                if (e.Delta < 0)
                {
                    if (this.Width > 100 || this.Height > 30)
                        nIncrement = -0.1F;
                    else return;
                }

                m_fScale += nIncrement;
                if (!m_bMinimum && !m_bMaxmum)
                {
                    this.Left = (int)(MousePosition.X - (int)(e.X / (m_fScale - nIncrement)) * m_fScale);
                    this.Top = (int)(MousePosition.Y - (int)(e.Y / (m_fScale - nIncrement)) * m_fScale);
                }
                this.Width = (int)(m_szForm.Width * m_fScale + 2);
                this.Height = (int)(m_szForm.Height * m_fScale + 2);
            }
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (m_bmp == null)
            {
                MessageBox.Show("Bitmap cannot be null!");
                this.Close();
            }
            Graphics g = e.Graphics;
            g.DrawImage(m_bmp, 1, 1, this.Width - 2, this.Height - 2);
            g.DrawRectangle(Pens.Cyan, 0, 0, this.Width - 1, this.Height - 1);
            if (m_bMouseEnter || m_bLoad)
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                {
                    g.FillRectangle(sb, 1, 1, this.Width - 2, this.Height - 2);
                    sb.Color = Color.FromArgb(150, 0, 255, 255);

                    StringFormat sf = new StringFormat();

                    string strDraw = "Original:\t[" + m_bmp.Width + "," + m_bmp.Height + "]"
                        + "\tScale:" + ((double)(this.Width - 2) / m_bmp.Width).ToString("F2") + "[W]"
                        + "\r\nCurrent:\t[" + (this.Width - 2) + "," + (this.Height - 2) + "]"
                        + "\tScale:" + ((double)(this.Height - 2) / m_bmp.Height).ToString("F2") + "[H]";
                    sf.SetTabStops(0.0F, new float[] { 60.0F, 60.0F });
                    Rectangle rectString = new Rectangle(new Point(1, 1), g.MeasureString(strDraw, this.Font, this.Width, sf).ToSize());
                    rectString.Inflate(1, 1);
                    g.FillRectangle(sb, rectString);
                    g.DrawString(strDraw, this.Font, Brushes.Wheat, rectString, sf);

                    rectString = new Rectangle(0, this.Height - 2 * this.Font.Height - 1,
                        this.Width, this.Font.Height * 2);
                    sf.Alignment = StringAlignment.Far;
                    g.FillRectangle(sb, rectString);
                    g.DrawString("Move [W,S,A,D] ReSize [T,G,F,H]\r\nScale [MouseWheel] Exit [MouseRight]", this.Font, Brushes.Wheat, rectString, sf);
                    g.DrawString("SaveOriginal\r\nSaveCurrent", this.Font, Brushes.Wheat, rectString.X + 20, rectString.Y);

                    g.DrawString("Help", this.Font, Brushes.Wheat, rectString.X + 150, rectString.Y);

                    g.FillRectangle(sb, this.Width - 51, 1, 50, 50);
                    if (m_bOnClose)
                        g.FillRectangle(Brushes.Red, this.Width - 50, 1, 49, 49);

                    g.FillEllipse(sb, this.Width - 51 - 60, 1, 50, 50);
                    if (m_bOnMinimize)
                        g.FillEllipse(Brushes.Yellow, this.Width - 50 - 60, 1, 49, 49);

                    sb.Color = m_bOnSaveO ? Color.Red : Color.Wheat;
                    m_rectSaveO = new Rectangle(2, rectString.Y + 2, 20, this.Font.Height - 3);
                    g.FillRectangle(sb, m_rectSaveO);
                    sb.Color = m_bOnSaveC ? Color.Red : Color.Wheat;
                    m_rectSaveC = new Rectangle(2, rectString.Y + this.Font.Height + 1, 20, this.Font.Height - 2);
                    g.FillRectangle(sb, m_rectSaveC);

                    sb.Color = m_bOnHelp ? Color.Red : Color.Wheat;
                    m_rectHelp = new Rectangle(130, rectString.Y + 2, 20, this.Font.Height - 3);
                    g.FillRectangle(sb, m_rectHelp);
                }
            }
            base.OnPaint(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == 'w') this.Top -= 1;
            if (e.KeyChar == 's') this.Top += 1;
            if (e.KeyChar == 'a') this.Left -= 1;
            if (e.KeyChar == 'd') this.Left += 1;
            if (e.KeyChar == 't') m_szForm.Height = (int)(((this.Height -= 1) - 2) / m_fScale);
            if (e.KeyChar == 'g') m_szForm.Height = (int)(((this.Height += 1) - 2) / m_fScale);
            if (e.KeyChar == 'f') m_szForm.Width = (int)(((this.Width -= 1) - 2) / m_fScale);
            if (e.KeyChar == 'h') m_szForm.Width = (int)(((this.Width += 1) - 2) / m_fScale);

            //if (e.KeyChar == ((char)ConsoleKey.Escape)) this.Close();  //暂时禁用
            base.OnKeyPress(e);
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            int w = Screen.PrimaryScreen.Bounds.Width;
            int h = Screen.PrimaryScreen.Bounds.Height;
            if (width < 100) width = 100;
            if (width > Screen.PrimaryScreen.Bounds.Width) width = w;
            if (height < 30) height = 30;
            if (height > Screen.PrimaryScreen.Bounds.Height) height = h;
            m_bMinimum = width == 100 || height == 30;
            m_bMaxmum = width == w || height == h;
            if (m_bMaxmum) x = y = 0;
            base.SetBoundsCore(x, y, width, height, specified);
        }

        private void TMenuItem_OriginalToClip_Click(object sender, EventArgs e)
        {
            this.SetClipBoard(true);
        }

        private void TMenuItem_CurrentToClip_Click(object sender, EventArgs e)
        {
            this.SetClipBoard(false);
        }

        private void TMenuItem_SaveOriginal_Click(object sender, EventArgs e)
        {
            this.SaveBmp(true);
        }

        private void TMenuItem_SaveCurrent_Click(object sender, EventArgs e)
        {
            this.SaveBmp(false);
        }

        private void TMenuItem_Size_Click(object sender, EventArgs e)
        {
            FrmSize frmSize = new FrmSize(new Size(this.Width - 2, this.Height - 2));
            if (frmSize.ShowDialog() == DialogResult.OK)
            {
                this.Width = frmSize.ImageSize.Width + 2;
                this.Height = frmSize.ImageSize.Height + 2;
                m_szForm.Width = (int)((this.Width - 2) / m_fScale);
                m_szForm.Height = (int)((this.Height - 2) / m_fScale);
            }
        }

        private void TMenuItem_Help_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "MoveWindow\t[W,A,S,D],[MouseMiddle]\r\n\t\t[MouseDown and move]\r\n" +
                "ReSizeWindow\t[T,F,G,H],[MouseWheel]\r\n" +
                "WindowState\t[MouseDoubleClick]");
        }

        private void TMenuItem_Close_Click(object sender, EventArgs e)
        {
            //this.Close();
            this.ContextMenuStrip.Close();
        }

        private void Twist()
        {
            Thread threadShow = new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < 4; i++)
                {
                    m_bLoad = !m_bLoad;
                    this.Invalidate();
                    Thread.Sleep(250);
                }
            }));
            threadShow.IsBackground = true;
            threadShow.Start();
        }

        private void SetClipBoard(bool bOriginal)
        {
            if (bOriginal)
            {
                Clipboard.SetImage(m_bmp);
                return;
            }
            using (Bitmap bmp = new Bitmap(this.Width - 2, this.Height - 2, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(m_bmp, 0, 0, bmp.Width, bmp.Height);
                    Clipboard.SetImage(bmp);
                }
            }
        }

        private void SaveBmp(bool bOriginal)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Bitmap(*.bmp)|*.bmp|JPEG(*.jpg)|*.jpg";
            saveDlg.FilterIndex = 2;
            saveDlg.FileName = "CAPTURE_" + GetTimeString();
            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                using (Bitmap bmp = bOriginal ? m_bmp.Clone() as Bitmap :
                    new Bitmap(this.Width - 2, this.Height - 2, PixelFormat.Format24bppRgb))
                {
                    if (bOriginal)
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.DrawImage(m_bmp, 0, 0, this.Width - 2, this.Height - 2);
                        }
                    }
                    switch (saveDlg.FilterIndex)
                    {
                        case 1:
                            bmp.Save(saveDlg.FileName, ImageFormat.Bmp);
                            break;
                        case 2:
                            bmp.Save(saveDlg.FileName, ImageFormat.Jpeg);
                            break;
                    }
                }
            }
        }
        //保存时获取当前时间字符串作文默认文件名
        private string GetTimeString()
        {
            DateTime time = DateTime.Now;
            return time.Date.ToShortDateString().Replace("/", "") + "_" +
                time.ToLongTimeString().Replace(":", "");
        }
    }
}
