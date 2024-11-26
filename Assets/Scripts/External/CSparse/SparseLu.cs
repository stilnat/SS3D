    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Factorization;
    using MathNet.Numerics.LinearAlgebra.Storage;
    using System;

    // Create an alias for CSparse's SparseLU class.
    using CSparseLU = CSparse.Double.Factorization.SparseLU;

    // Create an alias for CSparse's SparseMatrix class.
    using CSparseMatrix = CSparse.Double.SparseMatrix;
    using SparseMatrix = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix;

    public class SparseLu : ISolver<double>
    {
        private int _n;
        private CSparseLU _lu;

        private SparseLu(CSparseLU lu, int n)
        {
            _n = n;
            _lu = lu;
        }



        /// <summary>
        /// Compute the sparse LU factorization for given matrix.
        /// </summary>
        /// <param name="matrix">The matrix to factorize.</param>
        /// <param name="ordering">The column ordering method to use.</param>
        /// <param name="tol">Partial pivoting tolerance (form 0.0 to 1.0).</param>
        /// <returns>Sparse LU factorization.</returns>
        public static SparseLu Create(SparseMatrix matrix, CSparse.ColumnOrdering ordering,
            double tol = 1.0)
        {
            int n = matrix.RowCount;

            // Get CSR storage.
            SparseCompressedRowMatrixStorage<double> storage = (SparseCompressedRowMatrixStorage<double>)matrix.Storage;

            // Create CSparse matrix.
            CSparseMatrix cSparse = new(n, n)
            {
                // Assign storage arrays.
                ColumnPointers = storage.RowPointers,
                RowIndices = storage.ColumnIndices,
                Values = storage.Values,
            };

            return new(CSparseLU.Create(cSparse, ordering, tol), n);
        }

        /// <summary>
        /// Solves a system of linear equations, <c>Ax = b</c>, with A LU factorized.
        /// </summary>
        /// <param name="input">The right hand side vector, <c>b</c>.</param>
        /// <param name="result">The left hand side vector, <c>x</c>.</param>
        public void Solve(Vector<double> input, Vector<double> result)
        {
            // Check for proper arguments.
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            // Check for proper dimensions.
            if (input.Count != result.Count)
            {
                throw new ArgumentException("vectors must be same length");
            }

            if (input.Count != _n)
            {
                throw new ArgumentException("Dimensions don't match", "input");
            }

            var b = input.Storage as DenseVectorStorage<double>;
            var x = result.Storage as DenseVectorStorage<double>;

            if (b == null || x == null)
            {
                throw new NotSupportedException("Expected dense vector storage.");
            }

            _lu.SolveTranspose(b.Data, x.Data);
        }


        public Vector<double> Solve(Vector<double> input)
        {
            var result = Vector<double>.Build.Dense(input.Count);

            Solve(input, result);

            return result;
        }

        public void Solve(Matrix<double> input, Matrix<double> result)
        {
            throw new NotImplementedException();
        }

        public Matrix<double> Solve(Matrix<double> input)
        {
            throw new NotImplementedException();
        }
    }