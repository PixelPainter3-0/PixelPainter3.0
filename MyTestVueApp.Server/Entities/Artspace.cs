using MyTestVueApp.Server.Models;
using System.Data.SqlTypes;
using System.Reflection.Metadata.Ecma335;

namespace MyTestVueApp.Server.Entities
{
    public class Artspace
    {
        //Required
        public int Id { get; set; }
        public string Title { get; set; }
        public string Shape { get; set; }
    }
}
