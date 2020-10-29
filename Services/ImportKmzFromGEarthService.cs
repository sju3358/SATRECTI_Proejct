#region System Namespace
using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
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
using SMUDI.Core;
#endregion

using Module.Models;




namespace Module.Services
{
    /// <summary>
    /// 구글에서 Exporting된 KMZ 파일을 파싱하여 SMUDI Layer로 리턴합니다.
    /// </summary>
    public class ImportKmzFromGEarthService : DisposeService
    {
        #region Properties

        private bool _disposed;
        private int _fileNumberForPath;
        private OpenTK.Graphics.IGraphicsContext _iGraphicContext;
        private OpenTK.Platform.IWindowInfo _iWindowInfo;
        private KmzFile _kmzfile;
        #endregion

        #region Constructor

        private static ImportKmzFromGEarthService _instance;
        public static ImportKmzFromGEarthService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ImportKmzFromGEarthService();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        /// <summary>
        /// Constructor
        /// Label 객체생성을 위해 SMUDI.Earth.Earth 인스턴스를 _smudi에 대입하고
        /// WEB Url 형식으로되어있는 파일을 다운로드하여 파일에 번호를 매기기 위한 fileNumberForPath에 1을 대입한다
        /// </summary>
        private ImportKmzFromGEarthService()
        {
            _fileNumberForPath = 1;
        }


        #endregion


        /// <summary>
        /// KMZ파일을 읽어 그에 알맞는 SMUDI.Scence.Layer.Layer를 생성합니다.
        /// 매개변수는 파일명을 포함한 상대경로 or 절대경로입니다
        /// </summary>
        /// <param name="iGraphicContext"></param>
        /// <param name="iWindowInfo"></param>
        /// <param name="filePath">상대경로 or 절대경로</param>
        /// <returns>Layer 객체 인스턴스</returns>
        public Layer CreateLayerFromKmz(OpenTK.Graphics.IGraphicsContext iGraphicContext, OpenTK.Platform.IWindowInfo iWindowInfo, string filePath = "input.kmz")
        {
            _iGraphicContext = iGraphicContext;
            _iWindowInfo = iWindowInfo;

            using (Stream stream = File.OpenRead(filePath))
            {
                using (_kmzfile = KmzFile.Open(stream))
                {
                    Parser parser = new Parser();
                    parser.ParseString(_kmzfile.ReadKml(),false);
                    Layer layer = MapKmlOntoLayer((Kml)parser.Root);
                    return layer;
                }
            }
        }
        

        #region Pharsing Methods



