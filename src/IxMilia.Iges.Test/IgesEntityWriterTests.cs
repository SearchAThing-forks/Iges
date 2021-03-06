﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using IxMilia.Iges.Entities;
using Xunit;

namespace IxMilia.Iges.Test
{
    public class IgesEntityWriterTests
    {

        #region Private methods

        public static void VerifyFileContains(IgesFile file, string expectedText)
        {
            using (var ms = new MemoryStream())
            {
                file.Save(ms);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    var actual = reader.ReadToEnd();
                    Assert.Contains(expectedText.Trim('\r', '\n').Replace("\r", ""), actual);
                }
            }
        }

        private static void VerifyEntity(IgesEntity entity, string expectedText)
        {
            var file = new IgesFile();
            file.Entities.Add(entity);
            VerifyFileContains(file, expectedText);
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteNullEntityTest()
        {
            VerifyEntity(new IgesNull(), @"
       0       1       0       0       0                        00000000D      1
       0       0       0       1       0                                D      2
0;                                                                     1P      1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteCircularArcTest()
        {
            var circle = new IgesCircularArc(new IgesPoint(22, 33, 11), new IgesPoint(44, 55, 11), new IgesPoint(66, 77, 11));
            circle.Color = IgesColorNumber.Green;
            VerifyEntity(circle, @"
     100       1       0       0       0                        00000000D      1
     100       0       3       1       0                                D      2
100,11.,22.,33.,44.,55.,66.,77.;                                       1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteCompositeCurveTest()
        {
            var curve = new IgesCompositeCurve();
            curve.Entities.Add(new IgesLine(new IgesPoint(11, 22, 33), new IgesPoint(44, 55, 66)));
            curve.Entities.Add(new IgesCircularArc(new IgesPoint(11, 22, 33), new IgesPoint(11, 22, 33), new IgesPoint(11, 22, 33)));
            VerifyEntity(curve, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     100       2       0       0       0                        00000000D      3
     100       0       0       1       0                                D      4
     102       3       0       0       0                        00000000D      5
     102       0       0       1       0                                D      6
110,11.,22.,33.,44.,55.,66.;                                           1P      1
100,33.,11.,22.,11.,22.,11.,22.;                                       3P      2
102,2,1,3;                                                             5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteConicArcTest()
        {
            var arc = IgesConicArc.MakeEllipse(2, 4);
            arc.StartPoint = new IgesPoint(8, 9, 7);
            arc.EndPoint = new IgesPoint(10, 11, 7);
            VerifyEntity(arc, @"
     104       1       0       0       0                        00000000D      1
     104       0       0       1       1                                D      2
104,16.,0.,4.,0.,0.,-64.,7.,8.,9.,10.,11.;                             1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteCopiousDataTest()
        {
            var cd = new IgesCopiousData();
            cd.DataPoints.Add(new IgesPoint(1, 2, 3));
            cd.DataPoints.Add(new IgesPoint(4, 5, 3));
            cd.DataPoints.Add(new IgesPoint(6, 7, 3));
            cd.DataType = IgesCopiousDataType.WitnessLine;
            VerifyEntity(cd, @"
     106       1       0       0       0                        00000100D      1
     106       0       0       1      40                                D      2
106,1,3,3.,1.,2.,4.,5.,6.,7.;                                          1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WritePlaneTest()
        {
            // unbounded
            var plane = new IgesPlane()
            {
                PlaneCoefficientA = 11,
                PlaneCoefficientB = 22,
                PlaneCoefficientC = 33,
                PlaneCoefficientD = 44,
                DisplaySymbolLocation = new IgesPoint(55, 66, 77),
                DisplaySymbolSize = 88,
                Bounding = IgesPlaneBounding.Unbounded
            };
            VerifyEntity(plane, @"
     108       1       0       0       0                        00000000D      1
     108       0       0       1       0                                D      2
108,11.,22.,33.,44.,0,55.,66.,77.,88.;                                 1P      1
");

            // bounded
            plane.Bounding = IgesPlaneBounding.BoundedPositive;
            plane.ClosedCurveBoundingEntity = new IgesCircularArc();
            VerifyEntity(plane, @"
     100       1       0       0       0                        00000000D      1
     100       0       0       1       0                                D      2
     108       2       0       0       0                        00000000D      3
     108       0       0       1       1                                D      4
100,0.,0.,0.,0.,0.,0.,0.;                                              1P      1
108,11.,22.,33.,44.,1,55.,66.,77.,88.;                                 3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteLineTest()
        {
            var line = new IgesLine(new IgesPoint(11, 22, 33), new IgesPoint(44, 55, 66));
            line.Bounding = IgesLineBounding.Unbounded;
            VerifyEntity(line, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       2                                D      2
110,11.,22.,33.,44.,55.,66.;                                           1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteParametricSplineCurveTest()
        {
            var psc = new IgesParametricSplineCurve();
            psc.SplineType = IgesSplineType.BSpline;
            psc.DegreeOfContinuity = 1;
            psc.NumberOfDimensions = 2;
            psc.Segments.Add(new IgesSplinePolynomialSegment()
            {
                BreakPoint = 3.0,
                AX = 4.0,
                BX = 5.0,
                CX = 6.0,
                DX = 7.0,
                AY = 8.0,
                BY = 9.0,
                CY = 10.0,
                DY = 11.0,
                AZ = 12.0,
                BZ = 13.0,
                CZ = 14.0,
                DZ = 15.0,
                XValue = 16.0,
                XFirstDerivative = 17.0,
                XSecondDerivative = 18.0,
                XThirdDerivative = 19.0,
                YValue = 20.0,
                YFirstDerivative = 21.0,
                YSecondDerivative = 22.0,
                YThirdDerivative = 23.0,
                ZValue = 24.0,
                ZFirstDerivative = 25.0,
                ZSecondDerivative = 26.0,
                ZThirdDerivative = 27.0
            });
            VerifyEntity(psc, @"
     112       1       0       0       0                        00000000D      1
     112       0       0       2       0                                D      2
112,6,1,2,1,3.,4.,5.,6.,7.,8.,9.,10.,11.,12.,13.,14.,15.,16.,          1P      1
17.,18.,19.,20.,21.,22.,23.,24.,25.,26.,27.;                           1P      2
");
        }

        [Fact(Skip = "need additional spec"), Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteParametricSplineSurfaceTest()
        {
            throw new NotImplementedException();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteLocationTest()
        {
            VerifyEntity(new IgesLocation(11, 22, 33), @"
     116       1       0       0       0                        00000000D      1
     116       0       0       1       0                                D      2
116,11.,22.,33.;                                                       1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRuledSurfaceTest()
        {
            var ruledSurface = new IgesRuledSurface(new IgesLine(), new IgesLine());
            VerifyEntity(ruledSurface, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     110       2       0       0       0                        00000000D      3
     110       0       0       1       0                                D      4
     118       3       0       0       0                        00000000D      5
     118       0       0       1       0                                D      6
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
110,0.,0.,0.,0.,0.,0.;                                                 3P      2
118,1,3,0,0;                                                           5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSurfaceOfRevolutionTest()
        {
            var surface = new IgesSurfaceOfRevolution()
            {
                AxisOfRevolution = new IgesLine(new IgesPoint(1, 0, 0), IgesPoint.Origin),
                Generatrix = new IgesLine(new IgesPoint(2, 0, 0), IgesPoint.Origin),
                StartAngle = 3,
                EndAngle = 4
            };
            VerifyEntity(surface, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     110       2       0       0       0                        00000000D      3
     110       0       0       1       0                                D      4
     120       3       0       0       0                        00000000D      5
     120       0       0       1       0                                D      6
110,1.,0.,0.,0.,0.,0.;                                                 1P      1
110,2.,0.,0.,0.,0.,0.;                                                 3P      2
120,1,3,3.,4.;                                                         5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTabulatedCylinderTest()
        {
            var tab = new IgesTabulatedCylinder()
            {
                Directrix = new IgesLine(),
                GeneratrixTerminatePoint = new IgesPoint(1, 2, 3)
            };
            VerifyEntity(tab, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     122       2       0       0       0                        00000000D      3
     122       0       0       1       0                                D      4
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
122,1,1.,2.,3.;                                                        3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteDirectionTest()
        {
            VerifyEntity(new IgesDirection(11, 22, 33), @"
     123       1       0       0       0                        00010200D      1
     123       0       0       1       0                                D      2
123,11.,22.,33.;                                                       1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTransformationMatrixTest()
        {
            var matrix = new IgesTransformationMatrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            VerifyEntity(matrix, @"
     124       1       0       0       0                        00000000D      1
     124       0       0       1       0                                D      2
124,1.,2.,3.,4.,5.,6.,7.,8.,9.,10.,11.,12.;                            1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteFlashTest()
        {
            var flash = new IgesFlash()
            {
                XOffset = 1,
                YOffset = 2,
                SizeParameter1 = 3,
                SizeParameter2 = 4,
                RotationAngle = 5,
                AreaType = IgesClosedAreaType.Donut
            };
            VerifyEntity(flash, @"
     125       1       0       0       0                        00000000D      1
     125       0       0       1       3                                D      2
125,1.,2.,3.,4.,5.,0;                                                  1P      1
");

            flash.ReferenceEntity = new IgesCircularArc();
            VerifyEntity(flash, @"
     100       1       0       0       0                        00000000D      1
     100       0       0       1       0                                D      2
     125       2       0       0       0                        00000000D      3
     125       0       0       1       0                                D      4
100,0.,0.,0.,0.,0.,0.,0.;                                              1P      1
125,1.,2.,3.,4.,5.,1;                                                  3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRationalBSplineCurveTest()
        {
            var curve = new IgesRationalBSplineCurve()
            {
                CurveType = IgesSplineCurveType.Custom,
                IsPlanar = true,
                IsClosed = false,
                IsPolynomial = true,
                IsPeriodic = false,
                StartParameter = 0.0,
                EndParameter = 1.0,
                Normal = IgesVector.ZAxis
            };
            curve.KnotValues.AddRange(new[] { 0.0, 0.0, 0.0, 0.0, 0.333333, 0.666667, 1.0, 1.0, 1.0, 1.0 });
            curve.Weights.AddRange(new[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 });
            curve.ControlPoints.AddRange(new[]
            {
                new IgesPoint(-178, 109, 0),
                new IgesPoint(-166, 128, 0),
                new IgesPoint(-144, 109, 0),
                new IgesPoint(-109, 112, 0),
                new IgesPoint(-106, 134, 0),
                new IgesPoint(-119, 138, 0)
            });
            VerifyEntity(curve, @"
     126       1       0       0       0                        00000000D      1
     126       0       0       3       0                                D      2
126,5,3,1,0,1,0,0.,0.,0.,0.,0.333333,0.666667,1.,1.,1.,1.,1.,1.,       1P      1
1.,1.,1.,1.,-178.,109.,0.,-166.,128.,0.,-144.,109.,0.,-109.,           1P      2
112.,0.,-106.,134.,0.,-119.,138.,0.,0.,1.,0.,0.,1.;                    1P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRationalBSplineSurfaceTest()
        {
            var surface = new IgesRationalBSplineSurface()
            {
                IsClosedInFirstParametricVariable = true,
                IsClosedInSecondParametricVariable = true,
                IsPolynomial = true,
                IsPeriodicInFirstParametricVariable = false,
                IsPeriodicInSecondParametricVariable = false,
                FirstParametricStartingValue = 0.0,
                FirstParametricEndingValue = 1.0,
                SecondParametricStartingValue = 0.0,
                SecondParametricEndingValue = 1.0,
                Weights = new double[2, 2] { { 1.0, 2.0 }, { 3.0, 4.0 } },
                ControlPoints = new IgesPoint[2, 2] { { new IgesPoint(5, 6, 0), new IgesPoint(7, 8, 0) }, { new IgesPoint(9, 10, 0), new IgesPoint(11, 12, 0) } }
            };
            surface.FirstKnotValueSequence.AddRange(new[] { -1.0 });
            surface.SecondKnotValueSequence.AddRange(new[] { -2.0 });
            VerifyEntity(surface, @"
     128       1       0       0       0                        00000000D      1
     128       0       0       2       0                                D      2
128,1,1,-2,-2,1,1,1,0,0,-1.,-2.,1.,3.,2.,4.,5.,6.,0.,9.,10.,0.,        1P      1
7.,8.,0.,11.,12.,0.,0.,1.,0.,1.;                                       1P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteOffsetCurveTest()
        {
            var curve = new IgesOffsetCurve()
            {
                CurveToOffset = new IgesLine(),
                DistanceType = IgesOffsetDistanceType.SingleUniformOffset,
                EntityOffsetCurveFunction = null,
                ParameterIndexOfFunctionEntityCurve = 0,
                TaperedOffsetType = IgesTaperedOffsetType.None,
                FirstOffsetDistance = 1.0,
                FirstOffsetDistanceValue = 2.0,
                SecondOffsetDistance = 3.0,
                SecondOffsetDistanceValue = 4.0,
                EntityNormal = IgesVector.ZAxis,
                StartingParameterValue = 5.0,
                EndingParameterValue = 6.0
            };
            VerifyEntity(curve, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     130       2       0       0       0                        00000000D      3
     130       0       0       1       0                                D      4
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
130,1,1,0,0,0,1.,2.,3.,4.,0.,0.,1.,5.,6.;                              3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteConnectPointTest()
        {
            var point = new IgesConnectPoint()
            {
                Location = new IgesPoint(1, 2, 3),
                DisplaySymbolGeometry = null,
                ConnectionType = IgesConnectionType.LogicalPortConnector,
                FunctionType = IgesConnectionFunctionType.ElectricalSignal,
                FunctionIdentifier = "func-id",
                FunctionIdentifierTextDisplayTemplate = null,
                FunctionName = "func-name",
                FunctionNameTextDisplayTemplate = null,
                UniqueIdentifier = 42,
                FunctionCode = IgesConnectionFunctionCode.InvertingOutput,
                ConnectPointMayBeSwapped = true,
                Owner = null
            };
            VerifyEntity(point, @"
     132       1       0       0       0                        00000400D      1
     132       0       0       1       0                                D      2
132,1.,2.,3.,0,102,1,7Hfunc-id,0,9Hfunc-name,0,42,20,0,0;              1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteNodeTest()
        {
            var node = new IgesNode(new IgesPoint(1, 2, 3));
            node.NodeNumber = 17u;
            VerifyEntity(node, @"
     134       1       0       0       0                        00000400D      1
     134       0       0       1       0                              17D      2
134,1.,2.,3.,0;                                                        1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteFiniteElementBeamTest()
        {
            var beam = new IgesBeam(new IgesPoint(1, 2, 3), new IgesPoint(4, 5, 6)) { ElementTypeName = "name" };
            VerifyEntity(beam, @"
     134       1       0       0       0                        00000400D      1
     134       0       0       1       0                                D      2
     134       2       0       0       0                        00000400D      3
     134       0       0       1       0                                D      4
     136       3       0       0       0                        00000000D      5
     136       0       0       1       0                                D      6
134,1.,2.,3.,0;                                                        1P      1
134,4.,5.,6.,0;                                                        3P      2
136,1,2,1,3,4Hname;                                                    5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteCustomFiniteElementTest()
        {
            var custom = new IgesCustomFiniteElement(5002);
            custom.ElementTypeName = "custom";
            custom.Nodes.Add(new IgesNode(new IgesPoint(1, 2, 3)));
            VerifyEntity(custom, @"
     134       1       0       0       0                        00000400D      1
     134       0       0       1       0                                D      2
     136       2       0       0       0                        00000000D      3
     136       0       0       1       0                                D      4
134,1.,2.,3.,0;                                                        1P      1
136,5002,1,1,6Hcustom;                                                 3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteNodalDisplacementAndRotationTest()
        {
            var displacement = new IgesNodalDisplacementAndRotation();
            displacement.NodeAnalyses.Add(new IgesNodalAnalysis(42, new IgesGeneralNote(), new IgesBeam(new IgesPoint(8, 9, 10), new IgesPoint(11, 12, 13)), new[] { new IgesNodalAnalysisCase(new IgesVector(2, 3, 4), 5, 6, 7) }));
            VerifyEntity(displacement, @"
     212       1       0       0       0                        00000100D      1
     212       0       0       1       0                                D      2
     134       2       0       0       0                        00000400D      3
     134       0       0       1       0                                D      4
     134       3       0       0       0                        00000400D      5
     134       0       0       1       0                                D      6
     136       4       0       0       0                        00000000D      7
     136       0       0       1       0                                D      8
     138       5       0       0       0                        00000000D      9
     138       0       0       1       0                                D     10
212,0;                                                                 1P      1
134,8.,9.,10.,0;                                                       3P      2
134,11.,12.,13.,0;                                                     5P      3
136,1,2,3,5,;                                                          7P      4
138,1,1,1,42,7,2.,3.,4.,5.,6.,7.;                                      9P      5
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteOffsetSurfaceTest()
        {
            var offset = new IgesOffsetSurface(new IgesVector(1, 2, 3), 4, new IgesRuledSurface());
            VerifyEntity(offset, @"
     118       1       0       0       0                        00000000D      1
     118       0       0       1       0                                D      2
     140       2       0       0       0                        00000000D      3
     140       0       0       1       0                                D      4
118,0,0,0,0;                                                           1P      1
140,1.,2.,3.,4.,1;                                                     3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTrimmedParametricSurfaceTest()
        {
            var surface = new IgesTrimmedParametricSurface()
            {
                Surface = new IgesRuledSurface(),
                IsOuterBoundaryD = false,
                OuterBoundary = new IgesRuledSurface(),
            };
            surface.BoundaryEntities.Add(new IgesRuledSurface());
            VerifyEntity(surface, @"
     118       1       0       0       0                        00000000D      1
     118       0       0       1       0                                D      2
     118       2       0       0       0                        00000000D      3
     118       0       0       1       0                                D      4
     118       3       0       0       0                        00000000D      5
     118       0       0       1       0                                D      6
     144       4       0       0       0                        00000000D      7
     144       0       0       1       0                                D      8
118,0,0,0,0;                                                           1P      1
118,0,0,0,0;                                                           3P      2
118,0,0,0,0;                                                           5P      3
144,1,1,1,3,5;                                                         7P      4
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteNodalResultsTest()
        {
            var nodalResults = new IgesNodalResults();
            nodalResults.AnalysisTime = new DateTime(2000, 1, 1, 0, 0, 0);
            nodalResults.ResultsType = IgesResultType.Temperature;
            var result = new IgesNodalResult();
            result.Values.Add(42);
            nodalResults.Results.Add(result);
            VerifyEntity(nodalResults, @"
     146       1       0       0       0                        00000000D      1
     146       0       0       1       1                                D      2
146,0,0,18H15H20000101.000000,1,1,0,0,42.;                             1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteElementResultsTest()
        {
            var elementResults = new IgesElementResults();
            elementResults.ReportingType = IgesResultsReportingType.ElementCentroid;
            VerifyEntity(elementResults, @"
     148       1       0       0       0                        00000000D      1
     148       0       0       1       0                                D      2
148,0,0,15H00010101.000000,0,1,0;                                      1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteBlockTest()
        {
            var block = new IgesBlock();
            block.XLength = 1.0;
            block.YLength = 2.0;
            block.ZLength = 3.0;
            block.Corner = new IgesPoint(4.0, 5.0, 6.0);
            VerifyEntity(block, @"
     150       1       0       0       0                        00000000D      1
     150       0       0       1       0                                D      2
150,1.,2.,3.,4.,5.,6.,1.,0.,0.,0.,0.,1.;                               1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRightAngularWedgeTest()
        {
            var wedge = new IgesRightAngularWedge()
            {
                XAxisSize = 1.0,
                YAxisSize = 2.0,
                ZAxisSize = 3.0
            };
            VerifyEntity(wedge, @"
     152       1       0       0       0                        00000000D      1
     152       0       0       1       0                                D      2
152,1.,2.,3.,0.,0.,0.,0.,1.,0.,0.,0.,0.,1.;                            1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRightCircularCylinderTest()
        {
            var cylinder = new IgesRightCircularCylinder()
            {
                Height = 1.0,
                Radius = 2.0
            };
            VerifyEntity(cylinder, @"
     154       1       0       0       0                        00000000D      1
     154       0       0       1       0                                D      2
154,1.,2.,0.,0.,0.,0.,0.,1.;                                           1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRightCircularConeFrustrumTest()
        {
            var cone = new IgesRightCircularConeFrustrum()
            {
                Height = 1.0,
                LargeFaceRadius = 2.0,
                SmallFaceRadius = 3.0
            };
            VerifyEntity(cone, @"
     156       1       0       0       0                        00000000D      1
     156       0       0       1       0                                D      2
156,1.,2.,3.,0.,0.,0.,0.,0.,1.;                                        1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSphereTest()
        {
            // regular case
            var sphere = new IgesSphere(11, new IgesPoint(22, 33, 44));
            VerifyEntity(sphere, @"
     158       1       0       0       0                        00000000D      1
     158       0       0       1       0                                D      2
158,11.,22.,33.,44.;                                                   1P      1
");

            // default center
            VerifyEntity(new IgesSphere() { Radius = 1 }, @"
     158       1       0       0       0                        00000000D      1
     158       0       0       1       0                                D      2
158,1.;                                                                1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTorusTest()
        {
            // regular case
            var torus = new IgesTorus(11, 22, new IgesPoint(33, 44, 55), new IgesVector(66, 77, 88));
            VerifyEntity(torus, @"
     160       1       0       0       0                        00000000D      1
     160       0       0       1       0                                D      2
160,11.,22.,33.,44.,55.,66.,77.,88.;                                   1P      1
");

            // default values; minimal data written
            VerifyEntity(new IgesTorus(), @"
     160       1       0       0       0                        00000000D      1
     160       0       0       1       0                                D      2
160,0.,0.;                                                             1P      1
");

            // non-default center; only center written
            VerifyEntity(new IgesTorus() { Center = new IgesPoint(1, 2, 3) }, @"
     160       1       0       0       0                        00000000D      1
     160       0       0       1       0                                D      2
160,0.,0.,1.,2.,3.;                                                    1P      1
");

            // non-default normal
            VerifyEntity(new IgesTorus() { Normal = new IgesVector(1, 2, 3) }, @"
     160       1       0       0       0                        00000000D      1
     160       0       0       1       0                                D      2
160,0.,0.,0.,0.,0.,1.,2.,3.;                                           1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSolidOfRevolutionTest()
        {
            var solid = new IgesSolidOfRevolution()
            {
                Curve = new IgesLine()
            };
            VerifyEntity(solid, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     162       2       0       0       0                        00000000D      3
     162       0       0       1       0                                D      4
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
162,1,1.,0.,0.,0.,0.,0.,1.;                                            3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSolidOfLinearExtrusionTest()
        {
            var solid = new IgesSolidOfLinearExtrusion()
            {
                Curve = new IgesCircularArc(),
                ExtrusionLength = 3.0
            };
            VerifyEntity(solid, @"
     100       1       0       0       0                        00000000D      1
     100       0       0       1       0                                D      2
     164       2       0       0       0                        00000000D      3
     164       0       0       1       0                                D      4
100,0.,0.,0.,0.,0.,0.,0.;                                              1P      1
164,1,3.,0.,0.,1.;                                                     3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteEllipsoidTest()
        {
            var el = new IgesEllipsoid()
            {
                XAxisLength = 1.0,
                YAxisLength = 2.0,
                ZAxisLength = 3.0,
                Center = new IgesPoint(4.0, 5.0, 6.0),
                XAxis = new IgesVector(7.0, 8.0, 9.0),
                ZAxis = new IgesVector(10.0, 11.0, 12.0)
            };
            VerifyEntity(el, @"
     168       1       0       0       0                        00000000D      1
     168       0       0       1       0                                D      2
168,1.,2.,3.,4.,5.,6.,7.,8.,9.,10.,11.,12.;                            1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteBooleanTreeTest()
        {
            //       U
            //    /     \
            //   -       I
            //  / \     / \
            // A   U   D   E
            //    / \
            //   B   C
            var node = new IgesBooleanTreeOperation(
                IgesBooleanTreeOperationKind.Union,
                new IgesBooleanTreeOperation(
                    IgesBooleanTreeOperationKind.Difference,
                    new IgesBooleanTreeEntity(new IgesLine() { EntityLabel = "A" }),
                    new IgesBooleanTreeOperation(
                        IgesBooleanTreeOperationKind.Union,
                        new IgesBooleanTreeEntity(new IgesLine() { EntityLabel = "B" }),
                        new IgesBooleanTreeEntity(new IgesLine() { EntityLabel = "C" }))),
                new IgesBooleanTreeOperation(
                    IgesBooleanTreeOperationKind.Intersection,
                    new IgesBooleanTreeEntity(new IgesLine() { EntityLabel = "D" }),
                    new IgesBooleanTreeEntity(new IgesLine() { EntityLabel = "E" })));
            var tree = new IgesBooleanTree(node);
            VerifyEntity(tree, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                       A        D      2
     110       2       0       0       0                        00000000D      3
     110       0       0       1       0                       B        D      4
     110       3       0       0       0                        00000000D      5
     110       0       0       1       0                       C        D      6
     110       4       0       0       0                        00000000D      7
     110       0       0       1       0                       D        D      8
     110       5       0       0       0                        00000000D      9
     110       0       0       1       0                       E        D     10
     180       6       0       0       0                        00000000D     11
     180       0       0       1       0                                D     12
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
110,0.,0.,0.,0.,0.,0.;                                                 3P      2
110,0.,0.,0.,0.,0.,0.;                                                 5P      3
110,0.,0.,0.,0.,0.,0.;                                                 7P      4
110,0.,0.,0.,0.,0.,0.;                                                 9P      5
180,9,-1,-3,-5,1,3,-7,-9,2,1;                                         11P      6
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSelectedComponentTest()
        {
            var selected = new IgesSelectedComponent(new IgesBooleanTree(), new IgesPoint(1.0, 2.0, 3.0));
            VerifyEntity(selected, @"
     180       1       0       0       0                        00000000D      1
     180       0       0       1       0                                D      2
     182       2       0       0       0                        00000300D      3
     182       0       0       1       0                                D      4
180,0;                                                                 1P      1
182,1,1.,2.,3.;                                                        3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSolidAssemblyTest()
        {
            var solid = new IgesSolidAssembly();
            solid.Solids.Add(new IgesSolidAndTransformationMatrix(new IgesLine(), IgesTransformationMatrix.Identity));
            VerifyEntity(solid, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     124       2       0       0       0                        00000000D      3
     124       0       0       1       0                                D      4
     184       3       0       0       0                        00000200D      5
     184       0       0       1       0                                D      6
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
124,1.,0.,0.,0.,0.,1.,0.,0.,0.,0.,1.,0.;                               3P      2
184,1,1,3;                                                             5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteManifestSolidBRepObjectTest()
        {
            var solid = new IgesManifestSolidBRepObject();
            solid.Shell = new IgesLine();
            solid.IsOriented = true;
            solid.Voids.Add(new IgesManifestSolidBRepVoid(new IgesCircularArc(), true));
            VerifyEntity(solid, @"
     110       1       0       0       0                        00000000D      1
     110       0       0       1       0                                D      2
     100       2       0       0       0                        00000000D      3
     100       0       0       1       0                                D      4
     186       3       0       0       0                        00000000D      5
     186       0       0       1       0                                D      6
110,0.,0.,0.,0.,0.,0.;                                                 1P      1
100,0.,0.,0.,0.,0.,0.,0.;                                              3P      2
186,1,1,1,3,1;                                                         5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WritePlaneSurfaceTest()
        {
            var plane = new IgesPlaneSurface()
            {
                Point = new IgesLocation(1.0, 2.0, 3.0),
                Normal = new IgesDirection(0.0, 0.0, 1.0),
                ReferenceDirection = new IgesDirection(1.0, 0.0, 0.0),
                IsParameterized = true,
            };
            VerifyEntity(plane, @"
     116       1       0       0       0                        00000000D      1
     116       0       0       1       0                                D      2
     123       2       0       0       0                        00010200D      3
     123       0       0       1       0                                D      4
     123       3       0       0       0                        00010200D      5
     123       0       0       1       0                                D      6
     190       4       0       0       0                        00000000D      7
     190       0       0       1       1                                D      8
116,1.,2.,3.;                                                          1P      1
123,0.,0.,1.;                                                          3P      2
123,1.,0.,0.;                                                          5P      3
190,1,3,5;                                                             7P      4
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRightCircularCylindricalSurfaceTest()
        {
            var cylinder = new IgesRightCircularCylindricalSurface()
            {
                Point = new IgesLocation(1.0, 2.0, 3.0),
                AxisDirection = new IgesDirection(0.0, 0.0, 1.0),
                Radius = 13.0,
                ReferenceDirection = new IgesDirection(1.0, 0.0, 0.0),
                IsParameterized = true,
            };
            VerifyEntity(cylinder, @"
     116       1       0       0       0                        00000000D      1
     116       0       0       1       0                                D      2
     123       2       0       0       0                        00010200D      3
     123       0       0       1       0                                D      4
     123       3       0       0       0                        00010200D      5
     123       0       0       1       0                                D      6
     192       4       0       0       0                        00010000D      7
     192       0       0       1       1                                D      8
116,1.,2.,3.;                                                          1P      1
123,0.,0.,1.;                                                          3P      2
123,1.,0.,0.;                                                          5P      3
192,1,3,13.,5;                                                         7P      4
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRightCircularConicalSurfaceTest()
        {
            var cone = new IgesRightCircularConicalSurface()
            {
                Point = new IgesLocation(1.0, 2.0, 3.0),
                AxisDirection = new IgesDirection(0.0, 0.0, 1.0),
                Radius = 13.0,
                SemiAngle = 45.0,
                ReferenceDirection = new IgesDirection(1.0, 0.0, 0.0),
                IsParameterized = true,
            };
            VerifyEntity(cone, @"
     116       1       0       0       0                        00000000D      1
     116       0       0       1       0                                D      2
     123       2       0       0       0                        00010200D      3
     123       0       0       1       0                                D      4
     123       3       0       0       0                        00010200D      5
     123       0       0       1       0                                D      6
     194       4       0       0       0                        00010000D      7
     194       0       0       1       1                                D      8
116,1.,2.,3.;                                                          1P      1
123,0.,0.,1.;                                                          3P      2
123,1.,0.,0.;                                                          5P      3
194,1,3,13.,45.,5;                                                     7P      4
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSphericalSurfaceTest()
        {
            var sphere = new IgesSphericalSurface()
            {
                Center = new IgesLocation(1.0, 2.0, 3.0),
                Radius = 13.0,
                AxisDirection = new IgesDirection(0.0, 0.0, 1.0),
                ReferenceDirection = new IgesDirection(1.0, 0.0, 0.0),
                IsParameterized = true,
            };
            VerifyEntity(sphere, @"
     116       1       0       0       0                        00000000D      1
     116       0       0       1       0                                D      2
     123       2       0       0       0                        00010200D      3
     123       0       0       1       0                                D      4
     123       3       0       0       0                        00010200D      5
     123       0       0       1       0                                D      6
     196       4       0       0       0                        00010000D      7
     196       0       0       1       1                                D      8
116,1.,2.,3.;                                                          1P      1
123,0.,0.,1.;                                                          3P      2
123,1.,0.,0.;                                                          5P      3
196,1,13.,3,5;                                                         7P      4
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteToroidalSurfaceTest()
        {
            var torus = new IgesToroidalSurface()
            {
                Center = new IgesLocation(1.0, 2.0, 3.0),
                AxisDirection = new IgesDirection(0.0, 0.0, 1.0),
                MajorRadius = 12.0,
                MinorRadius = 6.0,
                ReferenceDirection = new IgesDirection(1.0, 0.0, 0.0),
                IsParameterized = true,
            };
            VerifyEntity(torus, @"
     116       1       0       0       0                        00000000D      1
     116       0       0       1       0                                D      2
     123       2       0       0       0                        00010200D      3
     123       0       0       1       0                                D      4
     123       3       0       0       0                        00010200D      5
     123       0       0       1       0                                D      6
     198       4       0       0       0                        00010000D      7
     198       0       0       1       1                                D      8
116,1.,2.,3.;                                                          1P      1
123,0.,0.,1.;                                                          3P      2
123,1.,0.,0.;                                                          5P      3
198,1,3,12.,6.,5;                                                      7P      4
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteAngularDimensionTest()
        {
            var angular = new IgesAngularDimension();
            VerifyEntity(angular, @"
     202       1       0       0       0                        00000100D      1
     202       0       0       1       0                                D      2
202,0,0,0,0.,0.,0.,0,0;                                                1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteCurveDimensionTest()
        {
            var curve = new IgesCurveDimension();
            VerifyEntity(curve, @"
     204       1       0       0       0                        00000100D      1
     204       0       0       1       0                                D      2
204,0,0,0,0,0,0,0;                                                     1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteDiameterDimensionTest()
        {
            var diameter = new IgesDiameterDimension();
            VerifyEntity(diameter, @"
     206       1       0       0       0                        00000100D      1
     206       0       0       1       0                                D      2
206,0,0,0,0.,0.;                                                       1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteFlagNoteTest()
        {
            var flag = new IgesFlagNote()
            {
                Location = new IgesPoint(1.0, 2.0, 3.0),
                RotationAngle = 0.1,
                GeneralNote = new IgesGeneralNote()
            };
            flag.Leaders.Add(new IgesLeader());
            VerifyEntity(flag, @"
     212       1       0       0       0                        00000100D      1
     212       0       0       1       0                                D      2
     214       2       0       0       0                        00000100D      3
     214       0       0       1       1                                D      4
     208       3       0       0       0                        00000100D      5
     208       0       0       1       0                                D      6
212,0;                                                                 1P      1
214,0,0.,0.,0.,0.,0.;                                                  3P      2
208,1.,2.,3.,0.1,1,1,3;                                                5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteGeneralLabelTest()
        {
            var label = new IgesGeneralLabel()
            {
                GeneralNote = new IgesGeneralNote()
            };
            label.Leaders.Add(new IgesLeader());
            VerifyEntity(label, @"
     212       1       0       0       0                        00000100D      1
     212       0       0       1       0                                D      2
     214       2       0       0       0                        00000100D      3
     214       0       0       1       1                                D      4
     210       3       0       0       0                        00000100D      5
     210       0       0       1       0                                D      6
212,0;                                                                 1P      1
214,0,0.,0.,0.,0.,0.;                                                  3P      2
210,1,1,3;                                                             5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteGeneralNoteTest()
        {
            // regular case with font code
            var note = new IgesGeneralNote();
            note.NoteType = IgesGeneralNoteType.MultipleStackLeftJustified;
            var str = new IgesTextString();
            str.BoxWidth = 1.0;
            str.BoxHeight = 2.0;
            str.FontCode = 3;
            str.SlantAngle = 4.0;
            str.RotationAngle = 5.0;
            str.MirroringAxis = IgesTextMirroringAxis.PerpendicularToTextBase;
            str.RotationType = IgesTextRotationType.Vertical;
            str.Location = new IgesPoint(6.0, 7.0, 8.0);
            str.Value = "test string";
            note.Strings.Add(str);
            VerifyEntity(note, @"
     212       1       0       0       0                        00000100D      1
     212       0       0       1       6                                D      2
212,1,11,1.,2.,3,4.,5.,1,1,6.,7.,8.,11Htest string;                    1P      1
");

            // with text font definition pointer
            str.TextFontDefinition = new IgesTextFontDefinition();
            VerifyEntity(note, @"
     310       1       0       0       0                        00000200D      1
     310       0       0       1       0                                D      2
     212       2       0       0       0                        00000100D      3
     212       0       0       1       6                                D      4
310,0,,,0,0;                                                           1P      1
212,1,11,1.,2.,-1,4.,5.,1,1,6.,7.,8.,11Htest string;                   3P      2
");

            // default values
            note = new IgesGeneralNote();
            VerifyEntity(note, @"
     212       1       0       0       0                        00000100D      1
     212       0       0       1       0                                D      2
212,0;                                                                 1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteNewGeneralNoteTest()
        {
            var note = new IgesNewGeneralNote()
            {
                TextContainmentAreaWidth = 1.0,
                TextContainmentAreaHeight = 2.0,
                TextJustification = IgesTextJustification.Center,
                TextContainmentAreaLocation = default(IgesPoint),
                TextContainmentAreaRotation = 3.0,
                FirstBaseLineLocation = new IgesPoint(4.0, 5.0, 6.0),
                NormalInterLineSpacing = 7.0
            };
            note.Strings.Add(new IgesNewTextString()
            {
                CharacterDisplay = IgesCharacterDisplay.Variable,
                CharacterWidth = 11.0,
                CharacterHeight = 12.0,
                InterCharacterSpacing = 13.0,
                InterLineSpacing = 14.0,
                FontStyle = IgesFontStyle.Futura,
                CharacterAngle = 15.0,
                ControlCode = null,
                BoxWidth = 16.0,
                BoxHeight = 17.0,
                FontCode = 3,
                SlantAngle = 18.0,
                RotationAngle = 19.0,
                MirroringAxis = IgesTextMirroringAxis.None,
                RotationType = IgesTextRotationType.Horizontal,
                Location = new IgesPoint(20.0, 21.0, 22.0),
                Value = "foo"
            });
            VerifyEntity(note, @"
     213       1       0       0       0                        00000100D      1
     213       0       0       2       0                                D      2
213,1.,2.,2,0.,0.,0.,3.,4.,5.,6.,7.,1,1,11.,12.,13.,14.,3,15.,,        1P      1
3,16.,17.,3,18.,19.,0,0,20.,21.,22.,3Hfoo;                             1P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteLinearDimensionTest()
        {
            var linear = new IgesLinearDimension();
            VerifyEntity(linear, @"
     216       1       0       0       0                        00000100D      1
     216       0       0       1       0                                D      2
216,0,0,0,0,0;                                                         1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteOrdinateDimensionTest()
        {
            var ord = new IgesOrdinateDimension();
            VerifyEntity(ord, @"
     218       1       0       0       0                        00000100D      1
     218       0       0       1       0                                D      2
218,0,0;                                                               1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WritePointDimensionTest()
        {
            var point = new IgesPointDimension();
            VerifyEntity(point, @"
     220       1       0       0       0                        00000100D      1
     220       0       0       1       0                                D      2
220,0,0,0;                                                             1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteRadiusDimensinoTest()
        {
            var rad = new IgesRadiusDimension();
            VerifyEntity(rad, @"
     222       1       0       0       0                        00000100D      1
     222       0       0       1       0                                D      2
222,0,0,0.,0.;                                                         1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteGeneralSymbolTest()
        {
            var symbol = new IgesGeneralSymbol(
                new IgesGeneralNote(),
                new IgesLeader[0], // leaders
                new[] { new IgesLine() }); // geometries
            VerifyEntity(symbol, @"
     212       1       0       0       0                        00000100D      1
     212       0       0       1       0                                D      2
     110       2       0       0       0                        00000000D      3
     110       0       0       1       0                                D      4
     228       3       0       0       0                        00000100D      5
     228       0       0       1       0                                D      6
212,0;                                                                 1P      1
110,0.,0.,0.,0.,0.,0.;                                                 3P      2
228,1,1,3,0;                                                           5P      3
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTextDisplayTemplateTest()
        {
            // regular case with font code
            var tdt = new IgesTextDisplayTemplate();
            tdt.CharacterBoxWidth = 1.0;
            tdt.CharacterBoxHeight = 2.0;
            tdt.FontCode = 3;
            tdt.SlantAngle = 4.0;
            tdt.RotationAngle = 5.0;
            tdt.MirroringAxis = IgesTextMirroringAxis.PerpendicularToTextBase;
            tdt.RotationType = IgesTextRotationType.Vertical;
            tdt.LocationOrOffset = new IgesVector(6, 7, 8);
            VerifyEntity(tdt, @"
     312       1       0       0       0                        00000200D      1
     312       0       0       1       0                                D      2
312,1.,2.,3,4.,5.,1,1,6.,7.,8.;                                        1P      1
");

            // with text font definition pointer and incremental display
            tdt.TextFontDefinition = new IgesTextFontDefinition();
            tdt.IsIncrementalDisplayTemplate = true;
            VerifyEntity(tdt, @"
     310       1       0       0       0                        00000200D      1
     310       0       0       1       0                                D      2
     312       2       0       0       0                        00000200D      3
     312       0       0       1       1                                D      4
310,0,,,0,0;                                                           1P      1
312,1.,2.,-1,4.,5.,1,1,6.,7.,8.;                                       3P      2
");

            // default values
            tdt = new IgesTextDisplayTemplate();
            VerifyEntity(tdt, @"
     312       1       0       0       0                        00000200D      1
     312       0       0       1       0                                D      2
312,0.,0.,1,0.,0.,0,0,0.,0.,0.;                                        1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteLeaderTest()
        {
            // regular case
            var leader = new IgesLeader()
            {
                ArrowheadCoordinates = new IgesPoint(1, 2, 3),
                ArrowHeight = 8.0,
                ArrowWidth = 9.0,
                ArrowType = IgesArrowType.FilledCircle
            };
            leader.LineSegments.Add(new IgesPoint(4, 5, 3));
            leader.LineSegments.Add(new IgesPoint(6, 7, 3));
            VerifyEntity(leader, @"
     214       1       0       0       0                        00000100D      1
     214       0       0       1       6                                D      2
214,2,8.,9.,3.,1.,2.,4.,5.,6.,7.;                                      1P      1
");

            // default values
            leader = new IgesLeader();
            VerifyEntity(leader, @"
     214       1       0       0       0                        00000100D      1
     214       0       0       1       1                                D      2
214,0,0.,0.,0.,0.,0.;                                                  1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTemplateLineFontDefinitionTest()
        {
            // regular case
            var sfd = new IgesSubfigureDefinition();
            sfd.Entities.Add(new IgesNull());
            var lfd = new IgesTemplateLineFontDefinition(sfd, 1.0, 2.0);
            VerifyEntity(lfd, @"
       0       1       0       0       0                        00000000D      1
       0       0       0       1       0                                D      2
     308       2       0       0       0                        00000200D      3
     308       0       0       1       0                                D      4
     304       3       0       0       0                        00000200D      5
     304       0       0       1       1                                D      6
0;                                                                     1P      1
308,0,,1,1;                                                            3P      2
304,0,3,1.,2.;                                                         5P      3
");

            // default values
            lfd = new IgesTemplateLineFontDefinition();
            VerifyEntity(lfd, @"
     308       1       0       0       0                        00000200D      1
     308       0       0       1       0                                D      2
     304       2       0       0       0                        00000200D      3
     304       0       0       1       1                                D      4
308,0,,0;                                                              1P      1
304,0,1,0.,0.;                                                         3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WritePatternLineFontDefinitionTest()
        {
            // regular case
            var lfd = new IgesPatternLineFontDefinition();
            lfd.SegmentLengths.Add(1);
            lfd.SegmentLengths.Add(2);
            lfd.DisplayMask = 0x34;
            VerifyEntity(lfd, @"
     304       1       0       0       0                        00000200D      1
     304       0       0       1       2                                D      2
304,2,1.,2.,2H34;                                                      1P      1
");

            // default values
            lfd = new IgesPatternLineFontDefinition();
            VerifyEntity(lfd, @"
     304       1       0       0       0                        00000200D      1
     304       0       0       1       2                                D      2
304,0,1H0;                                                             1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteSubfigureEntityTest()
        {
            var trans = new IgesTransformationMatrix()
            {
                R11 = 1.0,
                R12 = 2.0,
                R13 = 3.0,
                T1 = 4.0,
                R21 = 5.0,
                R22 = 6.0,
                R23 = 7.0,
                T2 = 8.0,
                R31 = 9.0,
                R32 = 10.0,
                R33 = 11.0,
                T3 = 12.0
            };
            var sub = new IgesSubfigureDefinition();
            sub.Name = "this is a really long string that should span many lines because it is so huge";
            sub.Entities.Add(new IgesLine() { P1 = new IgesPoint(1, 2, 3), P2 = new IgesPoint(4, 5, 6), TransformationMatrix = trans });
            sub.Entities.Add(new IgesLine() { P1 = new IgesPoint(7, 8, 9), P2 = new IgesPoint(10, 11, 12) });
            var file = new IgesFile();
            file.Entities.Add(sub);
            VerifyFileContains(file, @"
     124       1       0       0       0                        00000000D      1
     124       0       0       1       0                                D      2
     110       2       0       0       0               1        00000000D      3
     110       0       0       1       0                                D      4
     110       3       0       0       0                        00000000D      5
     110       0       0       1       0                                D      6
     308       4       0       0       0                        00000200D      7
     308       0       0       2       0                                D      8
124,1.,2.,3.,4.,5.,6.,7.,8.,9.,10.,11.,12.;                            1P      1
110,1.,2.,3.,4.,5.,6.;                                                 3P      2
110,7.,8.,9.,10.,11.,12.;                                              5P      3
308,0,78Hthis is a really long string that should span many line       7P      4
s because it is so huge,2,3,5;                                         7P      5
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteTextFontDefinitionTest()
        {
            // regular case
            var tfd = new IgesTextFontDefinition();
            tfd.FontCode = 1;
            tfd.Name = "STANDARD";

            tfd.Scale = 8;
            var character = new IgesTextFontDefinitionCharacter();
            character.ASCIICode = 65;
            character.CharacterOrigin = new IgesGridPoint(11, 0);
            character.CharacterMovements.Add(new IgesTextFontDefinitionCharacterMovement() { IsUp = false, Location = new IgesGridPoint(4, 8) });
            character.CharacterMovements.Add(new IgesTextFontDefinitionCharacterMovement() { IsUp = false, Location = new IgesGridPoint(8, 0) });
            character.CharacterMovements.Add(new IgesTextFontDefinitionCharacterMovement() { IsUp = true, Location = new IgesGridPoint(2, 4) });
            character.CharacterMovements.Add(new IgesTextFontDefinitionCharacterMovement() { IsUp = false, Location = new IgesGridPoint(6, 4) });
            tfd.Characters.Add(character);
            VerifyEntity(tfd, @"
     310       1       0       0       0                        00000200D      1
     310       0       0       1       0                                D      2
310,1,8HSTANDARD,,8,1,65,11,0,4,,4,8,,8,0,1,2,4,,6,4;                  1P      1
");

            // with supercedes value
            tfd.SupercedesCode = 42;
            VerifyEntity(tfd, @"
     310       1       0       0       0                        00000200D      1
     310       0       0       1       0                                D      2
310,1,8HSTANDARD,42,8,1,65,11,0,4,,4,8,,8,0,1,2,4,,6,4;                1P      1
");

            // with supercedes pointer
            tfd.SupercedesFont = new IgesTextFontDefinition();
            VerifyEntity(tfd, @"
     310       1       0       0       0                        00000200D      1
     310       0       0       1       0                                D      2
     310       2       0       0       0                        00000200D      3
     310       0       0       1       0                                D      4
310,0,,,0,0;                                                           1P      1
310,1,8HSTANDARD,-1,8,1,65,11,0,4,,4,8,,8,0,1,2,4,,6,4;                3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteColorDefinitionTest()
        {
            // regular case
            var color = new IgesColorDefinition(11, 22, 33, "name");
            VerifyEntity(color, @"
     314       1       0       0       0                        00000200D      1
     314       0       0       1       0                                D      2
314,11.,22.,33.,4Hname;                                                1P      1
");

            // default values
            color = new IgesColorDefinition();
            VerifyEntity(color, @"
     314       1       0       0       0                        00000200D      1
     314       0       0       1       0                                D      2
314,1.,1.,1.,;                                                         1P      1
");

            // line with custom color
            var line = new IgesLine() { CustomColor = new IgesColorDefinition(11, 22, 33, "name") };
            VerifyEntity(line, @"
     314       1       0       0       0                        00000200D      1
     314       0       0       1       0                                D      2
     110       2       0       0       0                        00000000D      3
     110       0      -1       1       0                                D      4
314,11.,22.,33.,4Hname;                                                1P      1
110,0.,0.,0.,0.,0.,0.;                                                 3P      2
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteLabelDisplayAssociativityTest()
        {
            // regular values
            var disp = new IgesLabelDisplayAssociativity();
            disp.LabelPlacements.Add(new IgesLabelPlacement(new IgesPerspectiveView(), new IgesPoint(1, 2, 3), new IgesLeader(), 7, new IgesLine()));
            VerifyEntity(disp, @"
     410       1       0       0       0                        00000100D      1
     410       0       0       2       1                                D      2
     214       3       0       0       0                        00000100D      3
     214       0       0       1       1                                D      4
     110       4       0       0       0                        00000000D      5
     110       0       0       1       0                                D      6
     402       5       0       0       0                        00000000D      7
     402       0       0       1       5                                D      8
410,0,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0,         1P      1
0.,0.;                                                                 1P      2
214,0,0.,0.,0.,0.,0.;                                                  3P      3
110,0.,0.,0.,0.,0.,0.;                                                 5P      4
402,1,1,1.,2.,3.,3,7,5;                                                7P      5
");

            // default values
            disp = new IgesLabelDisplayAssociativity();
            VerifyEntity(disp, @"
     402       1       0       0       0                        00000000D      1
     402       0       0       1       5                                D      2
402,0;                                                                 1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WriteViewTest()
        {
            // regular values
            var view = new IgesView(
                1,
                2,
                new IgesPlane() { PlaneCoefficientA = 3 },
                new IgesPlane() { PlaneCoefficientA = 4 },
                new IgesPlane() { PlaneCoefficientA = 5 },
                new IgesPlane() { PlaneCoefficientA = 6 },
                new IgesPlane() { PlaneCoefficientA = 7 },
                new IgesPlane() { PlaneCoefficientA = 8 });
            VerifyEntity(view, @"
     108       1       0       0       0                        00000000D      1
     108       0       0       1       0                                D      2
     108       2       0       0       0                        00000000D      3
     108       0       0       1       0                                D      4
     108       3       0       0       0                        00000000D      5
     108       0       0       1       0                                D      6
     108       4       0       0       0                        00000000D      7
     108       0       0       1       0                                D      8
     108       5       0       0       0                        00000000D      9
     108       0       0       1       0                                D     10
     108       6       0       0       0                        00000000D     11
     108       0       0       1       0                                D     12
     410       7       0       0       0                        00000100D     13
     410       0       0       1       0                                D     14
108,3.,0.,0.,0.,0,0.,0.,0.,0.;                                         1P      1
108,4.,0.,0.,0.,0,0.,0.,0.,0.;                                         3P      2
108,5.,0.,0.,0.,0,0.,0.,0.,0.;                                         5P      3
108,6.,0.,0.,0.,0,0.,0.,0.,0.;                                         7P      4
108,7.,0.,0.,0.,0,0.,0.,0.,0.;                                         9P      5
108,8.,0.,0.,0.,0,0.,0.,0.,0.;                                        11P      6
410,1,2.,1,3,5,7,9,11;                                                13P      7
");

            // default values
            view = new IgesView();
            VerifyEntity(view, @"
     410       1       0       0       0                        00000100D      1
     410       0       0       1       0                                D      2
410,0,0.,0,0,0,0,0,0;                                                  1P      1
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Writing)]
        public void WritePerspectiveViewTest()
        {
            // regular values
            var view = new IgesPerspectiveView(
                1,
                2,
                new IgesVector(3, 0, 0),
                new IgesPoint(4, 0, 0),
                new IgesPoint(5, 0, 0),
                new IgesVector(6, 0, 0),
                7,
                8,
                9,
                10,
                11,
                IgesDepthClipping.BackClipping,
                12,
                13);
            VerifyEntity(view, @"
     410       1       0       0       0                        00000100D      1
     410       0       0       2       1                                D      2
410,1,2.,3.,0.,0.,4.,0.,0.,5.,0.,0.,6.,0.,0.,7.,8.,9.,10.,11.,1,       1P      1
12.,13.;                                                               1P      2
");

            // default values
            view = new IgesPerspectiveView();
            VerifyEntity(view, @"
     410       1       0       0       0                        00000100D      1
     410       0       0       2       1                                D      2
410,0,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0.,0,         1P      1
0.,0.;                                                                 1P      2
");
        }
    }
}
