using AIDrawingModuleAbstractions;
using AIDrawingModuleAbstractions.IocConventions;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using WinFormsApp1;

namespace AIDrawingModule
{
    public class CommandExecutor : ICommandExecutor,ISingletonScope
    {
        
        public string Draw(Dictionary<string, object> command)
        {
            if (!command.TryGetValue("image_description", out var descriptionObj) || descriptionObj is not string description)
            {
                return "Error: 'image_description' parameter is missing or invalid.";
            }
            // Here you would integrate with an AI image generation service.
            // For demonstration purposes, we'll return a placeholder message.
            return $"Image generated with description: '{description}'";
        }

        public string AddControl(ControlTypes controlType,string controlText,
            string controlName,
            int controlHorizontalPosition,
            int controlVerticalPosition
            )
        {
            try
            {
                ArgumentNullException.ThrowIfNull(Form1._LLMDrivenForm);
                Form1._LLMDrivenForm.Invoke(new Action(() => {
                    var type = Type.GetType($"System.Windows.Forms.{controlType.ToString()}, System.Windows.Forms", true, true);
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
        public string UpdateControlProperty(
           string controlName,
           ControlProperties propertyName,
           string propertyValue
          )
        {
            try
            {
                var t = new Control();
                object? propertyValueObj = null;
                if (int.TryParse(propertyValue, out int propertyValueInt))
                {
                    propertyValueObj = propertyValueInt;
                }
                if (bool.TryParse(propertyValue, out bool propertyValueBool))
                {
                    propertyValueObj = propertyValueBool;
                }
                else 
                {
                    propertyValueObj = propertyValue;
                    if (propertyName == ControlProperties.BackColor || propertyName == ControlProperties.ForeColor)
                    {
                        // If the property is a color, we need to parse it
                        if (propertyValueObj is string)
                        {
                            try
                            {
                                propertyValueObj = ColorTranslator.FromHtml(propertyValueObj.ToString());
                            }
                            catch (Exception ex)
                            {
                                throw new ArgumentException($"Invalid color value: {propertyValueObj}. Error: {ex.Message}");
                            }
                        }
                    }
                }
                ArgumentNullException.ThrowIfNull(Form1._LLMDrivenForm);
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
            catch (Exception ex)
            {
                return $"error executing call. Error is: {GetAllExceptionMessages(ex)}";
            }
        }

    
        public string GetFormLayout()
        {
            var ret = JsonSerializer.Serialize(Form1._LLMDrivenForm.SerializeControl());
            return ret;
        }

    
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
                        using var pen = new Pen(ColorTranslator.FromHtml(color), 2);
                        g.DrawLine(pen, startX, startY, endX, endY);
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


      
        public string DrawCircle(
          int startX,
          int startY,
          int radius,
          string color
          )
        {
            try
            {
                Form1._LLMDrivenForm.Invoke(new Action(() => {
                    using (Graphics g = Form1._LLMDrivenForm.CreateGraphics())
                    {
                        using var pen = new Pen(ColorTranslator.FromHtml(color), 2);
                        g.DrawEllipse(pen, startX - radius, startY - radius, radius * 2, radius * 2);
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

        public string MoveControlProperty(
           string controlName,
           int WidthDelta,
           int HeightDelta
          )
        {
            try
            {

                ArgumentNullException.ThrowIfNull(Form1._LLMDrivenForm);
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


        private static string GetAllExceptionMessages(Exception ex)
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

        private static void SetPropertyValue(object obj, string propertyName, object value)
        {
            var type = obj.GetType();

            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                if (property.PropertyType != value.GetType())
                {
                    throw new ArgumentException($"Property '{propertyName}' expect type {property.PropertyType.Name} but provided value is of type {value.GetType().Name}'.");
                }
                property.SetValue(obj, value);
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' not found or is not writable on type '{type.Name}'.");
            }
        }
    }

 
}
