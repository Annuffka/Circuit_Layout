using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Circuit_Layout
{
    class Layout : INotifyPropertyChanged
    {
        #region Singleton
        private static Layout instance;
        public static Layout GetInstance()
        {
            if ( instance == null )
                instance = new Layout();
            return instance;
        }
        #endregion
        #region Determination
        private Layout()
        {
            Nodes = new ObservableCollection<Node>();
            ConnectorsToDraw = new ObservableCollection<Connector>();
            Connectors = new List<Connector>();
            Contours = new ObservableCollection<ElementsContour>();
            Chains = new ObservableCollection<ElementsChain>();

        }
        #endregion
        #region Properties
        private double[,] CountMatrix;
        private double[] resultVector;
        public ObservableCollection<Node> Nodes { get; private set; }
        public ObservableCollection<Connector> ConnectorsToDraw { get; private set; }
        public List<Connector> Connectors { get; private set; }
        public ObservableCollection<ElementsContour> Contours { get; private set; }
        public ObservableCollection<ElementsChain> Chains { get; private set; }
        public List<string> Quations
        {
            get
            {
                List<string> tmp = new List<string>();
                Layout layout = Layout.GetInstance();
                if ( CountMatrix != null && resultVector != null )
                {
                    for ( int i = 0; i < CountMatrix.GetLength( 1 ); i++ )
                    {
                        string s = "";
                        for ( int j = 0; j < CountMatrix.GetLength( 0 ); j++ )
                        {
                            var value = CountMatrix[i, j];
                            if ( value != 0 )
                                s += string.Format( "{0}{1}*I{2}",
                                    value > 0 ? ( s == "" ? "" : " + " ) : " - ",
                                    i < Rule1Count || Chains[j].Connectors.FirstOrDefault( ch => ch is ResistorX ) == null ? Math.Abs( value ).ToString() :
                                    Math.Abs( Math.Abs( value ) - layout.GetResistorXResistance() ) < 0.00001 ? "Rx" : "(Rx + " + ( Math.Abs( value ) - layout.GetResistorXResistance() ).ToString() + ")",
                                    j
                                );

                            //s += ( CountMatrix[i, j] < 0 ?
                            //    " - " + ( -CountMatrix[i, j] ) :
                            //    ( s == "" ? "" : " + " ) + ( Chains[j].Connectors.FirstOrDefault( ch => ch is ResistorX ) == null ? 
                            //        CountMatrix[i, j].ToString() : (CountMatrix[i, j]-layout.GetResistorXResistance()) "Rx)" ) ) + "*I" + j;
                        }
                        s += " = " + resultVector[i].ToString();
                        tmp.Add( s );
                    }
                }
                return tmp;
            }
        }

        private int rule1Count;
        public int Rule1Count
        {
            get { return rule1Count; }
            private set
            {
                if ( rule1Count != value )
                {
                    rule1Count = value;
                    Fire( "Rule1Count", "Rule2Count" );
                }
            }
        }
        public int Rule2Count
        {
            get { return Chains.Count - Rule1Count; }
        }
        public Visibility DisplayMatrix { get { return rule1Count > 0 ? Visibility.Visible : Visibility.Collapsed; } }
        #endregion
        #region Methods
        #region Elements
        public Element CreateElement( Type type, int x, int y )
        {
            if ( type.BaseType.Name == "Element" )
            {
                if ( type.Name == "ResistorX" && Connectors.Count( i => i is ResistorX ) > 0 )
                    throw new ResistorXPlacedException();

                var element = Activator.CreateInstance( type ) as Element;              //создание элемента

                Node pina = new Node();                                                 //создание пинов
                Node pinb = new Node();

                if ( type.Name == "Reohord" )                                           //реохорд
                {
                    Node pinc = new Node();

                    Reohord reohord = element as Reohord;
                    reohord.ResistorAC = new NoDrawConnector() { Resistance = 100 };    //создание соединителей
                    reohord.ResistorBC = new NoDrawConnector();
                    ConnectNodes( pina, pinc, reohord.ResistorAC );                     //соединение пинов при помощи коннекторов
                    ConnectNodes( pinb, pinc, reohord.ResistorBC );

                    reohord.LengthAC = 50;

                    reohord.NodeA = pina;                                               //привязка ножек к нодам
                    reohord.NodeB = pinb;
                    reohord.NodeC = pinc;
                    Nodes.Add( reohord.NodeC );                                         //отрисовка нода C
                    ConnectorsToDraw.Add( element );                                    //отрисовка реохорда
                }
                else
                {
                    ConnectNodes( pina, pinb, element );                                //соединение пинов при помощи элемента
                }
                Nodes.Add( element.NodeA );                                             //отрисовка нодов
                Nodes.Add( element.NodeB );

                element.X = x;                                                          //перемещение по координатам (учитывая обновление свойств X и Y у пинов)
                element.Y = y;
                return element;
            }
            return null;
        }
        public void ConnectNodes( Node nodea, Node nodeb, Connector connector )
        {
            if ( ( nodea.Connections.Keys.Count( i => i == nodeb ) > 0 ) || ( nodeb.Connections.Keys.Count( i => i == nodea ) > 0 ) )
                return;

            nodea.Connections.Add( nodeb, connector );
            nodeb.Connections.Add( nodea, connector );
            connector.NodeA = nodea;
            connector.NodeB = nodeb;

            Connectors.Add( connector );
            if ( !( connector is NoDrawConnector ) )
                ConnectorsToDraw.Add( connector );
        }
        public void RemoveElement( Element element )
        {
            Node nodea = element.NodeA;
            Node nodeb = element.NodeB;
            if ( element is Reohord )
            {
                Reohord reohord = element as Reohord;
                Node nodec = reohord.NodeC;
                RemoveConnector( reohord.ResistorAC );
                RemoveConnector( reohord.ResistorBC );
                Connectors.Remove( reohord );
                ConnectorsToDraw.Remove( reohord );
                Nodes.Remove( nodec );
                while ( nodec.Connections.Count > 0 )
                {
                    RemoveConnector( nodec.Connections.Values.First() );
                }
            }
            else
            {
                RemoveConnector( element );
            }

            Nodes.Remove( nodea );                                                      //удаление пинов из отрисовки
            Nodes.Remove( nodeb );

            while ( nodea.Connections.Count > 0 )                                       //удаление соединителей, связанных с нодами
            {
                RemoveConnector( nodea.Connections.Values.First() );
            }
            while ( nodeb.Connections.Count > 0 )
            {
                RemoveConnector( nodeb.Connections.Values.First() );
            }
        }
        public void RemoveConnector( Connector connector )
        {
            Node nodea = connector.NodeA;
            Node nodeb = connector.NodeB;

            Connectors.Remove( connector );                                             //удаление из отрисовки
            ConnectorsToDraw.Remove( connector );
            nodea.Connections.Remove( nodeb );                                          //отвязка соединителя
            nodeb.Connections.Remove( nodea );
        }
        #endregion
        #region Nodes
        //public void CreateNode( Connector connector, Point position )
        //{
        //    Node node = new Node( (int)position.X, (int)position.Y );
        //    ConnectNodes( node, connector.NodeA, new Connector() );
        //    ConnectNodes( node, connector.NodeB, new Connector() );

        //    RemoveConnector( connector );

        //    Nodes.Add( node );
        //}
        public void RemoveNode( Node node )
        {
            if ( node.Connections.Values.Count( i => i is Element ) > 0 )
                return;

            while ( node.Connections.Count > 0 )
            {
                RemoveConnector( node.Connections.Values.First() );
            }
            Nodes.Remove( node );
        }
        #endregion
        #region Update
        private void ResetNodesState()
        {
            for ( int i = 0; i < Nodes.Count; i++ )
            {
                Nodes[i].Number = i;
                Nodes[i].Visited = false;
            }

            foreach ( Connector connetor in Connectors )
            {
                connetor.I = 0;
            }
        }
        public void Update()
        {
            ResetNodesState();                                                                      // Сброс состояния нод, присвоение им номеров

            ChainsSerach();                                                                         // Получаем список цепей
            UpdateChainNumbers();
            ContoursSearch();                                                                       // Получаем список замкнутых контуров

            if ( Contours.Count < 1 )
                return;

            if ( Contours.Count > 15 )
                throw new WrongSchemeException( "Too difficult" );

            if ( Contours.Count == 1 )
            {
                if ( Contours.First().Connectors.Count( i => i is Battery ) != 1 )
                    throw new WrongSchemeException( "Check layout! Wrong batteries amount!" );

                Battery bat = Contours.First().Connectors.First( i => i is Battery ) as Battery;
                ElementsChain batChain = new ElementsChain( new Node[] { bat.NodeA, bat.NodeB } );
                double amp = bat.Eds / Contours.First().Connectors.Sum( i => i.Resistance ) * Contours.First().SameDirected( batChain );

                Contours.First().RunElectricCurrent( amp );
                return;
            }

            foreach ( Connector connector in Connectors )
            {
                if ( connector.NodeA.Connections.Count == 1 && connector.NodeB.Connections.Count > 1 ||
                    connector.NodeA.Connections.Count > 1 && connector.NodeB.Connections.Count == 1 )
                {
                    throw new WrongSchemeException( "Unconnected pin found!" );
                }
            }

            Rule1Count = Nodes.Count( i => i.Connections.Count > 2 ) - 1;                       // Количество уравнений по первому правилу
            int matrixSize = Chains.Count;                                                          // Размер матрицы (количество неизвестных)

            CountMatrix = new double[matrixSize, matrixSize];                             // Матрица для левой части уравнений
            resultVector = new double[matrixSize];                                         // Вектор для правой части уравнений

            for ( int i = 0; i < matrixSize; i++ )                                                  // Уравнение
            {
                for ( int j = 0; j < matrixSize; j++ )                                              // Неизвестная
                {
                    if ( i < Rule1Count )                                                           // Первое правило
                    {
                        Node curNode = Nodes                                                        // Следующая по списку нода с >2 ног
                            .Where( nd => nd.Connections.Count > 2 )
                            .ElementAt( i );
                        CountMatrix[i, j] = Chains[j].GetCurrentDirection( curNode );
                    }
                    else                                                                            // Второе правило
                    {
                        // Если контур включает в себя хотябы один коннектор из цепочки, то он включает в себя всю цепочку
                        if ( Contours[i - Rule1Count].Connectors.Contains( Chains[j].Connectors.First() ) )
                        {
                            CountMatrix[i, j] = Chains[j].GetResistance() * Contours[i - Rule1Count].SameDirected( Chains[j] );
                        }
                    }
                }
                resultVector[i] = i < Rule1Count ? 0 : Contours[i - Rule1Count].GetEds();      // Результирующий вектор (0 или сумма ЭДС)
            }

            KramerCounter kc = new KramerCounter( CountMatrix, resultVector );          // Крамер
            double[] xv = kc.Answer;

            for ( int i = 0; i < xv.Length; i++ )
            {
                Chains[i].RunElectricCurrent( -xv[i] );
            }
            Fire( "Quations", "DisplayMatrix" );
        }
        #endregion
        #region ChainsSerach
        private void ChainsSerach()
        {
            Chains.Clear();                                                                                 // Очищаем списки

            foreach ( Node node in Nodes.Where( node => node.Connections.Count > 2 ) )                      // Для каждого узла
            {
                foreach ( Node nextNode in node.Connections.Keys.Where( i => !i.Visited ) )                 // Для каждой непосещенной ноды из смежных
                {
                    ChainDeeper( node, nextNode );                                                          // Уходим вглубь
                }
            }
        }
        private void ChainDeeper( Node firstNode, Node secondNode )
        {
            ElementsChain chain = new ElementsChain();
            chain.AddNode( firstNode );                                                                             // Добавляем текущую ноду в цепь
            chain.AddNode( secondNode );                                                                            // Добавляем предыдущую ноду в цепь

            Node currentNode = secondNode;
            Node prevNode = firstNode;

            while ( currentNode.Connections.Count < 3 )
            {
                if ( currentNode.Connections.Count < 2 )                                                            // Тупик
                    return;
                Node nextNode = currentNode.Connections.Keys.FirstOrDefault( i => i != prevNode );
                prevNode = currentNode;                                                                             // Переходим на следующую
                currentNode = nextNode;
                chain.AddNode( currentNode );                                                                       // Записываем ноду в цепь
            }
            if ( Chains.FirstOrDefault( i => i.Nodes.First() == chain.Nodes.Last() && i.Nodes.Last() == chain.Nodes.First() ) == null )     // Проверка на существующий путь
                Chains.Add( chain );                                                                                    // Добавляем цепь в список цепей
        }
        private void UpdateChainNumbers()
        {
            for ( int i = 0; i < Chains.Count; i++ )
            {
                Chains[i].Number = i;
            }
        }
        #endregion
        #region ContoursSearch
        private void ContoursSearch()
        {
            Contours.Clear();
            int[] color = new int[Nodes.Count];
            for ( int i = 0; i < Nodes.Count; i++ )             // По каждой ноде
            {
                for ( int k = 0; k < Nodes.Count; k++ )         // Сбрасываем цвет всех нод на 1
                    color[k] = 1;
                ElementsContour contour = new ElementsContour();            // Цикл
                contour.AddNode( Nodes[i] );                          // Добавляем начальную точку в цикл
                DFScycle( i, i, color, -1, contour );             // Находим списки всех циклов с началом в текущей точке
            }
        }
        private void DFScycle( int u, int endV, int[] color, int unavailableEdge, ElementsContour contour )
        {
            //если u == endV, то эту вершину перекрашивать не нужно, иначе мы в нее не вернемся, а вернуться необходимо
            if ( u != endV )
                color[u] = 2;                                  // Закрашиваем посещенную вершину
            else if ( contour.Count >= 2 )
            {
                if ( isContourFree( contour ) )                 // Проверяем, существует ли эта цепь
                    Contours.Add( contour );
                return;
            }
            for ( int w = 0; w < Connectors.Count; w++ )
            {
                if ( w == unavailableEdge )
                    continue;
                if ( color[Connectors[w].NodeB.Number] == 1 && Connectors[w].NodeA.Number == u )
                {
                    ElementsContour newContour = new ElementsContour( contour );
                    newContour.AddNode( Connectors[w].NodeB );
                    DFScycle( Connectors[w].NodeB.Number, endV, color, w, newContour );
                    color[Connectors[w].NodeB.Number] = 1;
                }
                else if ( color[Connectors[w].NodeA.Number] == 1 && Connectors[w].NodeB.Number == u )
                {
                    ElementsContour newContour = new ElementsContour( contour );
                    newContour.AddNode( Connectors[w].NodeA );
                    DFScycle( Connectors[w].NodeA.Number, endV, color, w, newContour );
                    color[Connectors[w].NodeA.Number] = 1;
                }
            }
        }
        private bool isContourFree( ElementSequence contour )
        {
            foreach ( ElementSequence checkContour in Contours.Where( i => i.Nodes.Length == contour.Count ) )
            {
                int i = -1;
                while ( ++i < contour.Count )
                {
                    if ( !checkContour.Nodes.Contains( contour.Nodes[i] ) )
                        break;
                }
                if ( i == contour.Count )
                    return false;
            }
            return true;
        }
        #endregion
        #region Clear
        internal void Clear()
        {
            Element toDel;
            while ( ( toDel = ConnectorsToDraw.FirstOrDefault( i => i is Element ) as Element ) != null )
            {
                RemoveElement( toDel );
            }
        }
        #endregion
        #region GetValue
        public double GetResistorXResistance()
        {
            ResistorX resistorx = Connectors.FirstOrDefault( i => i is ResistorX ) as ResistorX;
            return resistorx == null ? double.NaN : resistorx.Resistance;
        }
        #endregion
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
        public class ResistorXPlacedException : Exception           // Rx уже существует
        {
            public ResistorXPlacedException() : base() { }
            public ResistorXPlacedException( string message ) : base( message ) { }
            public ResistorXPlacedException( string message, Exception inner ) : base( message, inner ) { }
            protected ResistorXPlacedException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context )
                : base( info, context ) { }
        }
        [Serializable]
        public class WrongSchemeException : Exception
        {
            public WrongSchemeException() { }
            public WrongSchemeException( string message ) : base( message ) { }
            public WrongSchemeException( string message, Exception inner ) : base( message, inner ) { }
            protected WrongSchemeException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context )
                : base( info, context ) { }
        }
        #endregion
    }

    abstract class ElementSequence : INotifyPropertyChanged
    {
        #region Properties
        protected List<Node> nodes;
        protected List<Connector> connectors;

        public Node[] Nodes
        {
            get { return nodes.ToArray(); }
        }
        public Connector[] Connectors
        {
            get { return connectors.ToArray(); }
        }
        public int Count { get { return nodes.Count; } }
        public string Text
        {
            get
            {
                string s = "";
                for ( int i = 0; i < nodes.Count - 1; i++ )
                {
                    s += string.Format( "{0}-", nodes[i].Number );
                }
                s += nodes.Last().Number.ToString();
                return s;
            }
        }
        #endregion
        #region Determination
        public ElementSequence()
        {
            nodes = new List<Node>();
            connectors = new List<Connector>();
        }
        #endregion
        #region Methods
        public void Clear()
        {
            nodes.Clear();
            connectors.Clear();
        }
        public void AddNode( Node node )
        {
            if ( nodes.Count > 0 )
            {
                Connector connector = Nodes.Last().Connections[node];

                if ( connector == null )
                    return;             // Ну и поругаться можно

                connectors.Add( connector );
            }
            nodes.Add( node );
        }
        public void RunElectricCurrent( double amperage )
        {
            connectors.ForEach( i =>
            {
                i.I = nodes.FirstOrDefault( j => j == i.NodeA || j == i.NodeB ) == i.NodeA ? amperage : -amperage;
            } );
        }
        #endregion
        #region Fire
        protected void Fire( params string[] names )
        {
            if ( PropertyChanged != null )
                foreach ( var name in names )
                    PropertyChanged( this, new PropertyChangedEventArgs( name ) );
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
    class ElementsContour : ElementSequence
    {
        #region Determination
        public ElementsContour() { }
        public ElementsContour( ElementSequence elementSequence )
        {
            foreach ( Node node in elementSequence.Nodes )
            {
                AddNode( node );
            }
        }
        #endregion
        #region Methods
        public double GetEds()
        {
            return connectors
                .Where( i => i is Battery )
                .Sum( i => ( i as Battery ).Eds * ( nodes
                    .FirstOrDefault( pin => pin == i.NodeA || pin == i.NodeB ) == i.NodeA ? -1 : 1 )
                );
        }
        public int SameDirected( ElementsChain chain )
        {
            var left = Nodes.Skip( 1 ).TakeWhile( i => i != chain.Nodes.First() ).ToArray();
            var right = Nodes.Skip( left.Length + 1 ).ToArray();
            var rotated = right.Concat( left ).ToArray();
            return rotated[1] == chain.Nodes[1] ? 1 : rotated.Last() == chain.Nodes[1] ? -1 : 0;
        }
        #endregion
    }
    class ElementsChain : ElementSequence
    {
        #region Determination
        public ElementsChain() { }
        public ElementsChain( IEnumerable<Node> inpNodes )
        {
            nodes = new List<Node>();
            connectors = new List<Connector>();
            foreach ( Node node in inpNodes )
            {
                AddNode( node );
            }
        }
        #endregion
        #region Properties
        private int number;
        public int Number
        {
            get { return number; }
            set
            {
                if ( number != value )
                {
                    number = value;
                    Fire( "Number", "Background" );
                }
            }
        }
        #endregion
        #region Methods
        public double GetResistance()
        {
            return connectors.Sum( i => i.Resistance );
        }
        public double GetCurrentDirection( Node node )
        {
            return node == nodes.First() ? -1 : ( node == nodes.Last() ? 1 : 0 );
        }
        #endregion
    }
}
