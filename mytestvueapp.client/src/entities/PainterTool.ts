export default class PainterTool {
  label: string;
  icon: string;
  shortcut: string;
  cursor: string;

  constructor(label: string, icon: string, shortcut: string, cursor: string) {
    this.label = label;
    this.icon = icon;
    this.shortcut = shortcut;
    this.cursor = cursor;
  }

  static getDefaults(): PainterTool[] {
    return [
      new PainterTool("Pan", "pi pi-arrows-alt", "P", "grab"),
      new PainterTool("Brush", "pi pi-pencil", "B", "crosshair"),
      new PainterTool("Eraser", "pi pi-eraser", "E", "crosshair"),
      new PainterTool("Pipette", "pi pi-eye dropper", "D", "crosshair"),
      new PainterTool("Bucket", "pi pi-hammer", "F", "crosshair"),
      new PainterTool("Line", "pi pi-minus", "L", "crosshair"),
      new PainterTool("Rectangle", "pi pi-stop", "R", "crosshair"),
      new PainterTool("Ellipse", "pi pi-circle", "O", "crosshair"),
      new PainterTool("Select", "pi pi-clone", "S", "crosshair")
    ];
  }
}
