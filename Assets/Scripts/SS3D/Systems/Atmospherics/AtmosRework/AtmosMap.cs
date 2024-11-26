﻿using CSparse;
using CSparse.Double.Factorization;
using CSparse.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Single.Solvers;
using SS3D.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SS3D.Systems.Tile;
using Unity.Mathematics;

namespace SS3D.Engine.AtmosphericsRework
{
    public class AtmosMap
    {
        private TileMap tileMap;
        private string mapName;
        private AtmosManager atmosManager;
        
        private AtmosContainer[] _atmosGridList;

        private double[] _velHor;
        private double[] _velVert;
        private double[] _velHorPrev;
        private double[] _velVertPrev;
        private double[] _dens;
        private double[] _densPrev;
        
        private Vector3 _originPosition;
        private float _tileSize = 1f;

        public int Width { get; }

        public int WidthAndBorder => Width + 2;

        public int Size => WidthAndBorder * WidthAndBorder;
        
        public double[] Dens => _dens;

        private int Coords(int i, int j) => i + (WidthAndBorder * j);

        private double _massAdded;

        private SparseLu _diffuseSystem;

        public AtmosMap(TileMap tileMap, string name, int width)
        {
            atmosManager = Subsystems.Get<AtmosManager>();
            mapName = name;
            this.tileMap = tileMap;
            Width = width;
            _velHor = new double[Size];
            _velVert = new double[Size];
            _velHorPrev = new double[Size];
            _velVertPrev = new double[Size];
            _dens = new double[Size];
            _densPrev = new double[Size];
            _originPosition = new Vector3(- width / 2, 0, - width / 2);
            
        }

        public void Simulate(float dt, float viscosity)
        {
            if (_diffuseSystem == null)
            {
                CreateDiffuseSystem(dt, viscosity);
            }


            //VelStep(_velHor, _velVert, _velHorPrev, _velVertPrev, viscosity, dt);
            DensStep(_dens, _densPrev, _velHor, _velVert, viscosity, dt);
            CleanPrevs();
            TotalMass();
        }

        public double DensityAtWorldPosition(Vector3 pos)
        {
            Vector2Int xy = GetXY(pos);
            return _dens[Coords(xy.x, xy.y)];
        }
        
        public double2 VelocityAtWorldPosition(Vector3 pos)
        {
            Vector2Int xy = GetXY(pos);
            return new(_velHor[Coords(xy.x, xy.y)], _velVert[Coords(xy.x, xy.y)]);
        }
        
        public void AddGas(Vector3 worldPosition, float amount)
        {
            Vector2Int coords = GetXY(worldPosition);
            int i = Coords(coords.x, coords.y);
            _densPrev[i] += amount;
        }

        public void RemoveGas(Vector3 worldPosition, float amount)
        {
            Vector2Int coords = GetXY(worldPosition);
            int i = Coords(coords.x, coords.y);
            _densPrev[i] -= amount;
            _densPrev[i] = math.max(0, _densPrev[i]);
        }

        public void ClearGas()
        {
            Array.Clear(_dens, 0, _dens.Length);
            Array.Clear(_velVert, 0, _velVert.Length);
            Array.Clear(_velHor, 0, _velHor.Length);
            _massAdded = 0f;
        }


        public Vector2Int GetXY(Vector3 worldPosition)
        {
            int x = (int)Math.Round((worldPosition.x - _originPosition.x) / _tileSize);
            int y = (int)Math.Round((worldPosition.z - _originPosition.z) / _tileSize);
            return new(x, y);
        }
     
        public Vector3 GetWorldPosition(int x, int y)
        {
            return (new Vector3(x, 0, y) * _tileSize) + _originPosition;
        }

        public TileMap GetLinkedTileMap()
        {
            return tileMap;
        }

        private void TotalMass()
        {
            double mass = _dens.Sum();
            Debug.Log($"mass percent preserved {100 * mass/_massAdded}");
        }

        private void CleanPrevs()
        {
            Array.Clear(_velVertPrev, 0, _velVertPrev.Length);
            Array.Clear(_velHorPrev, 0, _velHorPrev.Length);
            Array.Clear(_densPrev, 0, _densPrev.Length);
        }

        private void Swap(ref double[] x, ref double[] x0)
        {
            (x0, x) = (x, x0);
        }

        private void DensStep(double[] x, double[] x0,  double[] u, double[] v, double diff, double dt)
        {
            AddSource(x, x0, dt);
            Swap(ref x, ref x0);
            Diffuse(0, diff, ref x, x0, dt);
            Swap(ref x, ref x0);
            Advect(0, x, x0, u, v, dt);
        }
        
        private void VelStep(double[] u, double[] v,  double[] u0, double[] v0, double visc, double dt)
        {
            AddSource(v, v0, dt);
            AddSource(u, u0, dt);
            Swap(ref u, ref u0);
            Swap(ref v, ref v0);
            Diffuse(1, visc, ref u, u0, dt);
            Diffuse(2, visc, ref v, v0, dt);
            Project(u, v, u0, v0);
            Swap(ref u, ref u0);
            Swap(ref v, ref v0);
            Advect(1, u, u0, u0, v0, dt);
            Advect(2, v, v0, u0, v0, dt);
            Project(u, v, u0, v0);
        }
        
