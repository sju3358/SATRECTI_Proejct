#region System Namespace
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

#endregion


#region KML Namespace
using SI.SMUDI.Cmm.Utils.Base;
using SI.SMUDI.Cmm.Utils.Dom;
using SI.SMUDI.Cmm.Utils.Engine;
#endregion


#region SMUDI Namespace
using SMUDI.Scene.Layers;
using SMUDI.Scene.Renderables.Label;
using SMUDI.Scene.Renderables.Symbol;
using SMUDI.Scene.Renderables.Line;
using SMUDI.Scene.Renderables.Polygon;
using SMUDI.Scene.Renderables;
using System.Drawing;
using System.Drawing.Imaging;
#endregion

namespace Module.Services
{
    /// <summary>
    /// SMUDI Layer객체를 파싱하여 KMZ파일을 만드는 클래스
    /// </summary>
    public class ExportKmzService : DisposeService
    {

        private bool _disposed;
        private int _fileNumber;
        private string _basePath;

        #region Constructor
        private static ExportKmzService _instance;
        public static ExportKmzService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ExportKmzService();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        private ExportKmzService()
        {
            _fileNumber = 1;
            _basePath = null;
        }
        #endregion


        /// <summary>
        /// Layer객체를 파싱하여 KMZ파일을 EXPORT합니다 기본위치는 Working Directory / Export / 에 추출된 파일이 생성됩니다.
        /// Layer가 참조하고있는 리소스파일의 위치가 Working Directory가 아닐경우, basePath를 지정해줘야합니다.
        /// </summary>
        /// <param name="inputLayerObject"></param>
        /// <param name="basePath"></param>
        /// <param name="filename"></param>
        public void CreateKmzFromLayer(Layer inputLayerObject, string filename = "output.kmz", string basePath = null)
        {

            string outputPath = @"Export\";
            if (basePath != null)
            {
                _basePath = basePath;
                outputPath = _basePath + @"Export\";
            }


            var directoryInfo = new DirectoryInfo(outputPath + "files");
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            
            
            if (inputLayerObject != null)
            {

                Kml kmlObject = MapLayerOntoKml(inputLayerObject);

                var kmlOutPut = KmlFile.Create(kmlObject, true);

                using (var temp = new FileStream(outputPath+ "doc.kml", FileMode.Create))
                {
                    kmlOutPut.Save(temp);

                }
                //KmlFile 클래스가 IDisposable을 상속받고 있지 않으므로 using문 사용 불가
                KmlFile kml = KmzIOService.Instance.LoadKml(outputPath + "doc.kml");

                if (kml != null && File.Exists(outputPath + "doc.kml"))
                {                        
                    using (KmzFile kmz = KmzIOService.Instance.SaveKmlAndLinkedContentIntoAKmzArchive(kml,_basePath))
                    using (Stream output = File.OpenWrite(outputPath + filename))
                    {
                        kmz.Save(output);
                    }
                }


                if (File.Exists(outputPath + "doc.kml"))
                {
                    try
                    {
                        //KmlFile 클래스가 IDisposable을 상속받고 있지 않으므로 using문 사용 불가
                        File.Delete(outputPath + "doc.kml");
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                directoryInfo = new DirectoryInfo(outputPath + "files");
                if (directoryInfo.Exists)
                    directoryInfo.Delete(true);
                KmzIOService.Instance.Dispose();
                
            }
            else
            {
                Console.WriteLine($"Invalid Layer Object".ToString(CultureInfo.InvariantCulture));
            }

            return;

        }

        #region Pharsing Methods


        /// <summary>
        /// Layer객체를 KML객체로 변환
        /// </summary>
        /// <param name="InputLayerObject"></param>
        /// <returns>Kml객체</returns>
        private  Kml MapLayerOntoKml(Layer InputLayerObject)
        {
            //layer를 kml로 매핑

            var outputKmlObject = new Kml();

            var document = new Document()
            {
                Name = InputLayerObject.Name,
                Visibility = InputLayerObject.Visible
            };


            Layer nextLayer = InputLayerObject;


            for (int i = 0; i < nextLayer.Entities.Count; i++)
                document.AddFeature(MapEntitiesToFeature(nextLayer.Entities[i]));


            for (int i = 0; i < nextLayer.Child.Count; i++)
            {
                var folder = new Folder()
                {
                    Name = nextLayer.Child[i].Name,
                    Visibility = nextLayer.Child[i].Visible
                };
                folder = Travel(folder, nextLayer.Child[i]);
                document.AddFeature(folder);
            }

            outputKmlObject.Feature = document;
            return outputKmlObject;
        }
        /// <summary>
        /// Layer객체 트리를 순회
        /// </summary>
        /// <param name="root"></param>
        /// <param name="nextLayer"></param>
        /// <returns>Foler 객체 인스턴스</returns>
        private  Folder Travel(Folder root, Layer nextLayer)
        {
            // layer객체 순횐

            for (int i = 0; i < nextLayer.Entities.Count; i++)
                root.AddFeature(MapEntitiesToFeature(nextLayer.Entities[i]));


            for (int i = 0; i < nextLayer.Child.Count; i++)
            {
                var folder = new Folder
                {
                    Name = nextLayer.Child[i].Name,
                    Visibility = nextLayer.Child[i].Visible
                };
                folder = Travel(folder, nextLayer.Child[i]);
                root.AddFeature(folder);
            }

            return root;
        }
        /// <summary>
        /// Entity 객체를 Feature객체로 변환
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Feature 객체 인스턴스</returns>
        private  Feature MapEntitiesToFeature(IRenderable entity)
        {
            var placemark = new Placemark()
            {
                Name = entity.Name,
                Visibility = entity.Visible
            };

            if (entity.GetType() == typeof(Label) || (entity.GetType() == typeof(SymbolCollection)))
                return CreatePoints(entity,placemark);
            else if (entity.GetType() == typeof(Line) || entity.GetType() == typeof(SlerpLine))
               return CreateLineString(entity, placemark);
            else if (entity.GetType() == typeof(PolygonTriSub))
                return CreatePolygon(entity, placemark);         
            else
            {
                /*******
                 * Overlays....or Tooltip...?            
                 ******************************/
                return null;
            }
        }
        #endregion


        #region   #region Methods Creating KML Features 
        /// <summary>
        /// Entity객체와 같은 내용의 Point객체 생성
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="placemark"></param>
        /// <returns>Feature객체 인스턴스</returns>
        private Feature CreatePoints (IRenderable entity, Placemark placemark)
        {
            if (entity.GetType().Equals(typeof(Label)))
            {
                var labelOfSMUDI = (Label)entity;

                var coordinate = new Vector()
                {
                    Latitude = labelOfSMUDI.GetPoints()[0].Latitude,
                    Longitude = labelOfSMUDI.GetPoints()[0].Longitude,
                    Altitude = labelOfSMUDI.GetPoints()[0].Altitude,
                };
                var point = new SI.SMUDI.Cmm.Utils.Dom.Point()
                {
                    Extrude = false,
                    AltitudeMode = AltitudeMode.RelativeToGround,
                    Coordinate = coordinate
                };

                var iconstyle = new IconStyle()
                {
                    Icon = null
                };
                var labelstyle = new LabelStyle()
                {
                    Color = new Color32(labelOfSMUDI.Color.A, labelOfSMUDI.Color.B, labelOfSMUDI.Color.G, labelOfSMUDI.Color.R),
                    Scale = 1//바궈줄필요있음
                };
                var style = new Style()
                {
                    Icon = iconstyle,
                    Label = labelstyle
                };

                placemark.AddStyle(style);
                placemark.Geometry = point;
            }
            else if (entity.GetType().Equals(typeof(SymbolCollection)))
            {
                var symbolcollection = (SymbolCollection)entity;
                if (symbolcollection.Count > 0)
                { 
                    
                    Bitmap symbolImg =symbolcollection.Texture;
                    
                    string filename = $"resource{_fileNumber++}.png";
                    string filepath;

                    if (_basePath == null)
                        filepath = @"Export\files\";
                    else
                        filepath = _basePath + @"Export\files\";

                    if (!Directory.Exists(filepath))
                        Directory.CreateDirectory(filepath);

                   

                    symbolImg.Save(filepath + filename);


                    var coordinate = new Vector()
                    {
                        Latitude = symbolcollection.GetSymbol(0).Position.Latitude,
                        Longitude = symbolcollection.GetSymbol(0).Position.Longitude,
                        Altitude = symbolcollection.GetSymbol(0).Position.Altitude
                    };
                    
                    var point = new SI.SMUDI.Cmm.Utils.Dom.Point()
                    {
                        Extrude = false,
                        AltitudeMode = AltitudeMode.Absolute,
                        Coordinate = coordinate
                    };


                    var iconstyle = new IconStyle()
                    {
                        Color = new Color32(symbolcollection.GetSymbol(0).Color.A, symbolcollection.GetSymbol(0).Color.B, symbolcollection.GetSymbol(0).Color.G, symbolcollection.GetSymbol(0).Color.R),
                        Icon = new IconStyle.IconLink(new Uri(@"files\"+filename, UriKind.Relative))
                    };

                    var labelstyle = new LabelStyle()
                    {
                        Scale = 0
                    };
                    var style = new Style()
                    {
                        Icon = iconstyle,
                        Label = labelstyle
                    };

                    placemark.AddStyle(style);
                    placemark.Geometry = point;
                }  
            }

            return placemark;
        }
        /// <summary>
        /// Entity객체와 같은 내용의 Line객체 생성
        /// SlerpLine / Tesselated : true , AltitudeMode : ClmapToGround
        /// Line / Tesslated : False, AltitudeMode : Absolute
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="placemark"></param>
        /// <returns>Feature객체 인스턴스</returns>
        private Feature CreateLineString(IRenderable entity, Placemark placemark)
        {
            if (entity.GetType().Equals(typeof(SlerpLine)))
            {
                var lineOfSMUDI = (SlerpLine)entity;

                var coordinates = new List<Vector>();
                for (int i = 0; i < lineOfSMUDI.Length; i++)
                {
                    var coordinate = new Vector()
                    {
                        Latitude = lineOfSMUDI.GetPoint(i).Latitude,
                        Longitude = lineOfSMUDI.GetPoint(i).Longitude,
                        Altitude = lineOfSMUDI.GetPoint(i).Altitude
                    };
                    coordinates.Add(coordinate);
                }

                var line = new LineString()
                {
                    AltitudeMode = AltitudeMode.ClampToGround,
                    Tessellate = true,
                    Extrude = false,
                    Coordinates = new CoordinateCollection(coordinates)
                };

                var linestyle = new SI.SMUDI.Cmm.Utils.Dom.LineStyle()
                {
                    Color = new Color32(lineOfSMUDI.LineStyle.Color.A, lineOfSMUDI.LineStyle.Color.B, lineOfSMUDI.LineStyle.Color.G, lineOfSMUDI.LineStyle.Color.R),
                    Width = lineOfSMUDI.LineStyle.Thickness
                };
                var style = new Style()
                {
                    Line = linestyle
                };
                placemark.AddStyle(style);
                placemark.Geometry = line;
            }
            else if (entity.GetType().Equals(typeof(Line)))
            {
                var lineOfSMUDI = (Line)entity;
                var coordinates = new List<Vector>();
                for (int i = 0; i < lineOfSMUDI.Length; i++)
                {
                    Vector coordinate = new Vector()
                    {
                        Latitude = lineOfSMUDI.GetPoint(i).Latitude,
                        Longitude = lineOfSMUDI.GetPoint(i).Longitude,
                        Altitude = lineOfSMUDI.GetPoint(i).Altitude
                    };
                    coordinates.Add(coordinate);
                }

                var line = new LineString()
                {
                    AltitudeMode = AltitudeMode.Absolute,
                    Tessellate = false,
                    Extrude = false,
                    Coordinates = new CoordinateCollection(coordinates)
                };

                var linestyle = new SI.SMUDI.Cmm.Utils.Dom.LineStyle()
                {
                    Color = new Color32(lineOfSMUDI.LineStyle.Color.A, lineOfSMUDI.LineStyle.Color.B, lineOfSMUDI.LineStyle.Color.G, lineOfSMUDI.LineStyle.Color.R),
                    Width = lineOfSMUDI.LineStyle.Thickness

                };
                var style = new Style()
                {
                    Line = linestyle
                };
                placemark.AddStyle(style);
                placemark.Geometry = line;
            }
            
            return placemark;
        }
        /// <summary>
        /// Entity객체와 같은 내용의 Polygon객체 생성
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="placemark"></param>
        /// <returns>Feature객체 인스턴스</returns>
        private Feature CreatePolygon(IRenderable entity, Placemark placemark)
        {

            var polyOfSMUDI = (PolygonTriSub)entity;

            var coordinates = new List<Vector>();
            for (int i = 0; i < polyOfSMUDI.Points.Count; i++)
            {
                var coordinate = new Vector()
                {
                    Latitude = polyOfSMUDI.Points[i].Latitude,
                    Longitude = polyOfSMUDI.Points[i].Longitude,
                    Altitude = polyOfSMUDI.Points[i].Altitude
                };
                coordinates.Add(coordinate);
            }
            var coordinate_ = new Vector()
            {
                Latitude = polyOfSMUDI.Points[0].Latitude,
                Longitude = polyOfSMUDI.Points[0].Longitude,
                Altitude = polyOfSMUDI.Points[0].Altitude
            };
            coordinates.Add(coordinate_);

            var poly = new SI.SMUDI.Cmm.Utils.Dom.Polygon()
            {
                AltitudeMode = AltitudeMode.RelativeToGround,
                OuterBoundary = new OuterBoundary()
                {
                    LinearRing = new LinearRing()
                    {
                        Coordinates = new CoordinateCollection(coordinates),
                        AltitudeMode = AltitudeMode.Absolute
                    }
                }
            };

            var linestyle = new SI.SMUDI.Cmm.Utils.Dom.LineStyle()
            {
                Color = new Color32(polyOfSMUDI.LineColor.A, polyOfSMUDI.LineColor.B, polyOfSMUDI.LineColor.G, polyOfSMUDI.LineColor.R),
                Width = polyOfSMUDI.LineThickness
            };

            var polystyle = new PolygonStyle()
            {
                Color = new Color32((byte)polyOfSMUDI.Opacity, polyOfSMUDI.PolygonColor.B, polyOfSMUDI.PolygonColor.G, polyOfSMUDI.PolygonColor.R),
                Fill = polyOfSMUDI.PolygonType >= PolygonType.Polygon ? true : false,
                Outline = true
            };
            var style = new Style()
            {
                Line = linestyle,
                Polygon = polystyle
            };

            placemark.AddStyle(style);
            placemark.Geometry = poly;
          
            return placemark;
        }

        #endregion
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _instance = null;
                _basePath = null;
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;

            base.Dispose(disposing);
        }
    }
}
