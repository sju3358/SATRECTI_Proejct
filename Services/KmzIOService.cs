#region System Namespace
using System;
using System.IO;
#endregion


#region KML Namespace
using SI.SMUDI.Cmm.Utils.Engine;

#endregion



namespace Module.Services
{
    /// <summary>
    /// KMZ파일 입출력 관련 클래스
    /// </summary>
    public class KmzIOService : DisposeService
    {
        private bool _disposed;

        private static KmzIOService _instance;
        public static KmzIOService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new KmzIOService();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Kml파일을 파일스트림으로 열어, KmlFile객체로 변환한다.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>KmlFile 객체 인스턴스</returns>
        public  KmlFile LoadKml(string path)
        {
           
            Stream file = File.OpenRead(path);
            return KmlFile.Load(file);

        }

        /// <summary>
        /// KML파일의 링크노드를 탐색해, 해당 링크와 관련된 파일을 엮어 KMZ형태로 만들기위한 폴더에 넣는다.
        /// SymbolContext의 
        /// </summary>
        /// <param name="kml"></param>
        /// <param name="path"></param>
        /// <returns>KmzFile 객체</returns>
        public KmzFile SaveKmlAndLinkedContentIntoAKmzArchive(KmlFile kml, string basePath = null)
        {
            // All the links in the KML will be relative to the KML file, so
            // find it's directory so we can add them later
            //basepath == null  --> 워킹디렉토리
            if (basePath == null)
                basePath = Directory.GetCurrentDirectory() + "\\Export";
            else
                basePath = basePath + "\\Export";
            
            // Create the archive with the KML data
            var kmz = KmzFile.Create(kml);

            // Now find all the linked content in the KML so we can add the
            // files to the KMZ archive
            var links = new LinkResolver(kml);

            // Next gather the local references and add them.
            foreach (string relativePath in links.GetRelativePaths())
            {
                // Make sure it doesn't point to a directory below the base path
                if (relativePath.StartsWith("..", StringComparison.Ordinal))
                {
                    continue;
                }

                // Add it to the archive
                string fullPath = Path.Combine(basePath, relativePath);
                using (FileStream file = File.OpenRead(fullPath))
                {
                    kmz.AddFile(relativePath, file);
                }
            }
           

            return kmz;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _instance = null;
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;

            base.Dispose(disposing);
        }
    }
}
