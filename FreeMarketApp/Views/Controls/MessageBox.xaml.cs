using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FreeMarketApp.Views.Controls
{
    public class MessageBox : Window
    {
        public enum MessageBoxButtons
        {
            Ok,
            OkCancel,
            YesNo,
            YesNoCancel
        }

        public enum MessageBoxResult
        {
            Ok,
            Cancel,
            Yes,
            No
        }

        public MessageBox()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Task<MessageBoxResult> Show(Window parent, string text, string title, MessageBoxButtons buttons)
        {
            var msgbox = new MessageBox()
            {
                Title = title
            };
            msgbox.FindControl<TextBlock>("Title").Text = title;
            msgbox.FindControl<TextBlock>("Text").Text = text;
            var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

            var res = MessageBoxResult.Ok;

            void AddButton(string caption, MessageBoxResult r, bool def = false)
            {
                var btn = new Button { Content = caption, Padding = new Thickness(15, 5), Cursor = new Cursor(StandardCursorType.Hand) };
                btn.Click += (_, __) => {
                    res = r;
                    msgbox.Close();
                };
                buttonPanel.Children.Add(btn);
                if (def) res = r;
            }

            if (buttons == MessageBoxButtons.Ok || buttons == MessageBoxButtons.OkCancel)
                AddButton("Ok", MessageBoxResult.Ok, true);

            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
            {
                AddButton("Yes", MessageBoxResult.Yes);
                AddButton("No", MessageBoxResult.No, true);
            }

            if (buttons == MessageBoxButtons.OkCancel || buttons == MessageBoxButtons.YesNoCancel)
                AddButton("Cancel", MessageBoxResult.Cancel, true);

            var tcs = new TaskCompletionSource<MessageBoxResult>();
            msgbox.Closed += delegate { tcs.TrySetResult(res); };

            if (parent != null)
            {
                msgbox.ShowDialog(parent);
            } 
            else
            {
                msgbox.Show();
            }
                
            return tcs.Task;
        }
    }
}