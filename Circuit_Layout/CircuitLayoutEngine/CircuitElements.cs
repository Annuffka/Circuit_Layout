using System;
using System.Linq;

namespace Circuit_Layout
{
    class Resistor : Element
    {
        #region Determination
        public Resistor()
        {
            Resistance = 100;
            Name = "R";
            IsEditable = false;
        }
        #endregion
        #region Properties
        private bool editable;
        public bool IsEditable
        {
            get { return editable; }
            set
            {
                if ( editable != value )
                {
                    editable = value;
                    Fire( "EditPartVisibility" );
                }
            }
        }
        public System.Windows.Visibility EditPartVisibility
        {
            get { return IsEditable ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
        }
        #endregion
    }
    class ResistorX : Element
    {
        #region Determination
        public ResistorX()
        {
            Random r = new Random();
            Resistance = r.Next( 10, 100000 ) / 100d;
            Name = "Rx";
        }
        #endregion
    }
    class NoDrawConnector : Element
    {

    }
    class Reohord : Element
    {
        #region Determination
        public Reohord()
        {
            Resistance = 100;
            OffsetA = -100;
            OffsetB = 100;
            OffsetC = -30;
            Name = "Rh";
            DisplayLength = true;
            PropertyChanged += Reohord_PropertyChanged;
        }

        void Reohord_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "Resistance" )
            {
                ResistorAC.Resistance = LengthAC / 100 * Resistance;
                ResistorBC.Resistance = LengthBC / 100 * Resistance;
            }
        }
        #endregion
        #region Properties
        #region Values
        private double lengthac;
        private bool displayLength;
        public System.Windows.Visibility LengthVisibility
        {
            get
            {
                return displayLength ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }
        public System.Windows.Visibility ResistanceVisibility
        {
            get
            {
                return displayLength ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
        }
        public bool DisplayLength
        {
            get
            {
                return displayLength;
            }
            set
            {
                if ( displayLength != value )
                {
                    displayLength = value;
                    Fire( "LengthVisibility", "ResistanceVisibility" );
                }
            }
        }
        #endregion
        #region Pins
        public Node NodeC { get; set; }
        public double LengthAC
        {
            get
            {
                return Math.Round( lengthac, 2 );
            }
            set
            {
                if ( lengthac != value )
                {
                    lengthac = value;
                    ResistorAC.Resistance = value / 100 * Resistance;
                    ResistorBC.Resistance = ( 100 - value ) / 100 * Resistance;
                    Fire( "LengthAC", "LengthBC" );
                }
            }
        }
        public double LengthBC
        {
            get { return Math.Round( 100 - lengthac, 2 ); }
        }
        public Connector ResistorAC { get; set; }
        public Connector ResistorBC { get; set; }
        public int OffsetC { get; protected set; }
        #endregion
        #endregion
        #region Methods
        protected override void PlacePins()
        {
            base.PlacePins();
            if ( NodeC != null )
            {
                NodeC.X = (int)( X + OffsetC * Math.Cos( ( Angle + 90 ) * Math.PI / 180 ) );
                NodeC.Y = (int)( Y + OffsetC * Math.Sin( ( Angle + 90 ) * Math.PI / 180 ) );
            }
        }
        #endregion
    }
    class Potentiometer : Element
    {
        #region Determination
        public Potentiometer()
        {
            OffsetA = -100;
            OffsetB = 100;
            MaxResistance = 100;
            Length = 50;
            Name = "Re";
        }
        #endregion
        #region Properties
        private double length, maxResistance;
        public double MaxResistance
        {
            get { return maxResistance; }
            set
            {
                if (maxResistance != value)
                {
                    maxResistance = value;
                    Fire( "MaxResistance", "Resistance" );
                }
            }
        }
        public double Length
        {
            get { return length; }
            set
            {
                if ( length != value )
                {
                    Resistance = length / 100 * MaxResistance;
                    length = value;
                    Fire( "Length", "Resistance" );
                }
            }
        }
        #endregion
    }
    class Battery : Element
    {
        #region Determination
        public Battery()
        {
            Resistance = 1;
            Eds = 1;
            Name = "Gb";
        }
        #endregion
        #region Properties
        public double Eds { get; set; }
        #endregion
    }
    class Amperemeter : Element
    {
        #region Determination
        public Amperemeter()
        {
            OffsetA = -40;
            OffsetB = 40;
            Resistance = 0;
            Division = 0.001;
            Name = "mA";
            PropertyChanged += Ampermeter_PropertyChanged;
        }
        void Ampermeter_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "I" )
                Fire( "ArrowAngle" );
        }
        #endregion
        #region Properties
        private double[] divisions;
        private double division;
        public double Division
        {
            get
            {
                return division;
            }
            set
            {
                if ( division != value )
                {
                    division = value;
                    divisions = new double[5]
                    .Select( ( i, j ) => ( j - 2 ) / 2d )
                    .ToArray();
                    Fire( "Division", "Divisions" );
                }
            }
        }
        public double ArrowAngle
        {
            get
            {
                double a = I * 10 / Division;
                return a > 100 ? 100 : a < -100 ? -100 : a;
            }
        }
        public double[] Divisions
        {
            get
            {
                return divisions;
            }
        }
        #endregion
    }
}
