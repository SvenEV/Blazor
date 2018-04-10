using System;
using System.Collections.Generic;
using System.Linq;
using Xamzor.UI.Components;

namespace Xamzor.UI
{
    public static class Hierarchy
    {
        public static event Action WindowResized;

        private static readonly Dictionary<string, Entry> _components = 
            new Dictionary<string, Entry>();

        public static void AddRelation(string parentId, string childId)
        {
            var parent = FindEntry(parentId);
            parent.Children.Add(childId);

            var child = FindEntry(childId);
            child.Parent = parentId;

            UILog.Write("HIERARCHY", $"'{parent.Element}' has child '{child.Element}'");
        }

        public static UIElement FindElement(string id) => FindEntry(id)?.Element;

        public static UIElement Parent(this UIElement element) =>
            FindEntry(element?.Id) is Entry entry
                ? FindElement(entry.Parent) 
                : null;

        public static IEnumerable<UIElement> Children(this UIElement element) =>
            FindEntry(element?.Id) is Entry entry
                ? entry.Children.Select(FindElement).Where(child => child != null)
                : Enumerable.Empty<UIElement>();

        private static Entry FindEntry(string id) =>
            id != null && _components.TryGetValue(id, out var entry) ? entry : null;

        public static void RegisterElement(UIElement element) => 
            _components.Add(element.Id, new Entry(element));

        public static void Dump()
        {
            var roots = _components.Values.Where(entry => FindElement(entry.Parent) == null).ToList();

            foreach (var root in roots)
                DumpRecursively(root);

            void DumpRecursively(Entry root)
            {
                using (UILog.BeginScope("HIERARCHY", root.Element.ToString(), writeBraces: true))
                    foreach (var child in root.Children.Select(FindEntry).Where(e => e != null))
                        DumpRecursively(child);
            }
        }

        public static void NotifyWindowResized() => WindowResized?.Invoke();

        class Entry
        {
            public UIElement Element { get; }

            public string Parent { get; set; }

            public List<string> Children { get; set; } = new List<string>();

            public Entry(UIElement element)
            {
                Element = element ?? throw new ArgumentNullException(nameof(element));
            }
        }
    }
}
