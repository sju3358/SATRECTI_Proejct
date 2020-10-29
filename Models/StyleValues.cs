using SI.SMUDI.Cmm.Utils.Dom;

namespace Module.Models
{
    /// <summary>
    /// Kml 문서의 StyleSelector가 <Style>이 아닌, <StyleMap> 형식일때,
    /// Style를 참조하기위한 두 key값에 대한 클래스
    /// </summary>
    public class StyleValues
    {
        /// <summary>
        /// 평상시에 보여지는 스타일에 대한 Style 객체
        /// </summary>
        public Style Normal { get; set; }
        /// <summary>
        /// 마우스로 클릭했을때 보여지는 스타일에 대한 Style 객체
        /// </summary>
        public Style Highlight { get; set; }
    }
}
