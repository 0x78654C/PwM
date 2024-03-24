namespace PwM.Utils
{
    public static class UI
    {
        /// <summary>
        /// Write error output in color Red.
        /// </summary>
        /// <param name="text"></param>
        public static void ErrorWriteLine(string text)
        {
            var currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {text}");
            Console.ForegroundColor = currentForeground;

        }

        /// <summary>
        /// Change color of a specific line in console.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleTextLine(ConsoleColor color, string text)
        {
            var currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = currentForeground;
        }

        /// <summary>
        /// Change color of a specific text in console.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        public static void ColorConsoleText(ConsoleColor color, string text)
        {
            var currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = currentForeground;
        }
    }
}