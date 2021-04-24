using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Modern.Vice.PdbMonitor.Behaviors
{
    /// <summary>
    /// Container class for attached properties. Must inherit from <see cref="AvaloniaObject"/>.
    /// </summary>
    /// <remarks>Sources from Avalonia docs, slightly updated.</remarks>
    public class DoubleTappedBehavior : AvaloniaObject
    {
        /// <summary>
        /// Identifies the <seealso cref="CommandProperty"/> avalonia attached property.
        /// </summary>
        /// <value>Provide an <see cref="ICommand"/> derived object or binding.</value>
        public static readonly AttachedProperty<ICommand> CommandProperty = AvaloniaProperty.RegisterAttached<DoubleTappedBehavior, Interactive, ICommand>(
            "Command", default!, false, BindingMode.OneTime, coerce: ValidateCommand);

        /// <summary>
        /// Identifies the <seealso cref="CommandParameterProperty"/> avalonia attached property.
        /// Use this as the parameter for the <see cref="CommandProperty"/>.
        /// </summary>
        /// <value>Any value of type <see cref="object"/>.</value>
        public static readonly AttachedProperty<object> CommandParameterProperty = AvaloniaProperty.RegisterAttached<DoubleTappedBehavior, Interactive, object>(
            "CommandParameter", default!, false, BindingMode.OneWay, null);


        /// <summary>
        /// The coerce value function. Returns the final (probably corrected result).
        /// can be used to perform actions during assign.
        /// </summary>
        private static ICommand ValidateCommand(IAvaloniaObject element, ICommand commandValue)
        {
            if (element is Interactive interactElement)
            {
                if (commandValue is not null)
                {
                    // Add non-null value
                    interactElement.AddHandler(InputElement.DoubleTappedEvent, Handler!);
                }
                else
                {
                    // remove prev value
                    interactElement.RemoveHandler(InputElement.DoubleTappedEvent, Handler!);
                }
            }

            return commandValue!;

            // local handler fcn
            void Handler(object s, RoutedEventArgs e)
            {
                // This is how we get the parameter off of the gui element.
                object commandParameter = interactElement.GetValue(CommandParameterProperty);
                if (commandValue?.CanExecute(commandParameter) == true)
                {
                    commandValue.Execute(commandParameter);
                }
            }
        }

        /// <summary>
        /// Accessor for Attached property <see cref="CommandProperty"/>.
        /// </summary>
        public static void SetCommand(AvaloniaObject element, ICommand commandValue)
        {
            element.SetValue(CommandProperty, commandValue);
        }

        /// <summary>
        /// Accessor for Attached property <see cref="CommandProperty"/>.
        /// </summary>
        public static ICommand GetCommand(AvaloniaObject element)
        {
            return element.GetValue(CommandProperty);
        }

        /// <summary>
        /// Accessor for Attached property <see cref="CommandParameterProperty"/>.
        /// </summary>
        public static void SetCommandParameter(AvaloniaObject element, object parameter)
        {
            element.SetValue(CommandParameterProperty, parameter);
        }

        /// <summary>
        /// Accessor for Attached property <see cref="CommandParameterProperty"/>.
        /// </summary>
        public static object GetCommandParameter(AvaloniaObject element)
        {
            return element.GetValue(CommandParameterProperty);
        }
    }
}
