using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Circuit_Layout
{
    public class TaskManager : INotifyPropertyChanged
    {
        #region Singleton
        private static TaskManager instance; //не можем создавать экземпляры класса
        public static TaskManager GetInstance()
        {
            if ( instance == null )
                instance = new TaskManager();
            return instance;
        }
        #endregion
        #region Determination
        private TaskManager()
        {
            DataTable = new ObservableCollection<ObservableCollection<TableCell>>();
            SolutionLines = new ObservableCollection<object>();
            TasksMenu = new ObservableCollection<MenuItem>();
            //Instructions = new ObservableCollection<string>();
            SolutionsNames = new Dictionary<string, TextBox>();
            Diagrams = new ObservableCollection<Diagram>();
            ButtonDiagramsVisibility = Visibility.Collapsed;
            ButtonCheckerVisibility = Visibility.Collapsed;
        }
        #endregion
        #region Methods
        #region UpdateSelectedTask
        private void UpdateSelectedTask( string number )
        {
            SelectedTask = Exercises
                .Descendants( "task" )
                    .FirstOrDefault( i => i
                        .Attribute( "number" )
                            .Value == number );

            DrawInstructions();
            DrawTaskTable();
            DrawDiagrams();
            DrawSolutionLines();
        }
        #endregion
        #region UpdateDiagrams
        public void UpdateDiagrams()
        {
            foreach ( Diagram diagram in Diagrams )
            {
                diagram.Points.Clear();
                for ( int i = 0; i < DataTable.Count; i++ )
                {
                    double x, y;
                    if ( double.TryParse( DataTable[i][diagram.ColumnA].Text, out x ) && double.TryParse( DataTable[i][diagram.ColumnB].Text, out y ) )
                        diagram.Add( x, y );
                }
            }
        }
        #endregion
        #region Checker
        public void CheckAnswer( object sender, RoutedEventArgs e )     // Проверка на правильность решения лабы
        {
            double val, mean, fault;

            bool v = GetCheckerValue( checker.Element( "value" ).Value, out val );
            bool m = GetCheckerValue( checker.Element( "mean" ).Value, out mean );
            bool f = GetCheckerValue( checker.Element( "fault" ).Value, out fault );

            if ( !( v && m && f ) )
            {
                MessageBox.Show( "Invalid input!" );
                return;
            }

            if ( val <= mean + fault && val >= mean - fault )
                MessageBox.Show( "Correct! Real value is " + val.ToString(), "Congratilations!" );
            else
                MessageBox.Show( "WRONG!", "Mistake!" );
        }
        private bool GetCheckerValue( string s, out double result )   // s - Rx, число или название поля; true - успех парсинга, false - некорректные входные данные
        {
            if ( s == "Rx" )
            {
                Layout layout = Layout.GetInstance();
                result = layout.GetResistorXResistance();
                return true;
            }

            double tmp;
            if ( SolutionsNames.ContainsKey( s ) )
            {
                if ( double.TryParse( SolutionsNames[s].Text, out tmp ) )
                {
                    result = tmp;
                    return true;
                }
                else
                {
                    result = double.NaN;
                    return false;
                }
            }

            if ( double.TryParse( s, out tmp ) )
            {
                result = tmp;
                return true;
            }

            throw new XMLValidationException( "<checker> field value error!" );
        }
        #endregion
        #region Print
        public void PrepareForPrinting()
        {
            ButtonDiagramsVisibility = Visibility.Collapsed;
            ButtonCheckerVisibility = Visibility.Collapsed;
        }

        public void AfterPrinting()
        {
            ButtonDiagramsVisibility = Diagrams.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            ButtonCheckerVisibility = checker == null ? Visibility.Collapsed : Visibility.Visible;
        }
        #endregion
        #region XML
        #region Tasks
        public void LoadTasks()   // Загрузка XML файла tasks.xml, обновление элемента Exercises и меню Exercises
        {
            Exercises = XDocument.Load( "tasks.xml", LoadOptions.SetBaseUri | LoadOptions.SetLineInfo );  // Обновление документа Exercises
            XmlSchemaSet schema = new XmlSchemaSet();
            schema.Add( "", "TasksSchema.xsd" );

            Exercises.Validate( schema, ( sender, e ) =>
            {
                throw new XMLValidationException( e.Message );
            } );

            var exercises = Exercises    // Последовательность заданий для пунктов меню
                .Descendants( "task" ) // элементы с каждым заданием
                    .Select( i => new
                    {
                        Header = i.Element( "name" ).Value.Trim( '\n', '\t' ),
                        Tag = i.Attribute( "number" ).Value
                    } );

            foreach ( var exercise in exercises ) // Добавление упражнений в меню
            {
                MenuItem mi = new MenuItem() { Header = exercise.Header, IsCheckable = true };
                mi.Click += ( sender, e ) =>
                    {
                        foreach ( MenuItem item in TasksMenu ) //убираем галки у всех, кроме выбранного
                            item.IsChecked = mi == item;

                        UpdateSelectedTask( exercise.Tag );
                    };
                TasksMenu.Add( mi );
            }
        }
        private void DrawInstructions()  // Отрисовка инструкций к текущему заданию
        {
            //Instructions.Clear();
            string instructions = SelectedTask
                .Element( "description" )
                .Value.Trim( '\n', '\t' );
            Instructions = ParseToStackPanel( instructions );
            //Instructions = .Add( instructions );
        }
        private void DrawTaskTable()  // Отрисовка таблицы из текущего задания
        {
            var tableHeaders = SelectedTask
                .Element( "table" )
                    .Descendants( "column" )
                        .Select( i => new
                        {
                            value = i.Value,
                            ai = i.Attribute( "ai" ) == null ? false : i.Attribute( "ai" ).Value == "true"      // Автоинкремент
                        } )
                        .ToArray();

            int length;
            int.TryParse( SelectedTask.Element( "table" ).Attribute( "tests" ).Value, out length );

            DataTable.Clear();

            for ( int i = 0; i < length + 1; i++ )
            {
                ObservableCollection<TableCell> column = new ObservableCollection<TableCell>();

                for ( int j = 0; j < tableHeaders.Count(); j++ )
                {
                    var tableHeader = tableHeaders[j];
                    if ( i == 0 )
                        column.Add( new TableCell( tableHeader.value, ParseToStackPanel( tableHeader.value ) ) );
                    else
                        column.Add( new TableCell( tableHeader.ai ? i.ToString() : "" ) );
                }

                DataTable.Add( column );
            }
        }
        private void DrawDiagrams()  // Построение и отрисовка диаграм
        {
            Diagrams.Clear();

            var Xdiagrams = SelectedTask
                .Element( "diagrams" );

            if ( Xdiagrams == null )
            {
                ButtonDiagramsVisibility = Visibility.Collapsed;
                return;
            }

            var diagrams = Xdiagrams.
                Descendants( "diagram" )
                    .Select( i => new
                    {
                        x = int.Parse( i.Element( "x" ).Attribute( "column" ).Value ),
                        y = int.Parse( i.Element( "y" ).Attribute( "column" ).Value ),
                    } )
                    .ToArray();

            foreach ( var diagram in diagrams )
            {
                if ( diagram.x > DataTable.First().Count || diagram.y > DataTable.First().Count || diagram.x < 0 || diagram.y < 0 )
                    throw new XMLValidationException( "Diagram value is bigger than length if the table or less than 0" );

                StackPanel spA = ParseToStackPanel( DataTable[0][diagram.x].Text );
                StackPanel spB = ParseToStackPanel( DataTable[0][diagram.y].Text );

                Diagram d = new Diagram( diagram.x, diagram.y, spA, spB );
                Diagrams.Add( d );
            }

            ButtonDiagramsVisibility = Diagrams.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateDiagrams();
        }
        private void DrawSolutionLines()                            // Отрисовка строк решения из текущего задания
        {
            var externalSolutionLines = SelectedTask
                .Element( "solution" )
                    .Descendants( "line" );

            SolutionLines.Clear();
            SolutionsNames.Clear();

            foreach ( var line in externalSolutionLines )
            {
                StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness( 5 ) };
                foreach ( var item in line.Descendants() )
                {
                    if ( item.Name == "text" )
                        sp.Children.Add( ParseToStackPanel( item.Value ) );
                    else if ( item.Name == "field" )
                    {
                        TextBox tb = new TextBox() { Width = 60, VerticalAlignment = VerticalAlignment.Center };
                        sp.Children.Add( tb );
                        if ( item.Attribute( "name" ) != null )
                            SolutionsNames.Add( item.Attribute( "name" ).Value, tb );
                    }
                }
                SolutionLines.Add( sp );
            }
            checker = SelectedTask.Element( "solution" ).Element( "checker" );
            ButtonCheckerVisibility = checker == null ? Visibility.Collapsed : Visibility.Visible;
        }
        private StackPanel ParseToStackPanel( string ParseString )  // Перевод строки в StackPanel, содержащую текстблоки (степени и индексы)
        {
            StackPanel stp = new StackPanel() { Orientation = Orientation.Horizontal };
            string tmp = "";
            foreach ( char ch in ParseString )
            {
                if ( ch == '_' || ch == '^' || ch == '|' )
                {
                    stp.Children.Add( new TextBlock()
                    {
                        Text = tmp,
                        Margin = new Thickness( ch == '|' ? 0 : 1, 0, 0, 0 ),
                        TextWrapping = System.Windows.TextWrapping.WrapWithOverflow,
                        MaxWidth = 720,
                        VerticalAlignment = ch == '_' ? System.Windows.VerticalAlignment.Bottom : System.Windows.VerticalAlignment.Top,
                        FontSize = ch == '|' ? 12 : 8
                    } );
                    tmp = "";
                }
                else
                {
                    tmp += ch;
                }
            }
            return stp;
        }
        #endregion
        #region Save/Load
        public bool Save( string FileName )
        {
            if ( !AnyTaskSelected || FileName == null )
                return false;

            XDocument doc = new XDocument( new XElement( "task", SelectedTask.Attribute( "number" ),
                new XElement( "table", DataTable
                    .Skip( 1 )
                    .Select( i =>
                        new XElement( "row", i
                            .Select( j =>
                                new XElement( "cell", new XAttribute( "value", j.Text ) )
                            ) ) ) ) ) );

            doc.Save( FileName );
            return true;
        }
        public bool Load( string FileName )
        {
            if ( !AnyTaskSelected )
                return false;

            if ( !System.IO.File.Exists( FileName ) )
                throw new XMLFileOpenSaveException( "File doesn't exist!" );

            XDocument doc = XDocument.Load( FileName );

            #region Validation
            var task = doc.Element( "task" );
            if ( task == null )
                throw new XMLValidationException( "XML file has no element <task>" );

            if ( task.Attribute( "number" ).Value != SelectedTask.Attribute( "number" ).Value )
                return false;

            var table = task.Element( "table" );
            if ( table == null )
                throw new XMLValidationException( "XML file has no element <table> in <task> section" );

            var rows = table.Descendants( "row" ).ToArray();
            if ( rows.Length != DataTable.Count - 1 )
                throw new XMLValidationException( "Element <table> doesn't match selected task table" );

            foreach ( var row in rows )
            {
                var cells = row.Descendants( "cell" );
                if ( cells.Count() != DataTable.First().Count )
                    throw new XMLValidationException( "Element <table> doesn't match selected task table" );

                foreach ( var cell in cells )
                {
                    if ( cell.Attribute( "value" ) == null )
                        throw new XMLValidationException( "Element <cell> has no attribute 'value'" );
                }
            }
            #endregion

            for ( int i = 0; i < rows.Length; i++ )
            {
                var cells = rows[i].Descendants( "cell" ).ToArray();
                for ( int j = 0; j < cells.Length; j++ )
                {
                    DataTable[i + 1][j].Text = cells[j].Attribute( "value" ).Value;
                }
            }
            return true;
        }
        #endregion
        #endregion
        #endregion
        #region Properties
        private XDocument Exercises;
        private XElement SelectedTask;
        private XElement checker;
        private StackPanel instructs;
        private Dictionary<string, TextBox> SolutionsNames;
        private Visibility buttonDiagramsVisibility, buttonCheckerVisibility;
        public ObservableCollection<MenuItem> TasksMenu { get; private set; }
        public ObservableCollection<ObservableCollection<TableCell>> DataTable { get; private set; }
        public ObservableCollection<object> SolutionLines { get; private set; }
        public StackPanel Instructions
        {
            get { return instructs; }
            private set
            {
                if ( instructs != value )
                {
                    instructs = value;
                    Fire( "Instructions" );
                }
            }
        }
        public ObservableCollection<Diagram> Diagrams { get; private set; }
        public Visibility ButtonDiagramsVisibility
        {
            get { return buttonDiagramsVisibility; }
            set
            {
                if ( buttonDiagramsVisibility != value )
                {
                    buttonDiagramsVisibility = value;
                    Fire( "ButtonDiagramsVisibility" );
                }
            }
        }
        public Visibility ButtonCheckerVisibility
        {
            get { return buttonCheckerVisibility; }
            set
            {
                if ( buttonCheckerVisibility != value )
                {
                    buttonCheckerVisibility = value;
                    Fire( "ButtonCheckerVisibility" );
                }
            }
        }
        public bool AnyTaskSelected { get { return SelectedTask != null; } }
        #endregion
        #region Fire
        private void Fire( params string[] names )
        {
            if ( PropertyChanged != null )
                foreach ( var name in names )
                    PropertyChanged( this, new PropertyChangedEventArgs( name ) );
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        #region Exceptions
        [Serializable]
        public class XMLValidationException : Exception
        {
            public XMLValidationException() { }
            public XMLValidationException( string message ) : base( message ) { }
            public XMLValidationException( string message, Exception inner ) : base( message, inner ) { }
            protected XMLValidationException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context )
                : base( info, context ) { }
        }
        [Serializable]
        public class XMLFileOpenSaveException : Exception
        {
            public XMLFileOpenSaveException() { }
            public XMLFileOpenSaveException( string message ) : base( message ) { }
            public XMLFileOpenSaveException( string message, Exception inner ) : base( message, inner ) { }
            protected XMLFileOpenSaveException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context )
                : base( info, context ) { }
        }
        #endregion
    }
}
