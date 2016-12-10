using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using System.Collections.ObjectModel;

namespace Circuit_Layout
{
    /// <summary>
    /// Логика взаимодействия для CircuitLayoutControl.xaml
    /// </summary>
    public partial class CircuitLayoutControl : UserControl
    {
        #region Determination
        public CircuitLayoutControl()
        {
            InitializeComponent();
            Layout layout = Layout.GetInstance();
            DataContext = layout;
        }
        #endregion
        #region ContextMenu
        #region isContextMenuShown
        private bool isContextMenuShown = false;
        private void ContextMenu_ContextMenuOpening( object sender, ContextMenuEventArgs e )        // Запрет на открытие при запщеной симуляции
        {
            if ( (bool)cbRun.IsChecked )
            {
                e.Handled = true;
                return;
            }
            isContextMenuShown = true;
        }
        private void ContextMenu_ContextMenuClosing( object sender, ContextMenuEventArgs e )
        {
            isContextMenuShown = false;
        }
        #endregion
        #region ContextMenuItems
        private void ContextMenu_Edit( object sender, RoutedEventArgs e )
        {
            isContextMenuShown = false;
            MenuItem menuItem = sender as MenuItem;
            ContextMenu cmenu = menuItem.Parent as ContextMenu;
            Grid grid = cmenu.PlacementTarget as Grid;
            Element element = grid.DataContext as Element;

            Window window = new EditWindow()
            {
                Tag = element,
                Owner = this.Owner,
            };

            window.ShowDialog();
        }

        private void ContextMenu_Remove( object sender, RoutedEventArgs e )             // Удаление элемента или соединителя
        {
            isContextMenuShown = false;
            MenuItem menuItem = sender as MenuItem;
            ContextMenu cmenu = menuItem.Parent as ContextMenu;
            Grid grid = cmenu.PlacementTarget as Grid;

            Layout layout = Layout.GetInstance();

            if ( grid.DataContext is Element )
            {
                Element element = grid.DataContext as Element;
                layout.RemoveElement( element );
            }
            else if ( grid.DataContext is Connector )
            {
                Connector connector = grid.DataContext as Connector;
                layout.RemoveConnector( connector );
            }
        }
        #endregion
        #endregion
        #region Element
        #region CreateElement
        private void Layout_MouseDown( object sender, MouseButtonEventArgs e )
        {
            if ( (bool)cbRun.IsChecked )
                return;

            if ( e.LeftButton == MouseButtonState.Pressed && !isContextMenuShown )             // создание элемента
            {
                Layout layout = Layout.GetInstance();
                Point pos = e.GetPosition( icLayout );

                Type[] types = new Type[] {
                typeof(Resistor),
                typeof(Battery),
                typeof(Amperemeter),
                typeof(Reohord),
                typeof(ResistorX),
                typeof(Potentiometer),
                };
                Element element;
                try                                                                             //проверка на существование Rx
                {
                    element = layout.CreateElement( types[lbElements.SelectedIndex], (int)pos.X, (int)pos.Y );
                    movingElement = element;
                }
                catch ( Layout.ResistorXPlacedException )
                {
                    MessageBox.Show( Properties.Resources.cc_ex_xresistorisplaced, Properties.Resources.ex_error, MessageBoxButton.OK );
                }

            }
            if ( e.RightButton == MouseButtonState.Pressed )
            {
                if ( movingElement != null )                              // удаление перетаскиваемого элемента
                {
                    Layout layout = Layout.GetInstance();
                    layout.RemoveElement( movingElement );

                    movingElement = null;
                    if ( selectedNode != null )
                    {
                        selectedNode.IsSelected = false;
                        selectedNode = null;
                    }
                }
            }
        }
        #endregion
        #region SelectElement
        private Element movingElement;
        private void Element_MouseDown( object sender, MouseButtonEventArgs e )
        {
            if ( (bool)cbRun.IsChecked )
                return;

            Grid grid = sender as Grid;
            Element element = grid.DataContext as Element;

            if ( e.LeftButton == MouseButtonState.Pressed )
            {
                movingElement = movingElement == null ? element : null;
                e.Handled = true;
            }
        }
        #endregion
        #region ElementMove
        private void Layout_MouseMove( object sender, MouseEventArgs e )
        {
            if ( movingElement == null )
                return;

            Point position = e.GetPosition( icLayout );

            double width = Math.Abs( movingElement.OffsetB * Math.Cos( movingElement.Angle * Math.PI / 180 ) );
            double height = Math.Abs( movingElement.OffsetB * Math.Sin( movingElement.Angle * Math.PI / 180 ) );

            if ( position.X > width + 5 && position.X < icLayout.ActualWidth - width - 5 )
                movingElement.X = (int)position.X;
            if ( position.Y > height + 25 && position.Y < icLayout.ActualHeight - height - 10 )
                movingElement.Y = (int)position.Y;
        }
        #endregion
        #region Changes
        #region Reohord
        private void ReohordSlider_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            if ( cbRun.IsChecked == false )
                return;
            Slider slider = sender as Slider;
            Reohord reohord = slider.DataContext as Reohord;
            reohord.LengthAC = slider.Value;
            if ( !RunLayout() )
                StopSim();
        }
        #endregion
        #region Resistor
        private void ResistanceStopEditing( object sender )
        {
            TextBox tb = sender as TextBox;
            Resistor res = tb.DataContext as Resistor;
            double resistance;
            double.TryParse( tb.Text, out resistance );
            res.Resistance = resistance;

            if ( cbRun.IsChecked == true )
            {
                if ( !RunLayout() )
                    StopSim();
            }
            Keyboard.ClearFocus();
        }

