using System;

namespace Circuit_Layout
{
    class KramerCounter
    {
        #region Determination
        public KramerCounter( double[,] left, double[] right )
        {
            LeftMatrix = left;
            RightVector = right;
            KramerCount();
        }
        #endregion
        #region Properties
        public double[] Answer { get; private set; }
        public double[,] LeftMatrix { get; private set; }
        public double[] RightVector { get; private set; }
        #endregion
        #region Methods
        private void KramerCount()
        {
            if ( RightVector.Length == 0 )
                throw new KramerCountException( "Zero length matrix" );

            double mainDet = Determinant( LeftMatrix );     // Главный детерминант

            if ( mainDet == 0 )
                throw new KramerCountException( "The system is inconsistent or has many solutions" );   // Матрица несовместна

            int matrixSize = RightVector.Length;        // Размер матрицы
            Answer = new double[matrixSize];            // Вектор ответов

            double[,] tmpMatrix = new double[matrixSize, matrixSize];
            for ( int i = 0; i < matrixSize; i++ )
            {
                for ( int j = 0; j < matrixSize; j++ )
                {
                    tmpMatrix[i, j] = LeftMatrix[i, j];
                }
            }

            for ( int j = 0; j < matrixSize; j++ )                  // Номер ответа
            {
                for ( int i = 0; i < matrixSize; i++ )              // Формирование tmpMatrix
                {
                    tmpMatrix[i, j] = RightVector[i];
                }

                double curDet = Determinant( tmpMatrix );           // Восстановление tmpMatrix до состояния LeftMatrix

                Answer[j] = curDet / mainDet;

                for ( int i = 0; i < matrixSize; i++ )
                {
                    tmpMatrix[i, j] = LeftMatrix[i, j];
                }
            }
        }
        #region Determinant/Minor
        private double Determinant( double[,] matrix )
        {
            int n = matrix.GetLength( 0 );
            double result = 0;
            if ( n < 3 )
            {
                result = n == 1 ? matrix[0, 0] : matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
            }
            else
            {
                for ( int j = 0; j < n; j++ )
                {
                    result += ( j % 2 == 0 ? 1 : -1 ) * matrix[0, j] * Determinant( Minor( matrix, 0, j ) );
                }
            }
            return result;
        }
        private double[,] Minor( double[,] matrix, int row, int column )
        {
            int n = matrix.GetLength( 0 );
            double[,] result = new double[n - 1, n - 1];
            int x = 0, y = 0;

            for ( int i = 0; i < n; i++ )
            {
                if ( row == i )
                    continue;

                for ( int j = 0; j < n; j++ )
                {
                    if ( column == j )
                        continue;

                    result[x, y] = matrix[i, j];
                    y++;
                }
                x++;
                y = 0;
            }
            return result;
        }
        #endregion
        #endregion
        #region Exceptions
        [Serializable]
        public class KramerCountException : Exception
        {
            public KramerCountException() { }
            public KramerCountException( string message ) : base( message ) { }
            public KramerCountException( string message, Exception inner ) : base( message, inner ) { }
            protected KramerCountException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context )
                : base( info, context ) { }
        }
        #endregion
    }
}