        /// <summary>
        /// KML 문서를 파싱하여 생성된 KML객체를 SMUDI Layer객체로 매핑합니다.
        /// </summary>
        /// <param name="inputKml"></param>
        /// <returns>Layer 객체 인스턴스</returns>
        private Layer MapKmlOntoLayer(Kml inputKml)
        {
            var outputLayer = new Layer();
            if (inputKml.Feature.GetType() == (typeof(Document)) || inputKml.Feature.GetType() == (typeof(Folder)))
            {
                outputLayer = Travel(outputLayer, (Container)inputKml.Feature);
            }
            else
                MapFeatureOntoEntity(outputLayer, inputKml.Feature); //ROOT는 한개이기때문에 CONTAINER가 아니라면 노드 하나만 달리고 끝

            return outputLayer;
        }
        /// <summary>
        /// KMZ파일로로 부터 파싱된 Feature 객체를 
        /// 아래에 해당하는 SMUDI Layer로 매핑합니다.
        /// 1. Label
        /// 2. SymbolCollection
        /// 3. SlerpLine
        /// 4. Line 
        /// 5. Polygon 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="feature"></param>
        private void MapFeatureOntoEntity(Layer root, Feature feature)
        {

            StyleValues styles = StyleSelectorService.Instance.LoadStyleList(feature);

            if (feature.GetType() == (typeof(Placemark)))
            {
                var placemark = (Placemark)feature;
                if (placemark.Geometry != null)
                {
                    if (placemark.Geometry.GetType() == (typeof(SI.SMUDI.Cmm.Utils.Dom.Point)))
                    {

                        // 구글어스에서 파싱하는 경우 라벨, 심볼 모두 표시

                        if (styles.Normal.Icon != null && styles.Normal.Icon.Icon!= null && styles.Normal.Icon.Icon.Href != null)
                            root.Add(CreateSymbol(placemark, styles));
                        
                        if(styles.Normal.Label == null || styles.Normal.Label.Scale == null || styles.Normal.Label.Scale != 0)
                            root.Add(CreateLabel(placemark, styles));

                    }
                    else if (placemark.Geometry.GetType() == (typeof(LineString)))
                    {


                        // Tessellate가 true이면 Slerpline,
                        // 그 외 Line은 Line에 매핑합니다.
                        // 고도모드는 RelativeToGround, Absolute, ClampToGround 모두, Absolute라고 가정합니다.


                        if (((LineString)placemark.Geometry).Tessellate == true)
                        {
                            SlerpLine slerpLineEntity = CreateSlerpLine(placemark, styles);
                            root.Add(slerpLineEntity);

                        }
                        else
                        {
                            Line lineEntity = CreateLine(placemark, styles);
                            root.Add(lineEntity);
                        }


                    } //Line
                    else if (placemark.Geometry.GetType() == (typeof(SI.SMUDI.Cmm.Utils.Dom.Polygon)))
                    {
                        List<PolygonTriSub> polygonEntity = CreatePolygon(placemark, styles);
                        for (IEnumerator<PolygonTriSub> nextPolygon = polygonEntity.GetEnumerator(); nextPolygon.MoveNext();)
                            root.Add(nextPolygon.Current);

                    }//Polygon              
                    else
                    {
                        /*  ~~~~ CODE ~~~~~*/
                    }//그 외 ex ) tooltip 아직 미구현
                }
            } //Geometry
            else if (feature.GetType() == (typeof(Overlay)))
            {
                return; //overlay의 경우 미구현
            } //Overlay
            else
            {
                /* ~~~~~CODE ~~~~*/
            }// SMUDI에서 미지원하는 경우

            return;
        }


        /// <summary>
        /// KML 객체 트리를 순회합니다.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="nextContainer"></param>
        /// <returns>Layer 객체인스턴스</returns>
        private Layer Travel(Layer root, Container nextContainer)
        { 
            root.Name = nextContainer.Name;
            root.Visible = nextContainer.Visibility != false;

            for (IEnumerator<StyleSelector> nextStyles = nextContainer.Styles.GetEnumerator(); nextStyles.MoveNext();)
            {
                if (nextStyles.Current.GetType() == (typeof(Style)))
                    StyleSelectorService.Instance.AddStyleDictionary("#" + nextStyles.Current.Id, (Style)nextStyles.Current);
                else if (nextStyles.Current.GetType() == (typeof(StyleMapCollection)))
                {
                    StyleKeys styleKeys = new StyleKeys();
                    for (IEnumerator<Pair> currentKey = ((StyleMapCollection)nextStyles.Current).GetEnumerator(); currentKey.MoveNext();)
                    {
                        if (currentKey.Current.State == StyleState.Normal)
                            styleKeys.Normal = currentKey.Current.StyleUrl.ToString();
                        else if (currentKey.Current.State == StyleState.Highlight)
                            styleKeys.Highlight = currentKey.Current.StyleUrl.ToString();
                    }

                    StyleSelectorService.Instance.AddStyleKeyDictionary("#" + nextStyles.Current.Id, styleKeys);
                }
            }

            //ENTITIY 추가 Recursive
            var featureList = (List<Feature>)nextContainer.Features;
            for (int i = 0; i < featureList.Count; i++)
            {
                //Container features 추가 / 순회
                if (featureList[i].GetType() == (typeof(Folder)) || featureList[i].GetType() == (typeof(Document)))
                {
                    Layer layer = new Layer();
                    layer = Travel(layer, (Container)featureList[i]);
                    root.AddChild(layer);
                }
                else
                {
                    MapFeatureOntoEntity(root, featureList[i]);
                }
            }
            return root;
        }