        private void Project(double[] u, double[] v, double[] p, double[] div)
        {
            float h = 1.0f / Width;

            for (int i = 1; i <= Width; i++)
            {
                for (int j = 1; j <= Width; j++)
                {
                    div[Coords(i, j)] = -0.5f * h * (u[Coords(i + 1, j)] - u[Coords(i - 1, j)] + v[Coords(i, j + 1)] - v[Coords(i, j - 1)]);
                    p[Coords(i, j)] = 0;
                }
            }

            SetBoundaries(0, div); 
            SetBoundaries(0, p);
            
            for (int k = 0; k < 20; k++)
            {
                for (int i = 1; i <= Width; i++)
                {
                    for (int j = 1; j <= Width; j++) 
                    {
                        p[Coords(i, j)] = (div[Coords(i, j)] + p[Coords(i - 1, j)] + p[Coords(i + 1, j)] +
                            p[Coords(i, j - 1)] + p[Coords(i, j + 1)]) / 4;
                    }
                }
                
                SetBoundaries(0, p);
            }
            
            for (int i = 1; i <= Width; i++)
            {
                for (int j = 1; j <= Width; j++)
                {
                    u[Coords(i, j)] -= 0.5f * (p[Coords(i + 1, j)] - p[Coords(i - 1, j)]) / h;
                    v[Coords(i, j)] -= 0.5f * (p[Coords(i, j+1)] - p[Coords(i, j - 1)]) / h;
                }
            }
            
            SetBoundaries(1, u); 
            SetBoundaries(2, v);
        }
        
        private void AddSource(double[] x, double[] x0, double dt)
        {
            for (int i = 0; i < Size; i++)
            {
                x[i] += dt * x0[i];
                _massAdded += dt * x0[i];
            }
        }

        private void CreateDiffuseSystem(double dt, double diff)
        {

            double a = dt * diff * Width * Width;

            SparseMatrix matrix = SparseMatrix.Create(Size, Size, (i, j) =>
            {
                /* // south west corner
                 if ((i == 0 && j == 1) || (i==0 && j == WidthAndBorder))
                 {
                     return 0.5f;
                 }

                 // south
                 if (i < WidthAndBorder && j == i+WidthAndBorder)
                 {
                     return 1f;
                 }

                 if (i == WidthAndBorder && j == i+WidthAndBorder)
                 {
                     return 0.5f;
                 }  
                 */  


                if (i == j)
                {
                    return 1 + 4 * a;
                }

                if (i - 1 == j || i + 1 == j || i == j - WidthAndBorder || i == j + WidthAndBorder)
                {
                    return -a;
                }

                return 0;
            });

            _diffuseSystem = SparseLu.Create(matrix, ColumnOrdering.MinimumDegreeAtPlusA, 0.1);
        }    
        
        private void Diffuse(int b, double diff, ref double[] x, double[] x0, double dt)
        {

            Vector<double> rightTerm = Vector<double>.Build.Dense(x0);
            Vector<double> result = Vector<double>.Build.Dense(x);
            _diffuseSystem.Solve(rightTerm, result);
            x = result.ToArray();


            //Vector<float> sol = _diffuseSystem.SolveIterative(rightTerm, new CompositeSolver());

            //x = sol.ToArray();



            /*for (int k = 0; k < 20; k++)
            {
                for (int i = 1; i <= Width; i++)
                {
                    for (int j = 1; j <= Width; j++)
                    {
                        x[Coords(i, j)] = (x0[Coords(i, j)] + (a * (x[Coords(i - 1, j)] + x[Coords(i + 1, j)] +
                            x[Coords(i, j - 1)] + x[Coords(i, j + 1)]))) / (1 + (4 * a));
                    }
                }

                SetBoundaries(b, x);
            }*/
        }
        
        private void Advect(int b,  double[] d, double[] d0,  double[] u, double[] v, double dt)
        {
            int i0, j0, i1, j1;
            double x, y, s0, t0, s1, t1, dt0;
            dt0 = dt * Width;
            for (int i = 1; i <= Width; i++) 
            {
                for (int j = 1; j <= Width; j++)
                {
                    x = i - (dt0 * u[Coords(i, j)]);
                    y = j - (dt0 * v[Coords(i, j)]);

                    x = math.clamp(x, 0.5f, Width + 0.5f);
                    y = math.clamp(y, 0.5f, Width + 0.5f);

                    i0 = (int)x; 
                    i1 = i0 + 1;
                    
                    j0 = (int)y;
                    j1 = j0 + 1;
                    s1 = x - i0;
                    s0 = 1 - s1;
                    t1 = y - j0;
                    t0 = 1 - t1;
                    
                    d[Coords(i, j)] = (s0 * ((t0 * d0[Coords(i0, j0)]) + (t1 * d0[Coords(i0, j1)]))) +
                        (s1 * ((t0 * d0[Coords(i1, j0)]) + (t1 * d0[Coords(i1, j1)])));
                }
            }

            SetBoundaries(b, d);
        }

        private void SetBoundaries(int b, double[] x)
        {
            for (int i = 1; i <= Width; i++)
            {
                x[Coords(0, i)] = b == 1 ? -x[Coords(1, i)] : x[Coords(1, i)];
                x[Coords(Width + 1, i)] = b == 1 ? -x[Coords(Width, i)] : x[Coords(Width, i)];
                x[Coords(i, 0)] = b == 2 ? -x[Coords(i, 1)] : x[Coords(i, 1)];
                x[Coords(i, Width + 1)] = b == 2 ? -x[Coords(i, Width)] : x[Coords(i, Width)];
            }
            
            x[Coords(0, 0)] = 0.5f * (x[Coords(1, 0)] + x[Coords(0, 1)]);
            x[Coords(0, Width + 1)] = 0.5f * (x[Coords(1, Width + 1)] + x[Coords(0, Width)]);
            x[Coords(Width + 1, 0)] = 0.5f * (x[Coords(Width, 0)] + x[Coords(Width + 1, 1)]);
            x[Coords(Width + 1, Width + 1)] = 0.5f * (x[Coords(Width, Width + 1)] + x[Coords(Width + 1, Width)]);
        }
    }
}