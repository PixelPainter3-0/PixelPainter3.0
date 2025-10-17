import Codec from "@/utils/Codec"

export class PixelGrid {
  width: number;
  height: number;
  backgroundColor: string;
  grid: string[][];
  lastGrid: string[][];
  encodedGrid?: string;
  isGif: boolean;
  blankGrid: string[][];
  undoStack: string[][][];
  redoStack: string[][][];
  redoStackMaxSize: number;

  constructor(
    width: number,
    height: number,
    backgroundColor: string,
    isGif: boolean,
    encodedGrid?: string
  ) {
    this.width = width;
    this.height = height;
    this.grid = this.createGrid(width, height);
    this.backgroundColor = backgroundColor;
    this.isGif = isGif;

    if (encodedGrid) {
      this.encodedGrid = encodedGrid;
      this.grid = Codec.Decode(
        encodedGrid,
        height,
        width,
        backgroundColor
      ).grid;
    }
    this.blankGrid = this.createGrid(width, height);
    this.lastGrid = this.createGrid(width, height);
    this.undoStack = [];
    this.redoStack = [];
    this.redoStackMaxSize = 0;
  }

  //Initialize a grid with a given width, height
  createGrid(
    width: number,
    height: number,
  ): string[][] {
    const grid: string[][] = [];
    for (let i = 0; i < height; i++) {
      const row: string[] = [];
      for (let j = 0; j < width; j++) {
        row.push("empty");
      }
      grid.push(row);
    }
    return grid;
  } 

  //Update the grid with another grid
  deepCopy(decodedGrid: PixelGrid): void {
    this.width = decodedGrid.width;
    this.height = decodedGrid.height;
    this.backgroundColor = decodedGrid.backgroundColor;
    this.isGif = decodedGrid.isGif;
    this.grid = this.createGrid(this.width, this.height);
    for (let i = 0; i < this.height; i++) {
      for (let j = 0; j < this.width; j++) {
        this.grid[i][j] = decodedGrid.grid[i][j];
      }
    }
    this.encodedGrid = Codec.Encode(this);
  }

  public getEncodedGrid(): string {
    return Codec.Encode(this);
  }

  public updateGrid(): void {
    this.undoStack.push(JSON.parse(JSON.stringify(this.lastGrid)));
    this.redoStack = [];
    this.redoStackMaxSize = 0;
    this.lastGrid = JSON.parse(JSON.stringify(this.grid));
  }

  public undo(): void {
    if(this.undoStack.length == 0) {
      if(this.redoStack.length != this.redoStackMaxSize){
        this.redoStack.push(JSON.parse(JSON.stringify(this.grid)));
      }
      this.grid = JSON.parse(JSON.stringify(this.blankGrid));
      this.lastGrid = JSON.parse(JSON.stringify(this.blankGrid));
    } else if(this.undoStack.length >= 1){
      this.redoStack.push(JSON.parse(JSON.stringify(this.grid)));
      if(this.redoStackMaxSize == 0){
        this.redoStackMaxSize = this.undoStack.length;
      }
      this.grid = JSON.parse(JSON.stringify(this.undoStack.pop()!));
      this.lastGrid = JSON.parse(JSON.stringify(this.grid));
    }
  }

  public redo(): void {
    if(this.redoStack.length >= 2){
      this.undoStack.push(JSON.parse(JSON.stringify(this.grid)));
      this.grid = JSON.parse(JSON.stringify(this.redoStack.pop()!));
      this.lastGrid = JSON.parse(JSON.stringify(this.grid));
    } else if (this.redoStack.length == 1){
      this.undoStack.push(JSON.parse(JSON.stringify(this.grid)));
      this.grid = JSON.parse(JSON.stringify(this.redoStack.pop()!));
      this.lastGrid = JSON.parse(JSON.stringify(this.grid));
    }
  }
}
