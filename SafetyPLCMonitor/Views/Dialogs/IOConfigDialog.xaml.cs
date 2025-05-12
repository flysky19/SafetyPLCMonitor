// SafetyPLCMonitor/Views/Dialogs/IOConfigDialog.xaml.cs
using System.Windows;
using SafetyPLCMonitor.Models;

namespace SafetyPLCMonitor.Views.Dialogs
{
    public partial class IOConfigDialog : Window
    {
        public string IOType { get; set; }
        public int Address { get; set; }
        public string DefaultName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        public IOConfigDialog()
        {
            InitializeComponent();
            DataContext = this;
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        
        // 정적 메서드로 다이얼로그 표시 및 결과 반환
        public static bool ShowDialog(Window owner, IOPoint ioPoint, out string name, out string description)
        {
            var dialog = new IOConfigDialog
            {
                Owner = owner,
                IOType = ioPoint.Type.ToString(),
                Address = ioPoint.Address,
                DefaultName = GetDefaultIOName(ioPoint.Type, ioPoint.Address),
                Name = ioPoint.Name,
                Description = ioPoint.Description
            };
            
            bool? result = dialog.ShowDialog();
            
            if (result == true)
            {
                name = dialog.Name;
                description = dialog.Description;
                return true;
            }
            
            name = null;
            description = null;
            return false;
        }
        
        private static string GetDefaultIOName(IOType type, int address)
        {
            switch (type)
            {
                case Models.IOType.DiscreteInput:
                    return $"Input {address}";
                case Models.IOType.DiscreteOutput:
                    return $"Output {address}";
                case Models.IOType.VirtualInput:
                    return $"VInput {address}";
                case Models.IOType.VirtualOutput:
                    return $"VOutput {address}";
                case Models.IOType.InputRegister:
                    return $"InReg {address}";
                case Models.IOType.HoldingRegister:
                    return $"HoldReg {address}";
                default:
                    return $"IO {address}";
            }
        }
    }
}