        #endregion


        #region  Methods Creating Irendable Object

        /// <summary>
        /// KML객체의 Point 태그(객체)와 같은 모양의 Label을 생성합니다.
        /// </summary>
        /// <param name="placemark"></param>
        /// <param name="styles"></param>
        /// <returns>Label 객체 인스턴스</returns>
        private Label CreateLabel(Placemark placemark, StyleValues styles)
        {
            var point = (SI.SMUDI.Cmm.Utils.Dom.Point)placemark.Geometry;
            var position = new LongLatAltitude(point.Coordinate.Longitude, point.Coordinate.Latitude, (double)point.Coordinate.Altitude);


            //라벨색깔
            var normalLabelColor32 = (Color32)(styles.Normal.Label != null && styles.Normal.Label.Color != null ? styles.Normal.Label.Color : StyleSelectorService.Instance.GetStyle("defaultNormal").Label.Color);
            var highlightLabelColor32 = (Color32)(styles.Highlight.Label != null && styles.Highlight.Label.Color != null ? styles.Highlight.Label.Color : StyleSelectorService.Instance.GetStyle("defaultHighlight").Label.Color);
            var normalLabelColor = Color.FromArgb(normalLabelColor32.Argb);
            var highlightLabelColor = Color.FromArgb(highlightLabelColor32.Argb);

            //라벨크기
            double scale = (double)(styles.Normal.Label != null && styles.Normal.Label.Scale.HasValue ? styles.Normal.Label.Scale : 1);

            //라벨객체 생성
            var labelEntity = new Label(_iGraphicContext, _iWindowInfo)
            {
                Visible = placemark.Visibility ?? true,
                Name = placemark.Name,
                Color = normalLabelColor,
                SelectedColor = highlightLabelColor,
                RectWidth = (int)(100 * scale),
                RectHeight = (int)(20 * scale)
            };
            labelEntity.AddFont2D(position, placemark.Name);
            
            return labelEntity;
        }
        /// <summary>
        /// KML객체의 Point 태그(객체)와 같은 모양의 SymbolCollection을 생성합니다.
        /// </summary>
        /// <param name="placemark"></param>
        /// <param name="styles"></param>
        /// <returns>SymbolCollection 객체 인스턴스</returns>
        private SymbolCollection CreateSymbol(Placemark placemark, StyleValues styles)
        {
            var point = (SI.SMUDI.Cmm.Utils.Dom.Point)placemark.Geometry;
            var position = new LongLatAltitude(point.Coordinate.Longitude, point.Coordinate.Latitude, (double)point.Coordinate.Altitude);


            //심볼 이미지 다운로드 및 메모리에 비트맵으로 올리기
            string iconPath = (styles.Normal.Icon.Icon.Href.ToString()).Replace("\\", "/");
            Bitmap bitmap;
            if (_kmzfile.Files.Contains(iconPath))
            {
                var memorystream = new MemoryStream(_kmzfile.ReadFile(iconPath));
                bitmap = new Bitmap(memorystream);
            }
            else
            {

                string urlPath = styles.Normal.Icon.Icon.Href.ToString();
                var webClient = new WebClient();
                string filenumber = (_fileNumberForPath++).ToString(CultureInfo.CurrentCulture);
                string filename = @"Resource\Temp\downloadedImag" + filenumber + ".png";

                try
                {
                    webClient.DownloadFile(urlPath, filename);
                    bitmap = new Bitmap(filename);
                    webClient.Dispose();
                }
                catch (ArgumentException)
                {
                    bitmap = new Bitmap(@"Resource\DefaultSymbol.png");
                }
                catch (WebException e)
                {
                    Console.WriteLine(e.Message);
                    bitmap = new Bitmap(@"Resource\DefaultSymbol.png");
                }
            }

            //심볼 색깔
            var normalIconColor32 = (Color32)(styles.Normal.Icon != null && styles.Normal.Icon.Color != null ? styles.Normal.Icon.Color : StyleSelectorService.Instance.GetStyle("defaultNormal").Icon.Color);
            var highlightIconColor32 = (Color32)(styles.Highlight.Icon != null && styles.Highlight.Icon.Color != null ? styles.Highlight.Icon.Color : StyleSelectorService.Instance.GetStyle("defaultHighlight").Icon.Color);
            var normalIconColor = Color.FromArgb(normalIconColor32.Argb);
            var highlightIconColor = Color.FromArgb(highlightIconColor32.Argb);
            
            //심볼 객체 생성
            var symbol = new Symbol()
            {
                Name = placemark.Name,
                Visible = placemark.Visibility ?? true,
                Color = normalIconColor,
                SelectedColor = highlightIconColor,
                Position = position,

            };
            

            var symbolCollection = new SymbolCollection(_iGraphicContext, _iWindowInfo)
            {
                Name = placemark.Name,
                Texture = bitmap ?? null
            };

            symbolCollection.Add(symbol);
            return symbolCollection;
        }
        /// <summary>
        /// KML객체의 Point 태그(객체)와 같은 모양의 SlerpLine을 생성합니다.
        /// </summary>
        /// <param name="placemark"></param>
        /// <param name="styles"></param>
        /// <returns>SlerpLine 객체 인스턴스</returns>
        private SlerpLine CreateSlerpLine(Placemark placemark, StyleValues styles)
        {
            var line = (LineString)placemark.Geometry;
            
            var normalLineColor32 = (Color32)(styles.Normal.Line != null && styles.Normal.Line.Color != null ? styles.Normal.Line.Color : StyleSelectorService.Instance.GetStyle("defaultNormal").Line.Color);
            var normalLineColor = Color.FromArgb(normalLineColor32.Argb);
            
            float lineWidth = (float)(styles.Normal.Line != null && styles.Normal.Line.Width != null ? styles.Normal.Line.Width : StyleSelectorService.Instance.GetStyle("defaultHighlight").Line.Width);

            var lineStyle = new SMUDI.Scene.Renderables.LineStyle(normalLineColor, lineWidth);


            var lineEntity = new SlerpLine()
            {
                Visible = placemark.Visibility ?? true,
                LineStyle = lineStyle,
                Name = placemark.Name,
            };
            for (IEnumerator<Vector> next = line.Coordinates.GetEnumerator(); next.MoveNext();)
                lineEntity.AddPoint(new LongLatAltitude(next.Current.Longitude, next.Current.Latitude, (double)next.Current.Altitude));

            return lineEntity;
        }
        /// <summary>
        /// KML객체의 Point 태그(객체)와 같은 모양의 Line을 생성합니다.
        /// </summary>
        /// <param name="placemark"></param>
        /// <param name="styles"></param>
        /// <returns>Line 객체 인스턴스</returns>
        private Line CreateLine(Placemark placemark, StyleValues styles)
        {
            var line = (LineString)placemark.Geometry;
            var normalLineColor32 = (Color32)(styles.Normal.Line != null && styles.Normal.Line.Color != null ? styles.Normal.Line.Color : StyleSelectorService.Instance.GetStyle("defaultNormal").Line.Color);
            var normalLineColor = Color.FromArgb(normalLineColor32.Argb);
            float lineWidth = (float)(styles.Normal.Line != null && styles.Normal.Line.Width != null ? styles.Normal.Line.Width : StyleSelectorService.Instance.GetStyle("defaultHighlight").Line.Width);

            var lineStyle = new SMUDI.Scene.Renderables.LineStyle(normalLineColor, lineWidth);


            var lineEntity = new Line()
            {
                Visible = placemark.Visibility ?? true,
                LineStyle = lineStyle,
                Name = placemark.Name,
            };
            for (IEnumerator<Vector> next = line.Coordinates.GetEnumerator(); next.MoveNext();)
                lineEntity.AddPoint(new LongLatAltitude(next.Current.Longitude, next.Current.Latitude, (double)next.Current.Altitude));

            return lineEntity;
        }
        /// <summary>
        /// KML객체의 Point 태그(객체)와 같은 모양의 Polygon을 생성합니다.
        /// </summary>
        /// <param name="placemark"></param>
        /// <param name="styles"></param>
        /// <returns>PolygonTriSub타입 리스트</returns>
        private List<PolygonTriSub> CreatePolygon(Placemark placemark, StyleValues styles)
        {
            var polygonEntities = new List<PolygonTriSub>();
            var poly = (SI.SMUDI.Cmm.Utils.Dom.Polygon)placemark.Geometry;
            var normalLineColor32 = (Color32)(styles.Normal.Line != null && styles.Normal.Line.Color != null ? styles.Normal.Line.Color : StyleSelectorService.Instance.GetStyle("defaultNormal").Line.Color);
            var normalPolygonColor32 = (Color32)(styles.Normal.Polygon != null && styles.Normal.Polygon.Color != null ? styles.Normal.Polygon.Color : StyleSelectorService.Instance.GetStyle("defaultHighlight").Polygon.Color);
            var normalLineColor = Color.FromArgb(normalLineColor32.Argb);
            var normalPolygonColor = Color.FromArgb(normalPolygonColor32.Argb);

            //outer boundary polygon 추가
            var outerBoundaryPolygon = new PolygonTriSub()
            {
                PolygonType = styles.Normal.Polygon.Fill == false ? PolygonType.Line : PolygonType.Polygon | PolygonType.Line,
                Name = placemark.Name,
                Visible = placemark.Visibility ?? true,
                PolygonColor = normalPolygonColor,
                Opacity = styles.Normal.Polygon.Fill == false ? 0 : styles.Normal.Polygon.Color.Value.Alpha,
                LineThickness = (float)(styles.Normal.Line.Width ?? 1),
                LineColor = normalLineColor
            };

            for (IEnumerator<Vector> next = poly.OuterBoundary.LinearRing.Coordinates.GetEnumerator(); next.MoveNext();)
                outerBoundaryPolygon.AddPoint(new LongLatAltitude(next.Current.Longitude, next.Current.Latitude, (double)next.Current.Altitude));

            outerBoundaryPolygon.Triangulation();
            polygonEntities.Add(outerBoundaryPolygon);

            //inner boundary polygon 추가
            for (IEnumerator<InnerBoundary> nextBoundary = poly.InnerBoundary.GetEnumerator(); nextBoundary.MoveNext();)
            {
                var innerBoundaryPolygon = new PolygonTriSub()
                {
                    PolygonType = styles.Normal.Polygon.Fill == false ? PolygonType.Line : PolygonType.Polygon | PolygonType.Line,
                    Name = placemark.Name,
                    Visible = placemark.Visibility ?? false,
                    PolygonColor = normalPolygonColor,
                    Opacity = 0,
                    LineThickness = (float)(styles.Normal.Line.Width ?? 1),
                    LineColor = normalLineColor,

                };
                for (IEnumerator<Vector> next = nextBoundary.Current.LinearRing.Coordinates.GetEnumerator(); next.MoveNext();)
                    innerBoundaryPolygon.AddPoint(new LongLatAltitude(next.Current.Longitude, next.Current.Latitude, (double)next.Current.Altitude));

                innerBoundaryPolygon.Triangulation();
                polygonEntities.Add(innerBoundaryPolygon);

            }

            return polygonEntities;

        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _instance = null;
                StyleSelectorService.Instance.Dispose();
                _iGraphicContext = null;
                _iWindowInfo = null;
                _kmzfile.Dispose();
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;

            base.Dispose(disposing);
        }
    }
}
