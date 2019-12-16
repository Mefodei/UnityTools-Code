namespace UniGreenModules.CommandTerminal.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public enum CommandPermissionLevel
    {
        Client = 1 << 0,
        Admin  = 1 << 1,
        Any    = ~0,
    }

    public class CommandInfo
    {
        public Action<CommandArg[]>   Proc;
        public int                    MaxArgCount;
        public int                    MinArgCount;
        public string                 Help;
        public CommandPermissionLevel PermissionLevel = CommandPermissionLevel.Any;
    }

    public struct CommandArg
    {
        public string String { get; set; }

        public int Int {
            get {
                int int_value;

                if (int.TryParse(String, out int_value)) {
                    return int_value;
                }

                TypeError("int");
                return 0;
            }
        }

        public float Float {
            get {
                float float_value;

                if (float.TryParse(String, out float_value)) {
                    return float_value;
                }

                TypeError("float");
                return 0;
            }
        }

        public bool Bool {
            get {
                if (string.Compare(String, "TRUE", ignoreCase: true) == 0) {
                    return true;
                }

                if (string.Compare(String, "FALSE", ignoreCase: true) == 0) {
                    return false;
                }

                TypeError("bool");
                return false;
            }
        }

        public override string ToString()
        {
            return String;
        }

        void TypeError(string expected_type)
        {
            Terminal.Shell.IssueErrorMessage(
                "Incorrect type for {0}, expected <{1}>",
                String, expected_type
            );
        }
    }

    public class CommandShell
    {
        Dictionary<string, CommandInfo> commands  = new Dictionary<string, CommandInfo>();
        List<CommandArg>                arguments = new List<CommandArg>(); // Cache for performance

        public string IssuedErrorMessage { get; private set; }

        public Dictionary<string, CommandInfo> Commands {
            get { return commands; }
        }

        /// <summary>
        /// Uses reflection to find all RegisterCommand attributes
        /// and adds them to the commands dictionary.
        /// </summary>
        public void RegisterCommands()
        {
            var rejectedCommands = new Dictionary<string, CommandInfo>();
            var method_flags      = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                foreach (var type in assembly.GetTypes()) {
                    foreach (var method in type.GetMethods(method_flags)) {
                        UpdateCommandMethod(method,rejectedCommands);
                    }
                }
            }

            HandleRejectedCommands(rejectedCommands);
        }

        private void UpdateCommandMethod(MethodInfo method,Dictionary<string, CommandInfo> rejectedCommands)
        {
            var attribute = Attribute.GetCustomAttribute(method, typeof(RegisterCommandAttribute)) as RegisterCommandAttribute;

            if (attribute == null) 
            {
                if (method.Name.StartsWith("FRONTCOMMAND", StringComparison.CurrentCultureIgnoreCase)) {
                    // Front-end Command methods don't implement RegisterCommand, use default attribute
                    attribute = new RegisterCommandAttribute();
                }
                else {
                    return;
                }
            }

            if (!IsCommandAllowed(attribute))
                return;

            var                  methods_params = method.GetParameters();
            var               command_name   = InferFrontCommandName(method.Name);
            Action<CommandArg[]> proc;

            if (attribute.Name == null) {
                // Use the method's name as the command's name
                command_name = InferCommandName(command_name == null ? method.Name : command_name);
            }
            else {
                command_name = attribute.Name;
            }

            if (methods_params.Length != 1 || methods_params[0].ParameterType != typeof(CommandArg[])) {
                // Method does not match expected Action signature,
                // this could be a command that has a FrontCommand method to handle its arguments.
                rejectedCommands.Add(command_name.ToUpper(),
                    CommandFromParamInfo(methods_params, attribute.Help));
                return;
            }

            // Convert MethodInfo to Action.
            // This is essentially allows us to store a reference to the method,
            // which makes calling the method significantly more performant than using MethodInfo.Invoke().
            proc = (Action<CommandArg[]>) Delegate.CreateDelegate(typeof(Action<CommandArg[]>), method);

            var info = new CommandInfo() {
                Proc            = proc,
                MinArgCount     = attribute.MinArgCount,
                MaxArgCount     = attribute.MaxArgCount,
                Help            = attribute.Help,
                PermissionLevel = attribute.PermissionLevel,
            };

            AddCommand(command_name, info);
        }

        private bool IsCommandAllowed(RegisterCommandAttribute commandAttribute)
        {
            if (commandAttribute.PermissionLevel == CommandPermissionLevel.Admin) {
#if ADMIN_TERMINAL_COMMANDS || UNITY_EDITOR
                return true;
#endif
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses an input line into a command and runs that command.
        /// </summary>
        public void RunCommand(string line)
        {
            var remaining = line;
            IssuedErrorMessage = null;
            arguments.Clear();

            while (remaining != "") {
                var argument = EatArgument(ref remaining);

                if (argument.String != "") {
                    arguments.Add(argument);
                }
            }

            if (arguments.Count == 0) {
                // Nothing to run
                return;
            }

            var command_name = arguments[0].String.ToUpper();
            arguments.RemoveAt(0); // Remove command name from arguments

            if (!commands.ContainsKey(command_name)) {
                IssueErrorMessage("Command {0} could not be found", command_name);
                return;
            }

            RunCommand(command_name, arguments.ToArray());
        }

        public void RunCommand(string command_name, CommandArg[] arguments)
        {
            var    command       = commands[command_name];
            var    arg_count     = arguments.Length;
            string error_message = null;
            var    required_arg  = 0;

            if (arg_count < command.MinArgCount) {
                if (command.MinArgCount == command.MaxArgCount) {
                    error_message = "exactly";
                }
                else {
                    error_message = "at least";
                }

                required_arg = command.MinArgCount;
            }
            else if (command.MaxArgCount > -1 && arg_count > command.MaxArgCount) {
                // Do not check max allowed number of arguments if it is -1
                if (command.MinArgCount == command.MaxArgCount) {
                    error_message = "exactly";
                }
                else {
                    error_message = "at most";
                }

                required_arg = command.MaxArgCount;
            }

            if (error_message != null) {
                var plural_fix = required_arg == 1 ? "" : "s";
                IssueErrorMessage(
                    "{0} requires {1} {2} argument{3}",
                    command_name,
                    error_message,
                    required_arg,
                    plural_fix
                );
                return;
            }

            command.Proc(arguments);
        }

        public void AddCommand(string name, CommandInfo info)
        {
            name = name.ToUpper();

            if (commands.ContainsKey(name)) {
                IssueErrorMessage("Command {0} is already defined.", name);
                return;
            }

            commands.Add(name, info);
        }

        public void AddCommand(string name,
            Action<CommandArg[]> proc,
            int min_arg_count = 0,
            int max_arg_count = -1,
            string help = "")
        {
            var info = new CommandInfo() {
                Proc        = proc,
                MinArgCount = min_arg_count,
                MaxArgCount = max_arg_count,
                Help        = help
            };

            AddCommand(name, info);
        }

        public void IssueErrorMessage(string format, params object[] message)
        {
            IssuedErrorMessage = string.Format(format, message);
        }

        string InferCommandName(string method_name)
        {
            string command_name;
            var    index = method_name.IndexOf("COMMAND", StringComparison.CurrentCultureIgnoreCase);

            if (index >= 0) {
                // Method is prefixed, suffixed with, or contains "COMMAND".
                command_name = method_name.Remove(index, 7);
            }
            else {
                command_name = method_name;
            }

            return command_name;
        }

        string InferFrontCommandName(string method_name)
        {
            var index = method_name.IndexOf("FRONT", StringComparison.CurrentCultureIgnoreCase);
            return index >= 0 ? method_name.Remove(index, 5) : null;
        }

        void HandleRejectedCommands(Dictionary<string, CommandInfo> rejected_commands)
        {
            foreach (var command in rejected_commands) {
                if (commands.ContainsKey(command.Key)) {
                    commands[command.Key] = new CommandInfo() {
                        Proc        = commands[command.Key].Proc,
                        MinArgCount = command.Value.MinArgCount,
                        MaxArgCount = command.Value.MaxArgCount,
                        Help        = command.Value.Help
                    };
                }
                else {
                    IssueErrorMessage("{0} is missing a front command.", command);
                }
            }
        }

        CommandInfo CommandFromParamInfo(ParameterInfo[] parameters, string help)
        {
            var optional_args = 0;

            foreach (var param in parameters) {
                if (param.IsOptional) {
                    optional_args += 1;
                }
            }

            return new CommandInfo() {
                Proc        = null,
                MinArgCount = parameters.Length - optional_args,
                MaxArgCount = parameters.Length,
                Help        = help
            };
        }

        CommandArg EatArgument(ref string s)
        {
            var arg         = new CommandArg();
            var space_index = s.IndexOf(' ');

            if (space_index >= 0) {
                arg.String = s.Substring(0, space_index);
                s          = s.Substring(space_index + 1); // Remaining
            }
            else {
                arg.String = s;
                s          = "";
            }

            return arg;
        }
    }
}