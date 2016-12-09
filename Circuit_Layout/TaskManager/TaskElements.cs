using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Circuit_Layout
{
    public class Diagram : INotifyPropertyChanged
    {
        #region Determination
        public Diagram( int columna, int columnb, StackPanel spa, StackPanel spb )
        {
            ColumnA = columna;
            ColumnB = columnb;
            Points = new ObservableCollection<DiagramPoint>();
            SignaturesVertical = new ObservableCollection<DiagramSignature>();
            SignaturesHorisontal = new ObservableCollection<DiagramSignature>();
            for ( int i = 0; i < 10; i++ )
            {
                SignaturesVertical.Add( new DiagramSignature() { Position = i * 20, Signature = i < 10 ? (double?)i : null } );
                SignaturesHorisontal.Add( new DiagramSignature() { Position = i * 20, Signature = i < 10 ? (double?)i : null } );
            }
            XName = spa;
            YName = spb;
        }
        #endregion
        #region Methods
        public void Add( double x, double y )
        {
            DiagramPoint dp = new DiagramPoint( x, y, this );
            Points.Add( dp );
            UpdateSignatures();
        }
        private void UpdateSignatures()
        {
            double ddx = ( MaxX - MinX ) / 8;
            double ddy = ( MaxY - MinY ) / 8;

            for ( int i = 0; i < SignaturesVertical.Count; i++ )
            {
                SignaturesVertical[i].Signature = Math.Round( MinX + ddx * i, ddx < 2 ? 2 : 0 );        //Если дельта меньше 2, то округляем до 2 знаков после запятой
            }
            for ( int i = 0; i < SignaturesHorisontal.Count; i++ )
            {
                SignaturesHorisontal[SignaturesHorisontal.Count - i - 1].Signature = Math.Round( MinY + ddy * i, ddy < 2 ? 2 : 0 );
            }
        }
        #endregion
        #region Properties
        public double MinX { get { return Points.Count > 0 ? Points.Min( i => i.X ) : 0; } }
        public double MinY { get { return Points.Count > 0 ? Points.Min( i => i.Y ) : 0; } }
        public double MaxX { get { return Points.Count > 0 ? Points.Max( i => i.X ) : 1; } }
        public double MaxY { get { return Points.Count > 0 ? Points.Max( i => i.Y ) : 1; } }
        public double DX { get { return 160 / ( MaxX - MinX ); } }
        public double DY { get { return 160 / ( MaxY - MinY ); } }
        public int ColumnA { get; private set; }
        public int ColumnB { get; private set; }
        public StackPanel XName { get; private set; }
        public StackPanel YName { get; private set; }
        public ObservableCollection<DiagramSignature> SignaturesVertical { get; set; }
        public ObservableCollection<DiagramSignature> SignaturesHorisontal { get; set; }
        public ObservableCollection<DiagramPoint> Points { get; set; }
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
    }
    public class DiagramSignature : INotifyPropertyChanged
    {
        #region Properties
        private double? position, signature;
        public double? Position
        {
            get { return position; }
            set
            {
                if ( position != value )
                {
                    position = value;
                    Fire( "Position" );
                }
            }
        }
        public double? Signature
        {
            get { return signature; }
            set
            {
                if ( signature != value )
                {
                    signature = value;
                    Fire( "Signature" );
                }
            }
        }
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
    }
    public class DiagramPoint : INotifyPropertyChanged
    {
        #region Determination
        public DiagramPoint( double inpX, double inpY, Diagram owner )
        {
            X = inpX;
            Y = inpY;
            Owner = owner;
        }
        #endregion
        #region Properties
        private Diagram Owner;
        private double x, y;
        public double X
        {
            get
            {
                return x;
            }
            set
            {
                if ( x != value )
                {
                    x = value;
                    Fire( "X", "RX" );
                }
            }
        }
        public double Y
        {
            get
            {
                return y;
            }
            set
            {
                if ( y != value )
                {
                    y = value;
                    Fire( "Y", "YX" );
                }
            }
        }
        public double RX
        {
            get { return ( X - Owner.MinX ) * Owner.DX + 60; }
        }
        public double RY
        {
            get { return 210 - ( ( Y - Owner.MinY ) * Owner.DY ); }
        }
        #endregion
        #region Fire
        public void Fire( params string[] names )
        {
            if ( PropertyChanged != null )
                foreach ( var name in names )
                    PropertyChanged( this, new PropertyChangedEventArgs( name ) );
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
    public class TableCell : INotifyPropertyChanged
    {
        #region Determination
        public TableCell( string text )
        {
            Text = text;
            Width = 100;
            IsHeader = false;
        }
        public TableCell( string text, StackPanel stackPanel )
        {
            Text = text;
            SPanel = stackPanel;
            Width = 100;
            IsHeader = true;
        }
        #endregion
        #region Properties
        public bool IsHeader { get; private set; }
        public StackPanel SPanel { get; set; }
        private string txt;
        public string Text
        {
            get { return txt; }
            set
            {
                if ( txt != value )
                {
                    txt = value;
                    Fire( "Text" );
                }
            }
        }
        public int Width { get; set; }
        public Visibility HeaderVisibility { get { return IsHeader ? Visibility.Visible : Visibility.Hidden; } }
        public Visibility NotHeaderVisibility { get { return IsHeader ? Visibility.Hidden : Visibility.Visible; } }
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
    }
}
