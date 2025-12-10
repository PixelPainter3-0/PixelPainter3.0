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
        public bool isDisabled { get; set; }

        public GridGroup(int canvasSize)
        {
            GroupName = "K8GcUiUQZ5dnN673tC7G";
            BackgroundColor = "#FFFFFF";
            CanvasSize = canvasSize;
            Artists = new();
            Pixels = new string[canvasSize][];
            isDisabled = false;
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
        public void Clear()
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                string[] line = new string[Pixels.Length];
                for (int j = 0; j < Pixels[0].Length; j++)
                {
                    line[j] = "empty";
                }
                Pixels[i] = line;
            }
            foreach (var artist in Artists.Values)
            {
                artist.Additions.Clear();
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
                if (artist.Additions.Count() < 64)
                {
                    if (Pixels[coord.X][coord.Y].Equals(color))
                    {
                        return false;
                    }
                    artist.Additions.Add(DateTime.Now);
                    Pixels[coord.X][coord.Y] = color;
                    return true;
                }
                else
                {
                    DateTime now = DateTime.Now.AddMinutes(-10);
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

        public Art ConvertGridToArt(string name)
        {
            Art newArt = new Art();
            newArt.Title = name;
            newArt.CreationDate = DateTime.Now;
            newArt.PixelGrid = ConvertToPixelGrid();
            newArt.ArtistId = [0];
            newArt.IsPublic = true;
            newArt.IsGif = false;
            return newArt;
        }
        public PixelGrid ConvertToPixelGrid()
        {
            PixelGrid pixelGrid = new PixelGrid();
            pixelGrid.Width = Pixels.Count();
            pixelGrid.Height = Pixels.Count();
            pixelGrid.BackgroundColor = BackgroundColor;
            pixelGrid.EncodedGrid = string.Join("", Pixels.Select(row => string.Join("", row.Select(v => v.Equals("empty") ? "ffffff" : v))));
            return pixelGrid;
        }
        public void DisableGrid()
        {
            isDisabled = true;
        }
        public void EnableGrid() {
            isDisabled = false; 
        }
    }
}
