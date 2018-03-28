using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public class SvensBlazorTests
    {
        [Fact]
        public void ComponentChildrenStuffWorks()
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
            Assert.Collection(component.Children,
                child => Assert.True(child is TestComponent2 c && c.Tag == "A"),
                child => Assert.True(child is TestComponent2 c && c.Tag == "C"));

            Assert.Collection(component.Children.ElementAt(0).Children,
                child => Assert.True(child is TestComponent2 c && c.Tag == "B"));

            Assert.Empty(component.Children.ElementAt(0).Children.ElementAt(0).Children);
            Assert.Empty(component.Children.ElementAt(1).Children);
        }
        
        private class TestComponent : IComponent
        {
            protected RenderFragment _renderFragment;
            protected RenderHandle _renderHandle;

            public TestComponent Parent => _renderHandle.GetParent() as TestComponent;

            public IEnumerable<TestComponent> Children => _renderHandle.GetChildren().OfType<TestComponent>();

            public TestComponent(RenderFragment renderFragment = null) => 
                _renderFragment = renderFragment;

            public void SetParameters(ParameterCollection parameters)
            {
                parameters.AssignToProperties(this);
                TriggerRender();
            }

            public void TriggerRender()
                => _renderHandle.Render(_renderFragment);

            public void Init(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }
        }

        private class TestComponent2 : TestComponent
        {
            public string Tag { get; set; }

            public TestComponent2()
            {
                _renderFragment = Render;
            }

            public RenderFragment ChildContent { get; set; }
            
            public override string ToString() => Tag;

            private void Render(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
        }
    }
}
