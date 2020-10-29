using System.Collections.Generic;
using System;

using Module.Models;

using SI.SMUDI.Cmm.Utils.Base;
using SI.SMUDI.Cmm.Utils.Dom;


namespace Module.Services
{
    /// <summary>
    /// Kml에 정의되어있는 스타일을 저장하고,불러오기 위한 클래스
    /// </summary>
    public class StyleSelectorService : DisposeService
    {


        private Dictionary<string, Style> _stylesDictionary;
        private Dictionary<string, StyleKeys> _stylesKeyDictionary;
        private bool _disposed;


        private static StyleSelectorService _instance;
        
        /// <summary>
        /// StyleSelectorSerivce 인스턴스 생성
        /// </summary>
        public static StyleSelectorService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StyleSelectorService();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        private StyleSelectorService()
        {
            this.InitDictionary();
        }


        /// <summary>
        /// Feature 객체를 파라미터로 받아, StyleUrl이나, Style객체를 참조하여, StyleValues객체를 만든다.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns>StyleValues 객체</returns>
        public StyleValues LoadStyleList(Feature feature)
        {

            if (feature == null)
                return new StyleValues { Normal = this.GetStyle("defaultNormal"), Highlight = this.GetStyle("defaultHighlight") };


            // 기본값 할당된 StyleValues 객체
            var styles = new StyleValues()
            {
                Normal = _stylesDictionary["defaultNormal"],
                Highlight = _stylesDictionary["defaultHighlight"]
            };

            
            if (feature.StyleUrl != null) // StyleUrl 형식일때
            {
                if (_stylesKeyDictionary.ContainsKey(feature.StyleUrl.ToString()))
                {
                    styles.Normal = _stylesDictionary[_stylesKeyDictionary[feature.StyleUrl.ToString()].Normal];
                    styles.Highlight = _stylesDictionary[_stylesKeyDictionary[feature.StyleUrl.ToString()].Highlight];
                }
                else if (_stylesDictionary.ContainsKey(feature.StyleUrl.ToString()))
                    styles.Normal = _stylesDictionary[feature.StyleUrl.ToString()];
            }
            else if (feature.Styles.Count != 0) //Style 형식일때
            {
                for (IEnumerator<StyleSelector> nextStyle = feature.Styles.GetEnumerator(); nextStyle.MoveNext();)
                {
                    if (nextStyle.Current.GetType() == typeof(Style))
                    {
                        if (nextStyle.Current.Id != null)
                            _stylesDictionary.Add("#" + nextStyle.Current.Id, (Style)nextStyle.Current);
                        else
                        {
                            styles.Normal = (Style)nextStyle.Current;
                            break;
                        }

                    }
                    else if (nextStyle.Current.GetType() == typeof(StyleMapCollection))
                    {
                        for (IEnumerator<Pair> nextPair = ((StyleMapCollection)nextStyle.Current).GetEnumerator(); nextPair.MoveNext();)
                        {
                            if (nextPair.Current.State == StyleState.Normal)
                                styles.Normal = _stylesDictionary[nextPair.Current.StyleUrl.ToString()];
                            else if (nextPair.Current.State == StyleState.Highlight)
                                styles.Highlight = _stylesDictionary[nextPair.Current.StyleUrl.ToString()];
                        }
                    }
                }
            }

            return styles;
        }


        /// <summary>
        /// key값과 Style 쌍을 Style딕셔너리에 저장.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddStyleDictionary(string key, Style value)
        {
            _stylesDictionary.Add(key, value);
        }

        /// <summary>
        /// key값과 StyleKeys 쌍을 Style키 딕셔너리에 저장.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddStyleKeyDictionary(string key, StyleKeys value)
        {
            _stylesKeyDictionary.Add(key, value);
        }
       

        /// <summary>
        /// key값을 받아 Style객체를 리턴
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// key값에 대응하는 Style객체
        /// 존재 하지 않다면 default값
        /// </returns>

        public Style GetStyle(string key)
        {
            if (_stylesDictionary.ContainsKey(key))
                return _stylesDictionary[key];
            else
                return _stylesDictionary["defaultNormal"];
        }

        /// <summary>
        /// key값을 받아 Key쌍을 리턴
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// key값에 대응하는 key쌍
        /// 존재하지 않는다면 default값
        /// </returns>

        public StyleKeys GetStyleKeys(string key)
        {
            if (_stylesKeyDictionary.ContainsKey(key))
                return _stylesKeyDictionary[key];
            else
                return _stylesKeyDictionary["default"];
        }
        



        /// <summary>
        /// StyleDictionary와 StyleKeyDictionary에 기본값을 가진 스타일을 추가한다.
        /// </summary>
        private void InitDictionary()
        {

            var defaultNormalColor = new Color32(255, 0, 255, 255);
            var defaultNormalIconStyle = new IconStyle()
            {
                Color = defaultNormalColor,
                ColorMode = ColorMode.Normal
            };
            var defaultNormalLabelStyle = new LabelStyle()
            {
                Color = defaultNormalColor,
                ColorMode = ColorMode.Normal,
                Scale = 1
            };
            var defaultNormalLineStyle = new LineStyle()
            {
                Color = defaultNormalColor,
                Width = 1,
            };
            var defaultNormalPolygonStyle = new PolygonStyle()
            {
                Color = defaultNormalColor,
                Fill = true,
                Outline = true,
            };

            var defaultHighlightColor = new Color32(255, 0, 0, 255);
            var defaultHighlightIconStyle = new IconStyle()
            {
                Color = defaultHighlightColor,
                ColorMode = ColorMode.Normal
            };
            var defaultHighlightLabelStyle = new LabelStyle()
            {
                Color = defaultHighlightColor,
                ColorMode = ColorMode.Normal,
                Scale = 1
            };
            var defaultHighlightLineStyle = new LineStyle()
            {
                Color = defaultHighlightColor,
                Width = 1,
            };
            var defaultHighlightPolygonStyle = new PolygonStyle()
            {
                Color = defaultHighlightColor,
                Fill = true,
                Outline = true,
            };

            var defualtNormalStyle = new Style()
            {
                Icon = defaultNormalIconStyle,
                Label = defaultNormalLabelStyle,
                Line = defaultNormalLineStyle,
                Polygon = defaultNormalPolygonStyle
            };
            var defaultHighlightStyle = new Style()
            {
                Icon = defaultHighlightIconStyle,
                Label = defaultHighlightLabelStyle,
                Line = defaultHighlightLineStyle,
                Polygon = defaultHighlightPolygonStyle
            };
            _stylesDictionary = new Dictionary<string, Style>();
            _stylesKeyDictionary = new Dictionary<string, StyleKeys>();

            var keyPair = new StyleKeys()
            {
                Normal = "defaultNormal",
                Highlight = "defaultHighlight"
            };

            _stylesKeyDictionary.Add("default", keyPair);
            _stylesDictionary.Add("defaultNormal", defualtNormalStyle);
            _stylesDictionary.Add("defaultHighlight", defaultHighlightStyle);
        }
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _stylesDictionary.Clear();
                _stylesKeyDictionary = null;
                _stylesKeyDictionary.Clear();
                _stylesKeyDictionary = null;
                _instance = null;
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;

            base.Dispose(disposing);

        }
    }
}
