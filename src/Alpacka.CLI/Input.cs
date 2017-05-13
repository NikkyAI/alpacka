using System;
using System.Collections.Generic;

namespace Alpacka.CLI
{
    public static class Input
    {
        public static string Text(string promt)
        {
            Console.Write($"{ promt }: ");
            return Console.ReadLine();
        }
        public static string Text(string promt, string @default)
        {
            var text = Text($"{ promt } (\"{ @default }\")");
            return (text.Length > 0) ? text : @default;
        }
        
        public static T Select<T>(string promt, IList<T> options) =>
            Select(promt, options, 0);
        public static T Select<T>(string promt, IList<T> options, T @default) =>
            Select(promt, options, options.IndexOf(@default));
        public static T Select<T>(string promt, IList<T> options, int startIndex)
        {
            var currentIndex = startIndex;
            
            Console.Write($"{ promt }: ");
            
            var currentSelectionLength = 0;
            var startCursorLeft = Console.CursorLeft;
            var startCursorTop  = Console.CursorTop;
            
            void UpdateIndex(int newIndex)
            {
                Console.SetCursorPosition(startCursorLeft, startCursorTop);
                var selectionText = $"{ options[newIndex] } [{ newIndex + 1 }/{ options.Count }]";
                Console.Write(selectionText);
                var newSelectionLength = selectionText.Length;
                if (newSelectionLength < currentSelectionLength) {
                    var selectionCursorLeft = Console.CursorLeft;
                    var selectionCursorTop  = Console.CursorTop;
                    Console.Write(new string(' ', currentSelectionLength - newSelectionLength));
                    Console.SetCursorPosition(selectionCursorLeft, selectionCursorTop);
                }
                currentSelectionLength = newSelectionLength;
                currentIndex = newIndex;
            }
            
            UpdateIndex(startIndex);
            
            while (true) {
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;
                var chr = keyInfo.KeyChar;
                
                switch (key) {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return options[currentIndex];
                    
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.UpArrow:
                        if (currentIndex <= 0) break;
                        UpdateIndex(currentIndex - 1);
                        break;
                    
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.DownArrow:
                        if (currentIndex >= options.Count - 1) break;
                        UpdateIndex(currentIndex + 1);
                        break;
                    
                    // TODO: If entering text, search for that text?
                }
            }
        }
    }
}
