namespace WinFormsApp1
{
    public static class FormExtensions
    {
        public static ControlLayout SerializeControl(this Control ctrl)
        {
            var layout = new ControlLayout
            {
                Type = ctrl.GetType().Name,
                Name = ctrl.Name,
                Text = ctrl.Text,
                X = ctrl.Left,
                Y = ctrl.Top,
                Width = ctrl.Width,
                Height = ctrl.Height,

                FontFamily = ctrl.Font?.FontFamily?.Name,
                FontSize = ctrl.Font?.Size ?? 0,
                FontBold = ctrl.Font != null && ctrl.Font.Bold,
                FontItalic = ctrl.Font != null && ctrl.Font.Italic,

                Anchor = ctrl.Anchor.ToString(),
                Dock = ctrl.Dock.ToString(),
                TabIndex = ctrl.TabIndex
            };

            foreach (Control child in ctrl.Controls)
            {
                layout.Children.Add(SerializeControl(child));
            }

            return layout;
        }
    }

    public class ControlLayout
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string FontFamily { get; set; }
        public float FontSize { get; set; }
        public bool FontBold { get; set; }
        public bool FontItalic { get; set; }

        public string Anchor { get; set; }
        public string Dock { get; set; }
        public int TabIndex { get; set; }

        public List<ControlLayout> Children { get; set; } = new List<ControlLayout>();
    }

}

 
