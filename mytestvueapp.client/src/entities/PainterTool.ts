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
      new PainterTool("Pan", "pi pi-arrows-alt", "p", "grab"),
      new PainterTool("Brush", "pi pi-pencil", "b", "crosshair"),
      new PainterTool("Eraser", "pi pi-eraser", "e", "crosshair"),
      new PainterTool("Pipette", "pi pi-eye dropper", "d", "crosshair"),
      new PainterTool("Bucket", "pi pi-hammer", "f", "crosshair"),
      new PainterTool("Rectangle", "pi pi-stop", "r", "crosshair"),
      new PainterTool("Ellipse", "pi pi-circle", "l", "crosshair"),
      new PainterTool("Select", "pi pi-clone", "s", "crosshair")
    ];
  }
}
