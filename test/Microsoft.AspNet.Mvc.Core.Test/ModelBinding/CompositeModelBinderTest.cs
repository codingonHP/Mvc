// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CompositeModelBinderTest
    {
        [Fact]
        public async Task BindModel_SuccessfulBind_ReturnsModel()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(
                    delegate (ModelBindingContext context)
                    {
                        Assert.Same(bindingContext.ModelMetadata, context.ModelMetadata);
                        Assert.Equal("someName", context.ModelName);
                        Assert.Same(bindingContext.ValueProvider, context.ValueProvider);


                        return Task.FromResult(
                            new ModelBindingResult(model: 42, key: "someName", isModelSet: true));
                    });
            var shimBinder = CreateCompositeBinder(mockIntBinder.Object);

            // Act
            var result = await shimBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);
        }

        [Fact]
        public async Task BindModel_SuccessfulBind_ComplexTypeFallback_ReturnsModel()
        {
            // Arrange
            var expectedModel = new List<int> { 1, 2, 3, 4, 5 };

            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(
                    delegate (ModelBindingContext mbc)
                    {
                        if (!string.IsNullOrEmpty(mbc.ModelName))
                        {
                            return Task.FromResult<ModelBindingResult>(null);
                        }

                        Assert.Same(bindingContext.ModelMetadata, mbc.ModelMetadata);
                        Assert.Equal("", mbc.ModelName);
                        Assert.Same(bindingContext.ValueProvider, mbc.ValueProvider);

                        return Task.FromResult(new ModelBindingResult(expectedModel, string.Empty, true));
                    });

            var shimBinder = CreateCompositeBinder(mockIntBinder.Object);

            // Act
            var result = await shimBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Equal(string.Empty, result.Key);
            Assert.Equal(expectedModel, result.Model);
        }

        [Fact]
        public async Task ModelBinder_ReturnsNull_IfBinderMatchesButDoesNotSetModel()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(new ModelBindingResult(model: null, key: "someName", isModelSet: false)));

            var composite = CreateCompositeBinder(modelBinder.Object);

            // Act
            var result = await composite.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ModelBinder_FallsBackToEmpty_IfBinderMatchesButDoesNotSetModel()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var count = 0;
            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback<ModelBindingContext>(context =>
                {
                    // Expect two calls; the second with empty ModelName.
                    Assert.InRange(count, 0, 1);
                    count++;
                    if (count == 1)
                    {
                        Assert.Equal("someName", context.ModelName);
                    }
                    else
                    {
                        Assert.Empty(context.ModelName);
                    }
                })
                .Returns(Task.FromResult(new ModelBindingResult(model: null, key: "someName", isModelSet: false)));

            var composite = CreateCompositeBinder(modelBinder.Object);

            // Act & Assert
            var result = await composite.BindModelAsync(bindingContext);
        }

        [Fact]
        public async Task ModelBinder_DoesNotFallBackToEmpty_IfFallbackToEmptyPrefixFalse()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = false,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback<ModelBindingContext>(context =>
                {
                    Assert.Equal("someName", context.ModelName);
                })
                .Returns(Task.FromResult(new ModelBindingResult(model: null, key: "someName", isModelSet: false)))
                .Verifiable();

            var composite = CreateCompositeBinder(modelBinder.Object);

            // Act & Assert
            var result = await composite.BindModelAsync(bindingContext);
            modelBinder.Verify(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()), Times.Once);
        }

        [Fact]
        public async Task ModelBinder_DoesNotFallBackToEmpty_IfErrorsAreAdded()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = false,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback<ModelBindingContext>(context =>
                {
                    Assert.Equal("someName", context.ModelName);
                    context.ModelState.AddModelError(context.ModelName, "this is an error message");
                })
                .Returns(Task.FromResult(new ModelBindingResult(model: null, key: "someName", isModelSet: false)))
                .Verifiable();

            var composite = CreateCompositeBinder(modelBinder.Object);

            // Act & Assert
            var result = await composite.BindModelAsync(bindingContext);
            modelBinder.Verify(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()), Times.Once);
        }

        [Fact]
        public async Task ModelBinder_ReturnsNotNull_SetsNullValue_SetsModelStateKey()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = GetValidatorProvider()
                }
            };

            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(new ModelBindingResult(model: null, key: "someName", isModelSet: true)));

            var composite = CreateCompositeBinder(modelBinder.Object);

            // Act
            var result = await composite.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Equal("someName", result.Key);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task BindModel_UnsuccessfulBind_BinderFails_ReturnsNull()
        {
            // Arrange
            var mockListBinder = new Mock<IModelBinder>();
            mockListBinder.Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                          .Returns(Task.FromResult<ModelBindingResult>(null))
                          .Verifiable();

            var shimBinder = mockListBinder.Object;

            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = false,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
            };

            // Act
            var result = await shimBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
            Assert.True(bindingContext.ModelState.IsValid);
            mockListBinder.Verify();
        }

        [Fact]
        public async Task BindModel_UnsuccessfulBind_SimpleTypeNoFallback_ReturnsNull()
        {
            // Arrange
            var innerBinder = Mock.Of<IModelBinder>();
            var shimBinder = CreateCompositeBinder(innerBinder);

            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)),
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = Mock.Of<OperationBindingContext>(),
            };

            // Act
            var result = await shimBinder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task BindModel_WithDefaultBinders_BindsSimpleType()
        {
            // Arrange
            var binder = CreateBinderWithDefaults();

            var valueProvider = new SimpleHttpValueProvider
            {
                { "firstName", "firstName-value"},
                { "lastName", "lastName-value"}
            };
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<SimplePropertiesModel>(result.Model);
            Assert.Equal("firstName-value", model.FirstName);
            Assert.Equal("lastName-value", model.LastName);

            Assert.NotNull(result.ValidationNode);
            Assert.Equal(2, result.ValidationNode.ChildNodes.Count);
            Assert.Equal("", result.ValidationNode.Key);
            Assert.Equal(bindingContext.ModelMetadata, result.ValidationNode.ModelMetadata);
            model = Assert.IsType<SimplePropertiesModel>(result.ValidationNode.Model);
            Assert.Equal("firstName-value", model.FirstName);
            Assert.Equal("lastName-value", model.LastName);

            Assert.Equal(2, result.ValidationNode.ChildNodes.Count);

            var validationNode = result.ValidationNode.ChildNodes[0];
            Assert.Equal("FirstName", validationNode.Key);
            Assert.Equal("firstName-value", validationNode.Model);
            Assert.Empty(validationNode.ChildNodes);

            validationNode = result.ValidationNode.ChildNodes[1];
            Assert.Equal("LastName", validationNode.Key);
            Assert.Equal("lastName-value", validationNode.Model);
            Assert.Empty(validationNode.ChildNodes);
        }

        [Fact]
        public async Task BindModel_WithDefaultBinders_BindsComplexType()
        {
            // Arrange
            var binder = CreateBinderWithDefaults();

            var valueProvider = new SimpleHttpValueProvider
            {
                { "firstName", "firstName-value"},
                { "lastName", "lastName-value"},
                { "friends[0].firstName", "first-friend"},
                { "friends[0].age", "40"},
                { "friends[0].friends[0].firstname", "nested friend"},
                { "friends[1].firstName", "some other"},
                { "friends[1].lastName", "name"},
                { "resume", "4+mFeTp3tPF=" }
            };

            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(Person));

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<Person>(result.Model);
            Assert.Equal("firstName-value", model.FirstName);
            Assert.Equal("lastName-value", model.LastName);
            Assert.Equal(2, model.Friends.Count);
            Assert.Equal("first-friend", model.Friends[0].FirstName);
            Assert.Equal(40, model.Friends[0].Age);
            var nestedFriend = Assert.Single(model.Friends[0].Friends);
            Assert.Equal("nested friend", nestedFriend.FirstName);
            Assert.Equal("some other", model.Friends[1].FirstName);
            Assert.Equal("name", model.Friends[1].LastName);
            Assert.Equal(new byte[] { 227, 233, 133, 121, 58, 119, 180, 241 }, model.Resume);
        }

        [Fact]
        public async Task BindModel_DoesNotAddAValidationNode_IfModelIsNotSet()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();
            var mockBinder = new Mock<IModelBinder>();
            mockBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(
                    delegate (ModelBindingContext context)
                    {
                        return Task.FromResult(
                            new ModelBindingResult(model: 42, key: "someName", isModelSet: false));
                    });
            var binder = CreateCompositeBinder(mockBinder.Object);
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task BindModel_DoesNotAddAValidationNode_IfModelBindingResultIsNull()
        {
            // Arrange
            var mockBinder = new Mock<IModelBinder>();
            mockBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(null));
            var binder = CreateCompositeBinder(mockBinder.Object);
            var valueProvider = new SimpleHttpValueProvider();
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task BindModel_UsesTheValidationNodeOnModelBindingResult_IfPresent()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();
            ModelValidationNode validationNode = null;

            var mockBinder = new Mock<IModelBinder>();
            mockBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(
                    delegate (ModelBindingContext context)
                    {
                        validationNode = new ModelValidationNode("someName", context.ModelMetadata, 42);
                        return Task.FromResult(
                            new ModelBindingResult(42, "someName", isModelSet: true, validationNode: validationNode));
                    });
            var binder = CreateCompositeBinder(mockBinder.Object);
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Same(validationNode, result.ValidationNode);
        }

        private static ModelBindingContext CreateBindingContext(IModelBinder binder,
                                                                IValueProvider valueProvider,
                                                                Type type,
                                                                IModelValidatorProvider validatorProvider = null)
        {
            validatorProvider = validatorProvider ?? GetValidatorProvider();
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var bindingContext = new ModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = metadataProvider.GetMetadataForType(type),
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = metadataProvider,
                    ModelBinder = binder,
                    ValidatorProvider = validatorProvider
                }
            };
            return bindingContext;
        }

        private static CompositeModelBinder CreateBinderWithDefaults()
        {
            var binders = new IModelBinder[]
            {
                new TypeMatchModelBinder(),
                new ByteArrayModelBinder(),
                new GenericModelBinder(),
                new TypeConverterModelBinder(),
                new MutableObjectModelBinder()
            };

            var binder = new CompositeModelBinder(binders);
            return binder;
        }

        private static CompositeModelBinder CreateCompositeBinder(IModelBinder mockIntBinder)
        {
            var shimBinder = new CompositeModelBinder(new[] { mockIntBinder });
            return shimBinder;
        }

        private static IModelValidatorProvider GetValidatorProvider(params IModelValidator[] validators)
        {
            var provider = new Mock<IModelValidatorProvider>();
            provider
                .Setup(v => v.GetValidators(It.IsAny<ModelValidatorProviderContext>()))
                .Callback<ModelValidatorProviderContext>(c =>
                {
                    if (validators == null)
                    {
                        return;
                    }

                    foreach (var validator in validators)
                    {
                        c.Validators.Add(validator);
                    }
                });

            return provider.Object;
        }

        private class SimplePropertiesModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private sealed class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public int Age { get; set; }

            public List<Person> Friends { get; set; }

            public byte[] Resume { get; set; }
        }

        private class User : IValidatableObject
        {
            public string Password { get; set; }

            [Compare("Password")]
            public string ConfirmPassword { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Password == "password")
                {
                    yield return new ValidationResult("Password does not meet complexity requirements.");
                }
            }
        }
    }
}
#endif
