
namespace Module.Models
{
    /// <summary>
    /// Kml 문서의 StyleSelector가 <Style>이 아닌, <StyleMap> 형식일때,
    /// StyleID를 참조하기위한 두 key값에 대한 클래스
    /// </summary>
    public class StyleKeys
    {
        /// <summary>
        /// 평상시에 보여지는 스타일에 대한 StyleID
        /// </summary>
        public string Normal { get; set; }
        /// <summary>
        /// 마우스로 클릭했을때 보여지는 스타일에 대한 StyleID
        /// </summary>
        public string Highlight { get; set; }
    }
}
