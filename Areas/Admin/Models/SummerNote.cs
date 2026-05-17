using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyPhongTro.Areas.Admin.Models
{
    public class SummerNote
    {
        public SummerNote (string idEditor, bool loadlibrary = true)
        {
            IDEditor = idEditor;
            Loadlibrary = loadlibrary;
        }
        public string IDEditor { get; set; }
        public bool Loadlibrary { get; set; }
        public int Height { get; set; } = 500;
        public string toolBar {get; set;} = @"
            [
                ['style', ['style']],
                ['font', ['bold', 'underline', 'clear']],
                ['fontname', ['fontname']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture', 'video']],
                ['view', ['fullscreen', 'codeview', 'help']]
            ]"; 

    }
}
