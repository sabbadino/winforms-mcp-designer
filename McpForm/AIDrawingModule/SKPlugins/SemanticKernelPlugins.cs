
using AIDrawingModule;
using AIDrawingModuleAbstractions;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

using System;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;


namespace AIDrawingModule.SKPlugins
{

    public sealed class SemanticKernelPlugins
    {
        private readonly ICommandExecutor _commandExecutor;

        public SemanticKernelPlugins(ICommandExecutor commandExecutor )
        {
            _commandExecutor = commandExecutor;
        }   
        [KernelFunction("add_control_to_form"), 
            Description("Add a control to the form. Returns the updated layout of the form")]
        public string AddControl(
            [Description("The type of the control")] ControlTypes controlType,
            [Description("The text displayed in the control")] string controlText,
            [Description("The name of the control")] string controlName,
            [Description("The Horizontal position of the control. If not specified choose a position that does not overlap with existing controls, If not controls are present set to 100")] int controlHorizontalPosition,
            [Description("The Vertical position of the control. If not specified choose a position that does not overlap with existing controls, If not controls are present set to 100")] int controlVerticalPosition
            ) {
            try
            {
                return _commandExecutor.AddControl(controlType, controlText , controlName,
                    controlHorizontalPosition, controlVerticalPosition); 
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }


        [KernelFunction("update_control_property"),
           Description("Update the text of a control in the form. Returns the updated layout of the form")]
        public string UpdateControlProperty(
            [Description("The name of the control to be updated")] string controlName,
            [Description("The property to be updated")] ControlProperties propertyName,
            [Description("The value of the property")] string propertyValue
           )
        {
            try
            {

                return _commandExecutor.UpdateControlProperty(controlName,
                    propertyName, propertyValue);
                
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }

        [KernelFunction("get_form_layout"),
           Description("Returns the layout of the form")]
        public string GetFormLayout()
        {
            try
            {

                return _commandExecutor.GetFormLayout();
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }

        [KernelFunction("draw_line"),
           Description("Draws a line on the form. Returns the updated layout of the form")]
        public string DrawLine(
            [Description("star x point")] int startX,
            [Description("star y point")] int startY,
            [Description("end x point")] int endX,
            [Description("end y point")] int endY,
            [Description("line color")] string color
            )
        {
            try
            {

                return _commandExecutor.DrawLine(startX, startY, endX, endY, color);
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }


        [KernelFunction("draw_circle"),
         Description("Draws a circle on the form. Returns the updated layout of the form")]
        public string DrawCircle(
          [Description("center x point")] int startX,
          [Description("center y point")] int startY,
          [Description("radius")] int radius,
          [Description("circle color")] string color
          )
        {
            try
            {

                return _commandExecutor.DrawCircle(startX, startY, radius, color);
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }

        [KernelFunction("resize_control"),
          Description("Resize The control by the specified X and y delta. If the user specify absolute values, inspect the form to calculate the delta. Returns the updated layout of the form")]
        public string MoveControlProperty(
           [Description("The name of the control to be updated")] string controlName,
           [Description("Delta X position")] int WidthDelta,
           [Description("Delta Y position")] int HeightDelta
          )
        {
            try
            {

                return _commandExecutor.MoveControlProperty(controlName, WidthDelta, HeightDelta);
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }


        public static string GetAllExceptionMessages(Exception ex)
        {
            if (ex == null) return string.Empty;
            Exception? exception = ex;
            var messages = new List<string>();
            while (exception != null)
            {
                messages.Add(exception.Message);
                exception = exception.InnerException;
            }

            return string.Join(" --> ", messages);
        }

       

    }

 
    
     

}
