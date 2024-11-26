using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CSparse;
using MathNet.Numerics.LinearAlgebra.Double;

public static class SparseMatrixExtensions
{
    /// <summary>
    /// Compute the sparse LU factorization for given matrix.
    /// </summary>
    /// <param name="matrix">The matrix to factorize.</param>
    /// <param name="tol">Partial pivoting tolerance (form 0.0 to 1.0).</param>
    /// <returns></returns>
    public static SparseLu ComputeSparseLu(this SparseMatrix matrix, double tol = 1.0)
    {
        return SparseLu.Create(matrix, ColumnOrdering.MinimumDegreeAtPlusA, tol);
    }
}