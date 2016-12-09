using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Circuit_Layout
{
    /// <summary>
    /// Логика взаимодействия для TaskControl.xaml
    /// </summary>
    public partial class TaskControl : UserControl
    {
        #region Determination
        public TaskControl()
        {
            InitializeComponent();
            TaskManager tm = TaskManager.GetInstance();
            this.DataContext = tm;
        }
        #endregion
        #region Buttons
        private void ButtonUpdateDiagram_Click( object sender, RoutedEventArgs e )
        {
            Button btn = sender as Button;
            Diagram d = btn.DataContext as Diagram;
            TaskManager tm = TaskManager.GetInstance();

            tm.UpdateDiagrams();
        }
        private void ButtonCheck_Click( object sender, RoutedEventArgs e )
        {
            TaskManager tm = TaskManager.GetInstance();
            tm.CheckAnswer( sender, e );
        }
        #endregion
        #region Print
        public void Print()
        {
            TaskManager tm = TaskManager.GetInstance();
            PrintDialog pd = new PrintDialog();
            if ( pd.ShowDialog() == true )
            {
                tm.PrepareForPrinting();
                spFormalisation.Background = Brushes.White;

                pd.PrintVisual( spFormalisation, Properties.Resources.tc_c_formalization_print );
                spFormalisation.Background = null;
                tm.AfterPrinting();
            }
        }
        #endregion
    }
}
