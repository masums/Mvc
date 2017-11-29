﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class PartialTagHelperTest
    {
        [Fact]
        public async Task ProcessAsync_RendersPartialView_IfGetViewReturnsView()
        {
            // Arrange
            var expected = "Hello world!";
            var bufferScope = new TestViewBufferScope();
            var partialName = "_Partial";
            var model = new object();
            var viewContext = GetViewContext();

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback((ViewContext v) =>
                {
                    v.Writer.Write(expected);
                })
                .Returns(Task.CompletedTask);

            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
                .Returns(ViewEngineResult.Found(partialName, view.Object));

            var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
            {
                Name = partialName,
                Model = model,
                ViewContext = viewContext,
            };
            var tagHelperContext = GetTagHelperContext();
            var output = GetTagHelperOutput();

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ProcessAsync_RendersPartialView_IfFindViewReturnsView()
        {
            // Arrange
            var expected = "Hello world!";
            var bufferScope = new TestViewBufferScope();
            var partialName = "_Partial";
            var model = new object();
            var viewContext = GetViewContext();

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback((ViewContext v) =>
                {
                    v.Writer.Write(expected);
                })
                .Returns(Task.CompletedTask);

            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
                .Returns(ViewEngineResult.NotFound(partialName, new[] { partialName }));

            viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
                .Returns(ViewEngineResult.Found(partialName, view.Object));

            var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
            {
                Name = partialName,
                Model = model,
                ViewContext = viewContext,
            };
            var tagHelperContext = GetTagHelperContext();
            var output = GetTagHelperOutput();

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ProcessAsync_UsesViewDataFromContext()
        {
            // Arrange
            var expected = "Hello world!";
            var bufferScope = new TestViewBufferScope();
            var partialName = "_Partial";
            var model = new object();
            var viewContext = GetViewContext();
            viewContext.ViewData["key"] = expected;

            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback((ViewContext v) =>
                {
                    v.Writer.Write(v.ViewData["key"]);
                })
                .Returns(Task.CompletedTask);

            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
                .Returns(ViewEngineResult.NotFound(partialName, new[] { partialName }));

            viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
                .Returns(ViewEngineResult.Found(partialName, view.Object));

            var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
            {
                Name = partialName,
                Model = model,
                ViewContext = viewContext,
            };
            var tagHelperContext = GetTagHelperContext();
            var output = GetTagHelperOutput();

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ProcessAsync_UsesPassedInViewData_WhenNotNull()
        {
            // Arrange
            var expected = "Hello world!";
            var bufferScope = new TestViewBufferScope();
            var partialName = "_Partial";
            var model = new object();
            var viewData = new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary());
            viewData["key"] = expected;
            var viewContext = GetViewContext();
            viewContext.ViewData["key"] = "ViewContext";


            var view = new Mock<IView>();
            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback((ViewContext v) =>
                {
                    v.Writer.Write(v.ViewData["key"]);
                })
                .Returns(Task.CompletedTask);

            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
                .Returns(ViewEngineResult.Found(partialName, view.Object));

            var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
            {
                Name = partialName,
                Model = model,
                ViewContext = viewContext,
                ViewData = viewData,
            };
            var tagHelperContext = GetTagHelperContext();
            var output = GetTagHelperOutput();

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ProcessAsync_DisposesViewInstance()
        {
            // Arrange
            var expected = "Hello world!";
            var bufferScope = new TestViewBufferScope();
            var partialName = "_Partial";
            var model = new object();
            var viewContext = GetViewContext();

            var disposable = new Mock<IDisposable>();
            disposable.Setup(d => d.Dispose()).Verifiable();
            var view = disposable.As<IView>();

            view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback((ViewContext v) =>
                {
                    v.Writer.Write(expected);
                })
                .Returns(Task.CompletedTask);

            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
                .Returns(ViewEngineResult.Found(partialName, view.Object));

            var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
            {
                Name = partialName,
                Model = model,
                ViewContext = viewContext,
            };
            var tagHelperContext = GetTagHelperContext();
            var output = GetTagHelperOutput();

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            disposable.Verify();
        }

        [Fact]
        public async Task ProcessAsync_Throws_IfGetViewAndFindReturnNotFoundResults()
        {
            // Arrange
            var bufferScope = new TestViewBufferScope();
            var partialName = "_Partial";
            var expected = string.Join(Environment.NewLine,
                $"The partial view '{partialName}' was not found. The following locations were searched:",
                "NotFound1",
                "NotFound2",
                "NotFound3",
                "NotFound4");
            var viewData = new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary());
            var viewContext = GetViewContext();

            var view = Mock.Of<IView>();
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
                .Returns(ViewEngineResult.NotFound(partialName, new[] { "NotFound1", "NotFound2" }));

            viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
                .Returns(ViewEngineResult.NotFound(partialName, new[] { $"NotFound3", $"NotFound4" }));

            var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
            {
                Name = partialName,
                ViewContext = viewContext,
                ViewData = viewData,
            };
            var tagHelperContext = GetTagHelperContext();
            var output = GetTagHelperOutput();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => tagHelper.ProcessAsync(tagHelperContext, output));
            Assert.Equal(expected, exception.Message);
        }

        private static ViewContext GetViewContext()
        {
            return new ViewContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                NullView.Instance,
                new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary()),
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());
        }

        private static TagHelperContext GetTagHelperContext()
        {
            return new TagHelperContext(
                "partial",
                new TagHelperAttributeList(),
                new Dictionary<object, object>(),
                Guid.NewGuid().ToString("N"));
        }

        private static TagHelperOutput GetTagHelperOutput()
        {
            return new TagHelperOutput(
                "partial",
                new TagHelperAttributeList(),
                (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        }
    }
}
