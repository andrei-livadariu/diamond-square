﻿using System.Linq;
using UnityEngine;

public class DiamondSquareAlgorithm : IIterativeGeneratorAlgorithm
{
    private DiamondSquareParameters _parameters;

    private int _res;
    private float[,] _heights;
    private int _iterationStep;

    private float[] _seeds;
    private float _variation;
    private float _smoothness;
    private float _outsideHeight;
    private float _heightScaling;
    private bool _inProgress;
    private int _currentIteration;

    private float _minHeight;
    private float _maxHeight;

    public int CurrentIteration
    {
        get { return _currentIteration; }
    }

    public int Resolution
    {
        get { return _res; }
    }

    public int IntermediateResolution
    {
        get { return ((_res - 1) / _iterationStep) + 1; }
    }

    public float[,] Heights
    {
        get
        {
            float[,] heights = new float[_res, _res];
            int i, j;
            for (i = 0; i < _res; ++i)
            {
                for (j = 0; j < _res; ++j)
                {
                    heights[i, j] = Mathf.InverseLerp(_minHeight, _maxHeight, _heights[i, j]) * _heightScaling;
                }
            }
            return heights;
        }
    }

    public float[,] IntermediateHeights
    {
        get
        {
            int iRes = IntermediateResolution;
            int iStep = _iterationStep;
            float[,] heights = new float[iRes, iRes];
            int i, j;
            for (i = 0; i < iRes; ++i)
            {
                for (j = 0; j < iRes; ++j)
                {
                    heights[i, j] = Mathf.InverseLerp(_minHeight, _maxHeight, _heights[i * iStep, j * iStep]) * _heightScaling;
                }
            }
            return heights;
        }
    }

    private float _RandomVariation
    {
        get { return Random.Range(-_variation, _variation); }
    }

    public DiamondSquareAlgorithm(DiamondSquareParameters parameters)
    {
        _parameters = parameters;
        _reset();
    }

    private void _reset()
    {
        _res = (int)Mathf.Pow(2, _parameters.nrIterations) + 1;
        _heights = new float[_res, _res];
        _currentIteration = 0;
        _inProgress = false;

        _seeds = _parameters.seeds;
        _variation = _parameters.variation;
        _smoothness = _parameters.smoothness;
        _outsideHeight = _parameters.outsideHeight;
        _heightScaling = _parameters.heightScaling;

        _iterationStep = _res - 1;

        _resetHeights();
    }

    private void _resetHeights()
    {
        int i, j;
        for (i = 0; i < _res; ++i)
        {
            for (j = 0; j < _res; ++j)
            {
                _heights[i, j] = 0f;
            }
        }
        // Initializing the corners
        _heights[0, 0] = _seeds[0];
        _heights[0, _res - 1] = _seeds[1];
        _heights[_res - 1, 0] = _seeds[2];
        _heights[_res - 1, _res - 1] = _seeds[3];
    }

    public void Generate()
    {
        _reset();
        _inProgress = true;
        while (_iterationStep > 1)
        {
            Iterate();
        }
    }

    public void Iterate()
    {
        if (!_inProgress)
        {
            _reset();
            _inProgress = true;
        }
        
        _minHeight = float.MaxValue;
        _maxHeight = float.MinValue;

        int i, j;
        for (i = 0; i < _res - 1; i += _iterationStep)
        {
            for (j = 0; j < _res - 1; j += _iterationStep)
            {
                Process(
                    new Vector3(i, j, _heights[i, j]),
                    new Vector3(i + _iterationStep, j, _heights[i + _iterationStep, j]),
                    new Vector3(i, j + _iterationStep, _heights[i, j + _iterationStep]),
                    new Vector3(i + _iterationStep, j + _iterationStep, _heights[i, j + _iterationStep])
                );
            }
        }
        _variation *= Mathf.Pow(2, -_smoothness);
        _iterationStep /= 2;
        ++_currentIteration;

        if (_iterationStep <= 1)
        {
            _inProgress = false;
        }
    }

    private void Process(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // Diamond step
        Vector3 dMid = DiamondStep(p1, p2, p3, p4);

        // Square step
        SquareStep(Vector3.zero, p1, p2, dMid, 1);
        SquareStep(p1, Vector3.zero, dMid, p3, 2);
        SquareStep(p2, dMid, Vector3.zero, p4, 3);
        SquareStep(dMid, p3, p4, Vector3.zero, 4);
    }

    private Vector3 DiamondStep(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        Vector3 dMid = new Vector3(
            (p1.x + p2.x) / 2,
            (p1.y + p3.y) / 2,
            _avg(p1.z, p2.z, p3.z, p4.z) + _RandomVariation
        );
        _heights[(int)dMid.x, (int)dMid.y] = dMid.z;
        _trackMinMaxHeight(dMid.z);
        return dMid;
    }

    private void SquareStep(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, int nullIndex)
    {
        switch (nullIndex)
        {
            case 1:
                p1 = new Vector3(p4.x, (2 * p2.y - p4.y), 0.0f);
                _initializeHeight(ref p1);
                break;
            case 2:
                p2 = new Vector3((2 * p1.x - p3.x), p3.y, 0.0f);
                _initializeHeight(ref p2);
                break;
            case 3:
                p3 = new Vector3((2 * p1.x - p2.x), p2.y, 0.0f);
                _initializeHeight(ref p3);
                break;
            case 4:
            default:
                p4 = new Vector3(p1.x, (2 * p2.y - p1.y), 0.0f);
                _initializeHeight(ref p4);
                break;
        }

        Vector3 sqMid = new Vector3(
            (p2.x + p3.x) / 2,
            (p1.y + p4.y) / 2,
            _avg(p1.z, p2.z, p3.z, p4.z) + _RandomVariation
         );
        _heights[(int)sqMid.x, (int)sqMid.y] = sqMid.z;
        _trackMinMaxHeight(sqMid.z);
    }

    private void _initializeHeight(ref Vector3 point)
    {
        point.z = _isPointInside(point, _res) ? _heights[(int)point.x, (int)point.y] : _outsideHeight;
    }

    private void _trackMinMaxHeight(float height)
    {
        if (height > _maxHeight)
        {
            _maxHeight = height;
        }
        else if (_minHeight > 0 && height < _minHeight)
        {
            _minHeight = Mathf.Max(height, 0f);
        }
    }

    private static bool _isPointInside(Vector3 point, float res)
    {
        return point.x >= 0 && point.x < res && point.y >= 0 && point.y < res;
    }

    private static float _avg(params float[] numbers)
    {
        return numbers.Average();
    }
}
