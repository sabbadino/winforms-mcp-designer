
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;


namespace WinFormsApp1
{
    public interface IMcpMarker
    {
        // This interface is used to mark classes that contain MCP tools.
        // It can be used for reflection or other purposes.
    }   
[McpServerToolType]
    public sealed class McpTools : ISingletonScope , IMcpMarker
    {

        public McpTools()
        {
            
        }   
        [McpServerTool(Name ="add_control_to_form"), 
            Description("Add a button to the form. Returns the updated layout of the form")]
        public string AddControl(
            [Description("The type of the control")] ControlTypes controlType,
            [Description("The text displayed in the control")] string controlText,
            [Description("The name of the control")] string controlName,
            [Description("The Horizontal position of the control. If not specified choose a position that does not overlap with existing controls, If not controls are present set to 100")] int controlHorizontalPosition,
            [Description("The Vertical position of the control. If not specified choose a position that does not overlap with existing controls, If not controls are present set to 100")] int controlVerticalPosition
            ) {
            try
            {
             //   ArgumentNullException.ThrowIfNull(Form1._LLMDrivenForm);
                Form1._LLMDrivenForm.Invoke(new Action(() => {
                    var type = Type.GetType($"System.Windows.Forms.{controlType.ToString()}, System.Windows.Forms",true,true);
                    if (type == null)
                    {
                        throw new ArgumentException($"Control type '{controlType}' is not recognized.");
                    }   
                    Control control = Activator.CreateInstance(type, true) as Control;
                    if (control == null)
                    {
                        throw new ArgumentException($"Could not create instance of '{controlType}'");
                    }

                    control.Text = string.IsNullOrEmpty(controlText) ? controlName : controlText;
                    control.Name = controlName;
                    control.Location = new System.Drawing.Point(controlHorizontalPosition, controlVerticalPosition);
                    control.AutoSize = true;    

                    Form1._LLMDrivenForm.Controls.Add(control);
            }));

            return JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }


        [McpServerTool(Name = "update_control_property"),
           Description("Update the text of a control in the form. Returns the updated layout of the form")]
        public string UpdateControlProperty(
            [Description("The name of the control to be updated")] string controlName,
            [Description("The property to be updated")] ControlProperties propertyName,
            [Description("The value of the property")] object propertyValue
           )
        {
            try
            {
                var t = new Control();  
                object propertyValueObj = null;
                if (propertyValue is JsonElement je)
                {
                    if(je.ValueKind == JsonValueKind.String)
                    {
                        propertyValueObj = je.GetString();  
                        if(propertyName == ControlProperties.BackColor || propertyName == ControlProperties.ForeColor)
                        {
                            // If the property is a color, we need to parse it
                            if (je.GetString() != null)
                            {
                                try
                                {
                                    propertyValueObj = ColorTranslator.FromHtml(je.GetString());
                                }
                                catch (Exception ex)
                                {
                                    throw new ArgumentException($"Invalid color value: {je.GetString()}. Error: {ex.Message}");
                                }
                            }
                        }
                    }
                    else if (je.ValueKind == JsonValueKind.Number)
                    {
                        propertyValueObj = je.GetInt32();
                    }
                    else if (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False)
                    {
                        propertyValueObj = je.GetBoolean();
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported JSON element type: {je.ValueKind}");
                    }   
              //      ArgumentNullException.ThrowIfNull(Form1._LLMDrivenForm);
                    Form1._LLMDrivenForm.Invoke(new Action(() =>
                    {
                        var control = Form1._LLMDrivenForm.Controls.Find(controlName, true).SingleOrDefault();
                        if (control != null)
                        {
                            SetPropertyValue(control, propertyName.ToString(), propertyValueObj);
                        }
                        else
                        {
                            throw new ArgumentException($"Control with name '{controlName}' not found.");
                        }
                    }));

                    return JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());
                }
                else
                {
                    throw new ArgumentException($"Property value must be a JSON element, but was {propertyValue.GetType().Name}."); 
                }
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }

        [McpServerTool(Name = "get_form_layout"),
           Description("Returns the layout of the form")]
        public string GetFormLayout()
        {
            var ret =  JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());
            return ret;
        }

        [McpServerTool(Name = "draw_line"),
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
                Form1._LLMDrivenForm.Invoke(new Action(() => {
                    using (Graphics g = Form1._LLMDrivenForm.CreateGraphics())
                    {
                        using (var pen = new Pen(ColorTranslator.FromHtml(color), 2))
                        {
                            g.DrawLine(pen, startX, startY, endX, endY);

                        }
                    }   

                }));
                var ret = JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());
                return ret;
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }


        [McpServerTool(Name = "draw_circle"),
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
                Form1._LLMDrivenForm.Invoke(new Action(() => {
                    using (Graphics g = Form1._LLMDrivenForm.CreateGraphics())
                    {
                        using (var pen = new Pen(ColorTranslator.FromHtml(color), 2))
                        {
                            g.DrawEllipse(pen, startX - radius, startY - radius, radius * 2, radius * 2);
                        }
                    }

                }));
                var ret = JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());
                return ret;
            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }

        [McpServerTool(Name = "resize_control"),
          Description("Resize The control by the specified X and y delta. If the user specify absolute values, inspect the form to calculate the delta. Returns the updated layout of the form")]
        public string MoveControlProperty(
           [Description("The name of the control to be updated")] string controlName,
           [Description("Delta X position")] int WidthDelta,
           [Description("Delta Y position")] int HeightDelta
          )
        {
            try
            {

                //ArgumentNullException.ThrowIfNull(Form1._LLMDrivenForm);
                Form1._LLMDrivenForm.Invoke(new Action(() =>
                {
                    var control = Form1._LLMDrivenForm.Controls.Find(controlName, true).SingleOrDefault();
                    if (control != null)
                    {
                        control.Size = new Size(control.Size.Width + WidthDelta, control.Size.Height + HeightDelta);
                    }
                    else
                    {
                        throw new ArgumentException($"Control with name '{controlName}' not found.");
                    }
                }));

                return JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());

            }
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }


        public static string GetAllExceptionMessages(Exception ex)
        {
            if (ex == null) return string.Empty;
            Exception exception = ex;
            var messages = new List<string>();
            while (exception != null)
            {
                messages.Add(exception.Message);
                exception = exception.InnerException;
            }

            return string.Join(" --> ", messages);
        }

        static void SetPropertyValue(object obj, string propertyName, object value)
        {
            var type = obj.GetType();
           
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
           
            if (property != null && property.CanWrite)
            {
                if (property.PropertyType != value.GetType())
                {
                    throw new ArgumentException($"Property '{propertyName}' expect type {property.PropertyType.Name} but propvedided value is of type {value.GetType().Name}'.");
                }
                property.SetValue(obj, value);
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' not found or is not writable on type '{type.Name}'.");   
            }
        }

    }

    public enum ControlProperties
    {
        Text,
        Left,
        Top,
        BackColor,
        ForeColor
    }
    public enum ControlTypes
    {
        Button,
        Label,
        TextBox,
        CheckBox,
        RadioButton,
        ComboBox,
        ListBox,
        PictureBox,
        Panel,
        GroupBox,
        TabControl,
        DataGridView
    }   
    
     

}
