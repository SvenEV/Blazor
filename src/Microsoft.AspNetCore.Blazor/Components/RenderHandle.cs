// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Allows a component to notify the renderer that it should be rendered.
    /// </summary>
    public readonly struct RenderHandle
    {
        private readonly Renderer _renderer;
        private readonly int _componentId;

        public event Action<IComponent> ChildrenChanged
        {
            add { VerifyInitialized(); _renderer.GetRequiredComponentState(_componentId).ChildrenChanged += value; }
            remove { VerifyInitialized(); _renderer.GetRequiredComponentState(_componentId).ChildrenChanged -= value; }
        }

        internal RenderHandle(Renderer renderer, int componentId)
        {
            _renderer = renderer ?? throw new System.ArgumentNullException(nameof(renderer));
            _componentId = componentId;
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="RenderHandle"/> has been
        /// initialised and is ready to use.
        /// </summary>
        public bool IsInitialized
            => _renderer != null;

        /// <summary>
        /// Notifies the renderer that the component should be rendered.
        /// </summary>
        /// <param name="renderFragment">The content that should be rendered.</param>
        public void Render(RenderFragment renderFragment)
        {
            VerifyInitialized();
            _renderer.AddToRenderQueue(_componentId, renderFragment);
        }

        public IComponent GetParent()
        {
            VerifyInitialized();
            var ownState = _renderer.GetRequiredComponentState(_componentId);
            return (ownState.ParentComponentId == -1)
                ? null
                : _renderer.GetRequiredComponentState(ownState.ParentComponentId)._component;
        }

        public IEnumerable<IComponent> GetChildren()
        {
            VerifyInitialized();
            var ownState = _renderer.GetRequiredComponentState(_componentId);
            var renderer = _renderer;
            return ownState.ChildComponentIds.Select(id => renderer.GetRequiredComponentState(id)._component);
        }

        private void VerifyInitialized()
        {
            if (_renderer == null)
                throw new InvalidOperationException("The render handle is not yet assigned.");
        }
    }
}
