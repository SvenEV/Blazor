using Microsoft.AspNetCore.Blazor.Browser.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamzor.UI.Components;

namespace Xamzor.UI
{
    public static class Application
    {
        private static readonly Dictionary<string, UIElement> _danglingElements =
            new Dictionary<string, UIElement>();

        private static readonly Dictionary<string, ApplicationView> _views =
            new Dictionary<string, ApplicationView>();

        public static IReadOnlyDictionary<string, ApplicationView> Views => _views;

        public static event Action WindowResized;

        public static void RegisterView(XamzorView root)
        {
            if (_views.ContainsKey(root.Id))
                throw new InvalidOperationException();

            var view = new ApplicationView(root);
            root.AttachToView(view);
            _views.Add(root.Id, view);

            RegisteredFunction.Invoke<string>("onViewRegistered", root.Id);
        }

        public static void UnregisterView(string id)
        {
            if (_views.TryGetValue(id, out var view))
            {
                view.Root.DetachFromView();
                _views.Remove(id);
            }
            RegisteredFunction.Invoke<string>("onViewUnregistered", id);
        }

        public static void RegisterElement(UIElement element)
        {
            // Since we don't know yet which view the element belongs to, it is added to
            // "dangling elements" first, until it is also detected in DOM (via JSRegisterElement).
            _danglingElements.Add(element.Id, element);
            UILog.Write("APP", $"RegisterElement({element})");
        }

        public static void UnregisterElement(UIElement element)
        {
            if (!_danglingElements.Remove(element.Id))
            {
                element.View.VisualTree.UnregisterElement(element);
                element.DetachFromView();
            }
            UILog.Write("APP", $"UnregisterElement({element})");
        }

        public static void JSNotifyWindowResized()
        {
            WindowResized?.Invoke();
        }

        public static void JSRegisterElement(string viewId, string parentId, string childId)
        {
            if (Views.TryGetValue(viewId, out var view) &&
                _danglingElements.TryGetValue(childId, out var child))
            {
                view.VisualTree.RegisterElement(parentId, child);
                child.AttachToView(view);
                _danglingElements.Remove(childId);

                UILog.Write("APP", $"JSRegisterElement({viewId}, {parentId}, {child})");
            }
        }
    }

    public class ApplicationView
    {
        public XamzorView Root { get; }

        public VisualTree VisualTree { get; }

        public ApplicationView(XamzorView root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            VisualTree = new VisualTree(root);
        }
    }

    public class VisualTree
    {
        private readonly Dictionary<string, Entry> _components =
            new Dictionary<string, Entry>();

        public VisualTree(XamzorView root)
        {
            _components.Add(root.Id, new Entry(root, null));
        }

        public void RegisterElement(string parentId, UIElement child)
        {
            var parentEntry = FindEntry(parentId);

            if (parentEntry == null)
                throw new InvalidOperationException("Parent doesn't exist: " + parentId);

            var childEntry = new Entry(child, parentId);
            _components.Add(child.Id, childEntry);

            parentEntry.Children.Add(child.Id);
            childEntry.Parent = parentId;

            UILog.Write("HIERARCHY", $"'{parentEntry.Element}' has child '{child}'");
        }

        public void UnregisterElement(UIElement element)
        {
            var entry = FindEntry(element.Id);

            var parent = FindEntry(entry?.Parent);
            parent?.Children.Remove(element.Id);

            foreach (var child in entry?.Children)

                _components.Remove(element.Id);
        }

        public UIElement FindElement(string id) => FindEntry(id)?.Element;

        public UIElement Parent(UIElement element) =>
            FindEntry(element?.Id) is Entry entry
                ? FindElement(entry.Parent)
                : null;

        public IEnumerable<UIElement> Children(UIElement element) =>
            FindEntry(element?.Id) is Entry entry
                ? entry.Children.Select(FindElement).Where(child => child != null)
                : Enumerable.Empty<UIElement>();

        private  Entry FindEntry(string id) =>
            id != null && _components.TryGetValue(id, out var entry) ? entry : null;

        public void Dump()
        {
            var roots = _components.Values.Where(entry => FindElement(entry.Parent) == null).ToList();

            foreach (var root in roots)
                DumpRecursively(root);

            void DumpRecursively(Entry root)
            {
                using (UILog.BeginScope("HIERARCHYDUMP", root.Element.ToString(), writeBraces: true))
                    foreach (var child in root.Children.Select(FindEntry).Where(e => e != null))
                        DumpRecursively(child);
            }
        }

        class Entry
        {
            public UIElement Element { get; }

            public string Parent { get; set; }

            public List<string> Children { get; set; } = new List<string>();

            public Entry(UIElement element, string parent)
            {
                Element = element ?? throw new ArgumentNullException(nameof(element));
                Parent = parent;
            }
        }
    }

    public static class VisualTreeHelper
    {
        public static UIElement Parent(this UIElement element) =>
            element.View.VisualTree.Parent(element);

        public static IEnumerable<UIElement> Children(this UIElement element) =>
            element.View.VisualTree.Children(element);
    }
}
