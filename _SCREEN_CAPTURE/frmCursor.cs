using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _SCREEN_CAPTURE
{
    public partial class frmCursor : Form
    {
        private Bitmap customCursorImage = new Bitmap("cursor.png"); // 你的 PNG 图像路径
        public frmCursor()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.None; // 使窗体边框消失
            this.BackColor = Color.Magenta; // 设置窗体背景颜色为透明色，这里选择 Magenta 便于区分
            this.TransparencyKey = Color.Magenta; // 将 Magenta 设置为窗体的透明颜色
            this.Cursor = Cursors.Cross; // 设置标准光标形状为十字架形状（可选）

            this.StartPosition = FormStartPosition.CenterScreen; // 设置窗体初始位置为屏幕中央
            this.Size = new Size(customCursorImage.Width, customCursorImage.Height); // 设置窗体大小与 PNG 图像大小相同
            this.TopMost = true; // 窗体始终在最顶层显示
            this.ShowInTaskbar = false; // 不在任务栏显示窗体

            // 隐藏光标
            Cursor.Hide();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // 根据鼠标位置更新窗体位置
            this.Location = new Point(MousePosition.X - (customCursorImage.Width / 2), MousePosition.Y - (customCursorImage.Height / 2));
            base.OnMouseMove(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 绘制 PNG 图像
            e.Graphics.DrawImage(customCursorImage, 0, 0, customCursorImage.Width, customCursorImage.Height);
            base.OnPaint(e);
        }
    }
}
