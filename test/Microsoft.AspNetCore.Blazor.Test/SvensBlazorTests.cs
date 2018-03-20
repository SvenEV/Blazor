using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public class SvensBlazorTests
    {
        [Fact]
        public void ComponentGetsAssignedChildren()
        {
            // Arrange
            var renderer = new TestRenderer();
            var component = new TestComponent(builder =>
            {
                builder.OpenComponent<TestComponent2>(0);
                builder.AddAttribute(1, "Tag", "A");
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(builder2 =>
                {
                    builder2.OpenComponent<TestComponent2>(2);
                    builder2.AddAttribute(3, "Tag", "B");
                    builder2.CloseComponent();
                }));
                builder.CloseComponent();

                builder.OpenComponent<TestComponent2>(4);
                builder.AddAttribute(5, "Tag", "C");
                builder.CloseComponent();
            });

            // Act
            var componentId = renderer.AssignComponentId(component);
            component.TriggerRender();

            // Assert
            Assert.Collection(component.GetChildren(),
                child => Assert.True(child is TestComponent2 c && c.Tag == "A"),
                child => Assert.True(child is TestComponent2 c && c.Tag == "C"));

            Assert.Collection(component.GetChildren()[0].GetChildren(),
                child => Assert.True(child is TestComponent2 c && c.Tag == "B"));

            Assert.Empty(component.GetChildren()[0].GetChildren()[0].GetChildren());
            Assert.Empty(component.GetChildren()[1].GetChildren());
        }

        private class TestComponent : IComponent
        {
            private RenderHandle _renderHandle;
            private RenderFragment _renderFragment;

            public IReadOnlyList<IComponent> Children => this.GetChildren();

            public TestComponent(RenderFragment renderFragment)
            {
                _renderFragment = renderFragment;
            }

            public void Init(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public void SetParameters(ParameterCollection parameters)
                => TriggerRender();

            public void TriggerRender()
                => _renderHandle.Render(_renderFragment);
        }

        private class TestComponent2 : BlazorComponent
        {
            public string Tag { get; set; }

            public RenderFragment ChildContent { get; set; }

            public IReadOnlyList<IComponent> Children => this.GetChildren();

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                base.BuildRenderTree(builder);
                builder.AddContent(0, ChildContent);
            }

            public override string ToString() => Tag;
        }
    }
}
