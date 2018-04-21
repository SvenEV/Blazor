using System;
using System.Collections.Generic;
using Xamzor.UI.Components;

namespace Xamzor.UI
{
    public static class Application
    {
        private static readonly Dictionary<string, UIElement> _components =
            new Dictionary<string, UIElement>();

        public static event Action WindowResized;

        public static void RegisterElement(UIElement element) => 
            _components.Add(element.Id, element);

        public static void UnregisterElement(UIElement element) => 
            _components.Remove(element.Id);

        public static UIElement GetComponent(string id) =>
            id != null && _components.TryGetValue(id, out var e) ? e : null;

        public static void JSNotifyWindowResized() => 
            WindowResized?.Invoke();
    }
}
