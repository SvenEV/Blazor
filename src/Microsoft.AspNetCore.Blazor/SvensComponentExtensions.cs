using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Blazor
{
    public static class SvensComponentExtensions
    {
        public static readonly ConditionalWeakTable<IComponent, IReadOnlyList<IComponent>> _children =
            new ConditionalWeakTable<IComponent, IReadOnlyList<IComponent>>();

        public static readonly ConditionalWeakTable<IComponent, Action<IReadOnlyList<IComponent>>> _childrenChangedHandlers =
            new ConditionalWeakTable<IComponent, Action<IReadOnlyList<IComponent>>>();

        public static void SetChildren(this IComponent component, IReadOnlyList<IComponent> children)
        {
            var oldList = _children.TryGetValue(component, out var list) ? list : null;
            _children.Remove(component);
            _children.Add(component, children);

            if ((oldList == null || !oldList.SequenceEqual(children)) &&
                _childrenChangedHandlers.TryGetValue(component, out var handler))
            {
                handler?.Invoke(children);
            }
        }

        public static IReadOnlyList<IComponent> GetChildren(this IComponent component) =>
            _children.TryGetValue(component, out var children) ? children : null;

        public static void SetChildrenChangedHandler(this IComponent component, Action<IReadOnlyList<IComponent>> handler)
        {
            _childrenChangedHandlers.Remove(component);
            _childrenChangedHandlers.Add(component, handler);
        }

        public static IReadOnlyList<IComponent> DetermineChildrenInTree(RenderTreeFrame[] frames, int frameCount)
        {
            var rootChildren = new List<IComponent>();

            for (var i = 0; i < frameCount; i++)
            {
                var frame = frames[i];

                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Element:
                        //i += frame.ElementSubtreeLength; // skip element subtree
                        break;

                    case RenderTreeFrameType.Component:
                        // add component instance as child to current parent
                        rootChildren.Add(frame.Component);
                        i += frame.ElementSubtreeLength; // skip component children (should only be attributes)
                        break;
                }
            }

            return rootChildren;
        }

        public static IReadOnlyList<IComponent> DetermineChildrenInTree_UNNECESSARILY_DEEP(RenderTreeFrame[] frames, int frameCount)
        {
            var rootChildren = new List<IComponent>();
            var ancestorStack = new Stack<(int startIndex, RenderTreeFrame frame, List<IComponent> children)>();

            for (var i = 0; i < frameCount; i++)
            {
                var frame = frames[i];

                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Element:
                        i += frame.ElementSubtreeLength; // skip element subtree
                        break;

                    case RenderTreeFrameType.Component:
                        // add component instance as child to current parent
                        var childList = (ancestorStack.Count > 0)
                            ? ancestorStack.Peek().children
                            : rootChildren;

                        childList.Add(frame.Component);

                        // recurse into component subtree
                        ancestorStack.Push((i, frame, new List<IComponent>()));
                        break;
                }

                while (ancestorStack.Count > 0 &&
                    ancestorStack.Peek() is var parentEntry &&
                    i == parentEntry.startIndex + parentEntry.frame.ComponentSubtreeLength - 1)
                {
                    // component closing - assign collected children & pop from stack
                    parentEntry.frame.Component.SetChildren(parentEntry.children);
                    ancestorStack.Pop();
                }
            }

            return rootChildren;
        }
    }
}
