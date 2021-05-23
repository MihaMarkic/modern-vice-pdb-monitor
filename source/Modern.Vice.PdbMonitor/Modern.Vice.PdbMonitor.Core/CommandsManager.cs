using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Core
{
    /// <summary>
    /// Provides capability to refresh command status based on property changes referenced in canExecute lambda.
    /// </summary>
    /// <remarks>
    /// Works only on expression tree level, won't evaluate properties or methods.
    /// </remarks>
    public class CommandsManager: DisposableObject
    {
        readonly NotifiableObject owner;
        readonly TaskFactory uiFactory;
        ImmutableDictionary<string, ImmutableArray<ICommandEx>> commands;
        public CommandsManager(NotifiableObject owner, TaskFactory uiFactory)
        {
            this.owner = owner;
            this.uiFactory = uiFactory;
            commands = ImmutableDictionary<string, ImmutableArray<ICommandEx>>.Empty;
            owner.PropertyChanged += Owner_PropertyChanged;
        }

        void Owner_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
            {
                if (commands.TryGetValue(e.PropertyName!, out var data))
                {
                    // always notify in UI thread
                    uiFactory.StartNew(() =>
                    {
                        foreach (var cmd in data)
                        {
                            cmd.RaiseCanExecuteChanged();
                        }
                    });
                }
            }
        }

        public RelayCommand CreateRelayCommand(Action execute, Expression<Func<bool>> canExecute, params string[] additionalProperties)
        {
            var command = new RelayCommand(execute, canExecute.Compile());
            RegisterCommandsByPropertyNames(command, canExecute.Body, additionalProperties);
            return command;
        }
        public RelayCommandAsync CreateRelayCommandAsync(Func<Task> execute, Expression<Func<bool>> canExecute, params string[] additionalProperties)
        {
            var command = new RelayCommandAsync(execute, canExecute.Compile());
            RegisterCommandsByPropertyNames(command, canExecute.Body, additionalProperties);
            return command;
        }
        //public RelayCommandGenericAsync<T> CreateRelayCommandGenericAsync<T>(Func<T?, Task> execute, Expression<Func<T?, bool>> canExecute, params string[] additionalProperties)
        //{
        //    var command = new RelayCommandGenericAsync<T>(execute, canExecute.Compile());
        //    RegisterCommandsByPropertyNames(command, canExecute.Body, additionalProperties);
        //    return command;
        //}
        public RelayCommand<T> CreateRelayCommand<T>(Action<T?> execute, Expression<Func<T?, bool>> canExecute, params string[] additionalProperties)
        {
            var command = new RelayCommand<T>(execute, canExecute.Compile());
            RegisterCommandsByPropertyNames(command, canExecute.Body, additionalProperties);
            return command;
        }
        internal void RegisterCommandsByPropertyNames(ICommandEx command, Expression body, string[] additionalProperties)
        {
            var propertyNames = ExtractPropertyNames(body);
            foreach (var pn in propertyNames.Union(additionalProperties))
            {
                if (!commands.TryGetValue(pn, out var data))
                {
                    data = ImmutableArray<ICommandEx>.Empty;
                }
                data = data.Add(command);
                commands = commands.SetItem(pn, data);
            }
        }
        internal ImmutableHashSet<string> ExtractPropertyNames(Expression body)
        {
            var result = ImmutableHashSet<string>.Empty;
            switch (body)
            {
                case MemberExpression memberExpression:
                    var member = memberExpression.Member;
                    if (member.MemberType == MemberTypes.Property)
                    {
                        if (owner.GetType().IsAssignableTo(member.DeclaringType))
                        {
                            result = result.Add(member.Name);
                            break;
                        }
                    }
                    break;
                case BinaryExpression binaryExpression:
                    result = result
                        .Union(ExtractPropertyNames(binaryExpression.Left))
                        .Union(ExtractPropertyNames(binaryExpression.Right));
                    break;
                case MethodCallExpression methodCallExpression:
                    foreach (var a in methodCallExpression.Arguments)
                    {
                        result = result.Union(ExtractPropertyNames(a));
                    }
                    break;
                case ConditionalExpression conditionalExpression:
                    result = result
                        .Union(ExtractPropertyNames(conditionalExpression.IfTrue))
                        .Union(ExtractPropertyNames(conditionalExpression.IfFalse))
                        .Union(ExtractPropertyNames(conditionalExpression.Test));
                    break;
                case ConstantExpression:
                    break;
                case UnaryExpression unary:
                    result = result.Union(ExtractPropertyNames(unary.Operand));
                    break;
                default:
                    throw new Exception($"Unrecognized expression {body.GetType().Name}");
            }
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                owner.PropertyChanged -= Owner_PropertyChanged;
            }
            base.Dispose(disposing);
        }
    }
}