        private void ResistanceTextBox_LostFocus( object sender, RoutedEventArgs e )
        {
            ResistanceStopEditing( sender );
        }

        private void ResistanceTextBox_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Enter )
            {
                ResistanceStopEditing( sender );
            }
            if ( e.Key == Key.Escape )
            {
                TextBox tb = sender as TextBox;
                Resistor res = tb.DataContext as Resistor;
                tb.Text = res.Resistance.ToString();
                Keyboard.ClearFocus();
            }

        }
        #endregion
        #region Potentiometer
        private void PotentiometerSlider_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            if ( cbRun.IsChecked == false )
                return;
            Slider slider = sender as Slider;
            Potentiometer pot = slider.DataContext as Potentiometer;
            pot.Length = slider.Value;
            if ( !RunLayout() )
                StopSim();
        }
        #endregion
        #endregion
        #endregion
        #region Node
        Node selectedNode;
        private void Node_MouseDown( object sender, MouseButtonEventArgs e )
        {
            if ( (bool)cbRun.IsChecked )
                return;

            Canvas canv = sender as Canvas;
            Node node = canv.DataContext as Node;
            Layout layout = Layout.GetInstance();

            if ( e.LeftButton == MouseButtonState.Pressed )
            {
                if ( selectedNode == null )
                {
                    selectedNode = node;
                    node.IsSelected = true;
                }
                else
                {
                    if ( selectedNode != node )
                    {
                        layout.ConnectNodes( selectedNode, node, new Connector() );
                    }
                    selectedNode.IsSelected = false;
                    selectedNode = null;
                }
            }
            if ( e.RightButton == MouseButtonState.Pressed )
            {
                //todo Context Menu!
                layout.RemoveNode( node );
            }
            e.Handled = true;
        }
        #endregion
        #region Keyboard
        public void Layout_KeyDown( object sender, KeyEventArgs e )
        {
            //MessageBox.Show( e.Key.ToString() );
            switch ( e.Key )
            {
                case Key.OemPlus:
                case Key.Add:
                    if ( movingElement != null )
                        movingElement.Angle += 45;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    if ( movingElement != null )
                        movingElement.Angle -= 45;
                    break;
            }
        }

        #endregion
        #region Run
        private void cbRun_Checked( object sender, RoutedEventArgs e )
        {
            if ( cbRun.IsChecked == true )
            {
                icLayout.Background = new SolidColorBrush( Color.FromRgb( 0x9a, 0xcf, 0x5c ) );
                grElements.Visibility = System.Windows.Visibility.Collapsed;
                brCountData.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                icLayout.Background = new SolidColorBrush( Color.FromRgb( 0xfa, 0xff, 0xc7 ) );
                grElements.Visibility = System.Windows.Visibility.Visible;
                brCountData.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
        private void ButtonRUN_Click( object sender, RoutedEventArgs e )
        {
            if ( !(bool)cbRun.IsChecked )
            {
                cbRun.IsChecked = true;
                btnRun.Content = Properties.Resources.cc_cb_stop;
                movingElement = null;
                if ( selectedNode != null )
                {
                    selectedNode.IsSelected = false;
                    selectedNode = null;
                }
                if ( RunLayout() )
                    return;

            }
            StopSim();
        }
        private void StopSim()
        {
            cbRun.IsChecked = false;
            btnRun.Content = Properties.Resources.cc_cb_run;
        }
        private bool RunLayout()
        {
            Layout layout = Layout.GetInstance();
            try
            {
                layout.Update();
            }
            catch ( Layout.WrongSchemeException ex )
            {
                MessageBox.Show( Properties.Resources.cc_ex_layoutsimexception + "\n\n" + ex.Message, Properties.Resources.ex_error );
                return false;
            }
            catch ( KramerCounter.KramerCountException ex )
            {
                MessageBox.Show( Properties.Resources.cc_ex_layoutsimexception + ": " + Properties.Resources.cc_ex_kramercounterexception + "\n\n" + ex.Message, Properties.Resources.ex_error );
                return false;
            }
            return true;
        }
        #endregion
        #region Properties
        public Window Owner { get; set; }
        #endregion
    }
}
