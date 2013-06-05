using System;
using System.Drawing;
using System.Windows.Forms;
using FrEee.Utility;
using FrEee.Utility.Extensions;

namespace FrEee.WinForms.Controls
{
	public partial class GameProgressBar : UserControl
	{
		public GameProgressBar()
		{
			InitializeComponent();
			this.SizeChanged += GameProgressBar_SizeChanged;
			Padding = new Padding(5, 5, 5, 5);
		}

		void GameProgressBar_SizeChanged(object sender, EventArgs e)
		{
			Invalidate();
		}

		private int value = 0;
		public int Value
		{
			get { return value; }
			set
			{
				this.value = value;
				Invalidate();
			}
		}

		private int maximum = 100;
		public int Maximum
		{
			get { return maximum; }
			set
			{
				maximum = value;
				Invalidate();
			}
		}

		private Color barColor = Color.Blue;
		public Color BarColor
		{
			get { return barColor; }
			set
			{
				barColor = value;
				Invalidate();
			}
		}

		private string leftText, rightText;

		public string LeftText { get { return leftText; } set { leftText = value; Invalidate(); } }

		public string RightText { get { return rightText; } set { rightText = value; Invalidate(); } }

		protected override void OnPaint(PaintEventArgs e)
		{
			var centerText = Value.ToUnitString() + "/" + Maximum.ToUnitString() + " (" + Math.Round(((double)Value / (double)Maximum * 100)) + "%)";
			base.OnPaint(e);
			e.Graphics.Clear(BackColor);
			if (Maximum != 0)
				e.Graphics.FillRectangle(new SolidBrush(BarColor), 0, 0, Value * Width / Maximum, Height);
			if (BorderStyle == BorderStyle.FixedSingle)
				ControlPaint.DrawBorder(e.Graphics, ClientRectangle, BorderColor, ButtonBorderStyle.Solid);
			Brush brush;
			if (BarColor.R + BarColor.G + BarColor.B > 128 * 3 && BackColor.R + BackColor.G + BackColor.B > 128 * 3)
				brush = new SolidBrush(Color.Black);
			else
				brush = new SolidBrush(Color.White);
			var rect = new Rectangle(0, 0, Width, Height);
			rect.X += Padding.Left;
			rect.Y += Padding.Top;
			rect.Width -= Padding.Left + Padding.Right;
			rect.Height -= Padding.Top + Padding.Bottom;
			e.Graphics.DrawString(LeftText, Font, brush, rect, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
			e.Graphics.DrawString(RightText, Font, brush, rect, new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
			if (Width > 100)
				e.Graphics.DrawString(centerText, Font, brush, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
		}

		private Color borderColor;
		/// <summary>
		/// Color of the border for BorderStyle.FixedSingle mode.
		/// </summary>
		public Color BorderColor
		{
			get { return borderColor; }
			set
			{
				borderColor = value;
				Invalidate();
			}
		}
	}
}
