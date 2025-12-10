using System.Drawing;

using System.Collections.Generic;

namespace MyTestVueApp.Server.Entities
{

    public class Group(string groupName)
    {
        public string Name { get; set; } = groupName;
        public int CanvasWidth { get; set; } = 32;
        public int CanvasHeight { get; set; } = 32;

        public string BackgroundColor { get; set; } = "#FFFFFF";
        public List<Artist> CurrentMembers { get; set; } = new();
        public List<Artist> MemberRecord { get; set; } = new();
        public List<string[][]> Pixels { get; set; } = new List<string[][]>();

        
        public Group(string groupName, List<Artist> contributors, string[][][] canvas, int canvasWidth, int canvasHeight, string backgroundColor): this(groupName)
          {
            Name = groupName;
            foreach(string[][] grid in canvas){
              Pixels.Add(grid);
            }
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
            BackgroundColor = backgroundColor;
            CurrentMembers = new();
            MemberRecord = contributors;
        }

        public bool IsEmpty()
        {
            return CurrentMembers.Count == 0;
        }

        public void AddMember(Artist member)
        {
            if (!CurrentMembers.Any(cm => cm.Id == member.Id))
            {
                CurrentMembers.Add(member);
            }
            if (!MemberRecord.Any(mr => mr.Id == member.Id))
            {
                MemberRecord.Add(member);
            }
        }

        public void RemoveMember(Artist member)
        {
            var itemToRemove = CurrentMembers.FirstOrDefault(mem => mem.Id == member.Id);
            if(itemToRemove != null)
                CurrentMembers.Remove(itemToRemove);
        }

        public void PaintPixels(int layer, string color, Coordinate[] coords)
        {
            foreach (Coordinate coord in coords)
            {
                if (coord.X >= 0 && coord.X < CanvasWidth &&
                    coord.Y >= 0 && coord.Y < CanvasHeight)
                {
                    Pixels[layer][coord.X][coord.Y] = color;
                }
            }
        }

        public List<List<Pixel>> GetPixelsAsList()
        {
            List<List<Pixel>> pixelVec = new();
            for (int l = 0; l < Pixels.Count; l++) 
            {
                List<Pixel> row = new();
                for (int i = 0; i < CanvasWidth; i++)
                {
                    for (int j = 0; j < CanvasHeight; j++)
                    {
                        string color = Pixels[l][i][j];
                        if (Pixels[l][i][j] != null)
                        {
                            row.Add(new Pixel(color, i, j));
                        } 
                    }
                }
                pixelVec.Add(row);
            }
            return pixelVec;
        }

        public int GetCurrentMemberCount()
        {
            return CurrentMembers.Count;
        }
    }

}
