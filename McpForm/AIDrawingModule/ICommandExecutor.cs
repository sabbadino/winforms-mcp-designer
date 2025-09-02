using AIDrawingModuleAbstractions;
using System.ComponentModel;

namespace AIDrawingModule
{
    public interface ICommandExecutor
    {
        string AddControl(
            ControlTypes controlType,
            string controlText,
            string controlName,
            int controlHorizontalPosition,
            int controlVerticalPosition
            );

        string UpdateControlProperty(
            string controlName,
            ControlProperties propertyName,
            string propertyValue
           );

        string GetFormLayout();

        string DrawLine(
           int startX,
           int startY,
           int endX,
           int endY,
           string color
           );

        string DrawCircle(
          int startX,
          int startY,
          int radius,
          string color
          );

        string MoveControlProperty(
          string controlName,
          int WidthDelta,
          int HeightDelta
         );
    }

}
