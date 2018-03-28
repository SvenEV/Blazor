using System;
using System.Threading;

namespace StandaloneApp.UI
{
    public static class UILog
    {
        private static readonly ThreadLocal<int> _depth = new ThreadLocal<int>();

        public static IDisposable BeginScope(string category, string enterText, Func<string> exitText = null, bool writeBraces = false)
        {
            var scope = new LogScope(() =>
            {
                _depth.Value--;
                if (exitText != null || writeBraces)
                    Write(category, (writeBraces ? "} " : "") + (exitText?.Invoke() ?? ""));
            });

            if (enterText != null)
                Write(category, enterText);
            if (writeBraces)
                Write(category, "{");
            _depth.Value++;

            return scope;
        }

        public static void Write(string category, string text)
        {
            Console.WriteLine($"[{category}] " + "".PadLeft(4 * _depth.Value) + text);
        }

        class LogScope : IDisposable
        {
            private readonly Action Disposed;
            public LogScope(Action disposed) => Disposed = disposed;
            public void Dispose() => Disposed?.Invoke();
        }
    }
}
