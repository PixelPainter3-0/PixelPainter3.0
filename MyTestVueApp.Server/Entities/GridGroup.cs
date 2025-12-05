using System.Drawing;

namespace MyTestVueApp.Server.Entities
{
    public class GridGroup
    {
        public string GroupName { get; set; }
        public int CanvasSize { get; set; }
        public string BackgroundColor { get; set; }
        public Dictionary<int, GridArtist> Artists { get; set; }
        public string[][] Pixels { get; set; }

        public GridGroup(int canvasSize)
        {
            GroupName = "K8GcUiUQZ5dnN673tC7G";
            BackgroundColor = "#FFFFFF";
            CanvasSize = canvasSize;
            Artists = new();
            Pixels = new string[canvasSize][];
            for (int i = 0; i < canvasSize; i++)
            {
                string[] line = new string[canvasSize];
                for (int j = 0; j < canvasSize; j++)
                {
                    line[j] = "empty";
                }
                Pixels[i] = line;
            }
        }
        public void AddMember(Artist member)
        {
            if (!Artists.ContainsKey(member.Id))
            {
                GridArtist newArtsit = new GridArtist(member);
                Artists.Add(member.Id, newArtsit);
            }
        }
        public bool PaintPixels(int id, string color, Coordinate coord)
        {
            if (coord.X >= 0 && coord.X < Pixels[0].GetLength(0) &&
                    coord.Y >= 0 && coord.Y < Pixels[0].GetLength(0))
            {
                GridArtist artist = Artists[id];
                if (artist.Additions.Count() < 5)
                {
                    artist.Additions.Add(DateTime.Now);
                    Pixels[coord.X][coord.Y] = color;
                    return true;
                }
                else
                {
                    DateTime now = DateTime.Now.AddMinutes(-5);
                    for (int i = 0; i < artist.Additions.Count(); i++)
                    {
                        if (now > artist.Additions[i])
                        {
                            artist.Additions[i] = DateTime.Now;
                            Pixels[coord.X][coord.Y] = color;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public List<List<Pixel>> GetPixelsAsList()
        {
            List<List<Pixel>> pixelVec = new();
            for (int i = 0; i < CanvasSize; i++)
            {
                List<Pixel> row = new();
                for (int j = 0; j < CanvasSize; j++)
                {
                    string color = Pixels[i][j];
                    if (Pixels[i][j] != null)
                    {
                        row.Add(new Pixel(color, i, j));
                    }
                }
                pixelVec.Add(row);
            }
            return pixelVec;
        }
        public void RemoveArtist(Artist artist)
        {
            if (!Artists.ContainsKey(artist.Id))
            {
                Artists.Remove(artist.Id);
            }
        }

        public IEnumerable<DateTime> TimeOuts(int artistId)
        {
            if (!Artists.TryGetValue(artistId, out var gridArtist))
            {
                return Enumerable.Empty<DateTime>();
            }

            DateTime cutoff = DateTime.Now.AddMinutes(-5);
            var dates = gridArtist.Additions.Where(t => t > cutoff).ToList();
            dates.Sort((d1, d2) => d1.CompareTo(d2));
            return dates;
        }

    }
}
