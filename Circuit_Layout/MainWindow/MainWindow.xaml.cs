using System.Windows.Media;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.ComponentModel;
using Microsoft.Win32;
using System.Threading;

namespace Circuit_Layout
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Determination
        public MainWindow()
        {
            InitializeComponent();
            TaskManager tm = TaskManager.GetInstance();

            try
            {
                tm.LoadTasks();
            }
            catch ( TaskManager.XMLValidationException ex )
            {
                MessageBox.Show( ex.Message, Properties.Resources.ex_error, MessageBoxButton.OK, MessageBoxImage.Error );
                this.Close();
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show( ex.Message, Properties.Resources.ex_error, MessageBoxButton.OK, MessageBoxImage.Error );
            }
            clcLayout.Owner = this;
            miMenuExercises.ItemsSource = tm.TasksMenu;
        }
        #endregion
        #region Layout tab
        private void Window_KeyDown( object sender, KeyEventArgs e )
        {
            clcLayout.Layout_KeyDown( sender, e );
            if ( e.Key == Key.S && ( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) ) )
            {
                Save();
            }
        }
        #endregion
        #region MainMenu
        private void MenuItemRxResistance_Click( object sender, RoutedEventArgs e )
        {
            Layout layout = Layout.GetInstance();
            MessageBox.Show( layout.GetResistorXResistance().ToString() );
        }
        private void MenuItem_ClearLayout_Click( object sender, RoutedEventArgs e )
        {
            Layout layout = Layout.GetInstance();
            layout.Clear();
        }
        private void MenuItem_ShowAbout_Click( object sender, RoutedEventArgs e )
        {
            AboutWindow aw = new AboutWindow() { WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner, Owner = this };
            aw.ShowDialog();
        }
        private void MenuItem_ShowHelp_Click( object sender, RoutedEventArgs e )
        {

        }
        private void MenuItemSave_Click( object sender, RoutedEventArgs e )
        {
            Save();
        }
        private void MenuItemSaveAs_Click( object sender, RoutedEventArgs e )
        {
            SaveAs();
        }
        private void MenuItemLoad_Click( object sender, RoutedEventArgs e )
        {
            Load();
        }
        private void MenuItemPrint_Click( object sender, RoutedEventArgs e )
        {
            clcFormalisation.Print();
        }
        #endregion
        #region Save/Load
        private void SaveAs()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CircuitLayout save files (*.xml)|*.xml|Все файлы (*.*)|*.*";

            TaskManager tm = TaskManager.GetInstance();
            if ( !tm.AnyTaskSelected )
            {
                MessageBox.Show( "No task selected", "Error" );
                return;
            }

            if ( sfd.ShowDialog() == true )
            {
                SaveFileName = sfd.FileName;
                Save();
            }
        }
        private void Save()
        {
            TaskManager tm = TaskManager.GetInstance();

            if ( SaveFileName == null )
                SaveAs();

            if ( !tm.Save( SaveFileName ) )
            {
                SaveFileName = null;
                MessageBox.Show( Properties.Resources.mw_ex_notaskselected, Properties.Resources.ex_error, MessageBoxButton.OK, MessageBoxImage.Exclamation );
            }
        }
        private void Load()
        {
            TaskManager tm = TaskManager.GetInstance();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = Properties.Resources.mw_c_savefiles + " (*.xml)|*.xml|Все файлы (*.*)|*.*";

            if ( ofd.ShowDialog() == true )
            {
                try
                {
                    if ( !tm.Load( ofd.FileName ) )
                    {
                        MessageBox.Show( Properties.Resources.mw_ex_tasknumberdoesntmatch, Properties.Resources.mw_ex_errorloadingfile, MessageBoxButton.OK, MessageBoxImage.Error );
                    }
                }
                catch ( TaskManager.XMLFileOpenSaveException ex )
                {
                    MessageBox.Show( ex.Message, Properties.Resources.mw_ex_errorloadingfile, MessageBoxButton.OK, MessageBoxImage.Error );
                }
                catch ( TaskManager.XMLValidationException ex )
                {
                    MessageBox.Show( Properties.Resources.mw_ex_wronginputfile + "\n" + ex.Message, Properties.Resources.mw_ex_errorloadingfile, MessageBoxButton.OK, MessageBoxImage.Error );
                }
            }
        }
        #endregion
        #region Properties
        private string SaveFileName;
        #endregion
    }
}
