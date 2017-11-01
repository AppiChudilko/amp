using GTA.Native;
using System.Drawing;

namespace GTA.UI
{
    public class Rectangle : IElement
    {
        public virtual bool Enabled { get; set; }

        public virtual Color Color { get; set; }

        public virtual PointF Position { get; set; }

        public virtual bool Centered { get; set; }

        public SizeF Size { get; set; }

        public Rectangle()
          : this(PointF.Empty, new SizeF(1280f, 720f), Color.Transparent, false)
        {
        }

        public Rectangle(PointF position, SizeF size)
          : this(position, size, Color.Transparent, false)
        {
        }

        public Rectangle(PointF position, SizeF size, Color color)
          : this(position, size, color, false)
        {
        }

        public Rectangle(PointF position, SizeF size, Color color, bool centered)
        {
            this.Enabled = true;
            this.Position = position;
            this.Size = size;
            this.Color = color;
            this.Centered = centered;
        }

        public virtual void Draw()
        {
            this.Draw(SizeF.Empty);
        }

        public virtual void Draw(SizeF offset)
        {
            this.InternalDraw(offset, 1280f, 720f);
        }

        public virtual void ScaledDraw()
        {
            this.ScaledDraw(SizeF.Empty);
        }

        public virtual void ScaledDraw(SizeF offset)
        {
            this.InternalDraw(offset, Screen.ScaledWidth, 720f);
        }

        private void InternalDraw(SizeF offset, float screenWidth, float screenHeight)
        {
            if (!this.Enabled)
                return;
            float num1 = this.Size.Width / screenWidth;
            float num2 = this.Size.Height / screenHeight;
            float num3 = (this.Position.X + offset.Width) / screenWidth;
            float num4 = (this.Position.Y + offset.Height) / screenHeight;
            if (!this.Centered)
            {
                num3 += num1 * 0.5f;
                num4 += num2 * 0.5f;
            }
            long num5 = 4206795403398567152;
            InputArgument[] inputArgumentArray = new InputArgument[8]
            {
        (InputArgument) num3,
        (InputArgument) num4,
        (InputArgument) num1,
        (InputArgument) num2,
        null,
        null,
        null,
        null
            };
            int index1 = 4;
            Color color = this.Color;
            InputArgument r = (InputArgument)color.R;
            inputArgumentArray[index1] = r;
            int index2 = 5;
            color = this.Color;
            InputArgument g = (InputArgument)color.G;
            inputArgumentArray[index2] = g;
            int index3 = 6;
            color = this.Color;
            InputArgument b = (InputArgument)color.B;
            inputArgumentArray[index3] = b;
            int index4 = 7;
            color = this.Color;
            InputArgument a = (InputArgument)color.A;
            inputArgumentArray[index4] = a;
            Function.Call((Hash)num5, inputArgumentArray);
        }
    }
}